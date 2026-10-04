using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Zuni.Data;
using Zuni.Models;

namespace Zuni.Controllers;

[Authorize(Roles = "Estudiante")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
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

        var perfil = await db.PerfilesEstudiante
            .AsNoTracking()
            .FirstOrDefaultAsync(perfil =>
                perfil.UsuarioId == usuarioId);

        return View(CrearViewModel(perfil));
    }

    [HttpPost("Completar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Completar(
        CompletarPerfilEstudianteViewModel model)
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

        var perfil = await db.PerfilesEstudiante
            .FirstOrDefaultAsync(perfil =>
                perfil.UsuarioId == usuarioId);

        // Estos indicadores son de presentación; nunca se aceptan del navegador.
        model.SolicitarCarne = true;
        model.SolicitarTelefono = true;

        string? carneIngresado = null;

        if (!TryConstruirCarne(
                model.CarneParte1,
                model.CarneParte2,
                model.CarneParte3,
                out carneIngresado))
        {
            ModelState.AddModelError(
                nameof(model.CarneParte1),
                "El carné debe contener 10 dígitos en el formato 0000-00-0000.");
        }

        if (string.IsNullOrWhiteSpace(model.Telefono))
        {
            ModelState.AddModelError(
                nameof(model.Telefono),
                "Ingresa tu teléfono.");
        }

        if (!ModelState.IsValid)
            return View(model);

        var carne = carneIngresado!;

        var carneExiste = await db.PerfilesEstudiante
                .AsNoTracking()
                .AnyAsync(perfilExistente =>
                    perfilExistente.Carne == carne &&
                    perfilExistente.UsuarioId != usuarioId);

        if (carneExiste)
        {
            ModelState.AddModelError(
                nameof(model.CarneParte1),
                "Ya existe un estudiante registrado con este carné.");

            return View(model);
        }

        if (perfil is null)
        {
            perfil = new PerfilEstudiante
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuarioId,
                Carne = carne,
                Telefono = model.Telefono!.Trim(),
                FechaCreacionUtc = DateTime.UtcNow,
                Activo = true
            };

            db.PerfilesEstudiante.Add(perfil);
        }
        else
        {
            perfil.Carne = carne;
            perfil.Telefono = model.Telefono!.Trim();
        }

        perfil.Carrera = model.Carrera.Trim();
        perfil.Semestre = NormalizarOpcional(model.Semestre);
        perfil.CicloAcademico =
            NormalizarOpcional(model.CicloAcademico);
        perfil.NombreContactoEmergencia =
            NormalizarOpcional(model.NombreContactoEmergencia);
        perfil.TelefonoContactoEmergencia =
            NormalizarOpcional(model.TelefonoContactoEmergencia);
        perfil.RelacionContactoEmergencia =
            NormalizarOpcional(model.RelacionContactoEmergencia);

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

            if (postgresException.ConstraintName == "IX_PerfilesEstudiante_UsuarioId")
            {
                ModelState.AddModelError(string.Empty,
                    "Tu perfil fue creado desde otra sesión. Recarga la página antes de guardar nuevamente.");
                return View(model);
            }

            if (postgresException.ConstraintName != "IX_PerfilesEstudiante_Carne")
                throw;

            ModelState.AddModelError(
                nameof(model.CarneParte1),
                "Ya existe un estudiante registrado con este carné.");

            return View(model);
        }

        TempData["Success"] =
            "Tu perfil fue guardado correctamente.";

        return RedirectToAction("MiPerfil", "Estudiante");
    }

    private static CompletarPerfilEstudianteViewModel CrearViewModel(
        PerfilEstudiante? perfil)
    {
        return new CompletarPerfilEstudianteViewModel
        {
            CarneParte1 = perfil?.Carne is { Length: 10 } carne1 ? carne1[..4] : null,
            CarneParte2 = perfil?.Carne is { Length: 10 } carne2 ? carne2[4..6] : null,
            CarneParte3 = perfil?.Carne is { Length: 10 } carne3 ? carne3[6..] : null,
            Telefono = perfil?.Telefono,
            Carrera = perfil?.Carrera ?? string.Empty,
            Semestre = perfil?.Semestre,
            CicloAcademico = perfil?.CicloAcademico,
            NombreContactoEmergencia =
                perfil?.NombreContactoEmergencia,
            TelefonoContactoEmergencia =
                perfil?.TelefonoContactoEmergencia,
            RelacionContactoEmergencia =
                perfil?.RelacionContactoEmergencia,
            SolicitarCarne = true,
            SolicitarTelefono = true
        };
    }

    private static string? NormalizarOpcional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static bool TryConstruirCarne(
        string? parte1,
        string? parte2,
        string? parte3,
        out string carne)
    {
        carne = string.Concat(parte1, parte2, parte3);

        return parte1 is { Length: 4 } &&
               parte2 is { Length: 2 } &&
               parte3 is { Length: 4 } &&
               carne.Length == 10 &&
               carne.All(c => c >= '0' && c <= '9');
    }
}
