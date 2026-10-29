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
        var doc = new Document(); doc.Info.Title = Idioma.Texto("Reporte compartido de atención"); doc.Info.Author = r.Psicologo;
        var beige = Color.FromRgb(237,232,208); var violeta = Color.FromRgb(206,162,253); var tinta=Color.FromRgb(17,17,17); var gris=Color.FromRgb(98,93,99);
        var normal = doc.Styles[StyleNames.Normal]!; normal.Font.Name = "ZuniVera"; normal.Font.Size = 10; normal.Font.Color=tinta; normal.ParagraphFormat.SpaceAfter = Unit.FromPoint(8); normal.ParagraphFormat.LineSpacingRule=LineSpacingRule.Multiple; normal.ParagraphFormat.LineSpacing=1.12;
        var s = doc.AddSection(); s.PageSetup.PageFormat = PageFormat.Letter; s.PageSetup.TopMargin = Unit.FromCentimeter(1.7); s.PageSetup.BottomMargin = Unit.FromCentimeter(3); s.PageSetup.FooterDistance = Unit.FromCentimeter(1.2); s.PageSetup.LeftMargin = Unit.FromCentimeter(2); s.PageSetup.RightMargin = Unit.FromCentimeter(2);
        string Logo() { using var stream=typeof(InformePdf).Assembly.GetManifestResourceStream("Zuni.Resources.Logo.png")!; using var bytes=new MemoryStream(); stream.CopyTo(bytes); return "base64:"+Convert.ToBase64String(bytes.ToArray()); }
        var encabezado=s.AddTable(); encabezado.AddColumn(Unit.FromCentimeter(14.5)); encabezado.AddColumn(Unit.FromCentimeter(3)); encabezado.Shading.Color=beige; encabezado.TopPadding=12; encabezado.BottomPadding=12; encabezado.LeftPadding=12;
        var er=encabezado.AddRow(); var etiqueta=er.Cells[0].AddParagraph(Idioma.Texto("ZUNI · ACOMPAÑAMIENTO EDUCATIVO")); etiqueta.Format.Font.Size=8; etiqueta.Format.Font.Bold=true;
        var title=er.Cells[0].AddParagraph(Idioma.Texto("Reporte compartido\nde atención")); title.Format.Font.Size=21; title.Format.Font.Bold=true; title.Format.SpaceAfter=4;
        var subtitle=er.Cells[0].AddParagraph(Idioma.Texto("Un resumen para recordar tus acuerdos y próximos pasos.")); subtitle.Format.Font.Size=9; subtitle.Format.Font.Color=gris;
        var imagen=er.Cells[1].AddImage(Logo()); imagen.Width=Unit.FromCentimeter(1.9); imagen.LockAspectRatio=true; er.Cells[1].VerticalAlignment=MigraDoc.DocumentObjectModel.Tables.VerticalAlignment.Center;
        var barra=s.AddParagraph(); barra.Format.Borders.Bottom.Color=violeta; barra.Format.Borders.Bottom.Width=4; barra.Format.SpaceAfter=15;
        var datos=s.AddTable(); datos.AddColumn(Unit.FromCentimeter(8.75)); datos.AddColumn(Unit.FromCentimeter(8.75)); datos.TopPadding=9; datos.BottomPadding=9; datos.LeftPadding=10; datos.RightPadding=10; datos.Borders.Color=Colors.White; datos.Borders.Width=4; datos.Shading.Color=Color.FromRgb(247,245,233);
        void Dato(MigraDoc.DocumentObjectModel.Tables.Cell cell,string key,string val) {var k=cell.AddParagraph(Idioma.Texto(key)); k.Format.Font.Size=8; k.Format.Font.Color=gris; k.Format.Font.Bold=true; k.Format.SpaceAfter=4; var v=cell.AddParagraph(val); v.Format.Font.Size=10; v.Format.SpaceAfter=0;}
        var fila=datos.AddRow(); Dato(fila.Cells[0],"ESTUDIANTE",r.Estudiante); Dato(fila.Cells[1],"CARNÉ",Zuni.Helpers.CarneHelper.Formatear(r.Carne)??r.Carne);
        fila=datos.AddRow(); Dato(fila.Cells[0],"PSICÓLOGO RESPONSABLE",r.Psicologo); Dato(fila.Cells[1],"FECHA Y HORARIO",$"{AgendaReglas.Local(r.InicioUtc):dd/MM/yyyy · HH:mm} — {AgendaReglas.Local(r.FinUtc):HH:mm}\n"+Idioma.Texto("Fin {0} · Guatemala",Idioma.Texto(r.FinAproximado ? "aproximado" : "programado")));
        void Campo(string numero,string t,string texto) {
            var h=s.AddParagraph(numero+"  "+Idioma.Texto(t)); h.Format.Font.Bold=true; h.Format.Font.Size=12; h.Format.SpaceBefore=15; h.Format.SpaceAfter=9; h.Format.KeepWithNext=true; h.Format.Borders.Bottom.Color=violeta; h.Format.Borders.Bottom.Width=1.5; h.Format.Borders.DistanceFromBottom=5;
            var p=s.AddParagraph(texto); p.Format.LeftIndent=10; p.Format.RightIndent=10; p.Format.SpaceBefore=3; p.Format.SpaceAfter=10; p.Format.Shading.Color=Color.FromRgb(253,252,249);
        }
        Campo("01","Resultado compartido",r.Resultado); Campo("02","Recomendaciones y acuerdos",r.Recomendaciones);
        if(r.OtraCita) Campo("03","Seguimiento recomendado",r.Indicaciones??"");
        var espacio=s.AddParagraph();espacio.Format.SpaceAfter=8;
        var aviso=s.AddTable();aviso.AddColumn(Unit.FromCentimeter(17.5));aviso.Shading.Color=beige;aviso.TopPadding=8;aviso.BottomPadding=8;aviso.LeftPadding=10;aviso.RightPadding=10;
        var ac=aviso.AddRow().Cells[0];var ak=ac.AddParagraph(Idioma.Texto("SOBRE ESTE DOCUMENTO"));ak.Format.Font.Size=8;ak.Format.Font.Bold=true;ak.Format.SpaceAfter=4;
        var av=ac.AddParagraph(Idioma.Texto("Contiene únicamente información compartida con el estudiante. No incluye notas profesionales privadas ni sustituye un informe educativo autorizado para terceros."));av.Format.Font.Size=8;av.Format.Font.Color=gris;av.Format.SpaceAfter=0;
        var ft=s.Footers.Primary.AddTable(); ft.AddColumn(Unit.FromCentimeter(1.3)); ft.AddColumn(Unit.FromCentimeter(12.5)); ft.AddColumn(Unit.FromCentimeter(3.7)); ft.Borders.Top.Color=violeta; ft.Borders.Top.Width=2; ft.TopPadding=7;
        var fr=ft.AddRow(); var fl=fr.Cells[0].AddImage(Logo()); fl.Width=Unit.FromCentimeter(.8); fl.LockAspectRatio=true;
        var fp=fr.Cells[1].AddParagraph(Idioma.Texto("ZUNI · Tu espacio. Tu ritmo.\nUso del estudiante y profesional autorizado")); fp.Format.Font.Size=8; fp.Format.Font.Color=gris;
        var pg=fr.Cells[2].AddParagraph(Idioma.Texto("Página ")); pg.AddPageField(); pg.AddText(Idioma.Texto(" de ")); pg.AddNumPagesField(); pg.Format.Font.Size=8; pg.Format.Alignment=ParagraphAlignment.Right;
        var renderer = new PdfDocumentRenderer { Document = doc }; renderer.RenderDocument(); using var output = new MemoryStream(); renderer.PdfDocument.Save(output, false); return output.ToArray();
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
