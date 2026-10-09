using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Security.Cryptography;
using System.Text;
using Zuni.Data;
using Zuni.Models.Evaluaciones;
namespace Zuni.Services;
public static class EvaluacionesDemostracion
{
    private static Guid Id(string text)=>new(SHA256.HashData(Encoding.UTF8.GetBytes(text)).AsSpan(0,16));
    public static async Task<int> Crear(ApplicationDbContext db,CancellationToken ct=default)
    {
        var cs=new NpgsqlConnectionStringBuilder(db.Database.GetConnectionString());
        if(cs.Database!="ZuniDesarrollo" || !new[]{"localhost","127.0.0.1","::1"}.Contains(cs.Host))throw new InvalidOperationException("Las demostraciones solo se crean en ZuniDesarrollo local.");
        const string evin="464d9b0e-70fe-4b0a-aa97-e74682596898";
        if(!await db.Users.AnyAsync(u=>u.Id==evin && u.Email=="ealvarezr9@miumg.edu.gt" && u.IsActive && u.PerfilEstudiante!=null && u.PerfilEstudiante.Carne=="74902015193",ct))throw new InvalidOperationException("No se encontró la cuenta Evin autorizada.");
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(74802026)",ct);
        var ejemplos=new[]{
            ("ZUNI-DEMO-01","Organización del tiempo",new[]{"Planifico mis actividades académicas de la semana.","Reservo tiempo para descansar entre actividades.","Identifico las tareas que necesito priorizar."}),
            ("ZUNI-DEMO-02","Hábitos de estudio",new[]{"Estudio en un espacio con pocas distracciones.","Reviso mis apuntes después de clase.","Divido las tareas extensas en pasos pequeños."}),
            ("ZUNI-DEMO-03","Adaptación universitaria",new[]{"Conozco los servicios de apoyo de mi universidad.","Pido ayuda cuando una actividad académica me resulta difícil.","Organizo mis horarios para participar en clase."}),
            ("ZUNI-DEMO-04","Rutinas de bienestar",new[]{"Dedico tiempo a actividades que disfruto.","Hago pausas durante las jornadas de estudio.","Mantengo contacto con personas de confianza."})
        };
        int creadas=0;
        for(int i=0;i<ejemplos.Length;i++) {
            var (codigo,titulo,textos)=ejemplos[i];var evalId=Id(codigo);var asignId=Id(codigo+"-evin");
            if(await db.Set<Evaluacion>().AnyAsync(e=>e.Codigo==codigo,ct))continue;
            var e=new Evaluacion{Id=evalId,Codigo=codigo,Titulo=titulo+" · Demostración",Descripcion="Cuestionario de ejemplo para probar el sistema. No es un instrumento clínico validado.",Instrucciones="Responde cada pregunta con un valor entre 1 (Nunca) y 5 (Siempre). Puedes guardar respuestas incompletas y continuar después. Los valores son datos de prueba y no generan diagnósticos.",EsDemostracion=true};
            for(int j=0;j<textos.Length;j++)e.Preguntas.Add(new PreguntaEvaluacion{Id=Id(codigo+"-"+j),EvaluacionId=evalId,Orden=j+1,Texto=textos[j],Obligatoria=true});
            var a=new AsignacionEvaluacion{Id=asignId,EvaluacionId=evalId,Evaluacion=e,EstudianteId=evin,Estado=(EstadoEvaluacion)i,FechaAsignacionUtc=DateTime.UtcNow.AddDays(-1),FechaInicioUtc=i>=2?DateTime.UtcNow.AddHours(-2):null,FechaFinalizacionUtc=i==3?DateTime.UtcNow.AddHours(-1):null,Comprobante=i==3?Id(codigo+"-comprobante"):null};
            if(i>=2)foreach(var p in e.Preguntas.Take(i==2?1:3))a.Respuestas.Add(new RespuestaEvaluacion{AsignacionId=asignId,EvaluacionId=evalId,PreguntaId=p.Id,Valor=p.Orden+1,ActualizadaUtc=DateTime.UtcNow.AddHours(-1)});
            if(i==3)a.Resultado=new ResultadoEvaluacion{AsignacionId=asignId,Puntuacion=9,ObservacionesPublicables="Resultado ficticio: 9 puntos como suma de respuestas de ejemplo. Sin interpretación clínica ni diagnóstico.",Publicado=false};
            db.Add(a);creadas++;
        }
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return creadas;
    }
}
