# Guías visuales, reporte compartido y programación de citas

> **Actualización 17/10/2026:** el Director ya no programa, reprograma ni consulta citas individuales ni sus notificaciones; solo ve la carga anónima en «Carga de psicólogos». Ver [post-marlon-review-fixes.md](post-marlon-review-fixes.md).

Entrega en `feature/director-V2`, 14 de octubre de 2026.

## Inicio y guías

El pie público ahora tiene identidad ZUNI, descripción, enlaces agrupados, política y contacto institucional cuando está configurado, además de la franja de derechos y lema. Flujo normal de página y diseño responsive, sin superposición sobre el contenido.

Las cuatro guías públicas usan encabezado visual, tarjetas con los mismos iconos del panel, navegación entre temas y bloques «Dónde pulsar». Bienestar explica Big Five opcional, solicitud de atención, organización de citas, resultados compartidos, pruebas institucionales y apoyo educativo. Privacidad desarrolla separación de datos, notas privadas, autorización por versión/destinatario, política, solicitudes de derechos y acceso cotidiano. Se conserva la paleta beige, violeta y tonos neutros del diseño existente; no se muestran gráficos con resultados ficticios.

## PDF compartido

El reporte conserva la proyección exclusiva de campos compartidos. Incluye encabezado beige, logotipo ZUNI incorporado como recurso, barra violeta, celdas con estudiante/carné/profesional/fecha, secciones numeradas y pie con logotipo y número de página. La misma fuente incluida funciona en Windows y Linux. Los textos largos pueden continuar en otras páginas sin incorporar notas profesionales privadas.

## Programar y reprogramar

- **Psicólogo → Agenda → Programar cita a estudiante**, o **Programación de citas → Programar nueva cita**. El Director tiene **Programación de citas** en su menú. Se eligen estudiante vinculado, fecha, inicio, duración, fin aproximado opcional y modalidad.
- La disponibilidad diaria incorpora **Estudiante (opcional)**. Sin seleccionar a nadie, solo guarda disponibilidad. Al elegir uno, programa la primera franja de ese rango y registra una notificación. «Ambas» sigue siendo una modalidad de disponibilidad: para programar una cita concreta se elige Presencial o Virtual.
- **Reprogramar cita** está en la tabla de Agenda, en la ventana del día y en Programación de citas. Conserva el identificador del registro, revisa versión, estado, vínculo, horarios, día bloqueado, cruces y la regla de una cita activa por estudiante/día.
- Solo se reprograman citas activas futuras. Las atenciones finalizadas y las imprevistas conservan el historial. No se traslada una cita a otro estudiante ni se cambia silenciosamente su psicólogo.
- Una programación del Director queda propuesta para confirmación del psicólogo. La presencial del psicólogo puede confirmarse directamente cuando el estudiante ya completó perfil, cambio de contraseña y revisión de mayoría de edad; de lo contrario queda Solicitada. Confirmar una nueva propuesta exige perfil y edad preparados. El registro clínico conserva además sus requisitos de expediente y consentimiento.
- Al cambiar una cita virtual, se quita el acceso guardado y vuelve a requerirse preparación y confirmación del enlace. Los enlaces usados siguen registrados para no reutilizarlos en otra cita. ZUNI no modifica una invitación o reunión externa en Google.

El Director recibe exclusivamente datos de programación, no expediente ni notas privadas. Administración y Estudiante no pueden programar mediante estas acciones.

## Notificaciones

El acceso **Notificaciones** aparece en la barra superior del panel, con cantidad pendiente. La notificación indica quién programó o reprogramó, su rol, fecha/hora de Guatemala, modalidad y estado. El estudiante consulta su cita actual; cuando actúa el Director, también se notifica al psicólogo responsable. Marcar como leída exige sesión propia y token del formulario; una cuenta no puede leer ni modificar notificaciones de otra. No contienen notas clínicas.

Son notificaciones internas persistentes, no correos ni mensajes externos. La creación/reprogramación y sus avisos se guardan en una misma transacción; un error no deja una notificación sobre una cita que no se guardó. La edición obsoleta y los envíos duplicados se rechazan.

## Datos locales

Se vincularon a `marito@gmail.com` los primeros diez usuarios con rol Estudiante del listado proporcionado, en su orden original. Se conservó la fila cuya cuenta ya era Director; no se agregó ese rol a Estudiante ni se sobrescribió su perfil. Se conservan los vínculos y datos ficticios previos de demostración. No se cambiaron contraseñas ni se inventaron revisiones de edad, teléfonos, carreras o consentimientos de los estudiantes reales.

La migración `20261014182455_AddProgramacionNotificacionesCitas` agrega origen de programación y notificaciones, sin crear nuevas tablas de autenticación. Se aplicó localmente después de verificar el respaldo `zuni-antes-programacion-20261014.dump`. Un rollback de esquema requiere revisar previamente las citas directas y notificaciones; no se realiza automáticamente.

## Verificación

Pasaron 859 comprobaciones: 473 de seguridad y módulos offline, 315 de agenda y 71 de formularios en Edge. Se revisaron las guías en escritorio y móvil y se renderizó el reporte compartido con textos extensos en tres páginas, comprobando contenido completo, logotipo, paginación y separación del pie. PostgreSQL confirmó diez vínculos vigentes para la asignación solicitada. La compilación Release terminó; una restauración posterior mostró NU1900 porque no pudo consultar el servicio de vulnerabilidades de NuGet, sin impedir la ejecución de las pruebas.
