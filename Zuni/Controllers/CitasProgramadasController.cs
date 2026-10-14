using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zuni.Models.Agenda;
using Zuni.Services.Agenda;
namespace Zuni.Controllers;
[Authorize,AgendaExceptionFilter,ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class CitasProgramadasController(AgendaCitasService agenda):Controller
{
    [Authorize(Roles="Psicologo,Director"),HttpGet]public async Task<IActionResult> Index(CancellationToken ct)=>View(await agenda.ListarProgramacion(User,ct));
    [Authorize(Roles="Psicologo,Director"),HttpGet]public async Task<IActionResult> Editar(Guid? citaId,DateOnly? fecha,CancellationToken ct)=>View(await agenda.PrepararProgramacion(User,citaId,fecha,ct));
    [Authorize(Roles="Psicologo,Director"),HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(ProgramarCitaInput input,CancellationToken ct){var actual=await agenda.PrepararProgramacion(User,input.CitaId,input.Fecha,ct);if(!ModelState.IsValid){input.Estudiantes=actual.Estudiantes;return View("Editar",input);}await agenda.GuardarProgramacion(User,input,ct);TempData["Success"]=input.CitaId is null?"Cita programada y notificación guardada para el estudiante.":"Cita reprogramada. El estudiante recibió una notificación con la nueva fecha.";return RedirectToAction(nameof(Index));}
    [HttpGet]public async Task<IActionResult> Notificaciones(CancellationToken ct)=>View(await agenda.Notificaciones(User,ct));
    [HttpPost,ValidateAntiForgeryToken]public async Task<IActionResult> Leida(Guid id,CancellationToken ct){await agenda.LeerNotificacion(User,id,ct);return RedirectToAction(nameof(Notificaciones));}
}
