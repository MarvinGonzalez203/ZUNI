using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
namespace Zuni.Models.Institucion;

public sealed class ConfiguracionTarifas
{
    public int Id { get; set; }=1;
    [Precision(12,2)] public decimal Mensual { get; set; }=199m;
    [Precision(12,2)] public decimal Trimestral { get; set; }=567m;
    [Precision(12,2)] public decimal Semestral { get; set; }=1075m;
    [Precision(12,2)] public decimal Anual { get; set; }=2030m;
    [ConcurrencyCheck] public Guid Revision { get; set; }
}

public sealed class EditarTarifasInput : IValidatableObject
{
    public Guid Revision { get; set; }
    [Range(typeof(decimal),"0.01","999999.99"),Display(Name="Total de 1 mes (GTQ)")] public decimal Mensual { get; set; }
    [Range(typeof(decimal),"0.01","999999.99"),Display(Name="Total de 3 meses (GTQ)")] public decimal Trimestral { get; set; }
    [Range(typeof(decimal),"0.01","999999.99"),Display(Name="Total de 6 meses (GTQ)")] public decimal Semestral { get; set; }
    [Range(typeof(decimal),"0.01","999999.99"),Display(Name="Total de 12 meses (GTQ)")] public decimal Anual { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        foreach(var (nombre,valor) in new[]{(nameof(Mensual),Mensual),(nameof(Trimestral),Trimestral),(nameof(Semestral),Semestral),(nameof(Anual),Anual)})
            if(decimal.Round(valor,2)!=valor)yield return new("Usa un importe en quetzales con un máximo de dos decimales.",[nombre]);
    }
}
