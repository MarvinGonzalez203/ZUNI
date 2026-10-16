using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models;
using Zuni.Models.Agenda;
using Zuni.Services.Agenda;

internal static class ReunionesRetiroChecks
{
    public static async Task Run(WebApplication app, DbContextOptions<ApplicationDbContext> options, string clave, Reloj reloj, Guid perfil)
    {
        var checks = 0;
        void Check(bool ok, string text) { if (!ok) throw new Exception("Reuniones/retiro: " + text); checks++; }
        OfflineDb Db() => new(options);
        foreach (var bad in new[] { "http://meet.google.com/abc-defg-hij", "https://meet.google.com.evil.test/abc-defg-hij", "https://meet.google.com@evil.test/abc-defg-hij",
            "https://evil.test/abc-defg-hij", "javascript:alert(1)", "https://meet.google.com/abc-defg-hij?x=1", "https://meet.google.com/abc-defg-hij#x", "https://meet.google.com/landing", "https://meet.google.com/%61bc-defg-hij", "https://meet.google.com:444/abc-defg-hij" })
            Check(!AgendaCitasService.TryNormalizarMeet(bad, out _), "URL inválida rechazada");
        Check(AgendaCitasService.TryNormalizarMeet(" HTTPS://MEET.GOOGLE.COM/ABC-DEFG-HIJ/ ", out var normalized) && normalized == "https://meet.google.com/abc-defg-hij", "normalización sin duplicados");
        var cita = new Cita { PerfilEstudianteId = perfil, PsicologoUsuarioId = "psi3", FechaHoraInicioUtc = reloj.Ahora.UtcDateTime.AddHours(1), FechaHoraFinUtc = reloj.Ahora.UtcDateTime.AddHours(2), Modalidad = ModalidadCita.Virtual, Estado = EstadoCita.Solicitada };
        var otra = new Cita { PerfilEstudianteId = perfil, PsicologoUsuarioId = "psi3", FechaHoraInicioUtc = reloj.Ahora.UtcDateTime.AddHours(3), FechaHoraFinUtc = reloj.Ahora.UtcDateTime.AddHours(4), Modalidad = ModalidadCita.Virtual, Estado = EstadoCita.Solicitada };
        var presencial = new Cita { PerfilEstudianteId = perfil, PsicologoUsuarioId = "psi3", FechaHoraInicioUtc = reloj.Ahora.UtcDateTime.AddHours(5), FechaHoraFinUtc = reloj.Ahora.UtcDateTime.AddHours(6), Modalidad = ModalidadCita.Presencial, Estado = EstadoCita.Solicitada };
        await using (var db = Db())
        {
            var horario = new DisponibilidadPsicologo { PsicologoUsuarioId = "psi3", HoraInicio = new(8, 0), HoraFin = new(18, 0), DuracionMinutos = 50, Modalidad = ModalidadDisponibilidad.Ambas };
            db.DisponibilidadesPsicologo.Add(horario); foreach (var c in new[] { cita, otra, presencial }) { c.DisponibilidadPsicologoId = horario.Id; db.Citas.Add(c); }
            // Simula una cuenta histórica, no un alta permitida por el módulo actual.
            var legacy = new ApplicationUser { Id = "retirado", FullName = "Usuario Retirado", Email = "retirado@miumg.edu.gt", NormalizedEmail = "RETIRADO@MIUMG.EDU.GT", UserName = "retirado@miumg.edu.gt", IsActive = true, SecurityStamp = "retirado-stamp" };
            legacy.PasswordHash = new PasswordHasher<ApplicationUser>().HashPassword(legacy, clave);
            db.Users.Add(legacy); db.UserRoles.Add(new IdentityUserRole<string> { UserId = legacy.Id, RoleId = "Catedratico" });
            await db.SaveChangesAsync();
        }
        using var psy = HostPrueba.Cliente(app); using var student = HostPrueba.Cliente(app); using var otherStudent = HostPrueba.Cliente(app);
        using var admin = HostPrueba.Cliente(app); using var director = HostPrueba.Cliente(app); using var otherPsy = HostPrueba.Cliente(app); using var anon = HostPrueba.Cliente(app);
        await HostPrueba.Login(psy, "psi3@miumg.edu.gt", clave); await HostPrueba.Login(student, "est2@miumg.edu.gt", clave); await HostPrueba.Login(otherStudent, "est@miumg.edu.gt", clave);
        await HostPrueba.Login(admin, "admin@miumg.edu.gt", clave); await HostPrueba.Login(director, "director@miumg.edu.gt", clave); await HostPrueba.Login(otherPsy, "psi2@miumg.edu.gt", clave);
        string Form(Guid id) => "/Reuniones/Preparar?citaId=" + id;
        string Acceso(Guid id) => "/Reuniones/Detalle?citaId=" + id;
        string Entrada(Guid id) => "/Reuniones/Entrar?citaId=" + id;
        async Task<Cita> Leer(Guid id) { await using var db = Db(); return await db.Citas.AsNoTracking().SingleAsync(c => c.Id == id); }
        var prep = await psy.GetAsync(Form(cita.Id)); Check(prep.StatusCode == HttpStatusCode.OK && prep.Headers.CacheControl?.NoStore == true, "preparación privada");
        foreach (var cliente in new[] { student, director, admin, otherPsy }) Check((await cliente.GetAsync(Form(cita.Id))).StatusCode == HttpStatusCode.Redirect, "preparación denegada a otra cuenta");
        Check((await psy.PostAsync("/Reuniones/Preparar", new FormUrlEncodedContent([]))).StatusCode == HttpStatusCode.BadRequest, "antiforgery");
        var without = await HostPrueba.Post(psy, "/Psicologo/Agenda", "/Psicologo/GestionarCita", ("CitaId", cita.Id.ToString()), ("Estado", "Confirmada"));
        Check(without.StatusCode == HttpStatusCode.Conflict && (await Leer(cita.Id)).Estado == EstadoCita.Solicitada, "confirmación antigua no evita enlace obligatorio");
        async Task<HttpResponseMessage> Save(Guid id, Guid revision, string link, bool ack, bool confirm = false) => await HostPrueba.Post(psy, Form(id), "/Reuniones/Preparar",
            ("CitaId", id.ToString()), ("Revision", revision.ToString()), ("Enlace", link), ("AccesoRestringidoRevisado", ack.ToString()), ("ConfirmarCita", confirm.ToString()));
        Check((await Save(cita.Id, cita.Revision, "https://meet.google.com/abc-defg-hij", false, true)).StatusCode == HttpStatusCode.Conflict, "exige revisión del acceso en Meet");
        Check((await Save(cita.Id, cita.Revision, "https://evil.test/abc-defg-hij", true, true)).StatusCode == HttpStatusCode.Conflict, "dominio diferente denegado");
        Check((await Save(cita.Id, cita.Revision, "https://meet.google.com/abc-defg-hij", true, true)).StatusCode == HttpStatusCode.Redirect, "confirma con enlace individual");
        var ready = await Leer(cita.Id); Check(ready.Estado == EstadoCita.Confirmada && ready.FechaEnlaceUtc.HasValue && ready.Revision != cita.Revision, "estado, fecha y revisión");
        Check((await student.GetStringAsync(Acceso(cita.Id))).Contains("15 minutos antes") && (await student.GetAsync(Entrada(cita.Id))).StatusCode == HttpStatusCode.Conflict, "no entrega URL antes del horario de acceso");
        foreach (var cliente in new[] { otherStudent, director, admin, otherPsy, anon }) Check((await cliente.GetAsync(Acceso(cita.Id))).StatusCode == HttpStatusCode.Redirect, "acceso privado a participantes exactos");
        Check((await Save(otra.Id, otra.Revision, ready.EnlaceReunion!, true, true)).StatusCode == HttpStatusCode.Conflict, "no reutiliza enlace de otra cita");
        Check((await Save(cita.Id, cita.Revision, "https://meet.google.com/jkl-mnop-qrs", true)).StatusCode == HttpStatusCode.Conflict, "revisión obsoleta no cambia enlace");
        Check((await Save(cita.Id, ready.Revision, "https://meet.google.com/jkl-mnop-qrs", true)).StatusCode == HttpStatusCode.Redirect, "enlace actualizado con nueva revisión");
        Check((await Save(otra.Id, otra.Revision, "https://meet.google.com/abc-defg-hij", true, true)).StatusCode == HttpStatusCode.Conflict, "enlace sustituido tampoco se reutiliza");
        Check((await psy.GetAsync(Form(presencial.Id))).StatusCode == HttpStatusCode.Conflict, "presencial no exige ni prepara enlace");
        var fechaOriginal = reloj.Ahora; reloj.Ahora = new DateTimeOffset(cita.FechaHoraInicioUtc.AddMinutes(-15));
        await HostPrueba.Login(student, "est2@miumg.edu.gt", clave); await HostPrueba.Login(psy, "psi3@miumg.edu.gt", clave);
        var entry = await student.GetAsync(Entrada(cita.Id)); Check(entry.StatusCode == HttpStatusCode.Redirect && entry.Headers.Location!.OriginalString == "https://meet.google.com/jkl-mnop-qrs", "estudiante entra al Meet de su cita: " + entry.StatusCode + " " + entry.Headers.Location);
        Check((await psy.GetAsync(Entrada(cita.Id))).Headers.Location!.OriginalString == "https://meet.google.com/jkl-mnop-qrs", "profesional autorizado entra");
        var preview = Environment.GetEnvironmentVariable("ZUNI_FORMS_PREVIEW_DIR");
        if (!string.IsNullOrEmpty(preview)) { Directory.CreateDirectory(preview); await File.WriteAllTextAsync(Path.Combine(preview, "reunion-acceso.html"), await student.GetStringAsync(Acceso(cita.Id))); await File.WriteAllTextAsync(Path.Combine(preview, "reunion-preparar.html"), await psy.GetStringAsync(Form(cita.Id))); }
        reloj.Ahora = new DateTimeOffset(cita.FechaHoraFinUtc);
        await HostPrueba.Login(student, "est2@miumg.edu.gt", clave);
        Check((await student.GetAsync(Entrada(cita.Id))).StatusCode == HttpStatusCode.Conflict, "fin del horario cierra acceso aunque siga confirmada");
        reloj.Ahora = new DateTimeOffset(cita.FechaHoraInicioUtc.AddMinutes(-15));
        await HostPrueba.Login(student, "est2@miumg.edu.gt", clave);
        await HostPrueba.Post(student, "/Estudiante/Citas", "/Estudiante/Citas/Cancelar", ("citaId", cita.Id.ToString()), ("MotivoCancelacion", "Cancelación de prueba"));
        Check((await Leer(cita.Id)).Estado == EstadoCita.Cancelada && (await student.GetAsync(Entrada(cita.Id))).StatusCode == HttpStatusCode.Conflict, "cancelación cierra acceso inmediato");
        reloj.Ahora = fechaOriginal;
        var referenciaJson = System.Text.Json.JsonSerializer.Serialize(cita.Id);
        await using (var db = Db()) {var eventos=await db.AuditoriaUsuarios.AsNoTracking().ToListAsync(); Check(await db.EnlacesReunionUsados.CountAsync(e => e.CitaId == cita.Id) == 2 && eventos.Any(a => a.Accion == "CITA_REUNION_ACCEDIDA" && a.DatosNuevos == referenciaJson) && !eventos.Any(a => a.DatosNuevos != null && a.DatosNuevos.Contains("meet.google.com")), "registro antirreutilización y auditoría sin URL");}

        using var retired = HostPrueba.Cliente(app);
        var login = await HostPrueba.Login(retired, "retirado@miumg.edu.gt", clave);
        Check(login.StatusCode == HttpStatusCode.Redirect && login.Headers.Location!.OriginalString.Contains("RolRetirado"), "cuenta histórica recibe aviso al iniciar sesión");
        foreach (var ruta in new[] { "/Catedratico", "/Catedratico/MisCursos", "/Catedratico/Estudiantes", "/Catedratico/AvanceEvaluaciones", "/Catedratico/ResultadosGenerales", "/Catedratico/Referencias" })
            Check((await retired.GetAsync(ruta)).StatusCode == HttpStatusCode.Gone, "ruta retirada cerrada: " + ruta);
        Check((await retired.GetAsync("/MiCuenta")).StatusCode == HttpStatusCode.OK && (await retired.GetAsync("/Privacidad")).StatusCode == HttpStatusCode.OK, "conserva consulta de cuenta y derechos");
        Check(!(await HostPrueba.Texto(await retired.GetAsync("/Cuenta/RolRetirado"))).Contains("Panel del Catedrático"), "navegación funcional retirada");
        var alta = await admin.GetStringAsync("/Administrador/AgregarUsuario"); var cambio = await admin.GetStringAsync("/Administrador/CambiarRol/retirado");
        Check(!alta.Contains("value=\"Catedratico\"") && !cambio.Contains("value=\"Catedratico\""), "rol retirado ausente de selectores");
        await HostPrueba.Post(admin, "/Administrador/CambiarRol/retirado", "/Administrador/CambiarRol", ("UsuarioId", "retirado"), ("NuevoRol", "Catedratico"));
        await using (var db = Db()) Check(await db.UserRoles.AnyAsync(r => r.UserId == "retirado" && r.RoleId == "Catedratico") && !await db.UserRoles.AnyAsync(r => r.UserId == "retirado" && r.RoleId != "Catedratico"), "sin eliminación ni conversión automática");
        await HostPrueba.Post(admin, "/Administrador/CambiarRol/retirado", "/Administrador/CambiarRol", ("UsuarioId", "retirado"), ("NuevoRol", "Director"), ("Motivo", "Revisión institucional de prueba"));
        await using (var db = Db()) {var eventos=await db.AuditoriaUsuarios.AsNoTracking().Where(a=>a.UsuarioAfectadoId=="retirado").ToListAsync(); Check(await db.UserRoles.AnyAsync(r => r.UserId == "retirado" && r.RoleId == "Director") && !await db.UserRoles.AnyAsync(r => r.UserId == "retirado" && r.RoleId == "Catedratico") && eventos.Any(a => a.DatosAnteriores != null && a.DatosAnteriores.Contains("Catedratico")), "cambio explícito auditado conserva historia");}
        Check((await HostPrueba.Login(retired, "retirado@miumg.edu.gt", clave)).Headers.Location!.OriginalString.Contains("Director"), "solo la revisión explícita habilita el panel nuevo");
        Console.WriteLine($"OK: {checks} comprobaciones de reuniones y retiro de Catedrático.");
    }
}
