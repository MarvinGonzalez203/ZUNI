using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zuni.Migrations
{
    /// <inheritdoc />
    public partial class AddProgramacionNotificacionesCitas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Citas_Disponibilidad",
                table: "Citas");

            migrationBuilder.AddColumn<bool>(
                name: "ProgramadaDirectamente",
                table: "Citas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ProgramadorRol",
                table: "Citas",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProgramadorUsuarioId",
                table: "Citas",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NotificacionesCita",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<string>(type: "text", nullable: false),
                    CitaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Texto = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Leida = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificacionesCita", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificacionesCita_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotificacionesCita_Citas_CitaId",
                        column: x => x.CitaId,
                        principalTable: "Citas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Citas_ProgramadorUsuarioId",
                table: "Citas",
                column: "ProgramadorUsuarioId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Citas_Disponibilidad",
                table: "Citas",
                sql: "\"EsImprevista\" OR \"ProgramadaDirectamente\" OR \"DisponibilidadPsicologoId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NotificacionesCita_CitaId",
                table: "NotificacionesCita",
                column: "CitaId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificacionesCita_UsuarioId_Leida_FechaUtc",
                table: "NotificacionesCita",
                columns: new[] { "UsuarioId", "Leida", "FechaUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_Citas_AspNetUsers_ProgramadorUsuarioId",
                table: "Citas",
                column: "ProgramadorUsuarioId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Citas_AspNetUsers_ProgramadorUsuarioId",
                table: "Citas");

            migrationBuilder.DropTable(
                name: "NotificacionesCita");

            migrationBuilder.DropIndex(
                name: "IX_Citas_ProgramadorUsuarioId",
                table: "Citas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Citas_Disponibilidad",
                table: "Citas");

            migrationBuilder.DropColumn(
                name: "ProgramadaDirectamente",
                table: "Citas");

            migrationBuilder.DropColumn(
                name: "ProgramadorRol",
                table: "Citas");

            migrationBuilder.DropColumn(
                name: "ProgramadorUsuarioId",
                table: "Citas");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Citas_Disponibilidad",
                table: "Citas",
                sql: "\"EsImprevista\" OR \"DisponibilidadPsicologoId\" IS NOT NULL");
        }
    }
}
