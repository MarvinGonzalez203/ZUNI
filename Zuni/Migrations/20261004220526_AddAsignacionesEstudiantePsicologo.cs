using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zuni.Migrations
{
    /// <inheritdoc />
    public partial class AddAsignacionesEstudiantePsicologo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AsignacionesEstudiantePsicologo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PerfilEstudianteId = table.Column<Guid>(type: "uuid", nullable: false),
                    PsicologoUsuarioId = table.Column<string>(type: "text", nullable: false),
                    FechaAsignacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaFinalizacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsignacionesEstudiantePsicologo", x => x.Id);
                    table.CheckConstraint("CK_AsignacionesEstudiantePsicologo_Fechas", "\"FechaFinalizacionUtc\" IS NULL OR \"FechaFinalizacionUtc\" >= \"FechaAsignacionUtc\"");
                    table.ForeignKey(
                        name: "FK_AsignacionesEstudiantePsicologo_AspNetUsers_PsicologoUsuari~",
                        column: x => x.PsicologoUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AsignacionesEstudiantePsicologo_PerfilesEstudiante_PerfilEs~",
                        column: x => x.PerfilEstudianteId,
                        principalTable: "PerfilesEstudiante",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesEstudiantePsicologo_PsicologoUsuarioId_FechaFin~",
                table: "AsignacionesEstudiantePsicologo",
                columns: new[] { "PsicologoUsuarioId", "FechaFinalizacionUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_AsignacionesEstudiantePsicologo_PerfilEstudiante_Vigente",
                table: "AsignacionesEstudiantePsicologo",
                column: "PerfilEstudianteId",
                unique: true,
                filter: "\"FechaFinalizacionUtc\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AsignacionesEstudiantePsicologo");
        }
    }
}
