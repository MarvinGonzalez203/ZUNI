using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zuni.Migrations
{
    /// <inheritdoc />
    public partial class AddBigFiveConsentimiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParticipacionesBigFive",
                columns: table => new
                {
                    AsignacionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PsicologoId = table.Column<string>(type: "text", nullable: false),
                    VersionInstrumento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PruebaLocal = table.Column<bool>(type: "boolean", nullable: false),
                    VersionConsentimiento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TextoConsentimiento = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    ConsentimientoUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevocadoUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MotivoConsulta = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: false),
                    Referencia = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Apertura = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: true),
                    Responsabilidad = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: true),
                    Extraversion = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: true),
                    Amabilidad = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: true),
                    Neuroticismo = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipacionesBigFive", x => x.AsignacionId);
                    table.CheckConstraint("CK_BigFive_Puntuaciones", "(\"Apertura\" IS NULL AND \"Responsabilidad\" IS NULL AND \"Extraversion\" IS NULL AND \"Amabilidad\" IS NULL AND \"Neuroticismo\" IS NULL) OR (\"Apertura\" IS NOT NULL AND \"Responsabilidad\" IS NOT NULL AND \"Extraversion\" IS NOT NULL AND \"Amabilidad\" IS NOT NULL AND \"Neuroticismo\" IS NOT NULL AND \"Apertura\" BETWEEN 1 AND 5 AND \"Responsabilidad\" BETWEEN 1 AND 5 AND \"Extraversion\" BETWEEN 1 AND 5 AND \"Amabilidad\" BETWEEN 1 AND 5 AND \"Neuroticismo\" BETWEEN 1 AND 5)");
                    table.ForeignKey(
                        name: "FK_ParticipacionesBigFive_AsignacionesEvaluacion_AsignacionId",
                        column: x => x.AsignacionId,
                        principalTable: "AsignacionesEvaluacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParticipacionesBigFive_AspNetUsers_PsicologoId",
                        column: x => x.PsicologoId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccesosBigFive",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AsignacionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PsicologoId = table.Column<string>(type: "text", nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccesosBigFive", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccesosBigFive_AspNetUsers_PsicologoId",
                        column: x => x.PsicologoId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccesosBigFive_ParticipacionesBigFive_AsignacionId",
                        column: x => x.AsignacionId,
                        principalTable: "ParticipacionesBigFive",
                        principalColumn: "AsignacionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccesosBigFive_AsignacionId_FechaUtc",
                table: "AccesosBigFive",
                columns: new[] { "AsignacionId", "FechaUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AccesosBigFive_PsicologoId",
                table: "AccesosBigFive",
                column: "PsicologoId");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipacionesBigFive_PsicologoId",
                table: "ParticipacionesBigFive",
                column: "PsicologoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccesosBigFive");

            migrationBuilder.DropTable(
                name: "ParticipacionesBigFive");
        }
    }
}
