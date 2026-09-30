using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zuni.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditoriaUsuarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditoriaUsuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioAfectadoId = table.Column<string>(type: "text", nullable: false),
                    AdministradorId = table.Column<string>(type: "text", nullable: false),
                    Accion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DatosAnteriores = table.Column<string>(type: "jsonb", nullable: true),
                    DatosNuevos = table.Column<string>(type: "jsonb", nullable: true),
                    Motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FechaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditoriaUsuarios", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriaUsuarios_AdministradorId",
                table: "AuditoriaUsuarios",
                column: "AdministradorId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriaUsuarios_FechaUtc",
                table: "AuditoriaUsuarios",
                column: "FechaUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriaUsuarios_UsuarioAfectadoId",
                table: "AuditoriaUsuarios",
                column: "UsuarioAfectadoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditoriaUsuarios");
        }
    }
}
