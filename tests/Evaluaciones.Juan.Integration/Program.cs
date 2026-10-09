// Desde la raíz, con ZUNI Development en 127.0.0.1:7111. Solo modifica las demos de Juan y las restaura en finally.
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Net;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Diagnostics;
using Npgsql;
const string juan="94093b8b-78a7-4eb4-9c54-2ee9ee8ba8c7", evin="464d9b0e-70fe-4b0a-aa97-e74682596898";
const string origin="http://127.0.0.1:7111";
var root=Path.GetFullPath("Zuni");
var cfg=new ConfigurationBuilder().SetBasePath(root).AddJsonFile("appsettings.json",true).AddJsonFile("appsettings.Development.json",true).AddUserSecrets("df74818a-cd0f-4ce3-9d60-29cf450de409").AddEnvironmentVariables().Build();
var cs=new NpgsqlConnectionStringBuilder(cfg.GetConnectionString("DefaultConnection"));
if(cs.Database!="ZuniDesarrollo" || !new[]{"localhost","127.0.0.1","::1"}.Contains(cs.Host))throw new Exception("Base no autorizada");
await using var db=new NpgsqlConnection(cs.ConnectionString);await db.OpenAsync();
async Task<string> Scalar(string sql,Guid? id=null){await using var c=new NpgsqlCommand(sql,db);if(id.HasValue)c.Parameters.AddWithValue("id",id.Value);return (string)(await c.ExecuteScalarAsync())!;}
async Task<int> Revision(Guid id)=>int.Parse(await Scalar("SELECT \"Revision\"::text FROM \"AsignacionesEvaluacion\" WHERE \"Id\"=@id",id));
async Task<string> State(Guid id)=>await Scalar("SELECT \"Estado\"::text FROM \"AsignacionesEvaluacion\" WHERE \"Id\"=@id",id);
var selection="SELECT a.\"Id\" FROM \"AsignacionesEvaluacion\" a JOIN \"Evaluaciones\" e ON e.\"Id\"=a.\"EvaluacionId\" WHERE e.\"EsDemostracion\" AND e.\"Codigo\" IN ('ZUNI-DEMO-JUAN-01','ZUNI-DEMO-03','ZUNI-DEMO-02','ZUNI-DEMO-JUAN-04') AND a.\"EstudianteId\"='"+juan+"'";
async Task<string> Snapshot(string table){var key=table=="AsignacionesEvaluacion"?"Id":"AsignacionId";return await Scalar("SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY to_jsonb(t)::text),'[]')::text FROM \""+table+"\" t WHERE t.\""+key+"\" IN ("+selection+")");}
var snapshots=new Dictionary<string,string>();foreach(var t in new[]{"AsignacionesEvaluacion","RespuestasEvaluacion","ResultadosEvaluacion"})snapshots[t]=await Snapshot(t);
var rows=JsonDocument.Parse(snapshots["AsignacionesEvaluacion"]);
if(rows.RootElement.GetArrayLength()!=4)throw new Exception("Se requieren exactamente cuatro demostraciones");
Guid Assigned(int state)=>rows.RootElement.EnumerateArray().Single(a=>a.GetProperty("Estado").GetInt32()==state).GetProperty("Id").GetGuid();
var blocked=Assigned(0);var pending=Assigned(1);var started=Assigned(2);var finished=Assigned(3);
// Huellas de todas las tablas: la auditoría puede aumentar; las tres tablas de prueba se comparan fuera de Juan.
var tables=new List<string>();await using(var c=new NpgsqlCommand("SELECT tablename FROM pg_tables WHERE schemaname='public' ORDER BY tablename",db))await using(var r=await c.ExecuteReaderAsync())while(await r.ReadAsync())tables.Add(r.GetString(0));
async Task<string> Digest(string table){var where=snapshots.ContainsKey(table)?" WHERE t.\""+(table=="AsignacionesEvaluacion"?"Id":"AsignacionId")+"\" NOT IN ("+selection+")":"";return await Scalar("SELECT md5(coalesce(string_agg(to_jsonb(t)::text,'|' ORDER BY to_jsonb(t)::text),'')) FROM \""+table.Replace("\"","\"\"")+"\" t"+where);}
var before=new Dictionary<string,string>();foreach(var t in tables.Where(t=>t!="AuditoriaUsuarios"))before[t]=await Digest(t);
Directory.CreateDirectory(".visual-check/juan-verification");await File.WriteAllTextAsync(".visual-check/juan-verification/estado-inicial.json",JsonSerializer.Serialize(snapshots));
var provider=DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(root,"App_Data","keys")),b=>b.SetApplicationName("Zuni"));
var formatter=new TicketDataFormat(provider.CreateProtector("Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationMiddleware","Cookies","v2"));
async Task<string> Cookie(string userId,string? stampOverride=null,bool expired=false){
 var stamp=stampOverride??await Scalar("SELECT \"SecurityStamp\" FROM \"AspNetUsers\" WHERE \"Id\"='"+userId+"'");
 var roles=(await Scalar("SELECT string_agg(r.\"Name\",',') FROM \"AspNetUserRoles\" ur JOIN \"AspNetRoles\" r ON r.\"Id\"=ur.\"RoleId\" WHERE ur.\"UserId\"='"+userId+"'")).Split(',');
 var claims=new List<Claim>{new(ClaimTypes.NameIdentifier,userId),new(ClaimTypes.Name,"Prueba local"),new("Zuni.SecurityStamp",stamp)};claims.AddRange(roles.Select(r=>new Claim(ClaimTypes.Role,r)));
 return formatter.Protect(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims,"Cookies")),new AuthenticationProperties{IssuedUtc=DateTimeOffset.UtcNow.AddMinutes(-2),ExpiresUtc=expired?DateTimeOffset.UtcNow.AddMinutes(-1):DateTimeOffset.UtcNow.AddMinutes(20)},"Cookies"));
}
HttpClient Client(string? cookie=null){var jar=new CookieContainer();if(cookie!=null)jar.Add(new Uri(origin),new Cookie(".AspNetCore.Cookies",cookie));return new HttpClient(new HttpClientHandler{AllowAutoRedirect=false,CookieContainer=jar}){BaseAddress=new Uri(origin)};}
using var owner=Client(await Cookie(juan));using var admin=Client(await Cookie(evin));using var other=Client(await Cookie(evin));
int checks=0;void Check(bool ok,string name){if(!ok)throw new Exception("FALLO: "+name);checks++;Console.WriteLine("OK: "+name);}
async Task<string> Page(HttpClient client,string path){var r=await client.GetAsync(path);Check(r.StatusCode==HttpStatusCode.OK,"GET "+path);return await r.Content.ReadAsStringAsync();}
string Token(string html)=>WebUtility.HtmlDecode(Regex.Match(html,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
async Task Post(HttpClient client,string path,Dictionary<string,string> values,string page){values["__RequestVerificationToken"]=Token(await Page(client,page));var r=await client.PostAsync(path,new FormUrlEncodedContent(values));Check(r.StatusCode==HttpStatusCode.Redirect,"POST "+path);}
async Task Manage(Guid id,string op)=>await Post(admin,"/Administrador/GestionarEvaluacion",new(){{"id",id.ToString()},{"revision",(await Revision(id)).ToString()},{"operacion",op}},"/Administrador/Evaluaciones");
try{
 var home=await Page(owner,"/Estudiante/Evaluaciones");
 foreach(var (state,title) in new[]{("Asignada","Bienestar emocional"),("Pendiente","Adaptación universitaria"),("EnProceso","Hábitos de estudio"),("Finalizada","Bienestar académico")}){
  var cards=Regex.Matches(home,"<article[^>]*data-estado=\""+state+"\"[\\s\\S]*?</article>");Check(cards.Count==1 && WebUtility.HtmlDecode(cards[0].Value).Contains(title),"Una tarjeta correcta en "+state);
 }
 Check(home.Contains("33 %"),"Progreso inicial 33 %");Check(!home.Contains("Rutinas de bienestar")&&!home.Contains("Organizaci&#xF3;n del tiempo"),"Listado no incluye demos exclusivas de Evin");
 var finalPage=await Page(owner,$"/Estudiante/Evaluaciones/{finished}");Check(finalPage.Contains("Comprobante")&&!finalPage.Contains("id=\"respuestas-form\""),"Finalizada presenta comprobante sin formulario editable");
 var results=WebUtility.HtmlDecode(await Page(owner,"/Estudiante/Resultados"));Check(results.Contains("Bienestar académico")&&results.Contains("Puntuación: 9")&&results.Contains("Resultado ficticio de Bienestar académico"),"Resultado autorizado de Juan visible");
 await Manage(finished,"ocultar");results=WebUtility.HtmlDecode(await Page(owner,"/Estudiante/Resultados"));Check(results.Contains("Resultado pendiente de publicación")&&!results.Contains("Puntuación:")&&!results.Contains("Resultado ficticio"),"Resultado oculto no expone puntuación ni observaciones");await Manage(finished,"publicar");
 await Post(owner,$"/Estudiante/Evaluaciones/{blocked}/iniciar",new(){{"revision",(await Revision(blocked)).ToString()}},$"/Estudiante/Evaluaciones/{blocked}");Check(await State(blocked)=="0","Asignada permanece sin iniciar");
 await Post(owner,$"/Estudiante/Evaluaciones/{pending}/iniciar",new(){{"revision",(await Revision(pending)).ToString()}},$"/Estudiante/Evaluaciones/{pending}");Check(await State(pending)=="2","Pendiente cambia a En proceso en PostgreSQL");
 // Prueba de navegador: continúa Hábitos, cancela y finaliza; la comprobación SQL sigue después.
 var qs=JsonDocument.Parse(await Scalar("SELECT jsonb_agg(p.\"Id\" ORDER BY p.\"Orden\")::text FROM \"PreguntasEvaluacion\" p JOIN \"AsignacionesEvaluacion\" a ON a.\"EvaluacionId\"=p.\"EvaluacionId\" WHERE a.\"Id\"=@id",started)).RootElement.EnumerateArray().Select(x=>x.GetGuid()).ToArray();
 var stale=await Revision(started);
 var input=JsonSerializer.Serialize(new{cookie=await Cookie(juan),id=started,questions=qs,origin});
 var procInfo=new ProcessStartInfo("node"){RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false};procInfo.ArgumentList.Add("tests/evaluaciones-browser.cjs");
 using(var proc=Process.Start(procInfo)!){await proc.StandardInput.WriteAsync(input);proc.StandardInput.Close();var output=proc.StandardOutput.ReadToEndAsync();var error=proc.StandardError.ReadToEndAsync();await proc.WaitForExitAsync();Console.Write(await output);Check(proc.ExitCode==0,"Chromium: guardado, cierre de sesión, progreso, diálogo, cancelación y confirmación. "+await error);}
 Check(await State(started)=="3","Confirmar cambia estado a Finalizada");Check(await Scalar("SELECT count(*)::text FROM \"RespuestasEvaluacion\" WHERE \"AsignacionId\"=@id",started)=="3","Confirmación conserva tres respuestas en PostgreSQL");
 Check(await Scalar("SELECT (\"FechaFinalizacionUtc\" IS NOT NULL AND \"Comprobante\" IS NOT NULL)::text FROM \"AsignacionesEvaluacion\" WHERE \"Id\"=@id",started)=="true","Fecha y comprobante registrados");
 var receipt=await Scalar("SELECT \"Comprobante\"::text FROM \"AsignacionesEvaluacion\" WHERE \"Id\"=@id",started);
 await Post(owner,$"/Estudiante/Evaluaciones/{started}/finalizar",new(){{"revision",stale.ToString()},{"confirmado","true"}},"/MiCuenta");Check(receipt==await Scalar("SELECT \"Comprobante\"::text FROM \"AsignacionesEvaluacion\" WHERE \"Id\"=@id",started),"Doble envío conserva comprobante");
 var saved=await Snapshot("RespuestasEvaluacion");await Post(owner,$"/Estudiante/Evaluaciones/{started}/guardar",new(){{"revision",(await Revision(started)).ToString()},{"respuestas["+qs[0]+"]","1"}},"/MiCuenta");Check(saved==await Snapshot("RespuestasEvaluacion"),"Finalizada rechaza modificación por POST");
 results=WebUtility.HtmlDecode(await Page(owner,"/Estudiante/Resultados"));Check(results.Contains("Resultado pendiente de publicación")&&!results.Contains("Resultado de demostración: suma"),"Nueva finalización no publica automáticamente");
 // Validación del servidor ante finalización incompleta, no confirmada, respuestas manipuladas y versión antigua.
 await Post(owner,$"/Estudiante/Evaluaciones/{pending}/finalizar",new(){{"revision",(await Revision(pending)).ToString()},{"confirmado","true"}},$"/Estudiante/Evaluaciones/{pending}/revisar");Check(await State(pending)=="2","Obligatorias incompletas impiden finalización");
 await Post(owner,$"/Estudiante/Evaluaciones/{pending}/finalizar",new(){{"revision",(await Revision(pending)).ToString()},{"confirmado","false"}},$"/Estudiante/Evaluaciones/{pending}/revisar");Check(await State(pending)=="2","Sin confirmación no cambia el estado");
 var rev=await Revision(pending);
 foreach(var (key,value) in new[]{("no-es-guid","3"),(qs[0].ToString(),"3"),(Guid.NewGuid().ToString(),"6")}){await Post(owner,$"/Estudiante/Evaluaciones/{pending}/guardar",new(){{"revision",rev.ToString()},{"respuestas["+key+"]",value}},$"/Estudiante/Evaluaciones/{pending}");Check(await Revision(pending)==rev,"Respuesta manipulada rechazada: "+key);}
 var pq=Guid.Parse(JsonDocument.Parse(await Scalar("SELECT jsonb_agg(p.\"Id\" ORDER BY p.\"Orden\")::text FROM \"PreguntasEvaluacion\" p JOIN \"AsignacionesEvaluacion\" a ON a.\"EvaluacionId\"=p.\"EvaluacionId\" WHERE a.\"Id\"=@id",pending)).RootElement[0].GetString()!);
 await Post(owner,$"/Estudiante/Evaluaciones/{pending}/guardar",new(){{"revision",rev.ToString()},{"respuestas["+pq+"]","3"}},$"/Estudiante/Evaluaciones/{pending}");
 await Post(owner,$"/Estudiante/Evaluaciones/{pending}/guardar",new(){{"revision",rev.ToString()},{"respuestas["+pq+"]","5"}},$"/Estudiante/Evaluaciones/{pending}");Check(await Scalar("SELECT \"Valor\"::text FROM \"RespuestasEvaluacion\" WHERE \"AsignacionId\"=@id",pending)=="3","Versión obsoleta no sobrescribe respuesta");
 foreach(var id in new[]{blocked,pending,started,finished})foreach(var suffix in new[]{"","/revisar"})Check((await other.GetAsync($"/Estudiante/Evaluaciones/{id}{suffix}")).StatusCode==HttpStatusCode.NotFound,"Lectura ajena devuelve 404 "+id+suffix);
 var protectedState=await Snapshot("AsignacionesEvaluacion");var protectedResponses=await Snapshot("RespuestasEvaluacion");var protectedResults=await Snapshot("ResultadosEvaluacion");
 foreach(var op in new[]{"iniciar","guardar","finalizar"})await Post(other,$"/Estudiante/Evaluaciones/{pending}/{op}",new(){{"revision",(await Revision(pending)).ToString()},{"confirmado","true"},{"respuestas["+pq+"]","5"}},"/MiCuenta");
 Check(protectedState==await Snapshot("AsignacionesEvaluacion")&&protectedResponses==await Snapshot("RespuestasEvaluacion")&&protectedResults==await Snapshot("ResultadosEvaluacion"),"Otro estudiante no puede modificar estado, respuestas ni resultados");
 var foreignResults=WebUtility.HtmlDecode(await Page(other,"/Estudiante/Resultados?estudianteId="+juan+"&id="+finished));Check(!foreignResults.Contains("Bienestar académico")&&!foreignResults.Contains("Resultado ficticio de Bienestar académico"),"Parámetros ajenos no filtran resultados de Juan");
 foreach(var path in new[]{"/Administrador/Evaluaciones","/Administrador/Estudiantes"}){var denied=await owner.GetAsync(path);Check(denied.StatusCode==HttpStatusCode.Redirect&&denied.Headers.Location!.ToString().Contains("AccesoDenegado"),"Juan no accede a "+path);}
 var adminBefore=await Snapshot("AsignacionesEvaluacion");await Post(owner,"/Administrador/GestionarEvaluacion",new(){{"id",blocked.ToString()},{"revision",(await Revision(blocked)).ToString()},{"operacion","habilitar"}},"/MiCuenta");Check(adminBefore==await Snapshot("AsignacionesEvaluacion"),"POST administrativo de estudiante no modifica datos");
 foreach(var op in new[]{"iniciar","guardar","finalizar"})Check((await owner.PostAsync($"/Estudiante/Evaluaciones/{pending}/{op}",new FormUrlEncodedContent(new Dictionary<string,string>()))).StatusCode==HttpStatusCode.BadRequest,"Sin CSRF se rechaza "+op);
 foreach(var cookie in new string?[]{null,"cookie-invalida",await Cookie(juan,"sello-invalido"),await Cookie(juan,expired:true)}){using var invalid=Client(cookie);foreach(var path in new[]{"/Estudiante/Evaluaciones","/Estudiante/Resultados",$"/Estudiante/Evaluaciones/{finished}"}){var denied=await invalid.GetAsync(path);Check(denied.StatusCode==HttpStatusCode.Redirect&&denied.Headers.Location!.ToString().Contains("IniciarSesion"),"Sesión ausente/inválida/vencida rechazada: "+path);}}
 using(var anonymous=Client()){var login=await Page(anonymous,"/Cuenta/IniciarSesion");var rejected=await anonymous.PostAsync("/Cuenta/IniciarSesion",new FormUrlEncodedContent(new Dictionary<string,string>{{"Email","jperezs@miumg.edu.gt"},{"Password",Guid.NewGuid().ToString()},{"__RequestVerificationToken",Token(login)}}));Check(rejected.StatusCode==HttpStatusCode.OK&&WebUtility.HtmlDecode(await rejected.Content.ReadAsStringAsync()).Contains("incorrectos"),"Login real rechaza contraseña incorrecta sin cambiar credenciales");}
 var singlePanel=await owner.GetAsync("/Panel");Check(singlePanel.StatusCode==HttpStatusCode.Redirect&&singlePanel.Headers.Location!.ToString().Contains("AccesoDenegado"),"Selector de dos roles restringido; Juan usa directamente Estudiante");
 foreach(var path in new[]{"/Estudiante","/MiCuenta","/Estudiante/Citas","/MiCuenta/Editar"})await Page(owner,path);
 foreach(var path in new[]{"/Panel","/Administrador/Tablero","/Administrador/Estudiantes","/Administrador","/Administrador/Importar","/Estudiante"})await Page(admin,path);
}finally{
 await using var tx=await db.BeginTransactionAsync();
 foreach(var table in new[]{"RespuestasEvaluacion","ResultadosEvaluacion"}){await using var del=new NpgsqlCommand("DELETE FROM \""+table+"\" WHERE \"AsignacionId\" IN (SELECT x.\"Id\" FROM jsonb_populate_recordset(NULL::\"AsignacionesEvaluacion\",CAST(@data AS jsonb)) x)",db,tx);del.Parameters.AddWithValue("data",snapshots["AsignacionesEvaluacion"]);await del.ExecuteNonQueryAsync();}
 await using(var reset=new NpgsqlCommand("UPDATE \"AsignacionesEvaluacion\" a SET \"Estado\"=x.\"Estado\",\"FechaInicioUtc\"=x.\"FechaInicioUtc\",\"FechaFinalizacionUtc\"=x.\"FechaFinalizacionUtc\",\"Comprobante\"=x.\"Comprobante\",\"Revision\"=x.\"Revision\" FROM jsonb_populate_recordset(NULL::\"AsignacionesEvaluacion\",CAST(@data AS jsonb)) x WHERE a.\"Id\"=x.\"Id\"",db,tx)){reset.Parameters.AddWithValue("data",snapshots["AsignacionesEvaluacion"]);await reset.ExecuteNonQueryAsync();}
 foreach(var table in new[]{"RespuestasEvaluacion","ResultadosEvaluacion"}){await using var put=new NpgsqlCommand("INSERT INTO \""+table+"\" SELECT * FROM jsonb_populate_recordset(NULL::\""+table+"\",CAST(@data AS jsonb))",db,tx);put.Parameters.AddWithValue("data",snapshots[table]);await put.ExecuteNonQueryAsync();}
 await tx.CommitAsync();
 foreach(var t in snapshots.Keys)Check(snapshots[t]==await Snapshot(t),"Restauración exacta de Juan: "+t);
 foreach(var t in before.Keys)Check(before[t]==await Digest(t),"Datos preservados: "+t);
}
Console.WriteLine($"PRUEBAS COMPLETADAS: {checks} comprobaciones HTTP/PostgreSQL; navegador real ejecutado. Auditoría administrativa conservada.");
