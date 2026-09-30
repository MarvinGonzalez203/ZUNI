using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zuni.Migrations;

public partial class AddAsignacionesPruebas : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AsignacionesPrueba",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PruebaId = table.Column<Guid>(type: "uuid", nullable: false),
                EstudianteId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                AsignadaPorId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                Modalidad = table.Column<int>(type: "integer", nullable: false),
                FechaAsignacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Cancelada = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AsignacionesPrueba", x => x.Id);
                table.ForeignKey(
                    name: "FK_AsignacionesPrueba_AspNetUsers_AsignadaPorId",
                    column: x => x.AsignadaPorId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_AsignacionesPrueba_AspNetUsers_EstudianteId",
                    column: x => x.EstudianteId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_AsignacionesPrueba_Pruebas_PruebaId",
                    column: x => x.PruebaId,
                    principalTable: "Pruebas",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AsignacionesPrueba_EstudianteId_FechaAsignacionUtc",
            table: "AsignacionesPrueba",
            columns: new[] { "EstudianteId", "FechaAsignacionUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_AsignacionesPrueba_PruebaId_EstudianteId",
            table: "AsignacionesPrueba",
            columns: new[] { "PruebaId", "EstudianteId" },
            unique: true,
            filter: "\"Cancelada\" = FALSE");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AsignacionesPrueba");
    }
}
