using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Zuni.Models;

namespace Zuni.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<PasswordResetToken> PasswordResetTokens =>
            Set<PasswordResetToken>();

        public DbSet<PerfilEstudiante> PerfilesEstudiante =>
            Set<PerfilEstudiante>();

        public DbSet<Prueba> Pruebas => Set<Prueba>();

        public DbSet<DimensionPrueba> DimensionesPrueba => Set<DimensionPrueba>();

        public DbSet<PreguntaPrueba> PreguntasPrueba => Set<PreguntaPrueba>();

        public DbSet<OpcionPreguntaPrueba> OpcionesPreguntaPrueba => Set<OpcionPreguntaPrueba>();

        public DbSet<AsignacionPrueba> AsignacionesPrueba => Set<AsignacionPrueba>();

        public DbSet<AuditoriaUsuario> AuditoriaUsuarios =>
            Set<AuditoriaUsuario>();

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<AuditoriaUsuario>(entity =>
            {
                entity.ToTable("AuditoriaUsuarios");
                entity.HasKey(auditoria => auditoria.Id);

                // Identificadores históricos sin relaciones para conservar la auditoría
                // incluso si los usuarios se eliminan físicamente.
                entity.Property(auditoria => auditoria.UsuarioAfectadoId)
                    .IsRequired();

                entity.Property(auditoria => auditoria.AdministradorId)
                    .IsRequired();

                entity.Property(auditoria => auditoria.Accion)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(auditoria => auditoria.Motivo)
                    .HasMaxLength(500);

                entity.Property(auditoria => auditoria.DatosAnteriores)
                    .HasColumnType("jsonb");

                entity.Property(auditoria => auditoria.DatosNuevos)
                    .HasColumnType("jsonb");

                entity.Property(auditoria => auditoria.FechaUtc)
                    .HasColumnType("timestamp with time zone")
                    .IsRequired();

                entity.HasIndex(auditoria => auditoria.UsuarioAfectadoId);
                entity.HasIndex(auditoria => auditoria.AdministradorId);
                entity.HasIndex(auditoria => auditoria.FechaUtc);
            });

            builder.Entity<PasswordResetToken>(entity =>
            {
                entity.ToTable("PasswordResetTokens");
                entity.HasKey(token => token.Id);

                entity.Property(token => token.UserId)
                    .IsRequired();

                entity.Property(token => token.TokenHash)
                    .HasMaxLength(64)
                    .IsRequired();

                entity.HasIndex(token => token.TokenHash)
                    .IsUnique();

                entity.HasIndex(token => new
                {
                    token.UserId,
                    token.ExpiresAtUtc,
                    token.UsedAtUtc
                });

                entity.HasOne(token => token.User)
                    .WithMany(user => user.PasswordResetTokens)
                    .HasForeignKey(token => token.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<PerfilEstudiante>(entity =>
            {
                entity.ToTable("PerfilesEstudiante");
                entity.HasKey(perfil => perfil.Id);

                entity.Property(perfil => perfil.UsuarioId)
                    .IsRequired();

                entity.Property(perfil => perfil.Carne)
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(perfil => perfil.Carrera)
                    .HasMaxLength(150);

                entity.Property(perfil => perfil.Semestre)
                    .HasMaxLength(50);

                entity.Property(perfil => perfil.CicloAcademico)
                    .HasMaxLength(30);

                entity.Property(perfil => perfil.Telefono)
                    .HasMaxLength(20);

                entity.Property(perfil => perfil.NombreContactoEmergencia)
                    .HasMaxLength(150);

                entity.Property(perfil => perfil.TelefonoContactoEmergencia)
                    .HasMaxLength(20);

                entity.Property(perfil => perfil.RelacionContactoEmergencia)
                    .HasMaxLength(60);

                entity.HasIndex(perfil => perfil.UsuarioId)
                    .IsUnique();

                entity.HasIndex(perfil => perfil.Carne)
                    .IsUnique();

                entity.HasOne(perfil => perfil.Usuario)
                    .WithOne(usuario => usuario.PerfilEstudiante)
                    .HasForeignKey<PerfilEstudiante>(
                        perfil => perfil.UsuarioId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Prueba>(entity =>
            {
                entity.ToTable("Pruebas");
                entity.HasKey(prueba => prueba.Id);

                entity.Property(prueba => prueba.Nombre)
                    .HasMaxLength(120)
                    .IsRequired();

                entity.Property(prueba => prueba.Descripcion)
                    .HasMaxLength(1000);

                entity.HasIndex(prueba => prueba.Activa);
            });

            builder.Entity<DimensionPrueba>(entity =>
            {
                entity.ToTable("DimensionesPrueba");
                entity.HasKey(dimension => dimension.Id);

                entity.Property(dimension => dimension.Nombre)
                    .HasMaxLength(100)
                    .IsRequired();
                entity.Property(dimension => dimension.Descripcion)
                    .HasMaxLength(500);
                entity.HasIndex(dimension => new { dimension.PruebaId, dimension.Orden });

                entity.HasOne(dimension => dimension.Prueba)
                    .WithMany()
                    .HasForeignKey(dimension => dimension.PruebaId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<PreguntaPrueba>(entity =>
            {
                entity.ToTable("PreguntasPrueba");
                entity.HasKey(pregunta => pregunta.Id);

                entity.Property(pregunta => pregunta.Texto)
                    .HasMaxLength(1000)
                    .IsRequired();
                entity.Property(pregunta => pregunta.PropositoExploratorio)
                    .HasMaxLength(500);
                entity.HasIndex(pregunta => new { pregunta.DimensionPruebaId, pregunta.Orden });

                entity.HasOne(pregunta => pregunta.Dimension)
                    .WithMany(dimension => dimension.Preguntas)
                    .HasForeignKey(pregunta => pregunta.DimensionPruebaId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<OpcionPreguntaPrueba>(entity =>
            {
                entity.ToTable("OpcionesPreguntaPrueba");
                entity.HasKey(opcion => opcion.Id);

                entity.Property(opcion => opcion.Texto)
                    .HasMaxLength(200)
                    .IsRequired();
                entity.HasIndex(opcion => new { opcion.PreguntaPruebaId, opcion.Orden });

                entity.HasOne(opcion => opcion.Pregunta)
                    .WithMany(pregunta => pregunta.Opciones)
                    .HasForeignKey(opcion => opcion.PreguntaPruebaId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<AsignacionPrueba>(entity =>
            {
                entity.ToTable("AsignacionesPrueba");
                entity.HasKey(asignacion => asignacion.Id);

                entity.Property(asignacion => asignacion.EstudianteId)
                    .HasMaxLength(450)
                    .IsRequired();
                entity.Property(asignacion => asignacion.AsignadaPorId)
                    .HasMaxLength(450)
                    .IsRequired();
                entity.Property(asignacion => asignacion.Modalidad)
                    .IsRequired();

                entity.HasIndex(asignacion => new { asignacion.PruebaId, asignacion.EstudianteId })
                    .IsUnique()
                    .HasFilter("\"Cancelada\" = FALSE");
                entity.HasIndex(asignacion => new { asignacion.EstudianteId, asignacion.FechaAsignacionUtc });

                entity.HasOne<Prueba>()
                    .WithMany()
                    .HasForeignKey(asignacion => asignacion.PruebaId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey(asignacion => asignacion.EstudianteId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey(asignacion => asignacion.AsignadaPorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
