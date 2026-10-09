using Microsoft.EntityFrameworkCore;
using Zuni.Data;
using Zuni.Models;
using Zuni.Models.Evaluaciones;
using System.Text.Json;
namespace Zuni.Services;
public sealed class EvaluacionOperacionException(string mensaje) : Exception(mensaje);
public sealed class EvaluacionesService(ApplicationDbContext db, BigFiveService? bigFive = null)
{
    public IQueryable<AsignacionEvaluacion> Consulta(string estudianteId) => db.Set<AsignacionEvaluacion>().AsNoTracking().AsSplitQuery().Where(a=>a.EstudianteId==estudianteId && a.EvaluacionId!=Ipip50.EvaluacionId).Include(a=>a.Evaluacion).ThenInclude(e=>e.Preguntas).Include(a=>a.Respuestas);
    private async Task<AsignacionEvaluacion> Bloquear(Guid id,string? estudianteId,CancellationToken ct)
    {
        var a=await db.Set<AsignacionEvaluacion>().FromSqlInterpolated($"SELECT * FROM \"AsignacionesEvaluacion\" WHERE \"Id\"={id} FOR UPDATE").SingleOrDefaultAsync(ct);
        if(a is null || (estudianteId!=null && a.EstudianteId!=estudianteId)) throw new EvaluacionOperacionException("Evaluación no disponible.");
        if(a.EvaluacionId==Ipip50.EvaluacionId && (estudianteId is null || bigFive is null || !bigFive.Disponible ||
            !await bigFive.Vigentes().AnyAsync(p=>p.AsignacionId==id && p.Asignacion.EstudianteId==estudianteId,ct)))
            throw new EvaluacionOperacionException("El cuestionario requiere consentimiento y un vínculo vigente; no admite administración general.");
        await db.Entry(a).Reference(x=>x.Evaluacion).LoadAsync(ct);await db.Entry(a.Evaluacion).Collection(x=>x.Preguntas).LoadAsync(ct);
        await db.Entry(a).Collection(x=>x.Respuestas).LoadAsync(ct);await db.Entry(a).Reference(x=>x.Resultado).LoadAsync(ct);
        return a;
    }
    private static void Version(AsignacionEvaluacion a,int revision) { if(a.Revision!=revision) throw new EvaluacionOperacionException("La evaluación cambió en otra pestaña. Recarga antes de guardar."); }
    public async Task Iniciar(Guid id,string estudianteId,int revision,CancellationToken ct)
    {
        await using var tx=await db.Database.BeginTransactionAsync(ct);var a=await Bloquear(id,estudianteId,ct);
        if(a.Estado==EstadoEvaluacion.EnProceso) return;
        Version(a,revision);if(a.Estado!=EstadoEvaluacion.Pendiente) throw new EvaluacionOperacionException("Esta evaluación no está habilitada para iniciar.");
        a.Estado=EstadoEvaluacion.EnProceso;a.FechaInicioUtc=DateTime.UtcNow;a.Revision++;await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
    }
    public async Task Guardar(Guid id,string estudianteId,int revision,Dictionary<Guid,int?> respuestas,CancellationToken ct)
    {
        await using var tx=await db.Database.BeginTransactionAsync(ct);var a=await Bloquear(id,estudianteId,ct);Version(a,revision);
        if(a.Estado!=EstadoEvaluacion.EnProceso) throw new EvaluacionOperacionException("Solo se pueden guardar evaluaciones en proceso.");
        if(respuestas.Count>a.Evaluacion.Preguntas.Count || respuestas.Any(r=>!a.Evaluacion.Preguntas.Any(p=>p.Id==r.Key) || r.Value is <1 or >5)) throw new EvaluacionOperacionException("Las respuestas no son válidas para esta evaluación.");
        foreach(var r in respuestas) {
            var actual=a.Respuestas.SingleOrDefault(x=>x.PreguntaId==r.Key);
            if(r.Value is null) { if(actual!=null) db.Remove(actual);continue; }
            if(actual is null) { actual=new RespuestaEvaluacion { AsignacionId=a.Id,EvaluacionId=a.EvaluacionId,PreguntaId=r.Key };db.Add(actual); }
            actual.Valor=r.Value.Value;actual.ActualizadaUtc=DateTime.UtcNow;
        }
        a.Revision++;await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
    }
    public async Task Finalizar(Guid id,string estudianteId,int revision,bool confirmado,CancellationToken ct)
    {
        if(!confirmado) throw new EvaluacionOperacionException("Confirma el envío antes de finalizar.");
        await using var tx=await db.Database.BeginTransactionAsync(ct);var a=await Bloquear(id,estudianteId,ct);
        if(a.Estado==EstadoEvaluacion.Finalizada) return; // Repetir el envío devuelve el mismo comprobante.
        Version(a,revision);
        if(a.Estado!=EstadoEvaluacion.EnProceso || a.Evaluacion.Preguntas.Where(p=>p.Obligatoria).Any(p=>!a.Respuestas.Any(r=>r.PreguntaId==p.Id))) throw new EvaluacionOperacionException("Completa todas las preguntas obligatorias antes de finalizar.");
        a.Estado=EstadoEvaluacion.Finalizada;a.FechaFinalizacionUtc=DateTime.UtcNow;a.Comprobante=Guid.NewGuid();a.Revision++;
        if(a.Resultado is null) { a.Resultado=new ResultadoEvaluacion { AsignacionId=a.Id }; }
        a.Resultado.Publicado=false;a.Resultado.PublicadoUtc=null;a.Resultado.PublicadoPorId=null;
        a.Resultado.Puntuacion=a.Evaluacion.EsDemostracion ? a.Respuestas.Sum(r=>r.Valor) : null;
        a.Resultado.ObservacionesPublicables=a.Evaluacion.EsDemostracion ? "Resultado de demostración: suma de respuestas de ejemplo, sin interpretación clínica ni diagnóstico." : "";
        if(a.EvaluacionId==Ipip50.EvaluacionId)
        {
            var scores=Ipip50.Calcular(a.Respuestas);
            var participation=await db.Set<ParticipacionBigFive>().SingleAsync(p=>p.AsignacionId==a.Id,ct);
            participation.Apertura=scores['O'];participation.Responsabilidad=scores['C'];participation.Extraversion=scores['E'];
            participation.Amabilidad=scores['A'];participation.Neuroticismo=scores['N'];
            a.Resultado.Puntuacion=null;a.Resultado.ObservacionesPublicables="";
        }
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
    }
    public async Task Administrar(Guid id,string actor,int revision,string operacion,CancellationToken ct)
    {
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        if(!await db.Users.AnyAsync(u=>u.Id==actor && u.IsActive && db.UserRoles.Any(ur=>ur.UserId==u.Id && db.Roles.Any(r=>r.Id==ur.RoleId && r.Name=="Administrador")),ct)) throw new EvaluacionOperacionException("Acceso administrativo no disponible.");
        var a=await Bloquear(id,null,ct);Version(a,revision);
        var antes=new { a.Estado,Publicado=a.Resultado?.Publicado,a.Comprobante };
        switch(operacion) {
            case "habilitar" when a.Estado==EstadoEvaluacion.Asignada: a.Estado=EstadoEvaluacion.Pendiente;break;
            case "publicar" when a.Estado==EstadoEvaluacion.Finalizada && a.Resultado!=null: a.Resultado.Publicado=true;a.Resultado.PublicadoUtc=DateTime.UtcNow;a.Resultado.PublicadoPorId=actor;break;
            case "ocultar" when a.Resultado!=null: a.Resultado.Publicado=false;a.Resultado.PublicadoUtc=null;a.Resultado.PublicadoPorId=null;break;
            case "reabrir" when a.Estado==EstadoEvaluacion.Finalizada:
                a.Estado=EstadoEvaluacion.EnProceso;a.FechaFinalizacionUtc=null;a.Comprobante=null;
                if(a.Resultado!=null) { a.Resultado.Publicado=false;a.Resultado.PublicadoUtc=null;a.Resultado.PublicadoPorId=null; }break;
            default: throw new EvaluacionOperacionException("La operación no está permitida en el estado actual.");
        }
        a.Revision++;
        db.AuditoriaUsuarios.Add(new AuditoriaUsuario { UsuarioAfectadoId=a.EstudianteId,AdministradorId=actor,Accion="EVALUACION_"+operacion.ToUpperInvariant(),DatosAnteriores=JsonSerializer.Serialize(antes),DatosNuevos=JsonSerializer.Serialize(new{AsignacionId=a.Id,a.Estado,Publicado=a.Resultado?.Publicado}),Motivo="Administración de evaluación; no se registran respuestas." });
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
    }
}
