using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Zuni.Localizacion;

namespace Zuni.Controllers;

[AllowAnonymous, ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class IdiomaController(IConfiguration config) : Controller
{
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Cambiar(string idioma,string? volver)
    {
        if(idioma is not ("es" or "en"))return BadRequest();
        Response.Cookies.Append(ConfiguracionIdioma.NombreCookie(config),CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(idioma=="en"?"en-US":"es-GT",idioma)),new CookieOptions {
            HttpOnly=true,Secure=Request.IsHttps,SameSite=SameSiteMode.Lax,IsEssential=true,Path=Request.PathBase.HasValue?Request.PathBase.Value:"/",MaxAge=TimeSpan.FromDays(365)
        });
        var destino=Url.IsLocalUrl(volver)&&!volver!.StartsWith("/Idioma",StringComparison.OrdinalIgnoreCase)?volver:Url.Content("~/");
        if(Request.Headers.Accept.Any(v=>v?.Contains("application/json",StringComparison.OrdinalIgnoreCase)==true))return Json(new {idioma,volver=destino});
        return LocalRedirect(destino);
    }
}
