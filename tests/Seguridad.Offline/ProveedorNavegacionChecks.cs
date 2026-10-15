using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Services.Institucion;

internal static class ProveedorNavegacionChecks
{
    public static async Task Run(WebApplication app,DbContextOptions<ApplicationDbContext> options,string clave)
    {
        int checks=0;void Check(bool ok,string msg){if(!ok)throw new Exception("Navegación del proveedor: "+msg);checks++;}
        using var super=HostPrueba.Cliente(app);using var admin=HostPrueba.Cliente(app);
        await HostPrueba.Login(super,"proveedor-ficticio@example.com",clave);await HostPrueba.Login(admin,"admin@miumg.edu.gt",clave);
        foreach(var url in new[]{"/SuperAdmin","/Tarifas","/Tarifas/Editar","/MiCuenta","/MiCuenta/Editar","/MiCuenta/CambiarContrasena","/Privacidad","/SuperAdmin/Apariencia"}){
            var html=await super.GetStringAsync(url);
            Check(html.Contains("~/Views/SuperAdmin/_Navegacion.cshtml")&&html.Contains("/MiCuenta/CambiarContrasena")&&html.Contains("/SuperAdmin/Apariencia"),"menú completo persistente en "+url);
            Check(Regex.Matches(html,"<a[^>]*class=\"[^\"]*\\bis-active\\b[^\"]*\"").Count==1,"selección única en "+url);
            Check(Regex.IsMatch(html,"class=\"dash-space-link\"[^>]*href=\"/SuperAdmin\""),"Mi espacio lleva al tablero en "+url);
        }
        foreach(var (correo,panel) in new[]{("admin@miumg.edu.gt","Administrador"),("director@miumg.edu.gt","Director"),("psi2@miumg.edu.gt","Psicologo"),("est@miumg.edu.gt","Estudiante")}){
            using var c=HostPrueba.Cliente(app);await HostPrueba.Login(c,correo,clave);var html=await c.GetStringAsync("/MiCuenta");
            Check(Regex.IsMatch(html,"class=\"dash-space-link\"[^>]*href=\"/"+panel+"\""),"Mi espacio regresa al perfil "+panel);
        }
        Check((await admin.GetAsync("/SuperAdmin/Apariencia")).Headers.Location?.OriginalString.Contains("AccesoDenegado")==true,"Administrador no configura proveedor");
        Check((await super.PostAsync("/SuperAdmin/Apariencia",new FormUrlEncodedContent([]))).StatusCode==HttpStatusCode.BadRequest,"color exige antiforgery");
        string colorInstitucion;Zuni.Models.Institucion.AparienciaProveedorInput antes;
        await using(var db=new OfflineDb(options)){colorInstitucion=(await ConfiguracionInstitucionService.ObtenerAsync(db)).ColorPrincipal;antes=await TarifasService.Apariencia(db,default);}
        Task<HttpResponseMessage> Save(Guid revision,string color)=>HostPrueba.Post(super,"/SuperAdmin/Apariencia","/SuperAdmin/Apariencia",("Revision",revision.ToString()),("ColorPrincipal",color));
        Check((await Save(antes.Revision,"red;display:none")).StatusCode==HttpStatusCode.OK,"rechaza CSS en color");
        Check((await Save(antes.Revision,"#F9DB5C")).StatusCode==HttpStatusCode.Redirect,"guarda amarillo del proveedor");
        await using(var db=new OfflineDb(options)){
            Check((await ConfiguracionInstitucionService.ObtenerAsync(db)).ColorPrincipal==colorInstitucion,"no altera los colores institucionales");
            Check((await TarifasService.Apariencia(db,default)).ColorPrincipal=="#f9db5c","color comercial persistido");
            Check((await TarifasService.Obtener(db,default)).Mensual==199m,"apariencia no modifica precios");
        }
        foreach(var url in new[]{"/MiCuenta","/Tarifas","/SuperAdmin"})Check((await super.GetStringAsync(url)).Contains("--institucion-color: #f9db5c"),"color compartido en panel comercial "+url);
        Check((await Save(antes.Revision,"#111111")).StatusCode==HttpStatusCode.Conflict,"configuración obsoleta no sobreescribe");
        var tarifas=await super.GetStringAsync("/Tarifas");Check(tarifas.Contains("tarifa-capacidades-superadmin"),"SuperAdmin tiene tarjeta de capacidades");
        Check(!(await admin.GetStringAsync("/Tarifas")).Contains("tarifa-capacidades-superadmin"),"Administrador no recibe tarjeta privada del proveedor");
        await using(var db=new OfflineDb(options))antes=await TarifasService.Apariencia(db,default);
        await Save(antes.Revision,"#cea2fd");
        var preview=Environment.GetEnvironmentVariable("ZUNI_FORMS_PREVIEW_DIR");if(!string.IsNullOrEmpty(preview)){
            foreach(var (url,file) in new[]{("/","proveedor-home"),("/SuperAdmin/Apariencia","proveedor-apariencia"),("/MiCuenta","proveedor-cuenta"),("/MiCuenta/Editar","proveedor-datos"),("/MiCuenta/CambiarContrasena","proveedor-clave"),("/Tarifas","tarifas-superadmin"),("/Tarifas/Editar","tarifas-editor")})await File.WriteAllTextAsync(Path.Combine(preview,file+".html"),await super.GetStringAsync(url));
        }
        Console.WriteLine($"OK: {checks} comprobaciones de navegación unificada y colores aislados.");
    }
}
