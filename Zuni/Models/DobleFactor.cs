using System.ComponentModel.DataAnnotations;
namespace Zuni.Models;

// El secreto se cifra con Data Protection; los códigos de respaldo solo se guardan como hash.
public sealed class DobleFactor
{
    [Key] public string UsuarioId { get; set; } = "";
    public string? SecretoProtegido { get; set; }
    public string? PendienteProtegido { get; set; }
    public DateTime? PendienteHastaUtc { get; set; }
    public string CodigosHash { get; set; } = "[]";
    public long UltimoPaso { get; set; } = -1;
    public int Fallos { get; set; }
    public DateTime? BloqueadoHastaUtc { get; set; }
    public string? RetoHash { get; set; }
    public string? RetoStamp { get; set; }
    public DateTime? RetoHastaUtc { get; set; }
    public bool Recordar { get; set; }
    public string? Retorno { get; set; }
    [ConcurrencyCheck] public Guid Revision { get; set; } = Guid.NewGuid();
}

public sealed class DobleFactorInput
{
    [Required, StringLength(128), DataType(DataType.Password)] public string Contrasena { get; set; } = "";
    [StringLength(32)] public string? Codigo { get; set; }
}
public sealed record DobleFactorVista(bool Activo, string? Clave = null, string? Error = null, IReadOnlyList<string>? Codigos = null);
