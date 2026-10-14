using System.ComponentModel.DataAnnotations;

namespace Zuni.Models.Pruebas;

public sealed class Prueba
{
    [MaxLength(200)] public string? Subtitulo { get; set; }
    public int? Nivel { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(150)] public string Nombre { get; set; } = "";
    [MaxLength(2000)] public string Descripcion { get; set; } = "";
    [MaxLength(2000)] public string Instrucciones { get; set; } = "";
    [MaxLength(500)] public string Fuente { get; set; } = "";
    [MaxLength(80)] public string Idioma { get; set; } = "Español";
    public bool Activa { get; set; }
    public Guid Revision { get; set; } = Guid.NewGuid();
    public DateTime CreadaUtc { get; set; } = DateTime.UtcNow;
    public List<DimensionPrueba> Dimensiones { get; set; } = [];
    public List<PreguntaPrueba> Preguntas { get; set; } = [];
}

public sealed class DimensionPrueba
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PruebaId { get; set; }
    [MaxLength(100)] public string Nombre { get; set; } = "";
}

public sealed class PreguntaPrueba
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PruebaId { get; set; }
    public Guid DimensionId { get; set; }
    public int Orden { get; set; }
    [MaxLength(1000)] public string Texto { get; set; } = "";
    public List<OpcionPrueba> Opciones { get; set; } = [];
}

public sealed class OpcionPrueba
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PreguntaPruebaId { get; set; }
    public int Orden { get; set; }
    [MaxLength(300)] public string Texto { get; set; } = "";
    public int Valor { get; set; }
}

public sealed class AsignacionPrueba
{
    public string? PsicologoInductorId { get; set; }
    public Guid? CitaInduccionId { get; set; }
    public DateTime? InduccionUtc { get; set; }
    public DateTime? FinalizadaUtc { get; set; }
    public DateTime? RetiradaUtc { get; set; }
    public Guid Revision { get; set; } = Guid.NewGuid();
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PruebaId { get; set; }
    public Guid PerfilEstudianteId { get; set; }
    public string DirectorUsuarioId { get; set; } = "";
    public DateTime AsignadaUtc { get; set; } = DateTime.UtcNow;
}
public sealed class RespuestaPrueba
{
    public Guid AsignacionPruebaId { get; set; }
    public Guid PreguntaPruebaId { get; set; }
    public Guid OpcionPruebaId { get; set; }
}
