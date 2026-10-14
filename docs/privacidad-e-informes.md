# Privacidad e informes educativos (R04 y R05)

## Preparación institucional

El Administrador abre **Política de privacidad**, introduce responsable y correo de contacto, finalidades, datos necesarios y accesos, reglas/plazos de conservación y eliminación, procedimiento para derechos e incidentes. Confirma la aprobación institucional y publica. Cada publicación conserva una versión independiente; una versión anterior no se edita ni se sobrescribe. No se publica un texto legal ficticio automáticamente.

Sin política publicada no se habilita el autorregistro. El registro requiere aceptación expresa de la versión vigente, revisada nuevamente dentro de la transacción. Para nuevas solicitudes de atención también se exige lectura/aceptación vigente desde **Privacidad y mis derechos**. Las atenciones históricas y los retiros independientes no se eliminan por publicar otra política.

La institución define los plazos y bases aplicables; la aplicación no anuncia un plazo legal universal. Como referencia inicial de Guatemala, el [Decreto 57-2008 publicado por el TSE](https://www.tse.org.gt/images/UECFFPP/leyes/ley_acceso_informacion_decreto_57-2008.pdf) contempla datos personales y sensibles, protección y difusión en su ámbito. Su aplicabilidad y otras obligaciones deben revisarse para cada institución. Publicar una política no certifica cumplimiento legal.

## Derechos sobre los datos

Cada usuario puede registrar solicitudes de acceso, corrección, supresión y retiro de consentimiento, consultar su comprobante, estado y respuesta. La solicitud no exige aceptar una nueva política. Se limita a cinco solicitudes simultáneamente pendientes; la lista propia muestra las últimas cien.

Administración abre **Solicitudes sobre datos**, identifica al solicitante y registra una respuesta con acciones realizadas o un procedimiento/canal seguro acordado. El estado **Respondida** significa que se registró una respuesta, no que una eliminación se ejecutó automáticamente. El procedimiento institucional debe comprobar identidad, determinar qué información puede entregarse, efectuar las acciones autorizadas y comunicar el resultado. El Administrador no adquiere acceso a notas clínicas por atender el trámite.

Los datos propios corregibles directamente siguen disponibles en Mi cuenta. Big Five conserva su retiro específico. La autorización de un informe puede revocarse inmediatamente desde su detalle. La supresión de expedientes y los retiros de atención requieren el procedimiento institucional definido; no se borra información clínica sin revisión profesional y de conservación.

## Recorrido del informe educativo

1. El psicólogo abre **Estudiantes → Expediente → Crear informe educativo** de un estudiante asignado, activo y con mayoría de edad verificada.
2. Redacta objetivo, periodo, fuentes autorizadas, barreras observadas, necesidades, recomendaciones, responsables y fecha de seguimiento. Elige un Director activo como destinatario.
3. Guarda un borrador privado o solicita autorización al estudiante. Los campos no se llenan con notas clínicas ni respuestas de cuestionarios.
4. El estudiante abre **Informes educativos**, revisa exactamente el texto, versión, finalidad y destinatario; autoriza o rechaza. Rechazar no cancela la atención.
5. El psicólogo confirma la revisión profesional y emite la versión autorizada. El Director designado puede leerla y descargarla en PDF. Administración, otros estudiantes y otros Directores no reciben acceso por su rol.
6. El estudiante puede revocar: las nuevas descargas quedan bloqueadas. Las copias descargadas fuera de ZUNI no se eliminan.
7. Para corregir un texto cerrado, el autor prepara otra versión, repite su autorización y emite. La nueva versión marca cuál sustituye; la anterior se conserva históricamente y deja de descargarse como vigente.

Contenido educativo únicamente: no incluir diagnósticos, DPI, contactos de emergencia, narraciones íntimas o respuestas individuales. La aplicación exige revisión profesional; no clasifica automáticamente el texto ni garantiza que una persona no copie información improcedente. Las recomendaciones deben apoyarse en fuentes y conductas observables, sin etiquetas personales.

## Controles implementados

Autorización contra usuario, rol y estado persistidos y sello de sesión; autorización clínica vigente para redactar/emitir; propietario del consentimiento; destinatario exacto; decisiones y descargas auditadas sin copiar contenido clínico/educativo; transacciones serializables y revisiones para rechazar cambios obsoletos; protección antiforgery; respuestas privadas sin caché. El PDF se genera en memoria y no se guarda en una ruta pública.

La versión del consentimiento, texto, destinatario (en la versión cerrada), finalidad, política de referencia y fechas quedan conservados. Las identidades mostradas son las capturadas para esa versión. El nombre del profesional no se anuncia como firma electrónica certificada.

PDF: [PDFsharp/MigraDoc 6.2.4](https://www.nuget.org/packages/PDFsharp-MigraDoc), con fuentes Bitstream Vera incluidas y su licencia conservada. El generador funciona sin fuentes del sistema ni herramientas de Windows. Se revisaron visualmente un informe corto y otro de varias páginas, con acentos, numeración y encabezados/pies. La lista de informes muestra las doscientas versiones más recientes autorizadas para la cuenta.

Los requisitos de HTTPS, cifrado y acceso a respaldos, conservación operativa y respuesta institucional deben configurarse/verificarse en el despliegue y en la operación real. El aislamiento entre dos hosts y bases de prueba se comprobó con una sesión cruzada rechazada; el despliegue Debian/Linode sigue en R08.

## Datos y puesta en marcha

Migración `20261014060401_AddPrivacidadInformesEducativos`: agrega seis tablas de políticas, aceptaciones, derechos, informes, versiones y autorizaciones. No crea otra autenticación, no agrega tablas Identity ni convierte registros históricos en informes.

Aplicada a `localhost/ZuniDb` el 14 de octubre de 2026, después de crear y comprobar el respaldo `zuni-antes-privacidad-informes-20261014.dump`. Los recuentos anteriores de usuarios, perfiles, solicitudes de atención, citas y verificaciones quedaron iguales. Las nuevas tablas de políticas, informes y solicitudes de derechos quedaron vacías.

Las pruebas usan datos ficticios en SQLite. La aplicación real inicia sin política ni informes publicados por estos tests. Publicar la política y autorizar un informe son acciones de los responsables reales.
