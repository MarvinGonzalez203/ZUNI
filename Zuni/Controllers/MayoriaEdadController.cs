using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Zuni.Data;
using Zuni.Models.MayoriaEdad;
using Zuni.Services.MayoriaEdad;

namespace Zuni.Controllers;

[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class MayoriaEdadController(ApplicationDbContext db, TimeProvider reloj) : Controller
{
    [HttpGet, Authorize(Roles = "Estudiante")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var perfil = await MayoriaEdadService.PropioAsync(db, User, ct);
        if (perfil is null) return Forbid();
        var registro = await db.VerificacionesMayoriaEdad.AsNoTracking().SingleOrDefaultAsync(v => v.PerfilEstudianteId == perfil.Id, ct);
        return View(new DeclararEdadViewModel { Revision = registro?.Revision ?? Guid.Empty, Constancia = registro });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Estudiante")]
    public async Task<IActionResult> Index(DeclararEdadViewModel input, CancellationToken ct)
    {
        var perfil = await MayoriaEdadService.PropioAsync(db, User, ct);
        if (perfil is null) return Forbid();
        input.Constancia = await db.VerificacionesMayoriaEdad.AsNoTracking().SingleOrDefaultAsync(v => v.PerfilEstudianteId == perfil.Id, ct);
        var error = MayoriaEdadService.ValidarNacimiento(input.FechaNacimiento, MayoriaEdadService.Hoy(reloj));
        if (error is not null) ModelState.AddModelError(nameof(input.FechaNacimiento), error);
        if (!ModelState.IsValid) return View(input);
        try { await MayoriaEdadService.DeclararAsync(db, User, input, reloj, ct); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (MayoriaEdadOperacionException ex) { ModelState.AddModelError("", ex.Message); return View(input); }
        catch (Exception ex) when (ex is DbUpdateException or NpgsqlException)
        { TempData["Error"] = "La declaración cambió o no se pudo guardar. Revisa el estado actual y vuelve a intentarlo."; return RedirectToAction(nameof(Index)); }
        TempData["Success"] = "Mayoría de edad declarada, pendiente de verificación. Coordina la revisión del documento con el Administrador.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet, Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Revisiones(int pagina = 1, CancellationToken ct = default)
    {
        if (!await MayoriaEdadService.ActorAsync(db, User, "Administrador", ct)) return Forbid();
        pagina = Math.Max(1, pagina);
        var query = from v in db.VerificacionesMayoriaEdad.AsNoTracking()
            join p in db.PerfilesEstudiante.AsNoTracking() on v.PerfilEstudianteId equals p.Id
            join u in db.Users.AsNoTracking() on p.UsuarioId equals u.Id
            where p.Activo && u.IsActive
            orderby v.Estado, v.FechaDeclaracionUtc
            select new RevisionEdadResumen(p.Id, u.FullName, u.Email!, v.Estado, v.FechaDeclaracionUtc);
        var total = await query.CountAsync(ct);
        var paginas = Math.Max(1, (int)Math.Ceiling(total / 20d));
        pagina = Math.Min(pagina, paginas);
        ViewData["Pagina"] = pagina;
        ViewData["Paginas"] = paginas;
        return View(await query.Skip((pagina - 1) * 20).Take(20).ToListAsync(ct));
    }

    private async Task<bool> Preparar(RevisarEdadViewModel input, CancellationToken ct)
    {
        if (!await MayoriaEdadService.ActorAsync(db, User, "Administrador", ct)) return false;
        var perfil = await db.PerfilesEstudiante.AsNoTracking().Include(p => p.Usuario).SingleOrDefaultAsync(p => p.Id == input.PerfilEstudianteId && p.Activo && p.Usuario.IsActive, ct);
        if (perfil is null) return false;
        input.Constancia = await db.VerificacionesMayoriaEdad.AsNoTracking().SingleOrDefaultAsync(v => v.PerfilEstudianteId == perfil.Id, ct);
        input.Estudiante = perfil.Usuario.FullName;
        input.Correo = perfil.Usuario.Email ?? "";
        input.Carne = perfil.Carne;
        return input.Constancia is not null;
    }
    [HttpGet, Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Revisar(Guid id, CancellationToken ct)
    {
        var input = new RevisarEdadViewModel { PerfilEstudianteId = id };
        if (!await Preparar(input, ct)) return Forbid();
        input.Revision = input.Constancia!.Revision;
        return View(input);
    }
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Revisar(RevisarEdadViewModel input, CancellationToken ct)
    {
        if (!await Preparar(input, ct)) return Forbid();
        if (!ModelState.IsValid) return View(input);
        try { await MayoriaEdadService.RevisarAsync(db, User, input, reloj, ct); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (MayoriaEdadOperacionException ex) { ModelState.AddModelError("", ex.Message); return View(input); }
        catch (Exception ex) when (ex is DbUpdateException or NpgsqlException)
        { TempData["Error"] = "La revisión cambió o no se pudo guardar. Vuelve a abrirla y revisa el estado actual."; return RedirectToAction(nameof(Revisiones)); }
        TempData["Success"] = input.Aprobada == true ? "Mayoría de edad verificada." : "Mayoría de edad no verificada. El estudiante puede presentar otra declaración para revisión.";
        return RedirectToAction(nameof(Revisiones));
    }
}
