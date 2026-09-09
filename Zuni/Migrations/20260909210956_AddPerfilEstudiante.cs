using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zuni.Migrations
{
    /// <inheritdoc />
    public partial class AddPerfilEstudiante : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PerfilesEstudiante",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<string>(type: "text", nullable: false),
                    Carne = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Carrera = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Semestre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Telefono = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    NombreContactoEmergencia = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    TelefonoContactoEmergencia = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    FechaCreacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfilesEstudiante", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerfilesEstudiante_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PerfilesEstudiante_Carne",
                table: "PerfilesEstudiante",
                column: "Carne",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PerfilesEstudiante_UsuarioId",
                table: "PerfilesEstudiante",
                column: "UsuarioId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PerfilesEstudiante");
        }
    }
}
