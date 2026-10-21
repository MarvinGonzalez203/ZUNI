using Zuni.Models.Reportes;

namespace Zuni.Services.Reportes;

// Control de divulgación estadística para los indicadores del Director (docs/propuestas-director-privacidad.md).
// Se aplica en el servidor: la vista solo recibe lo que puede publicarse.
//  1. Supresión primaria: ninguna celda publicada tiene entre 1 y Umbral-1 personas.
//  2. Supresión complementaria: las celdas ocultas se agrupan ("Otras") y, si su suma también es
//     pequeña, se agregan al grupo las celdas visibles más pequeñas hasta que el grupo llegue al
//     umbral. Así ninguna celda oculta puede reconstruirse restando las publicadas del total.
//  3. Si no quedan celdas visibles (o el total es pequeño) el desglose no se publica: solo el total.
// No altera ni inventa datos: cada cifra publicada es exacta; solo se omiten las que no pueden protegerse.
public static class SupresionEstadistica
{
    public const int Umbral = EstadisticasDirectorViewModel.UmbralMinimo;

    public static DesgloseProtegido Proteger(IEnumerable<(string Etiqueta, int Cantidad)> celdas, int umbral = Umbral)
    {
        var datos = celdas.Where(c => c.Cantidad > 0).ToList();
        var total = datos.Sum(c => c.Cantidad);
        if (total == 0) return new DesgloseProtegido(true, [], null, 0, 0);
        if (total < umbral) return new DesgloseProtegido(false, [], null, 0, total);
        var visibles = datos.Where(c => c.Cantidad >= umbral).OrderBy(c => c.Cantidad).ThenBy(c => c.Etiqueta, StringComparer.Ordinal).ToList();
        var grupo = datos.Where(c => c.Cantidad < umbral).ToList();
        if (grupo.Count == 0)
            return new DesgloseProtegido(true, Ordenar(visibles), null, 0, total);
        // Supresión complementaria: el grupo debe sumar al menos el umbral y contener al menos dos celdas.
        while ((grupo.Sum(c => c.Cantidad) < umbral || grupo.Count < 2) && visibles.Count > 0)
        {
            grupo.Add(visibles[0]);
            visibles.RemoveAt(0);
        }
        if (visibles.Count == 0)
            return new DesgloseProtegido(false, [], null, 0, total);
        return new DesgloseProtegido(true, Ordenar(visibles), grupo.Sum(c => c.Cantidad), grupo.Count, total);
    }

    // Cifra individual: 0 o al menos el umbral; de 1 a umbral-1 se publica como "menos de 5" (null).
    public static int? Cifra(int cantidad, int umbral = Umbral) => cantidad is > 0 && cantidad < umbral ? null : cantidad;

    private static IReadOnlyList<ConteoAgregado> Ordenar(IEnumerable<(string Etiqueta, int Cantidad)> celdas) =>
        celdas.OrderBy(c => c.Etiqueta, StringComparer.CurrentCulture).Select(c => new ConteoAgregado(c.Etiqueta, c.Cantidad)).ToList();
}
