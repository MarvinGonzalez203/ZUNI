using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Zuni.Data;
using Zuni.Models;
using Zuni.Models.Pruebas;

internal static class SuperAdminCatalogoChecks
{
    public static async Task Run(WebApplication app,DbContextOptions<ApplicationDbContext> options,string clave)
    {
        int checks=0;void Check(bool ok,string msg){if(!ok)throw new Exception("SuperAdmin y catálogo: "+msg);checks++;}
        var correo="proveedor-ficticio@example.com";string temporal;string id;
        await using(var db=new OfflineDb(options)){
            await DbInitializer.SeedRolesAsync(db);
            using var services=new ServiceCollection().AddSingleton<ApplicationDbContext>(db).AddSingleton<IPasswordHasher<ApplicationUser>>(new PasswordHasher<ApplicationUser>()).BuildServiceProvider();
            using var salida=new StringWriter();
            Check(await SuperAdministrador.EjecutarAsync(services,["--preparar-superadmin=true","--correo",correo],salida)==0,"provisión manual");
            temporal=salida.ToString().Split("Contraseña temporal: ")[1].Trim();
            var u=await db.Users.SingleAsync(u=>u.Email==correo);id=u.Id;
            Check(u.DebeCambiarContrasena&&u.PasswordHash!=temporal,"temporal solo hash y cambio obligatorio");
            Check((await db.AuditoriaUsuarios.Where(a=>a.UsuarioAfectadoId==id).Select(a=>a.DatosNuevos).SingleAsync())?.Contains(temporal)!=true,"auditoría sin contraseña");
            var hash=u.PasswordHash;Check(await SuperAdministrador.EjecutarAsync(services,["--preparar-superadmin=true","--correo",correo],new StringWriter())==1&&u.PasswordHash==hash,"repetir no resetea contraseña");
        }
        using var super=HostPrueba.Cliente(app);using var admin=HostPrueba.Cliente(app);using var director=HostPrueba.Cliente(app);using var estudiante=HostPrueba.Cliente(app);
        var login=await HostPrueba.Login(super,correo,temporal);Check(login.Headers.Location?.OriginalString.Contains("CambiarContrasenaObligatoria")==true,"primer acceso exige cambio");
        // Solo cuenta ficticia de ensayo: habilitarla con la clave efímera común del host.
        await using(var db=new OfflineDb(options)){var u=await db.Users.FindAsync(id);u!.PasswordHash=new PasswordHasher<ApplicationUser>().HashPassword(u,clave);u.DebeCambiarContrasena=false;await db.SaveChangesAsync();}
        using var activo=HostPrueba.Cliente(app);login=await HostPrueba.Login(activo,correo,clave);
        Check(login.Headers.Location?.OriginalString=="/SuperAdmin","login abre panel proveedor");
        await HostPrueba.Login(admin,"admin@miumg.edu.gt",clave);await HostPrueba.Login(director,"director@miumg.edu.gt",clave);await HostPrueba.Login(estudiante,"est@miumg.edu.gt",clave);
        Check((await activo.GetAsync("/SuperAdmin")).StatusCode==HttpStatusCode.OK,"panel SuperAdmin disponible");
        Check((await admin.GetAsync("/SuperAdmin")).StatusCode==HttpStatusCode.Redirect,"Administrador no entra a proveedor");
        foreach(var path in new[]{"/Administrador","/Psicologo","/Director","/Estudiante"})Check((await activo.GetAsync(path)).StatusCode==HttpStatusCode.Forbidden,"SuperAdmin solo no hereda "+path);
        var tarifasAdmin=WebUtility.HtmlDecode(await admin.GetStringAsync("/Tarifas"));var tarifasSuper=WebUtility.HtmlDecode(await activo.GetStringAsync("/Tarifas"));
        Check(!tarifasAdmin.Contains("PRECIO PROPUESTO")&&tarifasSuper.Contains("PRECIO PROPUESTO"),"leyenda exclusiva del proveedor");
        Check(tarifasAdmin.Contains("Funciones compartidas")&&!tarifasAdmin.Contains("<h3>SuperAdmin"),"beneficios por cuatro roles y funciones comunes");
        Check((await admin.GetAsync("/Administrador/Gestionar/"+id)).Headers.Location?.OriginalString.Contains("AccesoDenegado")==true,"Administrador no gestiona proveedor");
        Check((await HostPrueba.Post(admin,"/Administrador","/Administrador/Desactivar",("UsuarioId",id))).Headers.Location?.OriginalString.Contains("AccesoDenegado")==true,"Administrador no desactiva proveedor");
        Check((await HostPrueba.Post(admin,"/Administrador","/Administrador/CambiarRol",("UsuarioId",id),("NuevoRol","Estudiante"))).Headers.Location?.OriginalString.Contains("AccesoDenegado")==true,"Administrador no retira rol proveedor");
        var add=await HostPrueba.Post(admin,"/CarrerasAdmin","/CarrerasAdmin/AgregarArea",("Area","Área <educativa>"),("Carreras","Derecho de prueba\nCriminología de prueba\nTrabajo social de prueba\nDerecho de prueba"));
        Check(add.StatusCode==HttpStatusCode.Redirect,"carga simple de área");
        await using(var db=new OfflineDb(options))Check(await db.CarrerasInstitucionales.CountAsync(c=>c.Area=="Área <educativa>")==3,"tres carreras en área sin duplicar");
        var catalogo=await admin.GetStringAsync("/CarrerasAdmin");Check(catalogo.Contains("&lt;educativa&gt;")&&!catalogo.Contains("Área <educativa>"),"área codificada");
        Guid prueba;Guid revision;await using(var db=new OfflineDb(options)){var p=new Prueba{Nombre="Borrador para eliminación",Dimensiones=[new(){Nombre="Área"}]};db.PruebasDirector.Add(p);await db.SaveChangesAsync();prueba=p.Id;revision=p.Revision;}
        var url="/PruebasDirector/Detalle/"+prueba;
        Check((await director.GetAsync("/PruebasDirector/Eliminar/"+prueba)).StatusCode==HttpStatusCode.MethodNotAllowed,"GET no elimina");
        Check((await director.PostAsync("/PruebasDirector/Eliminar",new FormUrlEncodedContent([]))).StatusCode==HttpStatusCode.BadRequest,"borrado exige antiforgery");
        await HostPrueba.Post(director,url,"/PruebasDirector/Eliminar",("id",prueba.ToString()),("revision",Guid.NewGuid().ToString()),("confirmar","true"));
        await using(var db=new OfflineDb(options))Check(await db.PruebasDirector.AnyAsync(p=>p.Id==prueba),"revisión obsoleta no elimina");
        await HostPrueba.Post(director,url,"/PruebasDirector/Eliminar",("id",prueba.ToString()),("revision",revision.ToString()),("confirmar","false"));
        await using(var db=new OfflineDb(options))Check(await db.PruebasDirector.AnyAsync(p=>p.Id==prueba),"sin confirmación no elimina");
        await HostPrueba.Post(director,url,"/PruebasDirector/Eliminar",("id",prueba.ToString()),("revision",revision.ToString()),("confirmar","true"));
        await using(var db=new OfflineDb(options)){Check(!await db.PruebasDirector.AnyAsync(p=>p.Id==prueba),"eliminación de borrador personalizado");Check(await db.PruebasDirector.CountAsync(p=>p.Nivel!=null)==3,"tres instrumentos conservados");}
        await using(var db=new OfflineDb(options)){
            var protegida=await db.PruebasDirector.FirstAsync(p=>p.Nivel==1);
            await HostPrueba.Post(director,"/PruebasDirector/Detalle/"+protegida.Id,"/PruebasDirector/Eliminar",("id",protegida.Id.ToString()),("revision",protegida.Revision.ToString()),("confirmar","true"));
            Check(await db.PruebasDirector.AsNoTracking().AnyAsync(p=>p.Id==protegida.Id),"nivel protegido no se elimina");
        }
        var preview=Environment.GetEnvironmentVariable("ZUNI_FORMS_PREVIEW_DIR");if(!string.IsNullOrEmpty(preview)){
            await File.WriteAllTextAsync(Path.Combine(preview,"tarifas-superadmin.html"),tarifasSuper);
            await File.WriteAllTextAsync(Path.Combine(preview,"carreras-simple.html"),catalogo);
            await File.WriteAllTextAsync(Path.Combine(preview,"institucion-paleta.html"),await admin.GetStringAsync("/Institucion"));
            await File.WriteAllTextAsync(Path.Combine(preview,"superadmin.html"),await activo.GetStringAsync("/SuperAdmin"));
            await using var db=new OfflineDb(options);var primer=await db.PruebasDirector.FirstAsync(p=>p.Nivel==1);
            await File.WriteAllTextAsync(Path.Combine(preview,"prueba-director-escala.html"),await director.GetStringAsync("/PruebasDirector/Detalle/"+primer.Id));
        }
        Console.WriteLine($"OK: {checks} comprobaciones de SuperAdmin, catálogo y eliminación.");
    }
}
