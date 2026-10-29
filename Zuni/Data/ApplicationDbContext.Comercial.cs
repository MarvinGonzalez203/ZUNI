using Microsoft.EntityFrameworkCore;
using Zuni.Models;
using Zuni.Models.Comercial;
namespace Zuni.Data;
public partial class ApplicationDbContext
{
    public DbSet<CentroComercial> CentrosComerciales => Set<CentroComercial>();
    public DbSet<LecturaVersion> LecturasVersion => Set<LecturaVersion>();
    private static void ConfigurarComercial(ModelBuilder b)
    {
        b.Entity<CentroComercial>(e=>{
            e.ToTable("CentrosComerciales",t=>{
                t.HasCheckConstraint("CK_CentroComercial_Periodo","\"MesesPlan\" IN (1,3,6,12)");
                t.HasCheckConstraint("CK_CentroComercial_Estado","\"Estado\" IN (0,1,2)");
                t.HasCheckConstraint("CK_CentroComercial_Fechas","\"Fin\" >= \"Inicio\"");
                t.HasCheckConstraint("CK_CentroComercial_Importe","\"ImporteGtq\" >= 0");
            });
            e.HasKey(x=>x.Id); e.HasIndex(x=>x.Fin);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x=>x.ActualizadoPorId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<LecturaVersion>(e=>{
            e.ToTable("LecturasVersion"); e.HasKey(x=>new{x.UsuarioId,x.Version});
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x=>x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
