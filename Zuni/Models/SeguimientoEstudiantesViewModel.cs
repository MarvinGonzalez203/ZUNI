using Zuni.Models.Evaluaciones;
namespace Zuni.Models;

public sealed record EstudianteSeguimiento(Guid Id,string Nombre,string Carne,string? Carrera,bool BigFiveCompleto,DateTime? ProximaCitaUtc=null);
public sealed record SeguimientoEstudiantesViewModel(IReadOnlyList<EstudianteSeguimiento> Estudiantes,int Total,int Completados,
    string Busqueda,string Filtro,int Pagina,int Paginas,EstudianteSeguimiento? Seleccionado,ResumenBigFive? Resultado);
