using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Zuni.Data;
using Zuni.Models;
using Zuni.Models.MiCuenta;
using Zuni.Helpers;

namespace Zuni.Controllers;

[Authorize]
[Route("MiCuenta")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class MiCuentaController(
    ApplicationDbContext db,
    IDataProtectionProvider protection,
    IActionDescriptorCollectionProvider actions) : Controller
{
    private readonly IDataProtector revisionProtector = protection.CreateProtector("Zuni.MiCuenta.Revision.v1");

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var usuario = await UsuarioAsync(false, ct);
        if (usuario is null) return Forbid();
        var roles = await RolesAsync(usuario.Id, ct);
        PrepararLayout(usuario, roles);
        return View(new MiCuentaViewModel
        {
            NombreCompleto = usuario.FullName, Email = usuario.Email, Roles = roles,
            Activo = usuario.IsActive, EsEstudiante = roles.Contains("Estudiante"),
            PerfilCompleto = usuario.PerfilEstudiante?.EstaCompleto == true,
            Estudiante = roles.Contains("Estudiante") && usuario.PerfilEstudiante is not null
                ? Datos(usuario.PerfilEstudiante) : null
        });
    }

    [HttpGet("Editar")]
    public async Task<IActionResult> Editar(CancellationToken ct)
    {
        var usuario = await UsuarioAsync(false, ct);
        if (usuario is null) return Forbid();
        var roles = await RolesAsync(usuario.Id, ct);
        PrepararLayout(usuario, roles);
        var permitido = roles.Contains("Estudiante");
        return View(new EditarMiCuentaViewModel
        {
            NombreCompleto = usuario.FullName,
            PuedeEditarEstudiante = permitido,
            Estudiante = permitido ? usuario.PerfilEstudiante is { } perfil ? Datos(perfil) : new() : null,
            SolicitarCarne = permitido && string.IsNullOrWhiteSpace(usuario.PerfilEstudiante?.Carne),
            Revision = revisionProtector.Protect(RevisionActual(usuario, roles))
        });
    }

    [HttpPost("Editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(EditarMiCuentaViewModel model, CancellationToken ct)
    {
        // Incluye lectura de permisos, usuario y perfil: detecta escrituras concurrentes.
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var usuario = await UsuarioAsync(true, ct);
        if (usuario is null) return Forbid();
        var roles = await RolesAsync(usuario.Id, ct);
        PrepararLayout(usuario, roles);
        if (!RevisionValida(model.Revision, usuario, roles))
            return Conflicto();

        var ticket = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!ticket.Succeeded || ticket.Principal?.FindFirstValue("Zuni.SecurityStamp") != usuario.SecurityStamp)
            return Forbid();

        model.PuedeEditarEstudiante = roles.Contains("Estudiante");
        model.SolicitarCarne = model.PuedeEditarEstudiante && string.IsNullOrWhiteSpace(usuario.PerfilEstudiante?.Carne);
        string? carneNuevo = null;
        if (model.SolicitarCarne)
        {
            if (!CarneHelper.TryConstruir(model.CarneParte1, model.CarneParte2, model.CarneParte3, out var carne))
                ModelState.AddModelError(nameof(model.CarneParte1), CarneHelper.MensajeFormato);
            else if (await db.PerfilesEstudiante.AsNoTracking().AnyAsync(p => p.Carne == carne && p.UsuarioId != usuario.Id, ct))
                ModelState.AddModelError(nameof(model.CarneParte1), "Ya existe un estudiante registrado con este carné.");
            else
                carneNuevo = carne;
        }
        else
        {
            ModelState.Remove(nameof(model.CarneParte1));
            ModelState.Remove(nameof(model.CarneParte2));
            ModelState.Remove(nameof(model.CarneParte3));
        }
        if (!model.PuedeEditarEstudiante)
        {
            model.Estudiante = null;
            foreach (var key in ModelState.Keys.Where(k => k == "Estudiante" || k.StartsWith("Estudiante.", StringComparison.Ordinal)).ToArray())
                ModelState.Remove(key);
        }
        else if (model.Estudiante is null)
            ModelState.AddModelError(nameof(model.Estudiante), "Completa los datos del formulario.");
        else
        {
            model.Estudiante.Carne = usuario.PerfilEstudiante?.Carne;
            if (string.IsNullOrWhiteSpace(model.Estudiante.Telefono))
                ModelState.AddModelError("Estudiante.Telefono", "No puedes dejar vacío tu teléfono.");
            if (string.IsNullOrWhiteSpace(model.Estudiante.Carrera))
                ModelState.AddModelError("Estudiante.Carrera", "No puedes dejar vacía tu carrera.");
        }
        var nombre = model.NombreCompleto?.Trim() ?? string.Empty;
        if (nombre.Length < 2)
            ModelState.AddModelError(nameof(model.NombreCompleto), "El nombre debe tener entre 2 y 100 caracteres.");
        if (!ModelState.IsValid) return View(model);

        var cambioNombre = usuario.FullName != nombre;
        usuario.FullName = nombre;
        // Solo se genera en servidor; jamás se acepta el stamp desde el formulario.
        usuario.ConcurrencyStamp = Guid.NewGuid().ToString();
        if (model.PuedeEditarEstudiante && model.Estudiante is { } datos)
        {
            var perfil = usuario.PerfilEstudiante;
            if (perfil is null)
            {
                perfil = new PerfilEstudiante { UsuarioId = usuario.Id, Carne = carneNuevo! };
                db.PerfilesEstudiante.Add(perfil);
                usuario.PerfilEstudiante = perfil;
            }
            else if (model.SolicitarCarne)
                perfil.Carne = carneNuevo!;
            perfil.Telefono = Opcional(datos.Telefono);
            perfil.Carrera = Opcional(datos.Carrera);
            perfil.Semestre = Opcional(datos.Semestre);
            perfil.CicloAcademico = Opcional(datos.CicloAcademico);
            perfil.NombreContactoEmergencia = Opcional(datos.NombreContactoEmergencia);
            perfil.TelefonoContactoEmergencia = Opcional(datos.TelefonoContactoEmergencia);
            perfil.RelacionContactoEmergencia = Opcional(datos.RelacionContactoEmergencia);
        }
        try
        {
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(ct);
            return Conflicto();
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_PerfilesEstudiante_Carne" })
        {
            await transaction.RollbackAsync(ct);
            ModelState.AddModelError(nameof(model.CarneParte1), "Ya existe un estudiante registrado con este carné.");
            return View(model);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_PerfilesEstudiante_UsuarioId" })
        {
            await transaction.RollbackAsync(ct);
            return Conflicto();
        }
        catch (Exception exception) when (
            exception is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure } ||
            exception is DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.SerializationFailure } })
        {
            await transaction.RollbackAsync(ct);
            return Conflicto();
        }

        if (cambioNombre)
        {
            if (ticket.Properties?.ExpiresUtc is not { } expires || expires <= DateTimeOffset.UtcNow ||
                ticket.Principal?.Identity is not ClaimsIdentity)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                TempData["Success"] = "Tus datos fueron actualizados. Inicia sesión nuevamente para actualizar tu información.";
                return RedirectToAction("IniciarSesion", "Cuenta");
            }
            var principal = ticket.Principal.Clone();
            var identidad = (ClaimsIdentity)principal.Identity!;
            foreach (var claim in identidad.FindAll(identidad.NameClaimType).ToArray())
                identidad.RemoveClaim(claim);
            identidad.AddClaim(new Claim(identidad.NameClaimType, nombre));
            // Se preservan IssuedUtc, ExpiresUtc, persistencia, roles y SecurityStamp.
            ticket.Properties.AllowRefresh = false;
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, ticket.Properties);
        }
        TempData["Success"] = "Tus datos fueron actualizados correctamente.";
        return RedirectToAction(nameof(Index));
    }

    private Task<ApplicationUser?> UsuarioAsync(bool tracking, CancellationToken ct)
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var query = db.Users.Include(u => u.PerfilEstudiante).AsQueryable();
        if (!tracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(u => u.Id == id && u.IsActive, ct);
    }

    private Task<List<string>> RolesAsync(string id, CancellationToken ct) =>
        (from relacion in db.UserRoles.AsNoTracking()
         join rol in db.Roles.AsNoTracking() on relacion.RoleId equals rol.Id
         where relacion.UserId == id && rol.Name != null
         orderby rol.Name
         select rol.Name!).ToListAsync(ct);

    private void PrepararLayout(ApplicationUser usuario, List<string> roles)
    {
        ViewData["DashboardName"] = usuario.FullName;
        ViewData["DashboardRole"] = string.Join(", ", roles);
        var elegido = new[] { "Administrador", "Director", "Psicologo", "Catedratico", "Estudiante" }
            .FirstOrDefault(roles.Contains);
        var existe = actions.ActionDescriptors.Items.Any(a =>
            a.RouteValues.TryGetValue("controller", out var controller) && controller == elegido &&
            a.RouteValues.TryGetValue("action", out var action) && action == "Index");
        ViewData["PanelController"] = elegido is not null && existe ? elegido : "Home";
    }

    private IActionResult Conflicto()
    {
        TempData["Error"] = "Tu cuenta cambió mientras editabas. Vuelve a abrir el formulario e intenta nuevamente.";
        return RedirectToAction(nameof(Editar));
    }

    private bool RevisionValida(string? revision, ApplicationUser usuario, List<string> roles)
    {
        if (string.IsNullOrWhiteSpace(revision)) return false;
        try { return revisionProtector.Unprotect(revision) == RevisionActual(usuario, roles); }
        catch (CryptographicException) { return false; }
    }

    private static string RevisionActual(ApplicationUser usuario, List<string> roles) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            usuario.Id, usuario.ConcurrencyStamp, usuario.SecurityStamp, usuario.FullName,
            Roles = roles, Perfil = usuario.PerfilEstudiante is { } perfil ? Datos(perfil) : null
        }))));

    private static string? Opcional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DatosEstudianteViewModel Datos(PerfilEstudiante p) => new()
    {
        Carne = p.Carne, Telefono = p.Telefono, Carrera = p.Carrera, Semestre = p.Semestre,
        CicloAcademico = p.CicloAcademico, NombreContactoEmergencia = p.NombreContactoEmergencia,
        TelefonoContactoEmergencia = p.TelefonoContactoEmergencia,
        RelacionContactoEmergencia = p.RelacionContactoEmergencia
    };
}
