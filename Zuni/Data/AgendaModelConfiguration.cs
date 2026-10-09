using Microsoft.EntityFrameworkCore;
using Zuni.Models;
namespace Zuni.Data;
public static class AgendaModelConfiguration
{
    public static void ConfigurarAgenda(this ModelBuilder builder)
    {
        builder.Entity<DiaAgendaPsicologo>(e=>{
            e.ToTable("DiasAgendaPsicologo"); e.HasKey(x=>x.Id);
            e.HasIndex(x=>new{x.PsicologoId,x.Fecha}).IsUnique();
            e.HasOne(x=>x.Psicologo).WithMany().HasForeignKey(x=>x.PsicologoId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<HorarioAgendaPsicologo>(e=>{
            e.ToTable("HorariosAgendaPsicologo",t=>{
                t.HasCheckConstraint("CK_Agenda_Horas","\"Inicio\" >= TIME '08:00' AND \"Fin\" <= TIME '18:00' AND \"Fin\" > \"Inicio\"");
                t.HasCheckConstraint("CK_Agenda_Duracion","\"DuracionMinutos\" IN (15,30,45,50,60)");
                t.HasCheckConstraint("CK_Agenda_Modalidad","\"Modalidad\" IN ('Presencial','Virtual')");
            });
            e.HasKey(x=>x.Id); e.Property(x=>x.Modalidad).HasMaxLength(10);
            e.HasOne(x=>x.Dia).WithMany(x=>x.Horarios).HasForeignKey(x=>x.DiaId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
