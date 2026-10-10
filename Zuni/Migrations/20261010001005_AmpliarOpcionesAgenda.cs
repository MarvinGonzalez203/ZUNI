using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zuni.Migrations
{
    /// <inheritdoc />
    public partial class AmpliarOpcionesAgenda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Agenda_Duracion",
                table: "HorariosAgendaPsicologo");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Agenda_Modalidad",
                table: "HorariosAgendaPsicologo");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Agenda_Duracion",
                table: "HorariosAgendaPsicologo",
                sql: "\"DuracionMinutos\" IN (0,15,30,45,50,60)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Agenda_Modalidad",
                table: "HorariosAgendaPsicologo",
                sql: "\"Modalidad\" IN ('Presencial','Virtual','Ambas')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Agenda_Duracion",
                table: "HorariosAgendaPsicologo");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Agenda_Modalidad",
                table: "HorariosAgendaPsicologo");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Agenda_Duracion",
                table: "HorariosAgendaPsicologo",
                sql: "\"DuracionMinutos\" IN (15,30,45,50,60)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Agenda_Modalidad",
                table: "HorariosAgendaPsicologo",
                sql: "\"Modalidad\" IN ('Presencial','Virtual')");
        }
    }
}
