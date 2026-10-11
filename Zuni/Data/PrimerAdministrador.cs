using System.Data;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zuni.Helpers;
using Zuni.Models;
using Zuni.Services.Administracion;

namespace Zuni.Data;

// Creación manual del PRIMER Administrador, sin credenciales en código ni en variables de entorno.
//
//   dotnet Zuni.dll --crear-primer-admin=true --correo nombre@miumg.edu.gt --nombre "Nombre Apellido"
//
// - Solo actúa si NO existe ningún Administrador activo; en cualquier otro caso no cambia nada.
// - Genera una contraseña temporal aleatoria, la muestra UNA vez en la consola y guarda solo su hash.
// - La cuenta queda con DebeCambiarContrasena = true.
// - Debe ejecutarse de forma interactiva (no como servicio) para que la contraseña no termine en logs.
public static class PrimerAdministrador
{
    public const string Comando = "--crear-primer-admin";

    // Se usa la forma "--crear-primer-admin=true" para que el proveedor de configuración por línea
    // de comandos no confunda el siguiente argumento con su valor.
    public static bool Solicitado(string[] args) =>
        args.Any(a => a.Equals(Comando, StringComparison.Ordinal) ||
                      a.Equals(Comando + "=true", StringComparison.OrdinalIgnoreCase));

    public static async Task<int> EjecutarAsync(IServiceProvider services, string[] args, TextWriter salida, CancellationToken ct = default)
    {
        var correo = Valor(args, "--correo")?.Trim();
        var nombre = Valor(args, "--nombre")?.Trim();
        if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(nombre) || nombre.Length is < 2 or > 100)
        {
            await salida.WriteLineAsync("Uso: --crear-primer-admin=true --correo nombre@miumg.edu.gt --nombre \"Nombre completo\" (2 a 100 caracteres).");
            return 2;
        }
        var separador = correo.LastIndexOf('@');
        if (separador <= 0 || correo.IndexOf('@') != separador ||
            !correo[(separador + 1)..].Equals("miumg.edu.gt", StringComparison.OrdinalIgnoreCase))
        {
            await salida.WriteLineAsync("El correo debe ser institucional (@miumg.edu.gt).");
            return 2;
        }

        var db = services.GetRequiredService<ApplicationDbContext>();
        var hasher = services.GetRequiredService<IPasswordHasher<ApplicationUser>>();
        var reloj = services.GetService<TimeProvider>() ?? TimeProvider.System;
        var normalizado = correo.ToUpperInvariant();

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var existeAdministrador = await (from u in db.Users
                                         join ur in db.UserRoles on u.Id equals ur.UserId
                                         join r in db.Roles on ur.RoleId equals r.Id
                                         where u.IsActive && r.NormalizedName == "ADMINISTRADOR"
                                         select u.Id).AnyAsync(ct);
        if (existeAdministrador)
        {
            await salida.WriteLineAsync("Ya existe un Administrador activo. No se realizó ningún cambio. Usa el panel de Administrador para crear más cuentas.");
            return 1;
        }
        if (await db.Users.AnyAsync(u => u.NormalizedEmail == normalizado || u.NormalizedUserName == normalizado, ct))
        {
            await salida.WriteLineAsync("Ya existe una cuenta con ese correo. No se realizó ningún cambio.");
            return 1;
        }
        var rol = await db.Roles.SingleOrDefaultAsync(r => r.NormalizedName == "ADMINISTRADOR", ct);
        if (rol is null)
        {
            await salida.WriteLineAsync("No existe el rol Administrador. Verifica que las migraciones estén aplicadas.");
            return 1;
        }

        var ahora = reloj.GetUtcNow().UtcDateTime;
        var usuario = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            FullName = nombre,
            Email = correo,
            NormalizedEmail = normalizado,
            UserName = correo,
            NormalizedUserName = normalizado,
            CreatedAtUtc = ahora,
            IsActive = true,
            DebeCambiarContrasena = true,
            LockoutEnabled = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };
        var temporal = ContrasenaTemporalHelper.Generar();
        usuario.PasswordHash = hasher.HashPassword(usuario, temporal);
        db.Users.Add(usuario);
        db.UserRoles.Add(new IdentityUserRole<string> { UserId = usuario.Id, RoleId = rol.Id });
        db.AuditoriaUsuarios.Add(new AuditoriaUsuario
        {
            UsuarioAfectadoId = usuario.Id,
            AdministradorId = usuario.Id,
            Accion = AccionesAuditoria.PrimerAdministradorCreado,
            DatosNuevos = JsonSerializer.Serialize(new { Roles = new[] { "Administrador" }, IsActive = true, DebeCambiarContrasena = true }),
            FechaUtc = ahora
        });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        await salida.WriteLineAsync("Primer Administrador creado: " + correo);
        await salida.WriteLineAsync("Contraseña temporal (se muestra solo esta vez; cámbiala al iniciar sesión): " + temporal);
        return 0;
    }

    private static string? Valor(string[] args, string nombre)
    {
        var i = Array.IndexOf(args, nombre);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
