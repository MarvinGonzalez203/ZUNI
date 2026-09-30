using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models;

namespace Zuni.Controllers;

[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("Estudiante")]
public sealed class EstudianteController(ApplicationDbContext db) : Controller
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // La sesión la exige Authorize; el rol se valida aquí para devolver
        // 403 directamente, sin depender de una página de acceso denegado.
        if (!User.IsInRole("Estudiante"))
            context.Result = StatusCode(StatusCodes.Status403Forbidden);

        base.OnActionExecuting(context);
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var usuario = await ObtenerUsuarioAsync(cancellationToken);
        if (usuario is null) return StatusCode(StatusCodes.Status403Forbidden);

        return View(new EstudianteDashboardViewModel
        {
            Nombre = usuario.FullName,
            PerfilCompleto = usuario.PerfilEstudiante?.EstaCompleto == true
        });
    }

    [HttpGet("MiPerfil")]
    public async Task<IActionResult> MiPerfil(CancellationToken cancellationToken)
    {
        var usuario = await ObtenerUsuarioAsync(cancellationToken);
        if (usuario is null) return StatusCode(StatusCodes.Status403Forbidden);

        var perfil = usuario.PerfilEstudiante;
        return View(new MiPerfilViewModel
        {
            Nombre = usuario.FullName,
            Email = usuario.Email,
            Carne = perfil?.Carne,
            Telefono = perfil?.Telefono,
            Carrera = perfil?.Carrera,
            Semestre = perfil?.Semestre,
            CicloAcademico = perfil?.CicloAcademico,
            NombreContactoEmergencia = perfil?.NombreContactoEmergencia,
            TelefonoContactoEmergencia = perfil?.TelefonoContactoEmergencia,
            RelacionContactoEmergencia = perfil?.RelacionContactoEmergencia,
            Completo = perfil?.EstaCompleto == true
        });
    }

    [HttpGet("SolicitarCita")]
    public IActionResult SolicitarCita() => View(CrearSolicitudCitaPreview());

    [HttpPost("SolicitarCita")]
    [ValidateAntiForgeryToken]
    public IActionResult SolicitarCita(SolicitudCitaPreviewViewModel model)
    {
        var hoy = DateTime.Today;
        if (model.FechaPreferida.HasValue &&
            (model.FechaPreferida.Value.Date < hoy || model.FechaPreferida.Value.Date > hoy.AddDays(30)))
        {
            ModelState.AddModelError(
                nameof(model.FechaPreferida),
                "Elige una fecha entre hoy y los próximos 30 días.");
        }

        if (!Enum.IsDefined(typeof(ModalidadCitaPreview), model.Modalidad))
            ModelState.AddModelError(nameof(model.Modalidad), "Selecciona una modalidad válida.");

        var franjasValidas = CrearSolicitudCitaPreview().FranjasHorarias
            .Any(franja => franja.Value == model.FranjaHoraria);
        if (!franjasValidas)
            ModelState.AddModelError(nameof(model.FranjaHoraria), "Selecciona uno de los horarios de ejemplo.");

        if (!ModelState.IsValid)
        {
            model.FranjasHorarias = CrearSolicitudCitaPreview().FranjasHorarias;
            return View(model);
        }

        TempData["PreviewSuccess"] = "La interacción funcionó, pero esta solicitud es solo una vista previa: todavía no se envió ni reservó una cita.";
        return RedirectToAction(nameof(SolicitarCita));
    }

    [HttpGet("Seguimiento")]
    public IActionResult Seguimiento()
    {
        return View(new SeguimientoEstudiantePreviewViewModel
        {
            Cuestionarios =
            [
                new SeguimientoCuestionarioPreviewItem
                {
                    Nombre = "Cuestionario exploratorio de ejemplo",
                    Estado = "En progreso · ejemplo visual",
                    Modalidad = ModalidadPrueba.Media,
                    PreguntasRespondidas = 12,
                    PreguntasTotales = 40
                }
            ]
        });
    }

    [HttpGet("Bienestar")]
    public IActionResult Bienestar() => View();

    private static SolicitudCitaPreviewViewModel CrearSolicitudCitaPreview() => new()
    {
        FranjasHorarias =
        [
            new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem("09:00–11:00 · ejemplo", "09:00-11:00"),
            new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem("11:00–13:00 · ejemplo", "11:00-13:00"),
            new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem("14:00–16:00 · ejemplo", "14:00-16:00")
        ]
    };

    private Task<ApplicationUser?> ObtenerUsuarioAsync(CancellationToken cancellationToken)
    {
        // La identidad procede exclusivamente de la sesión, nunca de parámetros de la URL.
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return db.Users.AsNoTracking().Include(user => user.PerfilEstudiante)
            .SingleOrDefaultAsync(user => user.Id == usuarioId && user.IsActive, cancellationToken);
    }
}
