using System.ComponentModel.DataAnnotations;
namespace Zuni.Models.Evaluaciones;

public sealed class ParticipacionBigFive
{
    public Guid AsignacionId { get; set; }
    public AsignacionEvaluacion Asignacion { get; set; } = null!;
    public string PsicologoId { get; set; } = "";
    public ApplicationUser Psicologo { get; set; } = null!;
    public string VersionInstrumento { get; set; } = "";
    public bool PruebaLocal { get; set; }
    public string VersionConsentimiento { get; set; } = "";
    public string TextoConsentimiento { get; set; } = "";
    public DateTime ConsentimientoUtc { get; set; }
    public DateTime? RevocadoUtc { get; set; }
    public string MotivoConsulta { get; set; } = "";
    public string Referencia { get; set; } = "";
    public decimal? Apertura { get; set; }
    public decimal? Responsabilidad { get; set; }
    public decimal? Extraversion { get; set; }
    public decimal? Amabilidad { get; set; }
    public decimal? Neuroticismo { get; set; }
}

public sealed class AccesoBigFive
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AsignacionId { get; set; }
    public ParticipacionBigFive Participacion { get; set; } = null!;
    public string PsicologoId { get; set; } = "";
    public ApplicationUser Psicologo { get; set; } = null!;
    public DateTime FechaUtc { get; set; }
}

public sealed class ConsentimientoBigFiveInput
{
    [Required, StringLength(1500, MinimumLength = 5)]
    public string MotivoConsulta { get; set; } = "";
    [RegularExpression("^(Ninguna|Docente|Estudiante|Coordinacion|Otra)$")]
    public string Referencia { get; set; } = "Ninguna";
    public bool MayorDeEdad { get; set; }
    public bool Acepto { get; set; }
}

public sealed record DimensionBigFive(string Nombre, decimal Media, string ExtremoInferior, string ExtremoSuperior);
public sealed record ResumenBigFive(Guid AsignacionId, string Estudiante, DateTime? FinalizacionUtc,
    string MotivoConsulta, string Referencia, IReadOnlyList<DimensionBigFive> Dimensiones);
