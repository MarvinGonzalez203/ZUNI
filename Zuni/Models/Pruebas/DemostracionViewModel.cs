using Microsoft.AspNetCore.Mvc.ModelBinding;
namespace Zuni.Models.Pruebas;
public sealed class DemostracionViewModel
{
    public Dictionary<Guid, int?> Respuestas { get; set; } = [];
    public bool Consentimiento { get; set; }
    [BindNever] public Prueba Prueba { get; set; } = new();
    [BindNever] public bool Concluida { get; set; }
}
