using System.ComponentModel.DataAnnotations;
using Zuni.Services.Institucion;

namespace Zuni.Models.Institucion;

public sealed class ConfiguracionInstitucionViewModel : IValidatableObject
{
    [Required, StringLength(150, MinimumLength = 2), Display(Name = "Nombre de la institución")]
    public string Nombre { get; set; } = string.Empty;
    [EmailAddress, StringLength(254), Display(Name = "Correo de soporte (opcional)")]
    public string? CorreoSoporte { get; set; }
    [EnumDataType(typeof(PoliticaCorreoInstitucion)), Display(Name = "Registro y correo")]
    public PoliticaCorreoInstitucion PoliticaCorreo { get; set; }
    [StringLength(2000), Display(Name = "Dominios autorizados")]
    public string? DominiosAutorizados { get; set; }
    public Guid Revision { get; set; }
    [Required, StringLength(80), Display(Name = "Lema institucional")]
    public string Lema { get; set; } = "Tu espacio educativo";
    [Required, RegularExpression("^#[a-fA-F0-9]{6}$"), Display(Name = "Color de botones y navegación")]
    public string ColorPrincipal { get; set; } = "#cea2fd";
    [Display(Name = "Logotipo PNG (opcional)")]
    public IFormFile? Logotipo { get; set; }
    public bool QuitarLogotipo { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever] public bool TieneLogotipo { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if ((Nombre?.Trim().Length ?? 0) < 2)
            yield return new("Ingresa un nombre de al menos dos caracteres.", [nameof(Nombre)]);
        if (!PoliticaCorreoReglas.TryNormalizarDominios(DominiosAutorizados, out var dominios))
            yield return new("Ingresa hasta 20 dominios válidos, sin @, direcciones web ni comodines.", [nameof(DominiosAutorizados)]);
        else if (PoliticaCorreo == PoliticaCorreoInstitucion.DominiosAutorizados && dominios.Length == 0)
            yield return new("Ingresa al menos un dominio autorizado.", [nameof(DominiosAutorizados)]);
        if (!string.IsNullOrWhiteSpace(CorreoSoporte) && !PoliticaCorreoReglas.EsCorreoValido(CorreoSoporte))
            yield return new("Ingresa un correo de soporte válido.", [nameof(CorreoSoporte)]);
    }
}
