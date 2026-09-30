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

    private Task<ApplicationUser?> ObtenerUsuarioAsync(CancellationToken cancellationToken)
    {
        // La identidad procede exclusivamente de la sesión, nunca de parámetros de la URL.
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return db.Users.AsNoTracking().Include(user => user.PerfilEstudiante)
            .SingleOrDefaultAsync(user => user.Id == usuarioId && user.IsActive, cancellationToken);
    }
}
