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
        var profesionales = await ProfesionalesElegibles()
            .OrderBy(u => u.FullName).Select(u => new SelectListItem(u.FullName, u.Id)).ToListAsync(ct);
        psicologoId ??= profesionales.FirstOrDefault()?.Value;
        if (psicologoId is not null && !profesionales.Any(p => p.Value == psicologoId)) throw new AgendaAccesoException();
        var calendario = await LeerCalendario(psicologoId, fecha, false, null, ct);
        calendario.Director = true; calendario.PsicologoId = psicologoId;
        // Privacidad: el Director solo recibe bloques de tiempo y conteos. La consulta no proyecta
        // nombre, carné, correo, perfil, solicitud, Id de cita, modalidad, lugar ni contenido clínico.
        var desde = AgendaReglas.Utc(calendario.Mes, TimeOnly.MinValue);
        var hasta = AgendaReglas.Utc(calendario.Mes.AddMonths(1), TimeOnly.MinValue);
        var citas = await db.Citas.AsNoTracking()
            .Where(c => c.PsicologoUsuarioId == psicologoId && c.FechaHoraInicioUtc >= desde && c.FechaHoraInicioUtc < hasta)
            .Select(c => new { Inicio = c.FechaHoraInicioUtc, Fin = c.FechaHoraFinUtc, c.Estado }).ToListAsync(ct);
        static string? EstadoBloque(EstadoCita estado) => estado switch
        {
            EstadoCita.Solicitada => "Pendiente de revisar",
            EstadoCita.Reservada or EstadoCita.Confirmada or EstadoCita.Completada or EstadoCita.NoAsistio => "Ocupado",
            _ => null
        };
        foreach (var d in calendario.Dias)
            d.Bloques = citas.Where(c => AgendaReglas.Hoy(c.Inicio) == d.Fecha && EstadoBloque(c.Estado) is not null)
                .OrderBy(c => c.Inicio).Select(c => new BloqueCargaDirector(c.Inicio, c.Fin, EstadoBloque(c.Estado)!)).ToList();
        foreach (var p in profesionales) p.Selected = p.Value == psicologoId;
        var metricas = new MetricasCargaDirector(
            citas.Count(c => c.Estado is EstadoCita.Reservada or EstadoCita.Confirmada),
            citas.Count(c => c.Estado == EstadoCita.Solicitada),
            citas.Count(c => c.Estado == EstadoCita.Completada),
            citas.Count(c => c.Estado is EstadoCita.Cancelada or EstadoCita.Rechazada),
            citas.Count(c => c.Estado == EstadoCita.NoAsistio));
        return new(calendario, profesionales) { Metricas = metricas };
    }
    // La respuesta a propuestas de Dirección (avisos administrativos y auditoría) está en
    // AgendaCitasService.Propuestas.cs.
}
