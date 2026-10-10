using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Zuni.Data;
using Zuni.Models.Atencion;
namespace Zuni.Services;
public sealed class AsignacionPsicologoService(ApplicationDbContext db) : IAsignacionPsicologoService
{
    public async Task<ResultadoAsignacion> IntentarAsignarAsync(Guid perfilEstudianteId, CancellationToken ct = default)
    {
        // Reintentos acotados; la solicitud ya se confirmó en otra transacción.
        for (var intento = 0; intento < 3; intento++)
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            try
            {
                var elegible = await db.PerfilesEstudiante.AsNoTracking().AnyAsync(p =>
                    p.Id == perfilEstudianteId && p.Activo && p.Usuario.IsActive &&
                    db.UserRoles.Any(ur => ur.UserId == p.UsuarioId &&
                        db.Roles.Any(r => r.Id == ur.RoleId && r.NormalizedName == "ESTUDIANTE")), ct);
                if (!elegible) return ResultadoAsignacion.PerfilNoElegible;
                var vigente = await db.AsignacionesEstudiantePsicologo.AsNoTracking()
                    .AnyAsync(a => a.PerfilEstudianteId == perfilEstudianteId && a.FechaFinalizacionUtc == null, ct);
                if (vigente)
                {
                    await MarcarAsignadaAsync(perfilEstudianteId, ct);
                    await tx.CommitAsync(ct);
                    return ResultadoAsignacion.YaAsignado;
                }
                if (await db.AsignacionesEstudiantePsicologo.AsNoTracking().AnyAsync(a => a.PerfilEstudianteId == perfilEstudianteId, ct))
                    return ResultadoAsignacion.RequiereRevision;
                var psicologos = await db.Users.AsNoTracking().Where(u => u.IsActive &&
                    db.UserRoles.Any(ur => ur.UserId == u.Id && db.Roles.Any(r => r.Id == ur.RoleId && r.NormalizedName == "PSICOLOGO")))
                    .Select(u => u.Id).Take(2).ToListAsync(ct);
                if (psicologos.Count == 0) return ResultadoAsignacion.PendienteSinPsicologo;
                if (psicologos.Count > 1) return ResultadoAsignacion.PendienteMultiplesPsicologos;
                var psicologoId = psicologos.Single();
                // No asignar a una cuenta a la que la política clínica negaría acceso.
                if (await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id
                           where ur.UserId == psicologoId && (r.NormalizedName == "ADMINISTRADOR" ||
                               r.NormalizedName == "DIRECTOR" || r.NormalizedName == "CATEDRATICO")
                           select ur).AnyAsync(ct))
                    return ResultadoAsignacion.RequiereRevision;
                db.AsignacionesEstudiantePsicologo.Add(new AsignacionEstudiantePsicologo
                {
                    PerfilEstudianteId = perfilEstudianteId, PsicologoUsuarioId = psicologoId
                });
                await db.SaveChangesAsync(ct);
                await MarcarAsignadaAsync(perfilEstudianteId, ct);
                await tx.CommitAsync(ct);
                return ResultadoAsignacion.Asignado;
            }
            catch (Exception ex) when (EsConflicto(ex))
            {
                await tx.RollbackAsync(CancellationToken.None);
                db.ChangeTracker.Clear();
                if (intento == 2) return ResultadoAsignacion.ErrorTemporal;
            }
        }
        return ResultadoAsignacion.ErrorTemporal;
    }
    private Task<int> MarcarAsignadaAsync(Guid perfilId, CancellationToken ct) =>
        db.SolicitudesAtencion.Where(s => s.PerfilEstudianteId == perfilId && s.Estado == EstadoSolicitudAtencion.Pendiente)
            .ExecuteUpdateAsync(set => set.SetProperty(s => s.Estado, EstadoSolicitudAtencion.Asignada), ct);

    private static bool EsConflicto(Exception ex)
    {
        var pg = ex as PostgresException ?? ex.InnerException as PostgresException;
        return pg?.SqlState is PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected ||
            (pg?.SqlState == PostgresErrorCodes.UniqueViolation && pg.ConstraintName == "UX_AsignacionesEstudiantePsicologo_Vigente");
    }
}
