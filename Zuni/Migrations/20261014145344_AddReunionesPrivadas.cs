using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zuni.Migrations
{
    /// <inheritdoc />
    public partial class AddReunionesPrivadas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EnlaceReunion",
                table: "Citas",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaEnlaceUtc",
                table: "Citas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Revision",
                table: "Citas",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "EnlacesReunionUsados",
                columns: table => new
                {
                    Huella = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CitaId = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnlacesReunionUsados", x => x.Huella);
                    table.ForeignKey(
                        name: "FK_EnlacesReunionUsados_Citas_CitaId",
                        column: x => x.CitaId,
                        principalTable: "Citas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Citas_EnlaceReunion",
                table: "Citas",
                column: "EnlaceReunion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnlacesReunionUsados_CitaId",
                table: "EnlacesReunionUsados",
                column: "CitaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnlacesReunionUsados");

            migrationBuilder.DropIndex(
                name: "IX_Citas_EnlaceReunion",
                table: "Citas");

            migrationBuilder.DropColumn(
                name: "EnlaceReunion",
                table: "Citas");

            migrationBuilder.DropColumn(
                name: "FechaEnlaceUtc",
                table: "Citas");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "Citas");
        }
    }
}
