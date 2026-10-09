using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Zuni.Data;
using Zuni.Models;

namespace Zuni.Services;

public enum ResultadoAsignacionPsicologo
{
    Asignado,
    YaAsignado,
    PendienteSinPsicologo,
    PendienteMultiplesPsicologos,
    RequiereRevision,
    NoElegible,
    ErrorReintentable
}

public sealed class AsignacionPsicologoService(ApplicationDbContext context)
{
    public const string IndiceAsignacionVigente = "UX_AsignacionesEstudiantePsicologo_PerfilEstudiante_Vigente";

    public async Task<ResultadoAsignacionPsicologo> AsignarAsync(
        Guid perfilEstudianteId, CancellationToken ct = default)
    {
        // Esta operación posee su transacción y no debe guardar cambios del llamador.
        if (context.Database.CurrentTransaction is not null || context.ChangeTracker.HasChanges())
            throw new InvalidOperationException("La asignación requiere un contexto sin cambios pendientes ni transacción activa.");

        AsignacionEstudiantePsicologo? nueva = null;
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var elegible = await context.PerfilesEstudiante.AsNoTracking()
                .AnyAsync(p => p.Id == perfilEstudianteId && p.Activo && p.Usuario.IsActive &&
                    context.UserRoles.Any(ur => ur.UserId == p.UsuarioId &&
                        context.Roles.Any(r => r.Id == ur.RoleId && r.NormalizedName == "ESTUDIANTE")), ct);
            if (!elegible)
                return ResultadoAsignacionPsicologo.NoElegible;

            if (await TieneAsignacionVigenteAsync(perfilEstudianteId, ct))
                return ResultadoAsignacionPsicologo.YaAsignado;

            if (await context.AsignacionesEstudiantePsicologo.AsNoTracking()
                .AnyAsync(a => a.PerfilEstudianteId == perfilEstudianteId, ct))
                return ResultadoAsignacionPsicologo.RequiereRevision;

            var psicologos = await context.Users.AsNoTracking()
                .Where(u => u.IsActive && context.UserRoles.Any(ur => ur.UserId == u.Id &&
                    context.Roles.Any(r => r.Id == ur.RoleId && r.NormalizedName == "PSICOLOGO")))
                .Select(u => u.Id).Distinct().Take(2).ToListAsync(ct);
            if (psicologos.Count == 0)
                return ResultadoAsignacionPsicologo.PendienteSinPsicologo;
            if (psicologos.Count > 1)
                return ResultadoAsignacionPsicologo.PendienteMultiplesPsicologos;

            nueva = new AsignacionEstudiantePsicologo
            {
                PerfilEstudianteId = perfilEstudianteId,
                PsicologoUsuarioId = psicologos[0],
                FechaAsignacionUtc = DateTime.UtcNow
            };
            context.AsignacionesEstudiantePsicologo.Add(nueva);
            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return ResultadoAsignacionPsicologo.Asignado;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: IndiceAsignacionVigente })
        {
            await transaction.RollbackAsync(ct);
            if (nueva is not null) context.Entry(nueva).State = EntityState.Detached;
            // Consultar fuera de la transacción abortada para ver el commit del competidor.
            await transaction.DisposeAsync();
            return await TieneAsignacionVigenteAsync(perfilEstudianteId, ct)
                ? ResultadoAsignacionPsicologo.YaAsignado
                : ResultadoAsignacionPsicologo.ErrorReintentable;
        }
        catch (Exception ex) when (EsConflictoReintentable(ex))
        {
            await transaction.RollbackAsync(ct);
            if (nueva is not null) context.Entry(nueva).State = EntityState.Detached;
            await transaction.DisposeAsync();
            return await TieneAsignacionVigenteAsync(perfilEstudianteId, ct)
                ? ResultadoAsignacionPsicologo.YaAsignado
                : ResultadoAsignacionPsicologo.ErrorReintentable;
        }
        catch
        {
            if (nueva is not null) context.Entry(nueva).State = EntityState.Detached;
            throw;
        }
    }

    private Task<bool> TieneAsignacionVigenteAsync(Guid perfilId, CancellationToken ct) =>
        context.AsignacionesEstudiantePsicologo.AsNoTracking()
            .AnyAsync(a => a.PerfilEstudianteId == perfilId && a.FechaFinalizacionUtc == null, ct);

    private static bool EsConflictoReintentable(Exception exception) =>
        (exception is PostgresException pg ? pg : exception.InnerException as PostgresException)
            is { SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected };
}
