using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Zuni.Controllers;

[Authorize(Roles = "Psicologo")]
public sealed class PsicologoController(Zuni.Services.BigFiveService bigFive, Zuni.Services.AgendaService agenda) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    [ResponseCache(NoStore=true, Location=ResponseCacheLocation.None)]
    public async Task<IActionResult> Agenda(DateOnly? mes,CancellationToken ct) => View(await agenda.Leer(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value,true,mes,ct));

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Disponibilidad(Zuni.Models.HorarioAgendaInput input,string operacion,Guid? horario,CancellationToken ct)
    {
        if(!ModelState.IsValid) TempData["AgendaError"]="Revisa la fecha y los valores del horario.";
        else try {
            await agenda.Cambiar(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value,input.Fecha,operacion,input,horario,ct);
            TempData["AgendaMensaje"]="Disponibilidad guardada. El estudiante podrá verla al finalizar Big Five.";
        } catch(Zuni.Services.EvaluacionOperacionException ex) {TempData["AgendaError"]=ex.Message;}
        return RedirectToAction(nameof(Agenda),new {mes=input.Fecha.ToString("yyyy-MM-dd")});
    }

    [HttpGet]
    public IActionResult Estudiantes() => View();

    [HttpGet]
    public async Task<IActionResult> Resultados(CancellationToken ct)
    {
        ViewData["PruebaLocal"] = bigFive.PruebaLocal;
        if (bigFive.PruebaLocal && (HttpContext.Connection.RemoteIpAddress is not { } address || !System.Net.IPAddress.IsLoopback(address))) return StatusCode(403);
        if (!bigFive.Disponible) return View(Array.Empty<Zuni.Models.Evaluaciones.ResumenBigFive>());
        return View(await bigFive.Resumenes(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value, ct));
    }

    [HttpGet]
    public IActionResult Atencion() => View();

    [HttpGet]
    public IActionResult Historial() => View();
}
