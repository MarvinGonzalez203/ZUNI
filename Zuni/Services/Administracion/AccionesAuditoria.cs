using Microsoft.EntityFrameworkCore;
using Zuni.Data;

namespace Zuni.Services.Administracion;

// Nombres de acción de AuditoriaUsuarios (máximo 50 caracteres por la columna Accion).
public static class AccionesAuditoria
{
    public const string RolCambiado = "ROL_CAMBIADO";
    public const string UsuarioCreado = "USUARIO_CREADO";
    public const string UsuarioDesactivado = "USUARIO_DESACTIVADO";
    public const string UsuarioReactivado = "USUARIO_REACTIVADO";

    // Autorregistro: la cuenta queda inactiva hasta que un Administrador la reactive.
    // AdministradorId = el propio usuario (actor del registro).
    public const string RegistroPendienteActivacion = "REGISTRO_PENDIENTE_ACTIVACION";

    public const string ContrasenaTemporalAsignada = "CONTRASENA_TEMPORAL_ASIGNADA";
    public const string PrimerAdministradorCreado = "PRIMER_ADMIN_CREADO";

    public const string SolicitudAsignada = "SOLICITUD_ASIGNADA";
    public const string SolicitudReasignada = "SOLICITUD_REASIGNADA";
    public const string AsignacionFinalizada = "ASIGNACION_FINALIZADA";
    public const string SolicitudCerrada = "SOLICITUD_CERRADA";
}

// Sin columna nueva: "pendiente de activación" se deriva de IsActive = false y de que el último
// evento de estado de la cuenta sea REGISTRO_PENDIENTE_ACTIVACION. Una cuenta desactivada por un
// Administrador tiene como último evento USUARIO_DESACTIVADO y por eso no se confunde.
public static class EstadoCuenta
{
    public static async Task<bool> EsRegistroPendienteAsync(ApplicationDbContext db, string usuarioId, CancellationToken ct = default)
    {
        var activo = await db.Users.AsNoTracking().Where(u => u.Id == usuarioId).Select(u => (bool?)u.IsActive).SingleOrDefaultAsync(ct);
        if (activo != false) return false;
        var ultima = await db.AuditoriaUsuarios.AsNoTracking()
            .Where(a => a.UsuarioAfectadoId == usuarioId &&
                (a.Accion == AccionesAuditoria.RegistroPendienteActivacion ||
                 a.Accion == AccionesAuditoria.UsuarioDesactivado ||
                 a.Accion == AccionesAuditoria.UsuarioReactivado))
            .OrderByDescending(a => a.FechaUtc)
            .Select(a => a.Accion)
            .FirstOrDefaultAsync(ct);
        return ultima == AccionesAuditoria.RegistroPendienteActivacion;
    }
}
