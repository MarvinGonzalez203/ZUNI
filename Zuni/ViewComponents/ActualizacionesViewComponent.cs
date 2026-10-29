using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Resources;
using Zuni.Services.Privacidad;
namespace Zuni.ViewComponents;
public sealed class ActualizacionesViewComponent(ApplicationDbContext db):ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        try {
            var id=await PrivacidadService.UsuarioAsync(db,UserClaimsPrincipal,HttpContext.RequestAborted);
            return View(!await db.LecturasVersion.AsNoTracking().AnyAsync(x=>x.UsuarioId==id&&x.Version==VersionSistema.Actual,HttpContext.RequestAborted));
        } catch(UnauthorizedAccessException){return Content("");}
    }
}
