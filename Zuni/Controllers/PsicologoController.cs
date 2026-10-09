using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Zuni.Controllers;

[Authorize(Roles = "Psicologo")]
public sealed class PsicologoController(Zuni.Services.BigFiveService bigFive) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Agenda() => View();

    [HttpGet]
    public IActionResult Estudiantes() => View();

    [HttpGet]
    public async Task<IActionResult> Resultados(CancellationToken ct)
    {
        ViewData["PruebaLocal"] = bigFive.PruebaLocal;
        if (bigFive.PruebaLocal && (HttpContext.Connection.RemoteIpAddress is not { } address || !System.Net.IPAddress.IsLoopback(address))) return StatusCode(403);
        if (!bigFive.Disponible) return View(Array.Empty<Zuni.Models.Evaluaciones.ResumenBigFive>());
        return View(await bigFive.Resumenes(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value, ct));
    }

    [HttpGet]
    public IActionResult Atencion() => View();

    [HttpGet]
    public IActionResult Historial() => View();
}
