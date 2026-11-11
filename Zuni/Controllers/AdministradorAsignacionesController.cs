using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models.Administrador;
using Zuni.Services.Administracion;
using Zuni.Services.Privacidad;

namespace Zuni.Controllers;

[Authorize(Roles = "Administrador"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("Administrador/Asignaciones")]
public sealed class AdministradorAsignacionesController(ApplicationDbContext db, GestionSolicitudesService gestion) : Controller
{
    [HttpGet("{id}")]
    public async Task<IActionResult> Index(string id, string? buscar, int pagina = 1, CancellationToken ct = default)
    {
        if (!ModelState.IsValid) return BadRequest("Filtros inválidos.");
        try {
            var actor = await PrivacidadService.UsuarioAsync(db, User, ct);
            return View(await gestion.LeerFichaAsignaciones(actor, id, buscar, pagina, ct));
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (GestionSolicitudesAccesoException) { return Forbid(); }
        catch (GestionSolicitudesException ex) { TempData["Error"] = ex.Message; return RedirectToAction("Gestionar", "Administrador", new { id }); }
    }
    [HttpPost("{id}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(string id, AsignacionFichaInput input, string? buscar, int pagina = 1, CancellationToken ct = default)
        => await Ejecutar(id, input, buscar, pagina, false, ct);

    [HttpPost("{id}/Finalizar"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Finalizar(string id, AsignacionFichaInput input, string? buscar, int pagina = 1, CancellationToken ct = default)
        => await Ejecutar(id, input, buscar, pagina, true, ct);

    private async Task<IActionResult> Ejecutar(string id, AsignacionFichaInput input, string? buscar, int pagina, bool finalizar, CancellationToken ct)
    {
        if (id != input.UsuarioId) return BadRequest("Ficha no válida.");
        if (!ModelState.IsValid) { TempData["Error"] = "Revisa el estudiante, psicólogo y motivo seleccionados."; return RedirectToAction(nameof(Index), new { id, buscar, pagina }); }
        try {
            var actor = await PrivacidadService.UsuarioAsync(db, User, ct);
            if (finalizar) await gestion.FinalizarDesdeFicha(actor, input, ct);
            else await gestion.AsignarDesdeFicha(actor, input, ct);
            TempData["Success"] = finalizar ? "Asignación finalizada. Se conservó su historial."
                : "Asignación guardada. Los requisitos para recibir atención se verifican por separado.";
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (GestionSolicitudesAccesoException) { return Forbid(); }
        catch (GestionSolicitudesException ex) { TempData["Error"] = ex.Message; }
        catch (Exception ex) when (ex is DbUpdateException or Npgsql.NpgsqlException) { TempData["Error"] = "No se pudo guardar la asignación. Recarga la página y vuelve a intentarlo."; }
        return RedirectToAction(nameof(Index), new { id, buscar, pagina });
    }
}
