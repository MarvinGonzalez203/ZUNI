using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zuni.Migrations
{
    /// <inheritdoc />
    public partial class AddAlmuerzoAutoresAvisos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AutorRol",
                table: "NotificacionesCita",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AutorUsuarioId",
                table: "NotificacionesCita",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tipo",
                table: "NotificacionesCita",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ConfiguracionesAlmuerzo",
                columns: table => new
                {
                    PsicologoUsuarioId = table.Column<string>(type: "text", nullable: false),
                    Habilitado = table.Column<bool>(type: "boolean", nullable: false),
                    Inicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Fin = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Revision = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionesAlmuerzo", x => x.PsicologoUsuarioId);
                    table.CheckConstraint("CK_Almuerzo_Horas", "\"Inicio\" >= TIME '08:00' AND \"Fin\" <= TIME '18:00' AND \"Fin\" > \"Inicio\"");
                    table.ForeignKey(
                        name: "FK_ConfiguracionesAlmuerzo_AspNetUsers_PsicologoUsuarioId",
                        column: x => x.PsicologoUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificacionesCita_AutorUsuarioId",
                table: "NotificacionesCita",
                column: "AutorUsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_NotificacionesCita_AspNetUsers_AutorUsuarioId",
                table: "NotificacionesCita",
                column: "AutorUsuarioId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotificacionesCita_AspNetUsers_AutorUsuarioId",
                table: "NotificacionesCita");

            migrationBuilder.DropTable(
                name: "ConfiguracionesAlmuerzo");

            migrationBuilder.DropIndex(
                name: "IX_NotificacionesCita_AutorUsuarioId",
                table: "NotificacionesCita");

            migrationBuilder.DropColumn(
                name: "AutorRol",
                table: "NotificacionesCita");

            migrationBuilder.DropColumn(
                name: "AutorUsuarioId",
                table: "NotificacionesCita");

            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "NotificacionesCita");
        }
    }
}
