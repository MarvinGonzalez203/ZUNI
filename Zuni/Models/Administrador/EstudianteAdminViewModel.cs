namespace Zuni.Models.Administrador;
public sealed class EstudianteAdminViewModel
{
    public string Id { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? Correo { get; set; }
    public string? Carne { get; set; }
    public string? Carrera { get; set; }
    public bool Activo { get; set; }
}
public sealed class ImportacionEstudiantesViewModel
{
    public string Csv { get; set; } = "";
    public List<FilaEstudianteCsv> Filas { get; set; } = new();
    public List<string> Errores { get; set; } = new();
}
public sealed record FilaEstudianteCsv(string Nombre, string Correo, string Carne, string Carrera);
