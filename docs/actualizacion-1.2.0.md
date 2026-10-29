# ZUNI 1.2.0 — impresión y navegación

Entrega local de Director V2 del 29/10/2026. Añade la impresión de contratos y ajustes compatibles, por lo que incrementa la versión menor. No se publica en zuni.lat.

## Navegación

- Retiradas las líneas verticales y horizontales de los submenús. En vista ancha se aplica una tabulación de 16 px a los iconos y textos de las filas hijas. En vista compacta se alinean los iconos al eje principal; se conserva el ancho interno para evitar que el texto oculto cambie las alturas.
- El desplazamiento de la barra lateral se contiene en su propio panel, sin propagarse a la página al llegar a los extremos. Se desactiva el anclaje automático del navegador en ese panel y la cabecera queda fija, con el mismo desplazamiento superior para el menú y el contenido.
- Carga de psicólogos muestra siempre el selector cuando hay al menos un profesional elegible. Cambiarlo conserva el mes y continúa mostrando únicamente bloques y métricas sin identidades de estudiantes.
- Proponer cita y Mis propuestas comparten una entrada lateral, **Propuestas de citas**. Desde el listado se abre Proponer cita; desde el formulario se puede volver al listado. Se mantienen las rutas y las autorizaciones anteriores.

## Contratos y ejemplo Huellitas

En Centros educativos y en la edición de un contrato guardado aparece **Descargar contrato PDF**. La descarga exige una cuenta SuperAdmin vigente; no permite al Administrador institucional ni a otros roles consultar contratos. La generación utiliza una instantánea comercial, no consulta alumnos y deja el evento `CENTRO_COMERCIAL_PDF_GENERADO` en la auditoría.

El PDF incluye membrete de ZUNI, institución, periodo, fechas, estado, importe, contacto, observaciones, identificador, revisión y paginación. Los ejemplos se marcan como ficticios. Es una ficha administrativa del registro: no acredita pagos ni inventa condiciones legales o firmas.

Con respaldo previo y auditoría, se guardó en localhost **Colegio Huellitas**, marcado como demostración, del 28/10/2026 al 27/10/2027, con correo `administracion@huellitas.example` e importe de referencia del catálogo. No se crearon cuentas de alumnos ni otra instalación, y no se modificaron los datos de UMG.

## Informe educativo de demostración

Se entrega un ejemplo de dos páginas, generado con el mismo formato que utiliza el informe educativo de ZUNI. El destinatario es una Directora ficticia. Describe fuentes simuladas, observaciones educativas, relato de la estudiante, valoración contrastada, necesidades, acuerdos y seguimiento. No incorpora notas profesionales privadas, respuestas de cuestionarios ni diagnósticos.

Personas y autorización están identificadas como simuladas. ZUNI se presenta como herramienta de apoyo, nunca sustituto del psicólogo. La descarga real conserva la autorización por versión y destinatario, así como emisión, revocación y auditoría existentes. No se agregó un nuevo flujo automático de entrega a familiares.

Los dos ejemplos se regeneran sin base de datos mediante:

```powershell
dotnet build tests/Seguridad.Offline/Seguridad.Offline.csproj -c Release
dotnet tests/Seguridad.Offline/bin/Release/net8.0/Seguridad.Offline.dll --pdf-demo --salida output/pdf
```

Los PDF y sus previsualizaciones locales se excluyen de Git. Se inspeccionaron todas las páginas renderizadas: contrato de una página e informe de dos, con membrete, pie y contenido dentro de los márgenes.

## Verificación

Compilación Release y suite Seguridad.Offline completas correctas. Incluye 65 comprobaciones focalizadas, autorización y auditoría del PDF comercial, selector de un solo psicólogo y menú de propuestas. Navegador: 81 comprobaciones de navegación y límites de desplazamiento, 97 de continuidad móvil/idioma y 45 del historial de versiones. La revisión de textos y PDF también se realizó de forma visual.

Sin nuevas migraciones ni cambios de esquema. Las pruebas de servidor usan SQLite temporal; la única escritura de mantenimiento en PostgreSQL fue el ejemplo comercial Huellitas solicitado, con respaldo y auditoría.
