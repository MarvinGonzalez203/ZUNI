using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zuni.Migrations
{
    /// <inheritdoc />
    public partial class AddCitasAtencion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Citas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EstudianteId = table.Column<string>(type: "text", nullable: false),
                    PsicologoId = table.Column<string>(type: "text", nullable: false),
                    HorarioOriginalId = table.Column<Guid>(type: "uuid", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Inicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Fin = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    InicioUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DuracionVariable = table.Column<bool>(type: "boolean", nullable: false),
                    PruebaLocal = table.Column<bool>(type: "boolean", nullable: false),
                    Modalidad = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    SolicitudUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CitaAnteriorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Asistio = table.Column<bool>(type: "boolean", nullable: true),
                    AtencionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResultadoClinicoPrivado = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ResenaEstudiante = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: false),
                    ResultadoPublicable = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: false),
                    RecomiendaProximaCita = table.Column<bool>(type: "boolean", nullable: false),
                    IndicacionesProximaCita = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Citas", x => x.Id);
                    table.CheckConstraint("CK_Citas_Cierre", "(\"Estado\"=2 AND \"Asistio\" IS TRUE AND \"AtencionUtc\" IS NOT NULL) OR (\"Estado\"=5 AND \"Asistio\" IS FALSE AND \"AtencionUtc\" IS NOT NULL) OR (\"Estado\" NOT IN (2,5) AND \"Asistio\" IS NULL AND \"AtencionUtc\" IS NULL)");
                    table.CheckConstraint("CK_Citas_Estado", "\"Estado\" BETWEEN 0 AND 6 AND \"Revision\" > 0");
                    table.CheckConstraint("CK_Citas_Horas", "\"Fin\" > \"Inicio\" AND \"FinUtc\" > \"InicioUtc\"");
                    table.CheckConstraint("CK_Citas_Modalidad", "\"Modalidad\" IN ('Presencial','Virtual')");
                    table.ForeignKey(
                        name: "FK_Citas_AspNetUsers_EstudianteId",
                        column: x => x.EstudianteId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Citas_AspNetUsers_PsicologoId",
                        column: x => x.PsicologoId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Citas_Citas_CitaAnteriorId",
                        column: x => x.CitaAnteriorId,
                        principalTable: "Citas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccesosAtencion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CitaId = table.Column<Guid>(type: "uuid", nullable: false),
                    PsicologoId = table.Column<string>(type: "text", nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccesosAtencion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccesosAtencion_AspNetUsers_PsicologoId",
                        column: x => x.PsicologoId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccesosAtencion_Citas_CitaId",
                        column: x => x.CitaId,
                        principalTable: "Citas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventosCita",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CitaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<string>(type: "text", nullable: false),
                    Accion = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Motivo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosCita", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventosCita_AspNetUsers_ActorId",
                        column: x => x.ActorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventosCita_Citas_CitaId",
                        column: x => x.CitaId,
                        principalTable: "Citas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccesosAtencion_CitaId_FechaUtc",
                table: "AccesosAtencion",
                columns: new[] { "CitaId", "FechaUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AccesosAtencion_PsicologoId",
                table: "AccesosAtencion",
                column: "PsicologoId");

            migrationBuilder.CreateIndex(
                name: "IX_Citas_CitaAnteriorId",
                table: "Citas",
                column: "CitaAnteriorId");

            migrationBuilder.CreateIndex(
                name: "IX_Citas_EstudianteId_InicioUtc",
                table: "Citas",
                columns: new[] { "EstudianteId", "InicioUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Citas_PsicologoId_InicioUtc",
                table: "Citas",
                columns: new[] { "PsicologoId", "InicioUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Citas_PsicologoId_PruebaLocal_Fecha_Inicio",
                table: "Citas",
                columns: new[] { "PsicologoId", "PruebaLocal", "Fecha", "Inicio" },
                unique: true,
                filter: "\"Estado\" IN (0,1)");

            migrationBuilder.CreateIndex(
                name: "IX_EventosCita_ActorId",
                table: "EventosCita",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_EventosCita_CitaId_FechaUtc",
                table: "EventosCita",
                columns: new[] { "CitaId", "FechaUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccesosAtencion");

            migrationBuilder.DropTable(
                name: "EventosCita");

            migrationBuilder.DropTable(
                name: "Citas");
        }
    }
}
