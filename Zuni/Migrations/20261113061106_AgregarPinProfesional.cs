using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zuni.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPinProfesional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PinesProfesionales",
                columns: table => new
                {
                    UsuarioId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    HashProtegido = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IntentosFallidos = table.Column<int>(type: "integer", nullable: false),
                    BloqueadoHastaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RecuperacionHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RecuperacionExpiraUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RecuperacionSello = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UltimoEnvioUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Revision = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PinesProfesionales", x => x.UsuarioId);
                    table.ForeignKey(
                        name: "FK_PinesProfesionales_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PinesProfesionales");
        }
    }
}
