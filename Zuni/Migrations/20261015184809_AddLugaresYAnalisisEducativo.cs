using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zuni.Migrations
{
    /// <inheritdoc />
    public partial class AddLugaresYAnalisisEducativo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ContrasteRendimientoConfirmado",
                table: "VersionesInformesEducativos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IncluyeAnalisisRendimiento",
                table: "VersionesInformesEducativos",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RelatoRendimiento",
                table: "VersionesInformesEducativos",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ValoracionRendimiento",
                table: "VersionesInformesEducativos",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DireccionAtencion",
                table: "Citas",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContrasteRendimientoConfirmado",
                table: "VersionesInformesEducativos");

            migrationBuilder.DropColumn(
                name: "IncluyeAnalisisRendimiento",
                table: "VersionesInformesEducativos");

            migrationBuilder.DropColumn(
                name: "RelatoRendimiento",
                table: "VersionesInformesEducativos");

            migrationBuilder.DropColumn(
                name: "ValoracionRendimiento",
                table: "VersionesInformesEducativos");

            migrationBuilder.DropColumn(
                name: "DireccionAtencion",
                table: "Citas");
        }
    }
}
