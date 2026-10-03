using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Zuni.Controllers;

[Authorize(Roles = "Psicologo")]
public sealed class PsicologoController : Controller
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
    public IActionResult Resultados() => View();

    [HttpGet]
    public IActionResult Atencion() => View();

    [HttpGet]
    public IActionResult Historial() => View();
}
