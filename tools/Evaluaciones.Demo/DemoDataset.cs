using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Zuni.Data;
using Zuni.Models.Evaluaciones;

namespace Zuni.Demo;

public sealed record DemoManifest(int Version, string Database, string StudentId, string Batch, string OwnerToken)
{
    public string Prefix => "ZUNI-LOCAL-DEMO-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(StudentId+"\n"+Batch)))[..24];
    public string Marker => "[ZUNI-LOCAL-DEMO:v1:"+OwnerToken+"]";
    public Guid Id(string suffix) => new(SHA256.HashData(Encoding.UTF8.GetBytes(Prefix+":"+suffix)).AsSpan(0,16));
    public string Code(int index) => Prefix+"-"+(index+1);
    public static DemoManifest New(string database,string student,string batch)
    {
        if(string.IsNullOrWhiteSpace(database)||string.IsNullOrWhiteSpace(student)||!Regex.IsMatch(batch,"^[a-z0-9][a-z0-9-]{0,31}$"))
            throw new InvalidOperationException("Usa una base y un ID de estudiante, y un lote de 1–32 letras minúsculas, números o guiones.");
        return new(1,database,student,batch,Guid.NewGuid().ToString("N"));
    }
    public void Validate()
    {
        _=New(Database,StudentId,Batch);
        if(Version!=1||!Regex.IsMatch(OwnerToken??"","^[a-f0-9]{32}$"))throw new InvalidOperationException("Manifiesto inválido.");
    }
    public static async Task<DemoManifest> Load(string path)
    {
        var result=JsonSerializer.Deserialize<DemoManifest>(await File.ReadAllTextAsync(path))??throw new InvalidOperationException("Manifiesto vacío.");
        result.Validate();return result;
    }
    public async Task SaveNew(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        await using var file=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None);
        await JsonSerializer.SerializeAsync(file,this);await file.FlushAsync();
    }
}

public sealed record DemoStatus(string Code,Guid AssignmentId,string State,int Responses,bool Published);

// Reutiliza las entidades y el patrón transaccional de los creadores originales, sin identidades fijas.
public sealed class DemoDataset(ApplicationDbContext db,DemoManifest manifest)
{
    public static readonly string[] Titles={"Bienestar emocional","Adaptación universitaria","Hábitos de estudio","Bienestar académico publicado","Bienestar académico sin publicar"};
    private Guid EvalId(int i)=>manifest.Id("evaluation-"+i);
    private Guid AssignmentId(int i)=>manifest.Id("assignment-"+i);
    private Guid QuestionId(int i,int j)=>manifest.Id($"question-{i}-{j}");
    private Guid[] EvalIds=>Enumerable.Range(0,5).Select(EvalId).ToArray();
    private Guid[] AssignmentIds=>Enumerable.Range(0,5).Select(AssignmentId).ToArray();
    public void ValidateDestination()
    {
        manifest.Validate();var cs=new NpgsqlConnectionStringBuilder(db.Database.GetConnectionString());
        if(!new[]{"localhost","127.0.0.1","::1"}.Contains(cs.Host)||cs.Database!=manifest.Database)
            throw new InvalidOperationException("Solo se permite PostgreSQL local y el nombre de base debe coincidir exactamente con --database/manifiesto.");
    }
    public async Task ValidateSchema()
    {
        ValidateDestination();var migrations=(await db.Database.GetAppliedMigrationsAsync()).ToArray();
        if(!migrations.Contains("20261009043711_AddEvaluaciones")||(await db.Database.GetPendingMigrationsAsync()).Any())
            throw new InvalidOperationException("Faltan migraciones. Revísalas y aplícalas por separado; esta utilidad nunca actualiza el esquema.");
    }
    public async Task ValidateAccounts(string publisherId)
    {
        if(!await HasRole(manifest.StudentId,"Estudiante"))throw new InvalidOperationException("El estudiante debe existir, estar activo y tener el rol Estudiante.");
        if(!await HasRole(publisherId,"Administrador"))throw new InvalidOperationException("El publicador debe existir, estar activo y tener el rol Administrador.");
    }
    private Task<bool> HasRole(string id,string role)=>db.Users.AnyAsync(u=>u.Id==id&&u.IsActive&&db.UserRoles.Any(ur=>ur.UserId==id&&db.Roles.Any(r=>r.Id==ur.RoleId&&r.Name==role)));
    private async Task Lock()
    {
        var key=BitConverter.ToInt64(SHA256.HashData(Encoding.UTF8.GetBytes(manifest.Prefix)),0);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({key})");
        foreach(var id in EvalIds)await db.Database.ExecuteSqlInterpolatedAsync($"SELECT \"Id\" FROM \"Evaluaciones\" WHERE \"Id\"={id} FOR UPDATE");
        foreach(var id in AssignmentIds)await db.Database.ExecuteSqlInterpolatedAsync($"SELECT \"Id\" FROM \"AsignacionesEvaluacion\" WHERE \"Id\"={id} FOR UPDATE");
    }
    private async Task<List<AsignacionEvaluacion>> ReadOwned()
    {
        var ids=EvalIds;var assignmentIds=AssignmentIds;var codes=Enumerable.Range(0,5).Select(manifest.Code).ToArray();
        var evaluations=await db.Set<Evaluacion>().Include(e=>e.Preguntas).Where(e=>ids.Contains(e.Id)||codes.Contains(e.Codigo)).ToListAsync();
        var assignments=await db.Set<AsignacionEvaluacion>().Include(a=>a.Respuestas).Include(a=>a.Resultado).Where(a=>ids.Contains(a.EvaluacionId)||assignmentIds.Contains(a.Id)).ToListAsync();
        if(evaluations.Count==0&&assignments.Count==0)return assignments;
        if(evaluations.Count!=5||assignments.Count!=5)throw new InvalidOperationException("Colisión, lote incompleto o asignaciones adicionales: no se modificará ningún registro.");
        for(int i=0;i<5;i++)
        {
            var e=evaluations.SingleOrDefault(e=>e.Id==EvalId(i));var a=assignments.SingleOrDefault(a=>a.Id==AssignmentId(i));
            var expectedQuestions=Enumerable.Range(0,3).Select(j=>QuestionId(i,j)).Order().ToArray();
            if(e is null||!e.EsDemostracion||e.Codigo!=manifest.Code(i)||!e.Descripcion.StartsWith(manifest.Marker+" ",StringComparison.Ordinal)||a is null||a.EvaluacionId!=e.Id||a.EstudianteId!=manifest.StudentId||!e.Preguntas.Select(p=>p.Id).Order().SequenceEqual(expectedQuestions))
                throw new InvalidOperationException("La propiedad del lote no coincide con el manifiesto: se conserva todo sin cambios.");
        }
        return assignments;
    }
    public async Task<IReadOnlyList<DemoStatus>> List()
    {
        await ValidateSchema();var assignments=await ReadOwned();
        return Enumerable.Range(0,5).Where(i=>assignments.Any(a=>a.Id==AssignmentId(i))).Select(i=>{
            var a=assignments.Single(a=>a.Id==AssignmentId(i));return new DemoStatus(manifest.Code(i),a.Id,a.Estado.ToString(),a.Respuestas.Count,a.Resultado?.Publicado==true);
        }).ToArray();
    }
    public async Task<int> Create(string publisherId)
    {
        await ValidateSchema();await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        await Lock();await ValidateAccounts(publisherId);var existing=await ReadOwned();
        if(existing.Count>0){await tx.CommitAsync();return 0;}
        var now=DateTime.UtcNow;
        for(int i=0;i<5;i++)
        {
            var e=new Evaluacion{Id=EvalId(i),Codigo=manifest.Code(i),Titulo=Titles[i]+" · DEMOSTRACIÓN LOCAL ["+manifest.Prefix[^8..]+"]",EsDemostracion=true,
                Descripcion=manifest.Marker+" Datos ficticios de prueba; sin interpretación clínica ni diagnóstico.",
                Instrucciones="Responde de 1 (Nunca) a 5 (Siempre). Guarda manualmente antes de salir. Estas preguntas son solo ejemplos ficticios."};
            var texts=new[]{"Organizo mis actividades de ejemplo con anticipación.","Hago pausas durante una jornada de estudio de ejemplo.","Conozco opciones de apoyo académico en una situación ficticia."};
            for(int j=0;j<3;j++)e.Preguntas.Add(new PreguntaEvaluacion{Id=QuestionId(i,j),EvaluacionId=e.Id,Orden=j+1,Texto=texts[j],Obligatoria=true});
            var a=new AsignacionEvaluacion{Id=AssignmentId(i),Evaluacion=e,EvaluacionId=e.Id,EstudianteId=manifest.StudentId,Estado=i<3?(EstadoEvaluacion)i:EstadoEvaluacion.Finalizada,
                FechaAsignacionUtc=now.AddDays(-1),FechaInicioUtc=i>=2?now.AddHours(-2):null,FechaFinalizacionUtc=i>=3?now.AddHours(-1):null,Comprobante=i>=3?manifest.Id("receipt-"+i):null};
            if(i>=2)foreach(var p in e.Preguntas.OrderBy(p=>p.Orden).Take(i==2?1:3))a.Respuestas.Add(new RespuestaEvaluacion{AsignacionId=a.Id,EvaluacionId=e.Id,PreguntaId=p.Id,Valor=p.Orden+1,ActualizadaUtc=now.AddHours(-1)});
            if(i>=3)a.Resultado=new ResultadoEvaluacion{AsignacionId=a.Id,Puntuacion=9,ObservacionesPublicables="Resultado ficticio de DEMOSTRACIÓN: suma de ejemplo de 9 puntos, sin diagnóstico.",Publicado=i==3,PublicadoUtc=i==3?now:null,PublicadoPorId=i==3?publisherId:null};
            db.Add(a);
        }
        await db.SaveChangesAsync();await tx.CommitAsync();return 5;
    }
    public async Task<int> Remove()
    {
        await ValidateSchema();await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        await Lock();var assignments=await ReadOwned();
        if(assignments.Count==0){await tx.CommitAsync();return 0;}
        // Las validaciones anteriores rechazan toda referencia externa o preguntas agregadas.
        db.RemoveRange(assignments.SelectMany(a=>a.Respuestas));db.RemoveRange(assignments.Where(a=>a.Resultado!=null).Select(a=>a.Resultado!));await db.SaveChangesAsync();
        db.RemoveRange(assignments);await db.SaveChangesAsync();
        var evaluations=await db.Set<Evaluacion>().Include(e=>e.Preguntas).Where(e=>EvalIds.Contains(e.Id)).ToListAsync();
        db.RemoveRange(evaluations.SelectMany(e=>e.Preguntas));await db.SaveChangesAsync();db.RemoveRange(evaluations);await db.SaveChangesAsync();
        await tx.CommitAsync();return assignments.Count;
    }
}
