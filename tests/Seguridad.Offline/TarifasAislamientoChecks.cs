using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Zuni.Data;
using Zuni.Models;
using Zuni.Models.Institucion;
using Zuni.Services.Institucion;

internal static class TarifasAislamientoChecks
{
    public static async Task Run(WebApplication app,DbContextOptions<ApplicationDbContext> options,string clave,Reloj reloj)
    {
        int checks=0;void Check(bool ok,string msg){if(!ok)throw new Exception("Tarifas y aislamiento: "+msg);checks++;}
        using var proveedor=HostPrueba.Cliente(app);using var admin=HostPrueba.Cliente(app);using var anon=HostPrueba.Cliente(app);
        await HostPrueba.Login(proveedor,"proveedor-ficticio@example.com",clave);await HostPrueba.Login(admin,"admin@miumg.edu.gt",clave);
        var panel=await proveedor.GetStringAsync("/SuperAdmin");
        Check(!panel.Contains("/CitasProgramadas/Notificaciones")&&!panel.Contains("/MayoriaEdad/Revisiones")&&!panel.Contains("/Administrador/Estudiantes"),"panel sin accesos ni alertas institucionales");
        string id;
        await using(var db=new OfflineDb(options)){
            id=await db.Users.Where(u=>u.Email=="proveedor-ficticio@example.com").Select(u=>u.Id).SingleAsync();
            using var services=new ServiceCollection().AddSingleton<ApplicationDbContext>(db).AddSingleton<IPasswordHasher<ApplicationUser>>(new PasswordHasher<ApplicationUser>()).BuildServiceProvider();
            var anterior=(await db.Users.SingleAsync(u=>u.Email=="admin@miumg.edu.gt")).PasswordHash;
            Check(await SuperAdministrador.EjecutarAsync(services,["--preparar-superadmin=true","--correo","admin@miumg.edu.gt"],new StringWriter())==2,"operador no combina proveedor con Administrador");
            Check((await db.Users.SingleAsync(u=>u.Email=="admin@miumg.edu.gt")).PasswordHash==anterior,"rechazo conserva contraseña institucional");
        }
        foreach(var url in new[]{"/Administrador/Estudiantes","/Psicologo/Solicitudes","/Director","/Estudiante/Citas","/CitasProgramadas","/InformesEducativos","/PruebasAplicadas","/PruebasDirector","/Privacidad/Solicitudes","/CarrerasAdmin","/Reuniones/Detalle?citaId="+Guid.NewGuid(),"/InformesEducativos/Detalle/"+Guid.NewGuid()})
            Check((await proveedor.GetAsync(url)).StatusCode==HttpStatusCode.Forbidden,"proveedor bloqueado en "+url);
        Check((await anon.GetAsync("/Tarifas/Editar")).StatusCode==HttpStatusCode.Redirect,"editor requiere sesión");
        Check((await admin.GetAsync("/Tarifas/Editar")).Headers.Location?.OriginalString.Contains("AccesoDenegado")==true,"Administrador solo consulta precios");
        Check((await proveedor.PostAsync("/Tarifas/Editar",new FormUrlEncodedContent([]))).StatusCode==HttpStatusCode.BadRequest,"precios requieren antiforgery");
        EditarTarifasInput actual;await using(var db=new OfflineDb(options))actual=await TarifasService.Obtener(db,default);
        Task<HttpResponseMessage> Guardar(HttpClient c,Guid revision,string mensual="333.33",string trimestre="899.99",string semestre="1699",string anual="3333")=>HostPrueba.Post(c,"/Tarifas","/Tarifas/Editar",("Revision",revision.ToString()),("Mensual",mensual),("Trimestral",trimestre),("Semestral",semestre),("Anual",anual));
        Check((await Guardar(admin,actual.Revision)).Headers.Location?.OriginalString.Contains("AccesoDenegado")==true,"Administrador no publica precios por POST");
        Check((await Guardar(proveedor,actual.Revision,"-1")).StatusCode==HttpStatusCode.OK,"importe negativo muestra error");
        Check((await Guardar(proveedor,actual.Revision,"333.333")).StatusCode==HttpStatusCode.OK,"rechaza más de dos decimales");
        await using(var db=new OfflineDb(options))Check((await TarifasService.Obtener(db,default)).Mensual==199m,"errores no alteran catálogo");
        Check((await Guardar(proveedor,actual.Revision)).StatusCode==HttpStatusCode.Redirect,"proveedor guarda precios");
        await using(var db=new OfflineDb(options)){
            var nuevo=await TarifasService.Obtener(db,default);Check(nuevo.Mensual==333.33m&&nuevo.Trimestral==899.99m&&nuevo.Revision!=actual.Revision,"precios persistidos y revisión renovada");
            var lista=await TarifasService.Catalogo(db,default);Check(lista[1].Ahorro==100m&&lista[1].MensualEquivalente==899.99m/3m,"ahorro usa nueva tarifa mensual");
            Check(await db.AuditoriaUsuarios.AnyAsync(a=>a.AdministradorId==id&&a.Accion=="TARIFAS_COMERCIALES_ACTUALIZADAS"),"cambio comercial auditado");
        }
        Check((await admin.GetStringAsync("/Tarifas")).Contains("333.33"),"institución ve precio actualizado");
        Check((await Guardar(proveedor,actual.Revision,"999")).StatusCode==HttpStatusCode.Conflict,"edición obsoleta no sobreescribe");
        await using(var db=new OfflineDb(options)){
            foreach(var role in await db.Roles.Where(r=>r.Name=="Administrador"||r.Name=="Psicologo"||r.Name=="Estudiante"||r.Name=="Director").ToListAsync())db.UserRoles.Add(new(){UserId=id,RoleId=role.Id});
            await db.SaveChangesAsync();
            Check(!await TarifasService.EsSuperAdmin(db,id,default),"cuenta mezclada no tiene permiso comercial");
        }
        using var mezclado=HostPrueba.Cliente(app);await HostPrueba.Login(mezclado,"proveedor-ficticio@example.com",clave);
        foreach(var url in new[]{"/Administrador/Estudiantes","/Psicologo/Solicitudes","/InformesEducativos","/PruebasAplicadas","/MiCuenta","/Tarifas/Editar"})
            Check((await mezclado.GetAsync(url)).StatusCode==HttpStatusCode.Forbidden,"roles mezclados bloqueados en "+url);
        await using(var db=new OfflineDb(options)){
            db.UserRoles.RemoveRange(await db.UserRoles.Where(ur=>ur.UserId==id&&db.Roles.Any(r=>r.Id==ur.RoleId&&r.NormalizedName!="SUPERADMIN")).ToListAsync());await db.SaveChangesAsync();
            actual=await TarifasService.Obtener(db,default);
        }
        Check((await Guardar(proveedor,actual.Revision,"199","567","1075","2030")).StatusCode==HttpStatusCode.Redirect,"restaurar precios ficticios para otras pruebas");
        var preview=Environment.GetEnvironmentVariable("ZUNI_FORMS_PREVIEW_DIR");if(!string.IsNullOrEmpty(preview)){
            await File.WriteAllTextAsync(Path.Combine(preview,"tarifas-editor.html"),await proveedor.GetStringAsync("/Tarifas/Editar"));
            await File.WriteAllTextAsync(Path.Combine(preview,"tarifas-superadmin.html"),await proveedor.GetStringAsync("/Tarifas"));
            await File.WriteAllTextAsync(Path.Combine(preview,"superadmin.html"),await proveedor.GetStringAsync("/SuperAdmin"));
        }
        Console.WriteLine($"OK: {checks} comprobaciones de precios editables y aislamiento comercial.");
    }
}
