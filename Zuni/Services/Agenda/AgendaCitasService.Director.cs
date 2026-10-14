using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Zuni.Models.Agenda;
using Zuni.Services.MayoriaEdad;

namespace Zuni.Services.Agenda;

public sealed partial class AgendaCitasService
{
    public async Task<CalendarioDirectorViewModel> CalendarioDirector(ClaimsPrincipal actor, string? psicologoId, DateOnly? fecha, CancellationToken ct)
    {
        if (!await MayoriaEdadService.ActorAsync(db, actor, "Director", ct)) throw new AgendaAccesoException();
        var profesionales = await db.Users.AsNoTracking().Where(u => u.IsActive && !u.DebeCambiarContrasena &&
            db.UserRoles.Any(ur => ur.UserId == u.Id && db.Roles.Any(r => r.Id == ur.RoleId && r.NormalizedName == "PSICOLOGO")) &&
            !db.UserRoles.Any(ur => ur.UserId == u.Id && db.Roles.Any(r => r.Id == ur.RoleId && (r.NormalizedName == "ADMINISTRADOR" || r.NormalizedName == "DIRECTOR" || r.NormalizedName == "CATEDRATICO"))))
            .OrderBy(u => u.FullName).Select(u => new SelectListItem(u.FullName, u.Id)).ToListAsync(ct);
        psicologoId ??= profesionales.FirstOrDefault()?.Value;
        if (psicologoId is not null && !profesionales.Any(p => p.Value == psicologoId)) throw new AgendaAccesoException();
        var calendario = await LeerCalendario(psicologoId, fecha, false, null, ct);
        calendario.Director = true; calendario.PsicologoId = psicologoId;
        // Metadatos de agenda: sin expediente, motivo de consulta, notas o resultados.
        var desde = AgendaReglas.Utc(calendario.Mes, TimeOnly.MinValue);
        var hasta = AgendaReglas.Utc(calendario.Mes.AddMonths(1), TimeOnly.MinValue);
        var citas = await (from c in db.Citas.AsNoTracking() join p in db.PerfilesEstudiante on c.PerfilEstudianteId equals p.Id
            where c.PsicologoUsuarioId == psicologoId && c.FechaHoraInicioUtc >= desde && c.FechaHoraInicioUtc < hasta
            select new CitaCalendarioResumen { Id=c.Id, Nombre=p.Usuario.FullName, InicioUtc=c.FechaHoraInicioUtc, FinUtc=c.FechaHoraFinUtc,
                Modalidad=c.Modalidad, Estado=c.Estado, EsImprevista=c.EsImprevista }).ToListAsync(ct);
        foreach(var d in calendario.Dias) d.Citas = citas.Where(c => AgendaReglas.Hoy(c.InicioUtc) == d.Fecha).OrderBy(c => c.InicioUtc).ToList();
        foreach(var p in profesionales) p.Selected = p.Value == psicologoId;
        return new(calendario, profesionales);
    }

    private async Task NotificarRespuestaDirector(Cita c, CancellationToken ct)
    {
        if(c.ProgramadorRol != "Director" || c.ProgramadorUsuarioId is null || c.Estado is not (EstadoCita.Confirmada or EstadoCita.Rechazada)) return;
        var nombre = await db.Users.Where(u => u.Id == c.PsicologoUsuarioId).Select(u => u.FullName).SingleAsync(ct);
        var texto = $"El psicólogo {nombre} {(c.Estado == EstadoCita.Confirmada ? "aceptó" : "rechazó")} la cita propuesta para {AgendaReglas.Local(c.FechaHoraInicioUtc):dd/MM/yyyy HH:mm} (Guatemala).";
        if(c.Estado == EstadoCita.Rechazada) texto += " Motivo administrativo: " + c.MotivoCancelacion;
        db.NotificacionesCita.Add(new(){UsuarioId=c.ProgramadorUsuarioId, CitaId=c.Id, Texto=texto, FechaUtc=Ahora});
        var estudiante = await db.PerfilesEstudiante.Where(p => p.Id == c.PerfilEstudianteId).Select(p => p.UsuarioId).SingleAsync(ct);
        db.NotificacionesCita.Add(new(){UsuarioId=estudiante, CitaId=c.Id, Texto=texto, FechaUtc=Ahora});
    }
}
