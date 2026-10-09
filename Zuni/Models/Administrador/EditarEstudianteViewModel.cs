using System.ComponentModel.DataAnnotations;
namespace Zuni.Models.Administrador;
public sealed class EditarEstudianteViewModel
{
    [Required] public string Id { get; set; } = "";
    [Required, StringLength(100,MinimumLength=2)] public string Nombre { get; set; } = "";
    [Required, RegularExpression(@"^(?:[0-9]{10,11}|[0-9]{4}-[0-9]{2}-[0-9]{4,5})$")] public string Carne { get; set; } = "";
    [StringLength(150)] public string? Carrera { get; set; }
}
