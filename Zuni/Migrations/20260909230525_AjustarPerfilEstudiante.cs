using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zuni.Migrations
{
    /// <inheritdoc />
    public partial class AjustarPerfilEstudiante : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Carrera",
                table: "PerfilesEstudiante",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AddColumn<string>(
                name: "CicloAcademico",
                table: "PerfilesEstudiante",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RelacionContactoEmergencia",
                table: "PerfilesEstudiante",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CicloAcademico",
                table: "PerfilesEstudiante");

            migrationBuilder.DropColumn(
                name: "RelacionContactoEmergencia",
                table: "PerfilesEstudiante");

            migrationBuilder.Sql(
                @"UPDATE ""PerfilesEstudiante""
          SET ""Carrera"" = ''
          WHERE ""Carrera"" IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "Carrera",
                table: "PerfilesEstudiante",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldNullable: true);
        }
    }
}
