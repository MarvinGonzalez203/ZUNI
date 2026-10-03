using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using Zuni.Controllers;
using Zuni.Data;

namespace Zuni.Middleware;

public sealed class CambioContrasenaObligatorioMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ApplicationDbContext db)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var id = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var pendiente = await db.Users.AsNoTracking()
                .AnyAsync(u => u.Id == id && u.DebeCambiarContrasena, context.RequestAborted);
            if (pendiente)
            {
                var action = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
                var permitido = action?.ControllerTypeInfo.AsType() == typeof(CuentaController) &&
                    ((action.ActionName == nameof(CuentaController.CambiarContrasenaObligatoria) &&
                      (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsPost(context.Request.Method))) ||
                     (action.ActionName == nameof(CuentaController.CerrarSesion) && HttpMethods.IsPost(context.Request.Method)));
                if (!permitido)
                {
                    context.Response.Headers.CacheControl = "no-store";
                    context.Response.Redirect(context.Request.PathBase + "/Cuenta/CambiarContrasenaObligatoria");
                    return;
                }
            }
        }
        await next(context);
    }
}
