using Microsoft.EntityFrameworkCore;
using Zuni.Models.Agenda;
using Zuni.Models.Atencion;

namespace Zuni.Data;

public static class RegistroAtencionConfiguration
{
    public static void ConfigurarRegistroAtencion(this ModelBuilder builder)
    {
        builder.Entity<RegistroAtencionCita>(e =>
        {
            e.ToTable("RegistrosAtencionCita", t =>
            {
                t.HasCheckConstraint("CK_RegistroAtencion_Asistencia",
                    "(\"Asistio\" AND length(trim(coalesce(\"ResultadoProfesionalPrivado\", ''))) > 0 AND length(trim(coalesce(\"ResenaRecomendacionesEstudiante\", ''))) > 0 AND length(trim(coalesce(\"ResultadoBreveEstudiante\", ''))) > 0) OR (NOT \"Asistio\" AND \"ResultadoProfesionalPrivado\" IS NULL AND \"ResultadoBreveEstudiante\" IS NULL AND NOT \"RecomiendaOtraCita\")");
                t.HasCheckConstraint("CK_RegistroAtencion_Recomendacion",
                    "(\"RecomiendaOtraCita\" AND \"Asistio\" AND length(trim(coalesce(\"IndicacionesProximaCita\", ''))) > 0) OR (NOT \"RecomiendaOtraCita\" AND \"IndicacionesProximaCita\" IS NULL)");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.ResultadoProfesionalPrivado).HasMaxLength(4000);
            e.Property(x => x.ResenaRecomendacionesEstudiante).HasMaxLength(2000);
            e.Property(x => x.ResultadoBreveEstudiante).HasMaxLength(2000);
            e.Property(x => x.IndicacionesProximaCita).HasMaxLength(2000);
            e.Property(x => x.FechaCierreUtc).HasColumnType("timestamp with time zone");
            e.HasOne<Cita>().WithOne().HasForeignKey<RegistroAtencionCita>(x => x.CitaId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.CitaId).IsUnique();
            e.HasOne<SolicitudAtencion>().WithMany().HasForeignKey(x => x.SolicitudAtencionId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
