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
            var role = actor == "est" ? "Estudiante" : "Psicologo";
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
        Check(html.Contains("/Estudiante/Citas/Reservar") && html.Contains("__RequestVerificationToken") && html.Contains("agenda-citas.css"), "Formularios y estilos conectados");
        Check(response.Headers.CacheControl?.NoStore == true, "Contenido privado no-store");
        var token = WebUtility.HtmlDecode(Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
        Check(token.Length > 0, "Token antiforgery renderizado");
        var bad = await client.PostAsync("/Estudiante/Citas/Reservar", new FormUrlEncodedContent(new Dictionary<string, string>
        { ["Inicio"] = "2030-01-07T15:00:00Z", ["Modalidad"] = "Virtual" }));
        Check(bad.StatusCode == HttpStatusCode.BadRequest, "POST sin antiforgery rechazado");
        var ok = await client.PostAsync("/Estudiante/Citas/Reservar", new FormUrlEncodedContent(new Dictionary<string, string>
        { ["Inicio"] = "2030-01-07T15:00:00Z", ["Modalidad"] = "Virtual", ["__RequestVerificationToken"] = token,
          ["PsicologoUsuarioId"] = "psi", ["PerfilEstudianteId"] = Guid.NewGuid().ToString() }));
        Check(ok.StatusCode == HttpStatusCode.Redirect && ok.Headers.Location?.OriginalString == "/Estudiante/Citas", "Reserva HTTP válida con campos extra ignorados");
        Check(await db.Citas.AnyAsync(c => c.PsicologoUsuarioId == "otro-psi" && c.Estado == Zuni.Models.Agenda.EstadoCita.Reservada &&
            db.PerfilesEstudiante.Any(p => p.Id == c.PerfilEstudianteId && p.UsuarioId == "est")), "Identidades manipuladas ignoradas en HTTP");
        client.DefaultRequestHeaders.Remove("X-Offline-Actor"); client.DefaultRequestHeaders.Add("X-Offline-Actor", "otro-psi");
        var agenda = await client.GetAsync("/Psicologo/Agenda?fecha=2030-01-07");
        var agendaHtml = await agenda.Content.ReadAsStringAsync();
        Check(agenda.StatusCode == HttpStatusCode.OK && agendaHtml.Contains("agenda-citas.css"), "Agenda y sección Styles anidada renderizan");
        Check(agendaHtml.Contains("/Psicologo/AgregarDisponibilidad") && agendaHtml.Contains("/Psicologo/GestionarCita"), "Rutas POST psicólogo renderizadas");
        var history = await client.GetAsync("/Psicologo/Historial");
        Check(history.StatusCode == HttpStatusCode.OK, "Historial renderiza");
        var invalid = await client.GetAsync("/Psicologo/Agenda?fecha=incorrecta");
        Check((int)invalid.StatusCode == 409 && (await invalid.Content.ReadAsStringAsync()).Contains("No se pudo completar"), "Errores seguros renderizan");
        client.DefaultRequestHeaders.Remove("X-Offline-Actor"); client.DefaultRequestHeaders.Add("X-Offline-Actor", "mixto-Administrador");
        var denied = await client.GetAsync("/Psicologo/Agenda");
        Check(denied.StatusCode == HttpStatusCode.Forbidden, "Rol combinado denegado a través de HTTP");
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
