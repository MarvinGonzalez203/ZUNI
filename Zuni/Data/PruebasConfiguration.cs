using Microsoft.EntityFrameworkCore;
using Zuni.Models;
using Zuni.Models.Pruebas;

namespace Zuni.Data;

public static class PruebasConfiguration
{
    public static void ConfigurarPruebas(this ModelBuilder builder)
    {
        PruebasExploratorias.Configurar(builder);
        builder.Entity<Prueba>(e =>
        {
            e.ToTable("PruebasDirector");
            e.Property(x => x.Revision).IsConcurrencyToken();
            e.HasMany(x => x.Dimensiones).WithOne().HasForeignKey(x => x.PruebaId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Preguntas).WithOne().HasForeignKey(x => x.PruebaId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<DimensionPrueba>(e =>
        {
            e.ToTable("DimensionesPrueba");
            e.HasIndex(x => new { x.PruebaId, x.Nombre }).IsUnique();
            e.HasAlternateKey(x => new { x.PruebaId, x.Id });
        });
        builder.Entity<PreguntaPrueba>(e =>
        {
            e.ToTable("PreguntasPrueba");
            e.HasIndex(x => new { x.PruebaId, x.Orden }).IsUnique();
            e.HasOne<DimensionPrueba>().WithMany().HasForeignKey(x => new { x.PruebaId, x.DimensionId })
                .HasPrincipalKey(x => new { x.PruebaId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Opciones).WithOne().HasForeignKey(x => x.PreguntaPruebaId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<OpcionPrueba>(e =>
        {
            e.HasAlternateKey(x=>new{x.PreguntaPruebaId,x.Id});
            e.ToTable("OpcionesPrueba");
            e.HasIndex(x => new { x.PreguntaPruebaId, x.Orden }).IsUnique();
        });
        builder.Entity<AsignacionPrueba>(e =>
        {
            e.Property(x=>x.Revision).IsConcurrencyToken();
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x=>x.PsicologoInductorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Zuni.Models.Agenda.Cita>().WithMany().HasForeignKey(x=>x.CitaInduccionId).OnDelete(DeleteBehavior.Restrict);
            e.ToTable("AsignacionesPrueba");
            e.HasIndex(x => new { x.PruebaId, x.PerfilEstudianteId }).IsUnique();
            e.HasOne<Prueba>().WithMany().HasForeignKey(x => x.PruebaId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<PerfilEstudiante>().WithMany().HasForeignKey(x => x.PerfilEstudianteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.DirectorUsuarioId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<RespuestaPrueba>(e=>{
            e.ToTable("RespuestasPruebas");e.HasKey(x=>new{x.AsignacionPruebaId,x.PreguntaPruebaId});
            e.HasOne<AsignacionPrueba>().WithMany().HasForeignKey(x=>x.AsignacionPruebaId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<OpcionPrueba>().WithMany().HasForeignKey(x=>new{x.PreguntaPruebaId,x.OpcionPruebaId}).HasPrincipalKey(x=>new{x.PreguntaPruebaId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
