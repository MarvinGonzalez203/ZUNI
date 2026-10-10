using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models.Atencion;

namespace Zuni.ViewComponents;

// Solo etiqueta de navegación; no autoriza operaciones ni carga información clínica.
public sealed class EtiquetaSolicitudViewComponent(ApplicationDbContext db) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var id = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(id) || !HttpContext.User.IsInRole("Estudiante"))
            return Content("Solicitar atención");
        var activa = await db.SolicitudesAtencion.AsNoTracking().AnyAsync(s =>
            db.PerfilesEstudiante.Any(p => p.Id == s.PerfilEstudianteId && p.UsuarioId == id) &&
            (s.Estado == EstadoSolicitudAtencion.Pendiente || s.Estado == EstadoSolicitudAtencion.Asignada),
            HttpContext.RequestAborted);
        return Content(activa ? "Mi solicitud" : "Solicitar atención");
    }
}
