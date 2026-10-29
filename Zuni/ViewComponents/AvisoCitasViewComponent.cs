using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
namespace Zuni.ViewComponents;
public sealed class AvisoCitasViewComponent(ApplicationDbContext db):ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var id=HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        // Dirección no usa la bandeja general de citas (identifica estudiantes): solo cuenta avisos
        // administrativos de sus propias propuestas y enlaza a Mis propuestas.
        if(Zuni.Services.Agenda.AgendaCitasService.SinAccesoANotificaciones(HttpContext.User))
            return string.IsNullOrEmpty(id)?Content(string.Empty):View("Director",await Zuni.Services.Agenda.AgendaCitasService.AvisosPendientesDirector(db,id,HttpContext.RequestAborted));
        if(!HttpContext.User.IsInRole("Psicologo")&&!HttpContext.User.IsInRole("Estudiante"))return Content(string.Empty);
        var count=await Zuni.Services.Agenda.AgendaCitasService.NotificacionesVisibles(db,HttpContext.User,id).AsNoTracking().CountAsync(n=>!n.Leida);
        return View(count);
    }
}
