using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zuni.Migrations
{
    /// <inheritdoc />
    public partial class AddAgendaYCitas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DisponibilidadesPsicologo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PsicologoUsuarioId = table.Column<string>(type: "text", nullable: false),
                    DiaSemana = table.Column<int>(type: "integer", nullable: false),
                    HoraInicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    HoraFin = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    DuracionMinutos = table.Column<int>(type: "integer", nullable: false),
                    Modalidad = table.Column<int>(type: "integer", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisponibilidadesPsicologo", x => x.Id);
                    table.CheckConstraint("CK_Disponibilidad_Dia", "\"DiaSemana\" BETWEEN 0 AND 6");
                    table.CheckConstraint("CK_Disponibilidad_Duracion", "\"DuracionMinutos\" BETWEEN 15 AND 120 AND \"DuracionMinutos\" % 5 = 0");
                    table.CheckConstraint("CK_Disponibilidad_Horas", "\"HoraFin\" > \"HoraInicio\"");
                    table.CheckConstraint("CK_Disponibilidad_Modalidad", "\"Modalidad\" IN (0, 1, 2)");
                    table.ForeignKey(
                        name: "FK_DisponibilidadesPsicologo_AspNetUsers_PsicologoUsuarioId",
                        column: x => x.PsicologoUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Citas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PerfilEstudianteId = table.Column<Guid>(type: "uuid", nullable: false),
                    PsicologoUsuarioId = table.Column<string>(type: "text", nullable: false),
                    DisponibilidadPsicologoId = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaHoraInicioUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaHoraFinUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Modalidad = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    FechaCreacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MotivoCancelacion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Citas", x => x.Id);
                    table.CheckConstraint("CK_Citas_Estado", "\"Estado\" IN (0, 1, 2, 3)");
                    table.CheckConstraint("CK_Citas_Fechas", "\"FechaHoraFinUtc\" > \"FechaHoraInicioUtc\"");
                    table.CheckConstraint("CK_Citas_Modalidad", "\"Modalidad\" IN (0, 1)");
                    table.ForeignKey(
                        name: "FK_Citas_AspNetUsers_PsicologoUsuarioId",
                        column: x => x.PsicologoUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Citas_DisponibilidadesPsicologo_DisponibilidadPsicologoId",
                        column: x => x.DisponibilidadPsicologoId,
                        principalTable: "DisponibilidadesPsicologo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Citas_PerfilesEstudiante_PerfilEstudianteId",
                        column: x => x.PerfilEstudianteId,
                        principalTable: "PerfilesEstudiante",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Citas_DisponibilidadPsicologoId",
                table: "Citas",
                column: "DisponibilidadPsicologoId");

            migrationBuilder.CreateIndex(
                name: "IX_Citas_PsicologoUsuarioId_Estado_FechaHoraInicioUtc",
                table: "Citas",
                columns: new[] { "PsicologoUsuarioId", "Estado", "FechaHoraInicioUtc" });

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

            migrationBuilder.CreateIndex(
                name: "IX_DisponibilidadesPsicologo_PsicologoUsuarioId_DiaSemana_Acti~",
                table: "DisponibilidadesPsicologo",
                columns: new[] { "PsicologoUsuarioId", "DiaSemana", "Activo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Citas");

            migrationBuilder.DropTable(
                name: "DisponibilidadesPsicologo");
        }
    }
}
