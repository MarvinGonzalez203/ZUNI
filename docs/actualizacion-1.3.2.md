# ZUNI 1.3.2 — atención iniciada por el profesional

Una atención imprevista o programada por el psicólogo puede cerrarse y consultarse
sin que el estudiante envíe una solicitud posterior. El profesional registra
asistencia, notas privadas, recomendaciones y resultado compartido en la misma cita.
También puede programar la siguiente cita desde su agenda sin una solicitud previa
del estudiante. Los requisitos independientes de cuenta, perfil, edad y acceso se
conservan; no se crean solicitudes ni aceptaciones de consentimiento ficticias.

`RegistroAtencionCita.SolicitudAtencionId` admite null para las atenciones iniciadas
por el profesional que no tengan una solicitud consentida anterior. La propiedad
histórica de la cita y la asignación vigente se verifican antes de escribir o leer.
Los registros independientes se auditan por cita en `AuditoriaUsuarios`, dentro de
la transacción, con los mismos mecanismos de firma y copia externa. Las notas
siguen cifrándose y el reporte compartido no selecciona la columna privada.

Una solicitud posterior no se enlaza retroactivamente a un registro ya cerrado.
Las atenciones que ya tenían solicitud mantienen su vínculo y auditoría originales.
Los cuestionarios con participación del estudiante conservan su autorización separada.

## Migración

`20261111063900_PermitirAtencionProfesionalSinSolicitud` elimina únicamente el
NOT NULL del vínculo a solicitud en `RegistrosAtencionCita`; no cambia registros,
consentimientos, notas ni claves. Antes de aplicarla a la base local se conserva
un respaldo privado. No se publica ni se migra zuni.lat.

Revertir a NOT NULL falla si existen atenciones independientes: no se generan IDs
ficticios ni se eliminan registros para permitir el retroceso.

## Verificación

Las suites offline comprueban registro y consulta sin solicitud, auditoría por cita,
separación de notas privadas y compartidas, roles ajenos y conservación de los
controles de contraseña y edad. Se verifica la migración y el arranque local.
