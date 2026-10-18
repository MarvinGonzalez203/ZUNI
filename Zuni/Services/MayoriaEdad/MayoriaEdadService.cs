using System.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models;
using Zuni.Models.MayoriaEdad;
using Zuni.Services.Agenda;

namespace Zuni.Services.MayoriaEdad;

public static class MayoriaEdadService
{
    public static DateOnly Hoy(TimeProvider reloj) => DateOnly.FromDateTime(AgendaReglas.Local(reloj.GetUtcNow().UtcDateTime));
    public static int Edad(DateOnly nacimiento, DateOnly hoy)
    {
        var edad = hoy.Year - nacimiento.Year;
        return nacimiento.AddYears(edad) > hoy ? edad - 1 : edad;
    }
    public static string? ValidarNacimiento(DateOnly? nacimiento, DateOnly hoy, bool exigirAdulto = true)
    {
        if (nacimiento is null) return "Indica la fecha de nacimiento.";
        if (nacimiento > hoy) return "La fecha de nacimiento no puede ser futura.";
        var edad = Edad(nacimiento.Value, hoy);
        if (edad > 120) return "Revisa la fecha de nacimiento: la edad no puede superar 120 años.";
        return exigirAdulto && edad < 18 ? "Este servicio admite únicamente a personas de 18 años o más." : null;
    }
    public static Task<bool> VerificadaAsync(ApplicationDbContext db, Guid perfil, CancellationToken ct) =>
        db.VerificacionesMayoriaEdad.AsNoTracking().AnyAsync(v => v.PerfilEstudianteId == perfil && v.Estado == EstadoMayoriaEdad.Verificada, ct);

    public static async Task<bool> ActorAsync(ApplicationDbContext db, ClaimsPrincipal actor, string rol, CancellationToken ct)
    {
        var id = actor.FindFirstValue(ClaimTypes.NameIdentifier);
        var stamp = actor.FindFirstValue("Zuni.SecurityStamp");
        if (actor.Identity?.IsAuthenticated != true || !actor.IsInRole(rol) || string.IsNullOrEmpty(id) || string.IsNullOrEmpty(stamp)) return false;
        var roles = await (from ur in db.UserRoles.AsNoTracking() join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id
            where ur.UserId == id select r.NormalizedName).ToListAsync(ct);
        return roles.Contains(rol.ToUpperInvariant()) && (rol != "Estudiante" || !roles.Any(r => r is "ADMINISTRADOR" or "DIRECTOR" or "CATEDRATICO"))
            && await db.Users.AnyAsync(u => u.Id == id && u.IsActive && !u.DebeCambiarContrasena && u.SecurityStamp == stamp, ct);
    }
    public static async Task<PerfilEstudiante?> PropioAsync(ApplicationDbContext db, ClaimsPrincipal actor, CancellationToken ct) =>
        await ActorAsync(db, actor, "Estudiante", ct)
            ? await db.PerfilesEstudiante.SingleOrDefaultAsync(p => p.UsuarioId == actor.FindFirstValue(ClaimTypes.NameIdentifier) && p.Activo, ct) : null;

    public static async Task DeclararAsync(ApplicationDbContext db, ClaimsPrincipal actor, DeclararEdadViewModel input, TimeProvider reloj, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var perfil = await PropioAsync(db, actor, ct) ?? throw new UnauthorizedAccessException();
        var error = ValidarNacimiento(input.FechaNacimiento, Hoy(reloj));
        if (error is not null) throw new MayoriaEdadOperacionException(error);
        var registro = await db.VerificacionesMayoriaEdad.SingleOrDefaultAsync(v => v.PerfilEstudianteId == perfil.Id, ct);
        if ((registro?.Revision ?? Guid.Empty) != input.Revision) throw new DbUpdateConcurrencyException();
        if (registro?.Estado == EstadoMayoriaEdad.Verificada) throw new MayoriaEdadOperacionException("Tu mayoría de edad ya fue verificada.");
        if (registro is null) { registro = new() { PerfilEstudianteId = perfil.Id }; db.VerificacionesMayoriaEdad.Add(registro); }
        registro.EdadDeclarada = Edad(input.FechaNacimiento!.Value, Hoy(reloj));
        registro.FechaDeclaracionUtc = reloj.GetUtcNow().UtcDateTime;
        registro.Estado = EstadoMayoriaEdad.Pendiente;
        registro.DocumentoRevisado = null;
        registro.EdadVerificada = null;
        registro.ResponsableUsuarioId = null;
        registro.FechaRevisionUtc = null;
        registro.Revision = Guid.NewGuid();
        Auditar(db, perfil.UsuarioId, perfil.UsuarioId, "MAYORIA_EDAD_DECLARADA", reloj);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public static async Task RevisarAsync(ApplicationDbContext db, ClaimsPrincipal actor, RevisarEdadViewModel input, TimeProvider reloj, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (!await ActorAsync(db, actor, "Administrador", ct)) throw new UnauthorizedAccessException();
        var perfil = await db.PerfilesEstudiante.Include(p => p.Usuario).SingleOrDefaultAsync(p => p.Id == input.PerfilEstudianteId && p.Activo
            && db.UserRoles.Any(ur => ur.UserId == p.UsuarioId && db.Roles.Any(r => r.Id == ur.RoleId && r.NormalizedName == "ESTUDIANTE")), ct)
            ?? throw new UnauthorizedAccessException();
        // Permite revisar el documento de un autorregistro todavía pendiente de activación, en la misma
        // ficha. Revisar no activa la cuenta. Las cuentas desactivadas siguen fuera de este flujo.
        if (!await RevisableAsync(db, perfil.Usuario, ct)) throw new UnauthorizedAccessException();
        var registro = await db.VerificacionesMayoriaEdad.SingleOrDefaultAsync(v => v.PerfilEstudianteId == perfil.Id, ct)
            ?? throw new MayoriaEdadOperacionException("El estudiante debe presentar primero su declaración.");
        if (registro.Revision != input.Revision) throw new DbUpdateConcurrencyException();
        if (input.Aprobada is null) throw new MayoriaEdadOperacionException("Selecciona el resultado de la revisión.");
        if (input.Aprobada == true)
        {
            var error = ValidarNacimiento(input.FechaNacimiento, Hoy(reloj));
            if (error is not null) throw new MayoriaEdadOperacionException(error);
            if (!input.DocumentoComprobado || input.Documento is null || !Enum.IsDefined(input.Documento.Value))
                throw new MayoriaEdadOperacionException("Indica el documento y confirma su revisión antes de verificar la mayoría de edad.");
        }
        registro.Estado = input.Aprobada == true ? EstadoMayoriaEdad.Verificada : EstadoMayoriaEdad.Rechazada;
        registro.DocumentoRevisado = input.Aprobada == true ? input.Documento : null;
        registro.EdadVerificada = input.Aprobada == true ? Edad(input.FechaNacimiento!.Value, Hoy(reloj)) : null;
        registro.ResponsableUsuarioId = actor.FindFirstValue(ClaimTypes.NameIdentifier);
        registro.FechaRevisionUtc = reloj.GetUtcNow().UtcDateTime;
        registro.Revision = Guid.NewGuid();
        Auditar(db, perfil.UsuarioId, registro.ResponsableUsuarioId!, input.Aprobada == true ? "MAYORIA_EDAD_VERIFICADA" : "MAYORIA_EDAD_RECHAZADA", reloj);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
    public static async Task<bool> RevisableAsync(ApplicationDbContext db, ApplicationUser usuario, CancellationToken ct) =>
        usuario.IsActive || await Zuni.Services.Administracion.EstadoCuenta.EsRegistroPendienteAsync(db, usuario.Id, ct);
    private static void Auditar(ApplicationDbContext db, string estudiante, string actor, string accion, TimeProvider reloj) =>
        db.AuditoriaUsuarios.Add(new AuditoriaUsuario { UsuarioAfectadoId = estudiante, AdministradorId = actor, Accion = accion, FechaUtc = reloj.GetUtcNow().UtcDateTime });
}
