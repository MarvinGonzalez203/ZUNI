using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models.Agenda;
using Zuni.Seguridad;
using Zuni.Services.Agenda;

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

    // --- Propuestas de cita de Dirección (docs/propuestas-director-privacidad.md).
    // El servicio valida rol, sesión y todas las reglas en el servidor; las vistas solo presentan.
    // Dirección propone; únicamente el psicólogo acepta, reprograma o rechaza.

    [HttpGet, AgendaExceptionFilter, EnableRateLimiting(PoliticasAcceso.PropuestasDirector)]
    public async Task<IActionResult> ProponerCita([FromServices] AgendaCitasService agenda, string? q, Guid? perfilId, CancellationToken ct)
    {
        await PrepararDashboardAsync();
        return View(await agenda.PrepararPropuesta(User, q, perfilId, null, ct));
    }

    [HttpPost, ValidateAntiForgeryToken, AgendaExceptionFilter, EnableRateLimiting(PoliticasAcceso.PropuestasDirector)]
    public async Task<IActionResult> ProponerCita([FromServices] AgendaCitasService agenda, [Bind(Prefix = "Input")] PropuestaDirectorInput input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await PrepararDashboardAsync();
            Response.StatusCode = StatusCodes.Status409Conflict;
            return View(await agenda.PrepararPropuesta(User, null, null, input, ct));
        }
        var id = await agenda.CrearPropuestaDirector(User, input, ct);
        TempData["Success"] = "Propuesta enviada. El psicólogo la revisará; consulta su estado en Mis propuestas.";
        return RedirectToAction(nameof(Propuesta), new { id });
    }

    [HttpGet, AgendaExceptionFilter]
    public async Task<IActionResult> Propuestas([FromServices] AgendaCitasService agenda, int pagina = 1, CancellationToken ct = default)
    {
        await PrepararDashboardAsync();
        return View(await agenda.MisPropuestas(User, pagina, ct));
    }

    [HttpGet("Director/Propuesta/{id:guid}"), AgendaExceptionFilter]
    public async Task<IActionResult> Propuesta([FromServices] AgendaCitasService agenda, Guid id, CancellationToken ct)
    {
        await PrepararDashboardAsync();
        return View(await agenda.DetallePropuesta(User, id, ct));
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
