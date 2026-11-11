namespace Zuni.Resources;
public sealed record NovedadVersion(string Numero, IReadOnlyList<string> Cambios);
public static class VersionSistema
{
    public const string Actual="1.3.0";
    // Historial de entregas locales; la versión de zuni.lat cambia solo al desplegar.
    public static readonly IReadOnlyList<NovedadVersion> Historial=[
        new(Actual,["Asignar psicólogo desde Acciones en la ficha del estudiante.","Asignar estudiantes desde Acciones en la ficha del psicólogo, con búsqueda y paginación.","Asignaciones exclusivas del Administrador, con historial y protección frente a cambios simultáneos.","Interacción más fluida: revisión de formularios y desplazamiento del menú sin trabajo repetido.","Guías con preguntas de ejemplo, pasos y enlaces a las funciones."]),
        new("1.2.3",["Inicio local desde Visual Studio con configuración privada de desarrollo.","Rutas de claves y bitácora locales conservadas fuera de la carpeta pública."]),
        new("1.2.2",["Cinco requisitos de contraseña visibles y actualizados mientras escribes.","Validación de contraseña al primer ingreso, registro, recuperación y cambio desde Mi cuenta."]),
        new("1.2.1",["Retirada la asignación automática de psicólogo al enviar solicitudes.","Guías ampliadas con explicación, preguntas y accesos directos.","Calendario compartido entre estudiantes asignados y actualización automática de horarios."]),
        new("1.2.0",["Contratos imprimibles en PDF con membrete de ZUNI.","Submenús con tabulación y desplazamiento lateral sin arrastrar la página.","Selector de psicólogos visible y propuestas reunidas en una sola entrada."]),
        new("1.1.1",["Botones con forma, altura y tipografía consistentes en los paneles.","Submenús con sangría y conectores, conservando la alineación de los iconos al comprimir.","La campana conserva las últimas tres versiones y sus cambios."]),
        new("1.1.0",["Menú compacto con iconos propios y posición estable.","Guías por perfil, pasos ampliados y filtros sin recargar la página.","Contraste nocturno, avisos por rol y propuestas del Director revisados.","Control comercial de centros, periodos y vigencias."]),
        new("1.0.0",["Acceso móvil y navegación mejorados.","Cambio de idioma sin recargar formularios.","Centro de transparencia y guías de uso."])
    ];
    public static IReadOnlyList<string> Novedades => Historial[0].Cambios;
}
