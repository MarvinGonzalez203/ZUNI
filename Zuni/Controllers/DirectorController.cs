using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;

namespace Zuni.Controllers;

[Authorize(Roles = "Director")]
public sealed class DirectorController(ApplicationDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        await PrepararDashboardAsync();
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Carreras()
    {
        await PrepararDashboardAsync();
        return View();
    }

    [HttpGet, Zuni.Services.Agenda.AgendaExceptionFilter]
    public async Task<IActionResult> Calendario([FromServices] Zuni.Services.Agenda.AgendaCitasService agenda, string? psicologoId, DateOnly? fecha, CancellationToken ct)
    {
        await PrepararDashboardAsync();
        return View(await agenda.CalendarioDirector(User, psicologoId, fecha, ct));
    }

    [HttpGet]
    public async Task<IActionResult> AvanceEvaluaciones()
    {
        await PrepararDashboardAsync();
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ResultadosInstitucionales()
    {
        await PrepararDashboardAsync();
        return View();
    }

    // Indicadores institucionales agregados. No existe un listado que identifique a quién se atiende.
    [HttpGet]
    public async Task<IActionResult> Estadisticas([FromServices] TimeProvider reloj, CancellationToken ct)
    {
        if (!await Zuni.Services.MayoriaEdad.MayoriaEdadService.ActorAsync(db, User, "Director", ct)) return Forbid();
        await PrepararDashboardAsync();
        return View(await Zuni.Services.Reportes.EstadisticasDirector.CalcularAsync(db, reloj, ct));
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
