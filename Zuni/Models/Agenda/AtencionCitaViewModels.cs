using System.ComponentModel.DataAnnotations;

namespace Zuni.Models.Agenda;

public sealed class RegistrarAtencionInput : IValidatableObject
{
    public Guid CitaId { get; set; }
    [Required(ErrorMessage = "Indica si el estudiante asistió.")]
    public bool? Asistio { get; set; }
    public string? ResultadoProfesionalPrivado { get; set; }
    public string? ResenaRecomendacionesEstudiante { get; set; }
    public string? ResultadoBreveEstudiante { get; set; }
    public bool RecomiendaOtraCita { get; set; }
    public string? IndicacionesProximaCita { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var campos = new[]
        {
            (ResultadoProfesionalPrivado, nameof(ResultadoProfesionalPrivado), 4000, Asistio == true, Asistio == true),
            (ResenaRecomendacionesEstudiante, nameof(ResenaRecomendacionesEstudiante), 2000, Asistio == true, true),
            (ResultadoBreveEstudiante, nameof(ResultadoBreveEstudiante), 2000, Asistio == true, Asistio == true),
            (IndicacionesProximaCita, nameof(IndicacionesProximaCita), 2000, Asistio == true && RecomiendaOtraCita, Asistio == true && RecomiendaOtraCita)
        };
        foreach (var (valor, nombre, limite, requerido, aplica) in campos)
        {
            if (!aplica) continue;
            if (requerido && string.IsNullOrWhiteSpace(valor))
                yield return new ValidationResult("Este campo es obligatorio y no puede contener solo espacios.", [nombre]);
            else if (valor is not null && valor.Length > limite)
                yield return new ValidationResult($"Este campo no puede superar los {limite} caracteres.", [nombre]);
            else if (!string.IsNullOrEmpty(valor) && string.IsNullOrWhiteSpace(valor))
                yield return new ValidationResult("Escribe una observación válida o deja el campo vacío.", [nombre]);
        }
    }
}

public sealed record PrepararAtencionViewModel(Guid CitaId, string Estudiante, DateTime InicioUtc,
    ModalidadCita Modalidad, bool Cerrada);

// Proyección exclusiva del estudiante: no tiene ningún campo profesional privado.
public sealed class ResultadoAtencionEstudianteViewModel
{
    public Guid CitaId { get; init; }
    public DateTime InicioUtc { get; init; }
    public ModalidadCita Modalidad { get; init; }
    public string? ResenaRecomendacionesEstudiante { get; init; }
    public string? ResultadoBreveEstudiante { get; init; }
    public bool RecomiendaOtraCita { get; init; }
    public string? IndicacionesProximaCita { get; init; }
}

public sealed class AtencionPrivadaViewModel
{
    public string Estudiante { get; init; } = string.Empty;
    public DateTime InicioUtc { get; init; }
    public DateTime FechaCierreUtc { get; init; }
    public ModalidadCita Modalidad { get; init; }
    public bool Asistio { get; init; }
    public string? ResultadoProfesionalPrivado { get; init; }
    public string? ResenaRecomendacionesEstudiante { get; init; }
    public string? ResultadoBreveEstudiante { get; init; }
    public bool RecomiendaOtraCita { get; init; }
    public string? IndicacionesProximaCita { get; init; }
}
