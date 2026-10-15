using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zuni.Migrations
{
    /// <inheritdoc />
    public partial class AddColorPanelProveedor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ColorProveedor",
                table: "ConfiguracionTarifas",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#cea2fd");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ColorProveedor",
                table: "ConfiguracionTarifas");
        }
    }
}
