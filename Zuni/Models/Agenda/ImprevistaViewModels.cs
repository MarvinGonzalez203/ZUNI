using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
namespace Zuni.Models.Agenda;

public sealed class ImprevistaInput
{
    [Required] public DateOnly? Fecha { get; set; }
    [Required, Display(Name = "Hora de inicio de la atención")] public TimeOnly? HoraInicio { get; set; }
    [Display(Name = "Fin aproximado (opcional)")] public TimeOnly? HoraFin { get; set; }
    [Required, Display(Name = "Estudiante asignado")] public Guid? PerfilEstudianteId { get; set; }
    [EnumDataType(typeof(ModalidadCita))] public ModalidadCita Modalidad { get; set; }
    public Guid OperacionId { get; set; } = Guid.NewGuid();
    [BindNever] public IReadOnlyList<EstudianteImprevista> Estudiantes { get; set; } = [];
}
public sealed record EstudianteImprevista(Guid Id, string Nombre);
// Esta proyección no contiene notas privadas ni expediente.
public sealed record ReporteCompartido(Guid CitaId, string Estudiante, string Carne, string Psicologo, DateTime InicioUtc,
    DateTime FinUtc, bool FinAproximado, string Resultado, string Recomendaciones, bool OtraCita, string? Indicaciones);
