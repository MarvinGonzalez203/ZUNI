using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Zuni.Data;
using Zuni.Models;
using Zuni.Seguridad;

var builder = WebApplication.CreateBuilder(args);

// La cadena de conexión NUNCA se escribe en el repositorio: User Secrets en Development y
// variable de entorno ConnectionStrings__DefaultConnection en Production.
var cadenaConexion = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(cadenaConexion))
    throw new InvalidOperationException(
        "Falta ConnectionStrings:DefaultConnection. Configúrala con User Secrets (Development) " +
        "o con la variable de entorno ConnectionStrings__DefaultConnection (Production).");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(cadenaConexion));

builder.Services.AddScoped<
    IPasswordHasher<ApplicationUser>,
    PasswordHasher<ApplicationUser>>();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<Zuni.Services.BigFive.BigFiveService>();
builder.Services.AddScoped<Zuni.Services.IAsignacionPsicologoService, Zuni.Services.AsignacionPsicologoService>();
builder.Services.AddScoped<Zuni.Services.IAutorizacionClinicaService, Zuni.Services.AutorizacionClinicaService>();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddScoped<Zuni.Services.Agenda.AgendaCitasService>();
builder.Services.AddScoped<Zuni.Services.Administracion.GestionSolicitudesService>();

// Claves de Data Protection: carpeta persistente configurable (DataProtection:KeysPath o
// variable de entorno DataProtection__KeysPath). Por defecto se mantiene App_Data/keys, así las
// claves existentes siguen siendo válidas. Esa carpeta NUNCA se versiona ni se copia entre entornos.
var carpetaClaves = builder.Configuration["DataProtection:KeysPath"];
carpetaClaves = string.IsNullOrWhiteSpace(carpetaClaves)
    ? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys")
    : Path.GetFullPath(carpetaClaves, builder.Environment.ContentRootPath);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(carpetaClaves))
    .SetApplicationName(Zuni.Seguridad.AislamientoInstalacion.Nombre(builder.Configuration));

// Cookie de sesión: 30 min de inactividad (sliding) + máximo absoluto de 8 h,
// HttpOnly, SameSite=Lax, Secure fuera de Development y revalidación de SecurityStamp.
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => {
        SesionZuni.Configurar(options, builder.Environment.IsDevelopment());
        options.Cookie.Name = Zuni.Seguridad.AislamientoInstalacion.Cookie(builder.Configuration);
    });

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

builder.Services.AddZuniLimitesAcceso(builder.Configuration);

// Detrás de un proxy inverso (Nginx, Caddy, etc.) habilitar Zuni:ProxyInverso=true para usar la
// IP y el esquema reales (necesario para el rate limiting por IP y para HTTPS).
var proxyInverso = builder.Configuration.GetValue<bool>("Zuni:ProxyInverso");
if (proxyInverso)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        // Si el proxy no corre en la misma máquina, agregar su IP en KnownProxies.
    });
}

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>();

    await DbInitializer.SeedRolesAsync(dbContext);

    // Comando manual de un solo uso (ver docs/deployment.md). Solo actúa si no existe ningún
    // Administrador activo y nunca guarda la contraseña generada.
    if (PrimerAdministrador.Solicitado(args))
    {
        Environment.ExitCode = await PrimerAdministrador.EjecutarAsync(scope.ServiceProvider, args, Console.Out);
        return;
    }
}

if (proxyInverso)
    app.UseForwardedHeaders();

// Cabeceras de seguridad en todas las respuestas, incluidas páginas de error.
app.UseMiddleware<CabecerasSeguridadMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Después de UseRouting para respetar [EnableRateLimiting] por endpoint.
app.UseRateLimiter();

app.UseAuthentication();
app.UseMiddleware<Zuni.Middleware.ContenidoPrivadoNoCacheMiddleware>();
app.UseMiddleware<Zuni.Middleware.CambioContrasenaObligatorioMiddleware>();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
