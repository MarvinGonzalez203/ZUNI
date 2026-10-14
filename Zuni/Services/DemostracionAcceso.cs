using Microsoft.EntityFrameworkCore;
using Zuni.Data;
namespace Zuni.Services;

public static class DemostracionAcceso
{
    public const string Accion = "CUENTA_DEMO_HABILITADA";
    public static Task<bool> Habilitada(ApplicationDbContext db, string? usuarioId, bool desarrollo, CancellationToken ct = default) =>
        !desarrollo || string.IsNullOrEmpty(usuarioId) ? Task.FromResult(false) :
        db.AuditoriaUsuarios.AsNoTracking().AnyAsync(a => a.UsuarioAfectadoId == usuarioId && a.Accion == Accion, ct);
}
