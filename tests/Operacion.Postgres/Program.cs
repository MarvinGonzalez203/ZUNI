using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Zuni.Seguridad;
if(args.Contains("--create-restore-fixture")||args.Contains("--verify-restored-fixture")){await RestoreFixture.Run(args.Contains("--create-restore-fixture"));return;}
var cs=Environment.GetEnvironmentVariable("DENIS_FIXTURE_CONNECTION")??throw new Exception("Use an isolated PostgreSQL fixture.");
var options=new NpgsqlConnectionStringBuilder(cs);
if(options.Host!="127.0.0.1"||!options.Database!.StartsWith("zuni_seguridad_prueba_"))throw new Exception("Only disposable local fixture databases are allowed.");
if(!(options.ApplicationName??"").StartsWith("denis-fixture-"))throw new Exception("Use a dedicated fixture ApplicationName.");
int checks=0;void Check(bool value,string text){if(!value)throw new Exception(text);checks++;}
var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{["ConnectionStrings:DefaultConnection"]=cs}).Build();
var life=new Lifetime();using(var first=new InstanciaUnica(config,life,NullLogger<InstanciaUnica>.Instance))
{
 await first.Adquirir();Check(first.Disponible,"first instance acquired");
 using(var second=new InstanciaUnica(config,new Lifetime(),NullLogger<InstanciaUnica>.Instance))
 {try{await second.Adquirir();throw new Exception("second instance accepted");}catch(InvalidOperationException){checks++;}}
 await first.StartAsync(default);
 await using var admin=new NpgsqlConnection(cs);await admin.OpenAsync();
 await using(var terminate=new NpgsqlCommand("SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname=current_database() AND application_name=@app AND pid<>pg_backend_pid()",admin))
 {terminate.Parameters.AddWithValue("app",options.ApplicationName!);await terminate.ExecuteNonQueryAsync();}
 for(var i=0;i<80&&!life.ApplicationStopping.IsCancellationRequested;i++)await Task.Delay(100);
 Check(life.ApplicationStopping.IsCancellationRequested&&!first.Disponible,"lease loss stops the instance");await first.StopAsync(default);
}
using(var restarted=new InstanciaUnica(config,new Lifetime(),NullLogger<InstanciaUnica>.Instance)){await restarted.Adquirir();Check(restarted.Disponible,"database guard released for restart");}

// Migration contract is exercised inside a transaction. Role and schema are rolled back, not installed.
var sqlFile=Environment.GetEnvironmentVariable("DENIS_SCHOOL_SCHEMA")??throw new Exception("Set the staging schema path.");
await using var db=new NpgsqlConnection(cs);await db.OpenAsync();await using var tx=await db.BeginTransactionAsync();
async Task<int> Run(string sql){await using var c=new NpgsqlCommand(sql,db,tx);return await c.ExecuteNonQueryAsync();}
async Task<object?> Scalar(string sql){await using var c=new NpgsqlCommand(sql,db,tx);return await c.ExecuteScalarAsync();}
var name="fixture_dc_"+Guid.NewGuid().ToString("N");
try
{
 await Run($"CREATE SCHEMA {name};SET LOCAL search_path TO {name};");await Run(await File.ReadAllTextAsync(sqlFile));
 await Run("INSERT INTO dc_tenants VALUES('center-a'),('center-b'); INSERT INTO dc_students(tenant_id,id,code,name) VALUES('center-a','same-id','001','Fixture A'),('center-b','same-id','001','Fixture B'); INSERT INTO dc_charges(tenant_id,id,student_id,year,amount_cents) VALUES('center-a','charge-a','same-id',2026,10000);");
 await Run($"CREATE ROLE {name} NOLOGIN NOSUPERUSER NOBYPASSRLS;GRANT USAGE ON SCHEMA {name} TO {name};GRANT SELECT,INSERT,UPDATE ON ALL TABLES IN SCHEMA {name} TO {name};GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA {name} TO {name};SET LOCAL ROLE {name};");
 Check(Convert.ToInt64(await Scalar("SELECT count(*) FROM dc_students"))==0,"tenant unset returns no students");
 await Run("SET LOCAL app.tenant_id='center-a';");Check(Convert.ToInt64(await Scalar("SELECT count(*) FROM dc_students"))==1,"RLS scopes a query without application filter");
 Check((string)(await Scalar("SELECT name FROM dc_students"))! == "Fixture A","other tenant content absent");
 Check(await Run("UPDATE dc_students SET revision=revision+1 WHERE tenant_id='center-a' AND id='same-id' AND revision=0")==1,"first compare-and-swap succeeds");
 Check(await Run("UPDATE dc_students SET revision=revision+1 WHERE tenant_id='center-a' AND id='same-id' AND revision=0")==0,"stale revision cannot overwrite");
 await Run("INSERT INTO dc_payments VALUES('center-a','pay-a','charge-a',2550,'fixture-request-0001',now());");
 await Run("INSERT INTO dc_requests VALUES('center-a','fixture-request-0001',repeat('a',64),'{\"paymentId\":\"pay-a\"}');");
 Check(await Run("INSERT INTO dc_requests VALUES('center-a','fixture-request-0001',repeat('a',64),'{}') ON CONFLICT DO NOTHING")==0,"idempotency key cannot duplicate a request");
 Check(Convert.ToInt64(await Scalar("SELECT sum(amount_cents) FROM dc_payments"))==2550,"cents retained exactly");
 await Run("SAVEPOINT negative;");
 try{await Run("INSERT INTO dc_students VALUES('center-b','intruder','002','No',0)");throw new Exception("RLS accepted another tenant");}catch(PostgresException e)when(e.SqlState=="42501"){checks++;await Run("ROLLBACK TO SAVEPOINT negative;");}
 await Run("SAVEPOINT request_payload;");
 try{await Scalar("SELECT dc_register_request('center-a','fixture-request-0001',repeat('b',64),'{}')");throw new Exception("idempotency accepted changed payload");}catch(PostgresException e)when(e.SqlState=="23514"){checks++;await Run("ROLLBACK TO SAVEPOINT request_payload;");}
 await Run("SET LOCAL app.tenant_id='center-b';SAVEPOINT negative_fk;");
 try{await Run("INSERT INTO dc_payments VALUES('center-b','bad','charge-a',100,'fixture-request-0002',now())");throw new Exception("cross-tenant FK accepted");}catch(PostgresException e)when(e.SqlState=="23503"){checks++;await Run("ROLLBACK TO SAVEPOINT negative_fk;");}
}
finally{await tx.RollbackAsync();}
Check(Convert.ToInt64(await ScalarOutside(db,$"SELECT count(*) FROM pg_namespace WHERE nspname='{name}'"))==0,"staging schema rolled back");
Console.WriteLine($"OK: {checks} PostgreSQL checks: instance guard, connection loss, RLS, tenant FKs, revisions, cents and idempotency contract. No production data.");
static async Task<object?> ScalarOutside(NpgsqlConnection db,string sql){await using var c=new NpgsqlCommand(sql,db);return await c.ExecuteScalarAsync();}
sealed class Lifetime:IHostApplicationLifetime
{
 private readonly CancellationTokenSource stopping=new();public CancellationToken ApplicationStarted=>default;public CancellationToken ApplicationStopping=>stopping.Token;public CancellationToken ApplicationStopped=>default;public void StopApplication()=>stopping.Cancel();
}
