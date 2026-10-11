using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zuni.Migrations
{
    /// <inheritdoc />
    public partial class AddPruebasDirector : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PruebasDirector",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Instrucciones = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Fuente = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Idioma = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    Revision = table.Column<Guid>(type: "uuid", nullable: false),
                    CreadaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PruebasDirector", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AsignacionesPrueba",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PruebaId = table.Column<Guid>(type: "uuid", nullable: false),
                    PerfilEstudianteId = table.Column<Guid>(type: "uuid", nullable: false),
                    DirectorUsuarioId = table.Column<string>(type: "text", nullable: false),
                    AsignadaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsignacionesPrueba", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AsignacionesPrueba_AspNetUsers_DirectorUsuarioId",
                        column: x => x.DirectorUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AsignacionesPrueba_PerfilesEstudiante_PerfilEstudianteId",
                        column: x => x.PerfilEstudianteId,
                        principalTable: "PerfilesEstudiante",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AsignacionesPrueba_PruebasDirector_PruebaId",
                        column: x => x.PruebaId,
                        principalTable: "PruebasDirector",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DimensionesPrueba",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PruebaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DimensionesPrueba", x => x.Id);
                    table.UniqueConstraint("AK_DimensionesPrueba_PruebaId_Id", x => new { x.PruebaId, x.Id });
                    table.ForeignKey(
                        name: "FK_DimensionesPrueba_PruebasDirector_PruebaId",
                        column: x => x.PruebaId,
                        principalTable: "PruebasDirector",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PreguntasPrueba",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PruebaId = table.Column<Guid>(type: "uuid", nullable: false),
                    DimensionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    Texto = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreguntasPrueba", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreguntasPrueba_DimensionesPrueba_PruebaId_DimensionId",
                        columns: x => new { x.PruebaId, x.DimensionId },
                        principalTable: "DimensionesPrueba",
                        principalColumns: new[] { "PruebaId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PreguntasPrueba_PruebasDirector_PruebaId",
                        column: x => x.PruebaId,
                        principalTable: "PruebasDirector",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OpcionesPrueba",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PreguntaPruebaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    Texto = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Valor = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpcionesPrueba", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpcionesPrueba_PreguntasPrueba_PreguntaPruebaId",
                        column: x => x.PreguntaPruebaId,
                        principalTable: "PreguntasPrueba",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesPrueba_DirectorUsuarioId",
                table: "AsignacionesPrueba",
                column: "DirectorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesPrueba_PerfilEstudianteId",
                table: "AsignacionesPrueba",
                column: "PerfilEstudianteId");

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesPrueba_PruebaId_PerfilEstudianteId",
                table: "AsignacionesPrueba",
                columns: new[] { "PruebaId", "PerfilEstudianteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DimensionesPrueba_PruebaId_Nombre",
                table: "DimensionesPrueba",
                columns: new[] { "PruebaId", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpcionesPrueba_PreguntaPruebaId_Orden",
                table: "OpcionesPrueba",
                columns: new[] { "PreguntaPruebaId", "Orden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PreguntasPrueba_PruebaId_DimensionId",
                table: "PreguntasPrueba",
                columns: new[] { "PruebaId", "DimensionId" });

            migrationBuilder.CreateIndex(
                name: "IX_PreguntasPrueba_PruebaId_Orden",
                table: "PreguntasPrueba",
                columns: new[] { "PruebaId", "Orden" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AsignacionesPrueba");

            migrationBuilder.DropTable(
                name: "OpcionesPrueba");

            migrationBuilder.DropTable(
                name: "PreguntasPrueba");

            migrationBuilder.DropTable(
                name: "DimensionesPrueba");

            migrationBuilder.DropTable(
                name: "PruebasDirector");
        }
    }
}
