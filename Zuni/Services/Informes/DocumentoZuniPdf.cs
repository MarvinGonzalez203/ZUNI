using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
using Zuni.Localizacion;

namespace Zuni.Services.Informes;

// Shared geometry for printable reports and commercial records. Body pagination remains
// MigraDoc's responsibility; the fixed stationery never competes with flowing text.
internal sealed class DocumentoZuniPdf
{
    internal Document Documento { get; } = new();
    internal Section Cuerpo { get; }
    private const double Margen = 56.7;
    private const double Ancho = 612 - 2 * Margen;
    private readonly string titulo, subtitulo, aviso, uso;
    private readonly HashSet<DocumentObject> tarjetas = [];
    private static readonly XColor Arena = XColor.FromArgb(237, 232, 208);
    private static readonly XColor Violeta = XColor.FromArgb(206, 162, 253);
    private static readonly XColor Tinta = XColor.FromArgb(34, 27, 42);
    private static readonly XColor Gris = XColor.FromArgb(98, 93, 99);

    internal DocumentoZuniPdf(string titulo, string subtitulo, string aviso, string uso)
    {
        InformePdf.PrepararFuentes();
        this.titulo = Idioma.Texto(titulo); this.subtitulo = Idioma.Texto(subtitulo);
        this.aviso = Idioma.Texto(aviso); this.uso = Idioma.Texto(uso);
        Documento.Info.Title = this.titulo.Replace("\n", " "); Documento.Info.Author = "ZUNI";
        var normal = Documento.Styles[StyleNames.Normal]!;
        normal.Font.Name = "ZuniVera"; normal.Font.Size = 10; normal.Font.Color = MembretePdf.Tinta;
        normal.ParagraphFormat.SpaceAfter = 8;
        normal.ParagraphFormat.LineSpacingRule = LineSpacingRule.Multiple;
        normal.ParagraphFormat.LineSpacing = 1.12;
        Cuerpo = Documento.AddSection(); Cuerpo.PageSetup.PageFormat = PageFormat.Letter;
        Cuerpo.PageSetup.LeftMargin = Cuerpo.PageSetup.RightMargin = Unit.FromPoint(Margen);
        Cuerpo.PageSetup.TopMargin = Unit.FromPoint(184);
        Cuerpo.PageSetup.BottomMargin = Unit.FromPoint(148);
    }

    internal Table Tarjeta(params double[] proporciones)
    {
        var table = Cuerpo.AddTable();
        foreach (var proporcion in proporciones) table.AddColumn(Unit.FromPoint(Ancho * proporcion));
        table.LeftPadding = table.RightPadding = 10;
        table.TopPadding = table.BottomPadding = 9;
        table.Rows.LeftIndent = 10; // Align outer edge, rather than the first cell's text, to the margin.
        tarjetas.Add(table);
        return table;
    }

    internal void Campo(string titulo, string contenido)
    {
        var h = Cuerpo.AddParagraph(Idioma.Texto(titulo));
        h.Format.Font.Bold = true; h.Format.Font.Size = 12;
        h.Format.SpaceBefore = 14; h.Format.SpaceAfter = 9; h.Format.KeepWithNext = true;
        h.Format.Borders.Bottom.Color = MembretePdf.Violeta; h.Format.Borders.Bottom.Width = 1.5;
        h.Format.Borders.DistanceFromBottom = 4;
        Cuerpo.AddParagraph(contenido);
    }

    internal byte[] Guardar()
    {
        var renderer = new PdfDocumentRenderer { Document = Documento }; renderer.RenderDocument();
        using var logoStream = typeof(DocumentoZuniPdf).Assembly.GetManifestResourceStream("Zuni.Resources.Logo.png")!;
        using var logo = XImage.FromStream(logoStream);
        for (var i = 0; i < renderer.PdfDocument.PageCount; i++)
        {
            var page = renderer.PdfDocument.Pages[i];
            using var g = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Prepend);
            foreach (var info in renderer.DocumentRenderer!.GetRenderInfoFromPage(i + 1) ?? [])
            {
                if (!tarjetas.Contains(info.DocumentObject)) continue;
                var area = info.LayoutInfo.ContentArea;
                g.DrawRoundedRectangle(new XSolidBrush(Arena), Margen, area.Y.Point, Ancho, area.Height.Point, 14, 14);
            }
            g.DrawRoundedRectangle(new XSolidBrush(Arena), Margen, 46, Ancho, 116, 18, 18);
            Texto(g, Idioma.Texto("ZUNI · ACOMPAÑAMIENTO EDUCATIVO"), 8, true, Tinta, Margen + 14, 59, Ancho - 112, 15);
            Texto(g, titulo, 21, true, Tinta, Margen + 14, 79, Ancho - 112, 54);
            Texto(g, subtitulo, 8, false, Gris, Margen + 14, 137, Ancho - 112, 22);
            Logo(g, logo, Margen + Ancho - 88, 66, 70);
            g.DrawLine(new XPen(Violeta, 3), Margen, 169, Margen + Ancho, 169);

            // The notice has the same position on every page, even with long recommendations.
            g.DrawRoundedRectangle(new XSolidBrush(Arena), Margen, 660, Ancho, 56, 14, 14);
            Texto(g, Idioma.Texto("SOBRE ESTE DOCUMENTO"), 8, true, Tinta, Margen + 10, 668, Ancho - 20, 12);
            Texto(g, aviso, 8, false, Gris, Margen + 10, 683, Ancho - 20, 30);
            g.DrawLine(new XPen(Violeta, 2), Margen, 729, Margen + Ancho, 729);
            Logo(g, logo, Margen, 738, 26);
            Texto(g, Idioma.Texto("ZUNI · Tu espacio. Tu ritmo."), 8, true, Gris, Margen + 36, 739, Ancho - 130, 13);
            Texto(g, uso, 7, false, Gris, Margen + 36, 752, Ancho - 130, 23);
            g.DrawString(Idioma.Texto("Página ") + (i + 1) + Idioma.Texto(" de ") + renderer.PdfDocument.PageCount,
                new XFont("ZuniVera", 8), new XSolidBrush(Gris), new XRect(Margen, 740, Ancho, 14), XStringFormats.TopRight);
        }
        using var output = new MemoryStream(); renderer.PdfDocument.Save(output, false); return output.ToArray();
    }

    private static void Texto(XGraphics g, string texto, double size, bool bold, XColor color, double x, double y, double w, double h) =>
        new XTextFormatter(g) { Alignment = XParagraphAlignment.Left }.DrawString(texto, new XFont("ZuniVera", size, bold ? XFontStyleEx.Bold : XFontStyleEx.Regular), new XSolidBrush(color), new XRect(x, y, w, h));

    private static void Logo(XGraphics g, XImage logo, double x, double y, double size)
    {
        var state = g.Save();
        g.TranslateTransform(x + size / 2, y + size / 2);
        g.RotateTransform(-32);
        g.DrawEllipse(new XPen(XColors.White, .6), new XSolidBrush(XColor.FromArgb(90, 255, 255, 255)), -size / 2, -size * .39, size, size * .78);
        g.RotateTransform(24);
        g.DrawEllipse(new XPen(XColors.White, .6), new XSolidBrush(XColor.FromArgb(65, 255, 255, 255)), -size * .46, -size * .46, size * .92, size * .92);
        g.Restore(state);
        g.DrawImage(logo, x + size * .15, y + size * .15, size * .7, size * .7);
    }
}
