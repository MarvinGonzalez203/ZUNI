using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models;
using Zuni.Models.Agenda;

internal static class ImprevistasRegistroChecks
{
    public static async Task Run(WebApplication app, DbContextOptions<ApplicationDbContext> options, string clave, Reloj reloj)
    {
        var checks = 0;
        void Check(bool ok, string label) { if (!ok) throw new Exception("Registro imprevisto: " + label); checks++; }
        var perfil = new PerfilEstudiante { UsuarioId = "imprevista-temporal", Carne = "9750269001", Carrera = "Prueba", Telefono = "12345678" };
        var mixto = new PerfilEstudiante { UsuarioId = "imprevista-mixto", Carne = "9750269002", Carrera = "Prueba", Telefono = "12345678" };
        await using (var db = new OfflineDb(options)) {
            foreach (var (id, rol) in new[] { ("imprevista-psy", "Psicologo"), ("imprevista-otro-psy", "Psicologo"), ("imprevista-temporal", "Estudiante"), ("imprevista-mixto", "Estudiante") }) {
                var u = new ApplicationUser { Id = id, FullName = id, Email = id + "@example.com", UserName = id + "@example.com",
                    NormalizedEmail = (id + "@example.com").ToUpperInvariant(), NormalizedUserName = (id + "@example.com").ToUpperInvariant(),
                    IsActive = true, DebeCambiarContrasena = id == "imprevista-temporal", SecurityStamp = Guid.NewGuid().ToString() };
                u.PasswordHash = new PasswordHasher<ApplicationUser>().HashPassword(u, clave);
                db.Users.Add(u); db.UserRoles.Add(new() { UserId = id, RoleId = rol });
            }
            db.UserRoles.Add(new() { UserId = "imprevista-mixto", RoleId = "Director" });
            db.PerfilesEstudiante.AddRange(perfil, mixto);
            db.AsignacionesEstudiantePsicologo.AddRange(new() { PerfilEstudianteId = perfil.Id, PsicologoUsuarioId = "imprevista-psy" }, new() { PerfilEstudianteId = mixto.Id, PsicologoUsuarioId = "imprevista-psy" });
            await db.SaveChangesAsync();
        }
        using var psy = HostPrueba.Cliente(app); using var otro = HostPrueba.Cliente(app);
        await HostPrueba.Login(psy, "imprevista-psy@example.com", clave); await HostPrueba.Login(otro, "imprevista-otro-psy@example.com", clave);
        const string url = "/Atenciones/Imprevista";
        var fecha = Zuni.Services.Agenda.AgendaReglas.Hoy(reloj.Ahora.UtcDateTime).AddDays(-1);
        var op = Guid.NewGuid();
        Task<HttpResponseMessage> Save(HttpClient client, Guid? perfilId = null, Guid? operacion = null, string inicio = "10:00", string fin = "10:30") =>
            HostPrueba.Post(client, url, url, ("OperacionId", (operacion ?? op).ToString()), ("PerfilEstudianteId", (perfilId ?? perfil.Id).ToString()),
                ("Fecha", fecha.ToString("yyyy-MM-dd")), ("HoraInicio", inicio), ("HoraFin", fin), ("Modalidad", "Presencial"));
        var form = await psy.GetStringAsync(url);
        Check(form.Contains(perfil.Id.ToString()) && !form.Contains(mixto.Id.ToString()), "selector admite contraseña temporal, excluye roles institucionales");
        Check((await psy.PostAsync(url, new FormUrlEncodedContent([]))).StatusCode == HttpStatusCode.BadRequest, "mantiene antiforgery");
        Check(form.Contains("data-contrasena-pendiente=\"true\"") && form.Contains("data-imprevista-requisito"), "aviso anticipado de contraseña pendiente en el formulario");
        var pendiente = await Save(psy);
        var avisoPendiente = WebUtility.HtmlDecode(await pendiente.Content.ReadAsStringAsync());
        Check(pendiente.StatusCode == HttpStatusCode.Conflict && avisoPendiente.Contains("No se guardó la actividad") && avisoPendiente.Contains("contraseña temporal") && avisoPendiente.Contains("10:00"), "explica contraseña pendiente y conserva los datos sin acceso restringido");
        await using (var db = new OfflineDb(options)) {
            Check(!await db.Citas.AnyAsync(c => c.Id == op) && (await db.Users.SingleAsync(u => u.Id == perfil.UsuarioId)).DebeCambiarContrasena, "rechazo no guarda actividad ni cambia la cuenta");
            // Simular que el estudiante terminó su cambio de contraseña por el flujo de cuenta.
            var cuenta = await db.Users.SingleAsync(u => u.Id == perfil.UsuarioId); cuenta.DebeCambiarContrasena = false; await db.SaveChangesAsync();
        }
        var invalid = await Save(psy, operacion: Guid.NewGuid(), fin: "09:30");
        var htmlInvalid = WebUtility.HtmlDecode(await invalid.Content.ReadAsStringAsync());
        Check(invalid.StatusCode == HttpStatusCode.Conflict && htmlInvalid.Contains("name=\"HoraInicio\"") && htmlInvalid.Contains("10:00"), "horario inválido conserva el formulario y sus datos");
        var resultado = await Save(psy);
        Check(resultado.StatusCode == HttpStatusCode.Redirect && resultado.Headers.Location?.OriginalString.Contains("citaId=" + op) == true, "guarda y dirige a la actividad concreta");
        var agenda = await psy.GetStringAsync(resultado.Headers.Location!);
        Check(agenda.Contains(op.ToString()) && agenda.Contains("data-modal-inicial=\"dia-" + fecha.ToString("yyyy-MM-dd")), "agenda abre el día registrado sin acceso restringido");
        await using (var db = new OfflineDb(options)) {
            var cita = await db.Citas.SingleAsync(c => c.Id == op);
            Check(cita.EsImprevista && cita.PsicologoUsuarioId == "imprevista-psy" && cita.PerfilEstudianteId == perfil.Id, "horario persistido para el profesional asignado");
            Check(!(await db.Users.SingleAsync(u => u.Id == perfil.UsuarioId)).DebeCambiarContrasena, "registro respeta el estado actualizado por el estudiante");
            Check(!await db.SolicitudesAtencion.AnyAsync(s => s.PerfilEstudianteId == perfil.Id) && !await db.VerificacionesMayoriaEdad.AnyAsync(v => v.PerfilEstudianteId == perfil.Id), "no fabrica solicitud ni verificación");
        }
        var completar = await psy.GetAsync("/Psicologo/RegistrarAtencion?citaId=" + op);
        var aviso = WebUtility.HtmlDecode(await completar.Content.ReadAsStringAsync());
        Check(completar.StatusCode == HttpStatusCode.Conflict && aviso.Contains("horario de la atención está guardado") && aviso.Contains("consentimiento"), "explica requisito clínico pendiente sin pantalla de permisos");
        var repetido = await Save(psy);
        Check(repetido.StatusCode == HttpStatusCode.Conflict && WebUtility.HtmlDecode(await repetido.Content.ReadAsStringAsync()).Contains("ya fue guardado"), "doble envío informa sin duplicar");
        var ajeno = await Save(otro, operacion: Guid.NewGuid());
        Check(ajeno.Headers.Location?.OriginalString.Contains("AccesoDenegado") == true || ajeno.StatusCode == HttpStatusCode.Forbidden, "otro profesional no registra para un estudiante ajeno");
        var institucional = await Save(psy, perfilId: mixto.Id, operacion: Guid.NewGuid());
        Check(institucional.Headers.Location?.OriginalString.Contains("AccesoDenegado") == true || institucional.StatusCode == HttpStatusCode.Forbidden, "POST manipulado no admite cuenta institucional");
        await using (var db = new OfflineDb(options)) Check(await db.Citas.CountAsync(c => c.PerfilEstudianteId == perfil.Id || c.PerfilEstudianteId == mixto.Id) == 1, "intentos rechazados no guardan otras actividades");
        Console.WriteLine($"OK: {checks} comprobaciones de atención imprevista con contraseña temporal, guardado, requisitos, formulario y acceso.");
    }
}
