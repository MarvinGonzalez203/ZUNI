using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zuni.Models;
using Zuni.Services;
namespace Zuni.Controllers;

[Authorize(Roles="Estudiante,Psicologo")]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class CitasController(CitasService citas,BigFiveService bigFive):Controller
{
    private string Actor=>User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private bool Profesional=>User.IsInRole("Psicologo");
    private bool Permitido=>!bigFive.PruebaLocal||(HttpContext.Connection.RemoteIpAddress is {} ip&&System.Net.IPAddress.IsLoopback(ip));
    private IActionResult Volver(Guid? id=null)=>Profesional&&id is not null?RedirectToAction(nameof(Detalle),new{id}):RedirectToAction(Profesional?"Estudiantes":"Citas",Profesional?"Psicologo":"Estudiante");
    private async Task<bool> Ejecutar(Func<Task> accion)
    {
        if(!ModelState.IsValid){TempData["CitaError"]="Revisa los campos del formulario.";return false;}
        try{await accion();TempData["CitaMensaje"]="Cambio guardado correctamente.";return true;}
        catch(EvaluacionOperacionException ex){TempData["CitaError"]=ex.Message;}
        catch(DbUpdateConcurrencyException){TempData["CitaError"]="La cita cambió. Actualiza la página e inténtalo de nuevo.";}
        catch(DbUpdateException ex) when(ex.InnerException is Npgsql.PostgresException {SqlState:"23505"}){TempData["CitaError"]="El horario acaba de reservarse. Elige otro.";}
        return false;
    }
    [HttpPost,ValidateAntiForgeryToken,Authorize(Roles="Estudiante")]
    public async Task<IActionResult> Solicitar(SolicitudCitaInput input,CancellationToken ct)
    {
        if(!Permitido)return Forbid();await Ejecutar(async()=>{await citas.Solicitar(Actor,input,ct);});return Volver();
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Gestionar(Guid id,int revision,string accion,string? motivo,CancellationToken ct)
    {
        if(!Permitido)return Forbid();await Ejecutar(()=>citas.Gestionar(Actor,Profesional,id,revision,accion,motivo,ct));
        if(Profesional){var profile=await citas.PropiasProfesional(Actor).Where(c=>c.Id==id).Select(c=>(Guid?)c.Estudiante.PerfilEstudiante!.Id).SingleOrDefaultAsync(ct);return RedirectToAction("Estudiantes","Psicologo",new{estudiante=profile,seccion="citas"});}
        return Volver();
    }
    [HttpGet,Authorize(Roles="Psicologo")]
    public async Task<IActionResult> Detalle(Guid id,CancellationToken ct)
    {
        if(!Permitido)return Forbid();
        try{var model=await citas.Detalle(Actor,id,ct);return model is null?NotFound():View(model);}
        catch(EvaluacionOperacionException){return Forbid();}
    }
    [HttpPost,ValidateAntiForgeryToken,Authorize(Roles="Psicologo")]
    public async Task<IActionResult> Cerrar(CerrarAtencionInput input,CancellationToken ct)
    {
        if(!Permitido)return Forbid();
        if(await Ejecutar(()=>citas.Cerrar(Actor,input,ct)))return Volver(input.Id);
        try{
            var model=await citas.Detalle(Actor,input.Id,ct);if(model is null)return NotFound();
            ViewData["AtencionIntentada"]=input;return View("Detalle",model);
        }catch(EvaluacionOperacionException){return Forbid();}
    }
}
