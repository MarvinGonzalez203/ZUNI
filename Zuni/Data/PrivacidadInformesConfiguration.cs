using Microsoft.EntityFrameworkCore;
using Zuni.Models;
using Zuni.Models.Atencion;
using Zuni.Models.Privacidad;
using Zuni.Models.Informes;

namespace Zuni.Data;

public static class PrivacidadInformesConfiguration
{
    public static void ConfigurarPrivacidadInformes(this ModelBuilder b)
    {
        b.Entity<PoliticaPrivacidad>(e => {
            e.ToTable("PoliticasPrivacidad"); e.HasKey(x => x.Id); e.HasIndex(x => x.Version).IsUnique();
            e.Property(x => x.Institucion).HasMaxLength(150); e.Property(x => x.Responsable).HasMaxLength(150); e.Property(x => x.Contacto).HasMaxLength(254);
            foreach (var name in new[] { "Finalidades", "DatosYAccesos", "Conservacion", "ProcedimientoDerechos" }) e.Property<string>(name).HasMaxLength(4000);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.PublicadaPorId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<AceptacionPrivacidad>(e => {
            e.ToTable("AceptacionesPrivacidad"); e.HasKey(x => new { x.PoliticaId, x.UsuarioId });
            e.HasOne<PoliticaPrivacidad>().WithMany().HasForeignKey(x => x.PoliticaId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<SolicitudDerechoDatos>(e => {
            e.ToTable("SolicitudesDerechosDatos", t => { t.HasCheckConstraint("CK_Derecho_Tipo", "\"Tipo\" IN (0,1,2,3)"); t.HasCheckConstraint("CK_Derecho_Estado", "\"Estado\" IN (0,1)"); });
            e.HasKey(x => x.Id); e.Property(x => x.Detalle).HasMaxLength(1000); e.Property(x => x.Respuesta).HasMaxLength(2000); e.Property(x => x.Revision).IsConcurrencyToken();
            e.HasIndex(x => new { x.UsuarioId, x.FechaSolicitudUtc });
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ResponsableId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<InformeEducativo>(e => {
            e.ToTable("InformesEducativos"); e.HasKey(x => x.Id);
            e.HasOne<SolicitudAtencion>().WithMany().HasForeignKey(x => x.SolicitudAtencionId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.AutorUsuarioId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<VersionInformeEducativo>(e => {
            e.ToTable("VersionesInformesEducativos", t => { t.HasCheckConstraint("CK_Informe_Estado", "\"Estado\" BETWEEN 0 AND 6"); t.HasCheckConstraint("CK_Informe_Periodo", "\"PeriodoFin\" >= \"PeriodoInicio\""); });
            e.HasKey(x => x.Id); e.Property(x => x.Revision).IsConcurrencyToken(); e.HasIndex(x => new { x.InformeId, x.Numero }).IsUnique();
            e.HasIndex(x => x.InformeId).IsUnique().HasFilter("\"Estado\" = 3").HasDatabaseName("UX_Informe_VersionEmitida");
            e.HasOne<InformeEducativo>().WithMany().HasForeignKey(x => x.InformeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.DestinatarioUsuarioId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Objetivo).HasMaxLength(1000); e.Property(x => x.Fuentes).HasMaxLength(2000); e.Property(x => x.Observaciones).HasMaxLength(3000);
            e.Property(x => x.Necesidades).HasMaxLength(2000); e.Property(x => x.Recomendaciones).HasMaxLength(3000); e.Property(x => x.ResponsablesAcciones).HasMaxLength(1000);
            e.Property(x => x.Institucion).HasMaxLength(150); e.Property(x => x.Carne).HasMaxLength(30);
            foreach (var name in new[] { "Estudiante", "Psicologo", "Destinatario" }) e.Property<string>(name).HasMaxLength(100);
        });
        b.Entity<AutorizacionInforme>(e => {
            e.ToTable("AutorizacionesInformes"); e.HasKey(x => x.VersionInformeId);
            e.Property(x => x.VersionConsentimiento).HasMaxLength(50); e.Property(x => x.TextoConsentimiento).HasMaxLength(3000);
            e.HasOne<VersionInformeEducativo>().WithOne().HasForeignKey<AutorizacionInforme>(x => x.VersionInformeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<PoliticaPrivacidad>().WithMany().HasForeignKey(x => x.PoliticaPrivacidadId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.EstudianteUsuarioId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
