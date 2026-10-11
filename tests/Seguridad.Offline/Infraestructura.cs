using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Zuni.Controllers;
using Zuni.Data;
using Zuni.Models;
using Zuni.Seguridad;
using Zuni.Services;
using Zuni.Services.Administracion;
using Zuni.Services.Agenda;

// Reloj controlable: permite comprobar el bloqueo de 15 minutos y el máximo absoluto de sesión.
internal sealed class Reloj : TimeProvider
{
    public DateTimeOffset Ahora { get; set; } = DateTimeOffset.UtcNow;
    public override DateTimeOffset GetUtcNow() => Ahora;
}

// Envuelve el hasher real de Identity y cuenta las verificaciones (prueba del hash ficticio).
internal sealed class HasherContador : IPasswordHasher<ApplicationUser>
{
    private readonly PasswordHasher<ApplicationUser> real = new();
    public int Verificaciones { get; private set; }
    public string HashPassword(ApplicationUser user, string password) => real.HashPassword(user, password);
    public PasswordVerificationResult VerifyHashedPassword(ApplicationUser user, string hashedPassword, string providedPassword)
    {
        Verificaciones++;
        return real.VerifyHashedPassword(user, hashedPassword, providedPassword);
    }
}

// SQLite en memoria: se eliminan las restricciones CHECK con sintaxis PostgreSQL (regex, btrim).
// Los índices únicos filtrados se conservan y sí se aplican.
internal sealed class OfflineDb(DbContextOptions<ApplicationDbContext> options) : ApplicationDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        foreach (var entity in builder.Model.GetEntityTypes())
            foreach (var constraint in entity.GetCheckConstraints().ToArray())
                entity.RemoveCheckConstraint(constraint.Name!);
    }
}

internal sealed class LlavesEnMemoria : IXmlRepository
{
    private readonly List<System.Xml.Linq.XElement> elementos = [];
    public IReadOnlyCollection<System.Xml.Linq.XElement> GetAllElements() => elementos.Select(x => new System.Xml.Linq.XElement(x)).ToArray();
    public void StoreElement(System.Xml.Linq.XElement element, string friendlyName) => elementos.Add(new(element));
}

internal static class HostPrueba
{
    // Host efímero ligado a 127.0.0.1 con la MISMA configuración de seguridad que Program.cs
    // (SesionZuni, PoliticasAcceso, CabecerasSeguridadMiddleware y middlewares de Zuni).
    // No ejecuta Program ni seeds de Zuni, no carga secretos y no abre PostgreSQL.
    public static async Task<WebApplication> Crear(DbContextOptions<ApplicationDbContext> opciones, Reloj reloj,
        HasherContador hasher, int loginPorMinuto)
    {
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions
        { ApplicationName = typeof(CuentaController).Assembly.GetName().Name, EnvironmentName = "Development" });
        builder.WebHost.UseKestrel();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Zuni:LimitesAcceso:LoginPorMinuto"] = loginPorMinuto.ToString(System.Globalization.CultureInfo.InvariantCulture)
        });
        builder.Services.AddLogging(x => x.AddConsole().SetMinimumLevel(LogLevel.Warning));
        builder.Services.AddControllersWithViews().AddApplicationPart(typeof(CuentaController).Assembly);
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.Configure<KeyManagementOptions>(options =>
        {
            options.XmlRepository = new LlavesEnMemoria();
            options.XmlEncryptor = new Microsoft.AspNetCore.DataProtection.XmlEncryption.NullXmlEncryptor();
        });
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options => SesionZuni.Configurar(options, desarrollo: true));
        builder.Services.AddAuthorization();
        builder.Services.AddZuniLimitesAcceso(builder.Configuration);
        // Un contexto por petición, como en producción (evita entidades obsoletas entre peticiones).
        builder.Services.AddScoped<ApplicationDbContext>(_ => new OfflineDb(opciones));
        builder.Services.AddSingleton<IPasswordHasher<ApplicationUser>>(hasher);
        builder.Services.AddSingleton<TimeProvider>(reloj);
        builder.Services.AddScoped<IAutorizacionClinicaService, AutorizacionClinicaService>();
        builder.Services.AddScoped<IAsignacionPsicologoService, AsignacionPsicologoService>();
        builder.Services.AddScoped<AgendaCitasService>();
        builder.Services.AddScoped<Zuni.Services.BigFive.BigFiveService>();
        builder.Services.AddScoped<GestionSolicitudesService>();

        var app = builder.Build();
        app.Urls.Add("http://127.0.0.1:0");
        app.UseMiddleware<CabecerasSeguridadMiddleware>();
        app.UseRouting();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseMiddleware<Zuni.Middleware.ContenidoPrivadoNoCacheMiddleware>();
        app.UseMiddleware<Zuni.Middleware.CambioContrasenaObligatorioMiddleware>();
        app.UseAuthorization();
        app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
        await app.StartAsync();
        return app;
    }

    public static HttpClient Cliente(WebApplication app) =>
        new(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = true, CookieContainer = new CookieContainer() })
        { BaseAddress = new Uri(app.Urls.Single()) };

    public static string Token(string html)
    {
        var token = WebUtility.HtmlDecode(Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
        if (token.Length == 0) throw new Exception("No se encontró token antiforgery en la página.");
        return token;
    }

    public static async Task<HttpResponseMessage> Post(HttpClient cliente, string pagina, string accion, params (string Clave, string Valor)[] campos)
    {
        var html = await cliente.GetStringAsync(pagina);
        var datos = campos.ToDictionary(c => c.Clave, c => c.Valor);
        datos["__RequestVerificationToken"] = Token(html);
        return await cliente.PostAsync(accion, new FormUrlEncodedContent(datos));
    }

    public static Task<HttpResponseMessage> Login(HttpClient cliente, string correo, string contrasena) =>
        Post(cliente, "/Cuenta/IniciarSesion", "/Cuenta/IniciarSesion", ("Email", correo), ("Password", contrasena));

    public static async Task<string> Texto(HttpResponseMessage respuesta) =>
        WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());
}
