using System.ComponentModel.DataAnnotations;
namespace Zuni.Models;
public sealed class CarreraInstitucional
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(150)] public string Nombre { get; set; } = "";
    [MaxLength(150)] public string NombreNormalizado { get; set; } = "";
    [MaxLength(100)] public string Area { get; set; } = "";
    public bool Activa { get; set; } = true;
    public Guid Revision { get; set; } = Guid.NewGuid();
}
public sealed class ImportarCarrerasInput
{
    [MaxLength(100000)] public string? Listado { get; set; }
    public Microsoft.AspNetCore.Http.IFormFile? Archivo { get; set; }
}
public sealed class AgregarAreaCarrerasInput
{
    [Required,MaxLength(100)] public string Area { get; set; }="";
    [Required,MaxLength(100000)] public string Carreras { get; set; }="";
}
