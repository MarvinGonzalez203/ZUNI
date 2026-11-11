# ZUNI 1.3.0 — asignaciones desde fichas y fluidez

Entrega local pendiente de revisión y publicación. No cambia la versión desplegada
en zuni.lat ni realiza asignaciones sobre las cuentas reales durante las pruebas.

- En la ficha del estudiante, Acciones incluye **Asignar psicólogo**.
- En la ficha del psicólogo, Acciones incluye **Asignar estudiantes**.
- El Administrador puede vincular cuentas antes de recibir la solicitud de atención.
  La vinculación no crea consentimientos, verificaciones de edad ni expedientes.
- Se pueden reasignar y finalizar vínculos con motivo administrativo, revisión de
  concurrencia, historial y auditoría. Se mantienen los bloqueos por citas activas.
- Las reservas y confirmaciones requieren una solicitud consentida. La programación
  profesional puede permanecer pendiente mientras se completan los requisitos.
- Los listados de asignación muestran 25 filas por página y permiten buscar por nombre
  o carné, sin cargar el directorio completo ni contenidos clínicos.
- Las guías describen ambos accesos desde las fichas.

La campana y el número de versión del ensamblado pasan a 1.3.0. El historial recoge
las rondas locales previas: 1.2.1 (asignación manual, guías y calendario), 1.2.2
(requisitos de contraseña) y 1.2.3 (configuración privada de inicio local).
La campana muestra las tres entradas locales más recientes. Leer una versión anterior
no marca automáticamente la nueva como leída.

## Rendimiento

El observador de campos obligatorios antes recorría todos los formularios ante
cualquier cambio de nodos, incluidas las marcas de contraseña. Ahora ignora texto
y marcas propias y agrupa cambios relevantes por formulario en un fotograma.
La prueba de regresión verifica que 100 cambios de texto no hacen nuevos barridos
y que 100 cambios de atributos del mismo formulario producen una sola revisión local.

El desplazamiento del menú solo actualiza su indicador una vez por fotograma;
las etiquetas y los títulos se actualizan al cambiar el diseño o el idioma.
La comprobación de contraseñas solo procesa entradas de contraseña nueva.
En desarrollo se conservan errores y advertencias de base de datos sin volcar cada
consulta SQL en la salida de Visual Studio.

## Comprobaciones

`tests/Seguridad.Offline/AsignacionesFichaChecks.cs` cubre las dos fichas, selección
sin solicitud, exclusividad del Administrador, antiforgery, datos manipulados,
concurrencia, motivos, citas activas, finalización, auditoría y requisitos independientes.
Se ejecuta con la suite de seguridad o aisladamente con `--asignaciones-ficha`.

Las suites de seguridad y agenda usan SQLite en memoria y HTTP local. Las pruebas
de JavaScript están en `rendimiento-formularios.checks.cjs`,
`requisitos-contrasena.checks.cjs` y `calendario-estudiante.checks.cjs`.
No equivalen a una prueba de concurrencia real en PostgreSQL ni a un benchmark de
tiempos de respuesta de producción.
