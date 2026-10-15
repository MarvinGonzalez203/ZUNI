using System.Data;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zuni.Helpers;
using Zuni.Models;

namespace Zuni.Data;

// Operación manual del proveedor; nunca se ejecuta al iniciar el servidor normalmente.
public static class SuperAdministrador
{
    public static bool Solicitado(string[] args) => args.Contains("--preparar-superadmin=true");
    public static async Task<int> EjecutarAsync(IServiceProvider services, string[] args, TextWriter salida, CancellationToken ct=default)
    {
        var i=Array.IndexOf(args,"--correo");
        var correo=i>=0&&i+1<args.Length?args[i+1].Trim():"";
        if(!Zuni.Services.Institucion.PoliticaCorreoReglas.EsCorreoValido(correo))return 2;
        var db=services.GetRequiredService<ApplicationDbContext>();
        var hasher=services.GetRequiredService<IPasswordHasher<ApplicationUser>>();
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var rol=await db.Roles.SingleAsync(r=>r.NormalizedName=="SUPERADMIN",ct);
        var normal=correo.ToUpperInvariant();
        var user=await db.Users.SingleOrDefaultAsync(u=>u.NormalizedEmail==normal,ct);
        if(user is not null&&await db.UserRoles.AnyAsync(ur=>ur.UserId==user.Id&&ur.RoleId!=rol.Id,ct))
        {await salida.WriteLineAsync("Usa una cuenta exclusiva del proveedor, sin roles institucionales. No se modificó la cuenta.");return 2;}
        if(user is not null&&await db.UserRoles.AnyAsync(r=>r.UserId==user.Id&&r.RoleId==rol.Id,ct))
        {await salida.WriteLineAsync("La cuenta ya tiene SuperAdmin. No se cambió su contraseña.");return 1;}
        var ahora=DateTime.UtcNow;
        if(user is null){user=new(){Email=correo,NormalizedEmail=normal,UserName=correo,NormalizedUserName=normal,FullName="Administración ZUNI",CreatedAtUtc=ahora};db.Users.Add(user);}
        var temporal=ContrasenaTemporalHelper.Generar();
        user.PasswordHash=hasher.HashPassword(user,temporal);user.IsActive=true;user.DebeCambiarContrasena=true;
        user.SecurityStamp=Guid.NewGuid().ToString();user.ConcurrencyStamp=Guid.NewGuid().ToString();user.AccessFailedCount=0;user.LockoutEnd=null;user.LockoutEnabled=true;
        db.UserRoles.Add(new(){UserId=user.Id,RoleId=rol.Id});
        await db.PasswordResetTokens.Where(t=>t.UserId==user.Id&&t.UsedAtUtc==null).ExecuteUpdateAsync(s=>s.SetProperty(t=>t.UsedAtUtc,ahora),ct);
        db.AuditoriaUsuarios.Add(new(){UsuarioAfectadoId=user.Id,AdministradorId=user.Id,Accion="SUPERADMIN_PREPARADO_POR_OPERADOR",FechaUtc=ahora,DatosNuevos=JsonSerializer.Serialize(new{Rol="SuperAdmin",DebeCambiarContrasena=true})});
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
        await salida.WriteLineAsync("SuperAdmin preparado: "+correo);
        await salida.WriteLineAsync("Contraseña temporal: "+temporal);
        return 0;
    }
}
