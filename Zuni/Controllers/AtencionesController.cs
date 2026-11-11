using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zuni.Models.Agenda;
using Zuni.Services.Agenda;
using Zuni.Services.Informes;
namespace Zuni.Controllers;

[Authorize, AgendaExceptionFilter, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AtencionesController(AgendaCitasService agenda) : Controller
{
    [Authorize(Roles="Psicologo"), HttpGet]
    public async Task<IActionResult> Imprevista(DateOnly? fecha, CancellationToken ct) => View(await agenda.PrepararImprevista(User, fecha, ct));
    [Authorize(Roles="Psicologo"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Imprevista(ImprevistaInput input, CancellationToken ct)
    {
        var actual = await agenda.PrepararImprevista(User, null, ct);
        input.Estudiantes = actual.Estudiantes;
        if (!ModelState.IsValid) { input.Estudiantes = actual.Estudiantes; return View(input); }
        Guid citaId;
        try { citaId = await agenda.RegistrarImprevista(User, input, ct); }
        catch (AgendaOperacionException ex) {
            ModelState.AddModelError(string.Empty, ex.Message);
            Response.StatusCode = StatusCodes.Status409Conflict;
            return View(input);
        }
        TempData["Success"] = "Atención imprevista registrada. Abre la actividad para completar el registro profesional; no hace falta una solicitud posterior del estudiante.";
        return RedirectToAction("Agenda", "Psicologo", new { fecha = input.Fecha?.ToString("yyyy-MM-dd"), citaId });
    }
    [HttpGet]
    public async Task<IActionResult> Reporte(Guid citaId, CancellationToken ct) => View(await agenda.ReporteAtencion(User, citaId, false, ct));
    [HttpGet]
    public async Task<IActionResult> Pdf(Guid citaId, CancellationToken ct) => File(InformePdf.CrearCompartido(await agenda.ReporteAtencion(User, citaId, true, ct)), "application/pdf", $"reporte-compartido-{citaId:N}.pdf");
}
