using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Zuni.Models;
using Zuni.Models.Atencion;

namespace Zuni.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<Zuni.Models.BigFive.ParticipacionBigFive> ParticipacionesBigFive => Set<Zuni.Models.BigFive.ParticipacionBigFive>();
        public DbSet<Zuni.Models.BigFive.RespuestaBigFive> RespuestasBigFive => Set<Zuni.Models.BigFive.RespuestaBigFive>();
        public DbSet<SolicitudAtencion> SolicitudesAtencion => Set<SolicitudAtencion>();
        public DbSet<ConsentimientoAtencion> ConsentimientosAtencion => Set<ConsentimientoAtencion>();
        public DbSet<ExpedienteInicial> ExpedientesIniciales => Set<ExpedienteInicial>();
        public DbSet<ContactoEmergenciaExpediente> ContactosEmergenciaExpediente => Set<ContactoEmergenciaExpediente>();
        public DbSet<AsignacionEstudiantePsicologo> AsignacionesEstudiantePsicologo => Set<AsignacionEstudiantePsicologo>();
        public DbSet<EventoAccesoClinico> EventosAccesoClinico => Set<EventoAccesoClinico>();
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
            builder.ConfigurarBigFive();

            builder.Entity<SolicitudAtencion>(e =>
            {
                e.ToTable("SolicitudesAtencion", t =>
                {
                    t.HasCheckConstraint("CK_SolicitudesAtencion_Estado", "\"Estado\" IN (0, 1, 2)");
                    t.HasCheckConstraint("CK_SolicitudesAtencion_TipoIngreso", "\"TipoIngreso\" IN (0, 1)");
                });
                e.HasKey(x => x.Id);
                e.Property(x => x.FechaSolicitudUtc).HasColumnType("timestamp with time zone");
                e.Property(x => x.Estado).HasConversion<int>();
                e.Property(x => x.TipoIngreso).HasConversion<int>();
                e.HasOne<PerfilEstudiante>().WithMany().HasForeignKey(x => x.PerfilEstudianteId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UsuarioReferenteId).OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(x => x.PerfilEstudianteId).IsUnique()
                    .HasFilter("\"Estado\" IN (0, 1)").HasDatabaseName("UX_SolicitudesAtencion_Activa");
            });
            builder.Entity<ConsentimientoAtencion>(e =>
            {
                e.ToTable("ConsentimientosAtencion", t => t.HasCheckConstraint("CK_ConsentimientosAtencion_Aceptado", "\"Aceptado\" = TRUE"));
                e.HasKey(x => x.Id);
                e.Property(x => x.VersionConsentimiento).HasMaxLength(50).IsRequired();
                e.Property(x => x.FechaRespuestaUtc).HasColumnType("timestamp with time zone");
                e.HasOne<SolicitudAtencion>().WithOne().HasForeignKey<ConsentimientoAtencion>(x => x.SolicitudAtencionId).OnDelete(DeleteBehavior.Restrict);
            });
            builder.Entity<ExpedienteInicial>(e =>
            {
                e.ToTable("ExpedientesIniciales", t =>
                {
                    t.HasCheckConstraint("CK_ExpedientesIniciales_MayorEdad", "\"Edad\" BETWEEN 18 AND 120 AND \"EsMayorEdad\" = TRUE");
                    t.HasCheckConstraint("CK_ExpedientesIniciales_Requeridos", "length(btrim(\"Direccion\")) > 0 AND length(btrim(\"IdiomaPreferido\")) > 0 AND length(btrim(\"MotivoConsulta\")) > 0");
                });
                e.HasKey(x => x.Id);
                e.Property(x => x.Sexo).HasMaxLength(50);
                e.Property(x => x.Direccion).HasMaxLength(300).IsRequired();
                e.Property(x => x.IdiomaPreferido).HasMaxLength(80).IsRequired();
                e.Property(x => x.MotivoConsulta).HasMaxLength(2000).IsRequired();
                e.Property(x => x.NombreReferente).HasMaxLength(150);
                e.Property(x => x.MotivoReferencia).HasMaxLength(1000);
                e.Property(x => x.ConsideracionesAtencion).HasMaxLength(1500);
                e.Property(x => x.FechaCreacionUtc).HasColumnType("timestamp with time zone");
                e.HasOne<SolicitudAtencion>().WithOne().HasForeignKey<ExpedienteInicial>(x => x.SolicitudAtencionId).OnDelete(DeleteBehavior.Restrict);
            });
            builder.Entity<ContactoEmergenciaExpediente>(e =>
            {
                e.ToTable("ContactosEmergenciaExpediente", t =>
                {
                    t.HasCheckConstraint("CK_ContactosEmergenciaExpediente_Orden", "\"Orden\" BETWEEN 1 AND 3");
                    t.HasCheckConstraint("CK_ContactosEmergenciaExpediente_Telefono", "\"Telefono\" ~ '^[0-9]{8}$'");
                    t.HasCheckConstraint("CK_ContactosEmergenciaExpediente_NombreRelacion", "length(btrim(\"Nombre\")) > 0 AND length(btrim(\"Relacion\")) > 0");
                });
                e.HasKey(x => x.Id);
                e.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
                e.Property(x => x.Relacion).HasMaxLength(60).IsRequired();
                e.Property(x => x.Telefono).HasMaxLength(8).IsRequired();
                e.HasIndex(x => new { x.ExpedienteInicialId, x.Orden }).IsUnique();
                e.HasOne<ExpedienteInicial>().WithMany().HasForeignKey(x => x.ExpedienteInicialId).OnDelete(DeleteBehavior.Restrict);
            });
            builder.Entity<AsignacionEstudiantePsicologo>(e =>
            {
                e.ToTable("AsignacionesEstudiantePsicologo", t =>
                    t.HasCheckConstraint("CK_AsignacionesEstudiantePsicologo_Fechas", "\"FechaFinalizacionUtc\" IS NULL OR \"FechaFinalizacionUtc\" >= \"FechaAsignacionUtc\""));
                e.HasKey(x => x.Id);
                e.Property(x => x.PsicologoUsuarioId).IsRequired();
                e.Property(x => x.FechaAsignacionUtc).HasColumnType("timestamp with time zone");
                e.Property(x => x.FechaFinalizacionUtc).HasColumnType("timestamp with time zone");
                e.HasOne<PerfilEstudiante>().WithMany().HasForeignKey(x => x.PerfilEstudianteId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.PsicologoUsuarioId).OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(x => x.PerfilEstudianteId).IsUnique()
                    .HasFilter("\"FechaFinalizacionUtc\" IS NULL").HasDatabaseName("UX_AsignacionesEstudiantePsicologo_Vigente");
                e.HasIndex(x => new { x.PsicologoUsuarioId, x.FechaFinalizacionUtc });
            });
            builder.Entity<EventoAccesoClinico>(e =>
            {
                e.ToTable("EventosAccesoClinico");
                e.HasKey(x => x.Id);
                e.Property(x => x.UsuarioId).IsRequired();
                e.Property(x => x.TipoAcceso).HasMaxLength(50).IsRequired();
                e.Property(x => x.FechaUtc).HasColumnType("timestamp with time zone");
                e.HasIndex(x => new { x.UsuarioId, x.FechaUtc });
                e.HasIndex(x => new { x.SolicitudAtencionId, x.FechaUtc });
                e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne<SolicitudAtencion>().WithMany().HasForeignKey(x => x.SolicitudAtencionId).OnDelete(DeleteBehavior.Restrict);
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
