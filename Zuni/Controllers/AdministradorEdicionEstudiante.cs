using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using Zuni.Models;
using Zuni.Models.Administrador;
namespace Zuni.Controllers;
public sealed partial class AdministradorController
{
    [HttpGet]
    public async Task<IActionResult> EditarEstudiante(string id,CancellationToken ct)
    {
        var perfil = await _db.PerfilesEstudiante.AsNoTracking().Include(p=>p.Usuario).SingleOrDefaultAsync(p=>p.UsuarioId==id,ct);
        if(perfil is null) return NotFound();
        return View(new EditarEstudianteViewModel { Id=id,Nombre=perfil.Usuario.FullName,Carne=perfil.Carne,Carrera=perfil.Carrera });
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarEstudiante(EditarEstudianteViewModel model,CancellationToken ct)
    {
        if(!ModelState.IsValid) return View(model);
        await using var tx=await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable,ct);
        if(!await EsAdministradorActivoAsync(User.FindFirstValue(ClaimTypes.NameIdentifier),ct)) return Forbid();
        var perfil=await _db.PerfilesEstudiante.Include(p=>p.Usuario).SingleOrDefaultAsync(p=>p.UsuarioId==model.Id,ct);
        if(perfil is null) return NotFound();
        var carne=model.Carne.Replace("-", "");
        if(await _db.PerfilesEstudiante.AnyAsync(p=>p.UsuarioId!=model.Id && p.Carne.Replace("-", "")==carne,ct)) { ModelState.AddModelError(nameof(model.Carne),"Este carné ya está registrado."); return View(model); }
        var before=JsonSerializer.Serialize(new { perfil.Usuario.FullName,perfil.Carne,perfil.Carrera });
        perfil.Usuario.FullName=model.Nombre.Trim(); perfil.Carne=carne; perfil.Carrera=model.Carrera?.Trim();
        _db.AuditoriaUsuarios.Add(new AuditoriaUsuario { UsuarioAfectadoId=model.Id,AdministradorId=User.FindFirstValue(ClaimTypes.NameIdentifier)!,Accion="ESTUDIANTE_EDITADO",DatosAnteriores=before,DatosNuevos=JsonSerializer.Serialize(new { perfil.Usuario.FullName,perfil.Carne,perfil.Carrera }) });
        try { await _db.SaveChangesAsync(ct); await tx.CommitAsync(ct); }
        catch(Exception ex) when(ex is DbUpdateException || ex is Npgsql.PostgresException) { await tx.RollbackAsync(ct); ModelState.AddModelError("","Los datos cambiaron durante la edición. Revisa el carné e inténtalo otra vez."); return View(model); }
        TempData["Success"]="Datos del estudiante guardados."; return RedirectToAction(nameof(Estudiantes));
    }
}
