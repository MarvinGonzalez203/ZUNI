using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
namespace Zuni.Services;
public sealed class AutorizacionClinicaService(ApplicationDbContext db) : IAutorizacionClinicaService
{
    public async Task<string?> ObtenerPsicologoAutorizadoAsync(ClaimsPrincipal sesion, CancellationToken ct = default)
    {
        if (sesion.Identity?.IsAuthenticated != true || !sesion.IsInRole("Psicologo")) return null;
        var id = sesion.FindFirstValue(ClaimTypes.NameIdentifier);
        var stamp = sesion.FindFirstValue("Zuni.SecurityStamp");
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(stamp)) return null;
        if (!await db.Users.AsNoTracking().AnyAsync(u => u.Id == id && u.IsActive &&
                !u.DebeCambiarContrasena && u.SecurityStamp == stamp, ct)) return null;
        var roles = await (from ur in db.UserRoles.AsNoTracking() join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id
                           where ur.UserId == id select r.NormalizedName).ToListAsync(ct);
        // Regla explícita: los roles institucionales nunca reciben contenido clínico individual,
        // incluso si la misma cuenta tiene también Psicologo.
        if (!roles.Contains("PSICOLOGO") || roles.Any(r => r is "ADMINISTRADOR" or "DIRECTOR" or "CATEDRATICO"))
            return null;
        return id;
    }
    public async Task<bool> PuedeConsultarExpedienteAsync(ClaimsPrincipal sesion, Guid solicitudId, CancellationToken ct = default)
    {
        var id = await ObtenerPsicologoAutorizadoAsync(sesion, ct);
        if (id is null) return false;
        return await db.SolicitudesAtencion.AsNoTracking().AnyAsync(s => s.Id == solicitudId &&
            db.ConsentimientosAtencion.Any(c => c.SolicitudAtencionId == s.Id && c.Aceptado) &&
            db.AsignacionesEstudiantePsicologo.Any(a => a.PerfilEstudianteId == s.PerfilEstudianteId &&
                a.PsicologoUsuarioId == id && a.FechaFinalizacionUtc == null), ct);
    }
}
