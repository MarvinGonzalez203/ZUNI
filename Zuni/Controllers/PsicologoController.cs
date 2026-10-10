using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Zuni.Controllers;

[Authorize(Roles = "Psicologo")]
public sealed class PsicologoController(Zuni.Services.BigFiveService bigFive, Zuni.Services.AgendaService agenda, Zuni.Services.SeguimientoEstudiantesService seguimiento, Zuni.Services.CitasService citas) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    [ResponseCache(NoStore=true, Location=ResponseCacheLocation.None)]
    public async Task<IActionResult> Agenda(DateOnly? mes,CancellationToken ct)
    {
        if(bigFive.PruebaLocal && (HttpContext.Connection.RemoteIpAddress is not {} ip||!System.Net.IPAddress.IsLoopback(ip)))return Forbid();
        try{return View(await agenda.Leer(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value,true,mes,ct));}catch(Zuni.Services.EvaluacionOperacionException){return Forbid();}
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Disponibilidad(Zuni.Models.HorarioAgendaInput input,string operacion,Guid? horario,CancellationToken ct)
    {
        if(bigFive.PruebaLocal && (HttpContext.Connection.RemoteIpAddress is not {} ip||!System.Net.IPAddress.IsLoopback(ip)))return Forbid();
        if(!ModelState.IsValid) TempData["AgendaError"]="Revisa la fecha y los valores del horario.";
        else try {
            await agenda.Cambiar(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value,input.Fecha,operacion,input,horario,ct);
            TempData["AgendaMensaje"]="Disponibilidad guardada. El estudiante podrá verla al finalizar Big Five.";
        } catch(Zuni.Services.EvaluacionOperacionException ex) {TempData["AgendaError"]=ex.Message;}
        catch(Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException) {TempData["AgendaError"]="La disponibilidad cambió mientras guardabas. Revisa el calendario e inténtalo de nuevo.";}
        return RedirectToAction(nameof(Agenda),new {mes=input.Fecha.ToString("yyyy-MM-dd")});
    }

    [HttpGet]
    [ResponseCache(NoStore=true, Location=ResponseCacheLocation.None)]
    public async Task<IActionResult> Estudiantes(string? busqueda,string? filtro,int pagina=1,Guid? estudiante=null,CancellationToken ct=default)
    {
        ViewData["PruebaLocal"]=bigFive.PruebaLocal;
        if(bigFive.PruebaLocal && (HttpContext.Connection.RemoteIpAddress is not {} address || !System.Net.IPAddress.IsLoopback(address)))return StatusCode(403);
        var model=await seguimiento.Leer(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value,busqueda,filtro,pagina,estudiante,ct);
        if(estudiante is not null&&model.Seleccionado is null)return NotFound();
        return View(model);
    }

    [HttpGet]
    public IActionResult Resultados() => RedirectToAction(nameof(Estudiantes));

    [HttpGet]
    public IActionResult Atencion() => RedirectToAction(nameof(Estudiantes));

    [HttpGet]
    [ResponseCache(NoStore=true, Location=ResponseCacheLocation.None)]
    public async Task<IActionResult> Historial(string? busqueda,DateOnly? desde,DateOnly? hasta,string? modalidad,int pagina=1,CancellationToken ct=default)
    {
        if(bigFive.PruebaLocal && (HttpContext.Connection.RemoteIpAddress is not {} ip||!System.Net.IPAddress.IsLoopback(ip)))return Forbid();
        try{return View(await citas.Historial(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value,busqueda,desde,hasta,modalidad,pagina,ct));}catch(Zuni.Services.EvaluacionOperacionException){return Forbid();}
    }
}
