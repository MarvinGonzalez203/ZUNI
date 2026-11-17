using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Zuni.Models.Agenda;
using Zuni.Services.Privacidad;
namespace Zuni.Services.Agenda;

public sealed partial class AgendaCitasService
{
    // La ausencia del profesional ocupa su intervalo, pero libera al estudiante.
    private static readonly EstadoCita[] EstadosOcupadosProfesional = [EstadoCita.Reservada, EstadoCita.Solicitada, EstadoCita.Confirmada, EstadoCita.AusenciaProfesional];
    public async Task<CitaResumen> PrepararAusencia(ClaimsPrincipal sesion, Guid id, CancellationToken ct)
    {
        var psy = await Psicologo(sesion, ct);
        return await ResumenPsicologo(psy).SingleOrDefaultAsync(c => c.Id == id, ct) ?? throw new AgendaAccesoException();
    }
    public Task AvisarAusencia(ClaimsPrincipal sesion, Guid id, Guid revision, CancellationToken ct) => Escribir(async () =>
    {
        var psy = await Psicologo(sesion, ct);
        var c = await db.Citas.SingleOrDefaultAsync(c => c.Id == id && c.PsicologoUsuarioId == psy, ct) ?? throw new AgendaAccesoException();
        if (revision == Guid.Empty || revision != c.Revision) throw new AgendaOperacionException("La cita cambió. Abre su versión actual antes de avisar.");
        if (!AgendaReglas.Activa(c.Estado) || c.EsImprevista || await db.RegistrosAtencionCita.AnyAsync(r => r.CitaId == id, ct))
            throw new AgendaOperacionException("Solo puedes avisar ausencia en citas programadas pendientes de atención. No se modificó el registro.");
        c.Estado = EstadoCita.AusenciaProfesional; c.Revision = Guid.NewGuid();
        // No registrar ni comunicar diagnósticos, accidentes o detalles de la vida del profesional.
        c.MotivoCancelacion = "Cita no realizada por indisponibilidad del profesional. Pendiente de nueva programación.";
        c.EnlaceReunion = null; c.FechaEnlaceUtc = null;
        var estudiante = await db.PerfilesEstudiante.Where(p => p.Id == c.PerfilEstudianteId).Select(p => p.UsuarioId).SingleAsync(ct);
        var texto = $"Tu psicólogo avisó que no podrá atender la cita del {AgendaReglas.Local(c.FechaHoraInicioUtc):dd/MM/yyyy HH:mm} (Guatemala). La cita queda pendiente de nueva programación. Recibirás otro aviso cuando se registre la nueva fecha. No se registró una inasistencia del estudiante.";
        db.NotificacionesCita.Add(new() { UsuarioId = estudiante, CitaId = c.Id, Texto = texto, FechaUtc = Ahora, AutorUsuarioId = psy, AutorRol = "Psicologo", Tipo = "Ausencia profesional" });
        PrivacidadService.Auditar(db, estudiante, psy, "CITA_AUSENCIA_PROFESIONAL", reloj, c.Id);
        await NotificarRespuestaDirector(c, ct);
    }, ct);
}
