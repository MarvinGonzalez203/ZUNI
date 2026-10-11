using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zuni.Models.Catedratico;

namespace Zuni.Controllers;

[Authorize(Roles = "Catedratico")]
public sealed class CatedraticoController : Controller
{
    public IActionResult Index()
    {
        var model = new DashboardViewModel
        {
            NombreCatedratico = User.Identity?.Name ?? "Catedrático"
        };

        return View(model);
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
}
