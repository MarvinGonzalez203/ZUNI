using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zuni.Models.Agenda;
using Zuni.Services.Agenda;
namespace Zuni.Controllers;
[Authorize,AgendaExceptionFilter,ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class CitasProgramadasController(AgendaCitasService agenda):Controller
{
    [Authorize(Roles="Psicologo,Director"),HttpGet]
    public async Task<IActionResult> Index(DateOnly? fecha,string? psicologoId,Guid? citaId,CancellationToken ct)
    {
        if(!ModelState.IsValid)throw new AgendaOperacionException("Revisa la fecha del calendario.");
        var model=await agenda.ListarProgramacion(User,ct);
        if(User.IsInRole("Director")){
            if(citaId is {}id){var sitio=await agenda.UbicarCita(User,id,ct);fecha=sitio.Fecha;psicologoId=sitio.PsicologoId;}
            var calendario=await agenda.CalendarioDirector(User,psicologoId,fecha,ct);calendario.Calendario.CitaSeleccionada=citaId;
            model=model with {CalendarioDirector=calendario};
        }
        return View(model);
    }
    [Authorize(Roles="Psicologo,Director"),HttpGet]public async Task<IActionResult> Editar(Guid? citaId,DateOnly? fecha,string? psicologoId,CancellationToken ct)=>View(await agenda.PrepararProgramacion(User,citaId,fecha,ct,psicologoId));
    [Authorize(Roles="Psicologo,Director"),HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(ProgramarCitaInput input,CancellationToken ct){var actual=await agenda.PrepararProgramacion(User,input.CitaId,input.Fecha,ct);if(!ModelState.IsValid){input.Estudiantes=actual.Estudiantes;return View("Editar",input);}await agenda.GuardarProgramacion(User,input,ct);TempData["Success"]=input.CitaId is null?"Cita programada y notificación guardada para el estudiante.":"Cita reprogramada. El estudiante recibió una notificación con la nueva fecha.";return RedirectToAction(nameof(Index));}
    [HttpGet]public async Task<IActionResult> Notificaciones(CancellationToken ct)=>View(await agenda.Notificaciones(User,ct));
    [HttpPost,ValidateAntiForgeryToken]public async Task<IActionResult> Leida(Guid id,CancellationToken ct){await agenda.LeerNotificacion(User,id,ct);return RedirectToAction(nameof(Notificaciones));}
}
