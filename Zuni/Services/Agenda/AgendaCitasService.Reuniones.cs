using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Zuni.Models.Agenda;
using Zuni.Services.Privacidad;

namespace Zuni.Services.Agenda;

public sealed partial class AgendaCitasService
{
    public static bool TryNormalizarMeet(string? valor, out string enlace)
    {
        enlace = "";
        var texto = valor?.Trim() ?? "";
        if (texto.Length > 120 || !Regex.IsMatch(texto, @"\Ahttps://meet\.google\.com/[a-z]{3}-[a-z]{4}-[a-z]{3}/?\z", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)) return false;
        enlace = texto.TrimEnd('/').ToLowerInvariant(); return true;
    }
    private async Task ValidarParticipantes(Cita cita, CancellationToken ct)
    {
        if (await Asignado(cita.PerfilEstudianteId, ct) != cita.PsicologoUsuarioId ||
            !await db.PerfilesEstudiante.AnyAsync(p => p.Id == cita.PerfilEstudianteId && p.Activo && p.Usuario.IsActive && !p.Usuario.DebeCambiarContrasena &&
                db.UserRoles.Any(ur => ur.UserId == p.UsuarioId && db.Roles.Any(r => r.Id == ur.RoleId && r.NormalizedName == "ESTUDIANTE")) &&
                !db.UserRoles.Any(ur => ur.UserId == p.UsuarioId && db.Roles.Any(r => r.Id == ur.RoleId && (r.NormalizedName == "ADMINISTRADOR" || r.NormalizedName == "DIRECTOR" || r.NormalizedName == "CATEDRATICO"))), ct))
            throw new AgendaAccesoException();
    }
    private async Task FijarEnlace(Cita cita, string? enlace, bool revisado, CancellationToken ct)
    {
        if (cita.Modalidad != ModalidadCita.Virtual || !TryNormalizarMeet(enlace, out var normalizado))
            throw new AgendaOperacionException("Usa un enlace HTTPS individual de Google Meet, por ejemplo https://meet.google.com/abc-defg-hij, sin parámetros ni enlaces generales.");
        if (!revisado) throw new AgendaOperacionException("Configura la reunión en Meet y confirma la revisión de sus controles de acceso e invitados.");
        var huella = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizado)));
        var anterior = await db.EnlacesReunionUsados.SingleOrDefaultAsync(e => e.Huella == huella, ct);
        if (anterior is not null && anterior.CitaId != cita.Id) throw new AgendaOperacionException("Ese enlace ya pertenece a otra cita. Crea una reunión individual nueva.");
        if (anterior is null) db.EnlacesReunionUsados.Add(new() { Huella = huella, CitaId = cita.Id, FechaUtc = Ahora });
        cita.EnlaceReunion = normalizado; cita.FechaEnlaceUtc = Ahora; cita.Revision = Guid.NewGuid();
        var usuario = await db.PerfilesEstudiante.Where(p => p.Id == cita.PerfilEstudianteId).Select(p => p.UsuarioId).SingleAsync(ct);
        PrivacidadService.Auditar(db, usuario, cita.PsicologoUsuarioId, "CITA_ENLACE_REUNION_GUARDADO", reloj, cita.Id);
    }
    public async Task<GuardarReunionInput> PrepararReunion(ClaimsPrincipal actor, Guid id, CancellationToken ct)
    {
        var psy = await Psicologo(actor, ct);
        var c = await db.Citas.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id && c.PsicologoUsuarioId == psy, ct) ?? throw new AgendaAccesoException();
        await ValidarParticipantes(c, ct);
        if (c.Modalidad != ModalidadCita.Virtual || c.Estado is not (EstadoCita.Solicitada or EstadoCita.Confirmada or EstadoCita.Reservada) || c.FechaHoraFinUtc <= Ahora)
            throw new AgendaOperacionException("Solo puedes preparar enlaces de citas virtuales activas antes de que terminen.");
        return new() { CitaId = c.Id, Revision = c.Revision, Enlace = c.EnlaceReunion ?? "", Estado = c.Estado, InicioUtc = c.FechaHoraInicioUtc,
            Estudiante = await db.PerfilesEstudiante.Where(p => p.Id == c.PerfilEstudianteId).Select(p => p.Usuario.FullName).SingleAsync(ct) };
    }
    public Task GuardarReunion(ClaimsPrincipal actor, GuardarReunionInput input, CancellationToken ct) => Escribir(async () =>
    {
        var psy = await Psicologo(actor, ct);
        var c = await db.Citas.SingleOrDefaultAsync(c => c.Id == input.CitaId && c.PsicologoUsuarioId == psy, ct) ?? throw new AgendaAccesoException();
        await ValidarParticipantes(c, ct);
        if (c.Revision != input.Revision) throw new AgendaOperacionException("La cita cambió. Abre nuevamente su preparación antes de guardar.");
        if (c.Estado is not (EstadoCita.Solicitada or EstadoCita.Confirmada or EstadoCita.Reservada) || c.FechaHoraFinUtc <= Ahora)
            throw new AgendaOperacionException("La cita ya no está disponible para preparar su reunión.");
        await FijarEnlace(c, input.Enlace, input.AccesoRestringidoRevisado, ct);
        if (input.ConfirmarCita) { await ValidarConfirmacionProgramada(c,ct); AplicarCambio(c, EstadoCita.Confirmada, null); await NotificarRespuestaDirector(c, ct); }
    }, ct);
    private async Task<Cita> ReunionPropia(ClaimsPrincipal actor, Guid id, CancellationToken ct)
    {
        Cita? c;
        var psy = await autorizacion.ObtenerPsicologoAutorizadoAsync(actor, ct);
        if (psy is not null) c = await db.Citas.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id && c.PsicologoUsuarioId == psy, ct);
        else { var perfil = await Estudiante(actor, ct); c = await db.Citas.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id && c.PerfilEstudianteId == perfil.Id, ct); }
        if (c is null) throw new AgendaAccesoException();
        await ValidarParticipantes(c, ct); return c;
    }
    private bool PuedeEntrar(Cita c) => c.Modalidad == ModalidadCita.Virtual && c.Estado is EstadoCita.Confirmada or EstadoCita.Reservada
        && c.EnlaceReunion is not null && c.FechaHoraInicioUtc.AddMinutes(-15) <= Ahora && Ahora < c.FechaHoraFinUtc;
    public async Task<AccesoReunionViewModel> ConsultarReunion(ClaimsPrincipal actor, Guid id, CancellationToken ct)
    {
        var c = await ReunionPropia(actor, id, ct);
        return new(c.Id, c.FechaHoraInicioUtc, c.FechaHoraFinUtc, c.EnlaceReunion is not null, PuedeEntrar(c));
    }
    public async Task<string> EntrarReunion(ClaimsPrincipal actor, Guid id, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var c = await ReunionPropia(actor, id, ct);
        if (!PuedeEntrar(c)) throw new AgendaOperacionException("El acceso se habilita 15 minutos antes de una cita virtual confirmada y termina al finalizarla. Una cita cancelada o sin enlace no permite ingresar.");
        var usuario = await db.PerfilesEstudiante.Where(p => p.Id == c.PerfilEstudianteId).Select(p => p.UsuarioId).SingleAsync(ct);
        PrivacidadService.Auditar(db, usuario, actor.FindFirstValue(ClaimTypes.NameIdentifier)!, "CITA_REUNION_ACCEDIDA", reloj, id);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return c.EnlaceReunion!;
    }
}
