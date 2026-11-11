using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zuni.Migrations
{
    /// <inheritdoc />
    public partial class PermitirAtencionProfesionalSinSolicitud : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "SolicitudAtencionId",
                table: "RegistrosAtencionCita",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // SET NOT NULL falla si ya hay registros independientes: no inventar IDs
            // ni borrar atenciones para volver a una versión anterior.
            migrationBuilder.AlterColumn<Guid>(
                name: "SolicitudAtencionId",
                table: "RegistrosAtencionCita",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
