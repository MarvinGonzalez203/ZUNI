using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zuni.Migrations
{
    /// <inheritdoc />
    public partial class AddClasificacionEdadAdministrativa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MayorEdadDeclarada",
                table: "AspNetUsers",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MayorEdadDeclarada",
                table: "AspNetUsers");
        }
    }
}
