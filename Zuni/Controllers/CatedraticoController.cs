using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Zuni.Controllers;

[Authorize(Roles = "Catedratico")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CatedraticoController : Controller
{
    private IActionResult Retirado() { Response.StatusCode = StatusCodes.Status410Gone; return View("~/Views/Cuenta/RolRetirado.cshtml"); }
    public IActionResult Index() => Retirado();
    public IActionResult MisCursos() => Retirado();
    public IActionResult Estudiantes() => Retirado();
    public IActionResult AvanceEvaluaciones() => Retirado();
    public IActionResult ResultadosGenerales() => Retirado();
    public IActionResult Referencias() => Retirado();
}
