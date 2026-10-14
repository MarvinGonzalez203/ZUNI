namespace Zuni.Models.Institucion;

public enum PoliticaCorreoInstitucion { DominiosAutorizados, CualquierCorreo, SoloAltasAdministradas }

// Una configuración por instalación; no se comparten instituciones ni cuentas.
public sealed class ConfiguracionInstitucion
{
    public int Id { get; set; } = 1;
    public string Nombre { get; set; } = "Institución educativa";
    public string? CorreoSoporte { get; set; }
    public string Lema { get; set; } = "Tu espacio educativo";
    public string ColorPrincipal { get; set; } = "#cea2fd";
    public byte[]? LogotipoPng { get; set; }
    public PoliticaCorreoInstitucion PoliticaCorreo { get; set; }
    public string DominiosAutorizados { get; set; } = "miumg.edu.gt";
    public Guid Revision { get; set; }
}
