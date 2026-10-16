using System.Text.Json;
using System.Text.Json.Serialization;
namespace Zuni.Seguridad;

public static class JsonSeguridad
{
    // PostgreSQL conserva microsegundos; normalizar ANTES de firmar evita diferencias por ticks de .NET.
    public static DateTime UtcMicrosegundos(DateTime value)=>new(value.Ticks-value.Ticks%10,DateTimeKind.Utc);
    private sealed class FechaUtc : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader,Type type,JsonSerializerOptions options)=>DateTime.SpecifyKind(reader.GetDateTime(),DateTimeKind.Utc);
        public override void Write(Utf8JsonWriter writer,DateTime value,JsonSerializerOptions options)=>writer.WriteStringValue(UtcMicrosegundos(value).ToString("O"));
    }
    private static readonly JsonSerializerOptions Opciones=new(){Converters={new FechaUtc()}};
    public static string Serializar(object dato)=>JsonSerializer.Serialize(dato,Opciones);
}
