# Agenda de citas y atención

## Flujo implementado

1. El psicólogo publica intervalos en **Agenda**. La hora corresponde a Guatemala. Se admiten duraciones de 15, 30 o 50 minutos, o duración variable; modalidad presencial, virtual o ambas.
2. El estudiante, con Big Five finalizado, consentimiento vigente y vínculo activo, abre **Citas**, pulsa un día y solicita una franja. Si se ofrecen ambas modalidades, elige una.
3. La solicitud **reserva inmediatamente el horario**. Los demás estudiantes ven «Reservado / ocupado», sin conocer quién lo reservó. Una transacción y un bloqueo por psicólogo/día comprueban de nuevo la disponibilidad, incluso cuando hay solicitudes simultáneas.
4. El psicólogo acepta o rechaza la solicitud en Agenda o en la ficha del estudiante. Una solicitud no constituye todavía una cita confirmada.
5. Estudiante o psicólogo pueden cancelar antes del inicio, indicando un motivo. Rechazar o cancelar libera la franja. El estudiante puede reprogramar seleccionando otro horario: la anterior queda como Reprogramada, la nueva vuelve a Solicitada y requiere aceptación. Si el nuevo horario ya está ocupado, se conserva la cita original.
6. Después del fin del horario de una cita confirmada, el psicólogo registra asistencia. Si asistió, escribe el resultado clínico **privado**, la reseña/recomendaciones y un resultado breve **para compartir**. Puede recomendar seguimiento e indicar una próxima cita; esta se solicita y confirma normalmente, sin crear reservas automáticas.
7. Al cerrar, el estudiante ve únicamente los textos compartidos en **Mis resultados**. Una inasistencia no genera un diagnóstico ni resultado clínico. El historial individual y el general muestran los mismos registros; solo el profesional que atendió puede abrir su detalle privado.

El listado de estudiantes se ordena por la próxima cita futura, dejando al final a quienes no tienen cita. Incluye filtros de Big Five, solicitudes, confirmadas, pendientes, reprogramadas, canceladas, terminadas e inasistencia.

## Reglas de esta primera versión

- Las citas de duración variable reservan el intervalo completo publicado; no se inventa una duración para permitir reservas que puedan cruzarse.
- No se puede quitar un horario ni bloquear/eliminar un día con citas solicitadas o confirmadas que lo ocupen. Primero deben cancelarse o coordinarse nuevas solicitudes.
- Las solicitudes vencidas no se confirman retroactivamente. El profesional las rechaza y coordina una nueva fecha. No hay vencimiento automático de solicitudes.
- Las atenciones cerradas conservan su registro; esta versión no incorpora edición posterior ni borrado de historias clínicas.
- Retirar el consentimiento de Big Five suspende nuevas solicitudes y reprogramaciones. Se conserva acceso a las citas propias y posibilidad de cancelar una reserva futura. No reutiliza ni vuelve a aplicar automáticamente el cuestionario.
- La reserva virtual registra la modalidad. La generación de enlaces de videollamada y las notificaciones por correo quedan para una siguiente etapa.
- Los datos del piloto local se distinguen de los institucionales. No se convierten en registros de producción al cambiar de entorno. La configuración institucional y de privacidad pendiente del Big Five continúa vigente.

## Base de datos y otra computadora

Migración nueva: **20261010025838_AddCitasAtencion**. Agrega `Citas`, `EventosCita` y `AccesosAtencion`, sus claves, restricciones e índices. Conserva las migraciones anteriores y los datos existentes. No necesita un script de datos de pacientes.

En otra computadora, después de incorporar estos cambios y configurar una conexión privada a PostgreSQL:

```powershell
dotnet restore Zuni/Zuni.csproj
dotnet build Zuni/Zuni.csproj
dotnet ef database update --project Zuni/Zuni.csproj --startup-project Zuni/Zuni.csproj
dotnet run --project Zuni/Zuni.csproj
```

Requiere .NET 8 y `dotnet-ef` compatible con EF Core 8. Las contraseñas/conexiones privadas no se publican en Git. Antes de probar en un entorno compartido, revisar las migraciones pendientes y respaldar esa base según su procedimiento habitual.

Para probar manualmente: usar dos estudiantes vinculados con cuestionarios finalizados, solicitar el mismo horario desde ambas cuentas, comprobar que solo una reserva se guarda, aceptar/cancelar/reprogramar y comprobar el calendario. Para atención, usar una cita confirmada cuyo horario ya terminó; verificar los textos de Mis resultados y el detalle privado con la cuenta del profesional.

## Validación

Las comprobaciones de `tests/BigFive.Checks` usan una base PostgreSQL temporal y la aplicación MVC real con login y antifalsificación. Cubren colisiones entre estudiantes, permisos, revisiones obsoletas, cancelación/reprogramación, retiro de consentimiento, asistencia, separación de resultados privados/compartidos, auditoría, filtros y orden por próximas citas. La base temporal se elimina al terminar.

`tests/CombinedModel.Checks` verifica el snapshot y los caminos de migración desde una base nueva y desde Evaluaciones. `tests/StudentProfile.Checks` verifica que el perfil y sus validaciones se conservan.

Resultado local: 219 comprobaciones Big Five/citas/PostgreSQL/HTTP, 30 de modelo/migraciones y 35 de perfil aprobadas (284 en total). JavaScript pasó la comprobación de sintaxis. Compilación sin errores; NuGet emitió NU1900 porque la consulta de auditoría de dependencias no estuvo disponible por red. Se revisaron visualmente el calendario con reserva y el formulario de atención mediante páginas de prueba ficticias.

En esta computadora se aplicó la migración a `zuni`, con cero migraciones pendientes. Reiniciar la aplicación que estaba abierta permite cargar el código nuevo.
