using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models.Administrador;
using Zuni.Models;
using Zuni.Helpers;
using System.Data;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Npgsql;
using Microsoft.AspNetCore.Identity;

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

    [HttpGet]
    public async Task<IActionResult> Historial(string id, CancellationToken cancellationToken)
    {
        if (!await EsAdministradorActivoAsync(
                User.FindFirstValue(ClaimTypes.NameIdentifier), cancellationToken))
            return Forbid();

        var usuario = await _db.Users.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new { u.Id, u.FullName, u.Email })
            .SingleOrDefaultAsync(cancellationToken);
        if (usuario is null)
            return NotFound();

        var eventos = await _db.AuditoriaUsuarios.AsNoTracking()
            .Where(evento => evento.UsuarioAfectadoId == id)
            .OrderByDescending(evento => evento.FechaUtc)
            .ThenByDescending(evento => evento.Id)
            .Select(evento => new EventoAuditoriaUsuarioViewModel
            {
                Accion = evento.Accion,
                FechaUtc = evento.FechaUtc,
                Motivo = evento.Motivo,
                DatosAnteriores = evento.DatosAnteriores,
                DatosNuevos = evento.DatosNuevos
            })
            .ToListAsync(cancellationToken);

        return View(new HistorialUsuarioViewModel
        {
            UsuarioId = usuario.Id,
            NombreUsuario = usuario.FullName,
            Email = usuario.Email,
            Eventos = eventos
        });
    }

    [HttpGet]
    public async Task<IActionResult> AgregarUsuario(CancellationToken cancellationToken)
    {
        if (!await EsAdministradorActivoAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), cancellationToken))
            return Forbid();
        return View(new AgregarUsuarioViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AgregarUsuario(
        AgregarUsuarioViewModel model,
        [FromServices] IPasswordHasher<ApplicationUser> passwordHasher,
        CancellationToken cancellationToken)
    {
        var administradorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await using var transaction = await _db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        try
        {
            if (!await EsAdministradorActivoAsync(administradorId, cancellationToken))
                return Forbid();
            if (!ModelState.IsValid)
                return MostrarAltaConErrores(model);

            var correo = model.Correo.Trim();
            var normalizado = correo.ToUpperInvariant();
            string? carne = null;
            if (model.Rol == "Estudiante")
            {
                if (!CarneHelper.TryConstruir(model.CarneParte1, model.CarneParte2, model.CarneParte3, out var carneNormalizado))
                {
                    ModelState.AddModelError(nameof(model.CarneParte1), CarneHelper.MensajeFormato);
                    return MostrarAltaConErrores(model);
                }
                carne = carneNormalizado;
            }
            var rol = await _db.Roles.AsNoTracking()
                .SingleOrDefaultAsync(r => r.Name == model.Rol, cancellationToken);
            if (!AgregarUsuarioViewModel.RolesPermitidos.Contains(model.Rol) ||
                rol is null || string.IsNullOrWhiteSpace(rol.NormalizedName))
                ModelState.AddModelError(nameof(model.Rol), "Selecciona un rol permitido que exista en el sistema.");

            if (await _db.Users.AsNoTracking().AnyAsync(u =>
                    u.NormalizedEmail == normalizado || u.NormalizedUserName == normalizado, cancellationToken))
                ModelState.AddModelError(nameof(model.Correo), "Ya existe una cuenta con este correo.");
            if (carne is not null && await _db.PerfilesEstudiante.AsNoTracking()
                    .AnyAsync(p => p.Carne == carne, cancellationToken))
                ModelState.AddModelError(nameof(model.CarneParte1), "Ya existe un estudiante con este carné.");
            if (!ModelState.IsValid)
                return MostrarAltaConErrores(model);

            var usuario = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                FullName = model.NombreCompleto.Trim(),
                Email = correo,
                UserName = correo,
                NormalizedEmail = normalizado,
                NormalizedUserName = normalizado,
                CreatedAtUtc = DateTime.UtcNow,
                IsActive = true,
                DebeCambiarContrasena = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };
            usuario.PasswordHash = passwordHasher.HashPassword(usuario, model.ContrasenaTemporal);
            LimpiarContrasenasAlta(model);

            // El store comparte DbContext y transacción; se guarda todo al final.
            using var store = new UserStore<ApplicationUser>(_db) { AutoSaveChanges = false };
            var resultado = await store.CreateAsync(usuario, cancellationToken);
            if (!resultado.Succeeded)
                throw new InvalidOperationException("No se pudo crear el usuario.");
            await store.AddToRoleAsync(usuario, rol!.NormalizedName!, cancellationToken);

            if (carne is not null)
                _db.PerfilesEstudiante.Add(new PerfilEstudiante
                {
                    UsuarioId = usuario.Id,
                    Usuario = usuario,
                    Carne = carne
                });

            _db.AuditoriaUsuarios.Add(new AuditoriaUsuario
            {
                UsuarioAfectadoId = usuario.Id,
                AdministradorId = administradorId!,
                Accion = "USUARIO_CREADO",
                DatosAnteriores = null,
                DatosNuevos = JsonSerializer.Serialize(new
                {
                    Roles = new[] { model.Rol },
                    IsActive = true,
                    DebeCambiarContrasena = true
                }),
                Motivo = string.IsNullOrWhiteSpace(model.Motivo) ? null : model.Motivo.Trim(),
                FechaUtc = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            TempData["Success"] = "El usuario fue creado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
               { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            await transaction.RollbackAsync(CancellationToken.None);
            _db.ChangeTracker.Clear();
            var restriccion = ((PostgresException)ex.InnerException!).ConstraintName;
            if (restriccion == "IX_PerfilesEstudiante_Carne")
                ModelState.AddModelError(nameof(model.CarneParte1), "Ya existe un estudiante con este carné.");
            else if (restriccion is "UserNameIndex" or "EmailIndex")
                ModelState.AddModelError(nameof(model.Correo), "Ya existe una cuenta con este correo.");
            else
                ModelState.AddModelError(string.Empty, "No se pudo crear el usuario. No se guardaron cambios.");
            return MostrarAltaConErrores(model);
        }
        catch (Exception ex) when (ex is DbUpdateException or NpgsqlException or InvalidOperationException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            _db.ChangeTracker.Clear();
            ModelState.AddModelError(string.Empty, "No se pudo crear el usuario. No se guardaron cambios; vuelve a intentarlo.");
            return MostrarAltaConErrores(model);
        }
        // Toda salida sin Commit revierte la transacción al disponerla.
    }

    private ViewResult MostrarAltaConErrores(AgregarUsuarioViewModel model)
    {
        LimpiarContrasenasAlta(model);
        return View("AgregarUsuario", model);
    }

    private void LimpiarContrasenasAlta(AgregarUsuarioViewModel model)
    {
        model.ContrasenaTemporal = string.Empty;
        model.ConfirmarContrasenaTemporal = string.Empty;
        ModelState.SetModelValue(nameof(model.ContrasenaTemporal), null, null);
        ModelState.SetModelValue(nameof(model.ConfirmarContrasenaTemporal), null, null);
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
