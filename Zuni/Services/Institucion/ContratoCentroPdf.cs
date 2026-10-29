using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using Zuni.Localizacion;
using Zuni.Models.Comercial;
using Zuni.Services.Agenda;
using Zuni.Services.Informes;

namespace Zuni.Services.Institucion;

public static class ContratoCentroPdf
{
    public static byte[] Crear(CentroComercial centro,DateTime emisionUtc)
    {
        InformePdf.PrepararFuentes();
        var doc=new Document();doc.Info.Title=Idioma.Texto("Ficha de contrato y vigencia");doc.Info.Author="ZUNI";
        var normal=doc.Styles[StyleNames.Normal]!;normal.Font.Name="ZuniVera";normal.Font.Size=10;normal.Font.Color=MembretePdf.Tinta;
        normal.ParagraphFormat.SpaceAfter=8;
        var section=doc.AddSection();section.PageSetup.PageFormat=PageFormat.Letter;
        section.PageSetup.TopMargin=Unit.FromCentimeter(3);section.PageSetup.BottomMargin=Unit.FromCentimeter(2.2);
        section.PageSetup.LeftMargin=section.PageSetup.RightMargin=Unit.FromCentimeter(2.3);
        MembretePdf.Aplicar(section,Idioma.Texto("Servicios para instituciones educativas"),Idioma.Texto("ZUNI | Registro comercial; no contiene datos de estudiantes"));
        var title=section.AddParagraph(Idioma.Texto("Ficha de contrato\ny vigencia"));title.Format.Font.Size=24;title.Format.Font.Bold=true;title.Format.SpaceAfter=14;
        if(centro.EsDemostracion){var demo=section.AddParagraph(Idioma.Texto("DEMOSTRACIÓN - DATOS FICTICIOS - SIN VALIDEZ CONTRACTUAL"));demo.Format.Font.Bold=true;demo.Format.Font.Size=9;demo.Format.Shading.Color=Color.FromRgb(237,232,208);}
        var name=section.AddParagraph(centro.Nombre);name.Format.Font.Size=15;name.Format.Font.Bold=true;name.Format.SpaceAfter=14;
        var table=section.AddTable();table.AddColumn(Unit.FromCentimeter(5.5));table.AddColumn(Unit.FromCentimeter(11));table.TopPadding=8;table.BottomPadding=8;table.LeftPadding=10;
        table.Borders.Color=Colors.White;table.Borders.Width=3;
        void Field(string key,string value){var r=table.AddRow();r.Cells[0].Shading.Color=Color.FromRgb(237,232,208);r.Cells[1].Shading.Color=Color.FromRgb(247,244,250);r.Cells[0].AddParagraph(Idioma.Texto(key)).Format.Font.Bold=true;r.Cells[1].AddParagraph(value);}
        Field("Periodo",Idioma.Texto("{0} meses",centro.MesesPlan));Field("Inicio",centro.Inicio.ToString("dd/MM/yyyy"));Field("Finalización",centro.Fin.ToString("dd/MM/yyyy"));
        Field("Estado del contrato",Idioma.Texto(centro.Estado.ToString()));Field("Importe del periodo",centro.ImporteGtq.ToString("N2",System.Globalization.CultureInfo.GetCultureInfo("es-GT"))+" GTQ");
        Field("Correo de contacto comercial",centro.CorreoContacto??Idioma.Texto("Sin registrar"));
        var h=section.AddParagraph(Idioma.Texto("Observaciones comerciales"));h.Format.SpaceBefore=15;h.Format.Font.Size=12;h.Format.Font.Bold=true;h.Format.KeepWithNext=true;
        section.AddParagraph(string.IsNullOrWhiteSpace(centro.NotaComercial)?Idioma.Texto("Sin observaciones registradas."):centro.NotaComercial);
        var note=section.AddParagraph(Idioma.Texto("Este documento refleja el registro administrativo de ZUNI. No acredita un pago ni sustituye las condiciones contractuales acordadas entre las partes."));note.Format.Font.Size=8;note.Format.Shading.Color=Color.FromRgb(237,232,208);note.Format.SpaceBefore=14;
        var trace=section.AddParagraph(Idioma.Texto("Emisión: {0} (Guatemala)",AgendaReglas.Local(emisionUtc).ToString("dd/MM/yyyy HH:mm"))+"\n"+Idioma.Texto("Identificador: {0}",centro.Id)+"\n"+Idioma.Texto("Revisión: {0}",centro.Revision));trace.Format.Font.Size=7;trace.Format.Font.Color=Colors.Gray;trace.Format.SpaceBefore=12;
        var renderer=new PdfDocumentRenderer{Document=doc};renderer.RenderDocument();using var stream=new MemoryStream();renderer.PdfDocument.Save(stream,false);return stream.ToArray();
    }
}
