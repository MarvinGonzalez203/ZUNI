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
