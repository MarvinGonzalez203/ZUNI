namespace Zuni.Models.Institucion;

// Commercial estimates in GTQ, not active subscriptions or a billing gateway.
public sealed record TarifaSaas(string Nombre, int Meses, decimal Total)
{
    public decimal MensualEquivalente => Total / Meses;
    public decimal PrecioMensualBase { get; init; }=199m;
    public decimal Ahorro => PrecioMensualBase * Meses - Total;
    public static IReadOnlyList<TarifaSaas> Propuesta { get; } = Array.AsReadOnly(new[] {
        new TarifaSaas("Mensual",1,199m), new TarifaSaas("Trimestral",3,567m),
        new TarifaSaas("Semestral",6,1075m), new TarifaSaas("Anual",12,2030m)
    });
}
