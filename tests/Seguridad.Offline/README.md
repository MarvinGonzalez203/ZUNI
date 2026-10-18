# Comprobaciones offline de seguridad y administración

Ejecutar desde la raíz del repositorio:

```text
dotnet run --project tests/Seguridad.Offline -c Release
```

Usa SQLite en memoria y un host HTTP efímero ligado a `127.0.0.1` con puerto dinámico. No arranca `Program.cs` de Zuni,
no ejecuta seeds, no carga secretos y no abre PostgreSQL. Las cuentas y contraseñas de prueba se generan en cada ejecución
y solo existen en memoria.

El host reutiliza la configuración real de seguridad de Zuni (`SesionZuni`, `PoliticasAcceso`,
`CabecerasSeguridadMiddleware`, `ContenidoPrivadoNoCacheMiddleware` y `CambioContrasenaObligatorioMiddleware`).

Cubre:

- Privacidad del Director: carga anónima por bloques, métricas y estadísticas agregadas, rutas directas y programación
  individual denegadas en el servidor, avisos antes de Big Five/cuestionarios y coherencia estática de `deploy/debian`
  (`PrivacidadDirectorChecks.cs`). Citas históricas propuestas por un Director: `LegadoDirector.cs`.

- Configuración institucional: nombre, soporte, tres políticas de correo, dominio exacto,
  registro pendiente, altas administrativas, cierre de registro, revisión concurrente, auditoría,
  antiforgery y conservación del acceso de cuentas existentes (`InstitucionChecks.cs`).

- Director: catálogo, editor, dimensiones/preguntas/opciones, activación, asignaciones,
  antiforgery, acceso por rol, duplicados y conservación de contenido asignado (`PruebasChecks.cs`).

- Solicitudes de atención: más de un psicólogo, asignación manual, reasignación, finalizar asignación, cerrar,
  historial, bloqueo por citas activas, índice de asignación vigente única, auditoría sin contenido clínico,
  vistas administrativas sin datos clínicos y nueva solicitud después del cierre.
- Cambio de rol bloqueado para psicólogos con asignaciones vigentes.
- Contraseña temporal: hash, cambio obligatorio, SecurityStamp, tokens invalidados, auditoría sin contraseña, no-store.
- Inicio de sesión: contador de fallos, bloqueo de 15 minutos, reinicio del contador, mensajes genéricos, hash ficticio.
- Autorregistro pendiente de activación, activación por el Administrador y cuentas creadas por el Administrador.
- Sesión: sliding de 30 minutos, máximo absoluto de 8 horas, revalidación de SecurityStamp, cookie HttpOnly/SameSite/Secure.
- Cabeceras de seguridad y rate limiting (HTTP 429).
- Propuestas de cita de Dirección (`PropuestasDirectorChecks`): directorio sin indicadores de atención, propuesta sin confirmación unilateral, aceptar/reprogramar/rechazar por el psicólogo, avisos administrativos, propuestas ajenas y rutas clínicas denegadas, POST manipulado, duplicados, cruces, mayoría de edad y auditoría sin contenido clínico.

SQLite no reproduce la concurrencia SERIALIZABLE de PostgreSQL; la concurrencia real se valida aparte en una base descartable.
