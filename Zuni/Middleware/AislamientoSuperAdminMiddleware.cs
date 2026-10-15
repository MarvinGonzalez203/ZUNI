using Microsoft.AspNetCore.Mvc.Controllers;
using System.Security.Claims;
namespace Zuni.Middleware;

// El proveedor solo usa el área comercial y sus propios datos, incluso ante roles mal combinados.
public sealed class AislamientoSuperAdminMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if(context.User.Identity?.IsAuthenticated==true&&context.User.IsInRole("SuperAdmin")){
            var a=context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
            var mezclado=context.User.Claims.Any(c=>c.Type==ClaimTypes.Role&&c.Value!="SuperAdmin");
            var propia=a?.ControllerName is "SuperAdmin" or "Tarifas" or "MiCuenta" or "Preferencias" or "Idioma" or "Home" ||
                a?.ControllerName=="Cuenta"&&a.ActionName is "IniciarSesion" or "AccesoDenegado" or "CambiarContrasenaObligatoria" or "MantenerSesion" or "CerrarSesion" ||
                a?.ControllerName=="Privacidad"&&a.ActionName is "Index" or "Solicitar" or "Aceptar" or "Politica";
            if(a is not null&&(!propia||mezclado&&a.ControllerName is not ("Cuenta" or "Idioma" or "Home"))){context.Response.StatusCode=StatusCodes.Status403Forbidden;return;}
        }
        await next(context);
    }
}
