using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Zuni.Controllers;
[Authorize]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class PanelController : Controller
{
    public IActionResult Index()
    {
        if (!User.IsInRole("Administrador") || !User.IsInRole("Estudiante")) return Forbid();
        return View();
    }
}
