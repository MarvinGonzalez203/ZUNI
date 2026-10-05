using Microsoft.AspNetCore.Authorization;

namespace Zuni.Middleware;

/// <summary>Evita almacenar respuestas privadas; no sustituye la autorización.</summary>
public sealed class ContenidoPrivadoNoCacheMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Se evalúa antes de ejecutar la acción, también para la respuesta de logout.
        var privado = context.User.Identity?.IsAuthenticated == true ||
            context.GetEndpoint()?.Metadata.GetMetadata<IAuthorizeData>() is not null;

        if (privado)
        {
            // OnStarting se ejecuta en orden inverso: prevalece sobre ResponseCache
            // y sobre otros encabezados establecidos por la acción.
            context.Response.OnStarting(() =>
            {
                context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
                context.Response.Headers.Pragma = "no-cache";
                context.Response.Headers.Expires = "0";
                return Task.CompletedTask;
            });
        }

        await next(context);
    }
}
