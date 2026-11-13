using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Zuni.Models.Agenda;
using Zuni.Models.Atencion;
using Zuni.Services.Privacidad;

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

    private async Task<Guid?> SolicitudClinica(ClaimsPrincipal sesion, Cita cita, CancellationToken ct)
    {
        var registro = await db.RegistrosAtencionCita.AsNoTracking().Where(r => r.CitaId == cita.Id)
            .Select(r => new { r.SolicitudAtencionId }).SingleOrDefaultAsync(ct);
        var solicitud = registro?.SolicitudAtencionId;
        // Cita no tiene solicitud: fijamos la última consentida anterior a su creación.
        // Nunca se enlaza silenciosamente una solicitud futura para habilitar un cierre antiguo.
        if (registro is null) solicitud = await db.SolicitudesAtencion.AsNoTracking().Where(s =>
                s.PerfilEstudianteId == cita.PerfilEstudianteId && s.FechaSolicitudUtc <= cita.FechaCreacionUtc &&
                db.ConsentimientosAtencion.Any(c => c.SolicitudAtencionId == s.Id && c.Aceptado))
            .OrderByDescending(s => s.FechaSolicitudUtc).ThenBy(s => s.Id).Select(s => (Guid?)s.Id).FirstOrDefaultAsync(ct);
        if (solicitud is null)
        {
            if (!cita.EsImprevista && cita.ProgramadorRol != "Psicologo")
                throw new AgendaOperacionException("Esta cita requiere revisar su solicitud de atención antes de completar el registro.");
            // La atención profesional no depende de una solicitud posterior del estudiante.
            // Revalidar el vínculo y mantener la propiedad histórica de la cita.
            if (!await EstudiantesParaImprevista(cita.PsicologoUsuarioId).AnyAsync(p => p.Id == cita.PerfilEstudianteId, ct))
                throw new AgendaAccesoException();
            return null;
        }
        if (
            !await db.SolicitudesAtencion.AnyAsync(s => s.Id == solicitud && s.PerfilEstudianteId == cita.PerfilEstudianteId, ct) ||
            !await autorizacion.PuedeConsultarExpedienteAsync(sesion, solicitud.Value, ct))
            throw new AgendaAccesoException();
        return solicitud;
    }

    private async Task AuditarRegistroAtencion(Cita cita, Guid? solicitud, string accion, CancellationToken ct)
    {
        if (solicitud is { } solicitudId)
            db.EventosAccesoClinico.Add(new EventoAccesoClinico { UsuarioId = cita.PsicologoUsuarioId,
                SolicitudAtencionId = solicitudId, FechaUtc = Ahora, TipoAcceso = accion });
        else {
            var usuario = await db.PerfilesEstudiante.Where(p => p.Id == cita.PerfilEstudianteId).Select(p => p.UsuarioId).SingleAsync(ct);
            PrivacidadService.Auditar(db, usuario, cita.PsicologoUsuarioId, accion, reloj, cita.Id);
        }
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
        tracked.Revision = Guid.NewGuid();
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
        await AuditarRegistroAtencion(cita, solicitud, "ATENCION_REGISTRADA", ct);
    }, ct);

    private static string? TextoAtencion(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    public async Task<AtencionPrivadaViewModel> ConsultarAtencion(ClaimsPrincipal sesion, Guid citaId, CancellationToken ct, bool incluirPrivado = false)
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
                    ResultadoProfesionalPrivado = incluirPrivado ? r.ResultadoProfesionalPrivado : null,
                    ResenaRecomendacionesEstudiante = r.ResenaRecomendacionesEstudiante,
                    ResultadoBreveEstudiante = r.ResultadoBreveEstudiante,
                    RecomiendaOtraCita = r.RecomiendaOtraCita, IndicacionesProximaCita = r.IndicacionesProximaCita
                }).SingleOrDefaultAsync(ct) ?? throw new AgendaAccesoException();
            await AuditarRegistroAtencion(cita, solicitud, "ATENCION_CONSULTADA", ct);
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
            CitaId = c.Id,
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
