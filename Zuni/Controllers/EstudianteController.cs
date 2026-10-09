using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models;

namespace Zuni.Controllers;

[Authorize(Roles = "Estudiante")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("Estudiante")]
public sealed class EstudianteController(ApplicationDbContext db) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var usuario = await ObtenerUsuarioAsync(cancellationToken);
        if (usuario is null) return StatusCode(StatusCodes.Status403Forbidden);

        return View(new EstudianteDashboardViewModel
        {
            Nombre = usuario.FullName,
            PerfilCompleto = usuario.PerfilEstudiante?.EstaCompleto == true,
            DatosPerfil = CrearPerfilViewModel(usuario)
        });
    }

    [HttpGet("MiPerfil")]
    public IActionResult MiPerfil() => RedirectToAction("Index", "MiCuenta");

    [HttpGet("Evaluaciones")]
    public async Task<IActionResult> Evaluaciones(CancellationToken ct) => View(await db.Set<Zuni.Models.Evaluaciones.AsignacionEvaluacion>().AsNoTracking().AsSplitQuery().Where(a=>a.EstudianteId==User.FindFirstValue(ClaimTypes.NameIdentifier)).Include(a=>a.Evaluacion).ThenInclude(e=>e.Preguntas).Include(a=>a.Respuestas).OrderBy(a=>a.FechaAsignacionUtc).ToListAsync(ct));

    [HttpGet("Resultados")]
    public async Task<IActionResult> Resultados(CancellationToken ct)
    {
        var usuarioId=User.FindFirstValue(ClaimTypes.NameIdentifier);
        return View(await db.Set<Zuni.Models.Evaluaciones.AsignacionEvaluacion>().AsNoTracking()
            .Where(a=>a.EstudianteId==usuarioId && a.Estado==Zuni.Models.Evaluaciones.EstadoEvaluacion.Finalizada)
            .Select(a=>new Zuni.Models.Evaluaciones.ResultadoEstudianteViewModel { AsignacionId=a.Id,Titulo=a.Evaluacion.Titulo,EsDemostracion=a.Evaluacion.EsDemostracion,FechaFinalizacionUtc=a.FechaFinalizacionUtc,Publicado=a.Resultado!=null && a.Resultado.Publicado,Puntuacion=a.Resultado!=null && a.Resultado.Publicado ? a.Resultado.Puntuacion : null,Observaciones=a.Resultado!=null && a.Resultado.Publicado ? a.Resultado.ObservacionesPublicables : null }).ToListAsync(ct));
    }

    [HttpGet("Citas")]
    public IActionResult Citas() => View();

    private static MiPerfilViewModel CrearPerfilViewModel(ApplicationUser usuario)
    {
        var perfil = usuario.PerfilEstudiante;
        return new MiPerfilViewModel
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
        };
    }

    private Task<ApplicationUser?> ObtenerUsuarioAsync(CancellationToken cancellationToken)
    {
        // La identidad procede exclusivamente de la sesión, nunca de parámetros de la URL.
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return db.Users.AsNoTracking().Include(user => user.PerfilEstudiante)
            .SingleOrDefaultAsync(user => user.Id == usuarioId && user.IsActive, cancellationToken);
    }
}
