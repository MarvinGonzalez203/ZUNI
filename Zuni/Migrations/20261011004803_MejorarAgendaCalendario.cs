using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zuni.Migrations
{
    /// <inheritdoc />
    public partial class MejorarAgendaCalendario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Disponibilidad_Duracion",
                table: "DisponibilidadesPsicologo");

            migrationBuilder.DropIndex(
                name: "UX_Citas_Estudiante_InicioReservado",
                table: "Citas");

            migrationBuilder.DropIndex(
                name: "UX_Citas_Psicologo_InicioReservado",
                table: "Citas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Citas_Estado",
                table: "Citas");

            migrationBuilder.AddColumn<Guid>(
                name: "ConfiguracionAgendaDiaId",
                table: "DisponibilidadesPsicologo",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ConfiguracionesAgendaDia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PsicologoUsuarioId = table.Column<string>(type: "text", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Bloqueado = table.Column<bool>(type: "boolean", nullable: false),
                    UsaBaseSemanal = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionesAgendaDia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfiguracionesAgendaDia_AspNetUsers_PsicologoUsuarioId",
                        column: x => x.PsicologoUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DisponibilidadesPsicologo_ConfiguracionAgendaDiaId",
                table: "DisponibilidadesPsicologo",
                column: "ConfiguracionAgendaDiaId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Disponibilidad_Duracion",
                table: "DisponibilidadesPsicologo",
                sql: "(\"DuracionMinutos\" = 0 AND \"ConfiguracionAgendaDiaId\" IS NOT NULL) OR (\"DuracionMinutos\" BETWEEN 15 AND 120 AND \"DuracionMinutos\" % 5 = 0)");

            migrationBuilder.CreateIndex(
                name: "UX_Citas_Estudiante_InicioReservado",
                table: "Citas",
                columns: new[] { "PerfilEstudianteId", "FechaHoraInicioUtc" },
                unique: true,
                filter: "\"Estado\" IN (0, 4, 5)");

            migrationBuilder.CreateIndex(
                name: "UX_Citas_Psicologo_InicioReservado",
                table: "Citas",
                columns: new[] { "PsicologoUsuarioId", "FechaHoraInicioUtc" },
                unique: true,
                filter: "\"Estado\" IN (0, 4, 5)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Citas_Estado",
                table: "Citas",
                sql: "\"Estado\" IN (0, 1, 2, 3, 4, 5, 6)");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesAgendaDia_PsicologoUsuarioId_Fecha",
                table: "ConfiguracionesAgendaDia",
                columns: new[] { "PsicologoUsuarioId", "Fecha" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DisponibilidadesPsicologo_ConfiguracionesAgendaDia_Configur~",
                table: "DisponibilidadesPsicologo",
                column: "ConfiguracionAgendaDiaId",
                principalTable: "ConfiguracionesAgendaDia",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Ejecutar en la transacción de EF (sin suppressTransaction / --no-transactions).
            // Los bloqueos se conservan hasta finalizar la reversión y evitan escrituras
            // entre las comprobaciones y la eliminación de los elementos nuevos.
            migrationBuilder.Sql("""
                LOCK TABLE "Citas", "DisponibilidadesPsicologo", "ConfiguracionesAgendaDia"
                    IN ACCESS EXCLUSIVE MODE;

                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM "ConfiguracionesAgendaDia")
                       OR EXISTS (SELECT 1 FROM "DisponibilidadesPsicologo"
                                  WHERE "ConfiguracionAgendaDiaId" IS NOT NULL)
                       OR EXISTS (SELECT 1 FROM "Citas" WHERE "Estado" IN (4, 5, 6)) THEN
                        RAISE EXCEPTION 'No se puede revertir MejorarAgendaCalendario: existen configuraciones diarias, disponibilidades vinculadas o citas en estados 4, 5 o 6. No se eliminaron ni transformaron estos datos.';
                    END IF;
                END;
                $$;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_DisponibilidadesPsicologo_ConfiguracionesAgendaDia_Configur~",
                table: "DisponibilidadesPsicologo");

            migrationBuilder.DropIndex(
                name: "IX_DisponibilidadesPsicologo_ConfiguracionAgendaDiaId",
                table: "DisponibilidadesPsicologo");

            migrationBuilder.DropIndex(
                name: "UX_Citas_Estudiante_InicioReservado",
                table: "Citas");

            migrationBuilder.DropIndex(
                name: "UX_Citas_Psicologo_InicioReservado",
                table: "Citas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Citas_Estado",
                table: "Citas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Disponibilidad_Duracion",
                table: "DisponibilidadesPsicologo");

            migrationBuilder.DropTable(
                name: "ConfiguracionesAgendaDia");

            migrationBuilder.DropColumn(
                name: "ConfiguracionAgendaDiaId",
                table: "DisponibilidadesPsicologo");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Disponibilidad_Duracion",
                table: "DisponibilidadesPsicologo",
                sql: "\"DuracionMinutos\" BETWEEN 15 AND 120 AND \"DuracionMinutos\" % 5 = 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Citas_Estado",
                table: "Citas",
                sql: "\"Estado\" IN (0, 1, 2, 3)");

            migrationBuilder.CreateIndex(
                name: "UX_Citas_Estudiante_InicioReservado",
                table: "Citas",
                columns: new[] { "PerfilEstudianteId", "FechaHoraInicioUtc" },
                unique: true,
                filter: "\"Estado\" = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Citas_Psicologo_InicioReservado",
                table: "Citas",
                columns: new[] { "PsicologoUsuarioId", "FechaHoraInicioUtc" },
                unique: true,
                filter: "\"Estado\" = 0");
        }
    }
}
