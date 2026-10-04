using System.ComponentModel.DataAnnotations;
using Zuni.Helpers;
using Zuni.Models;
using Zuni.Models.Administrador;

// Pruebas sin acceso a PostgreSQL ni paquetes de pruebas externos.
var casos = new (string Carne, bool Valido)[]
{
    ("7490-17-5760", true), ("7490-17-12071", true),
    ("7490-20-15193", true), ("7490-22-77", true),
    ("749-17-5760", false), ("7490-1-5760", false),
    ("7490-17-7", false), ("7490-17-1234567", false),
    ("7490-AA-1234", false), ("7490-17-12A4", false),
    ("0000-00-00", true), ("0000-00-000000", true),
    ("７４９０-17-5760", false), ("7490-١٧-5760", false),
    ("7490-17-１２", false), ("7490-17-12\n", false),
    ("7490-17- 12", false), ("7490-17-", false)
};
int comprobaciones = 0;
void Comprobar(bool condicion, string descripcion)
{
    comprobaciones++;
    if (!condicion) throw new InvalidOperationException(descripcion);
}
bool Validar(object modelo) => Validator.TryValidateObject(
    modelo, new ValidationContext(modelo), new List<ValidationResult>(), true);

foreach (var (carne, esperado) in casos)
{
    var partes = carne.Split('-');
    var valido = CarneHelper.TryConstruir(partes[0], partes[1], partes[2], out var normalizado);
    Comprobar(valido == esperado, $"Validador: {carne}");
    var registro = new RegisterViewModel
    {
        FullName = "Usuario de prueba", Email = "prueba@miumg.edu.gt",
        Telefono = "12345678", Password = "Temporal123!", ConfirmPassword = "Temporal123!",
        CarneParte1 = partes[0], CarneParte2 = partes[1], CarneParte3 = partes[2]
    };
    Comprobar(Validar(registro) == esperado, $"Registro: {carne}");
    var perfil = new CompletarPerfilEstudianteViewModel
    {
        Carrera = "Carrera de prueba", SolicitarCarne = true,
        CarneParte1 = partes[0], CarneParte2 = partes[1], CarneParte3 = partes[2]
    };
    // Las partes son opcionales en el ViewModel; el controlador exige el carné si falta.
    var perfilValido = Validar(perfil) && CarneHelper.TryConstruir(
        perfil.CarneParte1, perfil.CarneParte2, perfil.CarneParte3, out _);
    Comprobar(perfilValido == esperado, $"Completar perfil: {carne}");
    var administrador = new AgregarUsuarioViewModel
    {
        NombreCompleto = "Usuario de prueba", Correo = "prueba@miumg.edu.gt", Rol = "Estudiante",
        ContrasenaTemporal = "Temporal123!", ConfirmarContrasenaTemporal = "Temporal123!",
        CarneParte1 = partes[0], CarneParte2 = partes[1], CarneParte3 = partes[2]
    };
    Comprobar(Validar(administrador) == esperado, $"Administrador: {carne}");
    if (esperado)
    {
        Comprobar(normalizado == string.Concat(partes), $"Normalización sin pérdida: {carne}");
        Comprobar(new MiPerfilViewModel { Carne = normalizado }.CarneFormateado == carne,
            $"Presentación: {carne}");
    }
    else Comprobar(normalizado == string.Empty, $"No devolver carné inválido: {carne}");
    Console.WriteLine($"OK: {carne.Replace("\n", "\\n")} => {(esperado ? "aceptado" : "rechazado")} en los tres flujos");
}
Comprobar(!CarneHelper.TryConstruir(null, "17", "77", out _), "Null rechazado");
Comprobar(CarneHelper.Formatear(null) is null, "Presentación null");
Comprobar(CarneHelper.Formatear("invalido") == "invalido", "Dato histórico intacto");
Comprobar(Validar(new AgregarUsuarioViewModel
{
    NombreCompleto = "Usuario de prueba", Correo = "prueba@miumg.edu.gt", Rol = "Director",
    ContrasenaTemporal = "Temporal123!", ConfirmarContrasenaTemporal = "Temporal123!"
}), "No exigir carné a otros roles");
Console.WriteLine($"Resultado: {casos.Length} casos, {comprobaciones} comprobaciones correctas.");
