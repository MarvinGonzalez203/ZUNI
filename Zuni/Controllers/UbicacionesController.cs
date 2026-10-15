using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zuni.Models.Agenda;
using Zuni.Services.Agenda;
namespace Zuni.Controllers;
[Authorize(Roles="Psicologo"),AgendaExceptionFilter,ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class UbicacionesController(AgendaCitasService agenda):Controller
{
    [HttpGet] public async Task<IActionResult> Editar(Guid citaId,CancellationToken ct)=>View(await agenda.PrepararLugar(User,citaId,ct));
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Editar(LugarCitaInput input,CancellationToken ct){var actual=await agenda.PrepararLugar(User,input.CitaId,ct);if(!ModelState.IsValid){input.Estudiante=actual.Estudiante;input.InicioUtc=actual.InicioUtc;input.Estado=actual.Estado;return View(input);}await agenda.GuardarLugar(User,input,ct);TempData["Success"]="Ubicación guardada y notificada al estudiante.";return RedirectToAction("Agenda","Psicologo",new{citaId=input.CitaId});}
}
