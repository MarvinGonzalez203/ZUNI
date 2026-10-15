# Calendarios, carreras, alertas e instrumentos con inducción

Entrega en `feature/director-V2`, 14 de octubre de 2026.

## Dónde probar

- Estudiante → Calendario de citas: mes completo y citas propias, incluso con verificación pendiente. El botón de reserva requiere perfil, mayoría de edad verificada y vínculo profesional. Se pueden consultar meses anteriores; no se muestran identidades de otras personas.
- Administrador → Catálogo de carreras: nueve carreras iniciales organizadas en cinco áreas. Admite pegar texto o subir TXT/CSV UTF-8, hasta 100 KB y 500 filas. Área seguida de «:» y una carrera por línea, o «Área;Carrera». Se omiten duplicados. Habilitar/deshabilitar no borra carreras de perfiles existentes.
- Mi cuenta → Editar: la carrera se selecciona del catálogo. El servidor rechaza nuevos valores ajenos o inactivos, pero conserva el valor anterior de una cuenta que todavía no coincide con el catálogo.
- Administrador: alerta superior «Edad pendiente» con cantidad y acceso directo a las revisiones. La cantidad refleja declaraciones pendientes de cuentas activas y se actualiza al navegar; no muestra documentos ni fechas de nacimiento y no desaparece por marcar otro aviso como leído.
- Director → Programación de citas: elegir psicólogo, consultar el calendario con colores y franjas y proponer una cita a sus estudiantes vinculados. El calendario aparece arriba y el listado general debajo, aunque no haya citas activas. Conserva bloqueos y reglas de cruce. Muestra exclusivamente metadatos de agenda. Las propuestas del Director quedan Solicitadas para aceptación profesional.
- Psicólogo → Notificaciones → Consultar / responder en mi agenda: abre el mes de la cita. Puede aceptar una presencial o preparar y confirmar una virtual. Para rechazar una propuesta del Director debe indicar un motivo administrativo de hasta 300 caracteres, sin detalles clínicos. El Director y el estudiante reciben el aviso de la decisión.

Si una propuesta cambió de fecha o estado después de abrir la agenda, responder desde esa versión antigua se rechaza. El psicólogo debe recargar y revisar la fecha actual. El calendario usa un DTO separado que no admite motivos de cancelación, enlaces de reunión ni contenido clínico.

## Tres instrumentos exploratorios

El catálogo del Director incluye los títulos y subtítulos solicitados para niveles de 15, 35 y 54 afirmaciones, con diez opciones por afirmación: 1 = totalmente en desacuerdo / nada identificado; 10 = totalmente de acuerdo / totalmente identificado. Los subtítulos aparecen en letra pequeña. Los tres se cargan como borradores inactivos para revisar sus preguntas antes de activarlos.

Las preguntas son una propuesta original de ZUNI, no ítems entregados por el usuario ni una reproducción de un instrumento estandarizado. Se organizan en interacción/comunicación, organización, afrontamiento cotidiano, decisiones/adaptación e intereses/motivación académica. El catálogo informa la fuente y sus límites. No se implementan ML.NET, diagnósticos, percentiles, baremos, umbrales de riesgo ni afirmaciones de alta exactitud. [Las directrices de la Comisión Internacional de Tests](https://www.intestcom.org/page/15) piden evidencia de fiabilidad y validez que sustente las inferencias sobre puntuaciones; aquí no se dispone de esa validación.

## Recorrido

1. Director → Gestión de pruebas → Ver prueba: revisar el contenido del borrador; ajustar si procede y activar. El contenido deja de ser editable cuando tiene asignaciones para conservar la versión respondida.
2. Psicólogo → Pruebas e inducción → Indicar prueba e inducción: elegir estudiante vinculado y prueba activa con escala completa. El sistema exige al menos una cita completada con registro de asistencia, autorización clínica vigente, cuenta y perfil preparados y edad verificada. El psicólogo confirma que explicó la consigna y participación voluntaria.
3. Una asignación creada por el Director todavía no permite responder. El psicólogo debe habilitarla después de la cita y registrar la inducción. La referencia a la cita y el profesional se guardan en servidor; no se aceptan estos datos desde el estudiante.
4. Estudiante → Pruebas indicadas: confirmar que comprendió la explicación y participa voluntariamente; guardar un avance o finalizar todas las respuestas. No hay opción seleccionada por defecto ni respuestas correctas/incorrectas. Preguntas duplicadas, opciones ajenas, finalizaciones incompletas y versiones obsoletas se rechazan.
5. Psicólogo vinculado → Pruebas e inducción → Consultar resumen exploratorio: medias aritméticas por tema y respuestas por afirmación. Se presentan como identificación con esas afirmaciones; requieren conversación profesional y no constituyen una medición estandarizada de rasgos. El contenido individual se oculta al imprimir desde el navegador. Administrador, Director, otros estudiantes y otros psicólogos no reciben ese resumen.
6. El estudiante puede retirar su participación. La aplicación conserva el registro, bloquea nuevas respuestas y oculta el resumen profesional en este módulo. No borra expedientes o informes históricos.

Big Five conserva su recorrido existente y es independiente de los tres instrumentos nuevos. La atención a menores sigue pendiente de una política y flujo institucional de consentimiento; esta entrega no habilita atención clínica de menores ni inventa revisiones documentales.

## Base local

La migración `20261014193516_AddCarrerasPruebasAplicadas` agrega catálogo, metadatos de inducción y respuestas con claves y control de versión. Incluye las nueve carreras y los tres borradores con sus preguntas y opciones. No crea tablas Identity ni modifica contraseñas, cuentas, revisiones de edad o respuestas existentes. Antes de aplicarla se creó y verificó `zuni-antes-carreras-pruebas-20261014.dump`.

Las pruebas usan únicamente datos ficticios en SQLite y HTTP local. No aprueban estudiantes reales ni crean respuestas reales para demostrar el recorrido. El uso clínico de los ítems propuestos requiere revisión del equipo profesional.

## Verificación final

Corrección posterior de navegación: se integró el calendario directamente en `/CitasProgramadas`, con selector de psicólogo y listado debajo. Se verificó esta entrada con 524 comprobaciones offline y 82 de navegador, incluido móvil. La ruta anterior reutiliza la misma vista parcial. Esta corrección no requiere cambios de base de datos.

Compilación Release sin errores. Se aprobó el recorrido con 523 comprobaciones de seguridad y módulos offline, 315 de agenda y 82 de formularios en Edge: 920 en total. Se revisaron las pantallas en escritorio y móvil y la ocultación del resumen individual al imprimir. PostgreSQL confirmó nueve carreras y los niveles inactivos con 15, 35 y 55 preguntas después de aplicar la migración. EF confirmó que no hay cambios del modelo pendientes de migración.

La restauración conserva una advertencia NU1900 porque no pudo consultar el servicio externo de vulnerabilidades de NuGet; no se afirma haber completado esa auditoría externa. Los ensayos SQLite no sustituyen una prueba de solicitudes concurrentes contra PostgreSQL. El respaldo local fue validado antes de la aplicación, y la migración se ejecutó en una transacción con detención ante errores.
