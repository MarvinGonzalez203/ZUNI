using System.Globalization;
using System.Text;
namespace Zuni.Resources;

public sealed record ArticuloPortal(string Id, string Programa, TextoPortal Titulo, TextoPortal Resumen,
    IReadOnlyList<TextoPortal> Pasos, string Enlace);
public sealed record BlogPortalVista(string Consulta, string Tema, IReadOnlySet<string> Coincidencias);
public static class BlogDenisCorp
{
    public static readonly IReadOnlyList<ArticuloPortal> Articulos = [
        new("elegir-programa", "general", new("Encuentra el programa para tu día a día", "Find the program for your daily work"),
            new("Conoce qué resuelve cada solución de Denis Corp antes de elegir.", "Discover what each Denis Corp solution does before choosing."),
            [new("Explora las tarjetas de nuestros programas y lee su propósito.", "Explore our program cards and read their purpose."),new("Abre la ficha para distinguir avances disponibles y funciones en preparación.", "Open the product page to distinguish available progress from work in preparation."),new("Si necesitas contratar o resolver una duda, contáctanos e indica el programa que te interesa.", "If you want to subscribe or ask a question, contact us and mention the program you are interested in.")], "/#programas"),
        new("idioma-tema", "general", new("Elige idioma y modo día o noche", "Choose your language and light or dark theme"),
            new("Adapta la lectura del portal conservando tu lugar y tus datos.", "Adapt the portal for comfortable reading while keeping your place and data."),
            [new("Usa el globo de la cabecera para alternar español e inglés.", "Use the globe in the header to switch between Spanish and English."),new("Pulsa el sol o la luna para elegir modo día o noche. El portal recuerda tu elección.", "Select the sun or moon to choose light or dark mode. The portal remembers your choice."),new("La V inferior permite avanzar; al llegar al final te devuelve al principio.", "The bottom chevron scrolls down; when you reach the end it returns you to the top.")], "/"),
        new("zuni-doble-factor", "zuni", new("Protege tu cuenta de ZUNI con 2FA", "Protect your ZUNI account with 2FA"),
            new("Agrega una aplicación autenticadora y guarda tus códigos de recuperación.", "Add an authenticator app and save your recovery codes."),
            [new("Inicia sesión en ZUNI y abre Mi cuenta → Configurar verificación en dos pasos.", "Sign in to ZUNI and open My account → Set up two-step verification."),new("Confirma tu contraseña, prepara la clave en tu autenticador y verifica el código.", "Confirm your password, set up the key in your authenticator and verify the code."),new("Conserva los códigos de recuperación en un lugar seguro. Cada uno funciona una sola vez.", "Keep the recovery codes in a safe place. Each works only once.")], "/DobleFactor"),
        new("zuni-ausencia", "zuni", new("Avisa si no podrás atender una cita", "Give notice if you cannot attend an appointment"),
            new("El psicólogo puede avisar al estudiante y coordinar una nueva fecha.", "The psychologist can notify the student and arrange a new date."),
            [new("Desde la agenda del psicólogo, abre la cita y pulsa No podré asistir.", "From the psychologist's calendar, open the appointment and select I cannot attend."),new("Revisa la fecha y confirma. El estudiante recibe una notificación sin detalles personales del profesional.", "Review the date and confirm. The student receives a notification without the professional's personal details."),new("Cuando puedas atender, usa Consultar aviso y nueva fecha. La cita original conserva su historial.", "When you can attend, use View notice and new date. The original appointment keeps its history.")], "/Psicologo/Agenda"),
        new("academy-aprender", "academy", new("Conoce el aprendizaje de Denis Academy", "Explore learning with Denis Academy"),
            new("Lecciones, audios y actividades para estudiar inglés y español a tu ritmo.", "Lessons, audio and activities for learning English and Spanish at your own pace."),
            [new("Consulta la ficha de Academy para conocer el autoestudio y sus actividades.", "Read Academy's product page to explore self-study and activities."),new("Distingue el progreso local de la sincronización entre dispositivos, que está en preparación.", "Distinguish local progress from cross-device synchronization, which is in preparation."),new("Revisa los avances antes de asumir que el acceso público o el tutor de IA ya están disponibles.", "Review progress before assuming public access or the AI tutor is available.")], "/deniscorp/academy/#avances"),
        new("fulleo-fechas", "fulleo", new("Revisa la fecha y fuente de un precio", "Check a price's date and source"),
            new("Una observación histórica no equivale al precio actual del combustible.", "A historical observation is not the current fuel price."),
            [new("En Fulleo, comprueba la fecha y fuente que acompañan a cada observación.", "In Fulleo, check the date and source attached to each observation."),new("Compara observaciones del mismo período y revisa la evidencia disponible.", "Compare observations from the same period and review available evidence."),new("La muestra del 26 de octubre de 2026 es histórica. Consulta la ficha para conocer avances y publicación pendiente.", "The October 26, 2026 sample is historical. Read the product page for progress and pending publication.")], "/deniscorp/fulleo/#avances"),
        new("pos-primera-venta", "pos", new("Prepara una venta en el POS", "Prepare a sale in the POS"),
            new("Revisa productos, cantidades y pago antes de confirmar una operación.", "Review products, quantities and payment before confirming a transaction."),
            [new("En el POS, busca los productos por nombre, código o barras y agrega las cantidades.", "In the POS, find products by name, code or barcode and add quantities."),new("Revisa la tarifa, el cliente y la forma de pago antes de confirmar.", "Review the price, customer and payment method before confirming."),new("Comprueba el resultado y el número de venta antes de repetir una operación.", "Check the result and sale number before repeating a transaction.")], "/deniscorp/pos/#avances"),
        new("colegios-saas", "colegios", new("Consulta el servicio SaaS para tu colegio", "Ask about the SaaS service for your school"),
            new("Denis Corp Colegios es la propuesta general para instituciones que desean contratar gestión escolar.", "Denis Corp Schools is the general offering for institutions seeking school management services."),
            [new("Identifica las necesidades académicas y administrativas de tu institución.", "Identify your institution's academic and administrative needs."),new("Consulta la propuesta de colegios y sus capacidades previstas.", "Read the school service offering and its planned capabilities."),new("Contáctanos para confirmar disponibilidad, condiciones comerciales y puesta en marcha.", "Contact us to confirm availability, commercial terms and onboarding.")], "/deniscorp/contacto/?programa=colegios&tema=contratacion")
    ];
    public static string Normalizar(string texto) => new(texto.ToLowerInvariant().Normalize(NormalizationForm.FormD)
        .Where(c=>CharUnicodeInfo.GetUnicodeCategory(c)!=UnicodeCategory.NonSpacingMark).ToArray());
    public static string Busqueda(ArticuloPortal a) => Normalizar(a.Titulo+" "+a.Resumen+" "+string.Join(" ",a.Pasos));
    public static BlogPortalVista Buscar(string? q,string? tema)
    {
        q=(q??"").Trim();if(q.Length>80){q=q[..80];if(char.IsHighSurrogate(q[^1]))q=q[..^1];}
        tema=tema=="general"||PortalDenisCorp.Productos.Any(p=>p.Id==tema)?tema!:"todos";
        var palabras=Normalizar(q).Split(' ',StringSplitOptions.RemoveEmptyEntries);
        return new(q,tema,Articulos.Where(a=>(tema=="todos"||a.Programa==tema)&&palabras.All(p=>Busqueda(a).Contains(p))).Select(a=>a.Id).ToHashSet());
    }
}
public static class ContactoDenisCorp
{
    public const string Telefono = "+502 3039 0175";
    public const string WhatsApp = "50230390175";
    public const string Correo = "corporation.denis@gmail.com";
}
