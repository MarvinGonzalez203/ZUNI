using System.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models.Atencion;
using Zuni.Models.BigFive;
using Zuni.Resources;
namespace Zuni.Services.BigFive;

public sealed class BigFiveService(ApplicationDbContext db, IAutorizacionClinicaService autorizacion)
{
    private async Task<Guid> PerfilAutorizado(ClaimsPrincipal sesion, CancellationToken ct)
    {
        var id = sesion.FindFirstValue(ClaimTypes.NameIdentifier);
        var stamp = sesion.FindFirstValue("Zuni.SecurityStamp");
        if (sesion.Identity?.IsAuthenticated != true || !sesion.IsInRole("Estudiante") ||
            string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(stamp)) throw new BigFiveAccesoException();
        var perfil = await db.PerfilesEstudiante.AsNoTracking()
            .Where(p => p.UsuarioId == id && p.Activo && p.Usuario.IsActive &&
                !p.Usuario.DebeCambiarContrasena && p.Usuario.SecurityStamp == stamp &&
                db.UserRoles.Any(ur => ur.UserId == id && db.Roles.Any(r => r.Id == ur.RoleId && r.NormalizedName == "ESTUDIANTE")) &&
                !db.UserRoles.Any(ur => ur.UserId == id && db.Roles.Any(r => r.Id == ur.RoleId &&
                    (r.NormalizedName == "ADMINISTRADOR" || r.NormalizedName == "DIRECTOR" || r.NormalizedName == "CATEDRATICO"))))
            .Select(p => (Guid?)p.Id).SingleOrDefaultAsync(ct);
        return perfil ?? throw new BigFiveAccesoException();
    }

    private IQueryable<SolicitudAtencion> Activas(Guid perfil) => db.SolicitudesAtencion.AsNoTracking()
        .Where(s => s.PerfilEstudianteId == perfil &&
            (s.Estado == EstadoSolicitudAtencion.Pendiente || s.Estado == EstadoSolicitudAtencion.Asignada) &&
            db.ConsentimientosAtencion.Any(c => c.SolicitudAtencionId == s.Id && c.Aceptado) &&
            db.ExpedientesIniciales.Any(e => e.SolicitudAtencionId == s.Id && e.EsMayorEdad && e.Edad >= 18));

    public async Task<EstadoBigFiveViewModel> Estado(ClaimsPrincipal sesion, CancellationToken ct)
    {
        var perfil = await PerfilAutorizado(sesion, ct);
        var activa = await Activas(perfil).Select(s => (Guid?)s.Id).SingleOrDefaultAsync(ct);
        // Proyección explícita: ni puntuaciones ni respuestas se entregan al estudiante.
        var estado = await db.ParticipacionesBigFive.AsNoTracking()
            .Where(p => p.PerfilEstudianteId == perfil && p.VersionInstrumento == Ipip50.Version)
            .Select(p => new EstadoBigFiveViewModel
            {
                Id = p.Id, VersionInstrumento = p.VersionInstrumento, TextoConsentimiento = p.TextoConsentimiento,
                FechaConsentimientoUtc = p.FechaConsentimientoUtc, FechaFinalizacionUtc = p.FechaFinalizacionUtc,
                FechaRetiroConsentimientoUtc = p.FechaRetiroConsentimientoUtc,
                PuedeResponder = p.SolicitudAtencionId == activa && p.FechaFinalizacionUtc == null && p.FechaRetiroConsentimientoUtc == null
            }).SingleOrDefaultAsync(ct) ?? new EstadoBigFiveViewModel();
        estado.TieneSolicitudActiva = activa != null;
        return estado;
    }

    public async Task Aceptar(ClaimsPrincipal sesion, ConsentimientoBigFiveInput input, CancellationToken ct)
    {
        BigFiveReglas.ValidarConsentimiento(input.Acepto, input.MayorDeEdad);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var perfil = await PerfilAutorizado(sesion, ct);
        var solicitud = await Activas(perfil).SingleOrDefaultAsync(ct)
            ?? throw new BigFiveOperacionException("Necesitas una solicitud de atención activa para participar. Big Five sigue siendo opcional.");
        if (!await db.ParticipacionesBigFive.AnyAsync(p => p.PerfilEstudianteId == perfil && p.VersionInstrumento == Ipip50.Version, ct))
        {
            db.ParticipacionesBigFive.Add(new ParticipacionBigFive
            {
                PerfilEstudianteId = perfil, SolicitudAtencionId = solicitud.Id, VersionInstrumento = Ipip50.Version,
                VersionConsentimiento = ConsentimientoBigFive.Version, TextoConsentimiento = ConsentimientoBigFive.Texto,
                FechaConsentimientoUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync(ct);
        }
        await tx.CommitAsync(ct);
    }

    private async Task<ParticipacionBigFive> Propia(ClaimsPrincipal sesion, Guid id, bool requiereActiva, CancellationToken ct)
    {
        var perfil = await PerfilAutorizado(sesion, ct);
        var p = await db.ParticipacionesBigFive.SingleOrDefaultAsync(p => p.Id == id && p.PerfilEstudianteId == perfil &&
            p.VersionInstrumento == Ipip50.Version, ct) ?? throw new BigFiveAccesoException();
        // Comprueba también la relación solicitud/perfil: nunca aceptar identificadores del cliente como autorización.
        var solicitudes = requiereActiva ? Activas(perfil) : db.SolicitudesAtencion.AsNoTracking().Where(s => s.PerfilEstudianteId == perfil);
        if (!await solicitudes.AnyAsync(s => s.Id == p.SolicitudAtencionId, ct)) throw new BigFiveAccesoException();
        return p;
    }

    public async Task<CuestionarioBigFiveViewModel> Cuestionario(ClaimsPrincipal sesion, Guid id, CancellationToken ct)
    {
        var p = await Propia(sesion, id, true, ct);
        BigFiveReglas.Editable(p, p.Revision);
        var respuestas = await db.RespuestasBigFive.AsNoTracking().Where(r => r.ParticipacionBigFiveId == id)
            .ToDictionaryAsync(r => r.ItemId, r => (int?)r.Valor, ct);
        return new CuestionarioBigFiveViewModel { Id = id, Revision = p.Revision, Respuestas = respuestas };
    }

    public async Task Guardar(ClaimsPrincipal sesion, GuardarBigFiveInput input, CancellationToken ct)
    {
        BigFiveReglas.ValidarRespuestas(input.Respuestas);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var p = await Propia(sesion, input.Id, true, ct);
        BigFiveReglas.Editable(p, input.Revision);
        var respuestas = await db.RespuestasBigFive.Where(r => r.ParticipacionBigFiveId == p.Id).ToListAsync(ct);
        foreach (var (item, valor) in input.Respuestas)
        {
            var existente = respuestas.SingleOrDefault(r => r.ItemId == item);
            if (valor == null)
            {
                if (existente != null) db.RespuestasBigFive.Remove(existente);
                continue;
            }
            if (existente == null)
            {
                existente = new RespuestaBigFive { ParticipacionBigFiveId = p.Id, ItemId = item };
                db.RespuestasBigFive.Add(existente);
            }
            existente.Valor = valor.Value;
            existente.FechaActualizacionUtc = DateTime.UtcNow;
        }
        p.Revision++;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task Finalizar(ClaimsPrincipal sesion, FinalizarBigFiveInput input, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var p = await Propia(sesion, input.Id, true, ct);
        var respuestas = await db.RespuestasBigFive.AsNoTracking().Where(r => r.ParticipacionBigFiveId == p.Id).ToListAsync(ct);
        BigFiveReglas.Finalizar(p, input.Revision, input.Confirmado, respuestas, DateTime.UtcNow);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task Retirar(ClaimsPrincipal sesion, Guid id, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        // Puede retirar aunque su solicitud ya no esté activa. No toca solicitud, asignación ni consentimiento de atención.
        var p = await Propia(sesion, id, false, ct);
        BigFiveReglas.Retirar(p, DateTime.UtcNow);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task<ResumenBigFiveViewModel?> Resumen(ClaimsPrincipal sesion, Guid solicitudId, CancellationToken ct)
    {
        // Autorización, consentimiento, lectura y auditoría comparten la misma transacción.
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (!await autorizacion.PuedeConsultarExpedienteAsync(sesion, solicitudId, ct))
            throw new BigFiveAccesoException();
        var resultado = await (from p in db.ParticipacionesBigFive.AsNoTracking()
            join s in db.SolicitudesAtencion.AsNoTracking() on p.SolicitudAtencionId equals s.Id
            join perfil in db.PerfilesEstudiante.AsNoTracking() on p.PerfilEstudianteId equals perfil.Id
            join usuario in db.Users.AsNoTracking() on perfil.UsuarioId equals usuario.Id
            where s.Id == solicitudId && s.PerfilEstudianteId == p.PerfilEstudianteId && perfil.Activo && usuario.IsActive &&
                p.VersionInstrumento == Ipip50.Version && p.FechaFinalizacionUtc != null &&
                p.FechaRetiroConsentimientoUtc == null
            select new { p.FechaFinalizacionUtc, p.VersionInstrumento, p.Apertura, p.Responsabilidad,
                p.Extraversion, p.Amabilidad, p.Neuroticismo }).SingleOrDefaultAsync(ct);
        if (resultado == null) return null;
        var modelo = new ResumenBigFiveViewModel(solicitudId, resultado.VersionInstrumento,
            resultado.FechaFinalizacionUtc!.Value, Ipip50.Dimensiones(new ParticipacionBigFive
            {
                Apertura = resultado.Apertura, Responsabilidad = resultado.Responsabilidad,
                Extraversion = resultado.Extraversion, Amabilidad = resultado.Amabilidad, Neuroticismo = resultado.Neuroticismo
            }));
        db.EventosAccesoClinico.Add(new EventoAccesoClinico
        {
            UsuarioId = sesion.FindFirstValue(ClaimTypes.NameIdentifier)!,
            SolicitudAtencionId = solicitudId, TipoAcceso = "BIGFIVE_CONSULTADO", FechaUtc = DateTime.UtcNow
        });
        // Si SaveChanges o Commit falla, la excepción impide devolver el modelo.
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return modelo;
    }
}
