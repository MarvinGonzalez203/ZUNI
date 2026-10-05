using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;

namespace Zuni.Controllers;

[Authorize(Roles = "Director")]
public sealed class DirectorController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        await PrepararDashboardAsync();
        return View();
    }

    public async Task<IActionResult> Carreras()
    {
        await PrepararDashboardAsync();
        return View();
    }

    public async Task<IActionResult> AvanceEvaluaciones()
    {
        await PrepararDashboardAsync();
        return View();
    }

    public async Task<IActionResult> ResultadosInstitucionales()
    {
        await PrepararDashboardAsync();
        return View();
    }

    public async Task<IActionResult> Estadisticas()
    {
        await PrepararDashboardAsync();
        return View();
    }

    private async Task PrepararDashboardAsync()
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var nombre = string.IsNullOrWhiteSpace(usuarioId)
            ? null
            : await db.Users
                .AsNoTracking()
                .Where(usuario => usuario.Id == usuarioId)
                .Select(usuario => usuario.FullName)
                .SingleOrDefaultAsync();

        ViewData["DashboardName"] = string.IsNullOrWhiteSpace(nombre)
            ? "Director"
            : nombre;
    }
}
