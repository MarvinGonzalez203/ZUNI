using System.ComponentModel.DataAnnotations;
using Zuni.Models;

static CompletarPerfilEstudianteViewModel Valid() => new()
{
    CarneParte1 = "2026", CarneParte2 = "01", CarneParte3 = "0001",
    Telefono = "12345678", Carrera = "Ingeniería"
};
static bool IsValid(CompletarPerfilEstudianteViewModel model) =>
    Validator.TryValidateObject(model, new ValidationContext(model), new List<ValidationResult>(), true);
var cases = new (string Name, Action<CompletarPerfilEstudianteViewModel> Change)[]
{
    ("carné vacío", m => m.CarneParte1 = null),
    ("segundo bloque vacío", m => m.CarneParte2 = ""),
    ("último bloque vacío", m => m.CarneParte3 = null),
    ("carné no ASCII", m => m.CarneParte1 = "٢٠٢٦"),
    ("teléfono vacío", m => m.Telefono = null),
    ("teléfono corto", m => m.Telefono = "123"),
    ("teléfono no ASCII", m => m.Telefono = "١٢٣٤٥٦٧٨"),
    ("carrera en blanco", m => m.Carrera = "   "),
    ("carrera demasiado larga", m => m.Carrera = new string('a', 151)),
    ("semestre fuera de rango", m => m.Semestre = "11"),
    ("ciclo fuera de rango", m => m.CicloAcademico = "3"),
    ("teléfono de emergencia inválido", m => m.TelefonoContactoEmergencia = "123")
};
if (!IsValid(Valid())) throw new Exception("El perfil válido con opcionales vacíos debe aceptarse.");
foreach (var test in cases)
{
    var model = Valid();
    test.Change(model);
    if (IsValid(model)) throw new Exception($"Se aceptó: {test.Name}");
}
var complete = new MiPerfilViewModel { Carne = "2026010001", Telefono = "12345678", Carrera = "Ingeniería" };
if (complete.PorcentajeAvance != 100 || complete.CamposOpcionalesFaltantes.Count != 5)
    throw new Exception("Los opcionales no deben reducir el progreso requerido.");
if (new MiPerfilViewModel().PorcentajeAvance != 0 || complete.CarneFormateado != "2026-01-0001")
    throw new Exception("Progreso o formato incorrecto.");
Console.WriteLine($"Correcto: {cases.Length + 3} comprobaciones de validación y presentación.");
