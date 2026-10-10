using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models;
using Zuni.Models.Atencion;
using Zuni.Services;

namespace Zuni.Controllers;

[Authorize(Roles = "Estudiante")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("Estudiante")]
public sealed class EstudianteController(ApplicationDbContext db, IAsignacionPsicologoService asignacion) : Controller
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
            DatosPerfil = CrearPerfilViewModel(usuario),
            EstadoAtencion = await db.SolicitudesAtencion.AsNoTracking()
                .Where(s => db.PerfilesEstudiante.Any(p => p.Id == s.PerfilEstudianteId && p.UsuarioId == usuario.Id) &&
                    (s.Estado == EstadoSolicitudAtencion.Pendiente || s.Estado == EstadoSolicitudAtencion.Asignada))
                .Select(s => (EstadoSolicitudAtencion?)s.Estado).SingleOrDefaultAsync(cancellationToken)
        });
    }


    [HttpGet("SolicitarAtencion")]
    public async Task<IActionResult> SolicitarAtencion(CancellationToken ct)
    {
        var perfil = await PerfilSolicitanteAsync(ct);
        if (perfil is null) return PerfilRequerido();
        if (await TieneSolicitudActivaAsync(perfil.Id, ct)) return SolicitudExistente();
        return View(new SolicitarAtencionViewModel());
    }

    [HttpPost("SolicitarAtencion")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SolicitarAtencion(SolicitarAtencionViewModel model, CancellationToken ct)
    {
        Guid perfilId;
        await using (var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct))
        {
            var perfil = await PerfilSolicitanteAsync(ct);
            if (perfil is null) return PerfilRequerido();
            perfilId = perfil.Id;
            if (await TieneSolicitudActivaAsync(perfilId, ct)) return SolicitudExistente();
            if (!ModelState.IsValid)
            {
                // El formulario siempre tiene tres posiciones, nunca se renderiza una lista arbitraria del cliente.
                model.Contactos = (model.Contactos ?? []).Take(3).Select(c => c ?? new()).ToList();
                while (model.Contactos.Count < 3) model.Contactos.Add(new());
                return View(model);
            }
            var now = DateTime.UtcNow;
            var solicitud = new SolicitudAtencion
            {
                PerfilEstudianteId = perfilId, FechaSolicitudUtc = now,
                TipoIngreso = model.TipoIngreso,
                // El formulario captura referencia declarada, no identifica cuentas ajenas.
                UsuarioReferenteId = null
            };
            var expediente = new ExpedienteInicial
            {
                SolicitudAtencionId = solicitud.Id, Edad = model.Edad, EsMayorEdad = model.EsMayorEdad,
                Sexo = OpcionalAtencion(model.Sexo), Direccion = model.Direccion.Trim(),
                IdiomaPreferido = model.IdiomaPreferido.Trim(), MotivoConsulta = model.MotivoConsulta.Trim(),
                NombreReferente = model.TipoIngreso == TipoIngresoAtencion.Referencia ? OpcionalAtencion(model.NombreReferente) : null,
                MotivoReferencia = model.TipoIngreso == TipoIngresoAtencion.Referencia ? OpcionalAtencion(model.MotivoReferencia) : null,
                ConsideracionesAtencion = OpcionalAtencion(model.ConsideracionesAtencion), FechaCreacionUtc = now
            };
            db.SolicitudesAtencion.Add(solicitud);
            db.ConsentimientosAtencion.Add(new ConsentimientoAtencion
            {
                SolicitudAtencionId = solicitud.Id, Aceptado = true,
                VersionConsentimiento = Zuni.Resources.ConsentimientoInformado.Version, FechaRespuestaUtc = now
            });
            db.ExpedientesIniciales.Add(expediente);
            var orden = 0;
            foreach (var c in model.Contactos.Where(c => !c.Vacio))
                db.ContactosEmergenciaExpediente.Add(new ContactoEmergenciaExpediente
                {
                    ExpedienteInicialId = expediente.Id, Nombre = c.Nombre!.Trim(),
                    Relacion = c.Relacion!.Trim(), Telefono = c.Telefono!.Trim(), Orden = ++orden
                });
            try
            {
                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch (Exception ex) when (ex is DbUpdateException or Npgsql.NpgsqlException)
            {
                await tx.RollbackAsync(CancellationToken.None);
                db.ChangeTracker.Clear();
                // No incluir la excepción: puede contener datos del expediente.
                TempData["Error"] = "No se pudo confirmar el envío. Revisa el estado de tu solicitud antes de volver a intentarlo.";
                return RedirectToAction(nameof(Index));
            }
        }

        try
        {
            var resultado = await asignacion.IntentarAsignarAsync(perfilId, ct);
            TempData["Success"] = resultado is ResultadoAsignacion.Asignado or ResultadoAsignacion.YaAsignado
                ? "Tu solicitud fue guardada y está asignada."
                : "Tu solicitud fue guardada y está pendiente de asignación o revisión.";
        }
        catch (Exception ex) when (ex is DbUpdateException or Npgsql.NpgsqlException)
        {
            // La solicitud ya está confirmada: nunca se borra por un fallo de asignación.
            TempData["Success"] = "Tu solicitud fue guardada. La asignación está pendiente de revisión.";
        }
        return RedirectToAction(nameof(Index));
    }

    private Task<PerfilEstudiante?> PerfilSolicitanteAsync(CancellationToken ct)
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var stamp = User.FindFirstValue("Zuni.SecurityStamp");
        return db.PerfilesEstudiante.AsNoTracking().SingleOrDefaultAsync(p => p.UsuarioId == id && p.Activo &&
            p.Usuario.IsActive && !p.Usuario.DebeCambiarContrasena && p.Usuario.SecurityStamp == stamp &&
            db.UserRoles.Any(ur => ur.UserId == id && db.Roles.Any(r => r.Id == ur.RoleId && r.NormalizedName == "ESTUDIANTE")), ct);
    }

    private Task<bool> TieneSolicitudActivaAsync(Guid perfilId, CancellationToken ct) =>
        db.SolicitudesAtencion.AsNoTracking().AnyAsync(s => s.PerfilEstudianteId == perfilId &&
            (s.Estado == EstadoSolicitudAtencion.Pendiente || s.Estado == EstadoSolicitudAtencion.Asignada), ct);

    private IActionResult PerfilRequerido()
    {
        TempData["Error"] = "Verifica tus datos de estudiante en Mi cuenta antes de solicitar atención.";
        return RedirectToAction("Editar", "MiCuenta");
    }

    private IActionResult SolicitudExistente()
    {
        TempData["Info"] = "Ya tienes una solicitud activa. No se creó otra solicitud.";
        return RedirectToAction(nameof(Index));
    }

    private static string? OpcionalAtencion(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    [HttpGet("Evaluaciones")]
    public IActionResult Evaluaciones() => View();

    [HttpGet("Resultados")]
    public IActionResult Resultados() => View();

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
