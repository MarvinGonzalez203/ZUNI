using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Zuni.Controllers;

[Authorize(Roles = "Psicologo")]
public sealed class PsicologoController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
