using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
namespace Zuni.Seguridad;

public static class TotpZuni
{
    private const string Alfabeto = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
    public static string NuevaClave() => Codificar(RandomNumberGenerator.GetBytes(20));
    public static string Codificar(byte[] bytes)
    {
        var salida = new StringBuilder(); int acumulado = 0, bits = 0;
        foreach (var b in bytes) { acumulado = (acumulado << 8) | b; bits += 8; while (bits >= 5) { bits -= 5; salida.Append(Alfabeto[(acumulado >> bits) & 31]); } }
        if (bits > 0) salida.Append(Alfabeto[(acumulado << (5 - bits)) & 31]);
        return salida.ToString();
    }
    private static byte[] Decodificar(string clave)
    {
        var salida = new List<byte>(); int acumulado = 0, bits = 0;
        foreach (var c in clave) { var n = Alfabeto.IndexOf(c); if (n < 0) throw new FormatException(); acumulado = (acumulado << 5) | n; bits += 5; if (bits >= 8) { bits -= 8; salida.Add((byte)(acumulado >> bits)); } }
        return salida.ToArray();
    }
    public static string Codigo(string clave, long paso)
    {
        Span<byte> contador = stackalloc byte[8]; BinaryPrimitives.WriteInt64BigEndian(contador, paso);
        var hash = HMACSHA1.HashData(Decodificar(clave), contador);
        var offset = hash[^1] & 15;
        var valor = BinaryPrimitives.ReadInt32BigEndian(hash.AsSpan(offset, 4)) & 0x7fffffff;
        return (valor % 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
    }
    public static long? Verificar(string clave, string? codigo, DateTimeOffset ahora, long ultimo)
    {
        if (codigo is null || codigo.Length != 6 || codigo.Any(c => c is < '0' or > '9')) return null;
        var paso = ahora.ToUnixTimeSeconds() / 30;
        foreach (var p in new[] { paso, paso - 1, paso + 1 })
            if (p > ultimo && CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Codigo(clave, p)), Encoding.ASCII.GetBytes(codigo))) return p;
        return null;
    }
}
