using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zuni.Migrations
{
    /// <inheritdoc />
    public partial class AddEvaluaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Evaluaciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Instrucciones = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: false),
                    EsDemostracion = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Evaluaciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AsignacionesEvaluacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EvaluacionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EstudianteId = table.Column<string>(type: "text", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    FechaAsignacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaInicioUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaFinalizacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Comprobante = table.Column<Guid>(type: "uuid", nullable: true),
                    Revision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsignacionesEvaluacion", x => x.Id);
                    table.UniqueConstraint("AK_AsignacionesEvaluacion_Id_EvaluacionId", x => new { x.Id, x.EvaluacionId });
                    table.CheckConstraint("CK_Asignacion_Estado", "\"Estado\" BETWEEN 0 AND 3");
                    table.CheckConstraint("CK_Asignacion_Finalizacion", "(\"Estado\" = 3 AND \"FechaFinalizacionUtc\" IS NOT NULL AND \"Comprobante\" IS NOT NULL AND \"FechaInicioUtc\" IS NOT NULL) OR (\"Estado\" <> 3 AND \"FechaFinalizacionUtc\" IS NULL AND \"Comprobante\" IS NULL)");
                    table.CheckConstraint("CK_Asignacion_Inicio", "\"Estado\" <> 2 OR \"FechaInicioUtc\" IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_AsignacionesEvaluacion_AspNetUsers_EstudianteId",
                        column: x => x.EstudianteId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AsignacionesEvaluacion_Evaluaciones_EvaluacionId",
                        column: x => x.EvaluacionId,
                        principalTable: "Evaluaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PreguntasEvaluacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EvaluacionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    Texto = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Obligatoria = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreguntasEvaluacion", x => x.Id);
                    table.UniqueConstraint("AK_PreguntasEvaluacion_Id_EvaluacionId", x => new { x.Id, x.EvaluacionId });
                    table.CheckConstraint("CK_Pregunta_Orden", "\"Orden\" > 0");
                    table.ForeignKey(
                        name: "FK_PreguntasEvaluacion_Evaluaciones_EvaluacionId",
                        column: x => x.EvaluacionId,
                        principalTable: "Evaluaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResultadosEvaluacion",
                columns: table => new
                {
                    AsignacionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Puntuacion = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    ObservacionesPublicables = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Publicado = table.Column<bool>(type: "boolean", nullable: false),
                    PublicadoUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublicadoPorId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResultadosEvaluacion", x => x.AsignacionId);
                    table.CheckConstraint("CK_Resultado_Publicacion", "NOT \"Publicado\" OR (\"PublicadoUtc\" IS NOT NULL AND \"PublicadoPorId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_ResultadosEvaluacion_AsignacionesEvaluacion_AsignacionId",
                        column: x => x.AsignacionId,
                        principalTable: "AsignacionesEvaluacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResultadosEvaluacion_AspNetUsers_PublicadoPorId",
                        column: x => x.PublicadoPorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RespuestasEvaluacion",
                columns: table => new
                {
                    AsignacionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreguntaId = table.Column<Guid>(type: "uuid", nullable: false),
                    EvaluacionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Valor = table.Column<int>(type: "integer", nullable: false),
                    ActualizadaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RespuestasEvaluacion", x => new { x.AsignacionId, x.PreguntaId });
                    table.CheckConstraint("CK_Respuesta_Valor", "\"Valor\" BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_RespuestasEvaluacion_AsignacionesEvaluacion_AsignacionId_Ev~",
                        columns: x => new { x.AsignacionId, x.EvaluacionId },
                        principalTable: "AsignacionesEvaluacion",
                        principalColumns: new[] { "Id", "EvaluacionId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RespuestasEvaluacion_PreguntasEvaluacion_PreguntaId_Evaluac~",
                        columns: x => new { x.PreguntaId, x.EvaluacionId },
                        principalTable: "PreguntasEvaluacion",
                        principalColumns: new[] { "Id", "EvaluacionId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesEvaluacion_Comprobante",
                table: "AsignacionesEvaluacion",
                column: "Comprobante",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesEvaluacion_EstudianteId_EvaluacionId",
                table: "AsignacionesEvaluacion",
                columns: new[] { "EstudianteId", "EvaluacionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesEvaluacion_EvaluacionId",
                table: "AsignacionesEvaluacion",
                column: "EvaluacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Evaluaciones_Codigo",
                table: "Evaluaciones",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PreguntasEvaluacion_EvaluacionId_Orden",
                table: "PreguntasEvaluacion",
                columns: new[] { "EvaluacionId", "Orden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RespuestasEvaluacion_AsignacionId_EvaluacionId",
                table: "RespuestasEvaluacion",
                columns: new[] { "AsignacionId", "EvaluacionId" });

            migrationBuilder.CreateIndex(
                name: "IX_RespuestasEvaluacion_PreguntaId_EvaluacionId",
                table: "RespuestasEvaluacion",
                columns: new[] { "PreguntaId", "EvaluacionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResultadosEvaluacion_PublicadoPorId",
                table: "ResultadosEvaluacion",
                column: "PublicadoPorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RespuestasEvaluacion");

            migrationBuilder.DropTable(
                name: "ResultadosEvaluacion");

            migrationBuilder.DropTable(
                name: "PreguntasEvaluacion");

            migrationBuilder.DropTable(
                name: "AsignacionesEvaluacion");

            migrationBuilder.DropTable(
                name: "Evaluaciones");
        }
    }
}
