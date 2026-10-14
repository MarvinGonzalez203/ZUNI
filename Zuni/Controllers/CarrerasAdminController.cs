using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models;
using Zuni.Services;
using Zuni.Services.MayoriaEdad;
namespace Zuni.Controllers;
[Authorize(Roles="Administrador"),AutoValidateAntiforgeryToken,ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class CarrerasAdminController(ApplicationDbContext db):Controller
{
    [HttpGet] public async Task<IActionResult> Index(CancellationToken ct) {
        if(!await MayoriaEdadService.ActorAsync(db,User,"Administrador",ct))return Forbid();
        return View(await db.CarrerasInstitucionales.AsNoTracking().OrderBy(c=>c.Area).ThenBy(c=>c.Nombre).ToListAsync(ct));
    }
    [HttpPost,RequestSizeLimit(150000)] public async Task<IActionResult> Importar(ImportarCarrerasInput input,CancellationToken ct) {
        if(!await MayoriaEdadService.ActorAsync(db,User,"Administrador",ct))return Forbid();
        try {
            if(!ModelState.IsValid)throw new ArgumentException("Revisa el listado enviado.");
            var texto=input.Listado??"";
            if(input.Archivo is {}f) {
                if(f.Length>100000||!new[]{".txt",".csv"}.Contains(Path.GetExtension(f.FileName).ToLowerInvariant()))throw new ArgumentException("Usa TXT o CSV UTF-8 de hasta 100 KB.");
                using var reader=new StreamReader(f.OpenReadStream(),new System.Text.UTF8Encoding(false,true));texto=await reader.ReadToEndAsync(ct);
            }
            var lista=CatalogoCarreras.Leer(texto);
            await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
            var actuales=await db.CarrerasInstitucionales.ToListAsync(ct);int nuevos=0;
            foreach(var (area,nombre) in lista) {
                var key=CatalogoCarreras.Normalizar(nombre);if(actuales.Any(c=>c.NombreNormalizado==key))continue;
                var c=new CarreraInstitucional{Nombre=nombre,NombreNormalizado=key,Area=area};actuales.Add(c);db.CarrerasInstitucionales.Add(c);nuevos++;
            }
            await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);TempData["Success"]=$"Se agregaron {nuevos} carreras. Las existentes se conservaron.";
        }catch(Exception ex)when(ex is ArgumentException or System.Text.DecoderFallbackException){TempData["Error"]=ex.Message;}
        catch(Exception ex)when(ex is DbUpdateException or Npgsql.NpgsqlException){TempData["Error"]="El catálogo cambió o no pudo guardarse. Recarga y revisa antes de intentarlo de nuevo.";}
        return RedirectToAction(nameof(Index));
    }
    [HttpPost] public async Task<IActionResult> Estado(Guid id,Guid revision,bool activa,CancellationToken ct) {
        if(!await MayoriaEdadService.ActorAsync(db,User,"Administrador",ct))return Forbid();
        var c=await db.CarrerasInstitucionales.SingleOrDefaultAsync(c=>c.Id==id,ct);if(c is null)return NotFound();
        db.Entry(c).Property(c=>c.Revision).OriginalValue=revision;c.Activa=activa;c.Revision=Guid.NewGuid();
        try{await db.SaveChangesAsync(ct);TempData["Success"]=activa?"Carrera habilitada.":"Carrera deshabilitada para nuevas selecciones. Se conservan los perfiles existentes.";}
        catch(DbUpdateConcurrencyException){TempData["Error"]="La carrera cambió. Recarga el catálogo.";}
        return RedirectToAction(nameof(Index));
    }
}
