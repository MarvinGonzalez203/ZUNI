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
        return View(await Zuni.Services.Institucion.CentrosComercialesService.Listar(db,id,1,HttpContext.RequestServices.GetRequiredService<TimeProvider>(),ct));
    }
    [HttpGet] public async Task<IActionResult> Centros(int pagina=1,CancellationToken ct=default)
    {
        try{return View(await Zuni.Services.Institucion.CentrosComercialesService.Listar(db,await Zuni.Services.Privacidad.PrivacidadService.UsuarioAsync(db,User,ct),pagina,HttpContext.RequestServices.GetRequiredService<TimeProvider>(),ct));}
        catch(UnauthorizedAccessException){return Forbid();}
    }
    [HttpGet] public async Task<IActionResult> EditarCentro(Guid? id,bool ejemplo=false,CancellationToken ct=default)
    {
        if(!await Zuni.Services.Institucion.TarifasService.EsSuperAdmin(db,User.FindFirstValue(ClaimTypes.NameIdentifier),ct))return Forbid();
        if(id is null){
            var hoy=Zuni.Services.Agenda.AgendaReglas.Hoy(HttpContext.RequestServices.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime);
            return View(new Zuni.Models.Comercial.EditarCentroInput{Nombre=ejemplo?"Colegio Huellitas":"",EsDemostracion=ejemplo,Inicio=ejemplo?new DateOnly(2026,10,28):hoy,Fin=ejemplo?new DateOnly(2027,10,27):hoy.AddYears(1).AddDays(-1),ImporteGtq=(await Zuni.Services.Institucion.TarifasService.Obtener(db,ct)).Anual});
        }
        var c=await db.CentrosComerciales.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id,ct);if(c is null)return NotFound();
        return View(new Zuni.Models.Comercial.EditarCentroInput{Id=c.Id,Revision=c.Revision,Nombre=c.Nombre,CorreoContacto=c.CorreoContacto,MesesPlan=c.MesesPlan,Inicio=c.Inicio,Fin=c.Fin,Estado=c.Estado,ImporteGtq=c.ImporteGtq,EsDemostracion=c.EsDemostracion,NotaComercial=c.NotaComercial});
    }
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> EditarCentro(Zuni.Models.Comercial.EditarCentroInput input,CancellationToken ct)
    {
        if(!await Zuni.Services.Institucion.TarifasService.EsSuperAdmin(db,User.FindFirstValue(ClaimTypes.NameIdentifier),ct))return Forbid();
        if(!ModelState.IsValid)return View(input);
        try{await Zuni.Services.Institucion.CentrosComercialesService.Guardar(db,User,input,HttpContext.RequestServices.GetRequiredService<TimeProvider>(),ct);}
        catch(UnauthorizedAccessException){return Forbid();}
        catch(Zuni.Services.Privacidad.DatosOperacionException ex){ModelState.AddModelError("",ex.Message);return View(input);}
        catch(Exception ex)when(ex is DbUpdateException or Npgsql.NpgsqlException){Response.StatusCode=409;ModelState.AddModelError("","El contrato cambió o no se pudo guardar. Vuelve a abrirlo antes de editar.");return View(input);}
        TempData["Success"]="Contrato comercial guardado. No se realizaron cobros automáticos.";return RedirectToAction(nameof(Centros));
    }
    [HttpGet] public async Task<IActionResult> Guias(string? q,string? rol,CancellationToken ct)
    {
        if(!await Zuni.Services.Institucion.TarifasService.EsSuperAdmin(db,User.FindFirstValue(ClaimTypes.NameIdentifier),ct))return Forbid();
        ViewData["Busqueda"]=(q??"").Trim()[..Math.Min((q??"").Trim().Length,80)];ViewData["FiltroRol"]=rol;
        return View(Zuni.Resources.GuiasUso.Buscar((string)ViewData["Busqueda"]!,rol));
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
