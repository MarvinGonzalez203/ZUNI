// Únicamente crea una base PostgreSQL temporal local y la elimina al terminar.
// Arranca la aplicación real con conexión temporal y realiza login/antifalsificación HTTP.
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Npgsql;
using Zuni.Data;
using Zuni.Models;
using Zuni.Models.Evaluaciones;
using Zuni.Services;

int checks=0;
void Check(bool ok,string label){if(!ok)throw new Exception("FALLO: "+label);checks++;Console.WriteLine("OK: "+label);}
async Task Reject(Func<Task> action,string label)
{
    try{await action();}catch(EvaluacionOperacionException){Check(true,label);return;}
    throw new Exception("FALLO: "+label);
}
Check(Ipip50.Items.Count==50 && Ipip50.Items.Select(i=>i.Id).Distinct().Count()==50,"50 ítems únicos");
foreach(var trait in "OCEAN")Check(Ipip50.Items.Count(i=>i.Rasgo==trait)==10,"10 ítems para "+trait);
var key=new[]{"E+","A-","C+","N+","O+","E-","A+","C-","N-","O-","E+","A-","C+","N+","O+","E-","A+","C-","N-","O-","E+","A-","C+","N+","O+","E-","A+","C-","N+","O-","E+","A-","C+","N+","O+","E-","A+","C-","N+","O+","E+","A+","C+","N+","O+","E-","A+","C+","N+","O+"};
Check(Ipip50.Items.Select(i=>i.Rasgo+(i.Inversa?"-":"+")).SequenceEqual(key),"Clave oficial completa, IV invertido a Neuroticismo");
foreach(var endpoint in new[]{1,5})
{
    var result=Ipip50.Calcular(Ipip50.Items.Select(i=>new RespuestaEvaluacion{PreguntaId=i.Id,Valor=i.Inversa?6-endpoint:endpoint}));
    Check(result.Values.All(v=>v==endpoint),"Extremo uniforme corregido "+endpoint);
}
Check(Ipip50.Calcular(Ipip50.Items.Select(i=>new RespuestaEvaluacion{PreguntaId=i.Id,Valor=3})).Values.All(v=>v==3),"Neutral produce 3 en todas las dimensiones");
var allFive=Ipip50.Calcular(Ipip50.Items.Select(i=>new RespuestaEvaluacion{PreguntaId=i.Id,Valor=5}));
Check(allFive['E']==3 && allFive['A']==3.4m && allFive['C']==3.4m && allFive['N']==4.2m && allFive['O']==3.8m,"Todas las respuestas 5: cinco medias esperadas independientes");
var orderId=Guid.NewGuid();var order=Ipip50.Orden(orderId);
Check(order.Select(i=>i.Id).SequenceEqual(Ipip50.Orden(orderId).Select(i=>i.Id)),"Orden estable al retomar");
Check(!order.Select(i=>i.Id).SequenceEqual(Ipip50.Orden(Guid.NewGuid()).Select(i=>i.Id)),"Órdenes diferentes por aplicación");
for(var page=0;page<5;page++)Check(order.Skip(page*10).Take(10).GroupBy(i=>i.Rasgo).All(g=>g.Count()==2),"Bloque equilibrado "+(page+1));
await Reject(()=>Task.Run(()=>Ipip50.Calcular(Ipip50.Items.Take(49).Select(i=>new RespuestaEvaluacion{PreguntaId=i.Id,Valor=3}))),"No calcula con respuestas incompletas");

var cfg=new ConfigurationBuilder().AddUserSecrets("df74818a-cd0f-4ce3-9d60-29cf450de409").AddEnvironmentVariables().Build();
var cs=new NpgsqlConnectionStringBuilder(cfg.GetConnectionString("DefaultConnection"));
if(!new[]{"localhost","127.0.0.1","::1"}.Contains(cs.Host))throw new Exception("Solo PostgreSQL local.");
var name="ZuniBigFiveTest_"+Guid.NewGuid().ToString("N");
await using var administrative=new NpgsqlConnection(new NpgsqlConnectionStringBuilder(cs.ConnectionString){Database="postgres"}.ConnectionString);
await administrative.OpenAsync();bool created=false;Process? app=null;
var testCs=new NpgsqlConnectionStringBuilder(cs.ConnectionString){Database=name};
ApplicationDbContext Db()=>new(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(testCs.ConnectionString).Options);
var pilotCfg=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"BigFive:PruebaLocal","true"}}).Build();
BigFiveService Bf(ApplicationDbContext db)=>new(db,pilotCfg,new LocalEnvironment(),new AsignacionPsicologoService(db));
var student=Guid.NewGuid().ToString();var other=Guid.NewGuid().ToString();var psych=Guid.NewGuid().ToString();var stranger=Guid.NewGuid().ToString();var administrator=Guid.NewGuid().ToString();var director=Guid.NewGuid().ToString();
var password="LocalTest!"+Guid.NewGuid().ToString("N");
try
{
    await using(var cmd=new NpgsqlCommand("CREATE DATABASE \""+name+"\"",administrative))await cmd.ExecuteNonQueryAsync();created=true;
    await using(var db=Db())
    {
        // Una base con Evaluaciones ya aplicada también debe poder llegar al modelo final.
        await db.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>().MigrateAsync("20261009043711_AddEvaluaciones");
        await db.Database.MigrateAsync();Check(!(await db.Database.GetPendingMigrationsAsync()).Any(),"Actualización real desde Evaluaciones completa");
        var roles=new[]{"Estudiante","Psicologo","Administrador","Director"}.Select(n=>new IdentityRole(n){Id=Guid.NewGuid().ToString(),NormalizedName=n.ToUpperInvariant()}).ToArray();db.Roles.AddRange(roles);
        foreach(var (id,role) in new[]{(student,"Estudiante"),(other,"Estudiante"),(psych,"Psicologo"),(stranger,"Psicologo"),(administrator,"Administrador"),(director,"Director")})
        {
            var user=new ApplicationUser{Id=id,FullName="FICTICIO "+role+(id==student?" titular":" control"),UserName=id+"@example.invalid",Email=id+"@example.invalid",NormalizedEmail=(id+"@example.invalid").ToUpperInvariant(),SecurityStamp=Guid.NewGuid().ToString(),IsActive=id!=stranger};
            user.PasswordHash=new PasswordHasher<ApplicationUser>().HashPassword(user,password);db.Users.Add(user);
            db.UserRoles.Add(new IdentityUserRole<string>{UserId=id,RoleId=roles.Single(r=>r.Name==role).Id});
            if(role=="Estudiante")db.PerfilesEstudiante.Add(new PerfilEstudiante{Id=Guid.NewGuid(),UsuarioId=id,Carne=id==student?"20262001":"20262002",Telefono="55555555",Carrera="Prueba ficticia"});
        }
        await db.SaveChangesAsync();
        Check(!new BigFiveService(db,pilotCfg,new LocalEnvironment{EnvironmentName="Production"},new AsignacionPsicologoService(db)).Disponible,"Producción cerrada con política incompleta");
        await Reject(()=>Bf(db).Aceptar(student,new(){Acepto=false,MayorDeEdad=true,MotivoConsulta="Ejemplo ficticio"},default),"No inicia sin consentimiento");
        await Reject(()=>Bf(db).Aceptar(student,new(){Acepto=true,MayorDeEdad=false,MotivoConsulta="Ejemplo ficticio"},default),"No inicia para menor declarado");
    }
    const string origin="http://127.0.0.1:7129";
    var start=new ProcessStartInfo("dotnet"){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true,CreateNoWindow=true};
    start.ArgumentList.Add(Path.GetFullPath("Zuni/bin/BigFiveCheck/net8.0/Zuni.dll"));
    start.ArgumentList.Add("--contentRoot");start.ArgumentList.Add(Path.GetFullPath("Zuni"));
    start.Environment["ASPNETCORE_ENVIRONMENT"]="Development";start.Environment["ASPNETCORE_URLS"]=origin;start.Environment["ConnectionStrings__DefaultConnection"]=testCs.ConnectionString;
    start.Environment["Logging__LogLevel__Default"]="Error";start.Environment["Logging__LogLevel__Microsoft"]="Error";
    app=Process.Start(start)!;app.OutputDataReceived+=(_,_)=>{};app.ErrorDataReceived+=(_,_)=>{};app.BeginOutputReadLine();app.BeginErrorReadLine();
    HttpClient Client()=>new(new HttpClientHandler{AllowAutoRedirect=false,CookieContainer=new CookieContainer()}){BaseAddress=new Uri(origin)};
    using var owner=Client();using var pClient=Client();using var otherClient=Client();using var adminClient=Client();using var dirClient=Client();using var strangerClient=Client();using var anonymous=Client();
    bool ready=false;for(int i=0;i<80;i++){try{if((await owner.GetAsync("/Cuenta/IniciarSesion")).StatusCode==HttpStatusCode.OK){ready=true;break;}}catch(HttpRequestException){}await Task.Delay(250);}
    Check(ready,"Aplicación temporal disponible");
    async Task<string> Page(HttpClient client,string path){var response=await client.GetAsync(path);Check(response.StatusCode==HttpStatusCode.OK,"GET "+path);return await response.Content.ReadAsStringAsync();}
    string Token(string html)=>WebUtility.HtmlDecode(Regex.Match(html,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
    async Task<HttpResponseMessage> Post(HttpClient client,string path,Dictionary<string,string> values,string from){values["__RequestVerificationToken"]=Token(await Page(client,from));return await client.PostAsync(path,new FormUrlEncodedContent(values));}
    async Task Login(HttpClient client,string id){var response=await Post(client,"/Cuenta/IniciarSesion",new(){{"Email",id+"@example.invalid"},{"Password",password}},"/Cuenta/IniciarSesion");Check(response.StatusCode==HttpStatusCode.Redirect,"Login real "+(id==student?"titular":"control"));}
    await Login(owner,student);await Login(pClient,psych);await Login(otherClient,other);await Login(adminClient,administrator);await Login(dirClient,director);
    Check((await anonymous.GetAsync("/Estudiante/BigFive")).StatusCode==HttpStatusCode.Redirect,"Anónimo no entra");
    Directory.CreateDirectory(".visual-check/bigfive");
    var infoHtml=await Page(owner,"/Estudiante/BigFive");
    await File.WriteAllTextAsync(".visual-check/bigfive/consent.html",infoHtml);
    var info=WebUtility.HtmlDecode(infoHtml);Check(info.Contains("PRUEBA LOCAL")&&info.Contains("Esto no es un diagnóstico médico")&&info.Contains("pendiente de definir"),"Información, límites y política pendientes visibles");
    Check((await owner.PostAsync("/Estudiante/BigFive/aceptar",new FormUrlEncodedContent(new Dictionary<string,string>()))).StatusCode==HttpStatusCode.BadRequest,"Antifalsificación exige token");
    var consent=await Post(owner,"/Estudiante/BigFive/aceptar",new(){{"Acepto","true"},{"MayorDeEdad","true"},{"MotivoConsulta","Motivo FICTICIO de integración"},{"Referencia","Docente"}},"/Estudiante/BigFive");
    Check(consent.StatusCode==HttpStatusCode.Redirect,"Consentimiento por HTTP");
    Guid assignment;await using(var db=Db())
    {
        var participation=await Bf(db).Propia(student,default);Check(participation!=null && participation.PsicologoId==psych,"Asignación automática al único profesional");assignment=participation!.AsignacionId;
        Check(participation.PruebaLocal && participation.TextoConsentimiento.Contains("PRUEBA LOCAL")&&participation.VersionConsentimiento==BigFiveService.ConsentimientoVersion,"Consentimiento versionado y texto conservado");
    }
    var questionnaire=await Page(owner,"/Estudiante/BigFive/cuestionario");
    await File.WriteAllTextAsync(".visual-check/bigfive/questionnaire.html",questionnaire);
    Check(Regex.Matches(questionnaire,"class=\"bf-page\"").Count==5 && Regex.Matches(questionnaire,"class=\"dash-card bf-question\"").Count==50,"Cinco bloques con 50 afirmaciones");
    Check(!(await Page(adminClient,"/Administrador/Evaluaciones")).Contains("Big Five · IPIP-50"),"Administrador no lista Big Five");
    Check((await adminClient.GetAsync("/Psicologo/Resultados")).StatusCode==HttpStatusCode.Redirect,"Administrador no ve resultados del psicólogo");
    Check((await dirClient.GetAsync("/Psicologo/Resultados")).StatusCode==HttpStatusCode.Redirect,"Director no ve resultados del psicólogo");
    Check((await otherClient.GetAsync("/Estudiante/Evaluaciones/"+assignment)).StatusCode==HttpStatusCode.NotFound,"Otro estudiante no ve las respuestas");
    Check((await owner.GetAsync("/Estudiante/Evaluaciones/"+assignment)).StatusCode==HttpStatusCode.NotFound,"Ruta general no elude el flujo Big Five");
    async Task<int> Revision(){await using var db=Db();return await db.Set<AsignacionEvaluacion>().Where(a=>a.Id==assignment).Select(a=>a.Revision).SingleAsync();}
    var partial=new Dictionary<string,string>{{"id",assignment.ToString()},{"revision",(await Revision()).ToString()},{"respuestas["+Ipip50.Items[0].Id+"]","4"}};
    Check((await Post(owner,"/Estudiante/BigFive/guardar",partial,"/Estudiante/BigFive/cuestionario")).StatusCode==HttpStatusCode.Redirect,"Guardar parcial HTTP");
    await using(var db=Db())Check(await db.Set<RespuestaEvaluacion>().AnyAsync(r=>r.AsignacionId==assignment&&r.Valor==4),"Avance persiste en otra conexión");
    var stolen=await Post(otherClient,"/Estudiante/BigFive/guardar",new(){{"id",assignment.ToString()},{"revision",(await Revision()).ToString()}},"/Estudiante/BigFive");Check(stolen.StatusCode==HttpStatusCode.NotFound,"Escritura ajena rechazada");
    await using(var db=Db())await Reject(()=>new EvaluacionesService(db,Bf(db)).Finalizar(assignment,student,1,true,default),"No finaliza con 49 faltantes");
    var answers=new Dictionary<string,string>{{"id",assignment.ToString()},{"revision",(await Revision()).ToString()}};
    foreach(var item in Ipip50.Items)answers["respuestas["+item.Id+"]"]=(item.Inversa?2:4).ToString();
    await Post(owner,"/Estudiante/BigFive/guardar",answers,"/Estudiante/BigFive/cuestionario");
    var final=await Post(owner,"/Estudiante/BigFive/finalizar",new(){{"id",assignment.ToString()},{"revision",(await Revision()).ToString()},{"confirmado","true"}},"/Estudiante/BigFive/cuestionario");Check(final.StatusCode==HttpStatusCode.Redirect,"Finaliza por HTTP");
    await using(var db=Db())
    {
        var p=await db.Set<ParticipacionBigFive>().SingleAsync();Check(Ipip50.Dimensiones(p).All(d=>d.Media==4),"Persistencia de cinco medias corregidas");
        Check((await db.Set<AsignacionEvaluacion>().SingleAsync()).Comprobante!=null,"Comprobante generado");
        Check(await Bf(db).PuedeSolicitarCita(student,default),"Finalizado cumple requisito del futuro flujo de citas");
        await Reject(async ()=>await new EvaluacionesService(db,Bf(db)).Administrar(assignment,administrator,await Revision(),"reabrir",default),"Administrador no reabre Big Five");
    }
    var summaryHtml=await Page(pClient,"/Psicologo/Resultados");
    await File.WriteAllTextAsync(".visual-check/bigfive/results.html",summaryHtml);
    var summary=WebUtility.HtmlDecode(summaryHtml);Check(summary.Contains("4.00")||summary.Contains("4,00"),"Psicólogo recibe medias");
    Check(summary.Contains("Motivo FICTICIO de integración")&&!summary.Contains(Ipip50.Items[0].Texto)&&!summary.Contains("respuestas["),"Resumen sin preguntas ni respuestas individuales");
    await using(var db=Db())
    {
        Check(await db.Set<AccesoBigFive>().CountAsync()==1,"Acceso profesional registrado sin respuestas");
        var approved=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"BigFive:Habilitado","true"},{"BigFive:RevisionProfesionalAprobada","true"},{"BigFive:Institucion","Institución ficticia"},{"BigFive:ContactoPrivacidad","contacto ficticio"},{"BigFive:Conservacion","política ficticia"},{"BigFive:UbicacionDatos","servidor ficticio"}}).Build();
        var institutional=new BigFiveService(db,approved,new LocalEnvironment{EnvironmentName="Production"},new AsignacionPsicologoService(db));
        Check(institutional.Disponible && !await institutional.Vigentes().AnyAsync(),"Un perfil de prueba nunca aparece como institucional");
        var professional=await db.Users.SingleAsync(u=>u.Id==psych);professional.IsActive=false;await db.SaveChangesAsync();
        Check(!await Bf(db).Vigentes().AnyAsync(),"Profesional inactivo pierde acceso");professional.IsActive=true;await db.SaveChangesAsync();
        var link=await db.AsignacionesEstudiantePsicologo.SingleAsync();link.FechaFinalizacionUtc=DateTime.UtcNow;await db.SaveChangesAsync();
        Check(!await Bf(db).Vigentes().AnyAsync(),"Vínculo finalizado pierde acceso");link.FechaFinalizacionUtc=null;await db.SaveChangesAsync();
        var u=await db.Users.SingleAsync(u=>u.Id==stranger);u.IsActive=true;await db.SaveChangesAsync();
    }
    await Login(strangerClient,stranger);
    var alienSummary=await Page(strangerClient,"/Psicologo/Resultados");Check(!alienSummary.Contains("Motivo FICTICIO"),"Otro psicólogo no recibe perfil");
    await using(var db=Db())await Reject(()=>Bf(db).Aceptar(other,new(){Acepto=true,MayorDeEdad=true,MotivoConsulta="Ejemplo ficticio"},default),"Múltiples psicólogos no asignan arbitrariamente");
    var end=await Page(owner,"/Estudiante/BigFive/cuestionario");Check(end.Contains("Comprobante")&&!end.Contains("id=\"bigfive-form\""),"Finalizado bloquea edición");
    Check(WebUtility.HtmlDecode(await Page(owner,"/Estudiante/Citas")).Contains("Tu Big Five está finalizado"),"Citas reconoce la finalización sin habilitar reservas ficticias");
    await Post(owner,"/Estudiante/BigFive/retirar",new(),"/Estudiante/BigFive");
    Check(!(await Page(pClient,"/Psicologo/Resultados")).Contains("Motivo FICTICIO"),"Retiro oculta el perfil al profesional");
    await using(var db=Db())
    {
        await Reject(async ()=>await new EvaluacionesService(db,Bf(db)).Guardar(assignment,student,await Revision(),new(),default),"No guarda tras retirar consentimiento");
        Check(await db.Set<AccesoBigFive>().CountAsync()==1,"Sin nuevo acceso a perfil retirado");
        Check(!await Bf(db).PuedeSolicitarCita(student,default),"Retiro suspende requisito del futuro flujo de citas");
    }
    Console.WriteLine($"APROBADAS: {checks} comprobaciones Big Five, PostgreSQL y HTTP.");
}
finally
{
    if(app is not null && !app.HasExited){app.Kill(entireProcessTree:true);await app.WaitForExitAsync();}app?.Dispose();
    NpgsqlConnection.ClearAllPools();
    if(created){await using var cmd=new NpgsqlCommand("DROP DATABASE \""+name+"\" WITH (FORCE)",administrative);await cmd.ExecuteNonQueryAsync();Console.WriteLine("Base temporal eliminada; base de trabajo intacta.");}
}

sealed class LocalEnvironment : IWebHostEnvironment
{
    public string EnvironmentName{get;set;}="Development";
    public string ApplicationName{get;set;}="Zuni";
    public string WebRootPath{get;set;}="";
    public IFileProvider WebRootFileProvider{get;set;}=new NullFileProvider();
    public string ContentRootPath{get;set;}="";
    public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();
}
