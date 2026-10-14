using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models;
using Zuni.Models.Institucion;

namespace Zuni.Services.Institucion;

public static class ConfiguracionInstitucionService
{
    public static async Task<ConfiguracionInstitucion> ObtenerAsync(ApplicationDbContext db, CancellationToken ct = default) =>
        await db.ConfiguracionInstitucion.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, ct) ?? new();

    public static async Task<bool> EsAdministradorAsync(ApplicationDbContext db, string? id, CancellationToken ct) =>
        !string.IsNullOrWhiteSpace(id) && await (from u in db.Users.AsNoTracking()
            join ur in db.UserRoles.AsNoTracking() on u.Id equals ur.UserId
            join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id
            where u.Id == id && u.IsActive && r.NormalizedName == "ADMINISTRADOR"
            select u.Id).AnyAsync(ct);

    public static async Task GuardarAsync(ApplicationDbContext db, ConfiguracionInstitucionViewModel input, string actor, TimeProvider clock, CancellationToken ct)
    {
        var errors = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        if (!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(input, new(input), errors, true))
            throw new ArgumentException("La configuración no es válida.");
        var logo = input.Logotipo is null ? null : await IdentidadInstitucion.LeerPng(input.Logotipo, ct);
        if (input.QuitarLogotipo && logo is not null) throw new ArgumentException("Elige subir o quitar el logotipo, no ambas opciones.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (!await EsAdministradorAsync(db, actor, ct)) throw new UnauthorizedAccessException();
        var config = await db.ConfiguracionInstitucion.SingleOrDefaultAsync(x => x.Id == 1, ct);
        if ((config?.Revision ?? Guid.Empty) != input.Revision) throw new DbUpdateConcurrencyException();
        var before = config is null ? new ConfiguracionInstitucion() : new ConfiguracionInstitucion
        {
            Nombre = config.Nombre, CorreoSoporte = config.CorreoSoporte, Lema = config.Lema, ColorPrincipal = config.ColorPrincipal, LogotipoPng = config.LogotipoPng,
            PoliticaCorreo = config.PoliticaCorreo, DominiosAutorizados = config.DominiosAutorizados
        };
        if (config is null) { config = new(); db.ConfiguracionInstitucion.Add(config); }
        PoliticaCorreoReglas.TryNormalizarDominios(input.DominiosAutorizados, out var domains);
        config.Nombre = input.Nombre.Trim();
        config.Lema = input.Lema.Trim(); config.ColorPrincipal = input.ColorPrincipal.ToLowerInvariant();
        if (input.QuitarLogotipo) config.LogotipoPng = null;
        else if (logo is not null) config.LogotipoPng = logo;
        config.CorreoSoporte = string.IsNullOrWhiteSpace(input.CorreoSoporte) ? null : input.CorreoSoporte.Trim();
        config.PoliticaCorreo = input.PoliticaCorreo;
        config.DominiosAutorizados = string.Join("\n", domains);
        config.Revision = Guid.NewGuid();
        db.AuditoriaUsuarios.Add(new AuditoriaUsuario
        {
            UsuarioAfectadoId = actor, AdministradorId = actor, Accion = "CONFIGURACION_INSTITUCION_CAMBIADA",
            DatosAnteriores = JsonSerializer.Serialize(new { before.Nombre, before.CorreoSoporte, before.PoliticaCorreo, before.DominiosAutorizados, before.Lema, before.ColorPrincipal, Logotipo = IdentidadInstitucion.Huella(before.LogotipoPng) }),
            DatosNuevos = JsonSerializer.Serialize(new { config.Nombre, config.CorreoSoporte, config.PoliticaCorreo, config.DominiosAutorizados, config.Lema, config.ColorPrincipal, Logotipo = IdentidadInstitucion.Huella(config.LogotipoPng) }),
            FechaUtc = clock.GetUtcNow().UtcDateTime
        });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
