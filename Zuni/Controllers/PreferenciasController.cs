using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models.Institucion;
using Zuni.Services.Institucion;
namespace Zuni.Controllers;
[Authorize,ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class PreferenciasController(ApplicationDbContext db):Controller
{
    private void Preparar(string rol){ViewData["PreferenciaRol"]=rol;ViewData["DashboardRole"]=rol;ViewData["DashboardController"]=rol;ViewData["DashboardNavigation"]=$"~/Views/{rol}/_Navegacion.cshtml";}
    [HttpGet] public async Task<IActionResult> Colores(string? rol,CancellationToken ct){
        var actual=PreferenciasVistaService.Rol(User,rol);if(actual is null||!await PreferenciasVistaService.Autoriza(db,User,actual,ct))return Forbid();
        Preparar(actual);var defecto=actual=="SuperAdmin"?(await TarifasService.Apariencia(db,ct)).ColorPrincipal:(await ConfiguracionInstitucionService.ObtenerAsync(db,ct)).ColorPrincipal;
        return View(await PreferenciasVistaService.Obtener(db,User.FindFirstValue(ClaimTypes.NameIdentifier)!,actual,defecto,ct));
    }
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Colores(ColorPersonalInput input,CancellationToken ct){
        if(!await PreferenciasVistaService.Autoriza(db,User,input.Rol,ct))return Forbid();Preparar(input.Rol);
        if(!ModelState.IsValid)return View(input);
        try{await PreferenciasVistaService.Guardar(db,User,input,ct);}
        catch(UnauthorizedAccessException){return Forbid();}
        catch(Exception ex)when(ex is DbUpdateException or Npgsql.NpgsqlException){Response.StatusCode=409;ModelState.AddModelError("","La configuración cambió. Recarga esta página antes de guardar.");return View(input);}
        TempData["Success"]="Preferencia personal guardada. No cambia los colores de otras cuentas.";return RedirectToAction(nameof(Colores),new{rol=input.Rol});
    }
}
