using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;

internal static class ExperienciaPanelChecks
{
    public static async Task Run(WebApplication app,DbContextOptions<ApplicationDbContext> options,string clave)
    {
        int checks=0;void Check(bool ok,string msg){if(!ok)throw new Exception("Experiencia de panel: "+msg);checks++;}
        using var alumno=HostPrueba.Cliente(app);using var admin=HostPrueba.Cliente(app);using var psy=HostPrueba.Cliente(app);using var otro=HostPrueba.Cliente(app);using var demo=HostPrueba.Cliente(app);
        await HostPrueba.Login(alumno,"est@miumg.edu.gt",clave);await HostPrueba.Login(admin,"admin@miumg.edu.gt",clave);await HostPrueba.Login(psy,"psi2@miumg.edu.gt",clave);await HostPrueba.Login(otro,"meal-est@miumg.edu.gt",clave);await HostPrueba.Login(demo,"est2@miumg.edu.gt",clave);
        async Task<Guid> Revision(HttpClient c){var html=await c.GetStringAsync("/Preferencias/Colores");return Guid.Parse(Regex.Match(html,"name=\"Revision\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);}
        Task<HttpResponseMessage> Save(HttpClient c,Guid revision,string rol,string color)=>HostPrueba.Post(c,"/Preferencias/Colores","/Preferencias/Colores",("Revision",revision.ToString()),("Rol",rol),("ColorPrincipal",color),("UsuarioId","meal-est"));
        var rev=await Revision(alumno);
        Check((await Save(alumno,rev,"Estudiante","#BBF1F8")).StatusCode==HttpStatusCode.Redirect,"estudiante elige su color");
        Check((await Save(admin,await Revision(admin),"Administrador","#111111")).StatusCode==HttpStatusCode.Redirect,"Administrador elige color propio");
        Check((await Save(psy,await Revision(psy),"Psicologo","#F9DB5C")).StatusCode==HttpStatusCode.Redirect,"psicólogo elige color propio");
        Check((await alumno.GetStringAsync("/MiCuenta")).Contains("--institucion-color: #bbf1f8"),"los otros perfiles no cambian al alumno");
        await using(var db=new OfflineDb(options))Check(!await db.PreferenciasVista.AnyAsync(p=>p.UsuarioId=="meal-est"),"no modifica otra cuenta aunque se envíe su ID");
        Check((await Save(alumno,await Revision(alumno),"SuperAdmin","#111111")).Headers.Location?.OriginalString.Contains("AccesoDenegado")==true,"no escribe preferencias de rol ajeno");
        Check((await Save(alumno,rev,"Estudiante","#111111")).StatusCode==HttpStatusCode.Conflict,"revisión obsoleta protegida");
        Check((await alumno.PostAsync("/Preferencias/Colores",new FormUrlEncodedContent([]))).StatusCode==HttpStatusCode.BadRequest,"preferencia exige antiforgery");
        var home=await alumno.GetStringAsync("/MiCuenta");Check(Regex.Matches(home,"class=\"dash-round-control").Count==3,"tres controles inferiores");
        Check(home.Contains("dash-logo-glass-second")&&home.Contains("page-scroll-down"),"logo y señal de desplazamiento compartidos");
        Check(home.Contains("/MiCuenta/CambiarContrasena")&&home.Contains("/Preferencias/Colores?rol=Estudiante"),"menú del estudiante desglosado");
        var previewEs=Environment.GetEnvironmentVariable("ZUNI_FORMS_PREVIEW_DIR");if(!string.IsNullOrEmpty(previewEs))await File.WriteAllTextAsync(Path.Combine(previewEs,"demo-espanol.html"),await demo.GetStringAsync("/Demostracion"));
        await HostPrueba.Post(demo,"/MiCuenta","/Idioma/Cambiar",("idioma","en"),("volver","/Demostracion"));
        var en=await demo.GetStringAsync("/Demostracion");
        Check(Regex.IsMatch(en,"<details class=\"dash-nav-group\"\\s+open"),"demostración abre su sección Pruebas");
        Check(Regex.IsMatch(await alumno.GetStringAsync("/CitasProgramadas/Notificaciones"),"<details class=\"dash-nav-group\"\\s+open"),"notificaciones abre su sección Atención");
        Check(en.Contains("Behavioral Tendencies Screening Questionnaire")&&en.Contains("There are no right or wrong answers"),"título e instrucciones incluidos en inglés");
        Check(en.Contains("I feel comfortable starting a conversation")&&!en.Contains("No existen respuestas correctas"),"preguntas del catálogo traducidas sin mezcla");
        var nombre="No existen respuestas correctas ni incorrectas";Check(Zuni.Localizacion.TextoPrueba.Texto(Guid.NewGuid(),nombre)==nombre,"contenido personalizado no traducido");
        var preview=Environment.GetEnvironmentVariable("ZUNI_FORMS_PREVIEW_DIR");if(!string.IsNullOrEmpty(preview)){
            foreach(var (client,url,file) in new[]{(alumno,"/MiCuenta","panel-alumno"),(admin,"/MiCuenta","panel-admin"),(psy,"/MiCuenta","panel-psicologo"),(alumno,"/Preferencias/Colores","colores-alumno"),(otro,"/MiCuenta","panel-otro-alumno")})await File.WriteAllTextAsync(Path.Combine(preview,file+".html"),await client.GetStringAsync(url));
            await File.WriteAllTextAsync(Path.Combine(preview,"demo-ingles.html"),en);
        }
        foreach(var (correo,rol) in new[]{("est@miumg.edu.gt","Estudiante"),("admin@miumg.edu.gt","Administrador"),("psi2@miumg.edu.gt","Psicologo"),("director@miumg.edu.gt","Director"),("proveedor-ficticio@example.com","SuperAdmin")}){
            using var c=HostPrueba.Cliente(app);await HostPrueba.Login(c,correo,clave);var response=await HostPrueba.Post(c,"/MiCuenta","/Cuenta/CerrarSesion");
            Check(response.Headers.Location?.OriginalString=="/","cerrar sesión lleva a home "+rol);
            Check((await c.GetAsync("/MiCuenta")).StatusCode==HttpStatusCode.Redirect,"sesión invalidada "+rol);
        }
        Console.WriteLine($"OK: {checks} comprobaciones de preferencias personales, traducción y cierre de sesión.");
    }
}
