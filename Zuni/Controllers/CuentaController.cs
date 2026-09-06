using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Zuni.Data;
using Zuni.Models;

namespace Zuni.Controllers;

[Route("Cuenta")]
public sealed class CuentaController(
    IWebHostEnvironment environment,
    ApplicationDbContext db,
    IPasswordHasher<ApplicationUser> passwordHasher) : Controller
{
    private static readonly TimeSpan PasswordResetTokenLifetime =
        TimeSpan.FromMinutes(30);

    // ============================================================
    // INICIAR SESIÓN
    // ============================================================

    [HttpGet("IniciarSesion")]
    public IActionResult IniciarSesion(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAuthenticatedHome();

        return View(new LoginViewModel
        {
            ReturnUrl = returnUrl
        });
    }

    [HttpPost("IniciarSesion")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IniciarSesion(LoginViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAuthenticatedHome();

        if (!ModelState.IsValid)
            return View(model);

        var email = model.Email.Trim();
        var normalizedEmail = email.ToUpperInvariant();

        var dbUser = await db.Users
            .FirstOrDefaultAsync(u =>
                u.NormalizedEmail == normalizedEmail);

        if (dbUser is null || !dbUser.IsActive)
        {
            ModelState.AddModelError(
                string.Empty,
                "El correo o la contraseña son incorrectos.");

            return View(model);
        }

        var passwordResult =
            passwordHasher.VerifyHashedPassword(
                dbUser,
                dbUser.PasswordHash ?? string.Empty,
                model.Password);

        if (passwordResult == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(
                string.Empty,
                "El correo o la contraseña son incorrectos.");

            return View(model);
        }

        if (passwordResult ==
            PasswordVerificationResult.SuccessRehashNeeded)
        {
            dbUser.PasswordHash =
                passwordHasher.HashPassword(
                    dbUser,
                    model.Password);

            await db.SaveChangesAsync();
        }

        await SignInAsync(
            dbUser,
            model.RememberMe);

        return RedirectAfterLogin(model.ReturnUrl);
    }

    // ============================================================
    // REGISTRO
    // ============================================================
    // Los registros nuevos se guardan directamente en PostgreSQL.

    [HttpGet("Registro")]
    public IActionResult Registro()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAuthenticatedHome();

        return View(new RegisterViewModel());
    }

    [HttpPost("Registro")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registro(
        RegisterViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAuthenticatedHome();

        if (!ModelState.IsValid)
            return View(model);

        var email = model.Email.Trim();
        var normalizedEmail = email.ToUpperInvariant();

        var emailAlreadyExists = await db.Users
            .AsNoTracking()
            .AnyAsync(user =>
                user.NormalizedEmail == normalizedEmail);

        if (emailAlreadyExists)
        {
            ModelState.AddModelError(
                nameof(model.Email),
                "Ya existe una cuenta con este correo.");

            return View(model);
        }

        var estudianteRole = await db.Roles
            .AsNoTracking()
            .SingleOrDefaultAsync(role =>
                role.NormalizedName == "ESTUDIANTE");

        if (estudianteRole is null)
        {
            ModelState.AddModelError(
                string.Empty,
                "No fue posible completar el registro. Inténtalo nuevamente.");

            return View(model);
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            FullName = model.FullName.Trim(),
            Email = email,
            NormalizedEmail = normalizedEmail,
            UserName = email,
            NormalizedUserName = normalizedEmail,
            CreatedAtUtc = DateTime.UtcNow,
            IsActive = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };

        user.PasswordHash = passwordHasher.HashPassword(
            user,
            model.Password);

        db.Users.Add(user);
        db.UserRoles.Add(
            new IdentityUserRole<string>
            {
                UserId = user.Id,
                RoleId = estudianteRole.Id
            });

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
            })
        {
            ModelState.AddModelError(
                nameof(model.Email),
                "Ya existe una cuenta con este correo.");

            return View(model);
        }

        await SignInAsync(user, false);

        TempData["Success"] =
            "Tu cuenta fue creada correctamente.";

        return RedirectToAction(
            "Index",
            "Home");
    }

    // ============================================================
    // RECUPERAR CONTRASEÑA
    // ============================================================

    [HttpGet("RecuperarContrasena")]
    public IActionResult RecuperarContrasena()
    {
        return View(
            new ForgotPasswordViewModel());
    }

    [HttpPost("RecuperarContrasena")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecuperarContrasena(
        ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var email = model.Email.Trim();
        var normalizedEmail = email.ToUpperInvariant();
        var user = await db.Users
            .FirstOrDefaultAsync(candidate =>
                candidate.NormalizedEmail == normalizedEmail);

        if (user is not null && user.IsActive)
        {
            var now = DateTime.UtcNow;

            await using var transaction =
                await db.Database.BeginTransactionAsync();

            await db.PasswordResetTokens
                .Where(resetToken =>
                    resetToken.UserId == user.Id &&
                    resetToken.UsedAtUtc == null)
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(
                        resetToken => resetToken.UsedAtUtc,
                        now));

            var token = WebEncoders.Base64UrlEncode(
                RandomNumberGenerator.GetBytes(32));

            db.PasswordResetTokens.Add(
                new PasswordResetToken
                {
                    UserId = user.Id,
                    TokenHash = HashResetToken(token),
                    CreatedAtUtc = now,
                    ExpiresAtUtc = now.Add(
                        PasswordResetTokenLifetime)
                });

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            if (environment.IsDevelopment())
            {
                ViewBag.ResetUrl =
                    Url.Action(
                        nameof(RestablecerContrasena),
                        "Cuenta",
                        new
                        {
                            email,
                            token
                        },
                        Request.Scheme);
            }
        }

        ViewBag.Sent = true;

        return View(model);
    }

    // ============================================================
    // RESTABLECER CONTRASEÑA
    // ============================================================

    [HttpGet("RestablecerContrasena")]
    public IActionResult RestablecerContrasena(
        string email,
        string token)
    {
        return View(
            new ResetPasswordViewModel
            {
                Email = email,
                Token = token
            });
    }

    [HttpPost("RestablecerContrasena")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestablecerContrasena(
        ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var email = model.Email.Trim();
        var normalizedEmail = email.ToUpperInvariant();
        var tokenHash = HashResetToken(model.Token);
        var now = DateTime.UtcNow;

        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var user = await db.Users
            .FirstOrDefaultAsync(candidate =>
                candidate.NormalizedEmail == normalizedEmail &&
                candidate.IsActive);

        var resetTokenId = user is null
            ? null
            : await db.PasswordResetTokens
                .AsNoTracking()
                .Where(resetToken =>
                    resetToken.UserId == user.Id &&
                    resetToken.TokenHash == tokenHash &&
                    resetToken.UsedAtUtc == null &&
                    resetToken.ExpiresAtUtc > now)
                .OrderByDescending(resetToken =>
                    resetToken.CreatedAtUtc)
                .Select(resetToken => (Guid?)resetToken.Id)
                .FirstOrDefaultAsync();

        if (user is null || resetTokenId is null)
        {
            await transaction.RollbackAsync();

            ModelState.AddModelError(
                string.Empty,
                "El enlace no es válido o ya venció.");

            return View(model);
        }

        var consumedTokens = await db.PasswordResetTokens
            .Where(resetToken =>
                resetToken.Id == resetTokenId.Value &&
                resetToken.UsedAtUtc == null &&
                resetToken.ExpiresAtUtc > now)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(
                    resetToken => resetToken.UsedAtUtc,
                    now));

        if (consumedTokens != 1)
        {
            await transaction.RollbackAsync();

            ModelState.AddModelError(
                string.Empty,
                "El enlace no es válido o ya venció.");

            return View(model);
        }

        user.PasswordHash = passwordHasher.HashPassword(
            user,
            model.Password);
        user.SecurityStamp = Guid.NewGuid().ToString();

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["Success"] =
            "Tu contraseña fue actualizada. " +
            "Ya puedes iniciar sesión.";

        return RedirectToAction(
            nameof(IniciarSesion));
    }

    // ============================================================
    // CERRAR SESIÓN
    // ============================================================

    [Authorize]
    [HttpPost("CerrarSesion")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CerrarSesion()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction(
            "Index",
            "Home");
    }

    // ============================================================
    // CREAR COOKIE PARA USUARIO POSTGRESQL
    // ============================================================

    private async Task SignInAsync(
        ApplicationUser user,
        bool persistent)
    {
        var claims = new List<Claim>
        {
            new(
                ClaimTypes.NameIdentifier,
                user.Id),

            new(
                ClaimTypes.Name,
                user.FullName),

            new(
                ClaimTypes.Email,
                user.Email ?? string.Empty)
        };

        // Obtener roles desde PostgreSQL.
        var roles = await (
            from userRole in db.UserRoles
            join role in db.Roles
                on userRole.RoleId equals role.Id
            where userRole.UserId == user.Id
            select role.Name
        )
        .Where(roleName => roleName != null)
        .ToListAsync();

        foreach (var role in roles)
        {
            claims.Add(
                new Claim(
                    ClaimTypes.Role,
                    role!));
        }

        System.Diagnostics.Debug.WriteLine(
            $"Roles cargados para la sesión: " +
            $"{string.Join(", ", roles)}");

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal =
            new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = persistent
            });
    }

    // ============================================================
    // COOKIE DEL SISTEMA JSON ANTIGUO
    // ============================================================

    private async Task SignInAsync(
        AppUser user,
        bool persistent)
    {
        Claim[] claims =
        [
            new(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()),

            new(
                ClaimTypes.Name,
                user.FullName),

            new(
                ClaimTypes.Email,
                user.Email)
        ];

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(
                new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties
            {
                IsPersistent = persistent
            });
    }

    // ============================================================
    // REDIRECCIONES
    // ============================================================

    private IActionResult RedirectAfterLogin(
        string? returnUrl)
    {
        return Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl!)
            : RedirectToAction(
                "Index",
                "Home");
    }

    private IActionResult RedirectToAuthenticatedHome()
    {
        return RedirectToAction(
            "Index",
            "Home");
    }

    private static string HashResetToken(string token)
    {
        return Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(token)));
    }
}
