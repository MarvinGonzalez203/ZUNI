using Microsoft.EntityFrameworkCore;
using Zuni.Models;
namespace Zuni.Data;
public static class CitasModelConfiguration
{
    public static void ConfigurarCitas(this ModelBuilder builder)
    {
        builder.Entity<Cita>(e=>{
            e.ToTable("Citas",t=>{
                t.HasCheckConstraint("CK_Citas_Horas","\"Fin\" > \"Inicio\" AND \"FinUtc\" > \"InicioUtc\"");
                t.HasCheckConstraint("CK_Citas_Estado","\"Estado\" BETWEEN 0 AND 6 AND \"Revision\" > 0");
                t.HasCheckConstraint("CK_Citas_Modalidad","\"Modalidad\" IN ('Presencial','Virtual')");
                t.HasCheckConstraint("CK_Citas_Cierre","(\"Estado\"=2 AND \"Asistio\" IS TRUE AND \"AtencionUtc\" IS NOT NULL) OR (\"Estado\"=5 AND \"Asistio\" IS FALSE AND \"AtencionUtc\" IS NOT NULL) OR (\"Estado\" NOT IN (2,5) AND \"Asistio\" IS NULL AND \"AtencionUtc\" IS NULL)");
            });
            e.HasKey(x=>x.Id);e.Property(x=>x.Revision).IsConcurrencyToken();e.Property(x=>x.Modalidad).HasMaxLength(10);
            e.Property(x=>x.ResultadoClinicoPrivado).HasMaxLength(4000);e.Property(x=>x.ResenaEstudiante).HasMaxLength(1500);
            e.Property(x=>x.ResultadoPublicable).HasMaxLength(1500);e.Property(x=>x.IndicacionesProximaCita).HasMaxLength(1000);
            e.HasOne(x=>x.Estudiante).WithMany().HasForeignKey(x=>x.EstudianteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x=>x.Psicologo).WithMany().HasForeignKey(x=>x.PsicologoId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x=>x.CitaAnterior).WithMany().HasForeignKey(x=>x.CitaAnteriorId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x=>new{x.PsicologoId,x.PruebaLocal,x.Fecha,x.Inicio}).IsUnique().HasFilter("\"Estado\" IN (0,1)");
            e.HasIndex(x=>new{x.EstudianteId,x.InicioUtc});e.HasIndex(x=>new{x.PsicologoId,x.InicioUtc});
        });
        builder.Entity<EventoCita>(e=>{
            e.ToTable("EventosCita");e.HasKey(x=>x.Id);e.Property(x=>x.Accion).HasMaxLength(30);e.Property(x=>x.Motivo).HasMaxLength(300);
            e.HasOne(x=>x.Cita).WithMany().HasForeignKey(x=>x.CitaId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x=>x.Actor).WithMany().HasForeignKey(x=>x.ActorId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x=>new{x.CitaId,x.FechaUtc});
        });
        builder.Entity<AccesoAtencion>(e=>{
            e.ToTable("AccesosAtencion");e.HasKey(x=>x.Id);
            e.HasOne(x=>x.Cita).WithMany().HasForeignKey(x=>x.CitaId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x=>x.Psicologo).WithMany().HasForeignKey(x=>x.PsicologoId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x=>new{x.CitaId,x.FechaUtc});
        });
    }
}
