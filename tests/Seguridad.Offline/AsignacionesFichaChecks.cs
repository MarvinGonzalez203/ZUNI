using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models;
using Zuni.Models.Administrador;
using Zuni.Models.Agenda;
using Zuni.Models.Atencion;
using Zuni.Services.Administracion;

internal static class AsignacionesFichaChecks
{
    public static async Task Run(WebApplication app, DbContextOptions<ApplicationDbContext> options, string clave, Reloj reloj)
    {
        var checks = 0;
        void Check(bool ok, string label) { if (!ok) throw new Exception("Asignaciones desde ficha: " + label); checks++; }
        var perfiles = new List<Guid>();
        await using (var db = new OfflineDb(options)) {
            foreach (var (id, rol) in new[] { ("ficha-admin", "Administrador"), ("ficha-psi-a", "Psicologo"), ("ficha-psi-b", "Psicologo"), ("ficha-director", "Director") }
                .Concat(Enumerable.Range(0, 28).Select(i => ($"ficha-est-{i}", "Estudiante")))) {
                var u = new ApplicationUser { Id = id, FullName = "ZZ Ficha " + id, UserName = id + "@example.com", Email = id + "@example.com",
                    NormalizedEmail = (id + "@example.com").ToUpperInvariant(), NormalizedUserName = (id + "@example.com").ToUpperInvariant(),
                    IsActive = true, SecurityStamp = Guid.NewGuid().ToString() };
                u.PasswordHash = new PasswordHasher<ApplicationUser>().HashPassword(u, clave);
                db.Users.Add(u); db.UserRoles.Add(new() { UserId = id, RoleId = rol });
                if (rol == "Estudiante") { var p = new PerfilEstudiante { UsuarioId = id, Carne = "975026" + perfiles.Count.ToString("D4"), Carrera = "Prueba", Telefono = "12345678" }; db.PerfilesEstudiante.Add(p); perfiles.Add(p.Id); }
            }
            await db.SaveChangesAsync();
        }
        using var admin = HostPrueba.Cliente(app); using var psy = HostPrueba.Cliente(app); using var director = HostPrueba.Cliente(app);
        await HostPrueba.Login(admin, "ficha-admin@example.com", clave); await HostPrueba.Login(psy, "ficha-psi-a@example.com", clave); await HostPrueba.Login(director, "ficha-director@example.com", clave);
        const string estudiante = "/Administrador/Asignaciones/ficha-est-0", psicologo = "/Administrador/Asignaciones/ficha-psi-a";
        Check((await admin.GetStringAsync("/Administrador/Gestionar/ficha-est-0")).Contains("id=\"gestionar-asignaciones\""), "botón visible en Acciones del estudiante");
        Check((await admin.GetStringAsync("/Administrador/Gestionar/ficha-psi-a")).Contains("Asignar estudiantes"), "botón visible en Acciones del psicólogo");
        Check((await admin.GetStringAsync(estudiante)).Contains("name=\"PsicologoUsuarioId\""), "ficha permite elegir profesional sin solicitud previa");
        Check((await psy.GetAsync(psicologo)).StatusCode == HttpStatusCode.Redirect && (await director.GetAsync(estudiante)).StatusCode == HttpStatusCode.Redirect, "solo Administrador accede al formulario");
        Check((await admin.PostAsync(estudiante, new FormUrlEncodedContent([]))).StatusCode == HttpStatusCode.BadRequest, "exige antiforgery");
        Task<HttpResponseMessage> Save(string page, string source, Guid perfil, string profesional, Guid? actual = null, string? motivo = null, string? action = null) =>
            HostPrueba.Post(admin, page, action ?? page, ("UsuarioId", source), ("PerfilEstudianteId", perfil.ToString()), ("PsicologoUsuarioId", profesional),
                ("AsignacionActualId", actual?.ToString() ?? ""), ("Motivo", motivo ?? ""));
        Check((await Save(estudiante, "ficha-est-1", perfiles[0], "ficha-psi-a")).StatusCode == HttpStatusCode.BadRequest, "rechaza ficha manipulada");
        Check((await Save(estudiante, "ficha-est-0", perfiles[0], "ficha-psi-a")).StatusCode == HttpStatusCode.Redirect, "asigna desde la ficha del estudiante");
        Guid primera;
        using (var sinSolicitud = HostPrueba.Cliente(app)) {
            await HostPrueba.Login(sinSolicitud, "ficha-est-0@example.com", clave);
            Check(WebUtility.HtmlDecode(await sinSolicitud.GetStringAsync("/Estudiante/EstadoSolicitud")).Contains("Aún no tienes una solicitud registrada"), "vínculo administrativo no oculta que falta solicitar atención");
        }
        await using (var db = new OfflineDb(options)) {
            primera = await db.AsignacionesEstudiantePsicologo.Where(a => a.PerfilEstudianteId == perfiles[0] && a.FechaFinalizacionUtc == null).Select(a => a.Id).SingleAsync();
            Check(!await db.SolicitudesAtencion.AnyAsync(s => s.PerfilEstudianteId == perfiles[0]) && !await db.VerificacionesMayoriaEdad.AnyAsync(v => v.PerfilEstudianteId == perfiles[0]), "no fabrica solicitud ni verificación");
            var pagina = await new GestionSolicitudesService(db, reloj).LeerFichaAsignaciones("ficha-admin", "ficha-psi-a", "ZZ Ficha ficha-est", 1, default);
            Check(pagina.Estudiantes.Count == 25 && pagina.Paginas == 2, "paginación limita el trabajo y las filas");
        }
        await Save(estudiante, "ficha-est-0", perfiles[0], "ficha-psi-b", null, "Obsoleto");
        await using (var db = new OfflineDb(options)) Check(await db.AsignacionesEstudiantePsicologo.AnyAsync(a => a.Id == primera && a.FechaFinalizacionUtc == null), "revisión obsoleta no cambia vínculo");
        await Save(estudiante, "ficha-est-0", perfiles[0], "ficha-psi-b", primera);
        await using (var db = new OfflineDb(options)) Check(await db.AsignacionesEstudiantePsicologo.CountAsync(a => a.PerfilEstudianteId == perfiles[0]) == 1, "reasignar exige motivo");
        Guid segunda;
        await Save(estudiante, "ficha-est-0", perfiles[0], "ficha-psi-b", primera, "Cambio administrativo solicitado");
        await using (var db = new OfflineDb(options)) {
            Check(await db.AsignacionesEstudiantePsicologo.AnyAsync(a => a.Id == primera && a.FechaFinalizacionUtc != null), "conserva historial anterior");
            segunda = await db.AsignacionesEstudiantePsicologo.Where(a => a.PerfilEstudianteId == perfiles[0] && a.FechaFinalizacionUtc == null).Select(a => a.Id).SingleAsync();
            db.Citas.Add(new() { PerfilEstudianteId = perfiles[0], PsicologoUsuarioId = "ficha-psi-b", FechaHoraInicioUtc = reloj.GetUtcNow().UtcDateTime.AddDays(1), FechaHoraFinUtc = reloj.GetUtcNow().UtcDateTime.AddDays(1).AddMinutes(30), Estado = EstadoCita.Confirmada, Modalidad = ModalidadCita.Presencial }); await db.SaveChangesAsync();
        }
        await Save(estudiante, "ficha-est-0", perfiles[0], "ficha-psi-a", segunda, "Cambio bloqueado");
        await Save(estudiante, "ficha-est-0", perfiles[0], "ficha-psi-b", segunda, "Fin bloqueado", estudiante + "/Finalizar");
        await using (var db = new OfflineDb(options)) {
            Check(await db.AsignacionesEstudiantePsicologo.AnyAsync(a => a.Id == segunda && a.FechaFinalizacionUtc == null), "citas activas bloquean reasignación y finalización");
            var cita = await db.Citas.SingleAsync(c => c.PerfilEstudianteId == perfiles[0]); cita.Estado = EstadoCita.Cancelada; await db.SaveChangesAsync();
            db.SolicitudesAtencion.Add(new() { PerfilEstudianteId = perfiles[1] }); await db.SaveChangesAsync();
        }
        await Save(estudiante, "ficha-est-0", perfiles[0], "ficha-psi-b", segunda, "Fin del vínculo", estudiante + "/Finalizar");
        await Save(psicologo, "ficha-psi-a", perfiles[1], "ficha-psi-a");
        await using (var db = new OfflineDb(options)) {
            Check(!await db.AsignacionesEstudiantePsicologo.AnyAsync(a => a.PerfilEstudianteId == perfiles[0] && a.FechaFinalizacionUtc == null), "finaliza sin necesitar solicitud");
            Check(await db.SolicitudesAtencion.AnyAsync(s => s.PerfilEstudianteId == perfiles[1] && s.Estado == EstadoSolicitudAtencion.Asignada), "asignación desde psicólogo sincroniza la solicitud existente");
            Check(await db.AuditoriaUsuarios.AnyAsync(a => a.UsuarioAfectadoId == "ficha-est-1" && a.AdministradorId == "ficha-admin" && a.Accion == "ASIGNACION_DESDE_FICHA"), "registra actor y cambio en auditoría");
            Check(!await db.ConsentimientosAtencion.AnyAsync(c => db.SolicitudesAtencion.Any(s => s.Id == c.SolicitudAtencionId && s.PerfilEstudianteId == perfiles[1])), "asignación no crea consentimiento");
        }
        var manipulado = await Save(psicologo, "ficha-psi-a", perfiles[2], "ficha-psi-b");
        Check(manipulado.StatusCode == HttpStatusCode.Forbidden || manipulado.Headers.Location?.OriginalString.Contains("AccesoDenegado") == true, "contexto de ficha no admite otro profesional manipulado");
        await using (var db = new OfflineDb(options)) {
            Check(!await db.AsignacionesEstudiantePsicologo.AnyAsync(a => a.PerfilEstudianteId == perfiles[2]), "POST manipulado no escribe asignaciones");
            db.VerificacionesMayoriaEdad.Add(new() { PerfilEstudianteId = perfiles[1], Estado = Zuni.Models.MayoriaEdad.EstadoMayoriaEdad.Verificada,
                EdadDeclarada = 21, EdadVerificada = 21, ResponsableUsuarioId = "ficha-admin", DocumentoRevisado = Zuni.Models.MayoriaEdad.DocumentoEdad.Dpi, FechaDeclaracionUtc = reloj.Ahora.UtcDateTime });
            await db.SaveChangesAsync();
        }
        using var alumno = HostPrueba.Cliente(app); await HostPrueba.Login(alumno, "ficha-est-1@example.com", clave);
        Check(!(await alumno.GetStringAsync("/Estudiante/Citas")).Contains(">Solicitar cita</button>"), "asignación y edad verificadas no habilitan reserva sin consentimiento");
        var sinConsentimiento = await HostPrueba.Post(alumno, "/Estudiante/Citas", "/Estudiante/Citas/Reservar", ("Inicio", reloj.Ahora.AddDays(1).ToString("O")), ("Modalidad", "Presencial"));
        Check(sinConsentimiento.StatusCode == HttpStatusCode.Conflict && WebUtility.HtmlDecode(await sinConsentimiento.Content.ReadAsStringAsync()).Contains("consentimiento"), "servidor exige consentimiento para reservar");
        Console.WriteLine($"OK: {checks} comprobaciones de asignación desde fichas, roles, concurrencia, historial y requisitos independientes.");
    }
}
