using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models;

namespace Zuni.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;

        public HomeController(
            ILogger<HomeController> logger,
            ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var model = new HomeViewModel
            {
                PrimerNombre = ObtenerPrimerNombre(
                    User.Identity?.Name)
            };

            if (User.Identity?.IsAuthenticated == true &&
                User.IsInRole("Estudiante"))
            {
                var usuarioId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (!string.IsNullOrWhiteSpace(usuarioId))
                {
                    var perfil = await _db.PerfilesEstudiante
                        .AsNoTracking()
                        .SingleOrDefaultAsync(perfil =>
                            perfil.UsuarioId == usuarioId);

                    model = new HomeViewModel
                    {
                        PrimerNombre = model.PrimerNombre,
                        MostrarAvisoCompletarPerfil =
                            perfil is null ||
                            !perfil.EstaCompleto
                    };
                }
            }

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private static string ObtenerPrimerNombre(
            string? nombreCompleto)
        {
            var primerNombre = nombreCompleto?
                .Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(primerNombre))
                return "Usuario";

            return char.ToUpper(
                primerNombre[0],
                System.Globalization.CultureInfo.CurrentCulture) +
                primerNombre[1..].ToLower(
                    System.Globalization.CultureInfo.CurrentCulture);
        }
    }
}
