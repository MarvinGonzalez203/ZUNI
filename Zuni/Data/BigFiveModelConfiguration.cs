using Microsoft.EntityFrameworkCore;
using Zuni.Models.Evaluaciones;
namespace Zuni.Data;
public static class BigFiveModelConfiguration
{
    public static void ConfigurarBigFive(this ModelBuilder b)
    {
        b.Entity<ParticipacionBigFive>(e=>
        {
            e.ToTable("ParticipacionesBigFive", t=>t.HasCheckConstraint("CK_BigFive_Puntuaciones",
                "(\"Apertura\" IS NULL AND \"Responsabilidad\" IS NULL AND \"Extraversion\" IS NULL AND \"Amabilidad\" IS NULL AND \"Neuroticismo\" IS NULL) OR (\"Apertura\" IS NOT NULL AND \"Responsabilidad\" IS NOT NULL AND \"Extraversion\" IS NOT NULL AND \"Amabilidad\" IS NOT NULL AND \"Neuroticismo\" IS NOT NULL AND \"Apertura\" BETWEEN 1 AND 5 AND \"Responsabilidad\" BETWEEN 1 AND 5 AND \"Extraversion\" BETWEEN 1 AND 5 AND \"Amabilidad\" BETWEEN 1 AND 5 AND \"Neuroticismo\" BETWEEN 1 AND 5)"));
            e.HasKey(x=>x.AsignacionId);
            e.HasOne(x=>x.Asignacion).WithOne().HasForeignKey<ParticipacionBigFive>(x=>x.AsignacionId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x=>x.Psicologo).WithMany().HasForeignKey(x=>x.PsicologoId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x=>x.VersionInstrumento).HasMaxLength(100);
            e.Property(x=>x.VersionConsentimiento).HasMaxLength(100);
            e.Property(x=>x.TextoConsentimiento).HasMaxLength(8000);
            e.Property(x=>x.MotivoConsulta).HasMaxLength(1500);
            e.Property(x=>x.Referencia).HasMaxLength(30);
            foreach(var name in new[]{"Apertura","Responsabilidad","Extraversion","Amabilidad","Neuroticismo"}) e.Property<decimal?>(name).HasPrecision(3,2);
        });
        b.Entity<AccesoBigFive>(e=>
        {
            e.ToTable("AccesosBigFive");e.HasKey(x=>x.Id);
            e.HasOne(x=>x.Participacion).WithMany().HasForeignKey(x=>x.AsignacionId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x=>x.Psicologo).WithMany().HasForeignKey(x=>x.PsicologoId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x=>new{x.AsignacionId,x.FechaUtc});
        });
    }
}
