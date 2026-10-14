using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Zuni.Models.Agenda;
using Zuni.Services.Privacidad;
namespace Zuni.Services.Agenda;

public sealed partial class AgendaCitasService
{
    public async Task<ImprevistaInput> PrepararImprevista(ClaimsPrincipal actor, DateOnly? fecha, CancellationToken ct)
    {
        var psy = await Psicologo(actor, ct);
        var dia = fecha ?? AgendaReglas.Hoy(Ahora);
        if (dia > AgendaReglas.Hoy(Ahora)) throw new AgendaOperacionException("Una atención imprevista registra hechos ocurridos, no fechas futuras.");
        return new() { Fecha = dia, Estudiantes = await (from a in db.AsignacionesEstudiantePsicologo.AsNoTracking()
            join p in db.PerfilesEstudiante.AsNoTracking() on a.PerfilEstudianteId equals p.Id
            where a.PsicologoUsuarioId == psy && a.FechaFinalizacionUtc == null && p.Activo && p.Usuario.IsActive
            orderby p.Usuario.FullName select new EstudianteImprevista(p.Id, p.Usuario.FullName)).ToListAsync(ct) };
    }
    public async Task<Guid> RegistrarImprevista(ClaimsPrincipal actor, ImprevistaInput input, CancellationToken ct)
    {
        var id = input.OperacionId;
        await Escribir(async () => {
            var psy = await Psicologo(actor, ct);
            if (id == Guid.Empty || input.Fecha is null || input.HoraInicio is null || input.PerfilEstudianteId is null || !Enum.IsDefined(input.Modalidad)) throw new AgendaOperacionException("Completa la fecha, el inicio, el estudiante y la modalidad.");
            var inicio = AgendaReglas.Utc(input.Fecha.Value, input.HoraInicio.Value);
            if (inicio >= Ahora) throw new AgendaOperacionException("El inicio de una atención imprevista debe ser anterior a la hora actual.");
            var fin = input.HoraFin is { } horaFin ? AgendaReglas.Utc(input.Fecha.Value, horaFin) : new[] { inicio.AddMinutes(50), Ahora, AgendaReglas.Utc(input.Fecha.Value.AddDays(1), TimeOnly.MinValue).AddTicks(-1) }.Min();
            if (fin <= inicio || fin > Ahora) throw new AgendaOperacionException("Revisa el fin aproximado: debe seguir al inicio y no ser futuro.");
            if (await Asignado(input.PerfilEstudianteId.Value, ct) != psy || !await db.PerfilesEstudiante.AnyAsync(p => p.Id == input.PerfilEstudianteId && p.Activo && p.Usuario.IsActive, ct)) throw new AgendaAccesoException();
            await ValidarParticipantes(new Cita { PerfilEstudianteId = input.PerfilEstudianteId.Value, PsicologoUsuarioId = psy }, ct);
            if (await db.Citas.AnyAsync(c => c.Id == id, ct)) throw new AgendaOperacionException("Este registro ya fue guardado. Consúltalo en la agenda.");
            if (await db.Citas.AnyAsync(c => (c.PsicologoUsuarioId == psy || c.PerfilEstudianteId == input.PerfilEstudianteId) && (EstadosActivos.Contains(c.Estado) || c.Estado == EstadoCita.Completada) && c.FechaHoraInicioUtc < fin && c.FechaHoraFinUtc > inicio, ct)) throw new AgendaOperacionException("El horario se cruza con una atención existente. Revisa los registros antes de duplicarlos.");
            db.Citas.Add(new Cita { Id = id, PerfilEstudianteId = input.PerfilEstudianteId.Value, PsicologoUsuarioId = psy,
                FechaHoraInicioUtc = inicio, FechaHoraFinUtc = fin, Modalidad = input.Modalidad, Estado = EstadoCita.Confirmada,
                EsImprevista = true, FinAproximado = true, FechaCreacionUtc = Ahora });
            var user = await db.PerfilesEstudiante.Where(p => p.Id == input.PerfilEstudianteId).Select(p => p.UsuarioId).SingleAsync(ct);
            PrivacidadService.Auditar(db, user, psy, "ATENCION_IMPREVISTA_REGISTRADA", reloj, id);
        }, ct);
        return id;
    }
    public async Task<ReporteCompartido> ReporteAtencion(ClaimsPrincipal actor, Guid id, bool pdf, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var psy = await autorizacion.ObtenerPsicologoAutorizadoAsync(actor, ct);
        Cita cita;
        if (psy is not null) { cita = await CitaClinica(actor, id, ct); await SolicitudClinica(actor, cita, ct); }
        else { var perfil = await Estudiante(actor, ct); cita = await db.Citas.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id && c.PerfilEstudianteId == perfil.Id, ct) ?? throw new AgendaAccesoException(); }
        if (cita.Estado != EstadoCita.Completada) throw new AgendaOperacionException("El reporte compartido solo está disponible para atenciones finalizadas con asistencia.");
        var reporte = await (from r in db.RegistrosAtencionCita.AsNoTracking() join c in db.Citas.AsNoTracking() on r.CitaId equals c.Id
            join p in db.PerfilesEstudiante.AsNoTracking() on c.PerfilEstudianteId equals p.Id
            join u in db.Users.AsNoTracking() on c.PsicologoUsuarioId equals u.Id
            where c.Id == id && r.Asistio
            select new ReporteCompartido(c.Id, p.Usuario.FullName, p.Carne, u.FullName, c.FechaHoraInicioUtc, c.FechaHoraFinUtc, c.FinAproximado,
                r.ResultadoBreveEstudiante ?? "", r.ResenaRecomendacionesEstudiante ?? "", r.RecomiendaOtraCita, r.IndicacionesProximaCita)).SingleOrDefaultAsync(ct) ?? throw new AgendaAccesoException();
        PrivacidadService.Auditar(db, await db.PerfilesEstudiante.Where(p => p.Id == cita.PerfilEstudianteId).Select(p => p.UsuarioId).SingleAsync(ct), actor.FindFirstValue(ClaimTypes.NameIdentifier)!, pdf ? "REPORTE_COMPARTIDO_DESCARGADO" : "REPORTE_COMPARTIDO_CONSULTADO", reloj, id);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return reporte;
    }
}
