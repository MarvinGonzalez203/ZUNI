using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Zuni.Migrations
{
    /// <inheritdoc />
    public partial class AddProteccionClinicaYEvidencias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ValoracionRendimiento",
                table: "VersionesInformesEducativos",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<string>(
                name: "ResponsablesAcciones",
                table: "VersionesInformesEducativos",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "RelatoRendimiento",
                table: "VersionesInformesEducativos",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<string>(
                name: "Recomendaciones",
                table: "VersionesInformesEducativos",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3000)",
                oldMaxLength: 3000);

            migrationBuilder.AlterColumn<string>(
                name: "Psicologo",
                table: "VersionesInformesEducativos",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Observaciones",
                table: "VersionesInformesEducativos",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3000)",
                oldMaxLength: 3000);

            migrationBuilder.AlterColumn<string>(
                name: "Objetivo",
                table: "VersionesInformesEducativos",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "Necesidades",
                table: "VersionesInformesEducativos",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<string>(
                name: "Fuentes",
                table: "VersionesInformesEducativos",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<string>(
                name: "Estudiante",
                table: "VersionesInformesEducativos",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Destinatario",
                table: "VersionesInformesEducativos",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Carne",
                table: "VersionesInformesEducativos",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "Respuesta",
                table: "SolicitudesDerechosDatos",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Detalle",
                table: "SolicitudesDerechosDatos",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "ResultadoProfesionalPrivado",
                table: "RegistrosAtencionCita",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ResultadoBreveEstudiante",
                table: "RegistrosAtencionCita",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ResenaRecomendacionesEstudiante",
                table: "RegistrosAtencionCita",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IndicacionesProximaCita",
                table: "RegistrosAtencionCita",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Sexo",
                table: "ExpedientesIniciales",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NombreReferente",
                table: "ExpedientesIniciales",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MotivoReferencia",
                table: "ExpedientesIniciales",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MotivoConsulta",
                table: "ExpedientesIniciales",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<string>(
                name: "IdiomaPreferido",
                table: "ExpedientesIniciales",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(80)",
                oldMaxLength: 80);

            migrationBuilder.AlterColumn<string>(
                name: "Direccion",
                table: "ExpedientesIniciales",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300);

            migrationBuilder.AlterColumn<string>(
                name: "ConsideracionesAtencion",
                table: "ExpedientesIniciales",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1500)",
                oldMaxLength: 1500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Telefono",
                table: "ContactosEmergenciaExpediente",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(8)",
                oldMaxLength: 8);

            migrationBuilder.AlterColumn<string>(
                name: "Relacion",
                table: "ContactosEmergenciaExpediente",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(60)",
                oldMaxLength: 60);

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "ContactosEmergenciaExpediente",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "TextoConsentimiento",
                table: "AutorizacionesInformes",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3000)",
                oldMaxLength: 3000);

            migrationBuilder.AlterColumn<string>(
                name: "Motivo",
                table: "AuditoriaUsuarios",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DatosNuevos",
                table: "AuditoriaUsuarios",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DatosAnteriores",
                table: "AuditoriaUsuarios",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "EstadoProteccion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    ConversionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstadoProteccion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExportacionesSeguridad",
                columns: table => new
                {
                    EventoId = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportacionesSeguridad", x => x.EventoId);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosSeguridad",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Referencia = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ContenidoCifrado = table.Column<string>(type: "text", nullable: false),
                    Firma = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosSeguridad", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosSeguridad_EventoId",
                table: "RegistrosSeguridad",
                column: "EventoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosSeguridad_Tipo_Referencia",
                table: "RegistrosSeguridad",
                columns: new[] { "Tipo", "Referencia" }, unique: true);
            migrationBuilder.DropCheckConstraint("CK_ContactosEmergenciaExpediente_Telefono", "ContactosEmergenciaExpediente");
            migrationBuilder.AddCheckConstraint("CK_ContactosEmergenciaExpediente_Telefono", "ContactosEmergenciaExpediente", "\"Telefono\" ~ '^[0-9]{8}$' OR \"Telefono\" LIKE 'zenc1:%'");
            migrationBuilder.Sql("""
                CREATE FUNCTION zuni_seguridad_inmutable() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN RAISE EXCEPTION 'Registro de seguridad inmutable' USING ERRCODE = '42501'; END;
                $$;
                """);
            foreach (var tabla in new[] { "AuditoriaUsuarios", "EventosAccesoClinico", "RegistrosSeguridad", "ExportacionesSeguridad", "ConsentimientosAtencion", "AceptacionesPrivacidad", "EstadoProteccion" })
            {
                migrationBuilder.Sql($"CREATE TRIGGER seguridad_inmutable BEFORE UPDATE OR DELETE OR TRUNCATE ON \"{tabla}\" FOR EACH STATEMENT EXECUTE FUNCTION zuni_seguridad_inmutable(); ALTER TABLE \"{tabla}\" ENABLE ALWAYS TRIGGER seguridad_inmutable;");
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("La conversión clínica no admite reversión automática. Restaura un respaldo coherente de base y claves en mantenimiento.");
        }
    }
}
