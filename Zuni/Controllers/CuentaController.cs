using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zuni.Models;
using Zuni.Services;

namespace Zuni.Controllers;

[Route("Cuenta")]
public sealed class CuentaController(IUserStore users, IWebHostEnvironment environment) : Controller
{
    [HttpGet("IniciarSesion")]
    public IActionResult IniciarSesion(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAuthenticatedHome();
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost("IniciarSesion")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IniciarSesion(LoginViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAuthenticatedHome();
        if (!ModelState.IsValid) return View(model);
        var user = await users.FindByEmailAsync(model.Email);
        if (user is null || !await users.VerifyPasswordAsync(user, model.Password))
        {
            ModelState.AddModelError(string.Empty, "El correo o la contraseña son incorrectos.");
            return View(model);
        }

        await SignInAsync(user, model.RememberMe);
        return Url.IsLocalUrl(model.ReturnUrl) ? LocalRedirect(model.ReturnUrl!) : RedirectToAction("Index", "Home");
    }

    [HttpGet("Registro")]
    public IActionResult Registro()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAuthenticatedHome();
        return View(new RegisterViewModel());
    }

    [HttpPost("Registro")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registro(RegisterViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAuthenticatedHome();
        if (!ModelState.IsValid) return View(model);
        var user = new AppUser { FullName = model.FullName.Trim(), Email = model.Email };
        if (!await users.CreateAsync(user, model.Password))
        {
            ModelState.AddModelError(nameof(model.Email), "Ya existe una cuenta con este correo.");
            return View(model);
        }
        await SignInAsync(user, false);
        TempData["Success"] = "Tu cuenta fue creada correctamente.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet("RecuperarContrasena")]
    public IActionResult RecuperarContrasena() => View(new ForgotPasswordViewModel());

    [HttpPost("RecuperarContrasena")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecuperarContrasena(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var token = await users.CreateResetTokenAsync(model.Email);
        if (token is not null && environment.IsDevelopment())
            ViewBag.ResetUrl = Url.Action(nameof(RestablecerContrasena), "Cuenta", new { email = model.Email, token }, Request.Scheme);
        ViewBag.Sent = true;
        return View(model);
    }

    [HttpGet("RestablecerContrasena")]
    public IActionResult RestablecerContrasena(string email, string token) => View(new ResetPasswordViewModel { Email = email, Token = token });

    [HttpPost("RestablecerContrasena")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestablecerContrasena(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        if (!await users.ResetPasswordAsync(model.Email, model.Token, model.Password))
        {
            ModelState.AddModelError(string.Empty, "El enlace no es válido o ya venció.");
            return View(model);
        }
        TempData["Success"] = "Tu contraseña fue actualizada. Ya puedes iniciar sesión.";
        return RedirectToAction(nameof(IniciarSesion));
    }

    [Authorize]
    [HttpPost("CerrarSesion")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CerrarSesion()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    private async Task SignInAsync(AppUser user, bool persistent)
    {
        Claim[] claims = [new(ClaimTypes.NameIdentifier, user.Id.ToString()), new(ClaimTypes.Name, user.FullName), new(ClaimTypes.Email, user.Email)];
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties { IsPersistent = persistent });
    }

    private IActionResult RedirectToAuthenticatedHome() => RedirectToAction("Index", "Home");
}
