using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Zuni.Seguridad;
using Zuni.Services;
namespace Zuni.Controllers;

public sealed partial class CuentaController
{
    private const string CookieReto = "Zuni.Reto2FA";
    private CookieOptions OpcionesReto() => new() { HttpOnly = true, Secure = !environment.IsDevelopment() || Request.IsHttps,
        SameSite = SameSiteMode.Strict, Path = "/Cuenta", MaxAge = TimeSpan.FromMinutes(5), IsEssential = true };

    [HttpGet("VerificarDobleFactor"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult VerificarDobleFactor() => Request.Cookies.ContainsKey(CookieReto) ? View() : RedirectToAction(nameof(IniciarSesion));

    [HttpPost("VerificarDobleFactor"), ValidateAntiForgeryToken, EnableRateLimiting(PoliticasAcceso.Login)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> VerificarDobleFactor(string? codigo, [FromServices] DobleFactorService servicio)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAuthenticatedHome(null);
        if (codigo?.Length > 32) codigo = null;
        try
        {
            var resultado = await servicio.Completar(Request.Cookies[CookieReto], codigo);
            if (resultado is { } r)
            {
                Response.Cookies.Delete(CookieReto, OpcionesReto());
                await SignInAsync(r.Usuario, r.Recordar);
                return r.Usuario.DebeCambiarContrasena ? RedirectToAction(nameof(CambiarContrasenaObligatoria)) : await RedirectAfterLogin(r.Usuario.Id, r.Retorno);
            }
        }
        catch (DobleFactorOperacionException) { }
        ModelState.AddModelError("", "No se pudo verificar el código. Puede ser incorrecto, haberse usado o haber vencido el acceso. Tras cinco intentos debes esperar 15 minutos y volver a iniciar sesión.");
        return View();
    }
}
