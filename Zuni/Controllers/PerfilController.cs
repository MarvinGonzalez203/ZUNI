using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Zuni.Controllers;
// Conserva enlaces antiguos; la edición y sus permisos se gestionan en MiCuenta.
[Authorize(Roles = "Estudiante")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PerfilController : Controller
{
    [HttpGet]
    public IActionResult Completar() => RedirectToAction("Editar", "MiCuenta");
}
