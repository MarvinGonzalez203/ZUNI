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

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

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
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(perfil => perfil.Semestre)
                    .HasMaxLength(50);

                entity.Property(perfil => perfil.Telefono)
                    .HasMaxLength(20);

                entity.Property(perfil => perfil.NombreContactoEmergencia)
                    .HasMaxLength(150);

                entity.Property(perfil => perfil.TelefonoContactoEmergencia)
                    .HasMaxLength(20);

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
