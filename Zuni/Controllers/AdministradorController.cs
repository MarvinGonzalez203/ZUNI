using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models.Administrador;

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
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
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
                // La interfaz muestra un solo rol de forma determinista;
                // no modifica las asignaciones múltiples de Identity.
                Rol = (from asignacion in _db.UserRoles.AsNoTracking()
                       join rol in _db.Roles.AsNoTracking()
                           on asignacion.RoleId equals rol.Id
                       where asignacion.UserId == usuario.Id
                       orderby rol.Name, rol.Id
                       select rol.Name).FirstOrDefault() ?? "Sin rol"
            })
            .ToListAsync(cancellationToken);

        return View(usuarios);
    }
}
