using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Zuni.Models.Agenda;
using Zuni.Models.Atencion;

namespace Zuni.Services.Agenda;

public sealed partial class AgendaCitasService
{
    private async Task<Cita> CitaClinica(ClaimsPrincipal sesion, Guid citaId, CancellationToken ct)
    {
        var id = await Psicologo(sesion, ct);
        // También se exige propiedad histórica: un nuevo asignado no hereda notas privadas.
        return await db.Citas.AsNoTracking().SingleOrDefaultAsync(c => c.Id == citaId && c.PsicologoUsuarioId == id, ct)
            ?? throw new AgendaAccesoException();
    }

    private async Task<Guid> SolicitudClinica(ClaimsPrincipal sesion, Cita cita, CancellationToken ct)
    {
        var solicitud = await db.RegistrosAtencionCita.AsNoTracking().Where(r => r.CitaId == cita.Id)
            .Select(r => (Guid?)r.SolicitudAtencionId).SingleOrDefaultAsync(ct);
        // Cita no tiene solicitud: fijamos la última consentida anterior a su creación.
        // Nunca se enlaza silenciosamente una solicitud futura para habilitar un cierre antiguo.
        solicitud ??= await db.SolicitudesAtencion.AsNoTracking().Where(s =>
                s.PerfilEstudianteId == cita.PerfilEstudianteId && s.FechaSolicitudUtc <= cita.FechaCreacionUtc &&
                db.ConsentimientosAtencion.Any(c => c.SolicitudAtencionId == s.Id && c.Aceptado))
            .OrderByDescending(s => s.FechaSolicitudUtc).ThenBy(s => s.Id).Select(s => (Guid?)s.Id).FirstOrDefaultAsync(ct);
        if (solicitud is null ||
            !await db.SolicitudesAtencion.AnyAsync(s => s.Id == solicitud && s.PerfilEstudianteId == cita.PerfilEstudianteId, ct) ||
            !await autorizacion.PuedeConsultarExpedienteAsync(sesion, solicitud.Value, ct))
            throw new AgendaAccesoException();
        return solicitud.Value;
    }

    public async Task<PrepararAtencionViewModel> PrepararAtencion(ClaimsPrincipal sesion, Guid citaId, CancellationToken ct)
    {
        var cita = await CitaClinica(sesion, citaId, ct);
        await SolicitudClinica(sesion, cita, ct);
        var cerrada = await db.RegistrosAtencionCita.AnyAsync(r => r.CitaId == cita.Id, ct);
        if (!cerrada) AgendaReglas.ValidarCambio(cita, EstadoCita.Completada, Ahora);
        var nombre = await db.PerfilesEstudiante.Where(p => p.Id == cita.PerfilEstudianteId).Select(p => p.Usuario.FullName).SingleAsync(ct);
        return new(cita.Id, nombre, cita.FechaHoraInicioUtc, cita.Modalidad, cerrada);
    }

    public Task RegistrarAtencion(ClaimsPrincipal sesion, RegistrarAtencionInput input, CancellationToken ct) => Escribir(async () =>
    {
        var cita = await CitaClinica(sesion, input.CitaId, ct);
        var solicitud = await SolicitudClinica(sesion, cita, ct);
        if (input.Asistio == true && !await Zuni.Services.MayoriaEdad.MayoriaEdadService.VerificadaAsync(db, cita.PerfilEstudianteId, ct))
            throw new AgendaOperacionException("La institución debe verificar la mayoría de edad del estudiante antes de registrar una atención con asistencia.");
        if (await db.RegistrosAtencionCita.AnyAsync(r => r.CitaId == cita.Id, ct))
            throw new AgendaOperacionException("Esta atención ya fue cerrada. Consulta su detalle de solo lectura.");
        var errores = new List<ValidationResult>();
        if (!Validator.TryValidateObject(input, new ValidationContext(input), errores, true))
            throw new AgendaOperacionException(errores[0].ErrorMessage!);
        var asistio = input.Asistio == true;
        var estado = asistio ? EstadoCita.Completada : EstadoCita.NoAsistio;
        AgendaReglas.ValidarCambio(cita, estado, Ahora);
        // Solo estas propiedades se actualizan; no se enlaza la entidad desde el cliente.
        var tracked = await db.Citas.SingleAsync(c => c.Id == cita.Id, ct);
        tracked.Estado = estado;
        db.RegistrosAtencionCita.Add(new RegistroAtencionCita
        {
            CitaId = cita.Id, SolicitudAtencionId = solicitud, Asistio = asistio,
            ResultadoProfesionalPrivado = asistio ? input.ResultadoProfesionalPrivado!.Trim() : null,
            ResenaRecomendacionesEstudiante = TextoAtencion(input.ResenaRecomendacionesEstudiante),
            ResultadoBreveEstudiante = asistio ? input.ResultadoBreveEstudiante!.Trim() : null,
            RecomiendaOtraCita = asistio && input.RecomiendaOtraCita,
            IndicacionesProximaCita = asistio && input.RecomiendaOtraCita ? input.IndicacionesProximaCita!.Trim() : null,
            FechaCierreUtc = Ahora
        });
        db.EventosAccesoClinico.Add(new EventoAccesoClinico
        {
            UsuarioId = cita.PsicologoUsuarioId, SolicitudAtencionId = solicitud,
            FechaUtc = Ahora, TipoAcceso = "ATENCION_REGISTRADA"
        });
    }, ct);

    private static string? TextoAtencion(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    public async Task<AtencionPrivadaViewModel> ConsultarAtencion(ClaimsPrincipal sesion, Guid citaId, CancellationToken ct)
    {
        AtencionPrivadaViewModel? detalle = null;
        // Si no se confirma la auditoría, nunca se devuelve el contenido privado.
        await Escribir(async () =>
        {
            var cita = await CitaClinica(sesion, citaId, ct);
            var solicitud = await SolicitudClinica(sesion, cita, ct);
            detalle = await (from r in db.RegistrosAtencionCita.AsNoTracking()
                join c in db.Citas.AsNoTracking() on r.CitaId equals c.Id
                join p in db.PerfilesEstudiante.AsNoTracking() on c.PerfilEstudianteId equals p.Id
                where c.Id == cita.Id && c.PsicologoUsuarioId == cita.PsicologoUsuarioId
                select new AtencionPrivadaViewModel
                {
                    Estudiante = p.Usuario.FullName, InicioUtc = c.FechaHoraInicioUtc,
                    Modalidad = c.Modalidad, FechaCierreUtc = r.FechaCierreUtc, Asistio = r.Asistio,
                    ResultadoProfesionalPrivado = r.ResultadoProfesionalPrivado,
                    ResenaRecomendacionesEstudiante = r.ResenaRecomendacionesEstudiante,
                    ResultadoBreveEstudiante = r.ResultadoBreveEstudiante,
                    RecomiendaOtraCita = r.RecomiendaOtraCita, IndicacionesProximaCita = r.IndicacionesProximaCita
                }).SingleOrDefaultAsync(ct) ?? throw new AgendaAccesoException();
            db.EventosAccesoClinico.Add(new EventoAccesoClinico
            {
                UsuarioId = cita.PsicologoUsuarioId, SolicitudAtencionId = solicitud,
                FechaUtc = Ahora, TipoAcceso = "ATENCION_CONSULTADA"
            });
        }, ct);
        return detalle!;
    }

    // Proyectar antes de materializar: ni siquiera se selecciona la columna privada.
    private IQueryable<ResultadoAtencionEstudianteViewModel> ResultadosCompartidos(Guid perfilId) =>
        from r in db.RegistrosAtencionCita.AsNoTracking()
        join c in db.Citas.AsNoTracking() on r.CitaId equals c.Id
        where c.PerfilEstudianteId == perfilId && c.Estado == EstadoCita.Completada && r.Asistio
        orderby c.FechaHoraInicioUtc descending, c.Id
        select new ResultadoAtencionEstudianteViewModel
        {
            InicioUtc = c.FechaHoraInicioUtc, Modalidad = c.Modalidad,
            ResultadoBreveEstudiante = r.ResultadoBreveEstudiante,
            ResenaRecomendacionesEstudiante = r.ResenaRecomendacionesEstudiante,
            RecomiendaOtraCita = r.RecomiendaOtraCita, IndicacionesProximaCita = r.IndicacionesProximaCita
        };

    public async Task<IReadOnlyList<ResultadoAtencionEstudianteViewModel>> ResultadosAtencion(ClaimsPrincipal sesion, CancellationToken ct)
    {
        var perfil = await Estudiante(sesion, ct);
        return await ResultadosCompartidos(perfil.Id).ToListAsync(ct);
    }
}
