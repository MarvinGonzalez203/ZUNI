using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zuni.Migrations
{
    /// <inheritdoc />
    public partial class AddConfiguracionInstitucion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfiguracionInstitucion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CorreoSoporte = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    PoliticaCorreo = table.Column<int>(type: "integer", nullable: false),
                    DominiosAutorizados = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Revision = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionInstitucion", x => x.Id);
                    table.CheckConstraint("CK_Institucion_PoliticaCorreo", "\"PoliticaCorreo\" IN (0, 1, 2)");
                    table.CheckConstraint("CK_Institucion_Unica", "\"Id\" = 1");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracionInstitucion");
        }
    }
}
