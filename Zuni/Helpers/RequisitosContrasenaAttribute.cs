using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Zuni.Helpers;

// Compartido por los formularios que definen una contraseña definitiva.
public sealed class RequisitosContrasenaAttribute : ValidationAttribute
{
    public const string Mensaje = "La contraseña debe tener al menos 8 caracteres, un número, una letra mayúscula, una letra minúscula y un símbolo.";

    public RequisitosContrasenaAttribute() : base(Mensaje) { }

    public override bool IsValid(object? value) => value is null || value is string password &&
        password.Length >= 8 && Regex.IsMatch(password, "[0-9]") &&
        Regex.IsMatch(password, @"\p{Lu}") && Regex.IsMatch(password, @"\p{Ll}") &&
        Regex.IsMatch(password, @"[^\p{L}\p{N}\s]");
}
