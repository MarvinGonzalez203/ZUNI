using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zuni.Services.Agenda;
namespace Zuni.Controllers;

[Authorize(Roles = "Psicologo"), AgendaExceptionFilter, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AusenciasController(AgendaCitasService agenda) : Controller
{
    [HttpGet] public async Task<IActionResult> Avisar(Guid citaId, CancellationToken ct) => View(await agenda.PrepararAusencia(User, citaId, ct));
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Confirmar(Guid citaId, Guid revision, CancellationToken ct)
    {
        await agenda.AvisarAusencia(User, citaId, revision, ct);
        TempData["Success"] = "Aviso guardado para el estudiante. La cita conserva su historial y está pendiente de nueva programación.";
        return RedirectToAction(nameof(Avisar), new { citaId });
    }
}
