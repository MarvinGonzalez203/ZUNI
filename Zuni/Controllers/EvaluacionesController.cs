using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Zuni.Services;
using Zuni.Models.Evaluaciones;
namespace Zuni.Controllers;
[Authorize(Roles="Estudiante")]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
[Route("Estudiante/Evaluaciones")]
public sealed class EvaluacionesController(EvaluacionesService service) : Controller
{
    private string Usuario=>User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detalle(Guid id,CancellationToken ct) { var a=await service.Consulta(Usuario).SingleOrDefaultAsync(a=>a.Id==id,ct);return a is null ? NotFound() : View(a); }
    [HttpPost("{id:guid}/iniciar"),ValidateAntiForgeryToken]
    public Task<IActionResult> Iniciar(Guid id,int revision,CancellationToken ct)=>Ejecutar(id,()=>service.Iniciar(id,Usuario,revision,ct),"Detalle");
    [HttpPost("{id:guid}/guardar"),ValidateAntiForgeryToken]
    public Task<IActionResult> Guardar(Guid id,int revision,[Bind(Prefix="respuestas")] Dictionary<string,int?> respuestas,bool revisar,CancellationToken ct)
    {
        if(!ModelState.IsValid || respuestas.Keys.Any(k=>!Guid.TryParse(k,out _))) { TempData["EvaluacionError"]="Hay respuestas inválidas. No se guardaron cambios.";return Task.FromResult<IActionResult>(RedirectToAction(nameof(Detalle),new{id})); }
        return Ejecutar(id,()=>service.Guardar(id,Usuario,revision,respuestas.ToDictionary(r=>Guid.Parse(r.Key),r=>r.Value),ct),revisar?"Revisar":"Detalle");
    }
    [HttpGet("{id:guid}/revisar")]
    public async Task<IActionResult> Revisar(Guid id,CancellationToken ct) { var a=await service.Consulta(Usuario).SingleOrDefaultAsync(a=>a.Id==id,ct); if(a is null)return NotFound();if(a.Estado!=EstadoEvaluacion.EnProceso)return RedirectToAction(nameof(Detalle),new{id});return View(a); }
    [HttpPost("{id:guid}/finalizar"),ValidateAntiForgeryToken]
    public Task<IActionResult> Finalizar(Guid id,int revision,bool confirmado,CancellationToken ct)=>Ejecutar(id,()=>service.Finalizar(id,Usuario,revision,confirmado,ct),"Detalle");
    private async Task<IActionResult> Ejecutar(Guid id,Func<Task> accion,string destino)
    {
        try { await accion();TempData["Success"]="Cambios guardados correctamente.";return RedirectToAction(destino,new{id}); }
        catch(EvaluacionOperacionException e) { TempData["EvaluacionError"]=e.Message;return RedirectToAction(nameof(Detalle),new{id}); }
        catch(DbUpdateException) { TempData["EvaluacionError"]="No se guardaron los cambios. Recarga e inténtalo nuevamente.";return RedirectToAction(nameof(Detalle),new{id}); }
    }
}
