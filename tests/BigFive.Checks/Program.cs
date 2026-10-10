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
var clock=new DateTime(2026,10,9,23,59,0);var clockDay=DateOnly.FromDateTime(clock);
var clockRange=new HorarioAgendaPsicologo{Inicio=new(8,0),Fin=new(18,0),DuracionMinutos=60};
Check(AgendaService.InicioDisponible(clockRange,clockDay,clock)==null,"Nunca reaparece disponibilidad al redondear hacia medianoche");
Check(AgendaService.InicioDisponible(clockRange,clockDay,clock.Date.AddHours(14).AddMinutes(10))==new TimeOnly(15,0),"Inicio futuro alineado a duración de citas");
Check(AgendaService.InicioDisponible(clockRange,clockDay,clock.Date.AddHours(17).AddMinutes(10))==null,"No ofrece fracción de cita al cerrar jornada");
var variableRange=new HorarioAgendaPsicologo{Inicio=new(8,0),Fin=new(18,0),DuracionMinutos=0};
Check(AgendaService.InicioDisponible(variableRange,clockDay,clock.Date.AddHours(17).AddMinutes(10))==new TimeOnly(17,10),"Duración no establecida admite intervalo restante sin duración fija");
Check(AgendaService.InicioDisponible(variableRange,clockDay,clock)==null,"Duración no establecida respeta cierre de jornada");
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
    Guid assignment;Guid studentProfile;await using(var db=Db())
    {
        var participation=await Bf(db).Propia(student,default);Check(participation!=null && participation.PsicologoId==psych,"Asignación automática al único profesional");assignment=participation!.AsignacionId;
        studentProfile=await db.PerfilesEstudiante.Where(p=>p.UsuarioId==student).Select(p=>p.Id).SingleAsync();
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
    var rosterHtml=await Page(pClient,"/Psicologo/Estudiantes");
    await File.WriteAllTextAsync(".visual-check/bigfive/students.html",rosterHtml);
    Check(rosterHtml.Contains("20262001")&&!rosterHtml.Contains("20262002")&&!rosterHtml.Contains("Motivo FICTICIO"),"Lista solo vinculados sin cargar sus perfiles Big Five");
    Check((await pClient.GetAsync("/Psicologo/Resultados")).StatusCode==HttpStatusCode.Redirect,"Resultados antiguos redirigen a estudiantes");
    Check((await pClient.GetAsync("/Psicologo/Atencion")).StatusCode==HttpStatusCode.Redirect,"Atención antigua redirige a estudiantes");
    Check(WebUtility.HtmlDecode(await Page(pClient,"/Psicologo/Historial")).Contains("Aún no hay atenciones con estos filtros"),"Historial general conserva su vista independiente");
    Check((await Page(pClient,"/Psicologo/Estudiantes?filtro=completado")).Contains("20262001"),"Filtro de test completado incluye al titular");
    Check(!(await Page(pClient,"/Psicologo/Estudiantes?filtro=sinresultado")).Contains("20262001"),"Filtro sin resultado excluye al completado");
    Check(!(await Page(pClient,"/Psicologo/Estudiantes?busqueda=nombreinexistente")).Contains("20262001"),"Búsqueda filtra el listado sin abandonar la cuenta propia");
    await using(var db=Db())Check(await db.Set<AccesoBigFive>().CountAsync()==0,"Listar y filtrar no leen ni auditan resultados individuales");
    var detailPath="/Psicologo/Estudiantes?estudiante="+studentProfile;
    var summaryHtml=await Page(pClient,detailPath);
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
    var alienSummary=await Page(strangerClient,"/Psicologo/Estudiantes");Check(!alienSummary.Contains("Motivo FICTICIO")&&!alienSummary.Contains("20262001"),"Otro psicólogo no recibe estudiante ni perfil");
    Check((await strangerClient.GetAsync(detailPath)).StatusCode==HttpStatusCode.NotFound,"Otro psicólogo no abre ficha por su identificador");
    Check((await owner.GetAsync(detailPath)).StatusCode==HttpStatusCode.Redirect,"Estudiante no abre ficha profesional");
    await using(var db=Db())await Reject(()=>Bf(db).Aceptar(other,new(){Acepto=true,MayorDeEdad=true,MotivoConsulta="Ejemplo ficticio"},default),"Múltiples psicólogos no asignan arbitrariamente");
    var end=await Page(owner,"/Estudiante/BigFive/cuestionario");Check(end.Contains("Comprobante")&&!end.Contains("id=\"bigfive-form\""),"Finalizado bloquea edición");
    Check(WebUtility.HtmlDecode(await Page(owner,"/Estudiante/Citas")).Contains("agenda-persistida"),"Citas habilita calendario tras finalizar");
    var date=AgendaService.Hoy.AddDays(1);var month=new DateOnly(date.Year,date.Month,1);
    var agendaPath="/Psicologo/Agenda?mes="+month.ToString("yyyy-MM-dd");
    var studentPath="/Estudiante/Citas?mes="+month.ToString("yyyy-MM-dd");
    var agendaHtml=await Page(pClient,agendaPath);
    Check(agendaHtml.Contains("Guardar horario")&&!agendaHtml.Contains("se reinicia al recargar"),"Agenda profesional persistente, sin prototipo");
    Check((await owner.GetAsync("/Psicologo/Agenda")).StatusCode==HttpStatusCode.Redirect,"Estudiante no edita agenda profesional");
    Check((await pClient.PostAsync("/Psicologo/Disponibilidad",new FormUrlEncodedContent(new Dictionary<string,string>()))).StatusCode==HttpStatusCode.BadRequest,"Agenda exige antifalsificación");
    var schedule=new Dictionary<string,string>{{"Fecha",date.ToString("yyyy-MM-dd")},{"Inicio","09:00"},{"Fin","11:00"},{"DuracionMinutos","50"},{"Modalidad","Virtual"},{"operacion","agregar"}};
    Check((await Post(pClient,"/Psicologo/Disponibilidad",schedule,agendaPath)).StatusCode==HttpStatusCode.Redirect,"Horario guardado por HTTP");
    Guid savedRange;await using(var db=Db())savedRange=await db.Set<HorarioAgendaPsicologo>().Select(h=>h.Id).SingleAsync();
    Check((await Post(pClient,"/Psicologo/Disponibilidad",new(){{"Fecha",date.ToString("yyyy-MM-dd")},{"operacion","quitar"},{"horario",savedRange.ToString()}},agendaPath)).StatusCode==HttpStatusCode.Redirect,"Retira el horario conservando su día");
    Check((await Post(pClient,"/Psicologo/Disponibilidad",schedule,agendaPath)).StatusCode==HttpStatusCode.Redirect,"Permite volver a añadir horario en el mismo día existente");
    await using(var db=Db()){
        Check(await db.Set<HorarioAgendaPsicologo>().CountAsync()==1,"Horario persiste en nueva conexión");
        var service=new AgendaService(db,Bf(db));
        await Reject(()=>service.Cambiar(psych,date,"agregar",new(){Inicio=new(10,0),Fin=new(12,0),DuracionMinutos=50},null,default),"Rechaza horarios superpuestos");
    }
    await using(var db=Db()){
        var service=new AgendaService(db,Bf(db));
        await Reject(()=>service.Cambiar(psych,date,"agregar",new(){Inicio=new(7,0),Fin=new(9,0),DuracionMinutos=50},null,default),"Rechaza fuera de jornada");
        await Reject(()=>service.Cambiar(psych,date,"agregar",new(){Inicio=new(12,0),Fin=new(12,30),DuracionMinutos=50},null,default),"Rechaza rango menor que cita");
        await Reject(()=>service.Cambiar(psych,AgendaService.Hoy.AddDays(-1),"bloquear",null,null,default),"Rechaza fecha pasada");
        await Reject(()=>service.Cambiar(student,date,"bloquear",null,null,default),"Revalida rol en base de datos");
        var foreign=await service.Leer(stranger,true,month,default);Check(foreign.Dias.Count==0,"Otro psicólogo no ve horarios ajenos");
    }
    var studentCalendar=WebUtility.HtmlDecode(await Page(owner,studentPath));
    Check(studentCalendar.Contains("09:00–11:00")&&studentCalendar.Contains("Virtual")&&studentCalendar.Contains("50 minutos")&&!studentCalendar.Contains("Guardar horario"),"Estudiante ve disponibilidad y modalidad, sin edición");
    Check((await Post(pClient,"/Psicologo/Disponibilidad",new(){{"Fecha",date.ToString("yyyy-MM-dd")},{"Inicio","12:00"},{"Fin","13:00"},{"DuracionMinutos","0"},{"Modalidad","Ambas"},{"operacion","agregar"}},agendaPath)).StatusCode==HttpStatusCode.Redirect,"Añade segundo horario al mismo día, sin duración y ambas modalidades");
    var flexibleCalendar=WebUtility.HtmlDecode(await Page(owner,studentPath));
    Check(flexibleCalendar.Contains("Sin duración establecida; puede variar")&&flexibleCalendar.Contains("Presencial o virtual"),"Estudiante ve duración variable y ambas modalidades");
    Guid flexibleId;await using(var db=Db()){
        flexibleId=await db.Set<HorarioAgendaPsicologo>().Where(h=>h.DuracionMinutos==0).Select(h=>h.Id).SingleAsync();
        Check(await db.Set<HorarioAgendaPsicologo>().CountAsync()==2,"Segundo horario se inserta sin modificar el anterior");
        await Reject(()=>new AgendaService(db,Bf(db)).Cambiar(psych,date,"agregar",new(){Inicio=new(14,0),Fin=new(16,0),DuracionMinutos=45},null,default),"No admite nueva duración de 45 minutos");
        await Reject(()=>new AgendaService(db,Bf(db)).Cambiar(psych,date,"agregar",new(){Inicio=new(14,0),Fin=new(16,0),DuracionMinutos=60},null,default),"No admite nueva duración de 60 minutos");
    }
    await File.WriteAllTextAsync(".visual-check/bigfive/calendar-student.html",await Page(owner,studentPath));
    await File.WriteAllTextAsync(".visual-check/bigfive/calendar-psych.html",await Page(pClient,agendaPath));
    await Post(pClient,"/Psicologo/Disponibilidad",new(){{"Fecha",date.ToString("yyyy-MM-dd")},{"operacion","quitar"},{"horario",flexibleId.ToString()}},agendaPath);
    await File.WriteAllTextAsync(".visual-check/bigfive/calendar-student.html",await Page(owner,studentPath));
    await File.WriteAllTextAsync(".visual-check/bigfive/calendar-psych.html",await Page(pClient,agendaPath));
    Check(!WebUtility.HtmlDecode(await Page(otherClient,studentPath)).Contains("09:00–11:00"),"Estudiante sin Big Five no ve disponibilidad");
    await Post(pClient,"/Psicologo/Disponibilidad",new(){{"Fecha",date.ToString("yyyy-MM-dd")},{"operacion","bloquear"}},agendaPath);
    var blocked=WebUtility.HtmlDecode(await Page(owner,studentPath));Check(blocked.Contains("Día ocupado")&&!blocked.Contains("09:00–11:00"),"Bloqueo se refleja y oculta horas al estudiante");
    await using(var db=Db())Check(await db.Set<HorarioAgendaPsicologo>().CountAsync()==1,"Bloqueo conserva horarios");
    await Post(pClient,"/Psicologo/Disponibilidad",new(){{"Fecha",date.ToString("yyyy-MM-dd")},{"operacion","habilitar"}},agendaPath);
    Check(WebUtility.HtmlDecode(await Page(owner,studentPath)).Contains("09:00–11:00"),"Habilitar restaura disponibilidad");
    // Dos ediciones simultáneas del mismo día se serializan incluso cuando no existía.
    async Task<bool> ConcurrentAdd(){await using var db=Db();try{await new AgendaService(db,Bf(db)).Cambiar(psych,date.AddDays(1),"agregar",new(){Inicio=new(9,0),Fin=new(11,0),DuracionMinutos=50},null,default);return true;}catch(EvaluacionOperacionException){return false;}}
    Check((await Task.WhenAll(ConcurrentAdd(),ConcurrentAdd())).Count(x=>x)==1,"Concurrencia evita duplicar/superponer horarios");
    await Post(pClient,"/Psicologo/Disponibilidad",new(){{"Fecha",date.ToString("yyyy-MM-dd")},{"operacion","limpiar"}},agendaPath);
    await using(var db=Db())Check(!await db.Set<DiaAgendaPsicologo>().AnyAsync(d=>d.Fecha==date),"Quitar configuración elimina solo el día propio");
    Check(!(await Page(owner,"/Estudiante/Evaluaciones")).Contains("DEMO"),"Panel de evaluaciones sin demostraciones");
    Check(WebUtility.HtmlDecode(await Page(owner,"/Estudiante/Resultados")).Contains("Aún no hay recomendaciones"),"Resultados sin puntuaciones automáticas");
    // Citas: únicamente datos ficticios en la base temporal.
    var bookingDate=date.AddDays(1);Guid bookingRange;
    await using(var db=Db()){
        bookingRange=await db.Set<HorarioAgendaPsicologo>().Where(h=>h.Dia.Fecha==bookingDate).Select(h=>h.Id).SingleAsync();
        await Reject(async()=>await new CitasService(db,Bf(db),TimeProvider.System).Solicitar(other,new(){HorarioId=bookingRange,Inicio=new(9,0),Modalidad="Presencial"},default),"No reserva sin Big Five finalizado");
        db.AsignacionesEstudiantePsicologo.Add(new(){PerfilEstudianteId=await db.PerfilesEstudiante.Where(p=>p.UsuarioId==other).Select(p=>p.Id).SingleAsync(),PsicologoUsuarioId=psych,FechaAsignacionUtc=DateTime.UtcNow});await db.SaveChangesAsync();
    }
    Guid otherAssignment;await using(var db=Db())otherAssignment=await Bf(db).Aceptar(other,new(){Acepto=true,MayorDeEdad=true,MotivoConsulta="Prueba ficticia de reserva"},default);
    await using(var db=Db()){
        var evaluation=new EvaluacionesService(db,Bf(db));await evaluation.Guardar(otherAssignment,other,await db.Set<AsignacionEvaluacion>().Where(a=>a.Id==otherAssignment).Select(a=>a.Revision).SingleAsync(),Ipip50.Items.ToDictionary(i=>i.Id,i=>(int?)3),default);
    }
    await using(var db=Db())await new EvaluacionesService(db,Bf(db)).Finalizar(otherAssignment,other,await db.Set<AsignacionEvaluacion>().Where(a=>a.Id==otherAssignment).Select(a=>a.Revision).SingleAsync(),true,default);
    async Task<Guid?> Race(string actor){await using var db=Db();try{return await new CitasService(db,Bf(db),TimeProvider.System).Solicitar(actor,new(){HorarioId=bookingRange,Inicio=new(9,0),Modalidad="Presencial"},default);}catch(EvaluacionOperacionException){return null;}}
    var race=await Task.WhenAll(Race(student),Race(other));Check(race.Count(id=>id!=null)==1,"Dos estudiantes simultáneos: una sola reserva");
    var raceId=race.First(id=>id!=null)!.Value;
    await using(var db=Db()){
        var appointment=await db.Set<Cita>().SingleAsync(c=>c.Id==raceId);Check(appointment.Estado==EstadoCita.Solicitada,"Solicitud reserva antes de aceptación");
        var cal=await new AgendaService(db,Bf(db)).Leer(student,false,month,default);Check(cal.Franjas.Single(f=>f.HorarioId==bookingRange&&f.Inicio==new TimeOnly(9,0)).Reservada,"Reserva aparece ocupada para todos");
        await Reject(()=>new AgendaService(db,Bf(db)).Cambiar(psych,bookingDate,"bloquear",null,null,default),"No bloquea día con cita activa");
    }
    await using(var db=Db())await Reject(()=>new AgendaService(db,Bf(db)).Cambiar(psych,bookingDate,"quitar",null,bookingRange,default),"No elimina horario con reserva activa");
    await using(var db=Db())await new CitasService(db,Bf(db),TimeProvider.System).Gestionar(psych,true,raceId,1,"rechazar","Prueba ficticia",default);
    await using(var db=Db())Check(!(await new AgendaService(db,Bf(db)).Leer(student,false,month,default)).Franjas.Single(f=>f.HorarioId==bookingRange&&f.Inicio==new TimeOnly(9,0)).Reservada,"Rechazo libera horario");
    await using(var db=Db())await Reject(async()=>await new CitasService(db,Bf(db),TimeProvider.System).Solicitar(student,new(){HorarioId=bookingRange,Inicio=new(9,10),Modalidad="Presencial"},default),"No permite hora inventada entre franjas");
    await using(var db=Db())await Reject(async()=>await new CitasService(db,Bf(db),TimeProvider.System).Solicitar(student,new(){HorarioId=bookingRange,Inicio=new(9,0),Modalidad="Ambas"},default),"Solicitud exige modalidad concreta");
    Check((await owner.PostAsync("/Citas/Solicitar",new FormUrlEncodedContent(new Dictionary<string,string>()))).StatusCode==HttpStatusCode.BadRequest,"Reserva exige antifalsificación");
    await Post(owner,"/Citas/Solicitar",new(){{"HorarioId",bookingRange.ToString()},{"Inicio","09:00"},{"Modalidad","Presencial"}},studentPath);
    Guid first;await using(var db=Db())first=await db.Set<Cita>().Where(c=>c.EstudianteId==student&&c.Estado==EstadoCita.Solicitada).Select(c=>c.Id).SingleAsync();
    var occupiedCalendar=await Page(otherClient,studentPath);
    Check(WebUtility.HtmlDecode(occupiedCalendar).Contains("Reservado / ocupado")&&!occupiedCalendar.Contains("FICTICIO Estudiante titular"),"Calendario de otro estudiante muestra reserva sin identidad del titular");
    Check(Regex.Matches(occupiedCalendar,"name=\"HorarioId\"").Count==1&&!occupiedCalendar.Contains("if(slot."),"Franja reservada sin formulario; solo el espacio libre permite solicitar");
    await File.WriteAllTextAsync(".visual-check/bigfive/appointment-reserved.html",occupiedCalendar);
    Check((await strangerClient.GetAsync("/Citas/Detalle/"+first)).StatusCode==HttpStatusCode.NotFound,"Psicólogo ajeno no accede a cita");
    Check((await owner.GetAsync("/Citas/Detalle/"+first)).StatusCode==HttpStatusCode.Redirect,"Estudiante no accede a detalle clínico");
    Check((await adminClient.GetAsync("/Citas/Detalle/"+first)).StatusCode==HttpStatusCode.Redirect,"Administrador no accede a detalle clínico");
    await using(var db=Db())await Reject(()=>new CitasService(db,Bf(db),TimeProvider.System).Gestionar(other,false,first,1,"cancelar","Ajena",default),"Otro estudiante no cancela cita por ID");
    await Post(pClient,"/Citas/Gestionar",new(){{"id",first.ToString()},{"revision","1"},{"accion","confirmar"}},agendaPath);
    await using(var db=Db())Check((await db.Set<Cita>().SingleAsync(c=>c.Id==first)).Estado==EstadoCita.Confirmada,"Profesional acepta por HTTP");
    await using(var db=Db())await Reject(()=>new CitasService(db,Bf(db),TimeProvider.System).Gestionar(student,false,first,1,"cancelar","Obsoleta",default),"Revisión obsoleta no cambia cita");
    await using(var db=Db())await Reject(()=>new CitasService(db,Bf(db),TimeProvider.System).Cerrar(psych,new(){Id=first,Revision=2,Asistio=true,ResultadoClinicoPrivado="CLINICO PRIVADO FICTICIO",ResenaEstudiante="RESEÑA PUBLICA FICTICIA",ResultadoPublicable="RESULTADO PUBLICO FICTICIO"},default),"No cierra atención futura");
    Guid busyId;await using(var db=Db())busyId=await new CitasService(db,Bf(db),TimeProvider.System).Solicitar(other,new(){HorarioId=bookingRange,Inicio=new(9,50),Modalidad="Presencial"},default);
    await using(var db=Db()){
        var ordered=await new SeguimientoEstudiantesService(db,Bf(db)).Leer(psych,null,null,1,null,default);
        Check(ordered.Estudiantes[0].Id==studentProfile&&ordered.Estudiantes[0].ProximaCitaUtc<ordered.Estudiantes[1].ProximaCitaUtc,"Listado ordena por cita más cercana antes que nombre");
    }
    await using(var db=Db())await Reject(async()=>await new CitasService(db,Bf(db),TimeProvider.System).Solicitar(student,new(){HorarioId=bookingRange,Inicio=new(9,50),Modalidad="Presencial",ReprogramarId=first,RevisionAnterior=2},default),"Reprogramar a espacio ocupado falla");
    await using(var db=Db()){
        Check((await db.Set<Cita>().SingleAsync(c=>c.Id==first)).Estado==EstadoCita.Confirmada,"Reprogramación fallida conserva reserva original");
        await new CitasService(db,Bf(db),TimeProvider.System).Gestionar(other,false,busyId,1,"cancelar","Prueba cancelación",default);
    }
    await Post(owner,"/Citas/Solicitar",new(){{"HorarioId",bookingRange.ToString()},{"Inicio","09:50"},{"Modalidad","Presencial"},{"ReprogramarId",first.ToString()},{"RevisionAnterior","2"}},studentPath+"&reprogramar="+first);
    Guid moved;await using(var db=Db()){
        var old=await db.Set<Cita>().SingleAsync(c=>c.Id==first);var newer=await db.Set<Cita>().SingleAsync(c=>c.CitaAnteriorId==first);moved=newer.Id;
        Check(old.Estado==EstadoCita.Reprogramada&&newer.Estado==EstadoCita.Solicitada,"Reprogramación conserva historial y pide nueva aceptación");
        var slots=(await new AgendaService(db,Bf(db)).Leer(student,false,month,default)).Franjas;Check(!slots.Single(f=>f.HorarioId==bookingRange&&f.Inicio==new TimeOnly(9,0)).Reservada&&slots.Single(f=>f.HorarioId==bookingRange&&f.Inicio==new TimeOnly(9,50)).Reservada,"Reprogramación libera original y reserva nuevo");
    }
    var pendingRoster=await Page(pClient,"/Psicologo/Estudiantes?filtro=solicitada");Check(pendingRoster.Contains("20262001"),"Filtro estudiantes con solicitudes real");
    Check((await Page(pClient,"/Psicologo/Estudiantes?filtro=reprogramada")).Contains("20262001"),"Filtro reprogramadas conserva movimiento anterior");
    await using(var db=Db())await new CitasService(db,Bf(db),TimeProvider.System).Gestionar(psych,true,moved,1,"confirmar",null,default);
    var after=new AppointmentClock(CitasService.Utc(bookingDate,new(12,0)));
    await using(var db=Db())await Reject(()=>new CitasService(db,Bf(db),after).Cerrar(psych,new(){Id=moved,Revision=2,Asistio=true},default),"Asistencia requiere resultado profesional y reseña");
    await using(var db=Db())await new CitasService(db,Bf(db),after).Cerrar(psych,new(){Id=moved,Revision=2,Asistio=true,ResultadoClinicoPrivado="CLINICO PRIVADO FICTICIO",ResenaEstudiante="RESEÑA PUBLICA FICTICIA",ResultadoPublicable="RESULTADO PUBLICO FICTICIO",RecomiendaProximaCita=true,IndicacionesProximaCita="SEGUIMIENTO FICTICIO"},default);
    await using(var db=Db())await Reject(()=>new CitasService(db,Bf(db),after).Cerrar(psych,new(){Id=moved,Revision=2,Asistio=true,ResultadoClinicoPrivado="Otro texto",ResenaEstudiante="Otra reseña",ResultadoPublicable="Otro resultado"},default),"No duplica ni sobrescribe atención cerrada");
    var publicResults=WebUtility.HtmlDecode(await Page(owner,"/Estudiante/Resultados"));Check(publicResults.Contains("RESULTADO PUBLICO FICTICIO")&&publicResults.Contains("RESEÑA PUBLICA FICTICIA")&&publicResults.Contains("SEGUIMIENTO FICTICIO")&&!publicResults.Contains("CLINICO PRIVADO"),"Estudiante ve solo reseña, resultado compartido y seguimiento");
    Check(!(await Page(otherClient,"/Estudiante/Resultados")).Contains("RESULTADO PUBLICO FICTICIO"),"Resultados de cita no se filtran a otro estudiante");
    var clinicalHtml=await Page(pClient,"/Citas/Detalle/"+moved);Check(clinicalHtml.Contains("CLINICO PRIVADO FICTICIO"),"Profesional propio ve resultado privado");
    await File.WriteAllTextAsync(".visual-check/bigfive/appointment-attention.html",clinicalHtml);
    var history=await Page(pClient,"/Psicologo/Historial");Check(history.Contains(moved.ToString())&&!history.Contains("CLINICO PRIVADO"),"Historial general conectado sin exponer notas en tabla");
    await File.WriteAllTextAsync(".visual-check/bigfive/appointment-history.html",history);
    Check(!(await Page(strangerClient,"/Psicologo/Historial")).Contains(moved.ToString()),"Historial profesional ajeno vacío");
    await File.WriteAllTextAsync(".visual-check/bigfive/students.html",await Page(pClient,"/Psicologo/Estudiantes"));
    await File.WriteAllTextAsync(".visual-check/bigfive/appointment-student.html",await Page(owner,studentPath));
    await File.WriteAllTextAsync(".visual-check/bigfive/appointment-results.html",await Page(owner,"/Estudiante/Resultados"));
    await using(var db=Db()){
        Check(await db.Set<AccesoAtencion>().CountAsync()==1,"Consulta clínica profesional auditada");
        Check(await db.Set<EventoCita>().CountAsync(e=>e.CitaId==moved)==3,"Solicitud, confirmación y cierre quedan auditados");
        var institutional=new BigFiveService(db,new ConfigurationBuilder().Build(),new LocalEnvironment{EnvironmentName="Production"},new AsignacionPsicologoService(db));
        Check((await new CitasService(db,institutional,TimeProvider.System).Listar(student,false,default)).Count==0,"Citas de prueba aisladas de producción");
    }
    // Una reserva con duración variable ocupa todo el intervalo ofrecido.
    Guid variableId;await using(var db=Db()){
        await new AgendaService(db,Bf(db)).Cambiar(psych,bookingDate,"agregar",new(){Inicio=new(12,0),Fin=new(14,0),DuracionMinutos=0,Modalidad="Ambas"},null,default);
        variableId=await db.Set<HorarioAgendaPsicologo>().Where(h=>h.Dia.Fecha==bookingDate&&h.DuracionMinutos==0).Select(h=>h.Id).SingleAsync();
    }
    Guid variableBooking;await using(var db=Db())variableBooking=await new CitasService(db,Bf(db),TimeProvider.System).Solicitar(student,new(){HorarioId=variableId,Inicio=new(12,0),Modalidad="Virtual"},default);
    await using(var db=Db())await Reject(async()=>await new CitasService(db,Bf(db),TimeProvider.System).Solicitar(other,new(){HorarioId=variableId,Inicio=new(13,0),Modalidad="Presencial"},default),"Duración variable evita segunda reserva dentro del intervalo");
    await using(var db=Db()){
        await new CitasService(db,Bf(db),TimeProvider.System).Gestionar(psych,true,variableBooking,1,"confirmar",null,default);
        await new CitasService(db,Bf(db),new AppointmentClock(CitasService.Utc(bookingDate,new(15,0)))).Cerrar(psych,new(){Id=variableBooking,Revision=2,Asistio=false,ResultadoClinicoPrivado="NO DEBE GUARDARSE",ResultadoPublicable="NO DEBE PUBLICARSE",ResenaEstudiante="INASISTENCIA FICTICIA"},default);
        db.ChangeTracker.Clear();var absent=await db.Set<Cita>().SingleAsync(c=>c.Id==variableBooking);Check(absent.Estado==EstadoCita.NoAsistio&&absent.ResultadoClinicoPrivado==""&&absent.ResultadoPublicable=="","Inasistencia no genera resultado clínico");
    }
    // Prueba HTTP del formulario de atención, con una cita pasada ficticia.
    Guid pastId=Guid.NewGuid();var pastDate=AgendaService.Hoy.AddDays(-1);
    await using(var db=Db()){
        db.Add(new Cita{Id=pastId,EstudianteId=student,PsicologoId=psych,HorarioOriginalId=Guid.NewGuid(),Fecha=pastDate,Inicio=new(9,0),Fin=new(9,50),InicioUtc=CitasService.Utc(pastDate,new(9,0)),FinUtc=CitasService.Utc(pastDate,new(9,50)),Estado=EstadoCita.Confirmada,PruebaLocal=true});await db.SaveChangesAsync();
    }
    var pastPath="/Citas/Detalle/"+pastId;
    await File.WriteAllTextAsync(".visual-check/bigfive/appointment-form.html",await Page(pClient,pastPath));
    var incompleteAttention=await Post(pClient,"/Citas/Cerrar",new(){{"Id",pastId.ToString()},{"Revision","1"},{"Asistio","true"},{"ResultadoClinicoPrivado","BORRADOR PRIVADO FICTICIO"}},pastPath);
    Check(incompleteAttention.StatusCode==HttpStatusCode.OK&&(await incompleteAttention.Content.ReadAsStringAsync()).Contains("BORRADOR PRIVADO FICTICIO"),"Validación conserva el texto del profesional en el formulario");
    await using(var db=Db())Check((await db.Set<Cita>().SingleAsync(c=>c.Id==pastId)).ResultadoClinicoPrivado=="","Formulario inválido no guarda atención parcial");
    await Post(pClient,"/Citas/Cerrar",new(){{"Id",pastId.ToString()},{"Revision","1"},{"Asistio","true"},{"ResultadoClinicoPrivado","HTTP PRIVADO FICTICIO"},{"ResenaEstudiante","HTTP RESEÑA FICTICIA"},{"ResultadoPublicable","HTTP PUBLICO FICTICIO"}},pastPath);
    await using(var db=Db())Check((await db.Set<Cita>().SingleAsync(c=>c.Id==pastId)).Estado==EstadoCita.Terminada,"Formulario HTTP guarda asistencia y atención profesional");
    Check(WebUtility.HtmlDecode(await Page(owner,"/Estudiante/Resultados")).Contains("HTTP PUBLICO FICTICIO")&&!(await Page(owner,"/Estudiante/Resultados")).Contains("HTTP PRIVADO FICTICIO"),"Publicación HTTP mantiene separado el resultado clínico privado");
    Guid keepForWithdrawal;await using(var db=Db())keepForWithdrawal=await new CitasService(db,Bf(db),TimeProvider.System).Solicitar(student,new(){HorarioId=bookingRange,Inicio=new(9,0),Modalidad="Presencial"},default);
    await Post(owner,"/Estudiante/BigFive/retirar",new(),"/Estudiante/BigFive");
    Check(!WebUtility.HtmlDecode(await Page(owner,studentPath)).Contains("agenda-persistida"),"Retiro de consentimiento vuelve a cerrar calendario");
    Check(!(await Page(pClient,detailPath)).Contains("Motivo FICTICIO"),"Retiro oculta el perfil dentro de la ficha profesional");
    Check((await Page(pClient,"/Psicologo/Estudiantes?filtro=sinresultado")).Contains("20262001"),"Retiro conserva vínculo y lista como sin resultado disponible");
    await using(var db=Db())
    {
        await Reject(async ()=>await new EvaluacionesService(db,Bf(db)).Guardar(assignment,student,await Revision(),new(),default),"No guarda tras retirar consentimiento");
        Check(await db.Set<AccesoBigFive>().CountAsync()==1,"Sin nuevo acceso a perfil retirado");
        Check(!await Bf(db).PuedeSolicitarCita(student,default),"Retiro suspende requisito del futuro flujo de citas");
    }
    await using(var db=Db())await Reject(async()=>await new CitasService(db,Bf(db),TimeProvider.System).Solicitar(student,new(){HorarioId=bookingRange,Inicio=new(9,50),Modalidad="Presencial"},default),"Retiro impide nuevas reservas");
    await using(var db=Db())await new CitasService(db,Bf(db),TimeProvider.System).Gestionar(student,false,keepForWithdrawal,1,"cancelar","Consentimiento retirado",default);
    await using(var db=Db())Check((await db.Set<Cita>().SingleAsync(c=>c.Id==keepForWithdrawal)).Estado==EstadoCita.Cancelada,"Retiro permite cancelar reservas previas");
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

sealed class AppointmentClock(DateTime now):TimeProvider {public override DateTimeOffset GetUtcNow()=>new DateTimeOffset(now,TimeSpan.Zero);}
