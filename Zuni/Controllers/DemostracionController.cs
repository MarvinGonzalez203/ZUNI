using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models.Pruebas;
using Zuni.Services;
using Zuni.Services.MayoriaEdad;

namespace Zuni.Controllers;

[Authorize(Roles="Estudiante"), AutoValidateAntiforgeryToken, ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class DemostracionController(ApplicationDbContext db, IWebHostEnvironment entorno) : Controller
{
    private async Task<Prueba?> Preparar(CancellationToken ct)
    {
        if (!await DemostracionAcceso.Habilitada(db, User.FindFirstValue(ClaimTypes.NameIdentifier), entorno.IsDevelopment(), ct) ||
            !await MayoriaEdadService.ActorAsync(db,User,"Estudiante",ct)) return null;
        var id = Guid.Parse("eeeeeeee-0001-0001-0000-000000000000");
        var prueba = await db.PruebasDirector.AsNoTracking().Include(p=>p.Preguntas).ThenInclude(q=>q.Opciones).AsSplitQuery().SingleOrDefaultAsync(p=>p.Id==id,ct);
        return prueba?.Preguntas.Count == 15 && prueba.Preguntas.All(q=>q.Opciones.Select(o=>o.Valor).Order().SequenceEqual(Enumerable.Range(1,10))) ? prueba : null;
    }
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var prueba=await Preparar(ct); return prueba is null ? NotFound() : View(new DemostracionViewModel { Prueba=prueba });
    }
    [HttpPost, RequestFormLimits(ValueCountLimit=100)]
    public async Task<IActionResult> Index(DemostracionViewModel model, CancellationToken ct)
    {
        var prueba=await Preparar(ct); if(prueba is null)return NotFound();
        model.Prueba=prueba;
        if(!model.Consentimiento) ModelState.AddModelError(nameof(model.Consentimiento),"Confirma que deseas realizar la demostración.");
        if(model.Respuestas.Count!=15 || prueba.Preguntas.Any(q=>!model.Respuestas.TryGetValue(q.Id,out var valor)||valor is null or <1 or >10))
            ModelState.AddModelError(nameof(model.Respuestas),"Responde las 15 preguntas con un valor del 1 al 10.");
        model.Concluida=ModelState.IsValid;
        // Recorrido exclusivamente visual: no escribe citas, inducciones, consentimientos ni respuestas clínicas.
        return View(model);
    }
}
