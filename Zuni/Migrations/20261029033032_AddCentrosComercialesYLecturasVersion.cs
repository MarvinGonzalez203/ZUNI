using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zuni.Migrations
{
    /// <inheritdoc />
    public partial class AddCentrosComercialesYLecturasVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CentrosComerciales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CorreoContacto = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    MesesPlan = table.Column<int>(type: "integer", nullable: false),
                    Inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    Fin = table.Column<DateOnly>(type: "date", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    ImporteGtq = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    EsDemostracion = table.Column<bool>(type: "boolean", nullable: false),
                    NotaComercial = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ActualizadoPorId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ActualizadoUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Revision = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CentrosComerciales", x => x.Id);
                    table.CheckConstraint("CK_CentroComercial_Estado", "\"Estado\" IN (0,1,2)");
                    table.CheckConstraint("CK_CentroComercial_Fechas", "\"Fin\" >= \"Inicio\"");
                    table.CheckConstraint("CK_CentroComercial_Importe", "\"ImporteGtq\" >= 0");
                    table.CheckConstraint("CK_CentroComercial_Periodo", "\"MesesPlan\" IN (1,3,6,12)");
                    table.ForeignKey(
                        name: "FK_CentrosComerciales_AspNetUsers_ActualizadoPorId",
                        column: x => x.ActualizadoPorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LecturasVersion",
                columns: table => new
                {
                    UsuarioId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LecturasVersion", x => new { x.UsuarioId, x.Version });
                    table.ForeignKey(
                        name: "FK_LecturasVersion_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CentrosComerciales_ActualizadoPorId",
                table: "CentrosComerciales",
                column: "ActualizadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_CentrosComerciales_Fin",
                table: "CentrosComerciales",
                column: "Fin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CentrosComerciales");

            migrationBuilder.DropTable(
                name: "LecturasVersion");
        }
    }
}
