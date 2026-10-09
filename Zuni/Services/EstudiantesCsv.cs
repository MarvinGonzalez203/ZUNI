using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;
using Zuni.Models.Administrador;
namespace Zuni.Services;
public static class EstudiantesCsv
{
    public static ImportacionEstudiantesViewModel Leer(string csv)
    {
        var result = new ImportacionEstudiantesViewModel { Csv = csv };
        if (string.IsNullOrWhiteSpace(csv) || System.Text.Encoding.UTF8.GetByteCount(csv) > 262144)
        { result.Errores.Add("El archivo está vacío o supera 256 KB."); return result; }
        using var parser = new TextFieldParser(new StringReader(csv));
        parser.SetDelimiters(","); parser.HasFieldsEnclosedInQuotes = true;
        try
        {
            var headers = parser.ReadFields();
            if (headers is null || !headers.Select(x => x.Trim().Trim('\uFEFF')).SequenceEqual(new[] { "Nombre", "Correo", "Carne", "Carrera" }, StringComparer.OrdinalIgnoreCase))
            { result.Errores.Add("Los encabezados deben ser Nombre,Correo,Carne,Carrera."); return result; }
            var emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var carnes = new HashSet<string>();
            int row = 1;
            while (!parser.EndOfData)
            {
                row++;
                if (row > 101) { result.Errores.Add("El máximo es 100 estudiantes por archivo."); break; }
                var fields = parser.ReadFields();
                if (fields is null || fields.Length != 4) { result.Errores.Add($"Fila {row}: se requieren cuatro columnas."); continue; }
                var nombre = fields[0].Trim(); var correo = fields[1].Trim(); var rawCarne = fields[2].Trim(); var carne = rawCarne.Replace("-", ""); var carrera = fields[3].Trim();
                if (nombre.Length < 2 || nombre.Length > 100 || correo.Length > 256 || !new EmailAddressAttribute().IsValid(correo) || !correo.EndsWith("@miumg.edu.gt",StringComparison.OrdinalIgnoreCase) || !Regex.IsMatch(rawCarne,@"^(?:[0-9]{10,11}|[0-9]{4}-[0-9]{2}-[0-9]{4,5})$") || carrera.Length > 150)
                    result.Errores.Add($"Fila {row}: revisa nombre, correo institucional, carné o carrera.");
                if (!emails.Add(correo) || !carnes.Add(carne)) result.Errores.Add($"Fila {row}: correo o carné repetido en el archivo.");
                result.Filas.Add(new(nombre,correo,carne,carrera));
            }
            if (result.Filas.Count == 0) result.Errores.Add("El archivo no contiene estudiantes.");
        }
        catch (MalformedLineException) { result.Errores.Add("CSV inválido: revisa las comillas y columnas."); }
        return result;
    }
}
