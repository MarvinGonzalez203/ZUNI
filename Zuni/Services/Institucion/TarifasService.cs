using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models;
using Zuni.Models.Institucion;
namespace Zuni.Services.Institucion;

public static class TarifasService
{
    public static Task<bool> EsSuperAdmin(ApplicationDbContext db,string? id,CancellationToken ct)=>db.Users.AnyAsync(u=>u.Id==id&&u.IsActive&&!u.DebeCambiarContrasena&&
        db.UserRoles.Any(ur=>ur.UserId==u.Id&&db.Roles.Any(r=>r.Id==ur.RoleId&&r.NormalizedName=="SUPERADMIN"))&&
        !db.UserRoles.Any(ur=>ur.UserId==u.Id&&db.Roles.Any(r=>r.Id==ur.RoleId&&r.NormalizedName!="SUPERADMIN")),ct);
    public static async Task<EditarTarifasInput> Obtener(ApplicationDbContext db,CancellationToken ct)
    {
        var c=await db.ConfiguracionTarifas.AsNoTracking().SingleOrDefaultAsync(c=>c.Id==1,ct)??new();
        return new(){Revision=c.Revision,Mensual=c.Mensual,Trimestral=c.Trimestral,Semestral=c.Semestral,Anual=c.Anual};
    }
    public static async Task<IReadOnlyList<TarifaSaas>> Catalogo(ApplicationDbContext db,CancellationToken ct)
    {
        var c=await Obtener(db,ct);
        return new[]{new TarifaSaas("Mensual",1,c.Mensual),new("Trimestral",3,c.Trimestral),new("Semestral",6,c.Semestral),new("Anual",12,c.Anual)}.Select(t=>t with{PrecioMensualBase=c.Mensual}).ToList();
    }
    public static async Task<AparienciaProveedorInput> Apariencia(ApplicationDbContext db,CancellationToken ct)
    {
        var c=await db.ConfiguracionTarifas.AsNoTracking().SingleOrDefaultAsync(c=>c.Id==1,ct)??new();
        return new(){Revision=c.Revision,ColorPrincipal=c.ColorProveedor};
    }
    public static async Task Guardar(ApplicationDbContext db,EditarTarifasInput input,string id,TimeProvider reloj,CancellationToken ct)
    {
        System.ComponentModel.DataAnnotations.Validator.ValidateObject(input,new(input),true);
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        if(!await EsSuperAdmin(db,id,ct))throw new UnauthorizedAccessException();
        var c=await db.ConfiguracionTarifas.SingleOrDefaultAsync(c=>c.Id==1,ct);
        if((c?.Revision??Guid.Empty)!=input.Revision)throw new DbUpdateConcurrencyException();
        var anterior=await Obtener(db,ct);
        if(c is null){c=new();db.ConfiguracionTarifas.Add(c);}
        c.Mensual=input.Mensual;c.Trimestral=input.Trimestral;c.Semestral=input.Semestral;c.Anual=input.Anual;c.Revision=Guid.NewGuid();
        db.AuditoriaUsuarios.Add(new(){UsuarioAfectadoId=id,AdministradorId=id,Accion="TARIFAS_COMERCIALES_ACTUALIZADAS",FechaUtc=reloj.GetUtcNow().UtcDateTime,
            DatosAnteriores=JsonSerializer.Serialize(new{anterior.Mensual,anterior.Trimestral,anterior.Semestral,anterior.Anual}),DatosNuevos=JsonSerializer.Serialize(new{c.Mensual,c.Trimestral,c.Semestral,c.Anual})});
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
    }
}
