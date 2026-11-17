using Zuni.Localizacion;
namespace Zuni.Resources;

// Catálogo público compartido por portada, fichas y favicons. Solo información de producto.
public sealed record TextoPortal(string Es, string En)
{
    public override string ToString() => Idioma.EsIngles ? En : Es;
}
public sealed record AvanceProducto(TextoPortal Titulo, TextoPortal Descripcion);
public sealed record ProductoDenisCorp(string Id, string Nombre, string Icono, string TipoIcono,
    string Version, TextoPortal Categoria, TextoPortal Descripcion, TextoPortal Estado,
    IReadOnlyList<AvanceProducto> Avances, IReadOnlyList<TextoPortal> Pendientes)
{
    public string Ruta => $"/deniscorp/{Id}/";
}
public static class PortalDenisCorp
{
    public static string T(string es, string en) => Idioma.EsIngles ? en : es;
    public static readonly IReadOnlyList<ProductoDenisCorp> Productos = [
        new("zuni", "ZUNI", "/favicon-96.png", "image/png", VersionSistema.Actual,
            new("Bienestar y acompañamiento", "Wellbeing and support"),
            new("Acompañamiento estudiantil que conecta a estudiantes, psicólogos e instituciones.", "Student support connecting students, psychologists and institutions."),
            new("Conoce el programa y accede a tu cuenta", "Explore the program and access your account"), [], []),
        new("academy", "Denis Academy", "/images/productos/academy.svg", "image/svg+xml", "1.2.1",
            new("Aprendizaje de idiomas", "Language learning"),
            new("Aprende inglés y español con lecciones y actividades interactivas, a tu ritmo.", "Learn English and Spanish with interactive lessons and activities at your own pace."),
            new("Avances disponibles · publicación pendiente", "Progress available · publication pending"),
            [new(new("Lecciones y actividades", "Lessons and activities"), new("Autoestudio de inglés y español con audios, escritura, repaso y minijuegos.", "English and Spanish self-study with audio, writing, review and mini-games.")),
             new(new("Progreso en el dispositivo", "On-device progress"), new("La aplicación conserva el avance local y permite continuar con contenido preparado para uso sin conexión.", "The app keeps local progress and supports continuing with content prepared for offline use."))],
            [new("Acceso público desde este portal y verificación del despliegue.", "Public access from this portal and deployment verification."),new("Cuentas, sincronización entre dispositivos y tutor de IA conectado.", "Accounts, cross-device synchronization and a connected AI tutor.")]),
        new("fulleo", "Fulleo", "/images/productos/fulleo.svg", "image/svg+xml", "0.8.1",
            new("Precios de combustibles", "Fuel prices"),
            new("Consulta precios de gasolina con su fecha y fuente para informarte antes de llenar el tanque.", "Check gasoline prices with their date and source before filling your tank."),
            new("Ver avances del programa", "View product progress"),
            [new(new("Consulta y comparación", "Search and comparison"),new("Catálogo de estaciones y comparación de observaciones de precios, con fecha y fuente visibles.", "Station catalog and comparison of price observations, with visible dates and sources.")),
             new(new("Evidencia y revisión", "Evidence and review"),new("Importación de lotes con PDF de respaldo, aprobación o rechazo y registro de auditoría.", "Batch imports with supporting PDFs, approval or rejection and audit records.")),
             new(new("Español e inglés", "Spanish and English"),new("Interfaz bilingüe, cambio de tema y avisos de nuevas versiones.", "Bilingual interface, theme switching and release notifications.")),
             new(new("Preparación para NAS", "NAS preparation"),new("Servidor independiente con persistencia local, acceso administrativo y pruebas de importación y reinicio documentadas.", "Standalone server with local persistence, administrative access and documented import and restart tests."))],
            [new("Publicación y comprobación de acceso desde zuni.lat.", "Publication and access verification from zuni.lat."),new("Suministro diario automatizado de precios; actualmente la recopilación requiere revisión.", "An automated daily price feed; collection currently requires review."),new("Cobros, historial Plus y predicciones de precios.", "Payments, Plus history and price predictions.")]),
        new("pos", "Denis Corp POS", "/images/productos/deniscorp-32.png", "image/png", "1.22.0",
            new("Ventas y negocios", "Sales and business"),
            new("Organiza ventas, inventario, caja y cuentas de tu negocio desde un mismo programa.", "Organize sales, inventory, cash and business accounts in one program."),
            new("Conoce sus funciones y avances", "Explore its features and progress"),
            [new(new("Venta e inventario", "Sales and inventory"),new("Punto de venta, productos, existencias, clientes y proveedores.", "Point of sale, products, stock, customers and suppliers.")),
             new(new("Caja y cuentas", "Cash and accounts"),new("Control de caja, saldos y comprobantes, con funciones por tipo de negocio.", "Cash control, balances and receipts, with business-specific features."))],
            [new("Integración del acceso público desde este portal.", "Integration of public access from this portal.")]),
        new("colegios", "Denis Corp Colegios", "/images/productos/colegios.svg", "image/svg+xml", "SaaS",
            new("Colegios y gestión escolar", "Schools and administration"),
            new("Una propuesta SaaS para que distintos colegios contraten la gestión académica y administrativa de su institución.", "A SaaS offering for different schools to subscribe to academic and administrative management for their institution."),
            new("Conoce el programa para colegios", "Explore the school program"),
            [new(new("Organización académica", "Academic organization"),new("La propuesta reúne matrículas, aulas, horarios, notas y asistencia para el trabajo de cada colegio.", "The offering brings together enrollment, classrooms, schedules, grades and attendance for each school.")),
             new(new("Gestión administrativa", "Administrative management"),new("Herramientas previstas para pagos, saldos y expedientes, con funciones según el perfil de cada usuario.", "Planned tools for payments, balances and records, with features based on each user's role."))],
            [new("Publicación, condiciones comerciales y validación del servicio para varias instituciones.", "Publication, commercial terms and validation of the service for multiple institutions."),new("Configuración y puesta en marcha de cada colegio.", "Configuration and onboarding for each school.")])
    ];
    public static ProductoDenisCorp Obtener(string id) => Productos.Single(p => p.Id == id);
}
