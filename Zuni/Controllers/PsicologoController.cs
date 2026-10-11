using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models.Atencion;
using Zuni.Services;
using Zuni.Models.Agenda;
using Zuni.Services.Agenda;

namespace Zuni.Controllers;

[Authorize(Roles = "Psicologo")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PsicologoController(ApplicationDbContext db, IAutorizacionClinicaService autorizacion) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var id = await autorizacion.ObtenerPsicologoAutorizadoAsync(User, ct);
        if (id is null) return Forbid();
        var perfiles = from a in db.AsignacionesEstudiantePsicologo.AsNoTracking()
            join p in db.PerfilesEstudiante.AsNoTracking() on a.PerfilEstudianteId equals p.Id
            join u in db.Users.AsNoTracking() on p.UsuarioId equals u.Id
            where a.PsicologoUsuarioId == id && a.FechaFinalizacionUtc == null && p.Activo && u.IsActive
            select p.Id;
        var solicitudes = db.SolicitudesAtencion.AsNoTracking()
            .Where(s => perfiles.Contains(s.PerfilEstudianteId));
        return View(new PsicologoDashboardViewModel
        {
            EstudiantesAsignados = await perfiles.Distinct().CountAsync(ct),
            SolicitudesAsignadas = await solicitudes.CountAsync(s => s.Estado == EstadoSolicitudAtencion.Asignada, ct),
            ExpedientesDisponibles = await solicitudes.CountAsync(s =>
                db.ConsentimientosAtencion.Any(c => c.SolicitudAtencionId == s.Id && c.Aceptado) &&
                db.ExpedientesIniciales.Any(e => e.SolicitudAtencionId == s.Id), ct)
        });
    }

    [HttpGet]
    [AgendaExceptionFilter]
    public async Task<IActionResult> Agenda([FromServices] AgendaCitasService agenda, DateOnly? fecha, CancellationToken ct)
    {
        if (!ModelState.IsValid) throw new AgendaOperacionException("Fecha no válida.");
        return View(await agenda.LeerAgenda(User, fecha, ct));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AgendaExceptionFilter]
    public async Task<IActionResult> AgregarDisponibilidad([FromServices] AgendaCitasService agenda, DisponibilidadInput input, CancellationToken ct)
    {
        if (!ModelState.IsValid) throw new AgendaOperacionException("Revisa los datos de disponibilidad.");
        await agenda.AgregarDisponibilidad(User, input, ct);
        TempData["Success"] = "Disponibilidad semanal guardada.";
        return RedirectToAction(nameof(Agenda));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AgendaExceptionFilter]
    public async Task<IActionResult> AgregarHorarioDia([FromServices] AgendaCitasService agenda, HorarioDiaInput input, CancellationToken ct)
    {
        if (!ModelState.IsValid) throw new AgendaOperacionException("Revisa la fecha y los datos del horario.");
        await agenda.AgregarHorarioDia(User, input, ct);
        TempData["Success"] = "Horario guardado para la fecha seleccionada.";
        return RedirectToAction(nameof(Agenda), new { fecha = input.Fecha?.ToString("yyyy-MM-dd") });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AgendaExceptionFilter]
    public async Task<IActionResult> CambiarDia([FromServices] AgendaCitasService agenda, DateOnly? fecha, string operacion, Guid? horarioId, CancellationToken ct)
    {
        if (!ModelState.IsValid || fecha is null) throw new AgendaOperacionException("Fecha u horario no válido.");
        await agenda.CambiarDia(User, fecha.Value, operacion, horarioId, ct);
        TempData["Success"] = "Disponibilidad del día actualizada.";
        return RedirectToAction(nameof(Agenda), new { fecha = fecha.Value.ToString("yyyy-MM-dd") });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AgendaExceptionFilter]
    public async Task<IActionResult> CambiarDisponibilidad([FromServices] AgendaCitasService agenda, Guid disponibilidadId, bool activo, CancellationToken ct)
    {
        if (!ModelState.IsValid) throw new AgendaOperacionException("Disponibilidad no válida.");
        await agenda.CambiarDisponibilidad(User, disponibilidadId, activo, ct);
        return RedirectToAction(nameof(Agenda));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AgendaExceptionFilter]
    public async Task<IActionResult> GestionarCita([FromServices] AgendaCitasService agenda, CambiarCitaInput input, CancellationToken ct)
    {
        if (!ModelState.IsValid) throw new AgendaOperacionException("Revisa el estado y el motivo administrativo.");
        await agenda.GestionarCita(User, input, ct);
        TempData["Success"] = "Estado de cita actualizado.";
        return RedirectToAction(nameof(Agenda));
    }


    [HttpGet]
    public async Task<IActionResult> Estudiantes(CancellationToken ct)
    {
        var id = await autorizacion.ObtenerPsicologoAutorizadoAsync(User, ct);
        if (id is null) return Forbid();
        var estudiantes = await (from a in db.AsignacionesEstudiantePsicologo.AsNoTracking()
            join p in db.PerfilesEstudiante.AsNoTracking() on a.PerfilEstudianteId equals p.Id
            join u in db.Users.AsNoTracking() on p.UsuarioId equals u.Id
            where a.PsicologoUsuarioId == id && a.FechaFinalizacionUtc == null && u.IsActive && p.Activo
            orderby u.FullName, p.Id
            select new EstudianteAsignadoViewModel
            {
                Nombre = u.FullName, Carne = p.Carne, Carrera = p.Carrera, FechaAsignacionUtc = a.FechaAsignacionUtc,
                SolicitudId = db.SolicitudesAtencion.Where(s => s.PerfilEstudianteId == p.Id &&
                    db.ExpedientesIniciales.Any(e => e.SolicitudAtencionId == s.Id) &&
                    db.ConsentimientosAtencion.Any(c => c.SolicitudAtencionId == s.Id && c.Aceptado))
                    .OrderByDescending(s => s.FechaSolicitudUtc).ThenBy(s => s.Id).Select(s => (Guid?)s.Id).FirstOrDefault()
            }).ToListAsync(ct);
        return View(estudiantes);
    }

    [HttpGet("Psicologo/Expediente/{solicitudId:guid}")]
    public async Task<IActionResult> Expediente(Guid solicitudId, CancellationToken ct)
    {
        // Autorización, lectura y auditoría comparten una transacción.
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        if (!await autorizacion.PuedeConsultarExpedienteAsync(User, solicitudId, ct)) return Forbid();
        var datos = await (from s in db.SolicitudesAtencion.AsNoTracking()
            join e in db.ExpedientesIniciales.AsNoTracking() on s.Id equals e.SolicitudAtencionId
            join p in db.PerfilesEstudiante.AsNoTracking() on s.PerfilEstudianteId equals p.Id
            join u in db.Users.AsNoTracking() on p.UsuarioId equals u.Id
            where s.Id == solicitudId
            select new
            {
                ExpedienteId = e.Id,
                Modelo = new ExpedienteConsultaViewModel
                {
                    Nombre = u.FullName, Carne = p.Carne, Carrera = p.Carrera,
                    FechaSolicitudUtc = s.FechaSolicitudUtc, TipoIngreso = s.TipoIngreso,
                    Edad = e.Edad, EsMayorEdad = e.EsMayorEdad, Sexo = e.Sexo, Direccion = e.Direccion,
                    IdiomaPreferido = e.IdiomaPreferido, MotivoConsulta = e.MotivoConsulta,
                    NombreReferente = e.NombreReferente, MotivoReferencia = e.MotivoReferencia,
                    ConsideracionesAtencion = e.ConsideracionesAtencion
                }
            }).SingleOrDefaultAsync(ct);
        if (datos is null) return Forbid(); // Misma respuesta para recurso ausente o ajeno.
        datos.Modelo.Contactos = await db.ContactosEmergenciaExpediente.AsNoTracking()
            .Where(c => c.ExpedienteInicialId == datos.ExpedienteId).OrderBy(c => c.Orden)
            .Select(c => new ContactoSolicitudViewModel { Nombre = c.Nombre, Relacion = c.Relacion, Telefono = c.Telefono })
            .ToListAsync(ct);
        db.EventosAccesoClinico.Add(new EventoAccesoClinico
        {
            UsuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier)!,
            SolicitudAtencionId = solicitudId
        });
        try
        {
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex) when (ex is DbUpdateException or Npgsql.NpgsqlException)
        {
            await tx.RollbackAsync(CancellationToken.None);
            // Si no se puede auditar, no devolver el contenido.
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "No fue posible completar la consulta. Intenta nuevamente.");
        }
        return View(datos.Modelo);
    }

    [HttpGet]
    public IActionResult Resultados() => View();

    [HttpGet]
    public IActionResult Atencion() => View();

    [HttpGet]
    [AgendaExceptionFilter]
    public async Task<IActionResult> RegistrarAtencion([FromServices] AgendaCitasService agenda, Guid citaId, CancellationToken ct)
    {
        var cita = await agenda.PrepararAtencion(User, citaId, ct);
        if (cita.Cerrada) return RedirectToAction(nameof(VerAtencion), new { citaId });
        ViewData["Cita"] = cita;
        return View(new RegistrarAtencionInput { CitaId = citaId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AgendaExceptionFilter]
    public async Task<IActionResult> RegistrarAtencion([FromServices] AgendaCitasService agenda, RegistrarAtencionInput input, CancellationToken ct)
    {
        // Revalidar acceso incluso cuando el formulario no supera validación MVC.
        var cita = await agenda.PrepararAtencion(User, input.CitaId, ct);
        if (cita.Cerrada) return RedirectToAction(nameof(VerAtencion), new { citaId = input.CitaId });
        ViewData["Cita"] = cita;
        if (!ModelState.IsValid) return View(input);
        await agenda.RegistrarAtencion(User, input, ct);
        TempData["Success"] = "La atención fue registrada y la cita quedó cerrada.";
        return RedirectToAction(nameof(Historial));
    }

    [HttpGet]
    [AgendaExceptionFilter]
    public async Task<IActionResult> VerAtencion([FromServices] AgendaCitasService agenda, Guid citaId, CancellationToken ct) =>
        View(await agenda.ConsultarAtencion(User, citaId, ct));

    [HttpGet]
    [AgendaExceptionFilter]
    public async Task<IActionResult> Historial([FromServices] AgendaCitasService agenda, string? busqueda,
        EstadoCita? estado, int pagina = 1, CancellationToken ct = default)
    {
        if (!ModelState.IsValid) throw new AgendaOperacionException("Filtros no válidos.");
        return View(await agenda.Historial(User, busqueda, estado, pagina, ct));
    }
}
