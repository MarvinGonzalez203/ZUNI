using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Zuni.Models;
using Zuni.Services;
using Zuni.Services.Agenda;
using Zuni.Services.Privacidad;
using Zuni.Seguridad;

namespace Zuni.Controllers;

[Authorize(Roles="Psicologo"),ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class PinProfesionalController(PinProfesionalService pins,AgendaCitasService agenda):Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        try{ViewData["PinConfigurado"]=await pins.Configurado(User,ct);return View(new ConfigurarPinInput());}
        catch(UnauthorizedAccessException){return Forbid();}
    }
    [HttpPost,ValidateAntiForgeryToken,EnableRateLimiting(PoliticasAcceso.PinProfesional)]
    public async Task<IActionResult> Configurar(ConfigurarPinInput input,CancellationToken ct)
    {
        try{
            if(!ModelState.IsValid){ViewData["PinConfigurado"]=await pins.Configurado(User,ct);return View("Index",new ConfigurarPinInput());}
            await pins.Configurar(User,input,ct);TempData["Success"]="PIN profesional configurado. Las notas se desbloquean únicamente al introducirlo.";return RedirectToAction(nameof(Index));
        }catch(UnauthorizedAccessException){return Forbid();}
        catch(Exception ex)when(ex is DatosOperacionException or DbUpdateException){
            ModelState.AddModelError("",ex is DatosOperacionException?ex.Message:"La configuración cambió. Vuelve a intentarlo.");
            ViewData["PinConfigurado"]=await pins.Configurado(User,ct);return View("Index",new ConfigurarPinInput());
        }
    }
    [HttpPost,ValidateAntiForgeryToken,EnableRateLimiting(PoliticasAcceso.Recuperacion)]
    public async Task<IActionResult> EnviarRecuperacion(CancellationToken ct)
    {
        try{await pins.SolicitarRecuperacion(User,ct);TempData["Success"]="Se solicitó el envío del enlace temporal al correo de tu cuenta. Revisa también el correo no deseado.";}
        catch(UnauthorizedAccessException){return Forbid();}
        catch(Exception ex)when(ex is DatosOperacionException or DbUpdateException){TempData["Error"]=ex is DatosOperacionException?ex.Message:"La solicitud cambió. Inténtalo de nuevo.";}
        return RedirectToAction(nameof(Index));
    }
    [AllowAnonymous,HttpGet]
    public IActionResult Recuperar()=>View(new RecuperarPinInput());
    [AllowAnonymous,HttpPost,ValidateAntiForgeryToken,EnableRateLimiting(PoliticasAcceso.Recuperacion)]
    public async Task<IActionResult> Recuperar(RecuperarPinInput input,CancellationToken ct)
    {
        if(!ModelState.IsValid)return View(input);
        try{await pins.Recuperar(input,ct);return View("Recuperado");}
        catch(Exception ex)when(ex is DatosOperacionException or DbUpdateException){ModelState.AddModelError("",ex is DatosOperacionException?ex.Message:"El enlace ya no está disponible. Solicita uno nuevo.");return View(input);}
    }
    [HttpPost,ValidateAntiForgeryToken,EnableRateLimiting(PoliticasAcceso.PinProfesional),AgendaExceptionFilter]
    public async Task<IActionResult> Desbloquear(Guid citaId,string? pin,CancellationToken ct)
    {
        // Verify current and historical ownership before accepting a PIN attempt for this record.
        await agenda.PrepararAtencion(User,citaId,ct);
        try{await pins.Verificar(User,pin??"",ct);}
        catch(UnauthorizedAccessException){return Forbid();}
        catch(Exception ex)when(ex is DatosOperacionException or DbUpdateException){return StatusCode(409,new {error=Zuni.Localizacion.Idioma.Texto(ex is DatosOperacionException?ex.Message:"La solicitud cambió. Inténtalo de nuevo.")});}
        var detalle=await agenda.ConsultarAtencion(User,citaId,ct,incluirPrivado:true);
        Response.Headers["X-Zuni-Private-Note"]="1";
        return PartialView("~/Views/Shared/_NotaPrivadaDesbloqueada.cshtml",detalle);
    }
}
