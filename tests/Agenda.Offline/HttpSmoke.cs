using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Zuni.Controllers;
using Zuni.Data;
using Zuni.Services;
using Zuni.Services.Agenda;

// Host efímero con SQLite proporcionado por el runner. No ejecuta Program/seed de Zuni.
internal static class HttpSmoke
{
    public static async Task<int> Run(ApplicationDbContext db, TimeProvider reloj)
    {
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions
        { ApplicationName = typeof(PsicologoController).Assembly.GetName().Name, EnvironmentName = "Offline" });
        builder.WebHost.UseKestrel();
        builder.Services.AddLogging(x => x.AddConsole().SetMinimumLevel(LogLevel.Warning));
        builder.Services.AddControllersWithViews().AddApplicationPart(typeof(PsicologoController).Assembly);
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.Configure<KeyManagementOptions>(options =>
        {
            // Exclusivo del host offline: las llaves efímeras nunca se escriben a disco.
            options.XmlRepository = new LlavesEnMemoria();
            options.XmlEncryptor = new Microsoft.AspNetCore.DataProtection.XmlEncryption.NullXmlEncryptor();
        });
        builder.Services.AddAuthentication("Offline").AddCookie("Offline", options =>
        {
            options.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = 403; return Task.CompletedTask; };
            options.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = 401; return Task.CompletedTask; };
        });
        builder.Services.AddSingleton(db);
        builder.Services.AddSingleton(reloj);
        builder.Services.AddScoped<IAutorizacionClinicaService, AutorizacionClinicaService>();
        builder.Services.AddScoped<IAsignacionPsicologoService, AsignacionPsicologoService>();
        builder.Services.AddScoped<AgendaCitasService>();
        await using var app = builder.Build();
        app.Urls.Add("http://127.0.0.1:0");
        app.UseRouting();
        app.Use((ctx, next) =>
        {
            var actor = ctx.Request.Headers["X-Offline-Actor"].ToString();
            var role = actor is "est" or "otro-est" ? "Estudiante" : "Psicologo";
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, actor),
                new Claim(ClaimTypes.Name, actor), new Claim(ClaimTypes.Role, role), new Claim("Zuni.SecurityStamp", actor + "-stamp") }, "Offline"));
            return next(ctx);
        });
        app.UseAuthorization();
        app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
        await app.StartAsync();
        using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = new Uri(app.Urls.Single()) };
        var count = 0;
        void Check(bool ok, string label) { if (!ok) throw new Exception("HTTP: " + label); count++; }
        client.DefaultRequestHeaders.Add("X-Offline-Actor", "est");
        var response = await client.GetAsync("/Estudiante/Citas?fecha=2030-01-07");
        var html = await response.Content.ReadAsStringAsync();
        Check(response.StatusCode == HttpStatusCode.OK, "Citas renderiza Razor y layout");
        Check(html.Contains("Motivo no registrado"), "Cancelación antigua sin motivo renderiza en Citas");
        Check(html.Contains("&lt;script&gt;alert(1)&lt;/script&gt;") && !html.Contains("<script>alert(1)</script>"), "Motivo propio codificado como texto seguro");
        Check(html.Contains("/Estudiante/Citas/Reservar") && html.Contains("__RequestVerificationToken") && html.Contains("agenda-citas.css"), "Formularios y estilos conectados");
        Check(response.Headers.CacheControl?.NoStore == true, "Contenido privado no-store");
        Check(html.Contains("agenda-grid") && html.Contains("data-bs-toggle=\"modal\"") && html.Contains("modal-dialog-scrollable"), "Calendario y modales diarios renderizados");
        Check(html.Contains("name=\"Inicio\"") && !html.Contains("name=\"PsicologoUsuarioId\"") && !html.Contains("name=\"PerfilEstudianteId\""), "Calendario alumno no envía identidades");
        var ids = Regex.Matches(html, "\\bid=\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToArray();
        Check(ids.Distinct().Count() == ids.Length, "IDs HTML únicos en modales del alumno");
        var token = WebUtility.HtmlDecode(Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
        Check(token.Length > 0, "Token antiforgery renderizado");
        var bad = await client.PostAsync("/Estudiante/Citas/Reservar", new FormUrlEncodedContent(new Dictionary<string, string>
        { ["Inicio"] = "2030-01-07T15:00:00Z", ["Modalidad"] = "Virtual" }));
        Check(bad.StatusCode == HttpStatusCode.BadRequest, "POST sin antiforgery rechazado");
        var ok = await client.PostAsync("/Estudiante/Citas/Reservar", new FormUrlEncodedContent(new Dictionary<string, string>
        { ["Inicio"] = "2030-01-07T15:00:00Z", ["Modalidad"] = "Virtual", ["__RequestVerificationToken"] = token,
          ["PsicologoUsuarioId"] = "psi", ["PerfilEstudianteId"] = Guid.NewGuid().ToString() }));
        Check(ok.StatusCode == HttpStatusCode.Redirect && ok.Headers.Location?.OriginalString == "/Estudiante/Citas", "Reserva HTTP válida con campos extra ignorados");
        Check(await db.Citas.AnyAsync(c => c.PsicologoUsuarioId == "otro-psi" && c.Estado == Zuni.Models.Agenda.EstadoCita.Solicitada &&
            db.PerfilesEstudiante.Any(p => p.Id == c.PerfilEstudianteId && p.UsuarioId == "est")), "Identidades manipuladas ignoradas en HTTP");
        var solicitud = await db.Citas.AsNoTracking().SingleAsync(c => c.Estado == Zuni.Models.Agenda.EstadoCita.Solicitada);
        var citasHtml = await client.GetStringAsync("/Estudiante/Citas");
        Check(citasHtml.Contains("<textarea") && citasHtml.Contains("maxlength=\"300\"") && citasHtml.Contains("data-bs-target=\"#cancelar-cita-"), "Estudiante abre confirmación con textarea");
        Check(citasHtml.Contains("data-bs-dismiss=\"modal\">Volver</button>"), "Volver cierra el modal sin enviar");
        foreach (var motivo in new[] { "", "   ", new string('x', 301) })
        {
            var resultado = await client.PostAsync("/Estudiante/Citas/Cancelar", new FormUrlEncodedContent(new Dictionary<string, string>
            { ["CitaId"] = solicitud.Id.ToString(), ["MotivoCancelacion"] = motivo, ["__RequestVerificationToken"] = token }));
            var mensaje = WebUtility.HtmlDecode(await resultado.Content.ReadAsStringAsync());
            Check((int)resultado.StatusCode == 409 && mensaje.Contains(motivo.Length > 300
                ? "El motivo de cancelación no puede superar los 300 caracteres." : "Debes indicar un motivo de cancelación."), "Validación HTTP estudiante");
        }
        client.DefaultRequestHeaders.Remove("X-Offline-Actor"); client.DefaultRequestHeaders.Add("X-Offline-Actor", "otro-psi");
        var agenda = await client.GetAsync("/Psicologo/Agenda?fecha=2030-01-07");
        var agendaHtml = await agenda.Content.ReadAsStringAsync();
        Check(agenda.StatusCode == HttpStatusCode.OK && agendaHtml.Contains("agenda-citas.css"), "Agenda y sección Styles anidada renderizan");
        Check(agendaHtml.Contains("/Psicologo/AgregarDisponibilidad") && agendaHtml.Contains("/Psicologo/GestionarCita"), "Rutas POST psicólogo renderizadas");
        Check(agendaHtml.Contains("/Psicologo/AgregarHorarioDia") && agendaHtml.Contains("/Psicologo/CambiarDia") && agendaHtml.Contains("value=\"Confirmada\""), "Rutas diarias y aceptación conectadas");
        var agendaIds = Regex.Matches(agendaHtml, "\\bid=\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToArray();
        Check(agendaIds.Distinct().Count() == agendaIds.Length, "IDs HTML únicos en modales del psicólogo");
        var history = await client.GetAsync("/Psicologo/Historial");
        Check(history.StatusCode == HttpStatusCode.OK, "Historial renderiza");
        var historialHtml = await history.Content.ReadAsStringAsync();
        Check(historialHtml.Contains("Motivo no registrado") && historialHtml.Contains("&lt;script&gt;alert(1)&lt;/script&gt;"), "Historial muestra motivo seguro y compatibilidad legado");
        var tokenPsicologo = WebUtility.HtmlDecode(Regex.Match(agendaHtml, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
        Check(agendaHtml.Contains("<textarea") && agendaHtml.Contains("name=\"MotivoCancelacion\""), "Psicólogo usa confirmación con motivo");
        foreach (var motivo in new[] { "", "   ", new string('x', 301) })
        {
            var resultado = await client.PostAsync("/Psicologo/GestionarCita", new FormUrlEncodedContent(new Dictionary<string, string>
            { ["CitaId"] = solicitud.Id.ToString(), ["Estado"] = "Cancelada", ["MotivoCancelacion"] = motivo, ["__RequestVerificationToken"] = tokenPsicologo }));
            var mensaje = WebUtility.HtmlDecode(await resultado.Content.ReadAsStringAsync());
            Check((int)resultado.StatusCode == 409 && mensaje.Contains(motivo.Length > 300
                ? "El motivo de cancelación no puede superar los 300 caracteres." : "Debes indicar un motivo de cancelación."), "Validación HTTP psicólogo");
        }
        Check((await db.Citas.AsNoTracking().SingleAsync(c => c.Id == solicitud.Id)).Estado == Zuni.Models.Agenda.EstadoCita.Solicitada, "POST inválidos no cancelaron");
        var cancelacion = await client.PostAsync("/Psicologo/GestionarCita", new FormUrlEncodedContent(new Dictionary<string, string>
        { ["CitaId"] = solicitud.Id.ToString(), ["Estado"] = "Cancelada", ["MotivoCancelacion"] = "  Cambio de disponibilidad HTTP.  ", ["__RequestVerificationToken"] = tokenPsicologo }));
        Check(cancelacion.StatusCode == HttpStatusCode.Redirect && (await db.Citas.AsNoTracking().SingleAsync(c => c.Id == solicitud.Id)).MotivoCancelacion == "Cambio de disponibilidad HTTP.", "Cancelación HTTP válida aplica Trim");
        var invalid = await client.GetAsync("/Psicologo/Agenda?fecha=incorrecta");
        Check((int)invalid.StatusCode == 409 && (await invalid.Content.ReadAsStringAsync()).Contains("No se pudo completar"), "Errores seguros renderizan");
        client.DefaultRequestHeaders.Remove("X-Offline-Actor"); client.DefaultRequestHeaders.Add("X-Offline-Actor", "mixto-Administrador");
        var denied = await client.GetAsync("/Psicologo/Agenda");
        Check(denied.StatusCode == HttpStatusCode.Forbidden, "Rol combinado denegado a través de HTTP");
        var perfil = await db.PerfilesEstudiante.AsNoTracking().SingleAsync(p => p.UsuarioId == "est");
        var disponibilidad = await db.DisponibilidadesPsicologo.AsNoTracking().FirstAsync(d => d.PsicologoUsuarioId == "otro-psi");
        var cierre = new Zuni.Models.Agenda.Cita
        {
            PerfilEstudianteId = perfil.Id, PsicologoUsuarioId = "otro-psi", DisponibilidadPsicologoId = disponibilidad.Id,
            FechaHoraInicioUtc = reloj.GetUtcNow().UtcDateTime.AddDays(-2), FechaHoraFinUtc = reloj.GetUtcNow().UtcDateTime.AddDays(-2).AddMinutes(50),
            Estado = Zuni.Models.Agenda.EstadoCita.Confirmada
        };
        db.Citas.Add(cierre); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        void Actor(string actor) { client.DefaultRequestHeaders.Remove("X-Offline-Actor"); client.DefaultRequestHeaders.Add("X-Offline-Actor", actor); }
        Actor("est");
        var empty = await client.GetAsync("/Estudiante/Resultados");
        Check(empty.StatusCode == HttpStatusCode.OK && (await empty.Content.ReadAsStringAsync()).Contains("No tienes resultados de atenci"), "Resultados vacíos reales");
        Check((await client.GetAsync($"/Psicologo/RegistrarAtencion?citaId={cierre.Id}")).StatusCode == HttpStatusCode.Forbidden, "Estudiante no abre cierre");
        Actor("otro-psi");
        var formulario = await client.GetAsync($"/Psicologo/RegistrarAtencion?citaId={cierre.Id}");
        var formularioHtml = await formulario.Content.ReadAsStringAsync();
        Check(formulario.StatusCode == HttpStatusCode.OK && formularioHtml.Contains("atencion-cita.js") && formularioHtml.Contains("maxlength=\"4000\""), "Formulario de atención renderiza con estilos y script");
        var cierreToken = WebUtility.HtmlDecode(Regex.Match(formularioHtml, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
        Check((await client.PostAsync("/Psicologo/RegistrarAtencion", new FormUrlEncodedContent(new Dictionary<string, string> { ["CitaId"] = cierre.Id.ToString() }))).StatusCode == HttpStatusCode.BadRequest, "Cierre exige antiforgery");
        var campos = new Dictionary<string, string> { ["CitaId"] = cierre.Id.ToString(), ["Asistio"] = "true", ["__RequestVerificationToken"] = cierreToken };
        var invalido = await client.PostAsync("/Psicologo/RegistrarAtencion", new FormUrlEncodedContent(campos));
        Check(invalido.StatusCode == HttpStatusCode.OK && (await invalido.Content.ReadAsStringAsync()).Contains("Este campo es obligatorio"), "Cierre inválido retorna validaciones sin guardar");
        campos["ResultadoProfesionalPrivado"] = "SECRETO-HTTP-PRIVADO";
        campos["ResultadoBreveEstudiante"] = "COMPARTIDO-HTTP-RESUMEN";
        campos["ResenaRecomendacionesEstudiante"] = "<script>recomendacion()</script>";
        var cerrado = await client.PostAsync("/Psicologo/RegistrarAtencion", new FormUrlEncodedContent(campos));
        Check(cerrado.StatusCode == HttpStatusCode.Redirect && cerrado.Headers.Location?.OriginalString == "/Psicologo/Historial", "Cierre HTTP exitoso");
        var soloLectura = await client.GetAsync($"/Psicologo/RegistrarAtencion?citaId={cierre.Id}");
        Check(soloLectura.StatusCode == HttpStatusCode.Redirect && soloLectura.Headers.Location!.OriginalString.Contains("VerAtencion"), "Cierre existente redirige a solo lectura");
        var privado = await client.GetAsync($"/Psicologo/VerAtencion?citaId={cierre.Id}");
        Check(privado.StatusCode == HttpStatusCode.OK && (await privado.Content.ReadAsStringAsync()).Contains("SECRETO-HTTP-PRIVADO") && privado.Headers.CacheControl?.NoStore == true, "Detalle privado autorizado no-store");
        Check(!(await client.GetStringAsync("/Psicologo/Historial")).Contains("SECRETO-HTTP-PRIVADO"), "Historial no precarga privado");
        Actor("est");
        var publico = await client.GetAsync("/Estudiante/Resultados?PerfilEstudianteId=" + Guid.NewGuid());
        var publicoHtml = await publico.Content.ReadAsStringAsync();
        Check(publico.StatusCode == HttpStatusCode.OK && publicoHtml.Contains("COMPARTIDO-HTTP-RESUMEN") && !publicoHtml.Contains("SECRETO-HTTP-PRIVADO") && !publicoHtml.Contains("ResultadoProfesionalPrivado"), "HTTP estudiante recibe únicamente compartidos; ignora ID arbitrario");
        Check(publicoHtml.Contains("&lt;script&gt;recomendacion()&lt;/script&gt;") && publico.Headers.CacheControl?.NoStore == true, "Textos compartidos codificados, no-store");
        Actor("otro-est");
        Check(!(await client.GetStringAsync("/Estudiante/Resultados?PerfilEstudianteId=" + perfil.Id)).Contains("COMPARTIDO-HTTP-RESUMEN"), "Otro estudiante no consulta resultado alterando URL");
        foreach (var actor in new[] { "psi", "mixto-Administrador", "mixto-Director", "mixto-Catedratico", "est" })
        {
            Actor(actor);
            Check((await client.GetAsync($"/Psicologo/VerAtencion?citaId={cierre.Id}")).StatusCode == HttpStatusCode.Forbidden, "Detalle privado denegado HTTP " + actor);
        }
        await app.StopAsync();
        return count;
    }

    private sealed class LlavesEnMemoria : IXmlRepository
    {
        private readonly List<System.Xml.Linq.XElement> elementos = [];
        public IReadOnlyCollection<System.Xml.Linq.XElement> GetAllElements() => elementos.Select(x => new System.Xml.Linq.XElement(x)).ToArray();
        public void StoreElement(System.Xml.Linq.XElement element, string friendlyName) => elementos.Add(new(element));
    }
}
