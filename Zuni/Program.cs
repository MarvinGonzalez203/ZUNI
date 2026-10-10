using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<
    IPasswordHasher<ApplicationUser>,
    PasswordHasher<ApplicationUser>>();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<Zuni.Services.IAsignacionPsicologoService, Zuni.Services.AsignacionPsicologoService>();
builder.Services.AddScoped<Zuni.Services.IAutorizacionClinicaService, Zuni.Services.AutorizacionClinicaService>();

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(
        new DirectoryInfo(
            Path.Combine(
                builder.Environment.ContentRootPath,
                "App_Data",
                "keys")))
    .SetApplicationName("Zuni");

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Cuenta/IniciarSesion";
        options.AccessDeniedPath = "/Cuenta/AccesoDenegado";

        // La sesión dura como máximo 30 minutos.
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);

        // No renovar automáticamente la sesión.
        options.SlidingExpiration = false;

        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;

        // Revalidar la sesión contra PostgreSQL.
        options.Events.OnValidatePrincipal = async context =>
        {
            var userId = context.Principal?
                .FindFirstValue(ClaimTypes.NameIdentifier);

            var cookieSecurityStamp = context.Principal?
                .FindFirstValue("Zuni.SecurityStamp");

            // Una cookie sin los datos necesarios ya no es válida.
            if (string.IsNullOrWhiteSpace(userId) ||
                string.IsNullOrWhiteSpace(cookieSecurityStamp))
            {
                context.RejectPrincipal();

                await context.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);

                return;
            }

            var db = context.HttpContext.RequestServices
                .GetRequiredService<ApplicationDbContext>();

            var usuario = await db.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new
                {
                    u.IsActive,
                    u.SecurityStamp
                })
                .SingleOrDefaultAsync();

            // Invalidar la cookie si:
            // - el usuario ya no existe;
            // - fue desactivado;
            // - cambió su SecurityStamp.
            if (usuario is null ||
                !usuario.IsActive ||
                !string.Equals(
                    usuario.SecurityStamp,
                    cookieSecurityStamp,
                    StringComparison.Ordinal))
            {
                context.RejectPrincipal();

                await context.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>();

    await DbInitializer.SeedRolesAsync(dbContext);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseMiddleware<Zuni.Middleware.ContenidoPrivadoNoCacheMiddleware>();
app.UseMiddleware<Zuni.Middleware.CambioContrasenaObligatorioMiddleware>();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
