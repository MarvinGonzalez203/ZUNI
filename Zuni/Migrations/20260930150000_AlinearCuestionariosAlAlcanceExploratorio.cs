using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zuni.Migrations;

public partial class AlinearCuestionariosAlAlcanceExploratorio : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "PropositoExploratorio",
            table: "PreguntasPrueba",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.DropColumn(
            name: "Puntaje",
            table: "OpcionesPreguntaPrueba");

        // Existing questions have not yet gone through the professional review
        // expected for the exploratory scope, so return them to draft status.
        migrationBuilder.Sql("UPDATE \"PreguntasPrueba\" SET \"Activa\" = FALSE;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "Puntaje",
            table: "OpcionesPreguntaPrueba",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.DropColumn(
            name: "PropositoExploratorio",
            table: "PreguntasPrueba");
    }
}
