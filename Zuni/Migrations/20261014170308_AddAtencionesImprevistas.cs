using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zuni.Migrations
{
    /// <inheritdoc />
    public partial class AddAtencionesImprevistas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "DisponibilidadPsicologoId",
                table: "Citas",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<bool>(
                name: "EsImprevista",
                table: "Citas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FinAproximado",
                table: "Citas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Citas_Disponibilidad",
                table: "Citas",
                sql: "\"EsImprevista\" OR \"DisponibilidadPsicologoId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Citas_Disponibilidad",
                table: "Citas");

            migrationBuilder.DropColumn(
                name: "EsImprevista",
                table: "Citas");

            migrationBuilder.DropColumn(
                name: "FinAproximado",
                table: "Citas");

            migrationBuilder.AlterColumn<Guid>(
                name: "DisponibilidadPsicologoId",
                table: "Citas",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
