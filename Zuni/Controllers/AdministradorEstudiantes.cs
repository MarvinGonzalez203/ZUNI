using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Data;
using Zuni.Models;
using Zuni.Models.Administrador;
using Zuni.Services;
namespace Zuni.Controllers;
public sealed partial class AdministradorController
{
    [HttpGet]
    public async Task<IActionResult> Tablero(CancellationToken ct) => View(new[] {
        await _db.UserRoles.CountAsync(ur => _db.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Estudiante"),ct),
        await _db.Users.CountAsync(ct) });

    [HttpGet]
    public async Task<IActionResult> Estudiantes(string? busqueda, CancellationToken ct)
    {
        var query = _db.Users.AsNoTracking().Where(u => _db.UserRoles.Any(ur => ur.UserId == u.Id && _db.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Estudiante")));
        if (!string.IsNullOrWhiteSpace(busqueda)) {
            var term = busqueda.Trim().ToLower(); var carne = term.Replace("-", "");
            query = query.Where(u => u.FullName.ToLower().Contains(term) || (u.Email != null && u.Email.ToLower().Contains(term)) || (u.PerfilEstudiante != null && u.PerfilEstudiante.Carne.Replace("-", "").Contains(carne)));
        }
        return View(await query.OrderBy(u => u.FullName).Select(u => new EstudianteAdminViewModel { Id=u.Id, Nombre=u.FullName, Correo=u.Email, Carne=u.PerfilEstudiante == null ? null : u.PerfilEstudiante.Carne, Carrera=u.PerfilEstudiante == null ? null : u.PerfilEstudiante.Carrera, Activo=u.IsActive }).ToListAsync(ct));
    }
    [HttpGet]
    public IActionResult Importar() => View(new ImportacionEstudiantesViewModel());
    [HttpGet]
    public IActionResult PlantillaCsv() => File(Encoding.UTF8.GetBytes("Nombre,Correo,Carne,Carrera\r\n"),"text/csv; charset=utf-8","estudiantes.csv");
    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(300000)]
    public async Task<IActionResult> Importar(IFormFile? archivo, CancellationToken ct)
    {
        if (archivo is null || archivo.Length > 262144) return View(new ImportacionEstudiantesViewModel { Errores = new() { "Selecciona un CSV de hasta 256 KB." } });
        using var reader = new StreamReader(archivo.OpenReadStream(),new UTF8Encoding(false,true),true);
        ImportacionEstudiantesViewModel model;
        try { model = EstudiantesCsv.Leer(await reader.ReadToEndAsync(ct)); }
        catch (DecoderFallbackException) { return View(new ImportacionEstudiantesViewModel { Errores = new() { "El archivo debe estar codificado en UTF-8." } }); }
        await RevisarDuplicados(model,ct);
        return View(model);
    }
    private async Task RevisarDuplicados(ImportacionEstudiantesViewModel model, CancellationToken ct)
    {
        foreach(var row in model.Filas) {
            var email = row.Correo.ToUpperInvariant();
            if(await _db.Users.AnyAsync(u => u.NormalizedEmail == email || (u.Email != null && u.Email.ToUpper() == email),ct)) model.Errores.Add($"Ya existe la cuenta {row.Correo}; no se reemplazará.");
            if(await _db.PerfilesEstudiante.AnyAsync(p => p.Carne.Replace("-", "") == row.Carne,ct)) model.Errores.Add($"El carné {row.Carne} ya está registrado.");
        }
    }
    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(600000)]
    public async Task<IActionResult> ConfirmarImportacion(string? csv, [FromServices] IPasswordHasher<ApplicationUser> hasher, CancellationToken ct)
    {
        var model = EstudiantesCsv.Leer(csv ?? "");
        if(model.Errores.Count > 0) return View("Importar",model);
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        if(!await EsAdministradorActivoAsync(User.FindFirstValue(ClaimTypes.NameIdentifier),ct)) return Forbid();
        await RevisarDuplicados(model,ct);
        if(model.Errores.Count > 0) return View("Importar",model);
        var role = await _db.Roles.SingleAsync(r => r.Name == "Estudiante",ct);
        foreach(var row in model.Filas) {
            var user = new ApplicationUser { Id=Guid.NewGuid().ToString(), FullName=row.Nombre, Email=row.Correo, NormalizedEmail=row.Correo.ToUpperInvariant(), UserName=row.Correo, NormalizedUserName=row.Correo.ToUpperInvariant(), SecurityStamp=Guid.NewGuid().ToString(), ConcurrencyStamp=Guid.NewGuid().ToString(), DebeCambiarContrasena=true };
            user.PasswordHash=hasher.HashPassword(user,Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));
            _db.Users.Add(user);
            _db.UserRoles.Add(new IdentityUserRole<string> { UserId=user.Id,RoleId=role.Id });
            _db.PerfilesEstudiante.Add(new PerfilEstudiante { UsuarioId=user.Id,Carne=row.Carne,Carrera=row.Carrera });
            _db.AuditoriaUsuarios.Add(new AuditoriaUsuario { UsuarioAfectadoId=user.Id,AdministradorId=User.FindFirstValue(ClaimTypes.NameIdentifier)!,Accion="IMPORTACION_CSV",DatosNuevos=JsonSerializer.Serialize(new { Roles=new[]{"Estudiante"}, row.Carne }),Motivo="Registro de estudiante desde CSV" });
        }
        try { await _db.SaveChangesAsync(ct); await tx.CommitAsync(ct); }
        catch(Exception ex) when (ex is DbUpdateException || ex is Npgsql.PostgresException) {
            await tx.RollbackAsync(ct); model.Errores.Add("No se guardó ningún registro. Los datos cambiaron durante la importación; revisa el archivo e inténtalo de nuevo."); return View("Importar",model);
        }
        TempData["Success"]=$"Se registraron {model.Filas.Count} estudiantes.";
        return RedirectToAction(nameof(Estudiantes));
    }
}
