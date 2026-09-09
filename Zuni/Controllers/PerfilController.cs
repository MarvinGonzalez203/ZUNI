using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Zuni.Data;
using Zuni.Models;

namespace Zuni.Controllers;

[Authorize(Roles = "Estudiante")]
[Route("Perfil")]
public sealed class PerfilController(ApplicationDbContext db) : Controller
{
    [HttpGet("Completar")]
    public async Task<IActionResult> Completar()
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(usuarioId))
            return Challenge();

        var usuarioExiste = await db.Users
            .AsNoTracking()
            .AnyAsync(usuario =>
                usuario.Id == usuarioId &&
                usuario.IsActive);

        if (!usuarioExiste)
            return Forbid();

        var perfilExiste = await db.PerfilesEstudiante
            .AsNoTracking()
            .AnyAsync(perfil =>
                perfil.UsuarioId == usuarioId);

        if (perfilExiste)
            return RedirectToAction("Index", "Home");

        return View(new CompletarPerfilEstudianteViewModel());
    }

    [HttpPost("Completar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Completar(
        CompletarPerfilEstudianteViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(usuarioId))
            return Challenge();

        var usuarioExiste = await db.Users
            .AsNoTracking()
            .AnyAsync(usuario =>
                usuario.Id == usuarioId &&
                usuario.IsActive);

        if (!usuarioExiste)
            return Forbid();

        var perfilExiste = await db.PerfilesEstudiante
            .AsNoTracking()
            .AnyAsync(perfil =>
                perfil.UsuarioId == usuarioId);

        if (perfilExiste)
            return RedirectToAction("Index", "Home");

        var carne = model.Carne.Trim();
        var carneExiste = await db.PerfilesEstudiante
            .AsNoTracking()
            .AnyAsync(perfil =>
                perfil.Carne == carne);

        if (carneExiste)
        {
            ModelState.AddModelError(
                nameof(model.Carne),
                "Ya existe un estudiante registrado con este carné.");

            return View(model);
        }

        db.PerfilesEstudiante.Add(
            new PerfilEstudiante
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuarioId,
                Carne = carne,
                Carrera = model.Carrera.Trim(),
                Semestre = string.IsNullOrWhiteSpace(model.Semestre)
                    ? null
                    : model.Semestre.Trim(),
                Telefono = string.IsNullOrWhiteSpace(model.Telefono)
                    ? null
                    : model.Telefono.Trim(),
                FechaCreacionUtc = DateTime.UtcNow,
                Activo = true
            });

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
            })
        {
            var postgresException =
                (PostgresException)exception.InnerException;

            if (postgresException.ConstraintName ==
                "IX_PerfilesEstudiante_UsuarioId")
            {
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(
                nameof(model.Carne),
                "Ya existe un estudiante registrado con este carné.");

            return View(model);
        }

        TempData["Success"] =
            "Tu perfil fue completado correctamente.";

        return RedirectToAction("Index", "Home");
    }
}
