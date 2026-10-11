using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zuni.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistroAtencionCita : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegistrosAtencionCita",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CitaId = table.Column<Guid>(type: "uuid", nullable: false),
                    SolicitudAtencionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Asistio = table.Column<bool>(type: "boolean", nullable: false),
                    ResultadoProfesionalPrivado = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ResenaRecomendacionesEstudiante = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ResultadoBreveEstudiante = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RecomiendaOtraCita = table.Column<bool>(type: "boolean", nullable: false),
                    IndicacionesProximaCita = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    FechaCierreUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosAtencionCita", x => x.Id);
                    table.CheckConstraint("CK_RegistroAtencion_Asistencia", "(\"Asistio\" AND length(trim(coalesce(\"ResultadoProfesionalPrivado\", ''))) > 0 AND length(trim(coalesce(\"ResenaRecomendacionesEstudiante\", ''))) > 0 AND length(trim(coalesce(\"ResultadoBreveEstudiante\", ''))) > 0) OR (NOT \"Asistio\" AND \"ResultadoProfesionalPrivado\" IS NULL AND \"ResultadoBreveEstudiante\" IS NULL AND NOT \"RecomiendaOtraCita\")");
                    table.CheckConstraint("CK_RegistroAtencion_Recomendacion", "(\"RecomiendaOtraCita\" AND \"Asistio\" AND length(trim(coalesce(\"IndicacionesProximaCita\", ''))) > 0) OR (NOT \"RecomiendaOtraCita\" AND \"IndicacionesProximaCita\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_RegistrosAtencionCita_Citas_CitaId",
                        column: x => x.CitaId,
                        principalTable: "Citas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegistrosAtencionCita_SolicitudesAtencion_SolicitudAtencion~",
                        column: x => x.SolicitudAtencionId,
                        principalTable: "SolicitudesAtencion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAtencionCita_CitaId",
                table: "RegistrosAtencionCita",
                column: "CitaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAtencionCita_SolicitudAtencionId",
                table: "RegistrosAtencionCita",
                column: "SolicitudAtencionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Mantener el bloqueo hasta DropTable evita inserciones entre la comprobación
            // y la eliminación. Ambas operaciones usan la transacción de migración de EF.
            migrationBuilder.Sql("""
                LOCK TABLE "RegistrosAtencionCita" IN ACCESS EXCLUSIVE MODE;
                DO $rollback$
                BEGIN
                    IF EXISTS (SELECT 1 FROM "RegistrosAtencionCita") THEN
                        RAISE EXCEPTION 'Reversión bloqueada para evitar pérdida de registros de atención: RegistrosAtencionCita contiene datos.';
                    END IF;
                END;
                $rollback$;
                """, suppressTransaction: false);

            migrationBuilder.DropTable(
                name: "RegistrosAtencionCita");
        }
    }
}
