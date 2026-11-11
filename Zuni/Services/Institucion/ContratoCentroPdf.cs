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
        var estilo=new DocumentoZuniPdf("Ficha de contrato\ny vigencia","Servicios para instituciones educativas",
            "Este documento refleja el registro administrativo de ZUNI. No acredita un pago ni sustituye las condiciones contractuales acordadas entre las partes.",
            "Registro comercial; no contiene datos de estudiantes");
        var section=estilo.Cuerpo;
        if(centro.EsDemostracion){var demo=section.AddParagraph(Idioma.Texto("DEMOSTRACIÓN - DATOS FICTICIOS - SIN VALIDEZ CONTRACTUAL"));demo.Format.Font.Bold=true;demo.Format.Font.Size=9;}
        var name=section.AddParagraph(centro.Nombre);name.Format.Font.Size=15;name.Format.Font.Bold=true;name.Format.SpaceAfter=14;
        var table=estilo.Tarjeta(.35,.65);
        void Field(string key,string value){var r=table.AddRow();foreach(var cell in r.Cells.Cast<MigraDoc.DocumentObjectModel.Tables.Cell>()){cell.VerticalAlignment=MigraDoc.DocumentObjectModel.Tables.VerticalAlignment.Center;cell.Format.SpaceAfter=0;}r.Cells[0].AddParagraph(Idioma.Texto(key)).Format.Font.Bold=true;r.Cells[1].AddParagraph(value);}
        Field("Periodo",Idioma.Texto("{0} meses",centro.MesesPlan));Field("Inicio",centro.Inicio.ToString("dd/MM/yyyy"));Field("Finalización",centro.Fin.ToString("dd/MM/yyyy"));
        Field("Estado del contrato",Idioma.Texto(centro.Estado.ToString()));Field("Importe del periodo",centro.ImporteGtq.ToString("N2",System.Globalization.CultureInfo.GetCultureInfo("es-GT"))+" GTQ");
        Field("Correo de contacto comercial",centro.CorreoContacto??Idioma.Texto("Sin registrar"));
        estilo.Campo("Observaciones comerciales",string.IsNullOrWhiteSpace(centro.NotaComercial)?Idioma.Texto("Sin observaciones registradas."):centro.NotaComercial);
        var trace=section.AddParagraph(Idioma.Texto("Emisión: {0} (Guatemala)",AgendaReglas.Local(emisionUtc).ToString("dd/MM/yyyy HH:mm"))+"\n"+Idioma.Texto("Identificador: {0}",centro.Id)+"\n"+Idioma.Texto("Revisión: {0}",centro.Revision));trace.Format.Font.Size=7;trace.Format.Font.Color=Colors.Gray;trace.Format.SpaceBefore=12;
        return estilo.Guardar();
    }
}
