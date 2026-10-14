using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Zuni.Data;
using Zuni.Models.Privacidad;
using Zuni.Services.Privacidad;
using Zuni.Services.MayoriaEdad;

namespace Zuni.Controllers;

[Authorize, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PrivacidadController(ApplicationDbContext db, TimeProvider reloj) : Controller
{
    [AllowAnonymous, HttpGet]
    public async Task<IActionResult> Politica(CancellationToken ct) => View(await PrivacidadService.VigenteAsync(db, ct));
    private async Task<PrivacidadViewModel> Modelo(SolicitarDerechoInput? input, CancellationToken ct)
    {
        var id = await PrivacidadService.UsuarioAsync(db, User, ct);
        return new() { Politica = await PrivacidadService.VigenteAsync(db, ct), Aceptada = await PrivacidadService.AceptadaAsync(db, id, ct),
            EsEstudiante = await MayoriaEdadService.ActorAsync(db, User, "Estudiante", ct), Nueva = input ?? new(),
            Solicitudes = await db.SolicitudesDerechosDatos.AsNoTracking().Where(s => s.UsuarioId == id).OrderByDescending(s => s.FechaSolicitudUtc).Take(100).ToListAsync(ct) };
    }
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    { try { return View(await Modelo(null, ct)); } catch (UnauthorizedAccessException) { return Forbid(); } }

    private async Task<IActionResult> Operar(Func<Task> accion, string destino = "Index")
    {
        try { await accion(); TempData["Success"] = "Operación registrada."; }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (DatosOperacionException ex) { TempData["Error"] = ex.Message; }
        catch (Exception ex) when (ex is DbUpdateException or NpgsqlException) { TempData["Error"] = "Los datos cambiaron o no se pudo guardar. Revisa el estado actual antes de volver a intentarlo."; }
        return RedirectToAction(destino);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Aceptar(Guid politicaId, bool confirmada, CancellationToken ct) =>
        Operar(() => PrivacidadService.AceptarAsync(db, User, politicaId, confirmada, reloj, ct));
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Solicitar([Bind(Prefix = "Nueva")] SolicitarDerechoInput input, CancellationToken ct)
    {
        if (!ModelState.IsValid) { try { return View("Index", await Modelo(input, ct)); } catch (UnauthorizedAccessException) { return Forbid(); } }
        return await Operar(() => PrivacidadService.SolicitarAsync(db, User, input, reloj, ct));
    }
    [Authorize(Roles = "Administrador"), HttpGet]
    public async Task<IActionResult> Configurar(CancellationToken ct)
    {
        if (!await MayoriaEdadService.ActorAsync(db, User, "Administrador", ct)) return Forbid();
        var p = await PrivacidadService.VigenteAsync(db, ct);
        return View(p is null ? new PublicarPoliticaInput() : new() { VersionAnteriorId = p.Id, Responsable = p.Responsable, Contacto = p.Contacto,
            Finalidades = p.Finalidades, DatosYAccesos = p.DatosYAccesos, Conservacion = p.Conservacion, ProcedimientoDerechos = p.ProcedimientoDerechos });
    }
    [Authorize(Roles = "Administrador"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Configurar(PublicarPoliticaInput input, CancellationToken ct)
    {
        if (!await MayoriaEdadService.ActorAsync(db, User, "Administrador", ct)) return Forbid();
        if (!ModelState.IsValid) return View(input);
        return await Operar(() => PrivacidadService.PublicarAsync(db, User, input, reloj, ct), "Configurar");
    }
    [Authorize(Roles = "Administrador"), HttpGet]
    public async Task<IActionResult> Solicitudes(int pagina = 1, CancellationToken ct = default)
    {
        if (!await MayoriaEdadService.ActorAsync(db, User, "Administrador", ct)) return Forbid();
        var q = db.SolicitudesDerechosDatos.AsNoTracking().OrderBy(s => s.Estado).ThenBy(s => s.FechaSolicitudUtc);
        var paginas = Math.Max(1, (int)Math.Ceiling(await q.CountAsync(ct) / 20d)); pagina = Math.Clamp(pagina, 1, paginas);
        ViewData["Pagina"] = pagina; ViewData["Paginas"] = paginas;
        return View(await q.Skip((pagina - 1) * 20).Take(20).ToListAsync(ct));
    }
    [Authorize(Roles = "Administrador"), HttpGet]
    public async Task<IActionResult> Responder(Guid id, CancellationToken ct)
    {
        if (!await MayoriaEdadService.ActorAsync(db, User, "Administrador", ct)) return Forbid();
        var s = await db.SolicitudesDerechosDatos.AsNoTracking().SingleOrDefaultAsync(s => s.Id == id, ct);
        if (s is null) return NotFound();
        PrivacidadService.Auditar(db, s.UsuarioId, await PrivacidadService.UsuarioAsync(db, User, ct), "DERECHO_DATOS_CONSULTADO", reloj, id); await db.SaveChangesAsync(ct);
        var usuario = await db.Users.AsNoTracking().SingleAsync(u => u.Id == s.UsuarioId, ct);
        return View(new ResponderDerechoInput { Id = id, Revision = s.Revision, Solicitud = s, Respuesta = s.Respuesta ?? "", Solicitante = usuario.FullName, Correo = usuario.Email ?? "" });
    }
    [Authorize(Roles = "Administrador"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Responder(ResponderDerechoInput input, CancellationToken ct)
    {
        if (!await MayoriaEdadService.ActorAsync(db, User, "Administrador", ct)) return Forbid();
        if (!ModelState.IsValid) {
            input.Solicitud = await db.SolicitudesDerechosDatos.AsNoTracking().SingleOrDefaultAsync(s => s.Id == input.Id, ct);
            if (input.Solicitud is null) return NotFound();
            var usuario = await db.Users.AsNoTracking().SingleAsync(u => u.Id == input.Solicitud.UsuarioId, ct);
            input.Solicitante = usuario.FullName; input.Correo = usuario.Email ?? ""; return View(input);
        }
        return await Operar(() => PrivacidadService.ResponderAsync(db, User, input, reloj, ct), "Solicitudes");
    }
}
