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
}
