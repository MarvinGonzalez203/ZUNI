# Verificación de mayoría de edad (R03)

## Recorrido de usuario

1. El estudiante inicia sesión y abre **Mayoría de edad** en su menú.
2. Introduce su fecha de nacimiento y envía la declaración. Se calcula la edad considerando el cumpleaños y la fecha local de Guatemala. Las fechas futuras, edades menores de 18 o superiores a 120 se rechazan.
3. El estado aparece como **Declarada, pendiente de verificación**. El estudiante coordina con su institución la revisión del documento.
4. El Administrador abre **Verificar mayoría de edad → Revisar documento**. Revisa personalmente el documento y comprueba que corresponde al estudiante.
5. Para aprobar, selecciona el tipo de documento, introduce la fecha de nacimiento que observó y confirma su revisión. Para rechazar, selecciona que no pudo acreditar mayoría de edad.
6. Una verificación aprobada habilita las nuevas solicitudes de atención. Una revisión rechazada permite presentar una nueva declaración. Cualquier nueva revisión debe usar la versión vigente.

El Administrador puede registrar DPI, pasaporte o documento de identidad extranjero. La institución determina qué documento admite y cómo se coordina la revisión presencial o institucional. No hay integración con RENAP ni comprobación automática de autenticidad.

## Datos y permisos

Se conserva la edad al declarar y al verificar, el estado, tipo de documento revisado, fecha y responsable. No se guarda fecha de nacimiento, CUI, fotografías ni biometría. Las fechas introducidas se utilizan para el cálculo y se descartan tras el proceso; una fecha inválida puede permanecer temporalmente visible en el formulario para corregirla.

La solicitud muestra la edad registrada al verificar, de solo lectura. El servidor utiliza ese valor aunque el navegador envíe otro. Es una edad documentada en la fecha de revisión; no se anuncia como un cálculo actualizado diariamente, puesto que no se conserva la fecha de nacimiento.

Antes de presentar una solicitud también se exige teléfono personal válido en el perfil. Si falta, se dirige al estudiante a Editar mi cuenta. El formulario de solicitud exige al menos un contacto de emergencia completo.

Las cuentas existentes no se consideran verificadas automáticamente. No cambia su rol ni contraseña. Las solicitudes históricas se conservan; nuevas solicitudes y reservas requieren verificación, igual que registrar atención con asistencia o participar y responder Big Five. Cancelar una cita, registrar inasistencia y consultar registros históricos conservan sus permisos anteriores.

La revisión utiliza las cuentas y roles existentes. El Director y el estudiante no pueden aprobar documentos. La pantalla administrativa muestra identificación básica y estado; no contiene motivo de consulta, contactos de emergencia ni resultados clínicos. Las decisiones se auditan sin copiar el documento. Las escrituras usan transacciones serializables y una revisión para impedir sobrescrituras obsoletas.

## Base de datos

Migración: `20261014051749_AddVerificacionMayoriaEdad`. Agrega únicamente `VerificacionesMayoriaEdad`, con un registro por perfil. No agrega tablas Identity ni modifica datos históricos.

Aplicada a la base local después de crear y comprobar el respaldo `zuni-antes-mayoria-edad-20261013.dump`. Se verificaron los recuentos de usuarios, perfiles y solicitudes antes y después; permanecen iguales. La tabla de constancias inicia vacía: la institución debe revisar a los estudiantes existentes.

Las pruebas offline usan perfiles ficticios previamente verificados para escenarios anteriores y un recorrido independiente de declaración y revisión para comprobar permisos, rechazo, aprobación y concurrencia. No se aprueban cuentas reales por ejecutar pruebas.
