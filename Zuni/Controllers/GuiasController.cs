using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Resources;
using Zuni.Services.Institucion;
using Zuni.Services.Privacidad;

namespace Zuni.Controllers;

[Authorize, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class GuiasController(ApplicationDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Asistente(CancellationToken ct)
    {
        string id;try{id=await PrivacidadService.UsuarioAsync(db,User,ct);}catch(UnauthorizedAccessException){return Forbid();}
        var rol=PreferenciasVistaService.Rol(User);if(rol is null||!await PreferenciasVistaService.Autoriza(db,User,rol,ct))return Forbid();
        return Json(new {product="zuni",scope=id+":"+rol,metricsEndpoint=Url.Action("Cifras","Guias"),topics=GuiasUso.Todas.Where(g=>g.Rol==rol||g.Rol=="General").Select((g,i)=>new {id=g.Tema+"-"+i,title=Zuni.Localizacion.Idioma.Texto(g.Titulo),description=Zuni.Localizacion.Idioma.Texto(g.Resumen),question=Zuni.Localizacion.Idioma.Texto(GuiasUso.Preguntas.FirstOrDefault(p=>p.Titulo==g.Titulo&&(p.Rol==rol||p.Rol=="General"))?.Pregunta??g.Titulo),aliases=g.Titulo+" "+g.Tema+" "+g.Donde,steps=g.Pasos.Select(p=>Zuni.Localizacion.Idioma.Texto(p)).ToArray(),href=RutaAyuda(AccesosGuias.Enlace(g,rol))}).ToArray()});
    }
    [HttpGet]
    public async Task<IActionResult> Cifras(string? desde,string? hasta,CancellationToken ct)
    {
        string id;try{id=await PrivacidadService.UsuarioAsync(db,User,ct);}catch(UnauthorizedAccessException){return Forbid();}
        var rol=PreferenciasVistaService.Rol(User);if(rol is not ("Psicologo" or "Estudiante")||!await PreferenciasVistaService.Autoriza(db,User,rol,ct))return Forbid();
        DateOnly? a=null,b=null;
        if(desde is not null){if(!DateOnly.TryParseExact(desde,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out var f)||f.Year>=9998)return BadRequest();a=f;}
        if(hasta is not null){if(!DateOnly.TryParseExact(hasta,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out var f)||f.Year>=9998)return BadRequest();b=f;}
        if(a.HasValue&&b.HasValue&&a>b)return BadRequest();
        var citas=db.Citas.AsNoTracking();
        if(rol=="Psicologo")citas=citas.Where(c=>c.PsicologoUsuarioId==id);
        else {var perfiles=db.PerfilesEstudiante.Where(p=>p.UsuarioId==id).Select(p=>p.Id);citas=citas.Where(c=>perfiles.Contains(c.PerfilEstudianteId));}
        if(a is not null){var inicio=Zuni.Services.Agenda.AgendaReglas.Utc(a.Value,TimeOnly.MinValue);citas=citas.Where(c=>c.FechaHoraInicioUtc>=inicio);}
        if(b is not null){var fin=Zuni.Services.Agenda.AgendaReglas.Utc(b.Value.AddDays(1),TimeOnly.MinValue);citas=citas.Where(c=>c.FechaHoraInicioUtc<fin);}
        return Json(new {count=await citas.CountAsync(ct),includesCancelled=true,from=a?.ToString("yyyy-MM-dd"),to=b?.ToString("yyyy-MM-dd")});
    }
    private string RutaAyuda(string ruta)=>ruta.StartsWith('/')?Url.Content("~"+ruta):ruta;

    [HttpGet]
    public async Task<IActionResult> Index(string? q, CancellationToken ct)
    {
        string id;
        try { id = await PrivacidadService.UsuarioAsync(db, User, ct); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        var rol = PreferenciasVistaService.Rol(User);
        if (rol is null || !await PreferenciasVistaService.Autoriza(db, User, rol, ct)) return Forbid();
        if (rol == "SuperAdmin") return RedirectToAction("Guias", "SuperAdmin", new { q });
        var busqueda = (q ?? "").Trim();
        if (busqueda.Length > 80) busqueda = busqueda[..80];
        ViewData["Busqueda"] = busqueda;
        ViewData["RolGuias"] = rol;
        ViewData["DashboardRole"] = rol;
        ViewData["DashboardController"] = rol;
        ViewData["DashboardName"] = await db.Users.Where(u => u.Id == id).Select(u => u.FullName).SingleAsync(ct);
        return View("~/Views/Shared/GuiasPorRol.cshtml", GuiasUso.Buscar(busqueda, null).Where(g => g.Rol == rol || g.Rol == "General").ToArray());
    }
}
