using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models.Administrador;
using Zuni.Models;
using System.Data;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Npgsql;

namespace Zuni.Controllers;

[Authorize(Roles = "Administrador")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdministradorController : Controller
{
    private readonly ApplicationDbContext _db;

    public AdministradorController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var usuarios = await _db.Users
            .AsNoTracking()
            .OrderBy(usuario => usuario.FullName)
            .ThenBy(usuario => usuario.Id)
            .Select(usuario => new UsuarioAdminViewModel
            {
                Id = usuario.Id,
                FullName = usuario.FullName,
                Email = usuario.Email,
                IsActive = usuario.IsActive,

                Rol = (
                    from asignacion in _db.UserRoles.AsNoTracking()
                    join rol in _db.Roles.AsNoTracking()
                        on asignacion.RoleId equals rol.Id
                    where asignacion.UserId == usuario.Id
                    orderby rol.Name, rol.Id
                    select rol.Name
                ).FirstOrDefault() ?? "Sin rol"
            })
            .ToListAsync(cancellationToken);

        return View(usuarios);
    }

    [HttpGet]
    public async Task<IActionResult> CambiarRol(
        string id,
        CancellationToken cancellationToken)
    {
        var administradorId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!await EsAdministradorActivoAsync(
                administradorId,
                cancellationToken))
        {
            return Forbid();
        }

        var usuario = await _db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                u => u.Id == id,
                cancellationToken);

        if (usuario is null)
            return NotFound();

        if (!usuario.IsActive)
        {
            TempData["Error"] =
                "Solo puedes cambiar el rol de un usuario activo.";

            return RedirectToAction(nameof(Index));
        }

        using var store =
            new UserStore<ApplicationUser>(_db);

        var roles = await store.GetRolesAsync(
            usuario,
            cancellationToken);

        return View(
            new CambiarRolViewModel
            {
                UsuarioId = usuario.Id,
                NombreUsuario = usuario.FullName,

                RolesActuales = roles.Count == 0
                    ? "Sin rol"
                    : string.Join(
                        ", ",
                        roles.OrderBy(r => r)),

                NuevoRol = roles.Count == 1
                    ? roles[0]
                    : string.Empty
            });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarRol(
        CambiarRolViewModel model,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(model.UsuarioId))
        {
            return BadRequest(
                "Debes seleccionar un usuario.");
        }

        var administradorId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        try
        {
            if (!await EsAdministradorActivoAsync(
                    administradorId,
                    cancellationToken))
            {
                return Forbid();
            }

            using var store =
                new UserStore<ApplicationUser>(_db);

            var usuario =
                await store.FindByIdAsync(
                    model.UsuarioId,
                    cancellationToken);

            if (usuario is null)
                return NotFound();

            var rolesAnteriores =
                await store.GetRolesAsync(
                    usuario,
                    cancellationToken);

            model.NombreUsuario =
                usuario.FullName;

            model.RolesActuales =
                rolesAnteriores.Count == 0
                    ? "Sin rol"
                    : string.Join(
                        ", ",
                        rolesAnteriores.OrderBy(r => r));

            if (!usuario.IsActive)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Solo puedes cambiar el rol de un usuario activo.");
            }

            var rol = await _db.Roles
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    r => r.Name == model.NuevoRol,
                    cancellationToken);

            if (!CambiarRolViewModel.RolesPermitidos
                    .Contains(model.NuevoRol) ||
                rol is null ||
                string.IsNullOrWhiteSpace(
                    rol.NormalizedName))
            {
                ModelState.AddModelError(
                    nameof(model.NuevoRol),
                    "Selecciona un rol permitido que exista en el sistema.");
            }

            if (usuario.Id == administradorId &&
                model.NuevoRol != "Administrador")
            {
                ModelState.AddModelError(
                    string.Empty,
                    "No puedes quitarte a ti mismo el rol Administrador.");
            }

            if (model.NuevoRol != "Administrador" &&
                !await (
                    from u in _db.Users.AsNoTracking()
                    join ur in _db.UserRoles.AsNoTracking()
                        on u.Id equals ur.UserId
                    join r in _db.Roles.AsNoTracking()
                        on ur.RoleId equals r.Id
                    where u.IsActive &&
                          u.Id != usuario.Id &&
                          r.Name == "Administrador"
                    select u.Id
                ).AnyAsync(cancellationToken))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Debe existir al menos un Administrador activo.");
            }

            if (!ModelState.IsValid)
                return View(model);

            if (rolesAnteriores.Count == 1 &&
                rolesAnteriores[0] == model.NuevoRol)
            {
                TempData["Info"] =
                    "El usuario ya tiene ese rol. No se realizó ningún cambio.";

                return RedirectToAction(nameof(Index));
            }

            var rolesAsignados =
                await (
                    from ur in _db.UserRoles
                    join r in _db.Roles
                        on ur.RoleId equals r.Id
                    where ur.UserId == usuario.Id
                    select r
                )
                .ToListAsync(cancellationToken);

            foreach (var anterior in rolesAsignados)
            {
                if (string.IsNullOrWhiteSpace(
                    anterior.NormalizedName))
                {
                    throw new InvalidOperationException(
                        "El rol no tiene nombre normalizado.");
                }

                await store.RemoveFromRoleAsync(
                    usuario,
                    anterior.NormalizedName,
                    cancellationToken);
            }

            // Persistir la eliminación de los roles anteriores.
            await _db.SaveChangesAsync(
                cancellationToken);

            // Asignar el nuevo rol.
            await store.AddToRoleAsync(
                usuario,
                rol!.NormalizedName!,
                cancellationToken);

            // Invalidar todas las sesiones anteriores del usuario.
            // Program.cs comparará este SecurityStamp con el que
            // existe dentro de la cookie.
            usuario.SecurityStamp =
                Guid.NewGuid().ToString();

            var resultado =
                await store.UpdateAsync(
                    usuario,
                    cancellationToken);

            if (!resultado.Succeeded)
            {
                throw new InvalidOperationException(
                    "No se pudo actualizar la asignación de roles.");
            }

            // Registrar la auditoría.
            _db.AuditoriaUsuarios.Add(
                new AuditoriaUsuario
                {
                    UsuarioAfectadoId =
                        usuario.Id,

                    AdministradorId =
                        administradorId!,

                    Accion =
                        "ROL_CAMBIADO",

                    DatosAnteriores =
                        JsonSerializer.Serialize(
                            new
                            {
                                Roles =
                                    rolesAnteriores
                                    .OrderBy(r => r)
                                    .ToArray()
                            }),

                    DatosNuevos =
                        JsonSerializer.Serialize(
                            new
                            {
                                Roles =
                                    new[]
                                    {
                                        model.NuevoRol
                                    }
                            }),

                    Motivo =
                        string.IsNullOrWhiteSpace(
                            model.Motivo)
                            ? null
                            : model.Motivo.Trim(),

                    FechaUtc =
                        DateTime.UtcNow
                });

            await _db.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            TempData["Success"] =
                "El rol del usuario fue actualizado correctamente.";

            return RedirectToAction(
                nameof(Index));
        }
        catch (Exception ex)
            when (ex is DbUpdateException
                or NpgsqlException
                or InvalidOperationException)
        {
            await transaction.RollbackAsync(
                CancellationToken.None);

            _db.ChangeTracker.Clear();

            TempData["Error"] =
                "No se pudo cambiar el rol. " +
                "No se guardaron cambios; vuelve a intentarlo.";

            return RedirectToAction(
                nameof(Index));
        }
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmarEstado(
        string id, bool activar, CancellationToken cancellationToken)
    {
        var administradorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!await EsAdministradorActivoAsync(administradorId, cancellationToken))
            return Forbid();

        var usuario = await _db.Users.AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (usuario is null)
            return NotFound();

        if (usuario.IsActive == activar)
        {
            TempData["Info"] = "El usuario ya tiene el estado solicitado. No se realizó ningún cambio.";
            return RedirectToAction(nameof(Index));
        }

        if (!activar && usuario.Id == administradorId)
        {
            TempData["Error"] = "No puedes desactivarte a ti mismo.";
            return RedirectToAction(nameof(Index));
        }

        return View(new CambiarEstadoUsuarioViewModel
        {
            UsuarioId = usuario.Id,
            NombreUsuario = usuario.FullName,
            EstadoActual = usuario.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Desactivar(
        CambiarEstadoUsuarioViewModel model, CancellationToken cancellationToken) =>
        CambiarEstadoAsync(model, false, cancellationToken);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Reactivar(
        CambiarEstadoUsuarioViewModel model, CancellationToken cancellationToken) =>
        CambiarEstadoAsync(model, true, cancellationToken);

    private async Task<IActionResult> CambiarEstadoAsync(
        CambiarEstadoUsuarioViewModel model, bool activar, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(model.UsuarioId))
            return BadRequest("Debes seleccionar un usuario.");

        var administradorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        // Mismo aislamiento que el cambio de roles: las comprobaciones y escrituras
        // concurrentes no pueden dejar el sistema sin un administrador activo.
        await using var transaction = await _db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        try
        {
            if (!await EsAdministradorActivoAsync(administradorId, cancellationToken))
                return Forbid();

            var usuario = await _db.Users.SingleOrDefaultAsync(
                u => u.Id == model.UsuarioId, cancellationToken);
            if (usuario is null)
                return NotFound();

            // Estos datos siempre se reconstruyen desde PostgreSQL.
            model.NombreUsuario = usuario.FullName;
            model.EstadoActual = usuario.IsActive;

            if (usuario.IsActive == activar)
            {
                TempData["Info"] = "El usuario ya tiene el estado solicitado. No se realizó ningún cambio.";
                return RedirectToAction(nameof(Index));
            }

            if (!activar)
            {
                if (usuario.Id == administradorId)
                    ModelState.AddModelError(string.Empty, "No puedes desactivarte a ti mismo.");

                if (await EsAdministradorActivoAsync(usuario.Id, cancellationToken) &&
                    !await (from u in _db.Users.AsNoTracking()
                            join ur in _db.UserRoles.AsNoTracking() on u.Id equals ur.UserId
                            join r in _db.Roles.AsNoTracking() on ur.RoleId equals r.Id
                            where u.IsActive && u.Id != usuario.Id && r.Name == "Administrador"
                            select u.Id).AnyAsync(cancellationToken))
                {
                    ModelState.AddModelError(string.Empty,
                        "No puedes desactivar al único Administrador activo.");
                }
            }

            if (!ModelState.IsValid)
                return View("ConfirmarEstado", model);

            var estadoAnterior = usuario.IsActive;
            usuario.IsActive = activar;
            usuario.SecurityStamp = Guid.NewGuid().ToString();
            usuario.ConcurrencyStamp = Guid.NewGuid().ToString();

            _db.AuditoriaUsuarios.Add(new AuditoriaUsuario
            {
                UsuarioAfectadoId = usuario.Id,
                AdministradorId = administradorId!,
                Accion = activar ? "USUARIO_REACTIVADO" : "USUARIO_DESACTIVADO",
                DatosAnteriores = JsonSerializer.Serialize(new { IsActive = estadoAnterior }),
                DatosNuevos = JsonSerializer.Serialize(new { IsActive = activar }),
                Motivo = string.IsNullOrWhiteSpace(model.Motivo) ? null : model.Motivo.Trim(),
                FechaUtc = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            TempData["Success"] = activar
                ? "El usuario fue reactivado correctamente."
                : "El usuario fue desactivado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex) when (ex is DbUpdateException or NpgsqlException or InvalidOperationException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            _db.ChangeTracker.Clear();
            TempData["Error"] = "No se pudo cambiar el estado. No se guardaron cambios; vuelve a intentarlo.";
            return RedirectToAction(nameof(Index));
        }
        // DisposeAsync revierte también las salidas sin Commit y excepciones no capturadas.
    }

    private Task<bool> EsAdministradorActivoAsync(
        string? id,
        CancellationToken cancellationToken)
    {
        return (
            from u in _db.Users.AsNoTracking()
            join ur in _db.UserRoles.AsNoTracking()
                on u.Id equals ur.UserId
            join r in _db.Roles.AsNoTracking()
                on ur.RoleId equals r.Id
            where u.Id == id &&
                  u.IsActive &&
                  r.Name == "Administrador"
            select u.Id
        ).AnyAsync(cancellationToken);
    }
}
