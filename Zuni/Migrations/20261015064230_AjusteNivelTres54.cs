using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Zuni.Migrations
{
    /// <inheritdoc />
    public partial class AjusteNivelTres54 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Never change a questionnaire already assigned, active, or edited by an institution.
            migrationBuilder.Sql("""
                LOCK TABLE "PruebasDirector", "AsignacionesPrueba", "PreguntasPrueba", "OpcionesPrueba" IN SHARE ROW EXCLUSIVE MODE;
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM "AsignacionesPrueba" WHERE "PruebaId" = 'eeeeeeee-0001-0003-0000-000000000000')
                       OR EXISTS (SELECT 1 FROM "PruebasDirector" WHERE "Id" = 'eeeeeeee-0001-0003-0000-000000000000'
                           AND ("Activa" OR "Revision" <> 'eeeeeeee-0005-0003-0000-000000000000')) THEN
                        RAISE EXCEPTION 'Level 3 has assignments or institutional edits. Preserve its content and create a separate 54-question version before applying this migration.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DeleteData(
                table: "OpcionesPrueba",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0004-0003-0000-000000000421"));

            migrationBuilder.DeleteData(
                table: "OpcionesPrueba",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0004-0003-0000-000000000422"));

            migrationBuilder.DeleteData(
                table: "OpcionesPrueba",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0004-0003-0000-000000000423"));

            migrationBuilder.DeleteData(
                table: "OpcionesPrueba",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0004-0003-0000-000000000424"));

            migrationBuilder.DeleteData(
                table: "OpcionesPrueba",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0004-0003-0000-000000000425"));

            migrationBuilder.DeleteData(
                table: "OpcionesPrueba",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0004-0003-0000-000000000426"));

            migrationBuilder.DeleteData(
                table: "OpcionesPrueba",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0004-0003-0000-000000000427"));

            migrationBuilder.DeleteData(
                table: "OpcionesPrueba",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0004-0003-0000-000000000428"));

            migrationBuilder.DeleteData(
                table: "OpcionesPrueba",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0004-0003-0000-000000000429"));

            migrationBuilder.DeleteData(
                table: "OpcionesPrueba",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0004-0003-0000-000000000430"));

            migrationBuilder.DeleteData(
                table: "PreguntasPrueba",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0003-0003-0000-000000000043"));

            migrationBuilder.UpdateData(
                table: "PreguntasPrueba",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0003-0003-0000-000000000044"),
                column: "Orden",
                value: 43);

            migrationBuilder.UpdateData(
                table: "PreguntasPrueba",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0003-0003-0000-000000000045"),
                column: "Orden",
                value: 44);

            migrationBuilder.UpdateData(
                table: "PreguntasPrueba",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0003-0003-0000-000000000046"),
                column: "Orden",
                value: 45);

            migrationBuilder.UpdateData(
                table: "PreguntasPrueba",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0003-0003-0000-000000000047"),
                column: "Orden",
                value: 46);

            migrationBuilder.UpdateData(
                table: "PreguntasPrueba",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0003-0003-0000-000000000048"),
                column: "Orden",
                value: 47);

            migrationBuilder.UpdateData(
                table: "PreguntasPrueba",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0003-0003-0000-000000000049"),
                column: "Orden",
                value: 48);

            migrationBuilder.UpdateData(
                table: "PreguntasPrueba",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0003-0003-0000-000000000050"),
                column: "Orden",
                value: 49);

            migrationBuilder.UpdateData(
                table: "PreguntasPrueba",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0003-0003-0000-000000000051"),
                column: "Orden",
                value: 50);

            migrationBuilder.UpdateData(
                table: "PreguntasPrueba",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0003-0003-0000-000000000052"),
                column: "Orden",
                value: 51);

            migrationBuilder.UpdateData(
                table: "PreguntasPrueba",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0003-0003-0000-000000000053"),
                column: "Orden",
                value: 52);

            migrationBuilder.UpdateData(
                table: "PreguntasPrueba",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0003-0003-0000-000000000054"),
                column: "Orden",
                value: 53);

            migrationBuilder.UpdateData(
                table: "PreguntasPrueba",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0003-0003-0000-000000000055"),
                column: "Orden",
                value: 54);

            migrationBuilder.UpdateData(
                table: "PruebasDirector",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0001-0003-0000-000000000000"),
                columns: new[] { "Descripcion", "Revision" },
                values: new object[] { "Nivel 3: 54 afirmaciones para explorar tendencias declaradas. Preguntas propuestas de ZUNI para revisión profesional; instrumento sin validación psicométrica ni baremos. No produce diagnósticos, banderas automáticas de riesgo ni decisiones académicas.", new Guid("eeeeeeee-0005-0003-0000-000000000001") });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Never change a questionnaire already assigned, active, or edited by an institution.
            migrationBuilder.Sql("""
                LOCK TABLE "PruebasDirector", "AsignacionesPrueba", "PreguntasPrueba", "OpcionesPrueba" IN SHARE ROW EXCLUSIVE MODE;
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM "AsignacionesPrueba" WHERE "PruebaId" = 'eeeeeeee-0001-0003-0000-000000000000')
                       OR EXISTS (SELECT 1 FROM "PruebasDirector" WHERE "Id" = 'eeeeeeee-0001-0003-0000-000000000000'
                           AND ("Activa" OR "Revision" <> 'eeeeeeee-0005-0003-0000-000000000001')) THEN
                        RAISE EXCEPTION 'Level 3 has assignments or institutional edits. Preserve its content and create a separate 54-question version before applying this migration.';
                    END IF;
                END $$;
                """);

            migrationBuilder.UpdateData(
                table: "PreguntasPrueba",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0003-0003-0000-000000000044"),
                column: "Orden",
                value: 44);

            migrationBuilder.UpdateData(
                table: "PreguntasPrueba",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0003-0003-0000-000000000045"),
                column: "Orden",
                value: 45);

            migrationBuilder.UpdateData(
                table: "PreguntasPrueba",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0003-0003-0000-000000000046"),
                column: "Orden",
                value: 46);

            migrationBuilder.UpdateData(
                table: "PreguntasPrueba",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0003-0003-0000-000000000047"),
                column: "Orden",
                value: 47);

            migrationBuilder.UpdateData(
                table: "PreguntasPrueba",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0003-0003-0000-000000000048"),
                column: "Orden",
                value: 48);

            migrationBuilder.UpdateData(
                table: "PreguntasPrueba",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0003-0003-0000-000000000049"),
                column: "Orden",
                value: 49);

            migrationBuilder.UpdateData(
                table: "PreguntasPrueba",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0003-0003-0000-000000000050"),
                column: "Orden",
                value: 50);

            migrationBuilder.UpdateData(
                table: "PreguntasPrueba",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0003-0003-0000-000000000051"),
                column: "Orden",
                value: 51);

            migrationBuilder.UpdateData(
                table: "PreguntasPrueba",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0003-0003-0000-000000000052"),
                column: "Orden",
                value: 52);

            migrationBuilder.UpdateData(
                table: "PreguntasPrueba",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0003-0003-0000-000000000053"),
                column: "Orden",
                value: 53);

            migrationBuilder.UpdateData(
                table: "PreguntasPrueba",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0003-0003-0000-000000000054"),
                column: "Orden",
                value: 54);

            migrationBuilder.UpdateData(
                table: "PreguntasPrueba",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0003-0003-0000-000000000055"),
                column: "Orden",
                value: 55);

            migrationBuilder.InsertData(
                table: "PreguntasPrueba",
                columns: new[] { "Id", "DimensionId", "Orden", "PruebaId", "Texto" },
                values: new object[] { new Guid("eeeeeeee-0003-0003-0000-000000000043"), new Guid("eeeeeeee-0002-0003-0000-000000000004"), 43, new Guid("eeeeeeee-0001-0003-0000-000000000000"), "Puedo cambiar de estrategia cuando una forma de trabajar no da resultado." });

            migrationBuilder.UpdateData(
                table: "PruebasDirector",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-0001-0003-0000-000000000000"),
                columns: new[] { "Descripcion", "Revision" },
                values: new object[] { "Nivel 3: 55 afirmaciones para explorar tendencias declaradas. Preguntas propuestas de ZUNI para revisión profesional; instrumento sin validación psicométrica ni baremos. No produce diagnósticos, banderas automáticas de riesgo ni decisiones académicas.", new Guid("eeeeeeee-0005-0003-0000-000000000000") });

            migrationBuilder.InsertData(
                table: "OpcionesPrueba",
                columns: new[] { "Id", "Orden", "PreguntaPruebaId", "Texto", "Valor" },
                values: new object[,]
                {
                    { new Guid("eeeeeeee-0004-0003-0000-000000000421"), 1, new Guid("eeeeeeee-0003-0003-0000-000000000043"), "1 · Totalmente en desacuerdo / Nada identificado", 1 },
                    { new Guid("eeeeeeee-0004-0003-0000-000000000422"), 2, new Guid("eeeeeeee-0003-0003-0000-000000000043"), "2", 2 },
                    { new Guid("eeeeeeee-0004-0003-0000-000000000423"), 3, new Guid("eeeeeeee-0003-0003-0000-000000000043"), "3", 3 },
                    { new Guid("eeeeeeee-0004-0003-0000-000000000424"), 4, new Guid("eeeeeeee-0003-0003-0000-000000000043"), "4", 4 },
                    { new Guid("eeeeeeee-0004-0003-0000-000000000425"), 5, new Guid("eeeeeeee-0003-0003-0000-000000000043"), "5", 5 },
                    { new Guid("eeeeeeee-0004-0003-0000-000000000426"), 6, new Guid("eeeeeeee-0003-0003-0000-000000000043"), "6", 6 },
                    { new Guid("eeeeeeee-0004-0003-0000-000000000427"), 7, new Guid("eeeeeeee-0003-0003-0000-000000000043"), "7", 7 },
                    { new Guid("eeeeeeee-0004-0003-0000-000000000428"), 8, new Guid("eeeeeeee-0003-0003-0000-000000000043"), "8", 8 },
                    { new Guid("eeeeeeee-0004-0003-0000-000000000429"), 9, new Guid("eeeeeeee-0003-0003-0000-000000000043"), "9", 9 },
                    { new Guid("eeeeeeee-0004-0003-0000-000000000430"), 10, new Guid("eeeeeeee-0003-0003-0000-000000000043"), "10 · Totalmente de acuerdo / Totalmente identificado", 10 }
                });
        }
    }
}
