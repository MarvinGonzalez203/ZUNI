using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models.Evaluaciones;
using Zuni.Services;
namespace Zuni.Controllers;

[Authorize(Roles="Estudiante")]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
[Route("Estudiante/BigFive")]
public sealed class BigFiveController(BigFiveService service,EvaluacionesService evaluations,ApplicationDbContext db) : Controller
{
    private string Usuario=>User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    public override async Task OnActionExecutionAsync(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context,
        Microsoft.AspNetCore.Mvc.Filters.ActionExecutionDelegate next)
    {
        if(service.PruebaLocal && (HttpContext.Connection.RemoteIpAddress is not { } address || !System.Net.IPAddress.IsLoopback(address)))
        {context.Result=StatusCode(403);return;}
        await next();
    }
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Disponible"]=service.Disponible;ViewData["Consentimiento"]=service.TextoConsentimiento;
        ViewData["PruebaLocal"]=service.PruebaLocal;ViewData["Participacion"]=await service.Propia(Usuario,ct);
        if(service.Disponible)ViewData["Profesional"]=await service.NombreProfesional(Usuario,ct);
        return View(new ConsentimientoBigFiveInput());
    }
    [HttpPost("aceptar"),ValidateAntiForgeryToken]
    public async Task<IActionResult> Aceptar(ConsentimientoBigFiveInput input,CancellationToken ct)
    {
        if(!ModelState.IsValid){TempData["BigFiveError"]="Revisa el motivo y los datos del consentimiento.";return RedirectToAction(nameof(Index));}
        try {await service.Aceptar(Usuario,input,ct);return RedirectToAction(nameof(Cuestionario));}
        catch(EvaluacionOperacionException e){TempData["BigFiveError"]=e.Message;return RedirectToAction(nameof(Index));}
    }
    [HttpGet("cuestionario")]
    public async Task<IActionResult> Cuestionario(CancellationToken ct)
    {
        if(!service.Disponible)return RedirectToAction(nameof(Index));
        var participation=await service.Vigentes().AsNoTracking().SingleOrDefaultAsync(p=>p.Asignacion.EstudianteId==Usuario,ct);
        if(participation is null)return RedirectToAction(nameof(Index));
        var assignment=await db.Set<AsignacionEvaluacion>().AsNoTracking().Include(a=>a.Respuestas).SingleAsync(a=>a.Id==participation.AsignacionId,ct);
        return View(assignment);
    }
    [HttpPost("guardar"),ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(Guid id,int revision,[Bind(Prefix="respuestas")] Dictionary<string,int?> respuestas,CancellationToken ct)
    {
        if(!service.Disponible || !ModelState.IsValid || respuestas.Keys.Any(k=>!Guid.TryParse(k,out _)))return BadRequest();
        if(!await service.Vigentes().AnyAsync(p=>p.AsignacionId==id&&p.Asignacion.EstudianteId==Usuario,ct))return NotFound();
        try {await evaluations.Guardar(id,Usuario,revision,respuestas.ToDictionary(r=>Guid.Parse(r.Key),r=>r.Value),ct);TempData["BigFiveSuccess"]="Tu avance quedó guardado.";}
        catch(EvaluacionOperacionException e){TempData["BigFiveError"]=e.Message;}
        catch(DbUpdateException){TempData["BigFiveError"]="No se pudo guardar. Recarga y vuelve a intentarlo.";}
        return RedirectToAction(nameof(Cuestionario));
    }
    [HttpPost("finalizar"),ValidateAntiForgeryToken]
    public async Task<IActionResult> Finalizar(Guid id,int revision,bool confirmado,CancellationToken ct)
    {
        if(!ModelState.IsValid || !service.Disponible)return BadRequest();
        if(!await service.Vigentes().AnyAsync(p=>p.AsignacionId==id&&p.Asignacion.EstudianteId==Usuario,ct))return NotFound();
        try {await evaluations.Finalizar(id,Usuario,revision,confirmado,ct);}
        catch(EvaluacionOperacionException e){TempData["BigFiveError"]=e.Message;}
        catch(DbUpdateException){TempData["BigFiveError"]="No se pudo finalizar. Recarga y vuelve a intentarlo.";}
        return RedirectToAction(nameof(Cuestionario));
    }
    [HttpPost("retirar"),ValidateAntiForgeryToken]
    public async Task<IActionResult> Retirar(CancellationToken ct){await service.Retirar(Usuario,ct);return RedirectToAction(nameof(Index));}
}
