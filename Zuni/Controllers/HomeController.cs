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
            if(HttpContext?.RequestServices.GetService<IConfiguration>()?.GetValue<bool>("Hosting:ApplicationOnly")==true)return await Zuni();
            return View("DenisCorp");
        }

        [HttpGet("deniscorp/{producto:regex(^(academy|fulleo|colegios|logistica-transporte)$)}")]
        public IActionResult AbrirProducto(string producto)
        {
            var config=HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var destino=Resources.EnlacesCorporativos.Producto(config,producto);
            return destino is null?View("Producto",model:Resources.PortalDenisCorp.Obtener(producto)):Redirect(destino);
        }

        [HttpGet("deniscorp")]
        public IActionResult DenisCorp() => View();

        [HttpGet("deniscorp/programas/academy")]
        public IActionResult Academy() => View("Producto", model: Resources.PortalDenisCorp.Obtener("academy"));

        [HttpGet("deniscorp/programas/fulleo")]
        public IActionResult Fulleo() => View("Producto", model: Resources.PortalDenisCorp.Obtener("fulleo"));

        [HttpGet("deniscorp/pos")]
        public IActionResult Pos() => View("Producto", model: Resources.PortalDenisCorp.Obtener("pos"));

        [HttpGet("deniscorp/programas/colegios")]
        public IActionResult Colegios() => View("Producto", model: Resources.PortalDenisCorp.Obtener("colegios"));

        [HttpGet("deniscorp/programas/logistica-transporte")]
        public IActionResult Logistica() => View("Producto", model: Resources.PortalDenisCorp.Obtener("logistica-transporte"));

        [HttpGet("deniscorp/blog")]
        public IActionResult Blog(string? q,string? tema) => View(Resources.BlogDenisCorp.Buscar(q,tema));

        [HttpGet("deniscorp/contacto")]
        public IActionResult Contacto(string? programa,string? tema)
        {
            ViewData["ProgramaContacto"]=Resources.PortalDenisCorp.Productos.Any(p=>p.Id==programa)?programa:"general";
            ViewData["TemaContacto"]=tema=="contratacion"?tema:"consulta";
            return View();
        }

        [HttpGet("deniscorp/zuni")]
        public async Task<IActionResult> Zuni()
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

            return View("Zuni",model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Guia(string tema = "cita")
        {
            if(tema is "seguridad" or "apariencia" or "avisos")return View("GuiaExtra",model:tema);
            if (tema is not ("cita" or "seguimiento" or "bienestar" or "privacidad" or "cuenta" or "pruebas" or "informes")) return NotFound();
            return View(model: tema);
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
