// Crea y elimina UNA base aleatoria nueva. Nunca migra ni escribe en la base configurada de ZUNI.
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Npgsql;
using Zuni.Data;
using Zuni.Demo;
using Zuni.Models;
using Zuni.Models.Evaluaciones;
using Zuni.Services;

var config=new ConfigurationBuilder().AddUserSecrets("df74818a-cd0f-4ce3-9d60-29cf450de409").AddEnvironmentVariables().Build();
var cs=new NpgsqlConnectionStringBuilder(config.GetConnectionString("DefaultConnection"));
if(!new[]{"localhost","127.0.0.1","::1"}.Contains(cs.Host))throw new InvalidOperationException("Las pruebas requieren PostgreSQL local.");
var name="ZuniDemoUtilityTest_"+Guid.NewGuid().ToString("N");
var administration=new NpgsqlConnectionStringBuilder(cs.ConnectionString){Database="postgres"};
await using var adminConnection=new NpgsqlConnection(administration.ConnectionString);await adminConnection.OpenAsync();
var created=false;int checks=0;
void Check(bool ok,string label){if(!ok)throw new InvalidOperationException("FALLO: "+label);checks++;Console.WriteLine("OK: "+label);}
async Task Rejected(Func<Task> action,string label){try{await action();}catch(Exception e) when(e is InvalidOperationException or EvaluacionOperacionException){Check(true,label);return;}throw new InvalidOperationException("FALLO: "+label);}
try
{
    await using(var command=new NpgsqlCommand("CREATE DATABASE \""+name+"\"",adminConnection))await command.ExecuteNonQueryAsync();created=true;
    var testCs=new NpgsqlConnectionStringBuilder(cs.ConnectionString){Database=name};
    ApplicationDbContext Db()=>new(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(testCs.ConnectionString).Options);
    var student=Guid.NewGuid().ToString();var publisher=Guid.NewGuid().ToString();var other=Guid.NewGuid().ToString();
    await using(var db=Db())
    {
        await db.Database.MigrateAsync();
        var studentRole=new IdentityRole("Estudiante"){Id=Guid.NewGuid().ToString(),NormalizedName="ESTUDIANTE"};
        var adminRole=new IdentityRole("Administrador"){Id=Guid.NewGuid().ToString(),NormalizedName="ADMINISTRADOR"};db.Roles.AddRange(studentRole,adminRole);
        db.Users.AddRange(new ApplicationUser{Id=student,FullName="Estudiante ficticio",Email="demo-student@example.invalid",SecurityStamp=Guid.NewGuid().ToString()},new ApplicationUser{Id=publisher,FullName="Administrador ficticio",Email="demo-admin@example.invalid",SecurityStamp=Guid.NewGuid().ToString()},new ApplicationUser{Id=other,FullName="Control ficticio",Email="demo-control@example.invalid",SecurityStamp=Guid.NewGuid().ToString()});
        db.UserRoles.AddRange(new IdentityUserRole<string>{UserId=student,RoleId=studentRole.Id},new IdentityUserRole<string>{UserId=other,RoleId=studentRole.Id},new IdentityUserRole<string>{UserId=publisher,RoleId=adminRole.Id});
        var control=new Evaluacion{Id=Guid.NewGuid(),Codigo="CONTROL-FICTICIO",Titulo="Control ajeno a la utilidad",Descripcion="Dato ficticio de control"};
        db.Add(new AsignacionEvaluacion{Id=Guid.NewGuid(),Evaluacion=control,EstudianteId=other,Estado=EstadoEvaluacion.Asignada,FechaAsignacionUtc=DateTime.UtcNow});await db.SaveChangesAsync();
    }
    await using var connection=new NpgsqlConnection(testCs.ConnectionString);await connection.OpenAsync();
    async Task<string> Scalar(string sql){await using var command=new NpgsqlCommand(sql,connection);return (string)(await command.ExecuteScalarAsync())!;}
    var tables=new List<string>();await using(var command=new NpgsqlCommand("SELECT tablename FROM pg_tables WHERE schemaname='public' ORDER BY tablename",connection))await using(var reader=await command.ExecuteReaderAsync())while(await reader.ReadAsync())tables.Add(reader.GetString(0));
    async Task<Dictionary<string,string>> Snapshot(){var output=new Dictionary<string,string>();foreach(var table in tables)output[table]=await Scalar("SELECT md5(coalesce(string_agg(to_jsonb(t)::text,'|' ORDER BY to_jsonb(t)::text),'')) FROM \""+table.Replace("\"","\"\"")+"\" t");return output;}
    var before=await Snapshot();var manifest=DemoManifest.New(name,student,"prueba-aislada");
    var manifestPath=Path.Combine(".visual-check","demo-utility-tests",name+".json");await manifest.SaveNew(manifestPath);
    Check((await DemoManifest.Load(manifestPath)).OwnerToken==manifest.OwnerToken,"Manifiesto conserva propiedad sin conexión ni credenciales");
    var protectedManifest=false;try{await manifest.SaveNew(manifestPath);}catch(IOException){protectedManifest=true;}
    Check(protectedManifest,"No sobrescribe un manifiesto existente");
    await using(var db=Db())Check(await new DemoDataset(db,manifest).Create(publisher)==5,"Crea cinco asignaciones en PostgreSQL aislado");
    await using(var db=Db())Check(await new DemoDataset(db,manifest).Create(publisher)==0,"Repetición no duplica ni reinicia datos");
    IReadOnlyList<DemoStatus> rows;await using(var db=Db())rows=await new DemoDataset(db,manifest).List();
    Check(rows.Count==5&&rows.Select(r=>r.State).Distinct().Count()==4,"Cuatro estados y dos finalizadas");
    Check(rows[2].Responses==1,"En proceso recupera 1/3 (33 %) guardado");
    Check(rows[3].Published&&!rows[4].Published,"Un resultado publicado y otro sin publicar");
    var pending=rows[1].AssignmentId;
    await using(var db=Db()){await new EvaluacionesService(db).Iniciar(pending,student,0,default);}
    Guid[] questions;await using(var db=Db()){var a=await db.Set<AsignacionEvaluacion>().Include(a=>a.Evaluacion).ThenInclude(e=>e.Preguntas).SingleAsync(a=>a.Id==pending);questions=a.Evaluacion.Preguntas.OrderBy(p=>p.Orden).Select(p=>p.Id).ToArray();await new EvaluacionesService(db).Guardar(pending,student,a.Revision,new(){{questions[0],4}},default);}
    await using(var db=Db())
    {
        var a=await db.Set<AsignacionEvaluacion>().Include(a=>a.Respuestas).SingleAsync(a=>a.Id==pending);
        Check(a.Estado==EstadoEvaluacion.EnProceso&&a.Respuestas.Single().Valor==4,"Nueva conexión recupera avance real después de iniciar y guardar");
        await Rejected(()=>new EvaluacionesService(db).Finalizar(pending,student,a.Revision,true,default),"No finaliza incompleta");
    }
    await using(var db=Db()){var revision=await db.Set<AsignacionEvaluacion>().Where(a=>a.Id==pending).Select(a=>a.Revision).SingleAsync();await new EvaluacionesService(db).Guardar(pending,student,revision,questions.ToDictionary(q=>q,q=>(int?)4),default);}
    await using(var db=Db()){var revision=await db.Set<AsignacionEvaluacion>().Where(a=>a.Id==pending).Select(a=>a.Revision).SingleAsync();await Rejected(()=>new EvaluacionesService(db).Finalizar(pending,student,revision,false,default),"Sin confirmar conserva estado y respuestas");await new EvaluacionesService(db).Finalizar(pending,student,revision,true,default);}
    Guid receipt;
    await using(var db=Db()){var a=await db.Set<AsignacionEvaluacion>().Include(a=>a.Resultado).SingleAsync(a=>a.Id==pending);receipt=a.Comprobante!.Value;Check(a.Estado==EstadoEvaluacion.Finalizada&&a.FechaFinalizacionUtc!=null&&!a.Resultado!.Publicado,"Finalización registra fecha y comprobante; no publica");await new EvaluacionesService(db).Finalizar(pending,student,0,true,default);}
    await using(var db=Db()){Check(await db.Set<AsignacionEvaluacion>().Where(a=>a.Id==pending).Select(a=>a.Comprobante).SingleAsync()==receipt,"Doble envío conserva comprobante");Check(await new DemoDataset(db,manifest).Create(publisher)==0,"Recrear tras avanzar no reinicia las evaluaciones");}
    // Publicación condicionada en la misma proyección usada por Resultados.
    await using(var db=Db()){var results=await db.Set<AsignacionEvaluacion>().Where(a=>a.EstudianteId==student&&a.Estado==EstadoEvaluacion.Finalizada).Select(a=>new{a.Id,Value=a.Resultado!=null&&a.Resultado.Publicado?a.Resultado.Puntuacion:null}).ToListAsync();Check(results.Count(r=>r.Value.HasValue)==1&&results.Single(r=>r.Value.HasValue).Value==9,"PostgreSQL entrega puntuación únicamente autorizada");}
    // Un manifiesto distinto o una referencia de otro estudiante debe impedir toda limpieza.
    await using(var db=Db())await Rejected(()=>new DemoDataset(db,manifest with{OwnerToken=Guid.NewGuid().ToString("N")}).Remove(),"Propiedad incorrecta no borra datos");
    var extra=Guid.NewGuid();await using(var db=Db()){db.Add(new AsignacionEvaluacion{Id=extra,EvaluacionId=manifest.Id("evaluation-0"),EstudianteId=other,Estado=EstadoEvaluacion.Asignada,FechaAsignacionUtc=DateTime.UtcNow});await db.SaveChangesAsync();}
    await using(var db=Db())await Rejected(()=>new DemoDataset(db,manifest).Remove(),"Referencia externa bloquea limpieza completa");
    await using(var db=Db()){Check(await db.Set<AsignacionEvaluacion>().AnyAsync(a=>a.Id==extra),"Referencia externa preservada");db.Remove(await db.Set<AsignacionEvaluacion>().SingleAsync(a=>a.Id==extra));await db.SaveChangesAsync();}
    await using(var db=Db())Check(await new DemoDataset(db,manifest).Remove()==5,"Retira solo las cinco demostraciones propias");
    await using(var db=Db())Check(await new DemoDataset(db,manifest).Remove()==0,"Retirada repetida sin efectos");
    var after=await Snapshot();foreach(var table in tables)Check(before[table]==after[table],"Contenido original intacto: "+table);
    await using(var db=Db()){var missing=DemoManifest.New(name,Guid.NewGuid().ToString(),"sin-estudiante");await Rejected(()=>new DemoDataset(db,missing).Create(publisher),"Estudiante inexistente no inserta datos");}
    await using(var db=Db()){var wrong=manifest with{Database="OtraBase"};await Rejected(()=>new DemoDataset(db,wrong).List(),"Destino distinto rechazado");}
    await connection.CloseAsync();Console.WriteLine($"APROBADAS: {checks} comprobaciones. Solo base temporal {name}.");
}
finally
{
    if(created){NpgsqlConnection.ClearAllPools();await using var command=new NpgsqlCommand("DROP DATABASE \""+name+"\"",adminConnection);await command.ExecuteNonQueryAsync();Console.WriteLine("Base temporal eliminada. No se ejecutó la utilidad en ZuniDesarrollo ni en una base compartida.");}
}
