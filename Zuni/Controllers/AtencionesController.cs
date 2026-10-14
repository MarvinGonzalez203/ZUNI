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
        var actual = await agenda.PrepararImprevista(User, input.Fecha, ct);
        if (!ModelState.IsValid) { input.Estudiantes = actual.Estudiantes; return View(input); }
        await agenda.RegistrarImprevista(User, input, ct);
        TempData["Success"] = "Atención imprevista registrada. Selecciona su día para completar el registro clínico cuando los requisitos del expediente estén listos.";
        return RedirectToAction("Agenda", "Psicologo", new { fecha = input.Fecha?.ToString("yyyy-MM-dd") });
    }
    [HttpGet]
    public async Task<IActionResult> Reporte(Guid citaId, CancellationToken ct) => View(await agenda.ReporteAtencion(User, citaId, false, ct));
    [HttpGet]
    public async Task<IActionResult> Pdf(Guid citaId, CancellationToken ct) => File(InformePdf.CrearCompartido(await agenda.ReporteAtencion(User, citaId, true, ct)), "application/pdf", $"reporte-compartido-{citaId:N}.pdf");
}
