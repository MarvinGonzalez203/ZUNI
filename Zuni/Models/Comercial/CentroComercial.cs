using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
namespace Zuni.Models.Comercial;

// Registro comercial del proveedor. No referencia alumnos, instituciones clínicas ni expedientes.
public sealed class CentroComercial
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required,MaxLength(150)] public string Nombre { get; set; } = "";
    [MaxLength(254)] public string? CorreoContacto { get; set; }
    public int MesesPlan { get; set; } = 12;
    public DateOnly Inicio { get; set; }
    public DateOnly Fin { get; set; }
    public EstadoContrato Estado { get; set; }
    [Precision(12,2)] public decimal ImporteGtq { get; set; }
    public bool EsDemostracion { get; set; }
    [MaxLength(1000)] public string? NotaComercial { get; set; }
    [Required,MaxLength(450)] public string ActualizadoPorId { get; set; } = "";
    public DateTime ActualizadoUtc { get; set; }
    [ConcurrencyCheck] public Guid Revision { get; set; } = Guid.NewGuid();
}
public enum EstadoContrato { Activo, Suspendido, Cancelado }
public sealed class EditarCentroInput : IValidatableObject
{
    public Guid? Id { get; set; }
    public Guid Revision { get; set; }
    [Required,StringLength(150),Display(Name="Centro educativo")] public string Nombre { get; set; } = "";
    [EmailAddress,StringLength(254),Display(Name="Correo de contacto comercial")] public string? CorreoContacto { get; set; }
    [Display(Name="Periodo del plan")] public int MesesPlan { get; set; } = 12;
    [Required,Display(Name="Inicio de vigencia")] public DateOnly? Inicio { get; set; }
    [Required,Display(Name="Fin de vigencia")] public DateOnly? Fin { get; set; }
    [EnumDataType(typeof(EstadoContrato)),Display(Name="Estado del contrato")] public EstadoContrato Estado { get; set; }
    [Range(typeof(decimal),"0","9999999999.99"),Display(Name="Importe del periodo (GTQ)")] public decimal ImporteGtq { get; set; }
    [Display(Name="Datos de demostración")] public bool EsDemostracion { get; set; }
    [StringLength(1000),Display(Name="Nota comercial")] public string? NotaComercial { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (string.IsNullOrWhiteSpace(Nombre)) yield return new("Ingresa el nombre del centro educativo.",[nameof(Nombre)]);
        if (MesesPlan is not (1 or 3 or 6 or 12)) yield return new("Selecciona un periodo de 1, 3, 6 o 12 meses.",[nameof(MesesPlan)]);
        if (Fin < Inicio) yield return new("La fecha final debe ser igual o posterior al inicio.",[nameof(Fin)]);
        if (Inicio < new DateOnly(2000,1,1) || Fin > new DateOnly(2200,12,31)) yield return new("Usa fechas entre 2000 y 2200.");
        if (decimal.Round(ImporteGtq,2) != ImporteGtq) yield return new("Usa un importe en quetzales con un máximo de dos decimales.",[nameof(ImporteGtq)]);
    }
}
public sealed record CentrosComercialesViewModel(IReadOnlyList<CentroComercial> Centros, int Pagina, int Paginas, int Total, int Vigentes, int ProximosAVencer);
public sealed class LecturaVersion
{
    [MaxLength(450)] public string UsuarioId { get; set; } = "";
    [MaxLength(32)] public string Version { get; set; } = "";
    public DateTime FechaUtc { get; set; }
}
