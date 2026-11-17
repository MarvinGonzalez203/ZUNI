using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zuni.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDobleFactorYAusencias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Citas_Estado",
                table: "Citas");

            migrationBuilder.AddColumn<Guid>(
                name: "SustituyeCitaId",
                table: "Citas",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DobleFactor",
                columns: table => new
                {
                    UsuarioId = table.Column<string>(type: "text", nullable: false),
                    SecretoProtegido = table.Column<string>(type: "text", nullable: true),
                    PendienteProtegido = table.Column<string>(type: "text", nullable: true),
                    PendienteHastaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CodigosHash = table.Column<string>(type: "text", nullable: false),
                    UltimoPaso = table.Column<long>(type: "bigint", nullable: false),
                    Fallos = table.Column<int>(type: "integer", nullable: false),
                    BloqueadoHastaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetoHash = table.Column<string>(type: "text", nullable: true),
                    RetoStamp = table.Column<string>(type: "text", nullable: true),
                    RetoHastaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Recordar = table.Column<bool>(type: "boolean", nullable: false),
                    Retorno = table.Column<string>(type: "text", nullable: true),
                    Revision = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DobleFactor", x => x.UsuarioId);
                    table.ForeignKey(
                        name: "FK_DobleFactor_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Citas_SustituyeCitaId",
                table: "Citas",
                column: "SustituyeCitaId",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Citas_Estado",
                table: "Citas",
                sql: "\"Estado\" IN (0, 1, 2, 3, 4, 5, 6, 7)");

            migrationBuilder.CreateIndex(
                name: "IX_DobleFactor_RetoHash",
                table: "DobleFactor",
                column: "RetoHash",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Citas_Citas_SustituyeCitaId",
                table: "Citas",
                column: "SustituyeCitaId",
                principalTable: "Citas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No dejar cuentas con 2FA habilitado sin su secreto ni borrar el historial nuevo.
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM "AspNetUsers" WHERE "TwoFactorEnabled")
                       OR EXISTS (SELECT 1 FROM "Citas" WHERE "Estado" = 7 OR "SustituyeCitaId" IS NOT NULL) THEN
                        RAISE EXCEPTION 'No se puede revertir 1.5.0 después de usar 2FA o ausencias. Restaura un respaldo compatible con el código.';
                    END IF;
                END $$;
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_Citas_Citas_SustituyeCitaId",
                table: "Citas");

            migrationBuilder.DropTable(
                name: "DobleFactor");

            migrationBuilder.DropIndex(
                name: "IX_Citas_SustituyeCitaId",
                table: "Citas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Citas_Estado",
                table: "Citas");

            migrationBuilder.DropColumn(
                name: "SustituyeCitaId",
                table: "Citas");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Citas_Estado",
                table: "Citas",
                sql: "\"Estado\" IN (0, 1, 2, 3, 4, 5, 6)");
        }
    }
}
