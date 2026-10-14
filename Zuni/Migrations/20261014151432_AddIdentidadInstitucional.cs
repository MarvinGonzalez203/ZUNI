using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zuni.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentidadInstitucional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ColorPrincipal",
                table: "ConfiguracionInstitucion",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#cea2fd");

            migrationBuilder.AddColumn<string>(
                name: "Lema",
                table: "ConfiguracionInstitucion",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "Tu espacio educativo");

            migrationBuilder.AddColumn<byte[]>(
                name: "LogotipoPng",
                table: "ConfiguracionInstitucion",
                type: "bytea",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ColorPrincipal",
                table: "ConfiguracionInstitucion");

            migrationBuilder.DropColumn(
                name: "Lema",
                table: "ConfiguracionInstitucion");

            migrationBuilder.DropColumn(
                name: "LogotipoPng",
                table: "ConfiguracionInstitucion");
        }
    }
}
