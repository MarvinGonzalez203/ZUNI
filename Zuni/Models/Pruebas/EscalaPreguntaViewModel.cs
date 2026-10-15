namespace Zuni.Models.Pruebas;
public sealed record EscalaPreguntaViewModel(PreguntaPrueba Pregunta, int Numero, int Total, string NombreCampo, string? Seleccionada, bool SoloLectura = false, bool ValorNumerico = false);
