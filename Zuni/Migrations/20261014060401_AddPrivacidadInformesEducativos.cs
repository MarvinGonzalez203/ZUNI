using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zuni.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivacidadInformesEducativos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InformesEducativos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SolicitudAtencionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AutorUsuarioId = table.Column<string>(type: "text", nullable: false),
                    FechaCreacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InformesEducativos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InformesEducativos_AspNetUsers_AutorUsuarioId",
                        column: x => x.AutorUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InformesEducativos_SolicitudesAtencion_SolicitudAtencionId",
                        column: x => x.SolicitudAtencionId,
                        principalTable: "SolicitudesAtencion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PoliticasPrivacidad",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Institucion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Responsable = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Contacto = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    Finalidades = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    DatosYAccesos = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Conservacion = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ProcedimientoDerechos = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    PublicadaPorId = table.Column<string>(type: "text", nullable: false),
                    FechaPublicacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoliticasPrivacidad", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PoliticasPrivacidad_AspNetUsers_PublicadaPorId",
                        column: x => x.PublicadaPorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SolicitudesDerechosDatos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<string>(type: "text", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Detalle = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    FechaSolicitudUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    Respuesta = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ResponsableId = table.Column<string>(type: "text", nullable: true),
                    FechaRespuestaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Revision = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesDerechosDatos", x => x.Id);
                    table.CheckConstraint("CK_Derecho_Estado", "\"Estado\" IN (0,1)");
                    table.CheckConstraint("CK_Derecho_Tipo", "\"Tipo\" IN (0,1,2,3)");
                    table.ForeignKey(
                        name: "FK_SolicitudesDerechosDatos_AspNetUsers_ResponsableId",
                        column: x => x.ResponsableId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesDerechosDatos_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VersionesInformesEducativos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InformeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Numero = table.Column<int>(type: "integer", nullable: false),
                    Revision = table.Column<Guid>(type: "uuid", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    Objetivo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PeriodoInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodoFin = table.Column<DateOnly>(type: "date", nullable: false),
                    Fuentes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Observaciones = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: false),
                    Necesidades = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Recomendaciones = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: false),
                    ResponsablesAcciones = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    FechaSeguimiento = table.Column<DateOnly>(type: "date", nullable: false),
                    DestinatarioUsuarioId = table.Column<string>(type: "text", nullable: true),
                    Institucion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Estudiante = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Carne = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Psicologo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Destinatario = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FechaEmisionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SustituyeNumero = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VersionesInformesEducativos", x => x.Id);
                    table.CheckConstraint("CK_Informe_Estado", "\"Estado\" BETWEEN 0 AND 6");
                    table.CheckConstraint("CK_Informe_Periodo", "\"PeriodoFin\" >= \"PeriodoInicio\"");
                    table.ForeignKey(
                        name: "FK_VersionesInformesEducativos_AspNetUsers_DestinatarioUsuario~",
                        column: x => x.DestinatarioUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VersionesInformesEducativos_InformesEducativos_InformeId",
                        column: x => x.InformeId,
                        principalTable: "InformesEducativos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AceptacionesPrivacidad",
                columns: table => new
                {
                    PoliticaId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<string>(type: "text", nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AceptacionesPrivacidad", x => new { x.PoliticaId, x.UsuarioId });
                    table.ForeignKey(
                        name: "FK_AceptacionesPrivacidad_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AceptacionesPrivacidad_PoliticasPrivacidad_PoliticaId",
                        column: x => x.PoliticaId,
                        principalTable: "PoliticasPrivacidad",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AutorizacionesInformes",
                columns: table => new
                {
                    VersionInformeId = table.Column<Guid>(type: "uuid", nullable: false),
                    EstudianteUsuarioId = table.Column<string>(type: "text", nullable: false),
                    PoliticaPrivacidadId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionConsentimiento = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TextoConsentimiento = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: false),
                    Aceptada = table.Column<bool>(type: "boolean", nullable: true),
                    FechaSolicitudUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaDecisionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaRevocacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutorizacionesInformes", x => x.VersionInformeId);
                    table.ForeignKey(
                        name: "FK_AutorizacionesInformes_AspNetUsers_EstudianteUsuarioId",
                        column: x => x.EstudianteUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AutorizacionesInformes_PoliticasPrivacidad_PoliticaPrivacid~",
                        column: x => x.PoliticaPrivacidadId,
                        principalTable: "PoliticasPrivacidad",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AutorizacionesInformes_VersionesInformesEducativos_VersionI~",
                        column: x => x.VersionInformeId,
                        principalTable: "VersionesInformesEducativos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AceptacionesPrivacidad_UsuarioId",
                table: "AceptacionesPrivacidad",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_AutorizacionesInformes_EstudianteUsuarioId",
                table: "AutorizacionesInformes",
                column: "EstudianteUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_AutorizacionesInformes_PoliticaPrivacidadId",
                table: "AutorizacionesInformes",
                column: "PoliticaPrivacidadId");

            migrationBuilder.CreateIndex(
                name: "IX_InformesEducativos_AutorUsuarioId",
                table: "InformesEducativos",
                column: "AutorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_InformesEducativos_SolicitudAtencionId",
                table: "InformesEducativos",
                column: "SolicitudAtencionId");

            migrationBuilder.CreateIndex(
                name: "IX_PoliticasPrivacidad_PublicadaPorId",
                table: "PoliticasPrivacidad",
                column: "PublicadaPorId");

            migrationBuilder.CreateIndex(
                name: "IX_PoliticasPrivacidad_Version",
                table: "PoliticasPrivacidad",
                column: "Version",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesDerechosDatos_ResponsableId",
                table: "SolicitudesDerechosDatos",
                column: "ResponsableId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesDerechosDatos_UsuarioId_FechaSolicitudUtc",
                table: "SolicitudesDerechosDatos",
                columns: new[] { "UsuarioId", "FechaSolicitudUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VersionesInformesEducativos_DestinatarioUsuarioId",
                table: "VersionesInformesEducativos",
                column: "DestinatarioUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_VersionesInformesEducativos_InformeId_Numero",
                table: "VersionesInformesEducativos",
                columns: new[] { "InformeId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Informe_VersionEmitida",
                table: "VersionesInformesEducativos",
                column: "InformeId",
                unique: true,
                filter: "\"Estado\" = 3");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AceptacionesPrivacidad");

            migrationBuilder.DropTable(
                name: "AutorizacionesInformes");

            migrationBuilder.DropTable(
                name: "SolicitudesDerechosDatos");

            migrationBuilder.DropTable(
                name: "PoliticasPrivacidad");

            migrationBuilder.DropTable(
                name: "VersionesInformesEducativos");

            migrationBuilder.DropTable(
                name: "InformesEducativos");
        }
    }
}
