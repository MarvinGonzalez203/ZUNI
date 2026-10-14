using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zuni.Migrations
{
    /// <inheritdoc />
    public partial class AddVerificacionMayoriaEdad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VerificacionesMayoriaEdad",
                columns: table => new
                {
                    PerfilEstudianteId = table.Column<Guid>(type: "uuid", nullable: false),
                    EdadDeclarada = table.Column<int>(type: "integer", nullable: false),
                    FechaDeclaracionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    DocumentoRevisado = table.Column<int>(type: "integer", nullable: true),
                    EdadVerificada = table.Column<int>(type: "integer", nullable: true),
                    ResponsableUsuarioId = table.Column<string>(type: "text", nullable: true),
                    FechaRevisionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Revision = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificacionesMayoriaEdad", x => x.PerfilEstudianteId);
                    table.CheckConstraint("CK_VerificacionEdad_Constancia", "(\"Estado\" = 1 AND \"EdadVerificada\" BETWEEN 18 AND 120 AND \"EdadVerificada\" IS NOT NULL AND \"ResponsableUsuarioId\" IS NOT NULL AND \"FechaRevisionUtc\" IS NOT NULL AND \"DocumentoRevisado\" IN (0, 1, 2) AND \"DocumentoRevisado\" IS NOT NULL) OR (\"Estado\" <> 1 AND \"EdadVerificada\" IS NULL)");
                    table.CheckConstraint("CK_VerificacionEdad_Declarada", "\"EdadDeclarada\" BETWEEN 18 AND 120");
                    table.CheckConstraint("CK_VerificacionEdad_Estado", "\"Estado\" IN (0, 1, 2)");
                    table.ForeignKey(
                        name: "FK_VerificacionesMayoriaEdad_AspNetUsers_ResponsableUsuarioId",
                        column: x => x.ResponsableUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VerificacionesMayoriaEdad_PerfilesEstudiante_PerfilEstudian~",
                        column: x => x.PerfilEstudianteId,
                        principalTable: "PerfilesEstudiante",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VerificacionesMayoriaEdad_ResponsableUsuarioId",
                table: "VerificacionesMayoriaEdad",
                column: "ResponsableUsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VerificacionesMayoriaEdad");
        }
    }
}
