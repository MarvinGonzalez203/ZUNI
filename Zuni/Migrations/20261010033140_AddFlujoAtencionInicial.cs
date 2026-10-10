using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zuni.Migrations
{
    /// <inheritdoc />
    public partial class AddFlujoAtencionInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AsignacionesEstudiantePsicologo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PerfilEstudianteId = table.Column<Guid>(type: "uuid", nullable: false),
                    PsicologoUsuarioId = table.Column<string>(type: "text", nullable: false),
                    FechaAsignacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaFinalizacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsignacionesEstudiantePsicologo", x => x.Id);
                    table.CheckConstraint("CK_AsignacionesEstudiantePsicologo_Fechas", "\"FechaFinalizacionUtc\" IS NULL OR \"FechaFinalizacionUtc\" >= \"FechaAsignacionUtc\"");
                    table.ForeignKey(
                        name: "FK_AsignacionesEstudiantePsicologo_AspNetUsers_PsicologoUsuari~",
                        column: x => x.PsicologoUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AsignacionesEstudiantePsicologo_PerfilesEstudiante_PerfilEs~",
                        column: x => x.PerfilEstudianteId,
                        principalTable: "PerfilesEstudiante",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SolicitudesAtencion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PerfilEstudianteId = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaSolicitudUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    TipoIngreso = table.Column<int>(type: "integer", nullable: false),
                    UsuarioReferenteId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesAtencion", x => x.Id);
                    table.CheckConstraint("CK_SolicitudesAtencion_Estado", "\"Estado\" IN (0, 1, 2)");
                    table.CheckConstraint("CK_SolicitudesAtencion_TipoIngreso", "\"TipoIngreso\" IN (0, 1)");
                    table.ForeignKey(
                        name: "FK_SolicitudesAtencion_AspNetUsers_UsuarioReferenteId",
                        column: x => x.UsuarioReferenteId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesAtencion_PerfilesEstudiante_PerfilEstudianteId",
                        column: x => x.PerfilEstudianteId,
                        principalTable: "PerfilesEstudiante",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConsentimientosAtencion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SolicitudAtencionId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionConsentimiento = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Aceptado = table.Column<bool>(type: "boolean", nullable: false),
                    FechaRespuestaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsentimientosAtencion", x => x.Id);
                    table.CheckConstraint("CK_ConsentimientosAtencion_Aceptado", "\"Aceptado\" = TRUE");
                    table.ForeignKey(
                        name: "FK_ConsentimientosAtencion_SolicitudesAtencion_SolicitudAtenci~",
                        column: x => x.SolicitudAtencionId,
                        principalTable: "SolicitudesAtencion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventosAccesoClinico",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<string>(type: "text", nullable: false),
                    SolicitudAtencionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoAcceso = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosAccesoClinico", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventosAccesoClinico_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventosAccesoClinico_SolicitudesAtencion_SolicitudAtencionId",
                        column: x => x.SolicitudAtencionId,
                        principalTable: "SolicitudesAtencion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExpedientesIniciales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SolicitudAtencionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Edad = table.Column<int>(type: "integer", nullable: false),
                    EsMayorEdad = table.Column<bool>(type: "boolean", nullable: false),
                    Sexo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Direccion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    IdiomaPreferido = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    MotivoConsulta = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    NombreReferente = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    MotivoReferencia = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ConsideracionesAtencion = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: true),
                    FechaCreacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpedientesIniciales", x => x.Id);
                    table.CheckConstraint("CK_ExpedientesIniciales_MayorEdad", "\"Edad\" BETWEEN 18 AND 120 AND \"EsMayorEdad\" = TRUE");
                    table.CheckConstraint("CK_ExpedientesIniciales_Requeridos", "length(btrim(\"Direccion\")) > 0 AND length(btrim(\"IdiomaPreferido\")) > 0 AND length(btrim(\"MotivoConsulta\")) > 0");
                    table.ForeignKey(
                        name: "FK_ExpedientesIniciales_SolicitudesAtencion_SolicitudAtencionId",
                        column: x => x.SolicitudAtencionId,
                        principalTable: "SolicitudesAtencion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContactosEmergenciaExpediente",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpedienteInicialId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Relacion = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Telefono = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactosEmergenciaExpediente", x => x.Id);
                    table.CheckConstraint("CK_ContactosEmergenciaExpediente_NombreRelacion", "length(btrim(\"Nombre\")) > 0 AND length(btrim(\"Relacion\")) > 0");
                    table.CheckConstraint("CK_ContactosEmergenciaExpediente_Orden", "\"Orden\" BETWEEN 1 AND 3");
                    table.CheckConstraint("CK_ContactosEmergenciaExpediente_Telefono", "\"Telefono\" ~ '^[0-9]{8}$'");
                    table.ForeignKey(
                        name: "FK_ContactosEmergenciaExpediente_ExpedientesIniciales_Expedien~",
                        column: x => x.ExpedienteInicialId,
                        principalTable: "ExpedientesIniciales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesEstudiantePsicologo_PsicologoUsuarioId_FechaFin~",
                table: "AsignacionesEstudiantePsicologo",
                columns: new[] { "PsicologoUsuarioId", "FechaFinalizacionUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_AsignacionesEstudiantePsicologo_Vigente",
                table: "AsignacionesEstudiantePsicologo",
                column: "PerfilEstudianteId",
                unique: true,
                filter: "\"FechaFinalizacionUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ConsentimientosAtencion_SolicitudAtencionId",
                table: "ConsentimientosAtencion",
                column: "SolicitudAtencionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContactosEmergenciaExpediente_ExpedienteInicialId_Orden",
                table: "ContactosEmergenciaExpediente",
                columns: new[] { "ExpedienteInicialId", "Orden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventosAccesoClinico_SolicitudAtencionId_FechaUtc",
                table: "EventosAccesoClinico",
                columns: new[] { "SolicitudAtencionId", "FechaUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EventosAccesoClinico_UsuarioId_FechaUtc",
                table: "EventosAccesoClinico",
                columns: new[] { "UsuarioId", "FechaUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpedientesIniciales_SolicitudAtencionId",
                table: "ExpedientesIniciales",
                column: "SolicitudAtencionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesAtencion_UsuarioReferenteId",
                table: "SolicitudesAtencion",
                column: "UsuarioReferenteId");

            migrationBuilder.CreateIndex(
                name: "UX_SolicitudesAtencion_Activa",
                table: "SolicitudesAtencion",
                column: "PerfilEstudianteId",
                unique: true,
                filter: "\"Estado\" IN (0, 1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AsignacionesEstudiantePsicologo");

            migrationBuilder.DropTable(
                name: "ConsentimientosAtencion");

            migrationBuilder.DropTable(
                name: "ContactosEmergenciaExpediente");

            migrationBuilder.DropTable(
                name: "EventosAccesoClinico");

            migrationBuilder.DropTable(
                name: "ExpedientesIniciales");

            migrationBuilder.DropTable(
                name: "SolicitudesAtencion");
        }
    }
}
