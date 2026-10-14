using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zuni.Models.Agenda;
using Zuni.Services.Agenda;
namespace Zuni.Controllers;

[Authorize, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None), AgendaExceptionFilter]
public sealed class ReunionesController(AgendaCitasService agenda) : Controller
{
    [Authorize(Roles = "Psicologo"), HttpGet]
    public async Task<IActionResult> Preparar(Guid citaId, CancellationToken ct) => View(await agenda.PrepararReunion(User, citaId, ct));
    [Authorize(Roles = "Psicologo"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Preparar(GuardarReunionInput input, CancellationToken ct)
    {
        var actual = await agenda.PrepararReunion(User, input.CitaId, ct);
        if (!ModelState.IsValid) { input.Estudiante = actual.Estudiante; input.InicioUtc = actual.InicioUtc; input.Estado = actual.Estado; return View(input); }
        await agenda.GuardarReunion(User, input, ct);
        TempData["Success"] = input.ConfirmarCita ? "Cita virtual confirmada con enlace individual." : "Enlace de reunión actualizado.";
        return RedirectToAction("Agenda", "Psicologo");
    }
    [HttpGet]
    public async Task<IActionResult> Detalle(Guid citaId, CancellationToken ct) => View(await agenda.ConsultarReunion(User, citaId, ct));
    [HttpGet]
    public async Task<IActionResult> Entrar(Guid citaId, CancellationToken ct) => Redirect(await agenda.EntrarReunion(User, citaId, ct));
}
