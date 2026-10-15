using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
namespace Zuni.Controllers;
[Authorize(Roles="SuperAdmin"),ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class SuperAdminController(ApplicationDbContext db):Controller
{
    [HttpGet] public async Task<IActionResult> Index(CancellationToken ct)
    {
        var id=User.FindFirstValue(ClaimTypes.NameIdentifier);
        if(!await Zuni.Services.Institucion.TarifasService.EsSuperAdmin(db,id,ct))return Forbid();
        ViewData["DashboardName"]=await db.Users.Where(u=>u.Id==id).Select(u=>u.FullName).SingleAsync(ct);
        return View();
    }
    [HttpGet] public async Task<IActionResult> Apariencia(CancellationToken ct)
    {
        if(!await Zuni.Services.Institucion.TarifasService.EsSuperAdmin(db,User.FindFirstValue(ClaimTypes.NameIdentifier),ct))return Forbid();
        var input=await Zuni.Services.Institucion.PreferenciasVistaService.Obtener(db,User.FindFirstValue(ClaimTypes.NameIdentifier)!,"SuperAdmin",(await Zuni.Services.Institucion.TarifasService.Apariencia(db,ct)).ColorPrincipal,ct);
        return View("~/Views/Preferencias/Colores.cshtml",input);
    }
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Apariencia(Zuni.Models.Institucion.ColorPersonalInput input,CancellationToken ct)
    {
        var id=User.FindFirstValue(ClaimTypes.NameIdentifier);
        if(!await Zuni.Services.Institucion.TarifasService.EsSuperAdmin(db,id,ct))return Forbid();
        input.Rol="SuperAdmin";if(!ModelState.IsValid)return View("~/Views/Preferencias/Colores.cshtml",input);
        try{await Zuni.Services.Institucion.PreferenciasVistaService.Guardar(db,User,input,ct);}
        catch(UnauthorizedAccessException){return Forbid();}
        catch(Exception ex)when(ex is DbUpdateException or Npgsql.NpgsqlException){Response.StatusCode=409;ModelState.AddModelError("","La configuración cambió. Recarga esta página antes de guardar.");return View("~/Views/Preferencias/Colores.cshtml",input);}
        TempData["Success"]="Colores del panel comercial actualizados.";return RedirectToAction(nameof(Apariencia));
    }
}
