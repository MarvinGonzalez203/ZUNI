using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zuni.Services.BigFive;
namespace Zuni.Controllers;

// El servicio central decide roles reales, combinaciones institucionales y acceso al recurso.
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class BigFivePsicologoController(BigFiveService service) : Controller
{
    [HttpGet("Psicologo/Expediente/{solicitudId:guid}/BigFive")]
    public async Task<IActionResult> Resumen(Guid solicitudId, CancellationToken ct)
    {
        try
        {
            var resumen = await service.Resumen(User, solicitudId, ct);
            ViewData["SolicitudId"] = solicitudId;
            return View(resumen);
        }
        catch (BigFiveAccesoException) { return Forbid(); }
        catch (Exception ex) when (ex is DbUpdateException or Npgsql.NpgsqlException)
        {
            // No devolver contenido si falla autorización/lectura/auditoría/commit. No registrar datos clínicos.
            return StatusCode(503, "No fue posible completar la consulta. Intenta nuevamente.");
        }
    }
}
