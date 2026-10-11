using System.ComponentModel.DataAnnotations;
namespace Zuni.Models.Atencion;
public sealed class SolicitarAtencionViewModel : IValidatableObject
{
    [Display(Name = "Confirmo que soy mayor de edad")] public bool EsMayorEdad { get; set; }
    [Range(18, 120, ErrorMessage = "Este flujo admite edades entre 18 y 120 años.")]
    public int Edad { get; set; }
    [StringLength(50)] public string? Sexo { get; set; }
    [Required, StringLength(300), Display(Name = "Dirección")] public string Direccion { get; set; } = string.Empty;
    [Required, StringLength(80), Display(Name = "Idioma preferido")] public string IdiomaPreferido { get; set; } = string.Empty;
    [Required, StringLength(2000), Display(Name = "Motivo de consulta")] public string MotivoConsulta { get; set; } = string.Empty;
    [EnumDataType(typeof(TipoIngresoAtencion)), Display(Name = "Tipo de ingreso")] public TipoIngresoAtencion TipoIngreso { get; set; }
    [StringLength(150), Display(Name = "Quién refiere")] public string? NombreReferente { get; set; }
    [StringLength(1000), Display(Name = "Motivo de referencia")] public string? MotivoReferencia { get; set; }
    [StringLength(1500), Display(Name = "Consideraciones culturales o personales (opcional)")] public string? ConsideracionesAtencion { get; set; }
    public bool AceptaConsentimiento { get; set; }
    public List<ContactoSolicitudViewModel> Contactos { get; set; } = [new(), new(), new()];
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!EsMayorEdad) yield return new("Debes confirmar que eres mayor de edad.", [nameof(EsMayorEdad)]);
        if (!AceptaConsentimiento) yield return new("Debes leer y aceptar el consentimiento informado.", [nameof(AceptaConsentimiento)]);
        if (TipoIngreso == TipoIngresoAtencion.Referencia)
        {
            if (string.IsNullOrWhiteSpace(NombreReferente)) yield return new("Indica quién refiere.", [nameof(NombreReferente)]);
            if (string.IsNullOrWhiteSpace(MotivoReferencia)) yield return new("Indica el motivo de referencia.", [nameof(MotivoReferencia)]);
        }
        if (Contactos is null || Contactos.Count > 3 || !Contactos.Any(c => c is not null && !c.Vacio))
            yield return new("Proporciona entre uno y tres contactos de emergencia.", [nameof(Contactos)]);
        if (Contactos is not null)
            for (var i = 0; i < Contactos.Count; i++)
            {
                var c = Contactos[i];
                if (c is null) { yield return new("Contacto inválido.", [nameof(Contactos)]); continue; }
                if (c.Vacio) continue;
                if (string.IsNullOrWhiteSpace(c.Nombre)) yield return new("Ingresa el nombre del contacto.", [$"Contactos[{i}].Nombre"]);
                if (string.IsNullOrWhiteSpace(c.Relacion)) yield return new("Ingresa la relación.", [$"Contactos[{i}].Relacion"]);
                if (string.IsNullOrWhiteSpace(c.Telefono)) yield return new("Ingresa el teléfono.", [$"Contactos[{i}].Telefono"]);
            }
    }
}
public sealed class ContactoSolicitudViewModel
{
    [StringLength(150)] public string? Nombre { get; set; }
    [StringLength(60), Display(Name = "Relación")] public string? Relacion { get; set; }
    [RegularExpression(@"^[0-9]{8}$", ErrorMessage = "El teléfono debe contener exactamente 8 dígitos."), Display(Name = "Teléfono")]
    public string? Telefono { get; set; }
    public bool Vacio => string.IsNullOrWhiteSpace(Nombre) && string.IsNullOrWhiteSpace(Relacion) && string.IsNullOrWhiteSpace(Telefono);
}
