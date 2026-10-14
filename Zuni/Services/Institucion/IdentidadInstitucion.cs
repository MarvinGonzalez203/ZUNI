using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Zuni.Services.Institucion;

public static class IdentidadInstitucion
{
    public static string Color(string? color) => Regex.IsMatch(color ?? "", "^#[a-fA-F0-9]{6}$") ? color! : "#cea2fd";
    public static string Texto(string? color)
    {
        var c = Color(color);
        double Canal(int offset) { var v = Convert.ToInt32(c.Substring(offset, 2), 16) / 255d; return v <= .04045 ? v / 12.92 : Math.Pow((v + .055) / 1.055, 2.4); }
        var l = .2126 * Canal(1) + .7152 * Canal(3) + .0722 * Canal(5);
        return (l + .05) / .05 >= 1.05 / (l + .05) ? "#000000" : "#ffffff";
    }
    public static string? Huella(byte[]? logo) => logo is null ? null : Convert.ToHexString(SHA256.HashData(logo));
    public static async Task<byte[]> LeerPng(IFormFile file, CancellationToken ct)
    {
        if (file.Length is < 45 or > 131072) throw new ArgumentException("Usa un PNG de hasta 128 KB y 1024 × 1024 píxeles.");
        await using var stream = file.OpenReadStream(); using var output = new MemoryStream();
        var buffer = new byte[4096]; int read;
        while ((read = await stream.ReadAsync(buffer, ct)) > 0) { if (output.Length + read > 131072) throw new ArgumentException("El logotipo supera 128 KB."); output.Write(buffer, 0, read); }
        var data = output.ToArray();
        if (!PngValido(data)) throw new ArgumentException("El archivo debe ser un PNG válido de hasta 1024 × 1024 píxeles. No se aceptan SVG ni archivos renombrados.");
        return data;
    }
    private static bool PngValido(byte[] data)
    {
        if (data.Length < 45 || !data.AsSpan(0, 8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10})) return false;
        int offset = 8; bool header = false, image = false;
        while (offset + 12 <= data.Length)
        {
            var length = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset, 4));
            if (length > data.Length - offset - 12) return false;
            var type = System.Text.Encoding.ASCII.GetString(data, offset + 4, 4);
            uint crc = 0xffffffff;
            for (int i = offset + 4; i < offset + 8 + length; i++) { crc ^= data[i]; for (int b = 0; b < 8; b++) crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xedb88320 : crc >> 1; }
            if ((crc ^ 0xffffffff) != BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 8 + (int)length, 4))) return false;
            if (!header)
            {
                if (type != "IHDR" || length != 13) return false;
                var w = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 8, 4)); var h = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 12, 4));
                if (w is 0 or > 1024 || h is 0 or > 1024) return false; header = true;
            }
            else if (type == "IHDR") return false;
            if (type == "IDAT") image = true;
            if (type == "IEND") return length == 0 && image && offset + 12 == data.Length;
            offset += 12 + (int)length;
        }
        return false;
    }
}
