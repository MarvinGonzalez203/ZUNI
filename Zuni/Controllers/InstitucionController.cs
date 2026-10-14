using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Zuni.Data;
using Zuni.Models.Institucion;
using Zuni.Services.Institucion;

namespace Zuni.Controllers;

[Authorize(Roles = "Administrador")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class InstitucionController(ApplicationDbContext db, TimeProvider reloj) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (!await ConfiguracionInstitucionService.EsAdministradorAsync(db, User.FindFirstValue(ClaimTypes.NameIdentifier), ct)) return Forbid();
        var config = await ConfiguracionInstitucionService.ObtenerAsync(db, ct);
        return View(new ConfiguracionInstitucionViewModel
        {
            Nombre = config.Nombre, CorreoSoporte = config.CorreoSoporte, PoliticaCorreo = config.PoliticaCorreo,
            Lema = config.Lema, ColorPrincipal = config.ColorPrincipal, TieneLogotipo = config.LogotipoPng is not null,
            DominiosAutorizados = config.DominiosAutorizados, Revision = config.Revision
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ConfiguracionInstitucionViewModel model, CancellationToken ct)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!await ConfiguracionInstitucionService.EsAdministradorAsync(db, actor, ct)) return Forbid();
        if (!ModelState.IsValid) return View(model);
        try { await ConfiguracionInstitucionService.GuardarAsync(db, model, actor!, reloj, ct); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ArgumentException ex) { ModelState.AddModelError("Logotipo", ex.Message); return View(model); }
        catch (Exception ex) when (ex is DbUpdateException or NpgsqlException)
        {
            db.ChangeTracker.Clear();
            TempData["Error"] = "La configuración cambió o no se pudo guardar. Revisa la versión actual y vuelve a intentarlo.";
            return RedirectToAction(nameof(Index));
        }
        TempData["Success"] = "Configuración guardada. Las cuentas existentes conservan sus roles y acceso.";
        return RedirectToAction(nameof(Index));
    }

    [AllowAnonymous, HttpGet, ResponseCache(NoStore = true)]
    public async Task<IActionResult> Logotipo(CancellationToken ct)
    {
        var config = await ConfiguracionInstitucionService.ObtenerAsync(db, ct);
        return config.LogotipoPng is null ? NotFound() : File(config.LogotipoPng, "image/png");
    }
}
