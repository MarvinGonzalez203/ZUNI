// Ejecutar desde la raíz después de compilar en .visual-check/eval-runtime.
// Usa exclusivamente las demostraciones de Evin. Restaura su estado al terminar.
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Net;
using System.Text.RegularExpressions;
using System.Text.Json;
using Npgsql;
var root=Path.GetFullPath("Zuni");
var cfg=new ConfigurationBuilder().SetBasePath(root).AddJsonFile("appsettings.json",true).AddJsonFile("appsettings.Development.json",true).AddUserSecrets("df74818a-cd0f-4ce3-9d60-29cf450de409").AddEnvironmentVariables().Build();
var cs=new NpgsqlConnectionStringBuilder(cfg.GetConnectionString("DefaultConnection"));
if(cs.Database!="ZuniDesarrollo" || !new[]{"localhost","127.0.0.1","::1"}.Contains(cs.Host))throw new Exception("Base no autorizada");
await using var db=new NpgsqlConnection(cs.ConnectionString);await db.OpenAsync();
const string evin="464d9b0e-70fe-4b0a-aa97-e74682596898";
async Task<string> Scalar(string sql,Guid? id=null) {await using var c=new NpgsqlCommand(sql,db);if(id.HasValue)c.Parameters.AddWithValue("id",id.Value);return (string)(await c.ExecuteScalarAsync())!;}
async Task<int> Revision(Guid id)=>int.Parse(await Scalar("SELECT \"Revision\"::text FROM \"AsignacionesEvaluacion\" WHERE \"Id\"=@id",id));
var rows=JsonDocument.Parse(await Scalar("SELECT jsonb_agg(to_jsonb(a))::text FROM \"AsignacionesEvaluacion\" a JOIN \"Evaluaciones\" e ON e.\"Id\"=a.\"EvaluacionId\" WHERE e.\"Codigo\" LIKE 'ZUNI-DEMO-%' AND a.\"EstudianteId\"='"+evin+"'"));
if(rows.RootElement.GetArrayLength()!=4)throw new Exception("Se requieren las cuatro demostraciones");
var snapshots=new Dictionary<string,string>();foreach(var t in new[]{"AsignacionesEvaluacion","RespuestasEvaluacion","ResultadosEvaluacion"}) {
 var where=t=="AsignacionesEvaluacion"?"t.\"Id\"":"t.\"AsignacionId\"";
 snapshots[t]=await Scalar("SELECT coalesce(jsonb_agg(to_jsonb(t)),'[]')::text FROM \""+t+"\" t WHERE "+where+" IN (SELECT a.\"Id\" FROM \"AsignacionesEvaluacion\" a JOIN \"Evaluaciones\" e ON e.\"Id\"=a.\"EvaluacionId\" WHERE e.\"Codigo\" LIKE 'ZUNI-DEMO-%' AND a.\"EstudianteId\"='"+evin+"')");
}
Directory.CreateDirectory(".visual-check/eval-test-backups");await File.WriteAllTextAsync(".visual-check/eval-test-backups/estado-inicial.json",JsonSerializer.Serialize(snapshots));
var provider=DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(root,"App_Data","keys")),b=>b.SetApplicationName("Zuni"));
var formatter=new TicketDataFormat(provider.CreateProtector("Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationMiddleware","Cookies","v2"));
async Task<HttpClient> Client(string userId,string[] roles) {
 var stamp=await Scalar("SELECT \"SecurityStamp\" FROM \"AspNetUsers\" WHERE \"Id\"='"+userId+"'");
 var claims=new List<Claim>{new(ClaimTypes.NameIdentifier,userId),new(ClaimTypes.Name,"Prueba local"),new("Zuni.SecurityStamp",stamp)};claims.AddRange(roles.Select(r=>new Claim(ClaimTypes.Role,r)));
 var ticket=new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims,"Cookies")),new AuthenticationProperties{IssuedUtc=DateTimeOffset.UtcNow,ExpiresUtc=DateTimeOffset.UtcNow.AddMinutes(15)},"Cookies");
 var jar=new CookieContainer();jar.Add(new Uri("http://127.0.0.1:7111"),new Cookie(".AspNetCore.Cookies",formatter.Protect(ticket)));
 return new HttpClient(new HttpClientHandler{AllowAutoRedirect=false,CookieContainer=jar}){BaseAddress=new Uri("http://127.0.0.1:7111")};
}
using var owner=await Client(evin,new[]{"Administrador","Estudiante"});using var student=await Client("94093b8b-78a7-4eb4-9c54-2ee9ee8ba8c7",new[]{"Estudiante"});
int checks=0;
void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;Console.WriteLine("OK: "+name);}
async Task<string> Page(HttpClient client,string path){var r=await client.GetAsync(path);Check(r.StatusCode==HttpStatusCode.OK,"GET "+path);return await r.Content.ReadAsStringAsync();}
string Token(string html)=>WebUtility.HtmlDecode(Regex.Match(html,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
async Task Post(HttpClient client,string path,Dictionary<string,string> values,string tokenPage){values["__RequestVerificationToken"]=Token(await Page(client,tokenPage));var r=await client.PostAsync(path,new FormUrlEncodedContent(values));Check(r.StatusCode==HttpStatusCode.Redirect,"POST "+path);}
Guid Assigned(int state)=>rows.RootElement.EnumerateArray().Single(a=>a.GetProperty("Estado").GetInt32()==state).GetProperty("Id").GetGuid();
async Task Manage(Guid id,string op)=>await Post(owner,"/Administrador/GestionarEvaluacion",new(){{"id",id.ToString()},{"revision",(await Revision(id)).ToString()},{"operacion",op}},"/Administrador/Evaluaciones");
try {
 var home=await Page(owner,"/Estudiante/Evaluaciones");foreach(var state in new[]{"Asignada","Pendiente","EnProceso","Finalizada"})Check(home.Contains("data-estado=\""+state+"\""),"Categoría "+state);
 var blocked=Assigned(0);var pending=Assigned(1);var started=Assigned(2);var finished=Assigned(3);
 // No puede iniciar una asignación deshabilitada (token de otra página válida).
 await Post(owner,$"/Estudiante/Evaluaciones/{blocked}/iniciar",new(){{"revision",(await Revision(blocked)).ToString()}},"/Administrador/Evaluaciones");
 Check(await Scalar("SELECT \"Estado\"::text FROM \"AsignacionesEvaluacion\" WHERE \"Id\"=@id",blocked)=="0","Asignada no inicia antes de habilitar");
 await Manage(blocked,"habilitar");Check(await Scalar("SELECT \"Estado\"::text FROM \"AsignacionesEvaluacion\" WHERE \"Id\"=@id",blocked)=="1","Habilitación administrativa");
 await Post(owner,$"/Estudiante/Evaluaciones/{pending}/iniciar",new(){{"revision",(await Revision(pending)).ToString()}},$"/Estudiante/Evaluaciones/{pending}");
 Check(await Scalar("SELECT \"Estado\"::text FROM \"AsignacionesEvaluacion\" WHERE \"Id\"=@id",pending)=="2","Pendiente pasa a En proceso");
 var qs=JsonDocument.Parse(await Scalar("SELECT jsonb_agg(p.\"Id\" ORDER BY p.\"Orden\")::text FROM \"PreguntasEvaluacion\" p JOIN \"AsignacionesEvaluacion\" a ON a.\"EvaluacionId\"=p.\"EvaluacionId\" WHERE a.\"Id\"=@id",pending)).RootElement.EnumerateArray().Select(x=>x.GetGuid()).ToArray();
 var stale=await Revision(pending);
 await Post(owner,$"/Estudiante/Evaluaciones/{pending}/guardar",new(){{"revision",stale.ToString()},{"respuestas["+qs[0]+"]","3"}},$"/Estudiante/Evaluaciones/{pending}");
 Check(await Scalar("SELECT count(*)::text FROM \"RespuestasEvaluacion\" WHERE \"AsignacionId\"=@id",pending)=="1","Avance parcial persistido en PostgreSQL");
 using(var fresh=await Client(evin,new[]{"Administrador","Estudiante"}))Check((await Page(fresh,$"/Estudiante/Evaluaciones/{pending}")).Contains("value=\"3\" selected"),"Nueva sesión recupera respuesta guardada");
 await Post(owner,$"/Estudiante/Evaluaciones/{pending}/guardar",new(){{"revision",stale.ToString()},{"respuestas["+qs[0]+"]","5"}},$"/Estudiante/Evaluaciones/{pending}");
 Check(await Scalar("SELECT \"Valor\"::text FROM \"RespuestasEvaluacion\" WHERE \"AsignacionId\"=@id",pending)=="3","Versión antigua no sobrescribe respuestas");
 await Post(owner,$"/Estudiante/Evaluaciones/{pending}/finalizar",new(){{"revision",(await Revision(pending)).ToString()},{"confirmado","true"}},$"/Estudiante/Evaluaciones/{pending}/revisar");
 Check(await Scalar("SELECT \"Estado\"::text FROM \"AsignacionesEvaluacion\" WHERE \"Id\"=@id",pending)=="2","No finaliza si faltan obligatorias");
 var full=new Dictionary<string,string>{{"revision",(await Revision(pending)).ToString()},{"revisar","true"}};foreach(var q in qs)full["respuestas["+q+"]"]="4";
 await Post(owner,$"/Estudiante/Evaluaciones/{pending}/guardar",full,$"/Estudiante/Evaluaciones/{pending}");
 var review=await Page(owner,$"/Estudiante/Evaluaciones/{pending}/revisar");Check(review.Contains("<dialog") && review.Contains("Cancelar") && review.Contains("Confirmar y finalizar"),"Confirmación y cancelación presentes");
 await Page(owner,$"/Estudiante/Evaluaciones/{pending}");Check(await Scalar("SELECT count(*)::text FROM \"RespuestasEvaluacion\" WHERE \"AsignacionId\"=@id",pending)=="3","Volver de revisión conserva respuestas");
 var lastRevision=await Revision(pending);var finalValues=new Dictionary<string,string>{{"revision",lastRevision.ToString()},{"confirmado","true"}};
 await Post(owner,$"/Estudiante/Evaluaciones/{pending}/finalizar",finalValues,$"/Estudiante/Evaluaciones/{pending}/revisar");
 var receipt=await Scalar("SELECT \"Comprobante\"::text FROM \"AsignacionesEvaluacion\" WHERE \"Id\"=@id",pending);
 await Post(owner,$"/Estudiante/Evaluaciones/{pending}/finalizar",finalValues,"/Administrador/Evaluaciones");Check(receipt==await Scalar("SELECT \"Comprobante\"::text FROM \"AsignacionesEvaluacion\" WHERE \"Id\"=@id",pending),"Doble envío conserva comprobante único");
 await Post(owner,$"/Estudiante/Evaluaciones/{pending}/guardar",new(){{"revision",(await Revision(pending)).ToString()},{"respuestas["+qs[0]+"]","1"}},"/Administrador/Evaluaciones");Check(await Scalar("SELECT sum(\"Valor\")::text FROM \"RespuestasEvaluacion\" WHERE \"AsignacionId\"=@id",pending)=="12","Finalizada rechaza cambios");
 var results=await Page(owner,"/Estudiante/Resultados");Check(results.Contains("Resultado pendiente de publicaci") && !results.Contains("Resultado ficticio:"),"No se filtran resultados privados");
 await Manage(finished,"publicar");Check((await Page(owner,"/Estudiante/Resultados")).Contains("Resultado ficticio:"),"Resultado publicado visible al titular");
 Check(!(await Page(student,"/Estudiante/Resultados")).Contains("Resultado ficticio:"),"Otro estudiante no ve el resultado publicado");
 var foreign=await student.GetAsync($"/Estudiante/Evaluaciones/{finished}");Check(foreign.StatusCode==HttpStatusCode.NotFound,"ID ajeno devuelve 404");
 var revisionBefore=await Revision(started);
 await Post(student,$"/Estudiante/Evaluaciones/{started}/guardar",new(){{"revision",revisionBefore.ToString()}},"/MiCuenta");
 Check(await Revision(started)==revisionBefore,"No se guardan respuestas de otro estudiante");
 var denied=await student.GetAsync("/Administrador/Evaluaciones");Check(denied.StatusCode==HttpStatusCode.Redirect && denied.Headers.Location!.ToString().Contains("AccesoDenegado"),"Controles administrativos protegidos");
 var csrf=await owner.PostAsync($"/Estudiante/Evaluaciones/{started}/finalizar",new FormUrlEncodedContent(new Dictionary<string,string>{{"confirmado","true"}}));Check(csrf.StatusCode==HttpStatusCode.BadRequest,"POST sin antifalsificación rechazado");
 await Manage(finished,"reabrir");Check(await Scalar("SELECT \"Estado\"::text FROM \"AsignacionesEvaluacion\" WHERE \"Id\"=@id",finished)=="2","Reapertura autorizada");Check(await Scalar("SELECT \"Publicado\"::text FROM \"ResultadosEvaluacion\" WHERE \"AsignacionId\"=@id",finished)=="false","Reapertura retira publicación");Check(await Scalar("SELECT count(*)::text FROM \"RespuestasEvaluacion\" WHERE \"AsignacionId\"=@id",finished)=="3","Reapertura conserva respuestas");
 foreach(var path in new[]{"/Panel","/Estudiante","/MiCuenta","/Estudiante/Citas","/MiCuenta/Editar","/Administrador/Tablero","/Administrador/Estudiantes","/Administrador","/Administrador/Importar"})await Page(owner,path);
 Console.WriteLine($"PRUEBAS COMPLETADAS: {checks} comprobaciones.");
} finally {
 await using var tx=await db.BeginTransactionAsync();
 foreach(var table in new[]{"RespuestasEvaluacion","ResultadosEvaluacion"}) {await using var del=new NpgsqlCommand("DELETE FROM \""+table+"\" WHERE \"AsignacionId\" IN (SELECT x.\"Id\" FROM jsonb_populate_recordset(NULL::\"AsignacionesEvaluacion\",CAST(@data AS jsonb)) x)",db,tx);del.Parameters.AddWithValue("data",snapshots["AsignacionesEvaluacion"]);await del.ExecuteNonQueryAsync();}
 await using(var reset=new NpgsqlCommand("UPDATE \"AsignacionesEvaluacion\" a SET \"Estado\"=x.\"Estado\",\"FechaInicioUtc\"=x.\"FechaInicioUtc\",\"FechaFinalizacionUtc\"=x.\"FechaFinalizacionUtc\",\"Comprobante\"=x.\"Comprobante\",\"Revision\"=x.\"Revision\" FROM jsonb_populate_recordset(NULL::\"AsignacionesEvaluacion\",CAST(@data AS jsonb)) x WHERE a.\"Id\"=x.\"Id\"",db,tx)){reset.Parameters.AddWithValue("data",snapshots["AsignacionesEvaluacion"]);await reset.ExecuteNonQueryAsync();}
 foreach(var table in new[]{"RespuestasEvaluacion","ResultadosEvaluacion"}){await using var put=new NpgsqlCommand("INSERT INTO \""+table+"\" SELECT * FROM jsonb_populate_recordset(NULL::\""+table+"\",CAST(@data AS jsonb))",db,tx);put.Parameters.AddWithValue("data",snapshots[table]);await put.ExecuteNonQueryAsync();}
 await tx.CommitAsync();Console.WriteLine("Las cuatro demostraciones volvieron a sus estados iniciales. Auditoría de pruebas conservada.");
}
