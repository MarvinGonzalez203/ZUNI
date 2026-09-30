using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models;

namespace Zuni.Controllers;

[Authorize(Roles = "Director")]
public sealed class DirectorController(ApplicationDbContext db) : Controller
{
    private static readonly string[] RolesAsignables =
    [
        "Psicologo",
        "Catedratico",
        "Estudiante"
    ];

    private static readonly string[] RolesAsignablesNormalizados =
        RolesAsignables
            .Select(nombre => nombre.ToUpperInvariant())
            .ToArray();

    private static readonly HashSet<string> RolesProtegidos =
    [
        "ADMINISTRADOR",
        "DIRECTOR"
    ];

    public async Task<IActionResult> Index()
    {
        var estudianteIds =
            from userRole in db.UserRoles.AsNoTracking()
            join role in db.Roles.AsNoTracking()
                on userRole.RoleId equals role.Id
            where role.NormalizedName == "ESTUDIANTE"
            select userRole.UserId;

        var estudiantesRegistrados = await estudianteIds.CountAsync();

        var perfilesCompletos = await db.PerfilesEstudiante
            .AsNoTracking()
            .Where(perfil =>
                estudianteIds.Contains(perfil.UsuarioId) &&
                !string.IsNullOrWhiteSpace(perfil.Carne) &&
                !string.IsNullOrWhiteSpace(perfil.Telefono) &&
                !string.IsNullOrWhiteSpace(perfil.Carrera))
            .CountAsync();

        var model = new DirectorDashboardViewModel
        {
            EstudiantesRegistrados = estudiantesRegistrados,
            PerfilesCompletos = perfilesCompletos,
            PerfilesIncompletos = Math.Max(
                0,
                estudiantesRegistrados - perfilesCompletos)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Usuarios()
    {
        var usuarios = await db.Users
            .AsNoTracking()
            .OrderBy(user => user.FullName)
            .ThenBy(user => user.Email)
            .Select(user => new
            {
                user.Id,
                user.FullName,
                user.Email
            })
            .ToListAsync();

        var asignaciones = await (
            from userRole in db.UserRoles.AsNoTracking()
            join role in db.Roles.AsNoTracking()
                on userRole.RoleId equals role.Id
            select new
            {
                userRole.UserId,
                role.Name,
                role.NormalizedName
            })
            .ToListAsync();

        var rolesPorUsuario = asignaciones
            .GroupBy(asignacion => asignacion.UserId)
            .ToDictionary(
                grupo => grupo.Key,
                grupo => grupo
                    .Select(asignacion => asignacion.Name ?? "Rol desconocido")
                    .OrderBy(nombre => nombre)
                    .ToArray());

        var rolesProtegidosPorUsuario = asignaciones
            .Where(asignacion =>
                asignacion.NormalizedName != null &&
                RolesProtegidos.Contains(asignacion.NormalizedName))
            .Select(asignacion => asignacion.UserId)
            .ToHashSet();

        var usuarioActualId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        var model = new DirectorUsuariosViewModel
        {
            Usuarios = usuarios
                .Select(usuario => new DirectorUsuarioRolItem
                {
                    Id = usuario.Id,
                    Nombre = string.IsNullOrWhiteSpace(usuario.FullName)
                        ? "Sin nombre"
                        : usuario.FullName,
                    Correo = usuario.Email ?? "Sin correo",
                    Roles = rolesPorUsuario.TryGetValue(
                        usuario.Id,
                        out var roles)
                            ? roles
                            : Array.Empty<string>(),
                    PuedeEditar =
                        usuario.Id != usuarioActualId &&
                        !rolesProtegidosPorUsuario.Contains(usuario.Id)
                })
                .ToArray()
        };

        ViewBag.RolesAsignables = RolesAsignables;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarRol(CambiarRolViewModel model)
    {
        if (!ModelState.IsValid ||
            !RolesAsignables.Contains(model.Rol, StringComparer.Ordinal))
        {
            TempData["Error"] =
                "No se pudo cambiar el tipo de usuario. Revisa la selección e inténtalo de nuevo.";
            return RedirectToAction(nameof(Usuarios));
        }

        var usuario = await db.Users
            .SingleOrDefaultAsync(user => user.Id == model.UsuarioId);

        if (usuario is null)
        {
            TempData["Error"] = "No se encontró la cuenta seleccionada.";
            return RedirectToAction(nameof(Usuarios));
        }

        var usuarioActualId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (usuario.Id == usuarioActualId)
        {
            TempData["Error"] = "No puedes cambiar tu propio tipo de usuario.";
            return RedirectToAction(nameof(Usuarios));
        }

        var roles = await db.Roles
            .Where(role =>
                role.NormalizedName != null &&
                RolesAsignablesNormalizados.Contains(role.NormalizedName))
            .ToListAsync();

        if (roles.Count != RolesAsignables.Length)
        {
            TempData["Error"] =
                "Falta configurar uno o más tipos de usuario. Contacta al administrador del sistema.";
            return RedirectToAction(nameof(Usuarios));
        }

        var rolesActuales = await (
            from userRole in db.UserRoles
            join role in db.Roles
                on userRole.RoleId equals role.Id
            where userRole.UserId == usuario.Id
            select role.NormalizedName)
            .ToListAsync();

        if (rolesActuales.Any(rol =>
                rol != null && RolesProtegidos.Contains(rol)))
        {
            TempData["Error"] =
                "Las cuentas con permisos de Director o Administrador no se pueden cambiar desde esta pantalla.";
            return RedirectToAction(nameof(Usuarios));
        }

        var idsRolesAsignables = roles
            .Select(role => role.Id)
            .ToArray();

        var asignacionesAnteriores = await db.UserRoles
            .Where(userRole =>
                userRole.UserId == usuario.Id &&
                idsRolesAsignables.Contains(userRole.RoleId))
            .ToListAsync();

        db.UserRoles.RemoveRange(asignacionesAnteriores);

        var nuevoRol = roles.Single(role =>
            role.NormalizedName == model.Rol.ToUpperInvariant());

        db.UserRoles.Add(new IdentityUserRole<string>
        {
            UserId = usuario.Id,
            RoleId = nuevoRol.Id
        });

        await db.SaveChangesAsync();

        TempData["Success"] =
            $"Se actualizó el tipo de usuario de {usuario.FullName}. El cambio de acceso se aplicará al refrescar su sesión.";

        return RedirectToAction(nameof(Usuarios));
    }
}
