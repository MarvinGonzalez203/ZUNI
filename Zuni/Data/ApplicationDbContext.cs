using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Zuni.Models;

namespace Zuni.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<AsignacionEstudiantePsicologo> AsignacionesEstudiantePsicologo =>
            Set<AsignacionEstudiantePsicologo>();

        public DbSet<PasswordResetToken> PasswordResetTokens =>
            Set<PasswordResetToken>();

        public DbSet<PerfilEstudiante> PerfilesEstudiante =>
            Set<PerfilEstudiante>();

        public DbSet<AuditoriaUsuario> AuditoriaUsuarios =>
            Set<AuditoriaUsuario>();

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ConfigurarEvaluaciones();

            builder.Entity<AsignacionEstudiantePsicologo>(entity =>
            {
                entity.ToTable("AsignacionesEstudiantePsicologo", table =>
                    table.HasCheckConstraint("CK_AsignacionesEstudiantePsicologo_Fechas",
                        "\"FechaFinalizacionUtc\" IS NULL OR \"FechaFinalizacionUtc\" >= \"FechaAsignacionUtc\""));
                entity.HasKey(a => a.Id);
                entity.Property(a => a.PsicologoUsuarioId).IsRequired();
                entity.Property(a => a.FechaAsignacionUtc).HasColumnType("timestamp with time zone");
                entity.Property(a => a.FechaFinalizacionUtc).HasColumnType("timestamp with time zone");
                entity.HasOne(a => a.PerfilEstudiante).WithMany()
                    .HasForeignKey(a => a.PerfilEstudianteId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(a => a.PsicologoUsuario).WithMany()
                    .HasForeignKey(a => a.PsicologoUsuarioId).OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(a => new { a.PsicologoUsuarioId, a.FechaFinalizacionUtc });
                entity.HasIndex(a => a.PerfilEstudianteId).IsUnique()
                    .HasDatabaseName(Zuni.Services.AsignacionPsicologoService.IndiceAsignacionVigente)
                    .HasFilter("\"FechaFinalizacionUtc\" IS NULL");
            });

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
        }
    }
}
