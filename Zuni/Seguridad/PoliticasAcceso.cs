using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Zuni.Seguridad;

// Rate limiting nativo de ASP.NET Core (.NET 8), sin paquetes externos.
// Se aplica solo a los POST de Cuenta mediante [EnableRateLimiting]; los GET no se limitan.
// Particiones por dirección IP y política. Detrás de un proxy inverso, habilitar
// Zuni:ProxyInverso para que RemoteIpAddress sea la IP real (ver docs/deployment.md).
//
// Valores por defecto pensados para no estorbar una demostración en aula (muchos usuarios
// detrás de la misma IP). Configurables en Zuni:LimitesAcceso:* (appsettings o variables de entorno).
public static class PoliticasAcceso
{
    public const string Login = "cuenta-login";
    public const string Registro = "cuenta-registro";
    public const string Recuperacion = "cuenta-recuperacion";

    public const int LoginPorMinutoPredeterminado = 30;
    public const int RegistroPorDiezMinutosPredeterminado = 20;
    public const int RecuperacionPorDiezMinutosPredeterminado = 10;

    public const string MensajeLimite =
        "Demasiados intentos desde esta conexión. Espera un momento y vuelve a intentarlo.";

    public static IServiceCollection AddZuniLimitesAcceso(this IServiceCollection services, IConfiguration configuration)
    {
        var seccion = configuration.GetSection("Zuni:LimitesAcceso");
        var login = Positivo(seccion.GetValue<int?>("LoginPorMinuto"), LoginPorMinutoPredeterminado);
        var registro = Positivo(seccion.GetValue<int?>("RegistroPorDiezMinutos"), RegistroPorDiezMinutosPredeterminado);
        var recuperacion = Positivo(seccion.GetValue<int?>("RecuperacionPorDiezMinutos"), RecuperacionPorDiezMinutosPredeterminado);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(Login, http => Particion(http, Login, login, TimeSpan.FromMinutes(1)));
            options.AddPolicy(Registro, http => Particion(http, Registro, registro, TimeSpan.FromMinutes(10)));
            options.AddPolicy(Recuperacion, http => Particion(http, Recuperacion, recuperacion, TimeSpan.FromMinutes(10)));
            options.OnRejected = async (context, cancellationToken) =>
            {
                var response = context.HttpContext.Response;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var espera))
                    response.Headers.RetryAfter = Math.Ceiling(espera.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                response.Headers.CacheControl = "no-store";
                response.ContentType = "text/html; charset=utf-8";
                // Sin detalles internos (ni IP, ni política, ni contadores).
                await response.WriteAsync(
                    "<!DOCTYPE html><html lang=\""+Zuni.Localizacion.Idioma.Codigo+"\"><head><meta charset=\"utf-8\"><title>"+Zuni.Localizacion.Idioma.Texto("Demasiados intentos")+" · ZUNI</title>" +
                    "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"></head>" +
                    "<body style=\"font-family:system-ui,sans-serif;max-width:40rem;margin:3rem auto;padding:0 1rem\">" +
                    "<h1>"+Zuni.Localizacion.Idioma.Texto("Demasiados intentos")+"</h1><p>" + Zuni.Localizacion.Idioma.Texto(MensajeLimite) + "</p>" +
                    "<p><a href=\"/Cuenta/IniciarSesion\">"+Zuni.Localizacion.Idioma.Texto("Volver a iniciar sesión")+"</a></p></body></html>",
                    cancellationToken);
            };
        });
        return services;
    }

    private static int Positivo(int? valor, int predeterminado) => valor is > 0 ? valor.Value : predeterminado;

    private static RateLimitPartition<string> Particion(HttpContext http, string politica, int permitidos, TimeSpan ventana) =>
        RateLimitPartition.GetFixedWindowLimiter(
            politica + ":" + (http.Connection.RemoteIpAddress?.ToString() ?? "sin-ip"),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitidos,
                Window = ventana,
                QueueLimit = 0,
                AutoReplenishment = true
            });
}
