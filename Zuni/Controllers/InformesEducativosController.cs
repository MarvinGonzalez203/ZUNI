using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Zuni.Data;
using Zuni.Models.Informes;
using Zuni.Services;
using Zuni.Services.Informes;
using Zuni.Services.Privacidad;

namespace Zuni.Controllers;

[Authorize, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class InformesEducativosController(ApplicationDbContext db, TimeProvider reloj) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    { try { ViewData["PuedeCrear"] = await new AutorizacionClinicaService(db).ObtenerPsicologoAutorizadoAsync(User, ct) is not null; return View(await InformeEducativoService.ListarAsync(db, User, ct)); } catch (UnauthorizedAccessException) { return Forbid(); } }
    private async Task Preparar(EditarInformeInput input, CancellationToken ct) => input.Destinatarios = await db.Users.AsNoTracking().Where(u => u.IsActive && !u.DebeCambiarContrasena &&
        db.UserRoles.Any(ur => ur.UserId == u.Id && db.Roles.Any(r => r.Id == ur.RoleId && r.NormalizedName == "DIRECTOR")))
        .OrderBy(u => u.FullName).Select(u => new SelectListItem(u.FullName + " · " + u.Email, u.Id)).ToListAsync(ct);
    [Authorize(Roles = "Psicologo"), HttpGet]
    public async Task<IActionResult> Crear(Guid solicitudId, CancellationToken ct)
    {
        if (!await new AutorizacionClinicaService(db).PuedeConsultarExpedienteAsync(User, solicitudId, ct)) return Forbid();
        var input = new EditarInformeInput { SolicitudAtencionId = solicitudId }; await Preparar(input, ct); return View("Editar", input);
    }
    [Authorize(Roles = "Psicologo"), HttpGet]
    public async Task<IActionResult> Editar(Guid id, CancellationToken ct)
    {
        try {
            var d = await InformeEducativoService.DetalleAsync(db, User, id, reloj, ct); if (!d.EsAutor) return Forbid();
            if (d.Version.Estado != EstadoInforme.Borrador) return RedirectToAction("Detalle", new { id });
            var v = d.Version;
            var input = new EditarInformeInput { Id = v.Id, Revision = v.Revision, Objetivo = v.Objetivo, PeriodoInicio = v.PeriodoInicio, PeriodoFin = v.PeriodoFin,
                Fuentes = v.Fuentes, Observaciones = v.Observaciones, Necesidades = v.Necesidades, Recomendaciones = v.Recomendaciones,
                IncluyeAnalisisRendimiento = v.IncluyeAnalisisRendimiento, RelatoRendimiento = v.RelatoRendimiento, ValoracionRendimiento = v.ValoracionRendimiento, ContrasteRendimientoConfirmado = v.ContrasteRendimientoConfirmado,
                ResponsablesAcciones = v.ResponsablesAcciones, FechaSeguimiento = v.FechaSeguimiento, DestinatarioUsuarioId = v.DestinatarioUsuarioId! };
            await Preparar(input, ct); return View(input);
        } catch (UnauthorizedAccessException) { return Forbid(); }
    }
    [Authorize(Roles = "Psicologo"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(EditarInformeInput input, bool solicitar, CancellationToken ct)
    {
        if (await new AutorizacionClinicaService(db).ObtenerPsicologoAutorizadoAsync(User, ct) is null) return Forbid();
        if (!ModelState.IsValid) { await Preparar(input, ct); return View("Editar", input); }
        try { var id = await InformeEducativoService.GuardarAsync(db, User, input, solicitar, reloj, ct); return RedirectToAction("Detalle", new { id }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (DatosOperacionException ex) { ModelState.AddModelError("", ex.Message); await Preparar(input, ct); return View("Editar", input); }
        catch (Exception ex) when (ex is DbUpdateException or NpgsqlException) { TempData["Error"] = "El informe cambió o no se pudo guardar. Revisa la versión actual."; return RedirectToAction("Index"); }
    }
    [HttpGet]
    public async Task<IActionResult> Detalle(Guid id, CancellationToken ct)
    { try { return View(await InformeEducativoService.DetalleAsync(db, User, id, reloj, ct)); } catch (UnauthorizedAccessException) { return Forbid(); } }
    private async Task<IActionResult> Operar(Guid id, Func<Task> action)
    {
        try { await action(); TempData["Success"] = "Operación registrada."; }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (DatosOperacionException ex) { TempData["Error"] = ex.Message; }
        catch (Exception ex) when (ex is DbUpdateException or NpgsqlException) { TempData["Error"] = "La versión cambió o no se pudo guardar. Revisa el estado actual."; }
        return RedirectToAction("Detalle", new { id });
    }
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Estudiante")]
    public Task<IActionResult> Decidir(Guid id, Guid revision, bool? aceptada, bool confirmada, CancellationToken ct) =>
        Operar(id, () => InformeEducativoService.DecidirAsync(db, User, id, revision, aceptada, confirmada, false, reloj, ct));
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Estudiante")]
    public Task<IActionResult> Revocar(Guid id, Guid revision, bool confirmada, CancellationToken ct) =>
        Operar(id, () => InformeEducativoService.DecidirAsync(db, User, id, revision, null, confirmada, true, reloj, ct));
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Psicologo")]
    public Task<IActionResult> Emitir(Guid id, Guid revision, bool confirmada, CancellationToken ct) =>
        Operar(id, () => InformeEducativoService.EmitirAsync(db, User, id, revision, confirmada, reloj, ct));
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Psicologo")]
    public async Task<IActionResult> NuevaVersion(Guid id, Guid revision, CancellationToken ct)
    {
        Guid? nueva = null; var result = await Operar(id, async () => nueva = await InformeEducativoService.NuevaVersionAsync(db, User, id, revision, reloj, ct));
        return nueva.HasValue ? RedirectToAction("Editar", new { id = nueva.Value }) : result;
    }
    [HttpGet]
    public async Task<IActionResult> Pdf(Guid id, CancellationToken ct)
    {
        try {
            var v = await InformeEducativoService.ParaPdfAsync(db, User, id, reloj, ct);
            return File(InformePdf.Crear(v), "application/pdf", $"informe-educativo-{v.InformeId:N}-v{v.Numero}.pdf");
        } catch (UnauthorizedAccessException) { return Forbid(); }
    }
}
