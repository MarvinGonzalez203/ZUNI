using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;

namespace Zuni.Seguridad;

// Política de sesión por cookie (sin cambios de esquema):
// - 30 minutos de inactividad (SlidingExpiration renueva la cookie con la actividad);
// - máximo absoluto de 8 horas desde el inicio de sesión, aunque haya actividad;
// - cada petición revalida usuario activo y SecurityStamp contra la base de datos.
public static class SesionZuni
{
    public static readonly TimeSpan Inactividad = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan MaximoAbsoluto = TimeSpan.FromHours(8);
    public const string ClaimInicio = "Zuni.InicioSesionUtc";
    public const string ClaimSecurityStamp = "Zuni.SecurityStamp";

    public static Claim CrearClaimInicio(DateTimeOffset ahora) =>
        new(ClaimInicio, ahora.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64);

    // Sin claim (cookies emitidas antes de este cambio) o fuera de rango => la sesión no es válida.
    public static bool ExcedeMaximo(ClaimsPrincipal? principal, DateTimeOffset ahora)
    {
        var valor = principal?.FindFirstValue(ClaimInicio);
        if (!long.TryParse(valor, NumberStyles.None, CultureInfo.InvariantCulture, out var segundos))
            return true;
        DateTimeOffset inicio;
        try { inicio = DateTimeOffset.FromUnixTimeSeconds(segundos); }
        catch (ArgumentOutOfRangeException) { return true; }
        // Un inicio "en el futuro" (más de 5 minutos de desfase) también se rechaza.
        return inicio > ahora.AddMinutes(5) || ahora - inicio > MaximoAbsoluto;
    }

    public static void Configurar(CookieAuthenticationOptions options, bool desarrollo)
    {
        options.LoginPath = "/Cuenta/IniciarSesion";
        options.AccessDeniedPath = "/Cuenta/AccesoDenegado";
        options.ExpireTimeSpan = Inactividad;
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = desarrollo
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Events.OnValidatePrincipal = ValidarPrincipalAsync;
    }

    public static async Task ValidarPrincipalAsync(CookieValidatePrincipalContext context)
    {
        var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var cookieSecurityStamp = context.Principal?.FindFirstValue(ClaimSecurityStamp);
        var reloj = context.HttpContext.RequestServices.GetService<TimeProvider>() ?? TimeProvider.System;

        // Una cookie sin los datos necesarios, o que superó el máximo absoluto, ya no es válida.
        if (string.IsNullOrWhiteSpace(userId) ||
            string.IsNullOrWhiteSpace(cookieSecurityStamp) ||
            ExcedeMaximo(context.Principal, reloj.GetUtcNow()))
        {
            await RechazarAsync(context);
            return;
        }

        var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
        var usuario = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.IsActive, u.SecurityStamp })
            .SingleOrDefaultAsync();

        // Invalidar si el usuario ya no existe, fue desactivado o cambió su SecurityStamp.
        if (usuario is null ||
            !usuario.IsActive ||
            !string.Equals(usuario.SecurityStamp, cookieSecurityStamp, StringComparison.Ordinal))
        {
            await RechazarAsync(context);
        }
    }

    private static async Task RechazarAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
