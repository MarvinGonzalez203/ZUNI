using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Zuni.Data;
using Zuni.Models;
using Zuni.Seguridad;
using Zuni.Localizacion;

var builder = WebApplication.CreateBuilder(args);
// Configuración privada de esta copia de desarrollo, compartida por CLI y Visual Studio.
// Permite resolver rutas locales sin depender del almacén User Secrets del proceso lanzador.
// .local-security está fuera del contenido de Zuni, excluida de Git y de la publicación.
if (builder.Environment.IsDevelopment())
    builder.Configuration.AddJsonFile(Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath,
        "..", ".local-security", "desarrollo.json")), optional: true, reloadOnChange: false);
var rutasSeguridad = RutasSeguridad.Obtener(builder.Configuration,builder.Environment);
if(args.Contains("--inicializar-seguridad"))
{
    ProteccionPersistencia.Inicializar(rutasSeguridad.Claves);
    Console.WriteLine("Claves creadas fuera de la aplicación. Respalda este archivo con acceso restringido; no se puede regenerar para recuperar datos.");
    return;
}
builder.Services.AddSingleton(rutasSeguridad);
builder.Services.AddSingleton(ProteccionPersistencia.Cargar(rutasSeguridad.Claves,AislamientoInstalacion.Nombre(builder.Configuration)));
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<BitacoraExterna>();
builder.Services.AddSingleton<InstanciaUnica>();
builder.Services.AddHostedService(s=>s.GetRequiredService<InstanciaUnica>());
builder.Services.AddHostedService<ExportadorSeguridad>();

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
builder.Services.AddZuniIdiomas(builder.Configuration);
builder.Services.AddScoped<Zuni.Services.BigFive.BigFiveService>();
builder.Services.AddScoped<Zuni.Services.IAutorizacionClinicaService, Zuni.Services.AutorizacionClinicaService>();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddScoped<Zuni.Services.Agenda.AgendaCitasService>();
builder.Services.AddHttpClient<Zuni.Services.Correo.ICorreoPin,Zuni.Services.Correo.CorreoPinBrevo>(c=>c.Timeout=TimeSpan.FromSeconds(15));
builder.Services.AddScoped<Zuni.Services.PinProfesionalService>();
builder.Services.AddScoped<Zuni.Services.DobleFactorService>();
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
        options.Cookie.Path = BasePublica.Prefijo(builder.Configuration) is {Length:>0} prefix ? prefix : "/";
        options.Cookie.Domain = null;
    });

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Path = BasePublica.Prefijo(builder.Configuration) is {Length:>0} prefix ? prefix : "/";
    options.Cookie.Domain = null;
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
        options.ForwardLimit=1;
        foreach(var ip in builder.Configuration.GetSection("Hosting:KnownProxies").Get<string[]>()??[])
            options.KnownProxies.Add(System.Net.IPAddress.Parse(ip));
        // Si el proxy no corre en la misma máquina, agregar su IP en KnownProxies.
    });
}

builder.Services.AddAuthorization();

var app = builder.Build();
// Acquire before seeds/migrations/exporters; maintenance writers must also run alone.
// Verification-only CLI is read-only and may inspect the active database.
if(!args.Contains("--verificar-seguridad"))await app.Services.GetRequiredService<InstanciaUnica>().Adquirir();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>();

    if(args.Contains("--exportar-seguridad"))
    {
        var bitacora=scope.ServiceProvider.GetRequiredService<BitacoraExterna>();
        while(await dbContext.RegistrosSeguridad.AnyAsync(r=>!dbContext.Set<ExportacionSeguridad>().Any(a=>a.EventoId==r.EventoId)))
        { if(await bitacora.Exportar(dbContext)==0)throw new InvalidOperationException("Hay otro exportador activo. Repite la orden cuando termine.");dbContext.ChangeTracker.Clear(); }
        Console.WriteLine("Copia externa de auditoría actualizada.");return;
    }
    if(args.Contains("--migrar-seguridad"))
    {
        await MigradorSeguridad.Ejecutar(dbContext);
        Console.WriteLine("Conversión verificada y completada. La evidencia histórica se identifica como tal.");
        return;
    }
    if(args.Contains("--verificar-seguridad"))
    {
        await scope.ServiceProvider.GetRequiredService<BitacoraExterna>().VerificarContraBase(dbContext);
        Console.WriteLine("Integridad verificada: firmas, copia externa y registros de auditoría coinciden.");
        return;
    }
    await MigradorSeguridad.ExigirConversion(dbContext);
    if(!app.Environment.IsDevelopment())await PermisosPersistencia.ValidarProduccion(dbContext);
    await DbInitializer.SeedRolesAsync(dbContext);
    if (SuperAdministrador.Solicitado(args))
    {
        Environment.ExitCode = await SuperAdministrador.EjecutarAsync(scope.ServiceProvider, args, Console.Out);
        return;
    }

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

BasePublica.Aplicar(app);
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
app.UseRequestLocalization();

// Después de UseRouting para respetar [EnableRateLimiting] por endpoint.
app.UseRateLimiter();

app.Use(async(context,next)=>
{
    if(!app.Services.GetRequiredService<InstanciaUnica>().Disponible){context.Response.StatusCode=503;return;}
    await next();
});
app.UseAuthentication();
app.UseMiddleware<Zuni.Middleware.ContenidoPrivadoNoCacheMiddleware>();
app.UseMiddleware<Zuni.Middleware.CambioContrasenaObligatorioMiddleware>();
app.UseMiddleware<Zuni.Middleware.AislamientoSuperAdminMiddleware>();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
