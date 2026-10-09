using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Zuni.Services;
using Zuni.Models.Evaluaciones;
namespace Zuni.Controllers;
public sealed partial class AdministradorController
{
    [HttpGet]
    public async Task<IActionResult> Evaluaciones(CancellationToken ct)=>View(await _db.Set<AsignacionEvaluacion>().AsNoTracking().Where(a=>a.EvaluacionId!=Ipip50.EvaluacionId).Include(a=>a.Evaluacion).Include(a=>a.Estudiante).Include(a=>a.Resultado).OrderBy(a=>a.Estudiante.FullName).ThenBy(a=>a.Evaluacion.Titulo).ToListAsync(ct));
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> GestionarEvaluacion(Guid id,int revision,string operacion,[FromServices] EvaluacionesService servicio,CancellationToken ct)
    {
        try { await servicio.Administrar(id,User.FindFirstValue(ClaimTypes.NameIdentifier)!,revision,operacion,ct);TempData["Success"]="Evaluación actualizada."; }
        catch(EvaluacionOperacionException e) { TempData["Error"]=e.Message; }
        catch(DbUpdateException) { TempData["Error"]="No se guardó el cambio. Recarga e inténtalo nuevamente."; }
        return RedirectToAction(nameof(Evaluaciones));
    }
}
