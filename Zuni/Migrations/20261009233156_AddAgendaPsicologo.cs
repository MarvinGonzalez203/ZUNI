using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zuni.Migrations
{
    /// <inheritdoc />
    public partial class AddAgendaPsicologo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiasAgendaPsicologo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PsicologoId = table.Column<string>(type: "text", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Ocupado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiasAgendaPsicologo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiasAgendaPsicologo_AspNetUsers_PsicologoId",
                        column: x => x.PsicologoId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HorariosAgendaPsicologo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Inicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Fin = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    DuracionMinutos = table.Column<int>(type: "integer", nullable: false),
                    Modalidad = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorariosAgendaPsicologo", x => x.Id);
                    table.CheckConstraint("CK_Agenda_Duracion", "\"DuracionMinutos\" IN (15,30,45,50,60)");
                    table.CheckConstraint("CK_Agenda_Horas", "\"Inicio\" >= TIME '08:00' AND \"Fin\" <= TIME '18:00' AND \"Fin\" > \"Inicio\"");
                    table.CheckConstraint("CK_Agenda_Modalidad", "\"Modalidad\" IN ('Presencial','Virtual')");
                    table.ForeignKey(
                        name: "FK_HorariosAgendaPsicologo_DiasAgendaPsicologo_DiaId",
                        column: x => x.DiaId,
                        principalTable: "DiasAgendaPsicologo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiasAgendaPsicologo_PsicologoId_Fecha",
                table: "DiasAgendaPsicologo",
                columns: new[] { "PsicologoId", "Fecha" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HorariosAgendaPsicologo_DiaId",
                table: "HorariosAgendaPsicologo",
                column: "DiaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HorariosAgendaPsicologo");

            migrationBuilder.DropTable(
                name: "DiasAgendaPsicologo");
        }
    }
}
