using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Zuni.Models;
using Zuni.Models.Agenda;
using Zuni.Services.MayoriaEdad;
using Zuni.Services.Privacidad;

namespace Zuni.Services.Agenda;

// Propuestas de cita de Dirección (ver docs/propuestas-director-privacidad.md).
// Reutiliza la cita "Solicitada" con ProgramadorRol = "Director" que Marlon introdujo; el psicólogo
// la acepta, reprograma o rechaza con los flujos existentes. Reglas de privacidad:
//  - el directorio solo devuelve nombre, carné y carrera, sin indicadores de atención;
//  - crear una propuesta NO exige asignación previa (exigirla revelaría quién está en atención);
//  - al crearla NO se consultan las otras citas del estudiante (salvo el choque exacto de hora que impone
//    el índice único): esas reglas se validan cuando el psicólogo acepta, para que el resultado de una
//    propuesta no revele si el estudiante tiene citas;
//  - cualquier impedimento del estudiante produce el mismo mensaje genérico;
//  - el Director solo ve sus propias propuestas y avisos administrativos, nunca texto libre.
public sealed partial class AgendaCitasService
{
    public const string MensajeEstudianteNoDisponible =
        "No es posible registrar la propuesta para este estudiante en esa fecha. Elige otra fecha u horario; si persiste, coordina con el área de psicología.";
    public const int ResultadosDirectorio = 10;
    public const int BusquedaMinima = 3;

    // Tipos de aviso dirigidos a Dirección. El historial y el contador solo consideran estos tipos;
    // avisos anteriores con texto libre (tipo "Respuesta") nunca se muestran al Director.
    public static class AvisosDirector
    {
        public const string Enviada = "Propuesta enviada";
        public const string Aceptada = "Propuesta aceptada";
        public const string Rechazada = "Propuesta rechazada";
        public const string Cancelada = "Propuesta cancelada";
        public const string Reprogramada = "Reprogramación";
        public static readonly string[] Todos = [Enviada, Aceptada, Rechazada, Cancelada, Reprogramada];
    }

    private async Task<string> DirectorActivo(ClaimsPrincipal actor, CancellationToken ct) =>
        await MayoriaEdadService.ActorAsync(db, actor, "Director", ct)
            ? actor.FindFirstValue(ClaimTypes.NameIdentifier)!
            : throw new AgendaAccesoException();

    // Psicólogos activos sin rol institucional (mismo criterio que la carga de psicólogos).
    private IQueryable<ApplicationUser> ProfesionalesElegibles() => db.Users.AsNoTracking().Where(u => u.IsActive && !u.DebeCambiarContrasena &&
        db.UserRoles.Any(ur => ur.UserId == u.Id && db.Roles.Any(r => r.Id == ur.RoleId && r.NormalizedName == "PSICOLOGO")) &&
        !db.UserRoles.Any(ur => ur.UserId == u.Id && db.Roles.Any(r => r.Id == ur.RoleId &&
            (r.NormalizedName == "ADMINISTRADOR" || r.NormalizedName == "DIRECTOR" || r.NormalizedName == "CATEDRATICO"))));

    // Directorio institucional existente: perfiles de estudiantes activos. No se consulta ninguna
    // tabla de atención (solicitudes, asignaciones, expedientes, citas, Big Five ni cuestionarios).
    private IQueryable<PerfilEstudiante> PerfilesDirectorio() => db.PerfilesEstudiante.AsNoTracking().Where(p => p.Activo && p.Usuario.IsActive &&
        db.UserRoles.Any(ur => ur.UserId == p.UsuarioId && db.Roles.Any(r => r.Id == ur.RoleId && r.NormalizedName == "ESTUDIANTE")) &&
        !db.UserRoles.Any(ur => ur.UserId == p.UsuarioId && db.Roles.Any(r => r.Id == ur.RoleId &&
            (r.NormalizedName == "ADMINISTRADOR" || r.NormalizedName == "DIRECTOR" || r.NormalizedName == "CATEDRATICO"))));

    public async Task<ProponerCitaViewModel> PrepararPropuesta(ClaimsPrincipal actor, string? busqueda, Guid? perfilId,
        PropuestaDirectorInput? input, CancellationToken ct)
    {
        await DirectorActivo(actor, ct);
        var texto = (busqueda ?? "").Trim();
        if (texto.Length > 80) texto = texto[..80];
        IReadOnlyList<EstudianteDirectorio> resultados = [];
        var hayMas = false;
        if (texto.Length >= BusquedaMinima)
        {
            var minusculas = texto.ToLower();
            var digitos = new string(texto.Where(char.IsDigit).ToArray());
            var buscarCarne = digitos.Length >= BusquedaMinima;
            // Resultados limitados (+1 solo para saber si conviene precisar la búsqueda).
            var lista = await PerfilesDirectorio()
                .Where(p => p.Usuario.FullName.ToLower().Contains(minusculas) || (buscarCarne && p.Carne.Contains(digitos)))
                .OrderBy(p => p.Usuario.FullName).ThenBy(p => p.Carne)
                .Select(p => new EstudianteDirectorio(p.Id, p.Usuario.FullName, p.Carne, p.Carrera))
                .Take(ResultadosDirectorio + 1).ToListAsync(ct);
            hayMas = lista.Count > ResultadosDirectorio;
            resultados = lista.Take(ResultadosDirectorio).ToList();
        }
        var seleccionId = input?.PerfilEstudianteId ?? perfilId;
        var seleccionado = seleccionId is null ? null : await PerfilesDirectorio().Where(p => p.Id == seleccionId)
            .Select(p => new EstudianteDirectorio(p.Id, p.Usuario.FullName, p.Carne, p.Carrera)).SingleOrDefaultAsync(ct);
        var psicologos = await ProfesionalesElegibles().OrderBy(u => u.FullName).Select(u => new SelectListItem(u.FullName, u.Id)).ToListAsync(ct);
        var hoy = AgendaReglas.Hoy(Ahora);
        var datos = input ?? new PropuestaDirectorInput { Fecha = hoy.AddDays(1), HoraInicio = new TimeOnly(9, 0) };
        datos.PerfilEstudianteId = seleccionado?.PerfilId;
        foreach (var p in psicologos) p.Selected = p.Value == datos.PsicologoId;
        return new ProponerCitaViewModel
        {
            Busqueda = texto, BusquedaCorta = texto.Length is > 0 and < BusquedaMinima, HayMasResultados = hayMas,
            Resultados = resultados, Seleccionado = seleccionado, Psicologos = psicologos, Input = datos, FechaMinima = hoy
        };
    }

    public async Task<Guid> CrearPropuestaDirector(ClaimsPrincipal actor, PropuestaDirectorInput input, CancellationToken ct)
    {
        var resultado = Guid.Empty;
        await Escribir(async () =>
        {
            var director = await DirectorActivo(actor, ct);
            if (input.PerfilEstudianteId is null || string.IsNullOrWhiteSpace(input.PsicologoId) || input.Fecha is null || input.HoraInicio is null ||
                !Enum.IsDefined(input.Modalidad) || input.DuracionMinutos is < 15 or > 120 || input.DuracionMinutos % 5 != 0)
                throw new AgendaOperacionException("Completa estudiante, psicólogo, fecha, hora, duración (15 a 120 minutos, en múltiplos de cinco) y modalidad.");
            if (input.OperacionId == Guid.Empty || await db.Citas.AnyAsync(c => c.Id == input.OperacionId, ct))
                throw new AgendaOperacionException("Esta propuesta ya se registró. Consulta Mis propuestas.");
            var psicologo = input.PsicologoId.Trim();
            if (!await ProfesionalesElegibles().AnyAsync(u => u.Id == psicologo, ct))
                throw new AgendaOperacionException("Selecciona un psicólogo disponible de la lista.");
            var inicio = AgendaReglas.Utc(input.Fecha.Value, input.HoraInicio.Value);
            var fin = inicio.AddMinutes(input.DuracionMinutos);
            if (inicio.Ticks % (5 * TimeSpan.TicksPerMinute) != 0) throw new AgendaOperacionException("Usa horas en múltiplos de cinco minutos.");
            // 1) Reglas del profesional: ya son visibles en la carga anonimizada, por eso se explican.
            await ValidarFranjaProfesional(psicologo, inicio, fin, ct);
            var perfil = input.PerfilEstudianteId.Value;
            var ahora = Ahora;
            // 2) Propuestas propias: datos que el Director ya conoce.
            if (await db.Citas.AnyAsync(c => c.ProgramadorRol == "Director" && c.ProgramadorUsuarioId == director && c.PerfilEstudianteId == perfil &&
                    c.Estado == EstadoCita.Solicitada && c.FechaHoraInicioUtc > ahora, ct))
                throw new AgendaOperacionException("Ya enviaste una propuesta pendiente para este estudiante. Espera la respuesta del psicólogo o consulta Mis propuestas.");
            // 3) Reglas del estudiante: un único mensaje genérico; no distingue cuenta inactiva, mayoría de edad
            //    ni choque exacto de hora, para no revelar si recibe atención.
            if (!await EstudiantePropuestaElegible(perfil, inicio, ct)) throw new AgendaOperacionException(MensajeEstudianteNoDisponible);
            var cita = new Cita
            {
                Id = input.OperacionId, PerfilEstudianteId = perfil, PsicologoUsuarioId = psicologo, FechaCreacionUtc = Ahora,
                FechaHoraInicioUtc = inicio, FechaHoraFinUtc = fin, FinAproximado = true, Modalidad = input.Modalidad,
                ProgramadaDirectamente = true, ProgramadorUsuarioId = director, ProgramadorRol = "Director", DisponibilidadPsicologoId = null,
                // Una propuesta nunca nace confirmada: requiere la aceptación del psicólogo.
                Estado = EstadoCita.Solicitada, Revision = Guid.NewGuid()
            };
            db.Citas.Add(cita);
            await NotificarNuevaPropuesta(cita, director, ct);
            resultado = cita.Id;
        }, ct);
        return resultado;
    }

    private async Task ValidarFranjaProfesional(string psicologo, DateTime inicio, DateTime fin, CancellationToken ct)
    {
        var fecha = AgendaReglas.FechaPermitida(AgendaReglas.Hoy(inicio), Ahora);
        if (inicio <= Ahora || fin <= inicio || AgendaReglas.Hoy(fin) != fecha ||
            TimeOnly.FromDateTime(AgendaReglas.Local(inicio)) < new TimeOnly(8, 0) || TimeOnly.FromDateTime(AgendaReglas.Local(fin)) > new TimeOnly(18, 0))
            throw new AgendaOperacionException("Usa una franja futura entre las 08:00 y 18:00 del mismo día.");
        if (await db.ConfiguracionesAgendaDia.AnyAsync(d => d.PsicologoUsuarioId == psicologo && d.Fecha == fecha && d.Bloqueado, ct))
            throw new AgendaOperacionException("El psicólogo tiene ese día bloqueado.");
        await ValidarAlmuerzo(psicologo, inicio, fin, ct);
        if (await db.Citas.AnyAsync(c => c.PsicologoUsuarioId == psicologo && EstadosActivos.Contains(c.Estado) &&
                c.FechaHoraInicioUtc < fin && c.FechaHoraFinUtc > inicio, ct))
            throw new AgendaOperacionException("El psicólogo ya tiene ocupado ese horario. Revisa la carga de psicólogos y elige otra franja.");
    }

    // Cuenta activa en el directorio y mayoría de edad verificada (regla vigente de ZUNI). De la agenda del
    // estudiante solo se evalúa el choque exacto de hora: el índice único (estudiante, inicio) de citas
    // activas impide guardarla y no puede cambiarse sin migración. Una cita el mismo día o que se cruza ya
    // no impide proponer: se valida al aceptar (ValidarConfirmacionProgramada), donde solo lo ve el
    // psicólogo asignado. El resultado es solo verdadero/falso.
    private async Task<bool> EstudiantePropuestaElegible(Guid perfil, DateTime inicio, CancellationToken ct)
    {
        if (!await PerfilesDirectorio().AnyAsync(p => p.Id == perfil, ct)) return false;
        if (!await MayoriaEdadService.VerificadaAsync(db, perfil, ct)) return false;
        return !await db.Citas.AnyAsync(c => c.PerfilEstudianteId == perfil && EstadosActivos.Contains(c.Estado) && c.FechaHoraInicioUtc == inicio, ct);
    }

    private static string Cuando(DateTime utc) => $"{AgendaReglas.Local(utc):dd/MM/yyyy} a las {AgendaReglas.Local(utc):HH:mm} (Guatemala)";

    private async Task NotificarNuevaPropuesta(Cita c, string director, CancellationToken ct)
    {
        var estudiante = await db.PerfilesEstudiante.Where(p => p.Id == c.PerfilEstudianteId)
            .Select(p => new { p.UsuarioId, Nombre = p.Usuario.FullName, p.Carne }).SingleAsync(ct);
        var psicologo = await db.Users.Where(u => u.Id == c.PsicologoUsuarioId).Select(u => u.FullName).SingleAsync(ct);
        var modalidad = c.Modalidad == ModalidadCita.Virtual ? "Virtual" : "Presencial";
        var minutos = (int)(c.FechaHoraFinUtc - c.FechaHoraInicioUtc).TotalMinutes;
        db.NotificacionesCita.Add(new()
        {
            UsuarioId = c.PsicologoUsuarioId, CitaId = c.Id, FechaUtc = Ahora, AutorUsuarioId = director, AutorRol = "Director", Tipo = "Propuesta",
            Texto = $"Nueva propuesta de Dirección.\nSe ha propuesto una cita para el estudiante {estudiante.Nombre} (carné {estudiante.Carne}) el {Cuando(c.FechaHoraInicioUtc)}.\n" +
                    $"Modalidad: {modalidad}. Duración: {minutos} minutos.\nAcciones: Aceptar, Reprogramar o Rechazar desde tu agenda.\n" +
                    "Para aceptarla, el estudiante debe tener una solicitud de atención asignada a ti. Si aún no la tiene, el estudiante la registra en ZUNI " +
                    "y Administración la asigna en Solicitudes de atención; mientras tanto puedes rechazarla con un motivo de la lista."
        });
        var textoEstudiante = $"Dirección propuso una cita de orientación psicológica para el {Cuando(c.FechaHoraInicioUtc)} con {psicologo}.\nModalidad: {modalidad}.\n" +
                              "Estado: pendiente de revisión del psicólogo. Recibirás un aviso cuando la acepte, la reprograme o la rechace.\n" +
                              "Para que el psicólogo pueda confirmarla necesitas una solicitud de atención vigente en ZUNI. Si aún no la tienes, " +
                              "regístrala desde Solicitar atención (puedes indicar que fue por referencia de Dirección).";
        if (c.Modalidad == ModalidadCita.Virtual)
            textoEstudiante += "\nAntes de la cita virtual: procura estar a solas, en un lugar privado y sin ruido. Consulta los consejos y el acceso individual desde tu cita en ZUNI.";
        db.NotificacionesCita.Add(new()
        {
            UsuarioId = estudiante.UsuarioId, CitaId = c.Id, FechaUtc = Ahora, AutorUsuarioId = director, AutorRol = "Director", Tipo = "Propuesta",
            Texto = textoEstudiante
        });
        // Registro propio del Director (leído): conserva la fecha original para la trazabilidad.
        db.NotificacionesCita.Add(new()
        {
            UsuarioId = director, CitaId = c.Id, FechaUtc = Ahora, AutorUsuarioId = director, AutorRol = "Director", Tipo = AvisosDirector.Enviada, Leida = true,
            Texto = $"Propuesta enviada a {psicologo} para el {Cuando(c.FechaHoraInicioUtc)}. Modalidad: {modalidad}. Estado: pendiente de revisión."
        });
        PrivacidadService.Auditar(db, estudiante.UsuarioId, director, "PROPUESTA_DIRECTOR_CREADA", reloj, c.Id);
    }

    // Respuesta a una propuesta de Dirección (aceptar, rechazar o cancelar). Al estudiante se le informa
    // como antes; a Dirección solo se le envía un aviso administrativo sin texto libre.
    private async Task NotificarRespuestaDirector(Cita c, CancellationToken ct, string? actorId = null, string actorRol = "Psicologo")
    {
        if (c.ProgramadorRol != "Director" || c.ProgramadorUsuarioId is null ||
            c.Estado is not (EstadoCita.Confirmada or EstadoCita.Rechazada or EstadoCita.Cancelada)) return;
        var actor = actorId ?? c.PsicologoUsuarioId;
        var nombre = await db.Users.Where(u => u.Id == c.PsicologoUsuarioId).Select(u => u.FullName).SingleAsync(ct);
        var estudiante = await db.PerfilesEstudiante.Where(p => p.Id == c.PerfilEstudianteId).Select(p => p.UsuarioId).SingleAsync(ct);
        if (c.Estado is EstadoCita.Confirmada or EstadoCita.Rechazada)
        {
            var texto = $"El psicólogo {nombre} {(c.Estado == EstadoCita.Confirmada ? "aceptó" : "rechazó")} la cita propuesta para {AgendaReglas.Local(c.FechaHoraInicioUtc):dd/MM/yyyy HH:mm} (Guatemala).";
            if (c.Estado == EstadoCita.Rechazada) texto += " Motivo administrativo: " + c.MotivoCancelacion;
            if (c.Estado == EstadoCita.Rechazada && c.MotivoCancelacion?.Trim() == MotivosPropuestaDirector.Requisitos)
                texto += " Si aún no has registrado tu solicitud de atención, hazlo desde Solicitar atención; cuando Administración la asigne podrá coordinarse una nueva cita.";
            db.NotificacionesCita.Add(new() { UsuarioId = estudiante, CitaId = c.Id, Texto = texto, FechaUtc = Ahora, AutorUsuarioId = c.PsicologoUsuarioId, AutorRol = "Psicologo", Tipo = "Respuesta" });
        }
        var (tipo, accion, textoDireccion) = c.Estado switch
        {
            EstadoCita.Confirmada => (AvisosDirector.Aceptada, "PROPUESTA_DIRECTOR_ACEPTADA",
                $"Propuesta aceptada por {nombre}. Cita confirmada para el {Cuando(c.FechaHoraInicioUtc)}."),
            EstadoCita.Rechazada => (AvisosDirector.Rechazada, "PROPUESTA_DIRECTOR_RECHAZADA",
                $"Propuesta rechazada por {nombre}. Motivo administrativo: {MotivosPropuestaDirector.ParaDireccion(c.MotivoCancelacion) ?? "no especificado"}."),
            _ => (AvisosDirector.Cancelada, "PROPUESTA_DIRECTOR_CANCELADA",
                $"La cita propuesta para el {Cuando(c.FechaHoraInicioUtc)} fue cancelada.")
        };
        db.NotificacionesCita.Add(new() { UsuarioId = c.ProgramadorUsuarioId, CitaId = c.Id, Texto = textoDireccion, FechaUtc = Ahora, AutorUsuarioId = actor, AutorRol = actorRol, Tipo = tipo });
        PrivacidadService.Auditar(db, estudiante, actor, accion, reloj, c.Id);
    }

    private async Task NotificarReprogramacionDirector(Cita c, DateTime inicioAnterior, string psicologo, CancellationToken ct)
    {
        if (c.ProgramadorRol != "Director" || c.ProgramadorUsuarioId is null) return;
        var nombre = await db.Users.Where(u => u.Id == psicologo).Select(u => u.FullName).SingleAsync(ct);
        var estudiante = await db.PerfilesEstudiante.Where(p => p.Id == c.PerfilEstudianteId).Select(p => p.UsuarioId).SingleAsync(ct);
        db.NotificacionesCita.Add(new()
        {
            UsuarioId = c.ProgramadorUsuarioId, CitaId = c.Id, FechaUtc = Ahora, AutorUsuarioId = psicologo, AutorRol = "Psicologo", Tipo = AvisosDirector.Reprogramada,
            Texto = $"Propuesta reprogramada por {nombre}. Fecha anterior: {Cuando(inicioAnterior)}. Nueva fecha: {Cuando(c.FechaHoraInicioUtc)}. " +
                    $"Estado: {(c.Estado == EstadoCita.Confirmada ? "confirmada" : "pendiente de confirmación del psicólogo")}."
        });
        PrivacidadService.Auditar(db, estudiante, psicologo, "PROPUESTA_DIRECTOR_REPROGRAMADA", reloj, c.Id);
    }

    // Estado visible para Dirección. Completada y No asistió se muestran igual: la asistencia es un
    // registro privado de atención. Una propuesta pendiente cuya fecha ya pasó se muestra como vencida
    // (estado derivado, sin persistir): el psicólogo aún puede rechazarla y Dirección puede proponer otra.
    public static string EstadoAdministrativo(EstadoCita estado, bool reprogramada, bool vencida = false) => estado switch
    {
        EstadoCita.Solicitada when vencida => "Sin respuesta · fecha vencida",
        EstadoCita.Solicitada => reprogramada ? "Reprogramada · pendiente de confirmación" : "Pendiente de revisión",
        EstadoCita.Confirmada or EstadoCita.Reservada => reprogramada ? "Reprogramada · confirmada" : "Aceptada · confirmada",
        EstadoCita.Rechazada => "Rechazada",
        EstadoCita.Cancelada => "Cancelada",
        _ => "Fecha concluida"
    };

    public async Task<PropuestasDirectorViewModel> MisPropuestas(ClaimsPrincipal actor, int pagina, CancellationToken ct)
    {
        var director = await DirectorActivo(actor, ct);
        var tipos = AvisosDirector.Todos;
        var ahora = Ahora;
        var query = from c in db.Citas.AsNoTracking()
                    join p in db.PerfilesEstudiante.AsNoTracking() on c.PerfilEstudianteId equals p.Id
                    join u in db.Users.AsNoTracking() on c.PsicologoUsuarioId equals u.Id
                    where c.ProgramadorRol == "Director" && c.ProgramadorUsuarioId == director
                    select new
                    {
                        c.Id, Estudiante = p.Usuario.FullName, p.Carne, Psicologo = u.FullName, c.FechaHoraInicioUtc, c.FechaHoraFinUtc, c.Estado, c.FechaCreacionUtc,
                        Reprogramada = db.NotificacionesCita.Any(n => n.CitaId == c.Id && n.UsuarioId == director && n.Tipo == AvisosDirector.Reprogramada),
                        Novedad = db.NotificacionesCita.Any(n => n.CitaId == c.Id && n.UsuarioId == director && !n.Leida && tipos.Contains(n.Tipo!))
                    };
        var paginas = Math.Max(1, (await query.CountAsync(ct) + TamanoPagina - 1) / TamanoPagina);
        pagina = Math.Clamp(pagina, 1, paginas);
        var filas = await query.OrderByDescending(x => x.FechaCreacionUtc).ThenBy(x => x.Id)
            .Skip((pagina - 1) * TamanoPagina).Take(TamanoPagina).ToListAsync(ct);
        return new PropuestasDirectorViewModel
        {
            Pagina = pagina, Paginas = paginas,
            Propuestas = filas.Select(x => new PropuestaDirectorResumen(x.Id, x.Estudiante, x.Carne, x.Psicologo, x.FechaHoraInicioUtc, x.FechaHoraFinUtc,
                EstadoAdministrativo(x.Estado, x.Reprogramada, x.FechaHoraInicioUtc <= ahora), x.Novedad)).ToList()
        };
    }

    public async Task<PropuestaDirectorDetalle> DetallePropuesta(ClaimsPrincipal actor, Guid id, CancellationToken ct)
    {
        var director = await DirectorActivo(actor, ct);
        var c = await (from x in db.Citas.AsNoTracking()
                       join p in db.PerfilesEstudiante.AsNoTracking() on x.PerfilEstudianteId equals p.Id
                       join u in db.Users.AsNoTracking() on x.PsicologoUsuarioId equals u.Id
                       where x.Id == id && x.ProgramadorRol == "Director" && x.ProgramadorUsuarioId == director
                       select new { x.Id, Estudiante = p.Usuario.FullName, p.Carne, p.Carrera, Psicologo = u.FullName, x.FechaHoraInicioUtc, x.FechaHoraFinUtc, x.Modalidad, x.Estado, x.MotivoCancelacion })
                .SingleOrDefaultAsync(ct) ?? throw new AgendaAccesoException();
        var tipos = AvisosDirector.Todos;
        var avisos = await db.NotificacionesCita.Where(n => n.CitaId == id && n.UsuarioId == director && tipos.Contains(n.Tipo!))
            .OrderBy(n => n.FechaUtc).ToListAsync(ct);
        if (avisos.Any(n => !n.Leida))
        {
            foreach (var n in avisos) n.Leida = true;
            await db.SaveChangesAsync(ct);
        }
        return new PropuestaDirectorDetalle
        {
            Id = c.Id, Estudiante = c.Estudiante, Carne = c.Carne, Carrera = c.Carrera, Psicologo = c.Psicologo,
            InicioUtc = c.FechaHoraInicioUtc, FinUtc = c.FechaHoraFinUtc, Modalidad = c.Modalidad,
            EstadoAdministrativo = EstadoAdministrativo(c.Estado, avisos.Any(n => n.Tipo == AvisosDirector.Reprogramada), c.FechaHoraInicioUtc <= Ahora),
            FechaDefinitiva = c.Estado is EstadoCita.Confirmada or EstadoCita.Reservada,
            MotivoAdministrativo = c.Estado == EstadoCita.Rechazada ? MotivosPropuestaDirector.ParaDireccion(c.MotivoCancelacion) : null,
            Historial = avisos.Select(n => new AvisoPropuestaDirector(n.FechaUtc, n.Tipo!, n.Texto)).ToList()
        };
    }

    // Avisos pendientes de Dirección: solo de sus propias propuestas.
    public static Task<int> AvisosPendientesDirector(Zuni.Data.ApplicationDbContext db, string directorId, CancellationToken ct)
    {
        var tipos = AvisosDirector.Todos;
        return db.NotificacionesCita.AsNoTracking().CountAsync(n => n.UsuarioId == directorId && !n.Leida && tipos.Contains(n.Tipo!) &&
            db.Citas.Any(c => c.Id == n.CitaId && c.ProgramadorRol == "Director" && c.ProgramadorUsuarioId == directorId), ct);
    }
}
