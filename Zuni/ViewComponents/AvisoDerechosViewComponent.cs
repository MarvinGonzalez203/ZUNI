using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models.Privacidad;
using Zuni.Services.MayoriaEdad;
namespace Zuni.ViewComponents;
public sealed class AvisoDerechosViewComponent(ApplicationDbContext db):ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        if(!await MayoriaEdadService.ActorAsync(db,HttpContext.User,"Administrador",HttpContext.RequestAborted))return Content("");
        var pendientes=await db.SolicitudesDerechosDatos.CountAsync(s=>s.Estado==EstadoDerechoDatos.Pendiente,HttpContext.RequestAborted);
        return pendientes==0?Content(""):View(pendientes);
    }
}
