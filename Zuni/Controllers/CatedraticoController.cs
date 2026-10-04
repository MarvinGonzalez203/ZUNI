using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Zuni.Controllers;

[Authorize(Roles = "Catedratico")]
public sealed class CatedraticoController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult MisCursos()
    {
        return View();
    }

    public IActionResult Estudiantes()
    {
        return View();
    }

    public IActionResult AvanceEvaluaciones()
    {
        return View();
    }

    public IActionResult ResultadosGenerales()
    {
        return View();
    }
    public IActionResult Referencias()
    {
        return View();
    }

    public IActionResult MiPerfil()
    {
        return View();
    }
}