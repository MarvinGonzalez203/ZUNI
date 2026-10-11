# Pruebas offline de Agenda y Citas

Ejecutar desde la raíz del repositorio:

```text
dotnet run --project tests/Agenda.Offline/Agenda.Offline.csproj
```

El ejecutable usa SQLite en memoria y un servidor HTTP de prueba ligado únicamente a `127.0.0.1` con puerto dinámico. No arranca `Program.cs` de Zuni, no ejecuta seed, no carga secretos y no abre PostgreSQL. Las cuentas de prueba, las citas y las llaves del host solo existen en memoria. El host se cierra al terminar.

SQLite 8.0.23 se usa únicamente en este proyecto de pruebas (ya estaba disponible en el equipo). No se agregaron paquetes a la aplicación Zuni.

Las comprobaciones cubren reglas horarias, permisos persistidos, SecurityStamp, cambio obligatorio, perfiles, asignaciones, propiedad, roles combinados, reserva sin Big Five, solapamiento, cancelación, asistencia, historial, antigüedad/horizonte de consulta, metadatos de la migración, snapshot y traducción de consultas Npgsql. Las pruebas HTTP renderizan vistas y layouts reales, envían formularios y comprueban antiforgery, no-cache y campos de identidad manipulados.

SQLite no reproduce la concurrencia de PostgreSQL. Se verifica el uso de transacciones SERIALIZABLE y los índices filtrados en los metadatos. Antes de publicar, validar en una base PostgreSQL descartable: dos reservas simultáneas del mismo horario; intervalos parcialmente superpuestos para el mismo estudiante; desactivación de disponibilidad frente a reserva; cancelación frente a registro de asistencia. No ejecutar estas pruebas sobre datos reales.

El host permite suplantar actores mediante una cabecera exclusivamente dentro de este ejecutable de pruebas; ese middleware no forma parte de Zuni. Su almacenamiento y protección de llaves efímeras tampoco cambian la configuración real de la aplicación.
