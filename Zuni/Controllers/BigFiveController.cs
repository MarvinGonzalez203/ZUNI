using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zuni.Models.BigFive;
using Zuni.Services.BigFive;
namespace Zuni.Controllers;

[Authorize(Roles = "Estudiante")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("Estudiante/BigFive")]
public sealed class BigFiveController(BigFiveService service) : Controller
{
    private static bool ErrorPersistencia(Exception ex) => ex is DbUpdateException or Npgsql.NpgsqlException;
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        try { return View(await service.Estado(User, ct)); }
        catch (BigFiveAccesoException) { return Forbid(); }
        catch (Exception ex) when (ErrorPersistencia(ex)) { return NoDisponible(); }
    }

    [HttpPost("Aceptar"), ValidateAntiForgeryToken]
    public Task<IActionResult> Aceptar(ConsentimientoBigFiveInput input, CancellationToken ct) =>
        Ejecutar(() => service.Aceptar(User, input, ct), "Tu consentimiento específico quedó registrado.");

    [HttpGet("Cuestionario/{id:guid}")]
    public async Task<IActionResult> Cuestionario(Guid id, CancellationToken ct)
    {
        try { return View(await service.Cuestionario(User, id, ct)); }
        catch (BigFiveAccesoException) { return Forbid(); }
        catch (BigFiveOperacionException ex) { TempData["BigFiveError"] = ex.Message; return RedirectToAction(nameof(Index)); }
        catch (Exception ex) when (ErrorPersistencia(ex)) { return NoDisponible(); }
    }

    [HttpPost("Guardar"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(GuardarBigFiveInput input, CancellationToken ct)
    {
        if (!ModelState.IsValid) { TempData["BigFiveError"] = "Revisa los valores enviados."; return RedirectToAction(nameof(Index)); }
        try
        {
            await service.Guardar(User, input, ct);
            TempData["BigFiveSuccess"] = "Tu avance quedó guardado.";
            return RedirectToAction(nameof(Cuestionario), new { id = input.Id });
        }
        catch (BigFiveAccesoException) { return Forbid(); }
        catch (BigFiveOperacionException ex) { TempData["BigFiveError"] = ex.Message; }
        catch (Exception ex) when (ErrorPersistencia(ex)) { TempData["BigFiveError"] = "No se pudo confirmar el guardado. Recarga el cuestionario antes de volver a intentarlo."; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Finalizar"), ValidateAntiForgeryToken]
    public Task<IActionResult> Finalizar(FinalizarBigFiveInput input, CancellationToken ct) =>
        Ejecutar(() => service.Finalizar(User, input, ct), "Cuestionario completado.");

    [HttpPost("Retirar"), ValidateAntiForgeryToken]
    public Task<IActionResult> Retirar(Guid id, CancellationToken ct) =>
        Ejecutar(() => service.Retirar(User, id, ct), "Retiraste el consentimiento Big Five. Tu atención no cambia. Los datos registrados no se borran automáticamente.");

    private async Task<IActionResult> Ejecutar(Func<Task> operacion, string mensaje)
    {
        if (!ModelState.IsValid) { TempData["BigFiveError"] = "Revisa los datos enviados."; return RedirectToAction(nameof(Index)); }
        try { await operacion(); TempData["BigFiveSuccess"] = mensaje; }
        catch (BigFiveAccesoException) { return Forbid(); }
        catch (BigFiveOperacionException ex) { TempData["BigFiveError"] = ex.Message; }
        catch (Exception ex) when (ErrorPersistencia(ex)) { TempData["BigFiveError"] = "No se pudo confirmar la operación. Recarga para consultar su estado antes de volver a intentarlo."; }
        return RedirectToAction(nameof(Index));
    }

    private ObjectResult NoDisponible() => StatusCode(503, "Big Five no está disponible temporalmente. Puedes continuar con tu atención habitual.");
}
