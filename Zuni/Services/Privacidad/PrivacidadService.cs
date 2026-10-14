using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models;
using Zuni.Models.Privacidad;
using Zuni.Services.MayoriaEdad;

namespace Zuni.Services.Privacidad;

public sealed class DatosOperacionException(string texto) : Exception(texto);
public static class PrivacidadService
{
    public static void Validar(object input)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(input, new(input), errors, true)) throw new DatosOperacionException(errors[0].ErrorMessage ?? "Revisa los datos.");
    }
    public static async Task<string> UsuarioAsync(ApplicationDbContext db, ClaimsPrincipal sesion, CancellationToken ct)
    {
        var id = sesion.FindFirstValue(ClaimTypes.NameIdentifier); var stamp = sesion.FindFirstValue("Zuni.SecurityStamp");
        if (sesion.Identity?.IsAuthenticated != true || string.IsNullOrEmpty(id) || string.IsNullOrEmpty(stamp) ||
            !await db.Users.AnyAsync(u => u.Id == id && u.IsActive && !u.DebeCambiarContrasena && u.SecurityStamp == stamp, ct)) throw new UnauthorizedAccessException();
        return id;
    }
    public static Task<PoliticaPrivacidad?> VigenteAsync(ApplicationDbContext db, CancellationToken ct) =>
        db.PoliticasPrivacidad.AsNoTracking().OrderByDescending(p => p.Version).FirstOrDefaultAsync(ct);
    public static async Task<bool> AceptadaAsync(ApplicationDbContext db, string id, CancellationToken ct)
    {
        var politica = await VigenteAsync(db, ct);
        return politica is not null && await db.AceptacionesPrivacidad.AnyAsync(a => a.UsuarioId == id && a.PoliticaId == politica.Id, ct);
    }
    public static void Auditar(ApplicationDbContext db, string usuario, string actor, string accion, TimeProvider reloj, Guid? referencia = null) =>
        db.AuditoriaUsuarios.Add(new AuditoriaUsuario { UsuarioAfectadoId = usuario, AdministradorId = actor, Accion = accion,
            DatosNuevos = referencia is { } id ? System.Text.Json.JsonSerializer.Serialize(id) : null, FechaUtc = reloj.GetUtcNow().UtcDateTime });

    public static async Task PublicarAsync(ApplicationDbContext db, ClaimsPrincipal actor, PublicarPoliticaInput input, TimeProvider reloj, CancellationToken ct)
    {
        Validar(input);
        if (!input.Confirmada) throw new DatosOperacionException("Confirma que la institución aprobó el texto y el procedimiento antes de publicarlos.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (!await MayoriaEdadService.ActorAsync(db, actor, "Administrador", ct)) throw new UnauthorizedAccessException();
        var actual = await VigenteAsync(db, ct);
        if (actual?.Id != input.VersionAnteriorId) throw new DbUpdateConcurrencyException();
        var institucion = await Zuni.Services.Institucion.ConfiguracionInstitucionService.ObtenerAsync(db, ct);
        var id = actor.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var politica = new PoliticaPrivacidad { Version = (actual?.Version ?? 0) + 1, Institucion = institucion.Nombre,
            Responsable = input.Responsable.Trim(), Contacto = input.Contacto.Trim(), Finalidades = input.Finalidades.Trim(),
            DatosYAccesos = input.DatosYAccesos.Trim(), Conservacion = input.Conservacion.Trim(), ProcedimientoDerechos = input.ProcedimientoDerechos.Trim(),
            PublicadaPorId = id, FechaPublicacionUtc = reloj.GetUtcNow().UtcDateTime };
        db.PoliticasPrivacidad.Add(politica); Auditar(db, id, id, "POLITICA_PRIVACIDAD_PUBLICADA", reloj, politica.Id);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    public static async Task AceptarAsync(ApplicationDbContext db, ClaimsPrincipal actor, Guid politicaId, bool confirmada, TimeProvider reloj, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var id = await UsuarioAsync(db, actor, ct);
        if (!confirmada) throw new DatosOperacionException("Confirma que leíste y aceptas el tratamiento para las finalidades indicadas.");
        var actual = await VigenteAsync(db, ct);
        if (actual?.Id != politicaId) throw new DatosOperacionException("La política cambió. Lee la versión vigente antes de aceptar.");
        if (!await db.AceptacionesPrivacidad.AnyAsync(a => a.UsuarioId == id && a.PoliticaId == politicaId, ct))
        {
            db.AceptacionesPrivacidad.Add(new() { UsuarioId = id, PoliticaId = politicaId, FechaUtc = reloj.GetUtcNow().UtcDateTime });
            Auditar(db, id, id, "PRIVACIDAD_ACEPTADA", reloj, politicaId); await db.SaveChangesAsync(ct);
        }
        await tx.CommitAsync(ct);
    }
    public static async Task SolicitarAsync(ApplicationDbContext db, ClaimsPrincipal actor, SolicitarDerechoInput input, TimeProvider reloj, CancellationToken ct)
    {
        Validar(input);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var id = await UsuarioAsync(db, actor, ct);
        if (await db.SolicitudesDerechosDatos.CountAsync(s => s.UsuarioId == id && s.Estado == EstadoDerechoDatos.Pendiente, ct) >= 5)
            throw new DatosOperacionException("Ya tienes cinco solicitudes pendientes. Espera su respuesta antes de registrar otra.");
        var solicitud = new SolicitudDerechoDatos { UsuarioId = id, Tipo = input.Tipo, Detalle = input.Detalle.Trim(), FechaSolicitudUtc = reloj.GetUtcNow().UtcDateTime };
        db.SolicitudesDerechosDatos.Add(solicitud); Auditar(db, id, id, "DERECHO_DATOS_SOLICITADO", reloj, solicitud.Id);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    public static async Task ResponderAsync(ApplicationDbContext db, ClaimsPrincipal actor, ResponderDerechoInput input, TimeProvider reloj, CancellationToken ct)
    {
        Validar(input);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (!await MayoriaEdadService.ActorAsync(db, actor, "Administrador", ct)) throw new UnauthorizedAccessException();
        var s = await db.SolicitudesDerechosDatos.SingleOrDefaultAsync(s => s.Id == input.Id, ct) ?? throw new UnauthorizedAccessException();
        if (s.Revision != input.Revision) throw new DbUpdateConcurrencyException();
        if (s.Estado != EstadoDerechoDatos.Pendiente) throw new DatosOperacionException("Esta solicitud ya tiene una respuesta registrada.");
        s.Estado = EstadoDerechoDatos.Respondida; s.Respuesta = input.Respuesta.Trim(); s.ResponsableId = actor.FindFirstValue(ClaimTypes.NameIdentifier);
        s.FechaRespuestaUtc = reloj.GetUtcNow().UtcDateTime; s.Revision = Guid.NewGuid();
        Auditar(db, s.UsuarioId, s.ResponsableId!, "DERECHO_DATOS_RESPONDIDO", reloj, s.Id);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
}
