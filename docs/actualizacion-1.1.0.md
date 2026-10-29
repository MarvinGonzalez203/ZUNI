# ZUNI 1.1.0

Entrega local de Director V2 del 28/10/2026 (Guatemala). Incorpora funciones compatibles y correcciones posteriores a 1.0.0; por eso aumenta el número central de versión. `Zuni.csproj` y `VersionSistema.Actual` coinciden en 1.1.0. No se publicó en zuni.lat.

## Cambios visibles

- Cabecera única en escritorio: logo de ZUNI, control de compresión fuera del menú, Mi espacio y ubicación actual, controles de cuenta.
- La compresión conserva el ancho interno del contenido lateral y su desplazamiento: oculta etiquetas sin reordenar filas. El logo y los iconos conservan sus posiciones. Los submenús tienen iconos SVG propios para sus funciones; se retiró el sustituto genérico en forma de flecha. Los SVG se definen en `_DashboardIcon.cshtml`, sin fuentes ni descargas de iconos externas.
- Se conservan los desplegables de las secciones, la ayuda sobre cada icono y la V que indica contenido restante del menú. El menú móvil continúa usando su apertura lateral.
- Guías accesibles desde todos los perfiles, encima de los controles inferiores. El servidor limita cada perfil a sus instrucciones y las generales; solo SuperAdmin puede filtrar otros roles. El catálogo contiene 45 guías, con instrucciones específicas para modo noche, colores, menú, búsqueda de ayuda, ventanas, citas y contratos.
- Los filtros y la búsqueda de guías actualizan solo los resultados mediante una petición autenticada; no recargan la página. Se conserva la barra superior, el menú y el formulario. Las respuestas se comprueban contra la sesión y el idioma. Las peticiones obsoletas se cancelan y los errores se muestran sin descartar los resultados anteriores.
- Correcciones de contraste en agenda y distintivos del modo oscuro, alineación de tarjetas y ubicación de la V de los modales en una fila reservada del pie.
- Acceso directo del Director a Proponer cita y Mis propuestas, manteniendo las restricciones de privacidad. Las notificaciones de citas requieren tanto el destinatario como la relación actual de la cuenta con la cita. Las novedades generales de versión siguen siendo comunes al producto, con lectura individual.

## Campana y versiones

La campana muestra las notas de 1.1.0: navegación compacta, guías y filtro, contraste y avisos por rol, gestión comercial. Una lectura de 1.0.0 no marca como leída 1.1.0. La lectura se guarda exclusivamente para el usuario autenticado; se verificó antiforgery e idempotencia.

Para entregas posteriores, actualizar conjuntamente el proyecto, el número visible y las notas: parche para correcciones compatibles, menor para nuevas funciones compatibles y mayor para cambios incompatibles. No cambiar números según una cuenta arbitraria de mensajes o commits.

## UMG en localhost

Con autorización explícita del responsable se guardó y verificó la tarjeta **UMG · Universidad Mariano Gálvez**, anual del **01/10/2026 al 30/09/2027**, activa y sin marca de demostración. Se respaldó antes la base local y se utilizó el servicio comercial con sus validaciones, transacción y auditoría. No se cambiaron cuentas, contraseñas, alumnos ni claves. El importe proviene del catálogo como referencia y la nota comercial aclara que no acredita cobro ni pago.

La tarjeta es un registro comercial. No mezcla bases de centros educativos ni constituye un nuevo alojamiento. Se mantiene la arquitectura de una instalación/base por institución. El correo `@miumg.edu.gt` ayuda a identificar el conjunto indicado por el responsable, pero no concede permisos ni sustituye la separación de instalaciones. No se trasladaron ni reclasificaron automáticamente cuentas de prueba con otros dominios.

No se añadieron migraciones en esta ronda. La migración de las tablas comerciales de 1.0.0 ya está aplicada **en localhost**; su publicación en la nube sigue siendo una operación pendiente y separada. Conservar las claves existentes y respaldar la base viva antes de una futura publicación; no restaurar un dump antiguo sobre la base publicada.

## Verificación

- Compilación Release correcta, sin errores ni advertencias del proyecto.
- Suite Seguridad.Offline completa correcta sobre SQLite temporal, incluidas propuestas del Director, privacidad y notificaciones mal dirigidas.
- 56 comprobaciones focalizadas de centros, guías por rol, versión y avisos.
- 63 comprobaciones de navegador para menú, ejes horizontales y verticales de los iconos, desplazamiento, filtro sin recarga, resaltado compacto y modales.
- 97 comprobaciones de continuidad móvil e idioma; catálogo de traducciones y sintaxis JavaScript correctos.

Las pruebas de navegador usan cuentas y páginas ficticias. Estos resultados no sustituyen una revisión integral de seguridad ni la aceptación manual. La única escritura adicional en PostgreSQL fue el registro comercial local UMG expresamente autorizado; las pruebas usaron SQLite temporal.

El documento institucional de privacidad anunciado sigue pendiente de adjuntar. La explicación operativa publicada no sustituye el texto institucional aprobado ni garantiza cumplimiento legal por sí sola.
