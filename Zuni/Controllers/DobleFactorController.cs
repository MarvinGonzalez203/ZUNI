using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Zuni.Models;
using Zuni.Services;
using Zuni.Seguridad;
namespace Zuni.Controllers;

[Authorize, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class DobleFactorController(DobleFactorService servicio) : Controller
{
    [HttpGet] public async Task<IActionResult> Index() => View(await servicio.Estado(User.FindFirstValue(ClaimTypes.NameIdentifier)!));
    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting(PoliticasAcceso.Login)]
    public async Task<IActionResult> Gestionar(DobleFactorInput input, string operacion)
    {
        if (!ModelState.IsValid) return View("Index", new DobleFactorVista((await servicio.Estado(User.FindFirstValue(ClaimTypes.NameIdentifier)!)).Activo, Error: "Revisa la contraseña y el código."));
        DobleFactorVista resultado;
        try { resultado = await servicio.Gestionar(User.FindFirstValue(ClaimTypes.NameIdentifier)!, User.FindFirstValue(SesionZuni.ClaimSecurityStamp)!, input, operacion); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (DobleFactorOperacionException ex) { return View("Index", new DobleFactorVista((await servicio.Estado(User.FindFirstValue(ClaimTypes.NameIdentifier)!)).Activo, Error: ex.Message)); }
        if (resultado.Error is null && resultado.Clave is null)
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        ModelState.Clear(); // Nunca devolver la contraseña ni el código escrito.
        return View("Index", resultado);
    }
}
