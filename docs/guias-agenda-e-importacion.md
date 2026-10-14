# Guías públicas, agenda histórica y atenciones imprevistas

Entrega en `feature/director-V2`, 14 de octubre de 2026.

## Inicio

Las cuatro opciones de Inicio ahora abren guías públicas: solicitar cita, seguimiento, bienestar y privacidad. Explican las pantallas y pasos correspondientes, con ejemplos de los estados y las acciones disponibles. La lectura no exige sesión; realizar las acciones del perfil conserva sus permisos.

## Disponibilidad normal

En **Psicólogo → Agenda → seleccionar día → Añadir horario disponible**, el inicio es obligatorio y el fin aproximado es opcional. Se eliminó el valor fijo de las 12:00. Al abrir una ventana, los campos del formulario de horario vuelven a sus valores iniciales vacíos.

Sin fin indicado se calcula un bloque con la duración seleccionada; para «Sin duración establecida», el bloque de planificación predeterminado es de 50 minutos. Si se indica un fin, se conserva como límite del rango reservable. Esto no mide ni obliga a que una sesión real termine en ese instante. Los controles y el servidor impiden añadir una disponibilidad normal que empiece antes de la hora actual, que cruce otra disponibilidad o que no quepa entre las 08:00 y las 18:00.

## Atención imprevista

La opción **Registrar atención imprevista** está en Agenda y en la ventana de cada día pasado o de hoy. Registra actividades ya ocurridas para estudiantes actualmente asignados, sin crear horarios artificiales ni requerir una reserva previa. Permite horas pasadas y horarios fuera del rango normal; rechaza inicios futuros, participantes ajenos, superposiciones y envíos duplicados.

El fin se marca como aproximado. Si se omite, se estima hasta 50 minutos desde el inicio, sin superar la hora actual ni el día indicado. El registro queda pendiente de cierre clínico: para completar el resultado con asistencia siguen siendo necesarios la solicitud consentida, la asignación y la revisión de mayoría de edad. Registrar la actividad no inventa esos requisitos ni constituye un protocolo de atención de emergencias.

## Historial desde el calendario y reimpresión

El psicólogo puede abrir días y meses pasados. La ventana del día muestra sus propias citas y actividades, estado y horario. Un día pasado permite consultar y registrar actividades imprevistas, pero no modificar disponibilidad normal. El calendario del estudiante conserva la restricción de ofrecer solamente franjas futuras y no recibe la lista de identidades de otras personas.

Una cita finalizada con registro ofrece **Ver reporte compartido y reimprimir**. La misma acción está en Historial y en Resultados del estudiante. El reporte y su PDF contienen exclusivamente resumen y recomendaciones compartidos, sin seleccionar la columna de notas profesionales privadas. Solo acceden el estudiante correspondiente y el psicólogo autorizado; Administración y Director no reciben acceso por ese rol. La consulta y descarga se auditan.

Las tarjetas y el menú del estudiante muestran Citas y Resultados como funciones disponibles, retirando los rótulos antiguos «Próximamente» y «En preparación» de esas dos funciones.

«Compartido» no significa accesible por una URL pública. Este documento no sustituye al informe educativo con autorización por versión y destinatario. La impresión del navegador oculta las notas privadas y el expediente inicial; esta regla de presentación no impide capturas o copias manuales realizadas por alguien que ya tiene acceso autorizado.

Las ventanas Bootstrap de los layouts común y de perfiles incorporan desenfoque de fondo.

## Importación y demostración local

Se importaron 19 cuentas nuevas de estudiante del archivo proporcionado; se conservó sin cambiar de rol la cuenta existente `mpivaralv@miumg.edu.gt`, que es Director. No se duplicaron cuentas ni carnés y no se incorporó el archivo de datos personales al repositorio. Las cuentas nuevas están activas con la contraseña temporal indicada por el usuario y cambio obligatorio al primer ingreso. No se fabricaron teléfonos, declaraciones de edad ni consentimientos de los estudiantes del listado.

Para la demostración se creó un estudiante ficticio separado, **DEMO AGENDA — Estudiante ficticio**, y una atención finalizada el **11/10/2026 de 10:00 a 10:50, hora de Guatemala**, asignada a `marito@gmail.com`. Los textos y el consentimiento de ese caso están identificados como demostración sin valor clínico, sin atribuir una atención a una persona del listado. No se generó una verificación documental de edad. Puede consultarse desde **Agenda → octubre de 2026 → día 11 → Ver reporte compartido y reimprimir**.

La migración `20261014170308_AddAtencionesImprevistas` permite disponibilidad nula únicamente para citas imprevistas y conserva las citas programadas existentes. Se aplicó con respaldo previo `zuni-antes-agenda-importacion-20261014.dump`. No debe revertirse a un esquema que exige disponibilidad sin resolver antes los registros imprevistos; utilizar un respaldo revisado para una recuperación real.

La comprobación en PostgreSQL detectó y corrigió la serialización de referencias de la auditoría compartida: ahora los identificadores se escriben como JSON válido para sus columnas jsonb. Esta corrección beneficia también a los flujos de privacidad, informes y reuniones que utilizan ese mismo helper.

Verificación final: compilación Release sin errores ni advertencias; 446 comprobaciones de seguridad y módulos asociados, 315 de agenda y 55 de formularios en navegador: **816 en total**. El calendario y el reporte de la cita ficticia del día 11 también se comprobaron contra PostgreSQL local. Se extrajo el texto del PDF de prueba para confirmar que incluye el contenido compartido y excluye la nota privada; se revisó visualmente su página renderizada. Las pruebas de agenda en SQLite no equivalen a un ensayo de solicitudes concurrentes en PostgreSQL.
