using System.Security.Cryptography;

namespace Zuni.Helpers;

// Genera contraseñas temporales con un generador criptográfico. Nunca se registran ni se guardan:
// solo se muestran una vez y se almacenan como hash.
public static class ContrasenaTemporalHelper
{
    // Sin caracteres ambiguos (0/O, 1/l/I) para facilitar el dictado.
    private const string Mayusculas = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Minusculas = "abcdefghijkmnpqrstuvwxyz";
    private const string Digitos = "23456789";
    private const string Simbolos = "!@#$%*?-_";
    public const int Longitud = 16;

    public static string Generar()
    {
        var todos = Mayusculas + Minusculas + Digitos + Simbolos;
        var caracteres = new char[Longitud];
        // Garantiza al menos un carácter de cada grupo.
        caracteres[0] = Elegir(Mayusculas);
        caracteres[1] = Elegir(Minusculas);
        caracteres[2] = Elegir(Digitos);
        caracteres[3] = Elegir(Simbolos);
        for (var i = 4; i < Longitud; i++)
            caracteres[i] = Elegir(todos);
        // Mezcla Fisher-Yates para que los grupos obligatorios no queden al inicio.
        for (var i = Longitud - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (caracteres[i], caracteres[j]) = (caracteres[j], caracteres[i]);
        }
        return new string(caracteres);
    }

    private static char Elegir(string grupo) => grupo[RandomNumberGenerator.GetInt32(grupo.Length)];
}
