using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zuni.Migrations
{
    /// <inheritdoc />
    public partial class AddBigFiveApoyo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParticipacionesBigFive",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SolicitudAtencionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PerfilEstudianteId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionInstrumento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    VersionConsentimiento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TextoConsentimiento = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    FechaConsentimientoUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaFinalizacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaRetiroConsentimientoUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Apertura = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: true),
                    Responsabilidad = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: true),
                    Extraversion = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: true),
                    Amabilidad = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: true),
                    Neuroticismo = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: true),
                    Revision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipacionesBigFive", x => x.Id);
                    table.CheckConstraint("CK_BigFive_Consentimiento", "\"VersionInstrumento\" = 'IPIP50-ES-ZUNI-1' AND \"VersionConsentimiento\" = 'ZUNI-BIGFIVE-1' AND length(btrim(\"TextoConsentimiento\")) > 0 AND \"Revision\" >= 0");
                    table.CheckConstraint("CK_BigFive_Fechas", "(\"FechaFinalizacionUtc\" IS NULL OR \"FechaFinalizacionUtc\" >= \"FechaConsentimientoUtc\") AND (\"FechaRetiroConsentimientoUtc\" IS NULL OR (\"FechaRetiroConsentimientoUtc\" >= \"FechaConsentimientoUtc\" AND (\"FechaFinalizacionUtc\" IS NULL OR \"FechaRetiroConsentimientoUtc\" >= \"FechaFinalizacionUtc\")))");
                    table.CheckConstraint("CK_BigFive_Finalizacion", "(\"FechaFinalizacionUtc\" IS NULL AND \"Apertura\" IS NULL AND \"Responsabilidad\" IS NULL AND \"Extraversion\" IS NULL AND \"Amabilidad\" IS NULL AND \"Neuroticismo\" IS NULL) OR (\"FechaFinalizacionUtc\" IS NOT NULL AND \"Apertura\" IS NOT NULL AND \"Apertura\" BETWEEN 1 AND 5 AND \"Responsabilidad\" IS NOT NULL AND \"Responsabilidad\" BETWEEN 1 AND 5 AND \"Extraversion\" IS NOT NULL AND \"Extraversion\" BETWEEN 1 AND 5 AND \"Amabilidad\" IS NOT NULL AND \"Amabilidad\" BETWEEN 1 AND 5 AND \"Neuroticismo\" IS NOT NULL AND \"Neuroticismo\" BETWEEN 1 AND 5)");
                    table.ForeignKey(
                        name: "FK_ParticipacionesBigFive_PerfilesEstudiante_PerfilEstudianteId",
                        column: x => x.PerfilEstudianteId,
                        principalTable: "PerfilesEstudiante",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParticipacionesBigFive_SolicitudesAtencion_SolicitudAtencio~",
                        column: x => x.SolicitudAtencionId,
                        principalTable: "SolicitudesAtencion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RespuestasBigFive",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipacionBigFiveId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    Valor = table.Column<int>(type: "integer", nullable: false),
                    FechaActualizacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RespuestasBigFive", x => x.Id);
                    table.CheckConstraint("CK_RespuestaBigFive_Item", "\"ItemId\" IN ('34ec9b83-e986-7b45-1212-a008b6464ac9', 'bc0f20a0-dcd5-4e12-f803-339b7e6e41f9', 'a7794b8c-b052-1596-eaeb-8502f29aa731', '8eae8870-b3f9-dd96-af90-1da7d58fbfb8', '6c32dd7d-b7e7-910e-7e79-f5c6f946f427', '85c36ac5-3f2b-e534-c1f0-5310794d032e', 'd24b91f5-65d9-49ff-84db-8602cb886d71', '672abc81-ee21-5adb-6083-01842a329dfa', 'dbff23a7-7441-3aec-0005-8594fb64b53c', '02fd1819-e710-5f78-fbfe-45e5eadf4b67', 'd43a3a0d-ccd3-5732-7bd5-7f92e5791beb', 'c08c2a34-2e02-24fc-2fa2-56c1eef3706e', '7483763b-a2fd-b70e-43c0-198a63e9125a', 'dc62784e-0b3c-5c5a-4e1e-e182276739a4', '1171a3bf-a7a4-abf8-b03f-5cd5e0594760', '6984d96b-503d-ada6-3ca5-c3c8f484962a', '0f8dfd92-9cc9-708c-806a-689c015c01a9', 'c553730a-0097-3365-aaa0-a972a774ea34', '4f937929-1e3a-d7fc-5767-f3114f4313dd', '11788183-ad65-2a99-834e-92c5be504237', 'afaf22c5-fb77-39a2-0701-786892bc192e', '13c41242-cad9-2539-f7e0-3c0fb857dd82', 'f2ec3f62-1e27-210c-a098-249a9ae255ad', 'fedc7ceb-7674-6ff8-a6e1-de94c5a48a7e', '15d0e94b-19a0-e471-d4c1-f466b32b5e8b', '75c97af0-e69a-d3a5-4665-d6f1d964601c', '2db6901a-a12c-9e34-bf5a-080e2620e041', 'f59fba24-ed04-0f4b-c2c7-01dc8ffd3230', 'fbcf06a9-dda5-07e5-f4a4-735c8df528e1', '86772d4e-210a-3a2c-eac4-2e367ab7a325', '3cc223e9-34c8-775c-993e-52007f1307d7', '4b758014-7f3b-3890-c104-9548f4502269', 'a8ec5bf8-ac26-312f-3fc4-cb6495c76cf9', 'd4fab853-3245-3ae9-6450-18c43b57bde2', 'd2b0b5c2-3134-2b84-e8e5-e533ea2305a9', 'e0f4fcc8-169d-ec18-6cb0-edede94551d0', 'ab00f0c4-e6ab-afc4-9e93-c5cf1b2ce3df', 'c40dd84b-9da3-abfa-fd1f-513bef322abe', '6ac8f479-263c-48e4-7d08-fb7043337bf3', '52654a58-5b8d-13d3-2b0d-0bf646c5e185', 'b5169765-88cd-d032-6101-f452f8ae17f1', '25b5a84f-6f37-91f2-2e71-37408a77cbe3', 'd9570eb2-facd-24ee-2e8d-beb1f4689b04', '99dae9f9-a45e-e4bb-088a-3649b235ad03', '7845b957-70de-3053-2580-f8aef412ea2b', '0763e5c4-6d3a-0af7-ebd3-9ce09893c65d', '22e9a86f-7cb7-5a81-53ed-f6445fb921d5', 'cb57fafb-1a23-1ce0-3a99-d76d230899e3', '66025095-c39b-77f9-bd66-3ec34a12362e', 'cdbe8f89-8ed4-6a17-ace1-5b9d0d76c8ed')");
                    table.CheckConstraint("CK_RespuestaBigFive_Valor", "\"Valor\" BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_RespuestasBigFive_ParticipacionesBigFive_ParticipacionBigFi~",
                        column: x => x.ParticipacionBigFiveId,
                        principalTable: "ParticipacionesBigFive",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParticipacionesBigFive_PerfilEstudianteId_VersionInstrumento",
                table: "ParticipacionesBigFive",
                columns: new[] { "PerfilEstudianteId", "VersionInstrumento" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParticipacionesBigFive_SolicitudAtencionId",
                table: "ParticipacionesBigFive",
                column: "SolicitudAtencionId");

            migrationBuilder.CreateIndex(
                name: "IX_RespuestasBigFive_ParticipacionBigFiveId_ItemId",
                table: "RespuestasBigFive",
                columns: new[] { "ParticipacionBigFiveId", "ItemId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RespuestasBigFive");

            migrationBuilder.DropTable(
                name: "ParticipacionesBigFive");
        }
    }
}
