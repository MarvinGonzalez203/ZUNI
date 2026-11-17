using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Zuni.Seguridad;
internal static class Infra0713Checks
{
 internal static async Task Run()
 {
  var count=0;void Check(bool ok,string label){if(!ok)throw new Exception(label);count++;}
  foreach(var prefix in new[]{"/deniscorp/zuni","/otra/base"})foreach(var enforce in new[]{false,true})
  {
   await using var connection=new SqliteConnection("Data Source=:memory:");await connection.OpenAsync();
   var options=new DbContextOptionsBuilder<Zuni.Data.ApplicationDbContext>().UseSqlite(connection).Options;
   await using(var db=new OfflineDb(options))await db.Database.EnsureCreatedAsync();
   await using var app=await HostPrueba.Crear(options,new Reloj(),new HasherContador(),100,pathBase:prefix,cspEnforced:enforce);
   using var client=HostPrueba.Cliente(app);
   foreach(var page in new[]{"/","/Cuenta/IniciarSesion","/Cuenta/Registro","/deniscorp/programas/fulleo/","/deniscorp/blog/"})
   {
    var response=await client.GetAsync(prefix+page);var html=await response.Content.ReadAsStringAsync();
    Check(response.StatusCode==HttpStatusCode.OK,"página bajo prefijo "+page);
    Check(!html.Contains("EncodeFirstSegmentContent"),"contenido HTML de URL preservado");
    Check(html.Contains("src=\""+prefix+"/js/"),"scripts locales resueltos bajo prefijo");
    Check(response.Headers.GetValues("X-Frame-Options").Single()=="DENY","sin frames");
    var policy=response.Headers.GetValues(enforce?"Content-Security-Policy":"Content-Security-Policy-Report-Only").Single();
    var nonce=Regex.Match(policy,"'nonce-([^']+)'",RegexOptions.None).Groups[1].Value;Check(nonce.Length>=32,"nonce aleatorio en política");
    foreach(Match script in Regex.Matches(html,"<(?:script|style)\\b[^>]*>"))Check(WebUtility.HtmlDecode(script.Value).Contains("nonce=\""+nonce+"\""),"scripts/estilos Razor autorizados por nonce "+page+" "+script.Value);
    foreach(Match route in Regex.Matches(html,"(?:href|src|action)=\"(/[^\"]*)\""))Check(route.Groups[1].Value.StartsWith(prefix+"/")||route.Groups[1].Value==prefix,"URL local conserva base "+route.Groups[1].Value);
    var second=await client.GetAsync(prefix+page);Check(second.Headers.GetValues(enforce?"Content-Security-Policy":"Content-Security-Policy-Report-Only").Single()!=policy,"nonce diferente por respuesta");
   }
   Check((await client.GetAsync("/Cuenta/IniciarSesion")).StatusCode==HttpStatusCode.NotFound,"raíz sin prefijo rechazada");
   var challenge=await client.GetAsync(prefix+"/Administrador");
   Check(challenge.StatusCode==HttpStatusCode.Redirect&&challenge.Headers.Location!.AbsolutePath==prefix+"/Cuenta/IniciarSesion","challenge conserva base");
   Check((await client.GetAsync(prefix+"/deniscorp/fulleo/")).StatusCode==HttpStatusCode.NotFound,"ficha no ocupa entrada de aplicación");
   await app.StopAsync();
  }
  foreach(var bad in new[]{"/UPPER","//bad","/a/","https://evil.test"})
  {try{BasePublica.Prefijo(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{["Hosting:PathBase"]=bad}).Build());throw new Exception("base inválida aceptada");}catch(InvalidOperationException){count++;}}
  Check(BasePublica.Ruta("//evil.test/",new("/base"))=="//evil.test/","no reescribe origen externo");
  var mailConfig=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{
   ["Hosting:PathBase"]="/otra/base",["Correo:UrlPublica"]="https://atencion.example.test/otra/base/",
   ["Correo:Brevo:ApiKey"]=new string('x',32),["Correo:Remitente"]="fixture@example.test",["Correo:RemitenteVerificado"]="true"
  }).Build();
  var mail=new Zuni.Services.Correo.CorreoPinBrevo(new HttpClient(),mailConfig);
  Check(mail.Disponible,"correo acepta origen HTTPS con el prefijo correcto sin enviar nada");
  mailConfig["Correo:UrlPublica"]="https://atencion.example.test/";
  Check(!mail.Disponible,"correo rechaza base distinta de la aplicación");
  Console.WriteLine($"OK: {count} comprobaciones HTTP de prefijos, nonce, CSP y autenticación.");
 }
}
