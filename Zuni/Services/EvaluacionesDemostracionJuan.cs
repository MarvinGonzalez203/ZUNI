using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Zuni.Data;
using Zuni.Models;
using Zuni.Models.Evaluaciones;
namespace Zuni.Services;
public static class EvaluacionesDemostracionJuan
{
    private static Guid Id(string text)=>new(SHA256.HashData(Encoding.UTF8.GetBytes(text)).AsSpan(0,16));
    public static async Task<int> Crear(ApplicationDbContext db,CancellationToken ct=default)
    {
        var cs=new NpgsqlConnectionStringBuilder(db.Database.GetConnectionString());
        if(cs.Database!="ZuniDesarrollo" || !new[]{"localhost","127.0.0.1","::1"}.Contains(cs.Host))throw new InvalidOperationException("Carga permitida únicamente en ZuniDesarrollo local.");
        const string juan="94093b8b-78a7-4eb4-9c54-2ee9ee8ba8c7",admin="464d9b0e-70fe-4b0a-aa97-e74682596898";
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(74802026)",ct);
        if(!await db.Users.AnyAsync(u=>u.Id==juan && u.Email=="jperezs@miumg.edu.gt" && u.IsActive && u.PerfilEstudiante!=null && u.PerfilEstudiante.Carne=="7490175760",ct))throw new InvalidOperationException("La identidad de Juan no coincide.");
        var roles=await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id where ur.UserId==juan select r.Name).ToListAsync(ct);
        if(roles.Count!=1 || roles[0]!="Estudiante")throw new InvalidOperationException("Roles de Juan inesperados.");
        if(!await db.Users.AnyAsync(u=>u.Id==admin && u.IsActive && db.UserRoles.Any(ur=>ur.UserId==u.Id && db.Roles.Any(r=>r.Id==ur.RoleId && r.Name=="Administrador")),ct))throw new InvalidOperationException("No se encontró el administrador autorizante.");
        var definitions=new[]{
            ("ZUNI-DEMO-JUAN-01","Bienestar emocional",new[]{"Identifico cómo me siento durante mi jornada universitaria.","Dedico tiempo a actividades que disfruto.","Busco apoyo de personas de confianza cuando lo necesito."}),
            ("ZUNI-DEMO-03","Adaptación universitaria",Array.Empty<string>()),
            ("ZUNI-DEMO-02","Hábitos de estudio",Array.Empty<string>()),
            ("ZUNI-DEMO-JUAN-04","Bienestar académico",new[]{"Organizo mis entregas académicas con anticipación.","Hago pausas durante mis actividades de estudio.","Conozco a quién acudir cuando necesito apoyo académico."})
        };
        int created=0;
        for(int index=0;index<definitions.Length;index++){
            var (code,title,questions)=definitions[index];
            var evaluation=await db.Set<Evaluacion>().Include(e=>e.Preguntas).SingleOrDefaultAsync(e=>e.Codigo==code,ct);
            if(evaluation is null){
                if(questions.Length==0)throw new InvalidOperationException("Falta una definición demostrativa que se debe reutilizar.");
                evaluation=new Evaluacion{Id=Id(code),Codigo=code,Titulo=title+" · DEMOSTRACIÓN",EsDemostracion=true,Descripcion="Cuestionario demostrativo con datos de prueba. No es una prueba clínica validada ni genera diagnósticos.",Instrucciones="Responde de 1 (Nunca) a 5 (Siempre). Guarda tu avance antes de salir. Todas las respuestas de ejemplo y sus resultados son ficticios, sin interpretación clínica."};
                for(int i=0;i<questions.Length;i++)evaluation.Preguntas.Add(new PreguntaEvaluacion{Id=Id(code+"-"+i),EvaluacionId=evaluation.Id,Orden=i+1,Texto=questions[i],Obligatoria=true});
                db.Add(evaluation);
            }
            if(!evaluation.EsDemostracion || evaluation.Preguntas.Count!=3)throw new InvalidOperationException("La definición existente no corresponde a una demostración de tres preguntas.");
            if(await db.Set<AsignacionEvaluacion>().AnyAsync(a=>a.EstudianteId==juan && a.EvaluacionId==evaluation.Id,ct))continue;
            var now=DateTime.UtcNow;
            var assignment=new AsignacionEvaluacion{Id=Id(code+"-juan"),Evaluacion=evaluation,EvaluacionId=evaluation.Id,EstudianteId=juan,Estado=(EstadoEvaluacion)index,FechaAsignacionUtc=now.AddDays(-1),FechaInicioUtc=index>=2?now.AddHours(-2):null,FechaFinalizacionUtc=index==3?now.AddHours(-1):null,Comprobante=index==3?Id(code+"-juan-comprobante"):null};
            if(index>=2)foreach(var question in evaluation.Preguntas.OrderBy(p=>p.Orden).Take(index==2?1:3))assignment.Respuestas.Add(new RespuestaEvaluacion{AsignacionId=assignment.Id,EvaluacionId=evaluation.Id,PreguntaId=question.Id,Valor=question.Orden+1,ActualizadaUtc=now.AddHours(-1)});
            if(index==3)assignment.Resultado=new ResultadoEvaluacion{AsignacionId=assignment.Id,Puntuacion=9,ObservacionesPublicables="Resultado ficticio de Bienestar académico: 9 puntos como suma de respuestas de DEMOSTRACIÓN. Sin interpretación clínica ni diagnóstico.",Publicado=true,PublicadoUtc=now,PublicadoPorId=admin};
            db.Add(assignment);
            db.AuditoriaUsuarios.Add(new AuditoriaUsuario{UsuarioAfectadoId=juan,AdministradorId=admin,Accion="DEMO_JUAN_ASIGNADA",DatosNuevos=JsonSerializer.Serialize(new{AsignacionId=assignment.Id,Estado=assignment.Estado,Publicado=index==3}),Motivo="Carga local de demostraciones autorizada expresamente; solo Bienestar académico se publica."});
            created++;
        }
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return created;
    }
}
