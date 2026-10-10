using Zuni.Models.Evaluaciones;
namespace Zuni.Models;

public sealed record EstudianteSeguimiento(Guid Id,string Nombre,string Carne,string? Carrera,bool BigFiveCompleto,DateTime? ProximaCitaUtc=null,string EstadoAgenda="Sin cita futura");
public sealed record SeguimientoEstudiantesViewModel(IReadOnlyList<EstudianteSeguimiento> Estudiantes,int Total,int Completados,
    string Busqueda,string Filtro,int Pagina,int Paginas,EstudianteSeguimiento? Seleccionado,ResumenBigFive? Resultado)
{
    public IReadOnlyList<CitaResumen> Citas {get;init;}=[];
    public int Proximas {get;init;}
}
