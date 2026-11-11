using Zuni.Localizacion;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using Zuni.Models.Informes;
using Zuni.Services.Agenda;

namespace Zuni.Services.Informes;

public static class InformePdf
{
    static InformePdf() { GlobalFontSettings.FontResolver = new FuenteInforme(); }
    internal static void PrepararFuentes() { }
    public static byte[] Crear(VersionInformeEducativo v,bool demostracion=false)
    {
        var doc = new Document();
        doc.Info.Title = Idioma.Texto("Informe de apoyo educativo"); doc.Info.Author = v.Psicologo;
        var normal = doc.Styles[StyleNames.Normal]!;
        normal.Font.Name = "ZuniVera"; normal.Font.Size = 10;
        normal.ParagraphFormat.SpaceAfter = Unit.FromPoint(6); normal.ParagraphFormat.LineSpacingRule = LineSpacingRule.Multiple; normal.ParagraphFormat.LineSpacing = 1.15;
        var h = doc.Styles[StyleNames.Heading1]!; h.Font.Name = "ZuniVera"; h.Font.Size = 12; h.Font.Bold = true;
        h.Font.Color = Color.FromRgb(72, 46, 108); h.ParagraphFormat.SpaceBefore = Unit.FromPoint(10); h.ParagraphFormat.SpaceAfter = Unit.FromPoint(6);
        h.ParagraphFormat.KeepWithNext = true;
        var s = doc.AddSection(); s.PageSetup.PageFormat = PageFormat.Letter;
        s.PageSetup.TopMargin = Unit.FromCentimeter(3.0); s.PageSetup.BottomMargin = Unit.FromCentimeter(2.2);
        s.PageSetup.LeftMargin = Unit.FromCentimeter(2.2); s.PageSetup.RightMargin = Unit.FromCentimeter(2.2);
        MembretePdf.Aplicar(s,v.Institucion,Idioma.Texto("Confidencial - uso exclusivo del destinatario autorizado"));
        if(demostracion){var demo=s.AddParagraph(Idioma.Texto("DEMOSTRACIÓN - PERSONAS Y AUTORIZACIÓN SIMULADAS"));demo.Format.Font.Bold=true;demo.Format.Font.Size=9;demo.Format.Shading.Color=Color.FromRgb(237,232,208);}
        var title = s.AddParagraph(Idioma.Texto("Informe de apoyo educativo")); title.Format.Font.Bold = true; title.Format.Font.Size = 20;
        title.Format.SpaceAfter = Unit.FromPoint(14);
        s.AddParagraph(Idioma.Texto("Identificador: {0} | Versión {1}",v.InformeId.ToString("D"),v.Numero));
        s.AddParagraph(Idioma.Texto("Emisión: {0} (Guatemala)",AgendaReglas.Local(v.FechaEmisionUtc!.Value).ToString("dd/MM/yyyy HH:mm")));
        if (v.SustituyeNumero is { } anterior) s.AddParagraph(Idioma.Texto("Esta versión sustituye a la versión {0}.",anterior));
        s.AddParagraph(Idioma.Texto("Estudiante: {0}\nCarné: {1}\nDestinatario autorizado: {2}\nPsicólogo responsable: {3}",v.Estudiante,v.Carne,v.Destinatario,v.Psicologo));
        void Apartado(string titulo, string texto) { s.AddParagraph(Idioma.Texto(titulo), StyleNames.Heading1); var paragraph=s.AddParagraph(texto); if(titulo.StartsWith("7."))paragraph.Format.KeepTogether=true; }
        Apartado("1. Objetivo y periodo", v.Objetivo + "\n"+Idioma.Texto("Periodo observado: {0} a {1}.",v.PeriodoInicio.ToString("dd/MM/yyyy"),v.PeriodoFin.ToString("dd/MM/yyyy")));
        Apartado("2. Fuentes de información", v.Fuentes);
        Apartado("3. Barreras observadas y desempeño", v.Observaciones);
        if(v.IncluyeAnalisisRendimiento==true){
            s.AddParagraph(Idioma.Texto("3.1. Análisis del rendimiento académico"),StyleNames.Heading1);
            var relato=s.AddParagraph(Idioma.Texto("Lo que refiere el estudiante"));relato.Format.Font.Bold=true;relato.Format.KeepWithNext=true;
            s.AddParagraph(v.RelatoRendimiento);
            var valoracion=s.AddParagraph(Idioma.Texto("Valoración contrastada del psicólogo"));valoracion.Format.Font.Bold=true;valoracion.Format.KeepWithNext=true;
            s.AddParagraph(v.ValoracionRendimiento);
        }else if(v.IncluyeAnalisisRendimiento==false){Apartado("3.1. Análisis del rendimiento académico",Idioma.Texto("En este informe no se establece una relación con el rendimiento académico."));}
        Apartado("4. Necesidades de apoyo educativo", v.Necesidades);
        Apartado("5. Recomendaciones", v.Recomendaciones);
        Apartado("6. Responsables y seguimiento", v.ResponsablesAcciones + "\n"+Idioma.Texto("Seguimiento propuesto: {0}.",v.FechaSeguimiento.ToString("dd/MM/yyyy")));
        Apartado("7. Aprobación y uso del informe", demostracion?Idioma.Texto("Ejemplo ficticio de contenido que el psicólogo puede revisar para un destinatario autorizado. No documenta una atención, firma ni autorización real. Una entrega real requiere la revisión profesional y la autorización correspondiente a esa versión y persona destinataria."):Idioma.Texto("Contenido revisado y emitido por {0}, con autorización específica del estudiante para esta versión y destinatario. El nombre presentado no constituye una firma electrónica certificada.\nUtilizar exclusivamente para coordinar el apoyo educativo descrito. No distribuir a terceros ni utilizar como diagnóstico clínico.",v.Psicologo));
        var apoyo=s.AddParagraph(Idioma.Texto("ZUNI es una herramienta de apoyo a la psicología educativa. No sustituye al psicólogo ni realiza diagnósticos automáticos."));apoyo.Format.Font.Size=8;apoyo.Format.Font.Color=Colors.Gray;
        var renderer = new PdfDocumentRenderer { Document = doc }; renderer.RenderDocument();
        using var stream = new MemoryStream(); renderer.PdfDocument.Save(stream, false); return stream.ToArray();
    }
    public static byte[] CrearCompartido(Zuni.Models.Agenda.ReporteCompartido r)
    {
        var estilo = new DocumentoZuniPdf("Reporte compartido\nde atención",
            "Un resumen para recordar tus acuerdos y próximos pasos.",
            "Contiene únicamente información compartida con el estudiante. No incluye notas profesionales privadas ni sustituye un informe educativo autorizado para terceros.",
            "Uso del estudiante y profesional autorizado");
        estilo.Documento.Info.Author = r.Psicologo;
        var datos = estilo.Tarjeta(.5, .5);
        void Dato(MigraDoc.DocumentObjectModel.Tables.Cell cell, string key, string value)
        {
            cell.VerticalAlignment = MigraDoc.DocumentObjectModel.Tables.VerticalAlignment.Center;
            var k = cell.AddParagraph(Idioma.Texto(key)); k.Format.Font.Size = 8; k.Format.Font.Bold = true;
            k.Format.Font.Color = Colors.Gray; k.Format.SpaceAfter = 4;
            var v = cell.AddParagraph(value); v.Format.SpaceAfter = 0;
        }
        var fila = datos.AddRow(); Dato(fila.Cells[0], "ESTUDIANTE", r.Estudiante);
        Dato(fila.Cells[1], "CARNÉ", Zuni.Helpers.CarneHelper.Formatear(r.Carne) ?? r.Carne);
        fila = datos.AddRow(); Dato(fila.Cells[0], "PSICÓLOGO RESPONSABLE", r.Psicologo);
        Dato(fila.Cells[1], "FECHA Y HORARIO", $"{AgendaReglas.Local(r.InicioUtc):dd/MM/yyyy · HH:mm} - {AgendaReglas.Local(r.FinUtc):HH:mm}\n" + Idioma.Texto("Fin {0} · Guatemala", Idioma.Texto(r.FinAproximado ? "aproximado" : "programado")));
        estilo.Campo("01  " + Idioma.Texto("Resultado compartido"), r.Resultado);
        estilo.Campo("02  " + Idioma.Texto("Recomendaciones y acuerdos"), r.Recomendaciones);
        if (r.OtraCita) estilo.Campo("03  " + Idioma.Texto("Seguimiento recomendado"), r.Indicaciones ?? "");
        return estilo.Guardar();
    }
    private sealed class FuenteInforme : IFontResolver
    {
        public FontResolverInfo ResolveTypeface(string familyName, bool bold, bool italic) => new(bold ? "VeraBd" : "Vera", false, italic);
        public byte[] GetFont(string faceName)
        {
            using var font = typeof(InformePdf).Assembly.GetManifestResourceStream("Zuni.Resources.Fonts." + faceName + ".ttf")
                ?? throw new InvalidOperationException("No se encontró la fuente del informe.");
            using var stream = new MemoryStream(); font.CopyTo(stream); return stream.ToArray();
        }
    }
}
