using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zuni.Models.Administrador;
using Zuni.Models.Atencion;
using Zuni.Services.Administracion;

namespace Zuni.Controllers;

// Resolución administrativa de solicitudes de atención. Solo metadatos: nunca contenido clínico.
[Authorize(Roles = "Administrador")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("Administrador/SolicitudesAtencion")]
public sealed class AdministradorSolicitudesController(GestionSolicitudesService gestion) : Controller
{
    private string? AdministradorId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpGet("")]
    public async Task<IActionResult> Index(EstadoSolicitudAtencion? estado, string? buscar, int pagina = 1, CancellationToken ct = default)
    {
        if (!ModelState.IsValid) return BadRequest("Filtros inválidos.");
        try { return View(await gestion.Listar(AdministradorId, estado, buscar, pagina, ct)); }
        catch (GestionSolicitudesAccesoException) { return Forbid(); }
        catch (GestionSolicitudesException ex) { return BadRequest(ex.Message); }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detalle(Guid id, CancellationToken ct)
    {
        try { return View(await gestion.Detalle(AdministradorId, id, ct)); }
        catch (GestionSolicitudesAccesoException) { return Forbid(); }
        catch (GestionSolicitudesException) { return NotFound(); }
    }

    [HttpPost("{id:guid}/Asignar"), ValidateAntiForgeryToken]
    public Task<IActionResult> Asignar(Guid id, OperacionSolicitudInput input, CancellationToken ct) =>
        Ejecutar(id, input, () => gestion.Asignar(AdministradorId, id, input.PsicologoUsuarioId, input.Motivo, ct),
            "La solicitud fue asignada al psicólogo seleccionado.");

    [HttpPost("{id:guid}/Reasignar"), ValidateAntiForgeryToken]
    public Task<IActionResult> Reasignar(Guid id, OperacionSolicitudInput input, CancellationToken ct) =>
        Ejecutar(id, input, () => gestion.Reasignar(AdministradorId, id, input.PsicologoUsuarioId, input.Motivo, ct),
            "La solicitud fue reasignada. La asignación anterior quedó finalizada en el historial.");

    [HttpPost("{id:guid}/FinalizarAsignacion"), ValidateAntiForgeryToken]
    public Task<IActionResult> FinalizarAsignacion(Guid id, OperacionSolicitudInput input, CancellationToken ct) =>
        Ejecutar(id, input, () => gestion.FinalizarAsignacion(AdministradorId, id, input.Motivo, ct),
            "La asignación fue finalizada. La solicitud volvió a Pendiente para asignar a otro psicólogo.");

    [HttpPost("{id:guid}/Cerrar"), ValidateAntiForgeryToken]
    public Task<IActionResult> Cerrar(Guid id, OperacionSolicitudInput input, CancellationToken ct) =>
        Ejecutar(id, input, () => gestion.Cerrar(AdministradorId, id, input.Motivo, input.Confirmar, ct),
            "La solicitud fue cerrada. El estudiante podrá iniciar una nueva solicitud si la necesita.");

    private async Task<IActionResult> Ejecutar(Guid id, OperacionSolicitudInput input, Func<Task> operacion, string exito)
    {
        // La ruta manda: un SolicitudId distinto en el formulario se considera manipulación.
        if (input.SolicitudId != id) return BadRequest("Solicitud no válida.");
        if (!ModelState.IsValid)
        {
            TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m)) ?? "Revisa los datos enviados.";
            return RedirectToAction(nameof(Detalle), new { id });
        }
        try
        {
            await operacion();
            TempData["Success"] = exito;
        }
        catch (GestionSolicitudesAccesoException) { return Forbid(); }
        catch (GestionSolicitudesException ex) { TempData["Error"] = ex.Message; }
        catch (Exception ex) when (ex is DbUpdateException or Npgsql.NpgsqlException)
        {
            TempData["Error"] = "No se pudo completar la operación. No se guardaron cambios; vuelve a intentarlo.";
        }
        return RedirectToAction(nameof(Detalle), new { id });
    }
}
