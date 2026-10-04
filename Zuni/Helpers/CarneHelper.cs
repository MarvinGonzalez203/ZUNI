namespace Zuni.Helpers;

/// <summary>Regla provisional de carné, pendiente de confirmación institucional.</summary>
public static class CarneHelper
{
    public const string PatronParte1 = @"^[0-9]{4}$";
    public const string PatronParte2 = @"^[0-9]{2}$";
    public const string PatronParte3 = @"^[0-9]{2,6}$";
    public const string MensajeFormato =
        "El carné debe tener 4 dígitos, 2 dígitos y entre 2 y 6 dígitos, usando únicamente números del 0 al 9 (0000-00-00 hasta 0000-00-000000).";

    public static bool TryConstruir(
        string? parte1, string? parte2, string? parte3, out string carne)
    {
        carne = string.Empty;
        if (!EsBloqueValido(parte1, 4, 4) ||
            !EsBloqueValido(parte2, 2, 2) ||
            !EsBloqueValido(parte3, 2, 6))
            return false;

        carne = string.Concat(parte1, parte2, parte3);
        return true;
    }

    public static string? Formatear(string? carne)
    {
        // Los valores históricos inválidos se muestran intactos, sin rellenar ni truncar.
        if (carne is null || carne.Length is < 8 or > 12 ||
            !TryConstruir(carne[..4], carne[4..6], carne[6..], out _))
            return carne;

        return $"{carne[..4]}-{carne[4..6]}-{carne[6..]}";
    }

    private static bool EsBloqueValido(string? parte, int minimo, int maximo) =>
        parte is not null && parte.Length >= minimo && parte.Length <= maximo &&
        parte.All(c => c >= '0' && c <= '9');
}
