using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Zuni.Data;

internal static class IdiomaChecks
{
    public static async Task Run(WebApplication app,DbContextOptions<ApplicationDbContext> options,string clave)
    {
        int checks=0; void Check(bool ok,string label){if(!ok)throw new Exception("Idioma: "+label);checks++;}
        string Decode(string s)=>WebUtility.HtmlDecode(s);
        async Task<HttpResponseMessage> Switch(HttpClient client,string language,string path="/")=>await HostPrueba.Post(client,"/","/Idioma/Cambiar",("idioma",language),("volver",path));
        using var anon=HostPrueba.Cliente(app);
        var inicio=await anon.GetStringAsync("/");Check(inicio.Contains("lang=\"es\"")&&inicio.Contains("Bienvenido a"),"español predeterminado");
        Check(inicio.Contains("class=\"zuni-language\"")&&inicio.Contains("value=\"en\""),"selector público renderizado");
        var change=await Switch(anon,"en","/Home/Guia?tema=privacidad");
        Check(change.StatusCode==HttpStatusCode.Redirect&&change.Headers.Location!.OriginalString=="/Home/Guia?tema=privacidad","conserva ruta y filtros");
        var cookie=string.Join(";",change.Headers.GetValues("Set-Cookie"));
        Check(cookie.Contains("max-age=31536000")&&cookie.Contains("httponly",StringComparison.OrdinalIgnoreCase)&&cookie.Contains("samesite=lax",StringComparison.OrdinalIgnoreCase),"preferencia persistente protegida");
        using(var ajax=HostPrueba.Cliente(app)){
            ajax.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            var response=await Switch(ajax,"en","/Home/Guia?tema=privacidad");
            using var json=System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Check(response.StatusCode==HttpStatusCode.OK&&json.RootElement.GetProperty("volver").GetString()=="/Home/Guia?tema=privacidad","selector asíncrono confirma idioma sin redirigir");
            Check((await ajax.GetStringAsync("/")).Contains("lang=\"en\""),"la petición posterior usa la nueva cookie");
            var unsafeReturn=await Switch(ajax,"es","https://otro.example/");using var safe=System.Text.Json.JsonDocument.Parse(await unsafeReturn.Content.ReadAsStringAsync());
            Check(safe.RootElement.GetProperty("volver").GetString()=="/","modo asíncrono tampoco permite retorno externo");
        }
        var english=await anon.GetStringAsync("/");Check(english.Contains("lang=\"en\"")&&english.Contains("Welcome to")&&english.Contains("Sign in"),"home y acceso en inglés");
        var guide=Decode(await anon.GetStringAsync("/Home/Guia?tema=privacidad"));
        Check(guide.Contains("Decide how your information is used")&&guide.Contains("What is shared and what stays private"),"guías completas y tarjetas traducidas");
        Check((await Switch(anon,"fr")).StatusCode==HttpStatusCode.BadRequest,"idioma no admitido rechazado");
        Check((await anon.PostAsync("/Idioma/Cambiar",new FormUrlEncodedContent(new Dictionary<string,string>{{"idioma","en"}}))).StatusCode==HttpStatusCode.BadRequest,"cambio requiere antiforgery");
        foreach(var bad in new[]{"https://example.com/","//example.com/","/\\example.com/"}) Check((await Switch(anon,"en",bad)).Headers.Location!.OriginalString=="/","sin redirección externa");
        var login=Decode(await anon.GetStringAsync("/Cuenta/IniciarSesion"));
        Check(login.Contains("Welcome back")&&login.Contains("Email address")&&login.Contains("Password"),"login y etiquetas en inglés");
        var invalid=await HostPrueba.Post(anon,"/Cuenta/IniciarSesion","/Cuenta/IniciarSesion",("Email",""),("Password",""));
        Check(Decode(await invalid.Content.ReadAsStringAsync()).Contains("Enter your email address."),"validación de servidor traducida");
        var path=Environment.GetEnvironmentVariable("ZUNI_FORMS_PREVIEW_DIR");
        if(!string.IsNullOrEmpty(path)){await File.WriteAllTextAsync(Path.Combine(path,"idioma-home.html"),english);await File.WriteAllTextAsync(Path.Combine(path,"idioma-login.html"),await anon.GetStringAsync("/Cuenta/IniciarSesion"));await File.WriteAllTextAsync(Path.Combine(path,"idioma-guia.html"),await anon.GetStringAsync("/Home/Guia?tema=privacidad"));}
        foreach(var (topic,title) in new[]{("cuenta","Your account and access, step by step"),("pruebas","Complete a test with guidance"),("informes","Review, authorize and keep your documents")}) {
            var html=await anon.GetStringAsync("/Home/Guia?tema="+topic);
            Check(Decode(html).Contains(title),"guía adicional traducida "+topic);
            if(!string.IsNullOrEmpty(path))await File.WriteAllTextAsync(Path.Combine(path,"guia-"+topic+"-en.html"),html);
        }
        foreach(var (email,route,name,expected) in new[]{("admin@miumg.edu.gt","/Administrador","admin","Add user"),("director@miumg.edu.gt","/Director","director","Questionnaire management"),("psi3@miumg.edu.gt","/Psicologo/Agenda","psicologo","My availability"),("est2@miumg.edu.gt","/Estudiante","estudiante","My account")})
        {
            using var c=HostPrueba.Cliente(app);await Switch(c,"en");await HostPrueba.Login(c,email,clave);
            var page=Decode(await c.GetStringAsync(route));Check(page.Contains("lang=\"en\"")&&page.Contains(expected),"panel traducido "+name);
            Check(page.Contains("class=\"zuni-language\"")&&page.Contains("Session active"),"selector y sesión en "+name);
            if(name=="psicologo")Check(page.Contains(">Mon</span>")&&page.Contains("Pending review"),"calendario y leyenda traducidos");
            if(name=="admin") {var form=Decode(await c.GetStringAsync("/Administrador/AgregarUsuario"));if(!string.IsNullOrEmpty(path))await File.WriteAllTextAsync(Path.Combine(path,"idioma-alta.html"),form);Check(form.Contains("Age classification")&&form.Contains("Temporary password")&&form.Contains("value=\"Estudiante\""),"etiquetas y valores POST independientes: edad="+form.Contains("Age classification")+", clave="+form.Contains("Temporary password")+", rol="+form.Contains("value=\"Estudiante\""));}
            if(!string.IsNullOrEmpty(path))await File.WriteAllTextAsync(Path.Combine(path,"idioma-"+name+".html"),await c.GetStringAsync(route));
            await Switch(c,"es",route);Check((await c.GetStringAsync(route)).Contains("lang=\"es\""),"regreso a español "+name);
        }
        string originalName; string? originalCareer; string institution;
        await using(var db=new OfflineDb(options)){
            var user=(await db.Users.FindAsync("est2"))!;originalName=user.FullName;user.FullName="Guardar cambios";
            var profile=await db.PerfilesEstudiante.SingleAsync(p=>p.UsuarioId==user.Id);originalCareer=profile.Carrera;profile.Carrera="Inicio";
            var config=(await db.ConfiguracionInstitucion.FindAsync(1))!;institution=config.Nombre;config.Nombre="Guardar cambios";await db.SaveChangesAsync();
        }
        using(var student=HostPrueba.Cliente(app)){
            await Switch(student,"en");await HostPrueba.Login(student,"est2@miumg.edu.gt",clave);
            var account=Decode(await student.GetStringAsync("/MiCuenta"));
            Check(account.Contains("Guardar cambios")&&account.Contains("Inicio")&&account.Contains("Full name"),"datos iguales a textos del catálogo no se traducen");
            var form=Decode(await student.GetStringAsync("/MiCuenta/Editar"));Check(form.Contains("value=\"Guardar cambios\""),"nombre de formulario intacto");
            await HostPrueba.Post(student,"/MiCuenta","/Cuenta/CerrarSesion");Check((await student.GetStringAsync("/Cuenta/IniciarSesion")).Contains("Welcome back"),"preferencia continúa tras cerrar sesión");
        }
        await using(var db=new OfflineDb(options)){
            var user=(await db.Users.FindAsync("est2"))!;Check(user.FullName=="Guardar cambios","cambiar idioma no modifica la DB");user.FullName=originalName;
            (await db.PerfilesEstudiante.SingleAsync(p=>p.UsuarioId==user.Id)).Carrera=originalCareer;(await db.ConfiguracionInstitucion.FindAsync(1))!.Nombre=institution;await db.SaveChangesAsync();
        }
        using(var temporal=HostPrueba.Cliente(app)){
            await HostPrueba.Login(temporal,"alta@miumg.edu.gt",clave);
            var forced=await HostPrueba.Post(temporal,"/Cuenta/CambiarContrasenaObligatoria","/Idioma/Cambiar",("idioma","en"),("volver","/Cuenta/CambiarContrasenaObligatoria"));
            Check(forced.Headers.TryGetValues("Set-Cookie",out _)&&(await temporal.GetStringAsync("/Cuenta/CambiarContrasenaObligatoria")).Contains("lang=\"en\""),"selector disponible al exigir contraseña nueva");
        }
        Console.WriteLine($"OK: {checks} comprobaciones de idioma y preservación de datos.");
    }
}
