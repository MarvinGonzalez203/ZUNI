namespace Zuni.Seguridad;

// Cabeceras de seguridad compatibles con las vistas actuales.
// La CSP es deliberadamente mínima: NO restringe script-src ni style-src porque las vistas usan
// scripts y estilos inline (layout del panel, Bootstrap). Solo bloquea embebido en otros sitios,
// plugins y cambios de <base>.
public sealed class CabecerasSeguridadMiddleware(RequestDelegate next)
{
    public const string PoliticaContenido =
        "frame-ancestors 'self'; object-src 'none'; base-uri 'self'";

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            // No se sobrescriben valores que una acción haya establecido explícitamente.
            Agregar(headers, "X-Content-Type-Options", "nosniff");
            Agregar(headers, "Referrer-Policy", "strict-origin-when-cross-origin");
            Agregar(headers, "X-Frame-Options", "SAMEORIGIN"); // equivalente a frame-ancestors 'self' para navegadores antiguos
            Agregar(headers, "Content-Security-Policy", PoliticaContenido);
            Agregar(headers, "Permissions-Policy", "camera=(), microphone=(), geolocation=()");
            return Task.CompletedTask;
        });
        return next(context);
    }

    private static void Agregar(IHeaderDictionary headers, string nombre, string valor)
    {
        if (!headers.ContainsKey(nombre))
            headers[nombre] = valor;
    }
}
