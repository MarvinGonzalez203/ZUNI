using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models;
using Zuni.Models.Pruebas;

internal static class ContinuidadChecks
{
    public static async Task Run(WebApplication app,DbContextOptions<ApplicationDbContext> options,string clave)
    {
        int checks=0;void Check(bool value,string message){if(!value)throw new Exception("Continuidad: "+message);checks++;}
        await using(var db=new OfflineDb(options)){
            var user=new ApplicationUser{Id="continuidad-alumno",FullName="Alumno ficticio de continuidad",SecurityStamp=Guid.NewGuid().ToString()};
            db.Users.Add(user);db.UserRoles.Add(new(){UserId=user.Id,RoleId=await db.Roles.Where(r=>r.NormalizedName=="ESTUDIANTE").Select(r=>r.Id).SingleAsync()});
            db.PerfilesEstudiante.Add(new(){UsuarioId=user.Id,Carne="9997-26-024680",Activo=true});await db.SaveChangesAsync();
        }
        foreach(var rol in new[]{"Psicologo","Director"}){
            var id="continuidad-"+rol;var email=id+"@miumg.edu.gt";
            await using(var db=new OfflineDb(options)){
                var u=new ApplicationUser{Id=id,FullName="Persona de continuidad",Email=email,NormalizedEmail=email.ToUpperInvariant(),UserName=email,NormalizedUserName=email.ToUpperInvariant(),SecurityStamp=Guid.NewGuid().ToString()};
                u.PasswordHash=new PasswordHasher<ApplicationUser>().HashPassword(u,clave);db.Users.Add(u);db.UserRoles.Add(new(){UserId=id,RoleId=await db.Roles.Where(r=>r.NormalizedName==rol.ToUpperInvariant()).Select(r=>r.Id).SingleAsync()});await db.SaveChangesAsync();
            }
            using var client=HostPrueba.Cliente(app);await HostPrueba.Login(client,email,clave);
            var html=await client.GetStringAsync("/MiCuenta/Editar");
            Check(html.Contains("name=\"Correo\"")&&html.Contains("name=\"Telefono\""),"contacto editable "+rol);
            if(rol=="Psicologo"){
                var preview=Environment.GetEnvironmentVariable("ZUNI_FORMS_PREVIEW_DIR");if(!string.IsNullOrEmpty(preview)){
                    await File.WriteAllTextAsync(Path.Combine(preview,"contacto-psicologo-es.html"),html);
                    await HostPrueba.Post(client,"/MiCuenta/Editar","/Idioma/Cambiar",("idioma","en"),("volver","/MiCuenta/Editar"));
                    await File.WriteAllTextAsync(Path.Combine(preview,"contacto-psicologo-en.html"),await client.GetStringAsync("/MiCuenta/Editar"));
                    await HostPrueba.Post(client,"/MiCuenta/Editar","/Idioma/Cambiar",("idioma","es"),("volver","/MiCuenta/Editar"));
                }
                Check(html.Contains("/PruebasAplicadas/Habilitar")&&html.Contains("Asignar cuestionario"),"asignación visible en menú del psicólogo");
                var home=await client.GetStringAsync("/");Check(home.Contains("home-session-entry")&&home.Contains("href=\"/Psicologo\""),"ingresar desde sesión activa");
            }
            else{
                Guid instrumento;Guid revision;
                await using(var db=new OfflineDb(options)){var p=new Prueba{Nombre="Cuestionario ficticio inactivo"};db.PruebasDirector.Add(p);await db.SaveChangesAsync();instrumento=p.Id;revision=p.Revision;}
                var asignar=await client.GetStringAsync("/PruebasDirector/Asignar/"+instrumento);
                var preview=Environment.GetEnvironmentVariable("ZUNI_FORMS_PREVIEW_DIR");if(!string.IsNullOrEmpty(preview))await File.WriteAllTextAsync(Path.Combine(preview,"catalogo-inactivo.html"),asignar);
                Check(asignar.Contains("name=\"PerfilEstudianteId\"")&&Regex.Matches(asignar,"<option[^>]*value=\"[a-fA-F0-9-]{36}\"").Count>0,"inactivo muestra estudiantes para seleccionar");
                await using(var db=new OfflineDb(options))Check(!await db.PruebasDirector.Where(p=>p.Id==instrumento).Select(p=>p.Activa).SingleAsync(),"consultar no activa el instrumento");
            }
            var revisionCuenta=WebUtility.HtmlDecode(Regex.Match(html,"name=\"Revision\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
            var nuevo="nuevo-"+email;
            var response=await HostPrueba.Post(client,"/MiCuenta/Editar","/MiCuenta/Editar",("Revision",revisionCuenta),("NombreCompleto","Nombre actualizado"),("Correo",nuevo),("Telefono","12345678"),("Roles","SuperAdmin"));
            Check(response.StatusCode==HttpStatusCode.Redirect&&response.Headers.Location?.OriginalString.Contains("IniciarSesion")==true,"correo propio exige iniciar sesión de nuevo");
            await using(var db=new OfflineDb(options)){
                var u=await db.Users.SingleAsync(u=>u.Id==id);Check(u.Email==nuevo&&u.PhoneNumber=="12345678","contacto guardado "+rol);
                Check(await db.UserRoles.CountAsync(r=>r.UserId==id)==1&&!await db.UserRoles.AnyAsync(r=>r.UserId==id&&db.Roles.Any(s=>s.Id==r.RoleId&&s.NormalizedName=="SUPERADMIN")),"editar no cambia permisos");
            }
        }
        await using(var db=new OfflineDb(options)){var institucion=(await db.ConfiguracionInstitucion.FindAsync(1))!;institucion.ColorPrincipal="#F9DB5C";await db.SaveChangesAsync();}
        using(var anonimo=HostPrueba.Cliente(app)){var home=await anonimo.GetStringAsync("/");Check(home.Contains("--institucion-color: #cea2fd"),"home anónimo violeta aunque la institución tenga otro color");}
        Console.WriteLine($"OK: {checks} comprobaciones de contacto, acceso a cuestionarios y portada pública.");
    }
}
