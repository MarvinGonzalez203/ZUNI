using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zuni.Migrations;

public partial class AddContenidoPruebas : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DimensionesPrueba",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PruebaId = table.Column<Guid>(type: "uuid", nullable: false),
                Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                Orden = table.Column<int>(type: "integer", nullable: false),
                Activa = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DimensionesPrueba", x => x.Id);
                table.ForeignKey(
                    name: "FK_DimensionesPrueba_Pruebas_PruebaId",
                    column: x => x.PruebaId,
                    principalTable: "Pruebas",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "PreguntasPrueba",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                DimensionPruebaId = table.Column<Guid>(type: "uuid", nullable: false),
                Texto = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                Orden = table.Column<int>(type: "integer", nullable: false),
                Activa = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PreguntasPrueba", x => x.Id);
                table.ForeignKey(
                    name: "FK_PreguntasPrueba_DimensionesPrueba_DimensionPruebaId",
                    column: x => x.DimensionPruebaId,
                    principalTable: "DimensionesPrueba",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "OpcionesPreguntaPrueba",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PreguntaPruebaId = table.Column<Guid>(type: "uuid", nullable: false),
                Texto = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Puntaje = table.Column<int>(type: "integer", nullable: false),
                Orden = table.Column<int>(type: "integer", nullable: false),
                Activa = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OpcionesPreguntaPrueba", x => x.Id);
                table.ForeignKey(
                    name: "FK_OpcionesPreguntaPrueba_PreguntasPrueba_PreguntaPruebaId",
                    column: x => x.PreguntaPruebaId,
                    principalTable: "PreguntasPrueba",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_DimensionesPrueba_PruebaId_Orden",
            table: "DimensionesPrueba",
            columns: new[] { "PruebaId", "Orden" });

        migrationBuilder.CreateIndex(
            name: "IX_PreguntasPrueba_DimensionPruebaId_Orden",
            table: "PreguntasPrueba",
            columns: new[] { "DimensionPruebaId", "Orden" });

        migrationBuilder.CreateIndex(
            name: "IX_OpcionesPreguntaPrueba_PreguntaPruebaId_Orden",
            table: "OpcionesPreguntaPrueba",
            columns: new[] { "PreguntaPruebaId", "Orden" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "OpcionesPreguntaPrueba");
        migrationBuilder.DropTable(name: "PreguntasPrueba");
        migrationBuilder.DropTable(name: "DimensionesPrueba");
    }
}
