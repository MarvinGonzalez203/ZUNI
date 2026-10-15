using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models.Institucion;
namespace Zuni.Services.Institucion;
public static class PreferenciasVistaService
{
    private static readonly string[] Roles=["SuperAdmin","Administrador","Director","Psicologo","Estudiante"];
    public static string? Rol(ClaimsPrincipal user,string? preferido=null)=>Roles.Contains(preferido)&&user.IsInRole(preferido!)?preferido:Roles.FirstOrDefault(user.IsInRole);
    public static async Task<bool> Autoriza(ApplicationDbContext db,ClaimsPrincipal user,string rol,CancellationToken ct){
        var id=user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Roles.Contains(rol)&&user.IsInRole(rol)&&await db.Users.AnyAsync(u=>u.Id==id&&u.IsActive&&!u.DebeCambiarContrasena&&db.UserRoles.Any(ur=>ur.UserId==id&&db.Roles.Any(r=>r.Id==ur.RoleId&&r.Name==rol)),ct);
    }
    public static async Task<ColorPersonalInput> Obtener(ApplicationDbContext db,string id,string rol,string defecto,CancellationToken ct){
        var p=await db.PreferenciasVista.AsNoTracking().SingleOrDefaultAsync(p=>p.UsuarioId==id&&p.Rol==rol,ct);
        return new(){Rol=rol,Revision=p?.Revision??Guid.Empty,ColorPrincipal=p?.Color??defecto};
    }
    public static async Task Guardar(ApplicationDbContext db,ClaimsPrincipal user,ColorPersonalInput input,CancellationToken ct){
        System.ComponentModel.DataAnnotations.Validator.ValidateObject(input,new(input),true);
        await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable,ct);
        if(!await Autoriza(db,user,input.Rol,ct))throw new UnauthorizedAccessException();
        var id=user.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var p=await db.PreferenciasVista.SingleOrDefaultAsync(p=>p.UsuarioId==id&&p.Rol==input.Rol,ct);
        if((p?.Revision??Guid.Empty)!=input.Revision)throw new DbUpdateConcurrencyException();
        if(p is null){p=new(){UsuarioId=id,Rol=input.Rol};db.PreferenciasVista.Add(p);}
        p.Color=input.ColorPrincipal.ToLowerInvariant();p.Revision=Guid.NewGuid();
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
    }
}
