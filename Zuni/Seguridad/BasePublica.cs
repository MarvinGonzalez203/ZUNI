using System.Text.RegularExpressions;
namespace Zuni.Seguridad;
public static class BasePublica
{
    public static string Prefijo(IConfiguration config)
    {
        var value=config["Hosting:PathBase"]??"";
        if(value!=""&&!Regex.IsMatch(value,"^(/[a-z0-9-]+)+$"))throw new InvalidOperationException("Hosting:PathBase debe ser vacío o un prefijo minúsculo sin barra final.");
        return value;
    }
    public static void Aplicar(WebApplication app)
    {
        var prefix=Prefijo(app.Configuration);
        if(prefix.Length==0)return;
        app.UsePathBase(prefix);
        app.Use(async(c,next)=>{if(c.Request.PathBase!=prefix){c.Response.StatusCode=404;return;}await next();});
    }
    public static string Ruta(string value,PathString pathBase)
    {
        if(!value.StartsWith('/')||value.StartsWith("//")||!pathBase.HasValue||value==pathBase.Value||value.StartsWith(pathBase.Value+"/")||value.StartsWith(pathBase.Value+"?"))return value;
        return pathBase+value;
    }
}
