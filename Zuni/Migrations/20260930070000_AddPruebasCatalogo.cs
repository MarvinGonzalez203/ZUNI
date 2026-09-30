using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zuni.Migrations;

public partial class AddPruebasCatalogo : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Pruebas",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                Descripcion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                Activa = table.Column<bool>(type: "boolean", nullable: false),
                FechaCreacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                FechaActualizacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Pruebas", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Pruebas_Activa",
            table: "Pruebas",
            column: "Activa");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Pruebas");
    }
}
