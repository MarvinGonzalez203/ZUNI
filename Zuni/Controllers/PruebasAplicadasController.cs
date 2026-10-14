using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zuni.Data;
using Zuni.Models.Pruebas;
using Zuni.Services;
using Zuni.Services.Agenda;
namespace Zuni.Controllers;
[Authorize,AutoValidateAntiforgeryToken,AgendaExceptionFilter,ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class PruebasAplicadasController(ApplicationDbContext db,IAutorizacionClinicaService autorizacion,TimeProvider reloj):Controller
{
    private readonly PruebasAplicadasService service=new(db,autorizacion,reloj);
    [HttpGet,Authorize(Roles="Estudiante")] public async Task<IActionResult> Index(CancellationToken ct)=>View(await service.MisPruebas(User,ct));
    [HttpGet,Authorize(Roles="Psicologo")] public async Task<IActionResult> Habilitar(CancellationToken ct)=>View(await service.Preparar(User,ct));
    [HttpPost,Authorize(Roles="Psicologo")] public async Task<IActionResult> Habilitar(HabilitarPruebaInput input,CancellationToken ct) {
        if(!ModelState.IsValid)return View(await service.Preparar(User,ct));
        await service.Habilitar(User,input,ct);TempData["Success"]="Prueba indicada e inducción registrada. El estudiante ya puede responderla voluntariamente.";return RedirectToAction(nameof(Resultados));
    }
    [HttpGet,Authorize(Roles="Estudiante")] public async Task<IActionResult> Contestar(Guid id,CancellationToken ct)=>View(await service.Cuestionario(User,id,ct));
    [HttpPost,Authorize(Roles="Estudiante"),RequestFormLimits(ValueCountLimit=1500)] public async Task<IActionResult> Contestar(ContestarPruebaInput input,CancellationToken ct) {
        if(!ModelState.IsValid)throw new AgendaOperacionException("Revisa las respuestas enviadas.");
        await service.Responder(User,input,ct);TempData["Success"]=input.Finalizar?"Respuestas finalizadas. Tu psicólogo podrá consultar el resumen exploratorio.":"Avance guardado.";return RedirectToAction(input.Finalizar?nameof(Index):nameof(Contestar),new{id=input.Id});
    }
    [HttpPost,Authorize(Roles="Estudiante")] public async Task<IActionResult> Retirar(Guid id,CancellationToken ct){await service.Retirar(User,id,ct);TempData["Success"]="Participación retirada. El resumen deja de estar disponible en este módulo.";return RedirectToAction(nameof(Index));}
    [HttpGet,Authorize(Roles="Psicologo")] public async Task<IActionResult> Resultados(CancellationToken ct)=>View(await service.ListarResultados(User,ct));
    [HttpGet,Authorize(Roles="Psicologo")] public async Task<IActionResult> Resumen(Guid id,CancellationToken ct)=>View(await service.Resumen(User,id,ct));
}
