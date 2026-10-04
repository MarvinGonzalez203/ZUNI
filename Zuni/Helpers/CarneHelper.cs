namespace Zuni.Helpers;

public static class CarneHelper
{
    public const string PatronParte1 = "^[0-9]{4}$";
    public const string PatronParte2 = "^[0-9]{2}$";
    public const string PatronParte3 = "^[0-9]{2,6}$";
    public const string MensajeFormato = "El carné debe usar el formato 0000-00-00 hasta 0000-00-000000.";

    public static bool TryConstruir(string? parte1, string? parte2, string? parte3, out string carne)
    {
        carne = string.Empty;
        if (parte1 is not { Length: 4 } || parte2 is not { Length: 2 } ||
            parte3 is not { Length: >= 2 and <= 6 }) return false;
        var valor = string.Concat(parte1, parte2, parte3);
        if (!valor.All(c => c is >= '0' and <= '9')) return false;
        carne = valor;
        return true;
    }

    public static string? Formatear(string? valor)
    {
        if (valor is not { Length: >= 8 and <= 12 } ||
            !TryConstruir(valor[..4], valor[4..6], valor[6..], out _)) return valor;
        return $"{valor[..4]}-{valor[4..6]}-{valor[6..]}";
    }
}
