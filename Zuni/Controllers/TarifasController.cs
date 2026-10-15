using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zuni.Models.Institucion;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Services.Institucion;
namespace Zuni.Controllers;

[Authorize(Roles="Administrador,SuperAdmin"),ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class TarifasController(ApplicationDbContext db,TimeProvider reloj):Controller
{
    [HttpGet] public async Task<IActionResult> Index(CancellationToken ct)=>View(await TarifasService.Catalogo(db,ct));
    [Authorize(Roles="SuperAdmin"),HttpGet]
    public async Task<IActionResult> Editar(CancellationToken ct)
    {
        if(!await TarifasService.EsSuperAdmin(db,User.FindFirstValue(ClaimTypes.NameIdentifier),ct))return Forbid();
        return View(await TarifasService.Obtener(db,ct));
    }
    [Authorize(Roles="SuperAdmin"),HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(EditarTarifasInput input,CancellationToken ct)
    {
        var id=User.FindFirstValue(ClaimTypes.NameIdentifier);
        if(!await TarifasService.EsSuperAdmin(db,id,ct))return Forbid();
        if(!ModelState.IsValid)return View(input);
        try{await TarifasService.Guardar(db,input,id!,reloj,ct);}
        catch(UnauthorizedAccessException){return Forbid();}
        catch(Exception ex)when(ex is DbUpdateException or Npgsql.NpgsqlException){Response.StatusCode=409;ModelState.AddModelError("","Las tarifas cambiaron. Recarga el editor antes de guardar nuevamente.");return View(input);}
        TempData["Success"]="Tarifas actualizadas. Los administradores verán los nuevos precios.";return RedirectToAction(nameof(Index));
    }
}
