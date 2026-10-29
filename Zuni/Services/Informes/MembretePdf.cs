using MigraDoc.DocumentObjectModel;

namespace Zuni.Services.Informes;

internal static class MembretePdf
{
    internal static readonly Color Violeta=Color.FromRgb(206,162,253);
    internal static readonly Color Tinta=Color.FromRgb(34,27,42);
    private static string Logo()
    {
        using var source=typeof(MembretePdf).Assembly.GetManifestResourceStream("Zuni.Resources.Logo.png")!;
        using var bytes=new MemoryStream();source.CopyTo(bytes);
        return "base64:"+Convert.ToBase64String(bytes.ToArray());
    }
    internal static void Aplicar(Section section,string institucion,string pie)
    {
        var header=section.Headers.Primary.AddTable();header.AddColumn(Unit.FromCentimeter(1.5));header.AddColumn(Unit.FromCentimeter(15.5));
        var row=header.AddRow();row.Cells[0].AddImage(Logo()).Width=Unit.FromCentimeter(1.05);
        var name=row.Cells[1].AddParagraph("ZUNI");name.Format.Font.Bold=true;name.Format.Font.Size=14;name.Format.SpaceAfter=2;
        var caption=row.Cells[1].AddParagraph(institucion);caption.Format.Font.Size=8;caption.Format.Font.Color=Colors.Gray;
        header.Borders.Bottom.Color=Violeta;header.Borders.Bottom.Width=2;header.BottomPadding=Unit.FromPoint(7);
        section.PageSetup.HeaderDistance=Unit.FromCentimeter(.8);
        var footer=section.Footers.Primary.AddParagraph(pie+" | ");footer.Format.Font.Size=7;footer.Format.Font.Color=Colors.Gray;
        footer.Format.Borders.Top.Color=Violeta;footer.Format.Borders.Top.Width=1;footer.Format.SpaceBefore=6;
        footer.AddPageField();footer.AddText(" / ");footer.AddNumPagesField();
    }
}
