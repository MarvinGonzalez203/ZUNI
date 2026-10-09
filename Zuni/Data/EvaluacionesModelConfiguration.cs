using Microsoft.EntityFrameworkCore;
using Zuni.Models.Evaluaciones;
namespace Zuni.Data;
public static class EvaluacionesModelConfiguration
{
    public static void ConfigurarEvaluaciones(this ModelBuilder b)
    {
        b.Entity<Evaluacion>(e=> { e.ToTable("Evaluaciones");e.HasKey(x=>x.Id);e.Property(x=>x.Codigo).HasMaxLength(100);e.HasIndex(x=>x.Codigo).IsUnique();e.Property(x=>x.Titulo).HasMaxLength(200);e.Property(x=>x.Descripcion).HasMaxLength(2000);e.Property(x=>x.Instrucciones).HasMaxLength(3000); });
        b.Entity<PreguntaEvaluacion>(e=> { e.ToTable("PreguntasEvaluacion",t=>t.HasCheckConstraint("CK_Pregunta_Orden","\"Orden\" > 0"));e.HasKey(x=>x.Id);e.HasAlternateKey(x=>new{x.Id,x.EvaluacionId});e.Property(x=>x.Texto).HasMaxLength(1000);e.HasIndex(x=>new{x.EvaluacionId,x.Orden}).IsUnique();e.HasOne(x=>x.Evaluacion).WithMany(x=>x.Preguntas).HasForeignKey(x=>x.EvaluacionId).OnDelete(DeleteBehavior.Restrict); });
        b.Entity<AsignacionEvaluacion>(e=> {
            e.ToTable("AsignacionesEvaluacion",t=> { t.HasCheckConstraint("CK_Asignacion_Estado","\"Estado\" BETWEEN 0 AND 3");t.HasCheckConstraint("CK_Asignacion_Finalizacion","(\"Estado\" = 3 AND \"FechaFinalizacionUtc\" IS NOT NULL AND \"Comprobante\" IS NOT NULL AND \"FechaInicioUtc\" IS NOT NULL) OR (\"Estado\" <> 3 AND \"FechaFinalizacionUtc\" IS NULL AND \"Comprobante\" IS NULL)");t.HasCheckConstraint("CK_Asignacion_Inicio","\"Estado\" <> 2 OR \"FechaInicioUtc\" IS NOT NULL"); });
            e.HasKey(x=>x.Id);e.HasAlternateKey(x=>new{x.Id,x.EvaluacionId});e.HasIndex(x=>new{x.EstudianteId,x.EvaluacionId}).IsUnique();e.HasIndex(x=>x.Comprobante).IsUnique();e.Property(x=>x.Revision).IsConcurrencyToken();e.HasOne(x=>x.Evaluacion).WithMany().HasForeignKey(x=>x.EvaluacionId).OnDelete(DeleteBehavior.Restrict);e.HasOne(x=>x.Estudiante).WithMany().HasForeignKey(x=>x.EstudianteId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<RespuestaEvaluacion>(e=> { e.ToTable("RespuestasEvaluacion",t=>t.HasCheckConstraint("CK_Respuesta_Valor","\"Valor\" BETWEEN 1 AND 5"));e.HasKey(x=>new{x.AsignacionId,x.PreguntaId});e.HasOne(x=>x.Asignacion).WithMany(x=>x.Respuestas).HasForeignKey(x=>new{x.AsignacionId,x.EvaluacionId}).HasPrincipalKey(x=>new{x.Id,x.EvaluacionId}).OnDelete(DeleteBehavior.Restrict);e.HasOne(x=>x.Pregunta).WithMany().HasForeignKey(x=>new{x.PreguntaId,x.EvaluacionId}).HasPrincipalKey(x=>new{x.Id,x.EvaluacionId}).OnDelete(DeleteBehavior.Restrict); });
        b.Entity<ResultadoEvaluacion>(e=> { e.ToTable("ResultadosEvaluacion",t=>t.HasCheckConstraint("CK_Resultado_Publicacion","NOT \"Publicado\" OR (\"PublicadoUtc\" IS NOT NULL AND \"PublicadoPorId\" IS NOT NULL)"));e.HasKey(x=>x.AsignacionId);e.Property(x=>x.Puntuacion).HasPrecision(10,2);e.Property(x=>x.ObservacionesPublicables).HasMaxLength(2000);e.HasOne(x=>x.Asignacion).WithOne(x=>x.Resultado).HasForeignKey<ResultadoEvaluacion>(x=>x.AsignacionId).OnDelete(DeleteBehavior.Restrict);e.HasOne(x=>x.PublicadoPor).WithMany().HasForeignKey(x=>x.PublicadoPorId).OnDelete(DeleteBehavior.Restrict); });
    }
}
