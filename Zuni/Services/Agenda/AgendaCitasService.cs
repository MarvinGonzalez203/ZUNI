using System.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Zuni.Data;
using Zuni.Models;
using Zuni.Models.Agenda;

namespace Zuni.Services.Agenda;

public sealed class AgendaCitasService(ApplicationDbContext db,
    IAutorizacionClinicaService autorizacion, TimeProvider reloj)
{
    private DateTime Ahora => reloj.GetUtcNow().UtcDateTime;
    private const int TamanoPagina = 20;

    private async Task<string> Psicologo(ClaimsPrincipal sesion, CancellationToken ct) =>
        await autorizacion.ObtenerPsicologoAutorizadoAsync(sesion, ct) ?? throw new AgendaAccesoException();

    private async Task<PerfilEstudiante> Estudiante(ClaimsPrincipal sesion, CancellationToken ct)
    {
        var id = sesion.FindFirstValue(ClaimTypes.NameIdentifier);
        var stamp = sesion.FindFirstValue("Zuni.SecurityStamp");
        if (sesion.Identity?.IsAuthenticated != true || !sesion.IsInRole("Estudiante") ||
            string.IsNullOrEmpty(id) || string.IsNullOrEmpty(stamp)) throw new AgendaAccesoException();
        var roles = await (from ur in db.UserRoles.AsNoTracking() join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id
                           where ur.UserId == id select r.NormalizedName).ToListAsync(ct);
        if (!roles.Contains("ESTUDIANTE") || roles.Any(r => r is "ADMINISTRADOR" or "DIRECTOR" or "CATEDRATICO"))
            throw new AgendaAccesoException();
        return await db.PerfilesEstudiante.AsNoTracking().SingleOrDefaultAsync(p => p.UsuarioId == id &&
            p.Activo && p.Usuario.IsActive && !p.Usuario.DebeCambiarContrasena && p.Usuario.SecurityStamp == stamp, ct)
            ?? throw new AgendaAccesoException();
    }

    private async Task<string?> Asignado(Guid perfilId, CancellationToken ct)
    {
        var id = await db.AsignacionesEstudiantePsicologo.AsNoTracking()
            .Where(a => a.PerfilEstudianteId == perfilId && a.FechaFinalizacionUtc == null)
            .Select(a => a.PsicologoUsuarioId).SingleOrDefaultAsync(ct);
        if (id is null || !await db.Users.AnyAsync(u => u.Id == id && u.IsActive && !u.DebeCambiarContrasena, ct)) return null;
        var roles = await (from ur in db.UserRoles.AsNoTracking() join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id
                           where ur.UserId == id select r.NormalizedName).ToListAsync(ct);
        return roles.Contains("PSICOLOGO") && !roles.Any(r => r is "ADMINISTRADOR" or "DIRECTOR" or "CATEDRATICO") ? id : null;
    }

    // Todas las escrituras validan identidad y reglas dentro de SERIALIZABLE.
    // Los predicados de solapamiento incluyen al estudiante y al psicólogo; PostgreSQL
    // aborta una de dos reservas concurrentes incompatibles (40001). No se reintenta a ciegas.
    private async Task Escribir(Func<Task> operacion, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            await operacion();
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex) when (Conflicto(ex))
        {
            await tx.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear();
            throw new AgendaOperacionException("El horario o la cita cambió mientras confirmabas. Actualiza la página y revisa antes de volver a intentarlo.");
        }
    }

    private static bool Conflicto(Exception ex)
    {
        var pg = ex as PostgresException ?? ex.InnerException as PostgresException;
        return pg?.SqlState is PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes.UniqueViolation;
    }

    public Task AgregarDisponibilidad(ClaimsPrincipal sesion, DisponibilidadInput input, CancellationToken ct) => Escribir(async () =>
    {
        var id = await Psicologo(sesion, ct);
        AgendaReglas.ValidarDisponibilidad(input);
        var inicio = input.HoraInicio!.Value;
        var fin = input.HoraFin!.Value;
        if (await db.DisponibilidadesPsicologo.AnyAsync(d => d.PsicologoUsuarioId == id && d.Activo &&
            d.DiaSemana == input.DiaSemana && d.HoraInicio < fin && inicio < d.HoraFin, ct))
            throw new AgendaOperacionException("El horario se superpone con otra disponibilidad activa.");
        db.DisponibilidadesPsicologo.Add(new DisponibilidadPsicologo
        {
            PsicologoUsuarioId = id, DiaSemana = input.DiaSemana, HoraInicio = inicio, HoraFin = fin,
            DuracionMinutos = input.DuracionMinutos, Modalidad = input.Modalidad
        });
    }, ct);

    public Task CambiarDisponibilidad(ClaimsPrincipal sesion, Guid disponibilidadId, bool activo, CancellationToken ct) => Escribir(async () =>
    {
        var id = await Psicologo(sesion, ct);
        var d = await db.DisponibilidadesPsicologo.SingleOrDefaultAsync(x => x.Id == disponibilidadId && x.PsicologoUsuarioId == id, ct)
            ?? throw new AgendaAccesoException();
        if (d.Activo == activo) return;
        var ahora = Ahora;
        if (!activo && await db.Citas.AnyAsync(c => c.DisponibilidadPsicologoId == d.Id &&
            c.Estado == EstadoCita.Reservada && c.FechaHoraFinUtc > ahora, ct))
            throw new AgendaOperacionException("Este horario tiene citas pendientes. Gestiona esas citas antes de desactivarlo.");
        if (activo && await db.DisponibilidadesPsicologo.AnyAsync(x => x.Id != d.Id && x.PsicologoUsuarioId == id &&
            x.Activo && x.DiaSemana == d.DiaSemana && x.HoraInicio < d.HoraFin && d.HoraInicio < x.HoraFin, ct))
            throw new AgendaOperacionException("El horario se superpone con otra disponibilidad activa.");
        d.Activo = activo;
    }, ct);

    public async Task<AgendaPsicologoViewModel> LeerAgenda(ClaimsPrincipal sesion, DateOnly? fecha, CancellationToken ct)
    {
        var id = await Psicologo(sesion, ct);
        var dia = fecha ?? AgendaReglas.Hoy(Ahora);
        if (dia < AgendaReglas.Hoy(Ahora).AddDays(-365) || dia > AgendaReglas.Hoy(Ahora).AddDays(90))
            throw new AgendaOperacionException("Consulta la agenda entre el último año y los próximos 90 días; las citas anteriores están en Historial.");
        var inicio = AgendaReglas.Utc(dia, TimeOnly.MinValue);
        var fin = AgendaReglas.Utc(dia.AddDays(1), TimeOnly.MinValue);
        return new AgendaPsicologoViewModel
        {
            Fecha = dia,
            Disponibilidades = await db.DisponibilidadesPsicologo.AsNoTracking().Where(d => d.PsicologoUsuarioId == id)
                .OrderBy(d => d.DiaSemana).ThenBy(d => d.HoraInicio)
                .Select(d => new DisponibilidadResumen(d.Id, d.DiaSemana, d.HoraInicio, d.HoraFin, d.DuracionMinutos, d.Modalidad, d.Activo)).ToListAsync(ct),
            Citas = await ResumenPsicologo(id).Where(c => c.InicioUtc >= inicio && c.InicioUtc < fin)
                .OrderBy(c => c.InicioUtc).ToListAsync(ct)
        };
    }

    private IQueryable<CitaResumen> ResumenPsicologo(string id) =>
        from c in db.Citas.AsNoTracking()
        join p in db.PerfilesEstudiante.AsNoTracking() on c.PerfilEstudianteId equals p.Id
        join u in db.Users.AsNoTracking() on p.UsuarioId equals u.Id
        where c.PsicologoUsuarioId == id
        select new CitaResumen { Id = c.Id, Nombre = u.FullName, InicioUtc = c.FechaHoraInicioUtc,
            FinUtc = c.FechaHoraFinUtc, Modalidad = c.Modalidad, Estado = c.Estado };

    public async Task<CitasEstudianteViewModel> LeerCitas(ClaimsPrincipal sesion, DateOnly? fecha, int pagina, CancellationToken ct)
    {
        var perfil = await Estudiante(sesion, ct);
        var ahora = Ahora;
        var dia = AgendaReglas.FechaPermitida(fecha, ahora);
        var psicologo = await Asignado(perfil.Id, ct);
        var horarios = new List<HorarioDisponible>();
        if (psicologo is not null && perfil.EstaCompleto)
        {
            var inicioDia = AgendaReglas.Utc(dia, TimeOnly.MinValue);
            var finDia = AgendaReglas.Utc(dia.AddDays(1), TimeOnly.MinValue);
            var ocupados = await db.Citas.AsNoTracking().Where(c => c.Estado == EstadoCita.Reservada &&
                (c.PsicologoUsuarioId == psicologo || c.PerfilEstudianteId == perfil.Id) &&
                c.FechaHoraInicioUtc < finDia && c.FechaHoraFinUtc > inicioDia)
                .Select(c => new { c.FechaHoraInicioUtc, c.FechaHoraFinUtc }).ToListAsync(ct);
            var disponibilidad = await db.DisponibilidadesPsicologo.AsNoTracking().Where(d =>
                d.PsicologoUsuarioId == psicologo && d.Activo && d.DiaSemana == dia.DayOfWeek).OrderBy(d => d.HoraInicio).ToListAsync(ct);
            foreach (var d in disponibilidad)
                for (var minuto = 0; minuto + d.DuracionMinutos <= (d.HoraFin - d.HoraInicio).TotalMinutes; minuto += d.DuracionMinutos)
                {
                    var desde = AgendaReglas.Utc(dia, d.HoraInicio.AddMinutes(minuto));
                    var hasta = desde.AddMinutes(d.DuracionMinutos);
                    if (desde > ahora && !ocupados.Any(c => AgendaReglas.Cruza(desde, hasta, c.FechaHoraInicioUtc, c.FechaHoraFinUtc)))
                        horarios.Add(new(desde, hasta, d.Modalidad));
                }
        }
        var propias = from c in db.Citas.AsNoTracking()
                      join u in db.Users.AsNoTracking() on c.PsicologoUsuarioId equals u.Id
                      where c.PerfilEstudianteId == perfil.Id
                      select new CitaResumen { Id = c.Id, Nombre = u.FullName, InicioUtc = c.FechaHoraInicioUtc,
                          FinUtc = c.FechaHoraFinUtc, Modalidad = c.Modalidad, Estado = c.Estado };
        var paginas = Math.Max(1, (await propias.CountAsync(ct) + TamanoPagina - 1) / TamanoPagina);
        pagina = Math.Clamp(pagina, 1, paginas);
        return new CitasEstudianteViewModel
        {
            Fecha = dia, PuedeReservar = psicologo is not null && perfil.EstaCompleto,
            Psicologo = psicologo is null ? null : await db.Users.Where(u => u.Id == psicologo).Select(u => u.FullName).SingleAsync(ct),
            Horarios = horarios, Pagina = pagina, Paginas = paginas,
            Citas = await propias.OrderBy(c => c.Estado != EstadoCita.Reservada).ThenByDescending(c => c.InicioUtc)
                .Skip((pagina - 1) * TamanoPagina).Take(TamanoPagina).ToListAsync(ct)
        };
    }

    public Task Reservar(ClaimsPrincipal sesion, ReservaCitaInput input, CancellationToken ct) => Escribir(async () =>
    {
        var perfil = await Estudiante(sesion, ct);
        if (!perfil.EstaCompleto) throw new AgendaOperacionException("Completa tus datos en Mi cuenta antes de reservar.");
        var psicologo = await Asignado(perfil.Id, ct)
            ?? throw new AgendaOperacionException("Necesitas una asignación vigente a un psicólogo disponible para reservar.");
        if (input.Inicio is null || !Enum.IsDefined(input.Modalidad)) throw new AgendaOperacionException("Selecciona un horario y una modalidad válidos.");
        var inicio = input.Inicio.Value.UtcDateTime;
        var ahora = Ahora;
        if (inicio <= ahora) throw new AgendaOperacionException("El horario debe ser futuro.");
        var dia = AgendaReglas.FechaPermitida(DateOnly.FromDateTime(AgendaReglas.Local(inicio)), ahora);
        var disponibles = await db.DisponibilidadesPsicologo.AsNoTracking().Where(d => d.PsicologoUsuarioId == psicologo &&
            d.Activo && d.DiaSemana == dia.DayOfWeek).ToListAsync(ct);
        var horario = disponibles.FirstOrDefault(d => AgendaReglas.Encaja(d, inicio) && AgendaReglas.Permite(d.Modalidad, input.Modalidad))
            ?? throw new AgendaOperacionException("El horario o la modalidad ya no están disponibles.");
        var fin = inicio.AddMinutes(horario.DuracionMinutos);
        if (await db.Citas.AnyAsync(c => c.Estado == EstadoCita.Reservada &&
            (c.PsicologoUsuarioId == psicologo || c.PerfilEstudianteId == perfil.Id) &&
            c.FechaHoraInicioUtc < fin && c.FechaHoraFinUtc > inicio, ct))
            throw new AgendaOperacionException("Ya existe una cita que se superpone con ese horario.");
        db.Citas.Add(new Cita
        {
            PerfilEstudianteId = perfil.Id, PsicologoUsuarioId = psicologo, DisponibilidadPsicologoId = horario.Id,
            FechaHoraInicioUtc = inicio, FechaHoraFinUtc = fin, Modalidad = input.Modalidad, FechaCreacionUtc = ahora
        });
    }, ct);

    public Task GestionarCita(ClaimsPrincipal sesion, CambiarCitaInput input, CancellationToken ct) => Escribir(async () =>
    {
        var id = await Psicologo(sesion, ct);
        var cita = await db.Citas.SingleOrDefaultAsync(c => c.Id == input.CitaId && c.PsicologoUsuarioId == id, ct)
            ?? throw new AgendaAccesoException();
        AplicarCambio(cita, input.Estado, input.MotivoCancelacion);
    }, ct);

    public Task CancelarEstudiante(ClaimsPrincipal sesion, Guid citaId, CancellationToken ct) => Escribir(async () =>
    {
        var perfil = await Estudiante(sesion, ct);
        var cita = await db.Citas.SingleOrDefaultAsync(c => c.Id == citaId && c.PerfilEstudianteId == perfil.Id, ct)
            ?? throw new AgendaAccesoException();
        // Puede cancelar una cita propia futura aunque posteriormente cambie su asignación.
        AplicarCambio(cita, EstadoCita.Cancelada, "Cancelada por el estudiante.");
    }, ct);

    private void AplicarCambio(Cita cita, EstadoCita estado, string? motivo)
    {
        AgendaReglas.ValidarCambio(cita, estado, Ahora);
        if (motivo?.Length > 300) throw new AgendaOperacionException("El motivo admite hasta 300 caracteres.");
        cita.Estado = estado;
        cita.MotivoCancelacion = estado == EstadoCita.Cancelada ? motivo?.Trim() : null;
    }

    public async Task<HistorialCitasViewModel> Historial(ClaimsPrincipal sesion, string? busqueda, EstadoCita? estado, int pagina, CancellationToken ct)
    {
        var id = await Psicologo(sesion, ct);
        if (estado.HasValue && estado is not (EstadoCita.Cancelada or EstadoCita.Completada or EstadoCita.NoAsistio))
            throw new AgendaOperacionException("Selecciona un estado de historial válido.");
        var texto = (busqueda ?? string.Empty).Trim();
        if (texto.Length > 80) throw new AgendaOperacionException("La búsqueda admite hasta 80 caracteres.");
        var query = ResumenPsicologo(id).Where(c => c.Estado != EstadoCita.Reservada);
        if (estado.HasValue) query = query.Where(c => c.Estado == estado.Value);
        if (texto.Length > 0) query = query.Where(c => c.Nombre.ToLower().Contains(texto.ToLower()));
        var paginas = Math.Max(1, (await query.CountAsync(ct) + TamanoPagina - 1) / TamanoPagina);
        pagina = Math.Clamp(pagina, 1, paginas);
        return new HistorialCitasViewModel
        {
            Busqueda = texto, Estado = estado, Pagina = pagina, Paginas = paginas,
            Citas = await query.OrderByDescending(c => c.InicioUtc).ThenBy(c => c.Id)
                .Skip((pagina - 1) * TamanoPagina).Take(TamanoPagina).ToListAsync(ct)
        };
    }
}
