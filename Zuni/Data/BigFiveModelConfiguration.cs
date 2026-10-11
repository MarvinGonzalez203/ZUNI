using Microsoft.EntityFrameworkCore;
using Zuni.Models;
using Zuni.Models.Atencion;
using Zuni.Models.BigFive;
using Zuni.Services.BigFive;
namespace Zuni.Data;

public static class BigFiveModelConfiguration
{
    public static void ConfigurarBigFive(this ModelBuilder builder)
    {
        builder.Entity<ParticipacionBigFive>(e =>
        {
            var scores = new[] { "Apertura", "Responsabilidad", "Extraversion", "Amabilidad", "Neuroticismo" };
            var vacias = string.Join(" AND ", scores.Select(n => $"\"{n}\" IS NULL"));
            var completas = string.Join(" AND ", scores.Select(n => $"\"{n}\" IS NOT NULL AND \"{n}\" BETWEEN 1 AND 5"));
            e.ToTable("ParticipacionesBigFive", t =>
            {
                t.HasCheckConstraint("CK_BigFive_Finalizacion",
                    $"(\"FechaFinalizacionUtc\" IS NULL AND {vacias}) OR (\"FechaFinalizacionUtc\" IS NOT NULL AND {completas})");
                t.HasCheckConstraint("CK_BigFive_Fechas",
                    "(\"FechaFinalizacionUtc\" IS NULL OR \"FechaFinalizacionUtc\" >= \"FechaConsentimientoUtc\") AND " +
                    "(\"FechaRetiroConsentimientoUtc\" IS NULL OR (\"FechaRetiroConsentimientoUtc\" >= \"FechaConsentimientoUtc\" AND " +
                    "(\"FechaFinalizacionUtc\" IS NULL OR \"FechaRetiroConsentimientoUtc\" >= \"FechaFinalizacionUtc\")))");
                t.HasCheckConstraint("CK_BigFive_Consentimiento",
                    "\"VersionInstrumento\" = 'IPIP50-ES-ZUNI-1' AND \"VersionConsentimiento\" = 'ZUNI-BIGFIVE-1' AND length(btrim(\"TextoConsentimiento\")) > 0 AND \"Revision\" >= 0");
            });
            e.HasKey(p => p.Id);
            e.Property(p => p.VersionInstrumento).HasMaxLength(100).IsRequired();
            e.Property(p => p.VersionConsentimiento).HasMaxLength(100).IsRequired();
            e.Property(p => p.TextoConsentimiento).HasMaxLength(8000).IsRequired();
            e.Property(p => p.Revision).IsConcurrencyToken();
            e.Property(p => p.FechaConsentimientoUtc).HasColumnType("timestamp with time zone");
            e.Property(p => p.FechaFinalizacionUtc).HasColumnType("timestamp with time zone");
            e.Property(p => p.FechaRetiroConsentimientoUtc).HasColumnType("timestamp with time zone");
            foreach (var score in scores) e.Property<decimal?>(score).HasPrecision(3, 2);
            // Una aplicación por perfil y versión, incluido el retiro. Reaplicaciones fuera de esta fase.
            e.HasIndex(p => new { p.PerfilEstudianteId, p.VersionInstrumento }).IsUnique();
            e.HasOne<PerfilEstudiante>().WithMany().HasForeignKey(p => p.PerfilEstudianteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<SolicitudAtencion>().WithMany().HasForeignKey(p => p.SolicitudAtencionId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<RespuestaBigFive>(e =>
        {
            var ids = string.Join(", ", Ipip50.Items.Select(i => $"'{i.Id:D}'"));
            e.ToTable("RespuestasBigFive", t =>
            {
                t.HasCheckConstraint("CK_RespuestaBigFive_Valor", "\"Valor\" BETWEEN 1 AND 5");
                t.HasCheckConstraint("CK_RespuestaBigFive_Item", $"\"ItemId\" IN ({ids})");
            });
            e.HasKey(r => r.Id);
            e.Property(r => r.ItemId).HasMaxLength(36).IsRequired();
            e.Property(r => r.FechaActualizacionUtc).HasColumnType("timestamp with time zone");
            e.HasIndex(r => new { r.ParticipacionBigFiveId, r.ItemId }).IsUnique();
            e.HasOne<ParticipacionBigFive>().WithMany().HasForeignKey(r => r.ParticipacionBigFiveId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
