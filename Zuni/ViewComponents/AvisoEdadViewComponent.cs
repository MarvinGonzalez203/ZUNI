using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models.MayoriaEdad;
using Zuni.Services.MayoriaEdad;
namespace Zuni.ViewComponents;
public sealed class AvisoEdadViewComponent(ApplicationDbContext db) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!await MayoriaEdadService.ActorAsync(db, HttpContext.User, "Administrador", HttpContext.RequestAborted)) return Content("");
        var count = await db.VerificacionesMayoriaEdad.CountAsync(v => v.Estado == EstadoMayoriaEdad.Pendiente &&
            db.PerfilesEstudiante.Any(p => p.Id == v.PerfilEstudianteId && p.Activo && p.Usuario.IsActive), HttpContext.RequestAborted);
        return View(count);
    }
}
