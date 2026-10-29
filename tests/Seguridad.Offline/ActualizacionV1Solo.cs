using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models;
internal static class ActualizacionV1Solo
{
    public static async Task Run()
    {
        await using var connection=new SqliteConnection("Data Source=:memory:");await connection.OpenAsync();
        var options=new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;var reloj=new Reloj();var hasher=new HasherContador();
        var password="Ficticio-"+Guid.NewGuid().ToString("N")+"!";
        await using(var db=new OfflineDb(options)){
            await db.Database.EnsureCreatedAsync();
            foreach(var rol in new[]{"SuperAdmin","Administrador","Director","Psicologo","Estudiante","Catedratico"})db.Roles.Add(new IdentityRole(rol){Id=rol,NormalizedName=rol.ToUpperInvariant()});
            foreach(var (id,correo,rol) in new[]{("admin","admin@miumg.edu.gt","Administrador"),("proveedor","proveedor-ficticio@example.com","SuperAdmin")}){
                var u=new ApplicationUser{Id=id,FullName="Persona ficticia",Email=correo,NormalizedEmail=correo.ToUpperInvariant(),UserName=correo,NormalizedUserName=correo.ToUpperInvariant(),IsActive=true,SecurityStamp=Guid.NewGuid().ToString()};
                u.PasswordHash=hasher.HashPassword(u,password);db.Users.Add(u);db.UserRoles.Add(new(){UserId=id,RoleId=rol});
            }
            await db.SaveChangesAsync();
        }
        await using var app=await HostPrueba.Crear(options,reloj,hasher,30);
        await ActualizacionV1Checks.Run(app,options,password,reloj);await app.StopAsync();
    }
}
