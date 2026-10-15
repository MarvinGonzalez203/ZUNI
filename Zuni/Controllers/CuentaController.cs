using Zuni.Helpers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Zuni.Data;
using Zuni.Services.Institucion;
using Zuni.Models.Institucion;
using Zuni.Models;
using Zuni.Services.Administracion;
using Zuni.Seguridad;

namespace Zuni.Controllers;

[Route("Cuenta")]
public sealed class CuentaController(
    IWebHostEnvironment environment,
    ApplicationDbContext db,
    IPasswordHasher<ApplicationUser> passwordHasher,
    TimeProvider reloj) : Controller
{
    private static readonly TimeSpan PasswordResetTokenLifetime =
        TimeSpan.FromMinutes(30);

    // Bloqueo por intentos fallidos (columnas Identity existentes, sin esquema nuevo).
    public const int MaximoIntentosFallidos = 5;
    public static readonly TimeSpan DuracionBloqueo = TimeSpan.FromMinutes(15);

    // Un único mensaje para cuenta inexistente, contraseña incorrecta o cuenta bloqueada.
    public const string MensajeCredencialesInvalidas =
        "Correo o contraseña incorrectos. Si fallaste varias veces, espera unos minutos antes de intentarlo de nuevo.";

    public const string MensajePendienteActivacion =
        "Tu cuenta está pendiente de activación por un administrador.";

    // Hash ficticio válido (mismo algoritmo y costo) para que un correo inexistente
    // o una cuenta bloqueada tarden lo mismo que una verificación real.
    private static readonly ApplicationUser UsuarioFicticio = new() { Id = "zuni-usuario-ficticio" };
    private static string? hashFicticio;

    [HttpGet("AccesoDenegado")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult AccesoDenegado()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }

    // ============================================================
    // INICIAR SESIÓN
    // ============================================================

    [HttpGet("IniciarSesion")]
    public IActionResult IniciarSesion(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAuthenticatedHome(returnUrl);

        return View(new LoginViewModel
        {
            ReturnUrl = returnUrl
        });
    }

    [HttpPost("IniciarSesion")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(PoliticasAcceso.Login)]
    public async Task<IActionResult> IniciarSesion(LoginViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAuthenticatedHome(model.ReturnUrl);

        if (!ModelState.IsValid)
            return View(model);

        var email = model.Email.Trim();
        var normalizedEmail = email.ToUpperInvariant();
        var ahora = reloj.GetUtcNow();

        var dbUser = await db.Users
            .FirstOrDefaultAsync(u =>
                u.NormalizedEmail == normalizedEmail);

        if (dbUser is null)
        {
            // Mismo costo que una verificación real; no se crea ningún registro.
            VerificarFicticio(model.Password);
            return CredencialesInvalidas(model);
        }

        // Bloqueada: no se vuelve a verificar la contraseña real (comparación en memoria,
        // compatible con cualquier proveedor).
        if (dbUser.LockoutEnd is { } finBloqueo && finBloqueo > ahora)
        {
            VerificarFicticio(model.Password);
            return CredencialesInvalidas(model);
        }

        var passwordResult = string.IsNullOrEmpty(dbUser.PasswordHash)
            ? VerificarFicticio(model.Password)
            : passwordHasher.VerifyHashedPassword(
                dbUser,
                dbUser.PasswordHash,
                model.Password);

        if (passwordResult == PasswordVerificationResult.Failed)
        {
            await RegistrarFalloAsync(dbUser.Id, ahora);
            return CredencialesInvalidas(model);
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

        // Credenciales correctas: reinicia el contador aunque la cuenta no pueda entrar.
        if (dbUser.AccessFailedCount != 0 || dbUser.LockoutEnd is not null)
        {
            await db.Users
                .Where(u => u.Id == dbUser.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(u => u.AccessFailedCount, 0)
                    .SetProperty(u => u.LockoutEnd, (DateTimeOffset?)null));
        }

        if (!dbUser.IsActive)
        {
            // Solo se revela "pendiente" después de una contraseña correcta.
            if (await EstadoCuenta.EsRegistroPendienteAsync(db, dbUser.Id))
            {
                ModelState.AddModelError(string.Empty, MensajePendienteActivacion);
                return View(model);
            }
            return CredencialesInvalidas(model);
        }

        await SignInAsync(
            dbUser,
            model.RememberMe);

        if (dbUser.DebeCambiarContrasena)
            return RedirectToAction(nameof(CambiarContrasenaObligatoria));

        return await RedirectAfterLogin(dbUser.Id, model.ReturnUrl);
    }

    private IActionResult CredencialesInvalidas(LoginViewModel model)
    {
        ModelState.AddModelError(string.Empty, MensajeCredencialesInvalidas);
        return View(model);
    }

    private PasswordVerificationResult VerificarFicticio(string password)
    {
        hashFicticio ??= passwordHasher.HashPassword(UsuarioFicticio, Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        passwordHasher.VerifyHashedPassword(UsuarioFicticio, hashFicticio, password);
        return PasswordVerificationResult.Failed;
    }

    private async Task RegistrarFalloAsync(string usuarioId, DateTimeOffset ahora)
    {
        // Incrementos atómicos: dos intentos simultáneos no pierden la cuenta.
        await db.Users
            .Where(u => u.Id == usuarioId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.AccessFailedCount, u => u.AccessFailedCount + 1)
                .SetProperty(u => u.LockoutEnabled, true));

        await db.Users
            .Where(u => u.Id == usuarioId && u.AccessFailedCount >= MaximoIntentosFallidos)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.LockoutEnd, (DateTimeOffset?)ahora.Add(DuracionBloqueo))
                .SetProperty(u => u.AccessFailedCount, 0));
    }

    // ============================================================
    // CAMBIO OBLIGATORIO DE CONTRASEÑA
    // ============================================================

    [Authorize]
    [HttpGet("CambiarContrasenaObligatoria")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> CambiarContrasenaObligatoria()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var pendiente = await db.Users.AsNoTracking()
            .AnyAsync(u => u.Id == id && u.IsActive && u.DebeCambiarContrasena);
        return pendiente ? View(new CambiarContrasenaObligatoriaViewModel())
            : RedirectToAuthenticatedHome();
    }

    [Authorize]
    [HttpPost("CambiarContrasenaObligatoria")]
    [ValidateAntiForgeryToken]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> CambiarContrasenaObligatoria(
        CambiarContrasenaObligatoriaViewModel model)
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var usuario = await db.Users.SingleOrDefaultAsync(u => u.Id == id);
        if (usuario is null || !usuario.IsActive ||
            usuario.SecurityStamp != User.FindFirstValue("Zuni.SecurityStamp"))
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(IniciarSesion));
        }
        if (!usuario.DebeCambiarContrasena)
            return RedirectToAuthenticatedHome();
        if (!ModelState.IsValid)
            return View(model);

        if (passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash ?? string.Empty,
                model.PasswordActual) == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(nameof(model.PasswordActual), "La contraseña actual es incorrecta.");
            return View(model);
        }
        if (passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash ?? string.Empty,
                model.Password) != PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(nameof(model.Password), "La nueva contraseña debe ser distinta de la actual.");
            return View(model);
        }

        usuario.PasswordHash = passwordHasher.HashPassword(usuario, model.Password);
        usuario.DebeCambiarContrasena = false;
        usuario.SecurityStamp = Guid.NewGuid().ToString();
        usuario.ConcurrencyStamp = Guid.NewGuid().ToString();
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var now = DateTime.UtcNow;
            await db.PasswordResetTokens
                .Where(token => token.UserId == usuario.Id && token.UsedAtUtc == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.UsedAtUtc, now));
            // Contraseña y revocación de enlaces se confirman juntas.
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Error"] = "La cuenta cambió durante la operación. Inicia sesión nuevamente.";
            return RedirectToAction(nameof(IniciarSesion));
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["Success"] = "Tu contraseña fue actualizada correctamente. Inicia sesión nuevamente.";
        return RedirectToAction(nameof(IniciarSesion));
    }

    // ============================================================
    // REGISTRO
    // ============================================================
    // Los registros nuevos se guardan directamente en PostgreSQL.

    [HttpGet("Registro")]
    public async Task<IActionResult> Registro()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAuthenticatedHome();

        var configuracion = await ConfiguracionInstitucionService.ObtenerAsync(db);
        if (configuracion.PoliticaCorreo == PoliticaCorreoInstitucion.SoloAltasAdministradas)
            return View("RegistroNoDisponible", configuracion);
        var privacidad = await Zuni.Services.Privacidad.PrivacidadService.VigenteAsync(db, CancellationToken.None);
        if (privacidad is null) { ViewData["PrivacidadPendiente"] = true; return View("RegistroNoDisponible", configuracion); }
        return View(new RegisterViewModel { PoliticaPrivacidadId = privacidad.Id });
    }

    [HttpPost("Registro")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(PoliticasAcceso.Registro)]
    public async Task<IActionResult> Registro(
        RegisterViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAuthenticatedHome();

        var email = model.Email?.Trim() ?? string.Empty;

        var configuracion = await ConfiguracionInstitucionService.ObtenerAsync(db);
        if (configuracion.PoliticaCorreo == PoliticaCorreoInstitucion.SoloAltasAdministradas)
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return View("RegistroNoDisponible", configuracion);
        }
        var errorCorreo = PoliticaCorreoReglas.ValidarCorreo(configuracion, email, registroPublico: true);
        if (errorCorreo is not null) ModelState.AddModelError(nameof(model.Email), errorCorreo);
        var privacidad = await Zuni.Services.Privacidad.PrivacidadService.VigenteAsync(db, CancellationToken.None);
        if (privacidad is null) { Response.StatusCode = StatusCodes.Status403Forbidden; ViewData["PrivacidadPendiente"] = true; return View("RegistroNoDisponible", configuracion); }
        if (!model.AceptaPrivacidad || model.PoliticaPrivacidadId != privacidad.Id) ModelState.AddModelError("", "Lee y acepta la política de privacidad vigente antes de crear tu cuenta.");

        if (!CarneHelper.TryConstruir(
            model.CarneParte1,
            model.CarneParte2,
            model.CarneParte3,
            out var carne))
        {
            ModelState.AddModelError(
                nameof(model.CarneParte1),
                CarneHelper.MensajeFormato);
        }

        if (!ModelState.IsValid)
            return View(model);

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

        var carneAlreadyExists = await db.PerfilesEstudiante
            .AsNoTracking()
            .AnyAsync(perfil =>
                perfil.Carne == carne);

        if (carneAlreadyExists)
        {
            ModelState.AddModelError(
                nameof(model.CarneParte1),
                "Ya existe un estudiante registrado con este carné.");

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
            // Sin verificación por correo: el autorregistro queda pendiente de activación
            // por un Administrador (Administrador → Usuarios → Activar registro).
            IsActive = false,
            LockoutEnabled = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };

        user.PasswordHash = passwordHasher.HashPassword(
            user,
            model.Password);

        var perfil = new PerfilEstudiante
        {
            Id = Guid.NewGuid(),
            UsuarioId = user.Id,
            Carne = carne,
            Telefono = model.Telefono.Trim(),
            Carrera = null,
            Semestre = null,
            CicloAcademico = null,
            NombreContactoEmergencia = null,
            TelefonoContactoEmergencia = null,
            RelacionContactoEmergencia = null,
            FechaCreacionUtc = DateTime.UtcNow,
            Activo = true
        };

        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

        // Volver a comprobar dentro de la transacción si la política cambió mientras se completaba el formulario.
        var privacidadActual = await Zuni.Services.Privacidad.PrivacidadService.VigenteAsync(db, CancellationToken.None);
        if (privacidadActual?.Id != model.PoliticaPrivacidadId) { ModelState.AddModelError("", "La política de privacidad cambió. Revisa la versión vigente y acepta antes de continuar."); return View(model); }
        var politicaActual = await ConfiguracionInstitucionService.ObtenerAsync(db);
        var errorActual = PoliticaCorreoReglas.ValidarCorreo(politicaActual, email, registroPublico: true);
        if (errorActual is not null)
        {
            ModelState.AddModelError(nameof(model.Email), errorActual);
            return View(model);
        }

        db.Users.Add(user);
        db.AceptacionesPrivacidad.Add(new() { UsuarioId = user.Id, PoliticaId = privacidadActual!.Id, FechaUtc = DateTime.UtcNow });
        Zuni.Services.Privacidad.PrivacidadService.Auditar(db, user.Id, user.Id, "PRIVACIDAD_ACEPTADA_REGISTRO", TimeProvider.System, privacidadActual.Id);

        db.UserRoles.Add(
            new IdentityUserRole<string>
            {
                UserId = user.Id,
                RoleId = estudianteRole.Id
            });

        db.PerfilesEstudiante.Add(perfil);

        // Marca el estado "pendiente de activación" sin columnas nuevas. El actor es el propio
        // usuario; no se guarda contraseña ni dato sensible.
        db.AuditoriaUsuarios.Add(new AuditoriaUsuario
        {
            UsuarioAfectadoId = user.Id,
            AdministradorId = user.Id,
            Accion = AccionesAuditoria.RegistroPendienteActivacion,
            DatosAnteriores = null,
            DatosNuevos = System.Text.Json.JsonSerializer.Serialize(new { Roles = new[] { "Estudiante" }, IsActive = false }),
            Motivo = null,
            FechaUtc = reloj.GetUtcNow().UtcDateTime
        });

        try
        {
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
            })
        {
            await transaction.RollbackAsync();

            if (((PostgresException)exception.InnerException).ConstraintName ==
                "IX_PerfilesEstudiante_Carne")
            {
                ModelState.AddModelError(
                    nameof(model.CarneParte1),
                    "Ya existe un estudiante registrado con este carné.");
            }
            else
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Ya existe una cuenta con este correo.");
            }

            return View(model);
        }
        catch (Exception exception) when (exception is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure } ||
            exception is DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.SerializationFailure } })
        {
            await transaction.RollbackAsync();
            db.ChangeTracker.Clear();
            ModelState.AddModelError(string.Empty, "La configuración cambió durante el registro. Revisa los datos y vuelve a intentarlo.");
            return View(model);
        }

        TempData["Success"] =
            "Tu cuenta fue creada y está pendiente de activación por un administrador. " +
            "Podrás iniciar sesión cuando sea activada.";

        return RedirectToAction(
            nameof(IniciarSesion));
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
    [EnableRateLimiting(PoliticasAcceso.Recuperacion)]
    public async Task<IActionResult> RecuperarContrasena(
        ForgotPasswordViewModel model)
    {
        // Sin proveedor de correo, no generar enlaces ni consultar cuentas fuera de desarrollo.
        if (!environment.IsDevelopment())
            return View(new ForgotPasswordViewModel());

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
    [EnableRateLimiting(PoliticasAcceso.Recuperacion)]
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
        user.ConcurrencyStamp = Guid.NewGuid().ToString();
        user.DebeCambiarContrasena = false;
        user.AccessFailedCount = 0;
        user.LockoutEnd = null;

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["Success"] =
            "Tu contraseña fue actualizada. " +
            "Ya puedes iniciar sesión.";

        return RedirectToAction(
            nameof(IniciarSesion));
    }

    // ============================================================
    // MANTENER SESIÓN (actividad mientras se escribe un formulario largo)
    // ============================================================

    // La renovación la hace SlidingExpiration; esta acción solo genera actividad autenticada.
    // No extiende el máximo absoluto ni omite SecurityStamp o el cambio obligatorio.
    [Authorize]
    [HttpGet("MantenerSesion")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult MantenerSesion() => NoContent();

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
            nameof(IniciarSesion));
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
                user.Email ?? string.Empty),

            new(
                "Zuni.SecurityStamp",
                user.SecurityStamp ?? string.Empty),

            // Inicio de la sesión: permite el máximo absoluto aunque la cookie se renueve.
            SesionZuni.CrearClaimInicio(reloj.GetUtcNow())
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

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal =
            new ClaimsPrincipal(identity);

        var authenticationProperties =
            new AuthenticationProperties
            {
                // Mantiene el comportamiento actual de "Recordarme".
                IsPersistent = persistent,

                // 30 minutos de inactividad; la cookie se renueva con la actividad
                // (SlidingExpiration) hasta el máximo absoluto de SesionZuni.
                ExpiresUtc = reloj.GetUtcNow().Add(SesionZuni.Inactividad),
                AllowRefresh = true
            };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            authenticationProperties);
    }

    // ============================================================
    // REDIRECCIONES
    // ============================================================

    private async Task<IActionResult> RedirectAfterLogin(
        string usuarioId,
        string? returnUrl)
    {
        // La cookie recién emitida todavía no actualiza HttpContext.User.
        var roles = await (
            from userRole in db.UserRoles.AsNoTracking()
            join role in db.Roles.AsNoTracking()
                on userRole.RoleId equals role.Id
            where userRole.UserId == usuarioId
            select role.Name
        ).ToListAsync();

        return Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl!)
            : RedirectByRole(rol => roles.Contains(rol));
    }

    private IActionResult RedirectToAuthenticatedHome(string? returnUrl = null)
    {
        return Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl!)
            : RedirectByRole(User.IsInRole);
    }

    [Authorize, HttpGet("RolRetirado")]
    public IActionResult RolRetirado()
    {
        if (!User.IsInRole("Catedratico")) return RedirectToAuthenticatedHome();
        Response.StatusCode = StatusCodes.Status410Gone; return View();
    }

    private IActionResult RedirectByRole(Func<string, bool> tieneRol)
    {
        if (tieneRol("Catedratico") && !tieneRol("Administrador") && !tieneRol("Director")) return RedirectToAction("RolRetirado", "Cuenta");
        // El orden es intencional para usuarios con múltiples roles.
        foreach (var rol in new[] { "SuperAdmin", "Administrador", "Director", "Psicologo", "Estudiante" })
        {
            if (tieneRol(rol))
            {
                var controller = rol switch
                {
                    "SuperAdmin" => "SuperAdmin",
                    "Administrador" => "Administrador",
                    "Director" => "Director",
                    "Psicologo" => "Psicologo",
                    "Estudiante" => "Estudiante",
                    _ => "Home"
                };
                return RedirectToAction("Index", controller);
            }
        }

        return RedirectToAction("Index", "Home");
    }

    private static string HashResetToken(string token)
    {
        return Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(token)));
    }


}
