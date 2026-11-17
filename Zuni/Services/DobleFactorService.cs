using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models;
using Zuni.Seguridad;
namespace Zuni.Services;

public sealed class DobleFactorOperacionException(string mensaje) : Exception(mensaje);

public sealed class DobleFactorService(ApplicationDbContext db, IDataProtectionProvider proteccion,
    IPasswordHasher<ApplicationUser> hasher, TimeProvider reloj)
{
    private DateTime Ahora => reloj.GetUtcNow().UtcDateTime;
    private IDataProtector Protector(string id) => proteccion.CreateProtector("Zuni.2FA.v1", id);
    private static string Hash(string codigo) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(codigo)));
    private static bool Igual(string a, string b) => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
    private async Task<DobleFactor> Config(string id)
    {
        var c = await db.Set<DobleFactor>().SingleOrDefaultAsync(x => x.UsuarioId == id);
        if (c is null) { c = new() { UsuarioId = id }; db.Add(c); }
        return c;
    }
    private async Task Guardar(DobleFactor c)
    {
        c.Revision = Guid.NewGuid();
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { db.ChangeTracker.Clear(); throw new DobleFactorOperacionException("La configuración cambió. Actualiza la página e inténtalo de nuevo."); }
    }
    private bool Bloqueado(DobleFactor c) => c.BloqueadoHastaUtc > Ahora;
    private async Task Fallo(DobleFactor c)
    {
        c.Fallos++;
        if (c.Fallos >= 5) { c.Fallos = 0; c.BloqueadoHastaUtc = Ahora.AddMinutes(15); c.RetoHash = null; }
        await Guardar(c);
    }
    private bool Consumir(DobleFactor c, string? codigo)
    {
        if (c.SecretoProtegido is null || codigo is null) return false;
        var paso = TotpZuni.Verificar(Protector(c.UsuarioId).Unprotect(c.SecretoProtegido), codigo, reloj.GetUtcNow(), c.UltimoPaso);
        if (paso.HasValue) { c.UltimoPaso = paso.Value; return true; }
        var hash = Hash(codigo.Trim().ToUpperInvariant());
        var codigos = JsonSerializer.Deserialize<List<string>>(c.CodigosHash) ?? [];
        var indice = codigos.FindIndex(x => Igual(x, hash));
        if (indice < 0) return false;
        codigos.RemoveAt(indice); c.CodigosHash = JsonSerializer.Serialize(codigos); return true;
    }
    private static IReadOnlyList<string> GenerarRespaldo(DobleFactor c)
    {
        var codigos = Enumerable.Range(0, 10).Select(_ => Convert.ToHexString(RandomNumberGenerator.GetBytes(10))).ToArray();
        c.CodigosHash = JsonSerializer.Serialize(codigos.Select(Hash)); return codigos;
    }
    public async Task<DobleFactorVista> Estado(string id)
    {
        var activo = await db.Users.Where(u => u.Id == id).Select(u => u.TwoFactorEnabled).SingleAsync();
        return new(activo);
    }
    // La contraseña y los fallos de 2FA comparten un bloqueo durable independiente del login.
    public async Task<DobleFactorVista> Gestionar(string id, string stamp, DobleFactorInput input, string operacion)
    {
        var u = await db.Users.SingleOrDefaultAsync(u => u.Id == id && u.IsActive && u.SecurityStamp == stamp)
            ?? throw new UnauthorizedAccessException();
        var c = await Config(id);
        DobleFactorVista Error(string texto) => new(u.TwoFactorEnabled, Error: texto);
        if (Bloqueado(c)) return Error("Demasiados intentos. Espera 15 minutos antes de volver a intentarlo.");
        if (hasher.VerifyHashedPassword(u, u.PasswordHash ?? "", input.Contrasena) == PasswordVerificationResult.Failed)
        { await Fallo(c); return Error("No se pudo verificar la contraseña o el código."); }
        if (!u.TwoFactorEnabled && operacion == "Preparar")
        {
            var clave = TotpZuni.NuevaClave(); c.PendienteProtegido = Protector(id).Protect(clave); c.PendienteHastaUtc = Ahora.AddMinutes(10);
            await Guardar(c); return new(false, Clave: clave);
        }
        if (!u.TwoFactorEnabled && operacion == "Activar")
        {
            var paso = c.PendienteProtegido is not null && c.PendienteHastaUtc > Ahora
                ? TotpZuni.Verificar(Protector(id).Unprotect(c.PendienteProtegido), input.Codigo, reloj.GetUtcNow(), -1) : null;
            if (paso is null) { await Fallo(c); return Error("Código incorrecto o configuración vencida. Vuelve a preparar tu autenticador."); }
            c.SecretoProtegido = c.PendienteProtegido; c.UltimoPaso = paso.Value; u.TwoFactorEnabled = true;
        }
        else if (u.TwoFactorEnabled && operacion is "Desactivar" or "Renovar")
        {
            if (!Consumir(c, input.Codigo)) { await Fallo(c); return Error("No se pudo verificar la contraseña o el código. Si acabas de usarlo, espera el siguiente código."); }
            if (operacion == "Desactivar") { u.TwoFactorEnabled = false; c.SecretoProtegido = null; c.CodigosHash = "[]"; }
        }
        else return Error("La configuración cambió. Vuelve a abrir esta página.");
        var codigos = operacion is "Activar" or "Renovar" ? GenerarRespaldo(c) : null;
        c.PendienteProtegido = null; c.PendienteHastaUtc = null; c.RetoHash = null; c.Fallos = 0; c.BloqueadoHastaUtc = null;
        // Cierra las sesiones anteriores al cambiar la protección de la cuenta.
        u.SecurityStamp = Guid.NewGuid().ToString(); u.ConcurrencyStamp = Guid.NewGuid().ToString();
        db.RegistrarSeguridad("CUENTA_2FA_" + operacion.ToUpperInvariant(), id, new { UsuarioId = id, Operacion = operacion });
        await Guardar(c); return new(u.TwoFactorEnabled, Codigos: codigos ?? []);
    }
    public async Task<string?> CrearReto(ApplicationUser u, bool recordar, string? retorno)
    {
        var c = await Config(u.Id);
        if (Bloqueado(c) || c.SecretoProtegido is null) return null;
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        c.RetoHash = Hash(token); c.RetoStamp = u.SecurityStamp; c.RetoHastaUtc = Ahora.AddMinutes(5); c.Recordar = recordar; c.Retorno = retorno;
        await Guardar(c); return token;
    }
    public async Task<(ApplicationUser Usuario, bool Recordar, string? Retorno)?> Completar(string? token, string? codigo)
    {
        if (token is null || token.Length != 64) return null;
        var hash = Hash(token);
        var c = await db.Set<DobleFactor>().SingleOrDefaultAsync(x => x.RetoHash == hash);
        if (c is null || Bloqueado(c) || c.RetoHastaUtc <= Ahora) return null;
        var u = await db.Users.SingleOrDefaultAsync(u => u.Id == c.UsuarioId && u.IsActive && u.TwoFactorEnabled && u.SecurityStamp == c.RetoStamp);
        if (u is null) return null;
        if (!Consumir(c, codigo)) { await Fallo(c); return null; }
        c.RetoHash = null; c.RetoHastaUtc = null; c.Fallos = 0; c.BloqueadoHastaUtc = null;
        await Guardar(c); return (u, c.Recordar, c.Retorno);
    }
}
