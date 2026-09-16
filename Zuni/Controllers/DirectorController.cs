using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Zuni.Controllers;

[Authorize(Roles = "Director")]
public sealed class DirectorController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
