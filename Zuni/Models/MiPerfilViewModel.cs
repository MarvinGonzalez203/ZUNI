namespace Zuni.Models;

public sealed class MiPerfilViewModel
{
    public string Nombre { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Carne { get; init; }
    public string? Telefono { get; init; }
    public string? Carrera { get; init; }
    public string? Semestre { get; init; }
    public string? CicloAcademico { get; init; }
    public string? NombreContactoEmergencia { get; init; }
    public string? TelefonoContactoEmergencia { get; init; }
    public string? RelacionContactoEmergencia { get; init; }
    public bool Completo { get; init; }

    // Los mismos tres campos que considera PerfilEstudiante.EstaCompleto.
    // Completo siempre proviene de esa propiedad; el progreso solo es informativo.
    public IReadOnlyList<string> CamposRequeridosFaltantes => new[]
        {
            ("Carné", Carne), ("Teléfono", Telefono), ("Carrera", Carrera)
        }.Where(campo => string.IsNullOrWhiteSpace(campo.Item2))
         .Select(campo => campo.Item1).ToArray();

    public int TotalCamposRequeridos => 3;
    public int CamposCompletados => TotalCamposRequeridos - CamposRequeridosFaltantes.Count;
    public int PorcentajeAvance => (int)Math.Round(100d * CamposCompletados / TotalCamposRequeridos);

    public IReadOnlyList<string> CamposOpcionalesFaltantes => new[]
        {
            ("Semestre", Semestre), ("Ciclo académico", CicloAcademico),
            ("Contacto de emergencia", NombreContactoEmergencia),
            ("Teléfono de emergencia", TelefonoContactoEmergencia),
            ("Relación del contacto", RelacionContactoEmergencia)
        }.Where(campo => string.IsNullOrWhiteSpace(campo.Item2))
         .Select(campo => campo.Item1).ToArray();

    public string? CarneFormateado => Carne is { Length: 10 }
        ? $"{Carne[..4]}-{Carne[4..6]}-{Carne[6..]}"
        : Carne;
}
