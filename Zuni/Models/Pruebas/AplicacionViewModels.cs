using System.ComponentModel.DataAnnotations;
namespace Zuni.Models.Pruebas;
public sealed record PruebaAplicacionResumen(Guid Id, string Nombre, string? Subtitulo,int Preguntas,DateTime? InduccionUtc,DateTime? FinalizadaUtc,DateTime? RetiradaUtc);
public sealed record HabilitarPruebasViewModel(IReadOnlyList<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Estudiantes,IReadOnlyList<Prueba> Pruebas);
public sealed class HabilitarPruebaInput
{
    public Guid PerfilEstudianteId { get; set; }
    public Guid PruebaId { get; set; }
    [Range(typeof(bool),"true","true",ErrorMessage="Confirma que explicaste las instrucciones y la escala al estudiante.")] public bool InduccionRealizada { get; set; }
}
public sealed class ContestarPruebaInput
{
    public Guid Id { get; set; }
    public Guid Revision { get; set; }
    public bool Finalizar { get; set; }
    public bool Consentimiento { get; set; }
    public List<RespuestaPruebaInput> Respuestas { get; set; }=[];
}
public sealed class RespuestaPruebaInput { public Guid PreguntaId { get; set; } public Guid? OpcionId { get; set; } }
public sealed record CuestionarioAplicado(AsignacionPrueba Asignacion,Prueba Prueba,IReadOnlyList<RespuestaPrueba> Respuestas);
public sealed record DimensionResumen(string Nombre,int Respondidas,int Total,double? Media);
public sealed record ResumenAplicado(string Estudiante,Prueba Prueba,DateTime? FinalizadaUtc,IReadOnlyList<DimensionResumen> Dimensiones,IReadOnlyList<(string Pregunta,int Valor)> Respuestas);
