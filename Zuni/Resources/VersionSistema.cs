namespace Zuni.Resources;
public sealed record NovedadVersion(string Numero, IReadOnlyList<string> Cambios);
public static class VersionSistema
{
    public const string Actual="1.5.0";
    // Historial de entregas locales; la versión de zuni.lat cambia solo al desplegar.
    public static readonly IReadOnlyList<NovedadVersion> Historial=[
        new(Actual,["Verificación en dos pasos con aplicación autenticadora, códigos de recuperación y bloqueo por intentos.","Aviso de ausencia del profesional con notificación al estudiante y nueva programación vinculada al historial.","Portada de Denis Corp con tarjetas de ZUNI, Denis Academy y Fulleo; accesos actuales de ZUNI conservados.","Guías ampliadas con seguridad del acceso y ausencias del profesional."]),
        new("1.4.0",["Guías con búsqueda tolerante a faltas, abreviaturas y periodos, y accesos en todas las funciones disponibles.","Planes de centros con tarifa y vigencia calculadas desde hoy al contratar o renovar.","Acceso directo a Google Meet para crear enlaces individuales por cita.","Notas profesionales privadas protegidas por un PIN de cuatro dígitos, bloqueo por intentos y recuperación por Brevo.","Separación corregida entre las tarjetas del tablero del Director."]),
        new("1.3.3",["Botones legibles al pasar el cursor en modo noche con colores personalizados.","Calendario móvil con días de altura uniforme, dos líneas de texto y acciones alineadas.","Desplegables con chevrones en V en lugar de triángulos.","PDF de atención y contrato con encabezado y pie unificados, tarjetas redondeadas y logo con círculos de cristal.","Sobre este documento conserva su posición sobre el pie, incluso en documentos extensos."]),
        new("1.3.2",["Registro completo de atención imprevista sin exigir una solicitud posterior del estudiante.","Notas privadas, resultado compartido y auditoría vinculados a la atención del profesional.","El psicólogo puede programar la próxima cita sin una solicitud del estudiante.","No se generan solicitudes ni consentimientos ficticios."]),
        new("1.3.1",["Aviso específico cuando el estudiante aún debe cambiar su contraseña temporal.","Confirmación del horario guardado y apertura del día y actividad registrados.","Avisos claros cuando falta solicitud o consentimiento para completar el registro profesional.","Los errores de requisitos y horario conservan los datos escritos en el formulario."]),
        new("1.3.0",["Asignar psicólogo desde Acciones en la ficha del estudiante.","Asignar estudiantes desde Acciones en la ficha del psicólogo, con búsqueda y paginación.","Asignaciones exclusivas del Administrador, con historial y protección frente a cambios simultáneos.","Interacción más fluida: revisión de formularios y desplazamiento del menú sin trabajo repetido.","Guías con preguntas de ejemplo, pasos y enlaces a las funciones."]),
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
