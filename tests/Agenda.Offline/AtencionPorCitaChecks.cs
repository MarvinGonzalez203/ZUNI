using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zuni.Models;
using Zuni.Models.Agenda;
using Zuni.Models.Atencion;
using Zuni.Services.Agenda;

// Registro de atención por cita: cada cita se habilita al terminar SU horario (inicio + duración),
// nunca al terminar la disponibilidad del psicólogo (DisponibilidadPsicologo / ConfiguracionAgendaDia).
// Escenario: disponibilidad 08:00–18:00 "sin duración establecida" con cuatro citas independientes.
internal static class AtencionPorCitaChecks
{
    public static async Task<int> Run(OfflineDb db, AgendaCitasService service, Reloj reloj, CancellationToken ct)
    {
        var checks = 0;
        void Check(bool ok, string label) { if (!ok) throw new Exception("Atención por cita: " + label); checks++; }
        async Task Rechaza<T>(Func<Task> action, string label) where T : Exception
        { try { await action(); } catch (T) { checks++; return; } throw new Exception("Atención por cita: " + label); }

        ClaimsPrincipal Sesion(string id, string rol) => new(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, id), new Claim(ClaimTypes.Role, rol), new Claim("Zuni.SecurityStamp", id + "-stamp")
        }, "Offline"));

        // ---------------------------------------------------------------- datos propios del escenario
        foreach (var (id, rol) in new[] { ("psi-dia", "Psicologo"), ("psi-ajeno-dia", "Psicologo"),
            ("dia-a", "Estudiante"), ("dia-b", "Estudiante"), ("dia-c", "Estudiante"), ("dia-d", "Estudiante") })
        {
            db.Users.Add(new ApplicationUser { Id = id, UserName = id, NormalizedUserName = id.ToUpperInvariant(),
                FullName = id, IsActive = true, SecurityStamp = id + "-stamp" });
            db.UserRoles.Add(new IdentityUserRole<string> { UserId = id, RoleId = rol });
        }
        var perfiles = new Dictionary<string, Guid>();
        var carne = 101;
        foreach (var id in new[] { "dia-a", "dia-b", "dia-c", "dia-d" })
        {
            var perfil = new PerfilEstudiante { UsuarioId = id, Carne = "0000000" + carne++, Carrera = "Offline", Telefono = "12345678" };
            var solicitud = new SolicitudAtencion { PerfilEstudianteId = perfil.Id, FechaSolicitudUtc = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
            db.PerfilesEstudiante.Add(perfil);
            db.VerificacionesMayoriaEdad.Add(new Zuni.Models.MayoriaEdad.VerificacionMayoriaEdad { PerfilEstudianteId = perfil.Id, EdadDeclarada = 21, Estado = Zuni.Models.MayoriaEdad.EstadoMayoriaEdad.Verificada, EdadVerificada = 21, DocumentoRevisado = Zuni.Models.MayoriaEdad.DocumentoEdad.Dpi, ResponsableUsuarioId = perfil.UsuarioId, FechaDeclaracionUtc = DateTime.UtcNow, FechaRevisionUtc = DateTime.UtcNow });
            db.SolicitudesAtencion.Add(solicitud);
            db.ConsentimientosAtencion.Add(new ConsentimientoAtencion { SolicitudAtencionId = solicitud.Id, Aceptado = true, VersionConsentimiento = "offline" });
            db.AsignacionesEstudiantePsicologo.Add(new AsignacionEstudiantePsicologo { PerfilEstudianteId = perfil.Id, PsicologoUsuarioId = "psi-dia" });
            perfiles[id] = perfil.Id;
        }
        await db.SaveChangesAsync(ct); db.ChangeTracker.Clear();

        var psicologo = Sesion("psi-dia", "Psicologo");
        var ajeno = Sesion("psi-ajeno-dia", "Psicologo");
        reloj.Ahora = new DateTimeOffset(2030, 1, 7, 12, 0, 0, TimeSpan.Zero);
        var dia = new DateOnly(2030, 1, 22); // martes, futuro respecto del reloj
        DateTimeOffset A(int hora, int minuto) => new(AgendaReglas.Utc(dia, new(hora, minuto)), TimeSpan.Zero);

        // CASO 1 (preparación): disponibilidad 08:00–18:00 sin duración fija.
        await service.AgregarHorarioDia(psicologo, new HorarioDiaInput
        {
            Fecha = dia, HoraInicio = new(8, 0), HoraFin = new(18, 0), DuracionMinutos = 0, Modalidad = ModalidadDisponibilidad.Ambas
        }, ct);
        db.ChangeTracker.Clear();
        var disponibilidad = await db.DisponibilidadesPsicologo.AsNoTracking()
            .SingleAsync(d => d.PsicologoUsuarioId == "psi-dia" && d.ConfiguracionAgendaDiaId != null, ct);
        Check(disponibilidad.HoraInicio == new TimeOnly(8, 0) && disponibilidad.HoraFin == new TimeOnly(18, 0) && disponibilidad.DuracionMinutos == 0,
            "Disponibilidad 08:00–18:00");

        async Task<Guid> Reservar(string estudiante, int hora, int minuto, int duracion)
        {
            await service.Reservar(Sesion(estudiante, "Estudiante"), new ReservaCitaInput
            { Inicio = A(hora, minuto), Modalidad = ModalidadCita.Presencial, DuracionMinutos = duracion }, ct);
            db.ChangeTracker.Clear();
            var cita = await db.Citas.AsNoTracking().SingleAsync(c => c.PerfilEstudianteId == perfiles[estudiante] && c.Estado == EstadoCita.Solicitada, ct);
            await service.GestionarCita(psicologo, new CambiarCitaInput { CitaId = cita.Id, Estado = EstadoCita.Confirmada }, ct);
            db.ChangeTracker.Clear();
            return cita.Id;
        }
        var citaA = await Reservar("dia-a", 8, 0, 120);   // 08:00–10:00
        var citaB = await Reservar("dia-b", 10, 30, 90);  // 10:30–12:00
        var citaC = await Reservar("dia-c", 13, 0, 60);   // 13:00–14:00
        async Task<Cita> Leer(Guid id) { db.ChangeTracker.Clear(); return await db.Citas.AsNoTracking().SingleAsync(c => c.Id == id, ct); }
        var leidaA = await Leer(citaA); var leidaB = await Leer(citaB); var leidaC = await Leer(citaC);
        Check(leidaA.FechaHoraInicioUtc == A(8, 0).UtcDateTime && leidaA.FechaHoraFinUtc == A(10, 0).UtcDateTime, "Cita A 08:00–10:00 (fin propio, no 18:00)");
        Check(leidaB.FechaHoraInicioUtc == A(10, 30).UtcDateTime && leidaB.FechaHoraFinUtc == A(12, 0).UtcDateTime, "Cita B 10:30–12:00");
        Check(leidaC.FechaHoraInicioUtc == A(13, 0).UtcDateTime && leidaC.FechaHoraFinUtc == A(14, 0).UtcDateTime, "Cita C 13:00–14:00");

        // El resto del rango sigue reservable para otro estudiante y el solapamiento se sigue rechazando.
        var dCal = (await service.LeerCitas(Sesion("dia-d", "Estudiante"), dia, 1, ct)).Calendario.Dias.Single(d => d.Fecha == dia);
        var libres = dCal.Franjas.Where(f => !f.Ocupada).Select(f => (f.InicioUtc, f.FinUtc)).ToArray();
        Check(libres.SequenceEqual(new[] { (A(10, 0).UtcDateTime, A(10, 30).UtcDateTime), (A(12, 0).UtcDateTime, A(13, 0).UtcDateTime), (A(14, 0).UtcDateTime, A(18, 0).UtcDateTime) }),
            "Huecos libres reales entre citas");
        Check(dCal.Franjas.Count(f => f.Ocupada) == 3 && dCal.Franjas.All(f => f.Variable), "Cada cita ocupa solo su propio horario");
        await Rechaza<AgendaOperacionException>(() => service.Reservar(Sesion("dia-d", "Estudiante"), new ReservaCitaInput
        { Inicio = A(9, 0), Modalidad = ModalidadCita.Presencial, DuracionMinutos = 30 }, ct), "Solapamiento con cita A rechazado");
        db.ChangeTracker.Clear();
        var citaD = await Reservar("dia-d", 15, 30, 90);  // 15:30–17:00

        RegistrarAtencionInput Atencion(Guid id, bool asistio = true) => new()
        {
            CitaId = id, Asistio = asistio, ResultadoProfesionalPrivado = "Nota privada", ResultadoBreveEstudiante = "Resumen",
            ResenaRecomendacionesEstudiante = "Recomendaciones"
        };
        async Task Registrar(Guid id, bool asistio = true) { await service.RegistrarAtencion(psicologo, Atencion(id, asistio), ct); db.ChangeTracker.Clear(); }
        void Hora(int hora, int minuto) => reloj.Ahora = A(hora, minuto);

        // CASO 1: cita A — 09:59 no, 10:00 sí.
        Hora(9, 59);
        await Rechaza<AgendaOperacionException>(() => service.PrepararAtencion(psicologo, citaA, ct), "A 09:59: formulario no habilitado");
        await Rechaza<AgendaOperacionException>(() => service.RegistrarAtencion(psicologo, Atencion(citaA), ct), "A 09:59: servidor rechaza el registro");
        db.ChangeTracker.Clear();
        Hora(10, 0);
        Check(!(await service.PrepararAtencion(psicologo, citaA, ct)).Cerrada, "A 10:00: formulario habilitado");
        var bAntes = await Leer(citaB); var cAntes = await Leer(citaC); var dAntes = await Leer(citaD);
        await Registrar(citaA);
        Check((await Leer(citaA)).Estado == EstadoCita.Completada, "A 10:00: atención registrada sin esperar a las 18:00");

        // CASO 3: registrar A no modifica B (ni C ni D).
        foreach (var (antes, nombre) in new[] { (bAntes, "B"), (cAntes, "C"), (dAntes, "D") })
        {
            var despues = await Leer(antes.Id);
            Check(despues.Estado == EstadoCita.Confirmada && despues.FechaHoraInicioUtc == antes.FechaHoraInicioUtc &&
                despues.FechaHoraFinUtc == antes.FechaHoraFinUtc && despues.DisponibilidadPsicologoId == antes.DisponibilidadPsicologoId &&
                !await db.RegistrosAtencionCita.AnyAsync(r => r.CitaId == antes.Id, ct), "Registrar A no modifica la cita " + nombre);
        }
        var disponibilidadDespues = await db.DisponibilidadesPsicologo.AsNoTracking().SingleAsync(d => d.Id == disponibilidad.Id, ct);
        Check(disponibilidadDespues.Activo && disponibilidadDespues.HoraFin == new TimeOnly(18, 0), "La disponibilidad no cambia al registrar");

        // CASO 2: cita B — 11:00 no, 12:00 sí.
        Hora(11, 0);
        await Rechaza<AgendaOperacionException>(() => service.PrepararAtencion(psicologo, citaB, ct), "B 11:00: formulario no habilitado");
        await Rechaza<AgendaOperacionException>(() => service.RegistrarAtencion(psicologo, Atencion(citaB, false), ct), "B 11:00: servidor rechaza el registro");
        db.ChangeTracker.Clear();
        Hora(12, 0);
        await Registrar(citaB, asistio: false);
        Check((await Leer(citaB)).Estado == EstadoCita.NoAsistio, "B 12:00: inasistencia registrada");

        // CASOS 4 y 5: Completada / NoAsistio no se registran otra vez.
        await Rechaza<AgendaOperacionException>(() => service.RegistrarAtencion(psicologo, Atencion(citaA), ct), "A Completada no se registra otra vez");
        db.ChangeTracker.Clear();
        await Rechaza<AgendaOperacionException>(() => service.RegistrarAtencion(psicologo, Atencion(citaB), ct), "B NoAsistio no se registra otra vez");
        db.ChangeTracker.Clear();
        Check((await service.PrepararAtencion(psicologo, citaA, ct)).Cerrada && (await service.PrepararAtencion(psicologo, citaB, ct)).Cerrada,
            "Completada y NoAsistio quedan en solo lectura");
        Check(await db.RegistrosAtencionCita.CountAsync(r => r.CitaId == citaA || r.CitaId == citaB, ct) == 2, "Un único registro por cita");

        // CASO 6: la cita C del mismo día sigue normal aunque A y B ya se cerraron.
        Check((await Leer(citaC)).Estado == EstadoCita.Confirmada, "C sigue Confirmada tras cerrar A y B");
        Hora(13, 59);
        await Rechaza<AgendaOperacionException>(() => service.RegistrarAtencion(psicologo, Atencion(citaC), ct), "C 13:59: todavía no");
        db.ChangeTracker.Clear();
        Hora(14, 0);
        await Registrar(citaC);
        Check((await Leer(citaC)).Estado == EstadoCita.Completada, "C 14:00: atención registrada");

        // CASO 7: la autorización sigue igual (otro psicólogo, estudiante y sesión caducada).
        Hora(17, 0);
        Check(A(17, 0) < A(18, 0), "D se cierra antes del fin de la disponibilidad");
        await Rechaza<AgendaAccesoException>(() => service.PrepararAtencion(ajeno, citaD, ct), "Psicólogo no asignado no abre el registro");
        await Rechaza<AgendaAccesoException>(() => service.RegistrarAtencion(ajeno, Atencion(citaD), ct), "Psicólogo no asignado no registra");
        db.ChangeTracker.Clear();
        await Rechaza<AgendaAccesoException>(() => service.RegistrarAtencion(Sesion("dia-d", "Estudiante"), Atencion(citaD), ct), "Estudiante no registra");
        db.ChangeTracker.Clear();
        var caducada = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "psi-dia"),
            new Claim(ClaimTypes.Role, "Psicologo"), new Claim("Zuni.SecurityStamp", "caducado") }, "Offline"));
        await Rechaza<AgendaAccesoException>(() => service.RegistrarAtencion(caducada, Atencion(citaD), ct), "SecurityStamp caducado no registra");
        db.ChangeTracker.Clear();
        Check((await Leer(citaD)).Estado == EstadoCita.Confirmada, "Intentos no autorizados no cambian la cita D");
        await Registrar(citaD);
        Check((await Leer(citaD)).Estado == EstadoCita.Completada, "D 17:00: el psicólogo asignado registra");

        // La vista del psicólogo usa el fin de cada cita para mostrar "Registrar atención".
        Hora(10, 0);
        var agenda = await service.LeerAgenda(psicologo, dia, ct);
        Check(agenda.Citas.Single(x => x.Id == citaA).FinUtc == A(10, 0).UtcDateTime, "La agenda expone el fin propio de la cita");

        reloj.Ahora = new DateTimeOffset(2030, 1, 7, 12, 0, 0, TimeSpan.Zero);
        return checks;
    }
}
