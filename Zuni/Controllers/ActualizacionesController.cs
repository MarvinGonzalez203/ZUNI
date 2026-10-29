using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models.Comercial;
using Zuni.Resources;
using Zuni.Services.Privacidad;
namespace Zuni.Controllers;
[Authorize,ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class ActualizacionesController(ApplicationDbContext db,TimeProvider reloj):Controller
{
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Leida(CancellationToken ct)
    {
        try {
            var id=await PrivacidadService.UsuarioAsync(db,User,ct);
            if(!await db.LecturasVersion.AnyAsync(x=>x.UsuarioId==id&&x.Version==VersionSistema.Actual,ct)) {
                db.LecturasVersion.Add(new LecturaVersion{UsuarioId=id,Version=VersionSistema.Actual,FechaUtc=reloj.GetUtcNow().UtcDateTime});
                try {await db.SaveChangesAsync(ct);} catch(DbUpdateException){
                    db.ChangeTracker.Clear();
                    if(!await db.LecturasVersion.AnyAsync(x=>x.UsuarioId==id&&x.Version==VersionSistema.Actual,ct))throw;
                }
            }
            return NoContent();
        }catch(UnauthorizedAccessException){return Forbid();}
    }
}
