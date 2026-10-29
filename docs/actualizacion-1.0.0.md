# Actualización 1.0.0 — Director V2

Nota histórica: este documento describe la preparación inicial. El estado posterior está en `actualizacion-1.1.0.md`; la migración comercial ya se aplicó a localhost, mientras que la publicación en zuni.lat sigue pendiente.

Estado: cambios locales en `feature/director-V2`. No publicados en zuni.lat; no se ha aplicado la nueva migración a PostgreSQL.

## Funciones

- Acceso móvil con el mensaje de bienvenida y sin el logo repetido del formulario.
- Indicadores de contenido inferior y regreso al inicio en páginas públicas y paneles.
- Cambio de idioma sobre el contenido visible, conservando desplazamiento, respuestas y campos sin guardar. Las entradas personales y los textos redactados en la base de datos conservan su idioma original.
- SuperAdmin: tablero comercial, centros educativos, contratos, fechas, periodos, importes, suspensión, cancelación y ampliación. Revisión de concurrencia y auditoría de cambios. No permite acceso a fichas, alumnos ni respuestas de las instituciones.
- El ejemplo Colegio Huellitas se ofrece en un formulario editable: anual, del 28/10/2026 al 27/10/2027. Abrirlo no registra un contrato; se guarda únicamente al confirmar. No representa un pago real.
- Las modificaciones comerciales no ejecutan cobros, crean servidores ni bloquean instalaciones remotas automáticamente.
- Diez temas de guías públicas y un catálogo de 33 guías por rol, con búsqueda y ubicación de las acciones.
- Transparencia operativa pública. SuperAdmin consulta sus responsabilidades sin el formulario institucional de solicitudes personales. El Administrador recibe el aviso de solicitudes pendientes.
- Favicon basado en el logo existente sobre círculo blanco, versión 1.0.0 y campana breve de novedades en los paneles autenticados. Lecturas de novedades independientes por usuario y versión.

## Documento de privacidad pendiente

El documento anunciado por el usuario todavía no se ha adjuntado. La explicación operativa añadida no sustituye ese documento ni acredita cumplimiento legal por sí misma. Se conserva la política institucional existente. Integrar el documento aprobado con sus divisiones cuando se reciba; no inventar términos, plazos ni garantías.

## Datos y preparación de publicación

La migración nueva `20261029033032_AddCentrosComercialesYLecturasVersion` crea solamente `CentrosComerciales` y `LecturasVersion`, sus índices, restricciones y referencias a usuarios existentes. No altera migraciones anteriores ni añade columnas a tablas clínicas.

Antes de iniciar esta versión contra una base existente, se necesita aplicar esa migración y conceder al usuario de ejecución los permisos correspondientes sobre ambas tablas. La migración está generada, no aplicada. No se debe arrancar esta versión esperando que las tablas aparezcan automáticamente.

Secuencia de publicación pendiente, a realizar de forma controlada:

1. Identificar el commit aprobado y verificar la versión del servidor. Respaldar la base viva, los binarios, la configuración y las claves persistentes; comprobar que las copias sean recuperables.
2. Probar la actualización sobre una copia aislada, con claves custodiadas y sin enviar comunicaciones reales. Revisar el SQL de la migración y la compatibilidad de los permisos runtime.
3. Preparar un directorio de publicación nuevo. Mantener las claves de cifrado, Data Protection, bitácora e identidad de instalación de la versión vigente. No inicializar de nuevo la seguridad ni cambiar la identidad criptográfica.
4. En una ventana acordada, aplicar únicamente las migraciones pendientes con el usuario migrador y actualizar permisos de ejecución. El usuario runtime no necesita permisos de creación de esquema.
5. Cambiar los binarios del servicio y verificar arranque, HTTPS, login por rol, descifrado autorizado, auditoría y las funciones nuevas. Conservar la versión anterior para una reversión revisada; no ejecutar automáticamente `Down` ni borrar tablas.

No restaurar el antiguo dump local de demostración encima de la base publicada: reemplazaría usuarios y cambios posteriores. Tampoco sobrescribir claves con otras de un entorno de pruebas. La aplicación y PostgreSQL conservan sus directorios persistentes separados de los binarios.

## Versiones y novedades

Actualizar de forma coordinada `Zuni.csproj` y `Resources/VersionSistema.cs`. Usar SemVer: parche para correcciones compatibles; menor para funciones compatibles; mayor para cambios incompatibles. Cambiar las notas de novedades al publicar: la lectura queda asociada a cada versión y usuario.

El favicon del buscador requiere publicar y permitir un nuevo rastreo. Su aparición y plazo dependen del buscador; configurar el enlace de favicon no garantiza que cambie inmediatamente en Google.

## Verificación aislada

- Compilación Release correcta.
- Suite Seguridad.Offline completa correcta con bases SQLite temporales; incluye 36 comprobaciones nuevas de centros, autorización, antiforgery, concurrencia, privacidad y novedades.
- Navegador: 97 comprobaciones nuevas de móvil, idioma, formularios y desplazamiento.
- Continuidad de idioma: 21 comprobaciones correctas, incluidas respuestas y archivos seleccionados sin guardar.
- Auditoría de catálogo de traducciones sin claves explícitas faltantes.

Estas comprobaciones no sustituyen la prueba manual de aceptación ni una auditoría integral de seguridad. Las pruebas usaron SQLite temporal sin modificar PostgreSQL; después se comprobó mediante una consulta de solo lectura que las dos tablas nuevas aún no existen en la base local. No se hizo push ni despliegue.
