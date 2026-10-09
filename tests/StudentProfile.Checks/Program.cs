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
var eleven = Valid(); eleven.CarneParte3 = "15193";
if (!IsValid(eleven)) throw new Exception("Carné de 11 dígitos rechazado");
if (new MiPerfilViewModel { Carne="74902015193" }.CarneFormateado != "7490-20-15193") throw new Exception("Formato de 11 dígitos incorrecto");
eleven.CarneParte3="151933";
if (IsValid(eleven)) throw new Exception("Se aceptó carné demasiado largo");
Console.WriteLine("Correcto: carnés de 10/11 dígitos y rechazo de 12.");
var csv = Zuni.Services.EstudiantesCsv.Leer("Nombre,Correo,Carne,Carrera\n\"Alumno, Uno\",uno@miumg.edu.gt,7490-20-15193,Ingeniería\nAlumno Dos,dos@miumg.edu.gt,2020010001,");
if(csv.Errores.Count!=0 || csv.Filas.Count!=2 || csv.Filas[0].Carne!="74902015193") throw new Exception("CSV válido rechazado");
foreach(var invalid in new[]{"Nombre,Correo,Carne,Carrera\nUno,uno@miumg.edu.gt,2020010001,X\nOtro,UNO@miumg.edu.gt,2020010002,X", "Nombre,Correo,Carne,Carrera\nUno,uno@gmail.com,2020010001,X", "Nombre,Correo,Carne,Carrera\nUno,uno@miumg.edu.gt,abc,X", "Nombre,Correo,Carne,Carrera\n\"Sin cierre", "Otra,Cabecera"})
if(Zuni.Services.EstudiantesCsv.Leer(invalid).Errores.Count==0) throw new Exception("CSV inválido aceptado");
Console.WriteLine("Correcto: CSV con comillas, 10/11 dígitos, duplicados, dominio, formato y encabezados.");
