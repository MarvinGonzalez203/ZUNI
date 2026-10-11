using Microsoft.EntityFrameworkCore;
using Zuni.Models;
using Zuni.Models.Agenda;

namespace Zuni.Data;

public static class AgendaCitasConfiguration
{
    public static void ConfigurarAgendaCitas(this ModelBuilder builder)
    {
        builder.Entity<DisponibilidadPsicologo>(e =>
        {
            e.ToTable("DisponibilidadesPsicologo", t =>
            {
                t.HasCheckConstraint("CK_Disponibilidad_Dia", "\"DiaSemana\" BETWEEN 0 AND 6");
                t.HasCheckConstraint("CK_Disponibilidad_Horas", "\"HoraFin\" > \"HoraInicio\"");
                t.HasCheckConstraint("CK_Disponibilidad_Duracion", "\"DuracionMinutos\" BETWEEN 15 AND 120 AND \"DuracionMinutos\" % 5 = 0");
                t.HasCheckConstraint("CK_Disponibilidad_Modalidad", "\"Modalidad\" IN (0, 1, 2)");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.PsicologoUsuarioId).IsRequired();
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.PsicologoUsuarioId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.PsicologoUsuarioId, x.DiaSemana, x.Activo });
        });
        builder.Entity<Cita>(e =>
        {
            e.ToTable("Citas", t =>
            {
                t.HasCheckConstraint("CK_Citas_Fechas", "\"FechaHoraFinUtc\" > \"FechaHoraInicioUtc\"");
                t.HasCheckConstraint("CK_Citas_Estado", "\"Estado\" IN (0, 1, 2, 3)");
                t.HasCheckConstraint("CK_Citas_Modalidad", "\"Modalidad\" IN (0, 1)");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.MotivoCancelacion).HasMaxLength(300);
            e.Property(x => x.FechaHoraInicioUtc).HasColumnType("timestamp with time zone");
            e.Property(x => x.FechaHoraFinUtc).HasColumnType("timestamp with time zone");
            e.Property(x => x.FechaCreacionUtc).HasColumnType("timestamp with time zone");
            e.HasOne<PerfilEstudiante>().WithMany().HasForeignKey(x => x.PerfilEstudianteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.PsicologoUsuarioId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<DisponibilidadPsicologo>().WithMany().HasForeignKey(x => x.DisponibilidadPsicologoId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.PsicologoUsuarioId, x.FechaHoraInicioUtc }).IsUnique()
                .HasFilter("\"Estado\" = 0").HasDatabaseName("UX_Citas_Psicologo_InicioReservado");
            e.HasIndex(x => new { x.PerfilEstudianteId, x.FechaHoraInicioUtc }).IsUnique()
                .HasFilter("\"Estado\" = 0").HasDatabaseName("UX_Citas_Estudiante_InicioReservado");
            e.HasIndex(x => new { x.PsicologoUsuarioId, x.Estado, x.FechaHoraInicioUtc });
        });
    }
}
