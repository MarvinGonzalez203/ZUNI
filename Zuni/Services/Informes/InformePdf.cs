using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using Zuni.Models.Informes;
using Zuni.Services.Agenda;

namespace Zuni.Services.Informes;

public static class InformePdf
{
    static InformePdf() { GlobalFontSettings.FontResolver = new FuenteInforme(); }
    public static byte[] Crear(VersionInformeEducativo v)
    {
        var doc = new Document();
        doc.Info.Title = "Informe de apoyo educativo"; doc.Info.Author = v.Psicologo;
        var normal = doc.Styles[StyleNames.Normal]!;
        normal.Font.Name = "ZuniVera"; normal.Font.Size = 10;
        normal.ParagraphFormat.SpaceAfter = Unit.FromPoint(8); normal.ParagraphFormat.LineSpacingRule = LineSpacingRule.Multiple; normal.ParagraphFormat.LineSpacing = 1.15;
        var h = doc.Styles[StyleNames.Heading1]!; h.Font.Name = "ZuniVera"; h.Font.Size = 12; h.Font.Bold = true;
        h.Font.Color = Color.FromRgb(72, 46, 108); h.ParagraphFormat.SpaceBefore = Unit.FromPoint(14); h.ParagraphFormat.SpaceAfter = Unit.FromPoint(6);
        h.ParagraphFormat.KeepWithNext = true;
        var s = doc.AddSection(); s.PageSetup.PageFormat = PageFormat.Letter;
        s.PageSetup.TopMargin = Unit.FromCentimeter(2.0); s.PageSetup.BottomMargin = Unit.FromCentimeter(2.0);
        s.PageSetup.LeftMargin = Unit.FromCentimeter(2.2); s.PageSetup.RightMargin = Unit.FromCentimeter(2.2);
        var header = s.Headers.Primary.AddParagraph(v.Institucion + " | ZUNI"); header.Format.Font.Size = 8; header.Format.Font.Color = Colors.Gray;
        var footer = s.Footers.Primary.AddParagraph("Confidencial - uso exclusivo del destinatario autorizado | ");
        footer.Format.Font.Size = 8; footer.AddPageField(); footer.AddText(" / "); footer.AddNumPagesField();
        var title = s.AddParagraph("Informe de apoyo educativo"); title.Format.Font.Bold = true; title.Format.Font.Size = 20;
        title.Format.SpaceAfter = Unit.FromPoint(14);
        s.AddParagraph($"Identificador: {v.InformeId:D} | Versión {v.Numero}");
        s.AddParagraph($"Emisión: {AgendaReglas.Local(v.FechaEmisionUtc!.Value):dd/MM/yyyy HH:mm} (Guatemala)");
        if (v.SustituyeNumero is { } anterior) s.AddParagraph($"Esta versión sustituye a la versión {anterior}.");
        s.AddParagraph($"Estudiante: {v.Estudiante}\nCarné: {v.Carne}\nDestinatario autorizado: {v.Destinatario}\nPsicólogo responsable: {v.Psicologo}");
        void Apartado(string titulo, string texto) { s.AddParagraph(titulo, StyleNames.Heading1); s.AddParagraph(texto); }
        Apartado("1. Objetivo y periodo", v.Objetivo + $"\nPeriodo observado: {v.PeriodoInicio:dd/MM/yyyy} a {v.PeriodoFin:dd/MM/yyyy}.");
        Apartado("2. Fuentes de información", v.Fuentes);
        Apartado("3. Barreras observadas y desempeño", v.Observaciones);
        Apartado("4. Necesidades de apoyo educativo", v.Necesidades);
        Apartado("5. Recomendaciones", v.Recomendaciones);
        Apartado("6. Responsables y seguimiento", v.ResponsablesAcciones + $"\nSeguimiento propuesto: {v.FechaSeguimiento:dd/MM/yyyy}.");
        Apartado("7. Aprobación y uso del informe", $"Contenido revisado y emitido por {v.Psicologo}, con autorización específica del estudiante para esta versión y destinatario. El nombre presentado no constituye una firma electrónica certificada.\nUtilizar exclusivamente para coordinar el apoyo educativo descrito. No distribuir a terceros ni utilizar como diagnóstico clínico.");
        var renderer = new PdfDocumentRenderer { Document = doc }; renderer.RenderDocument();
        using var stream = new MemoryStream(); renderer.PdfDocument.Save(stream, false); return stream.ToArray();
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
