# Calendario conectado

La disponibilidad se configura en **Psicólogo → Agenda** y se guarda en PostgreSQL. El estudiante la consulta en **Citas** después de finalizar Big Five, con consentimiento y vínculo profesional vigentes. Cada consulta vuelve a validar esos requisitos. No se envían nombres de otros estudiantes, citas, respuestas ni motivos de consulta al calendario.

El psicólogo puede añadir rangos entre 08:00 y 18:00 (hora de Guatemala), indicar modalidad presencial, virtual o ambas y duración de 15, 30, 50 minutos o sin duración establecida (variable). Al pulsar un día del calendario se abre una ventana emergente con sus horarios. Para el psicólogo incluye los formularios de añadir/quitar horarios, marcar ocupado, habilitar y quitar configuración, vinculados a la fecha seleccionada. El estudiante solo consulta los detalles. Se retiró la lista extensa de horarios debajo del calendario. Se permiten fechas desde hoy hasta doce meses adelante. El servidor rechaza fechas pasadas, valores inválidos, rangos demasiado cortos y cruces entre horarios. Un bloqueo de transacción por profesional/día impide duplicados ante ediciones simultáneas.

**Marcar ocupado** conserva los rangos pero los oculta al estudiante. **Habilitar día** vuelve a mostrarlos. **Quitar configuración** elimina los horarios de ese día y lo deja sin disponibilidad definida. Sin configuración se muestra gris; disponible verde; ocupado rojo, con texto además del color. Para hoy solo se muestran rangos con una cita completa futura según la duración definida; sin duración establecida se muestra el intervalo que todavía queda disponible, sin atribuir una duración fija. Los cambios se ven al cargar o actualizar el calendario; no hay actualización automática en tiempo real.

Esta entrega permite consultar disponibilidad. Todavía no crea reservas, enlaces virtuales, tokens de asistencia ni bloqueos derivados de citas. La modalidad virtual indica disponibilidad, sin generar una sala de videollamada.

## Datos y migración

`20261009233156_AddAgendaPsicologo` añade únicamente `DiasAgendaPsicologo` y `HorariosAgendaPsicologo`, sus índices, restricciones y claves foráneas. No modifica las migraciones de Evaluaciones ni Big Five ni borra sus registros. Los horarios antiguos del prototipo eran temporales y se deben definir una vez en la nueva agenda.

`20261010001005_AmpliarOpcionesAgenda` amplía las restricciones para permitir duración variable (valor interno 0) y modalidad Ambas. Conserva los registros antiguos de 45/60 minutos, pero el formulario y el servicio ya no permiten crear nuevos con esas duraciones. No altera respuestas ni consentimiento. El nuevo horario se marca explícitamente como una inserción para evitar que Entity Framework intente actualizar una fila inexistente al reutilizar un día.

En otra computadora: .NET 8, PostgreSQL, conexión privada configurada y aplicar las migraciones pendientes desde la raíz del repositorio:

```powershell
dotnet ef database update --project Zuni/Zuni.csproj --startup-project Zuni/Zuni.csproj
```

Reiniciar la aplicación después de actualizar. Si está ejecutándose en Visual Studio, detenerla, compilar y volver a iniciar. No copiar conexiones privadas ni datos de prueba al repositorio. Big Five conserva los campos institucionales configurables y el modo local de pruebas mientras estén pendientes.

## Panel del estudiante

Evaluaciones muestra únicamente el acceso a Big Five. Las demostraciones genéricas se conservan en la base y su manifiesto, pero se retiraron de la vista. Resultados queda sin puntuaciones ni demostraciones: muestra un aviso vacío hasta implementar recomendaciones escritas por el psicólogo.

## Verificación

`tests/BigFive.Checks` usa una base PostgreSQL local temporal, aplica todas las migraciones y prueba login, antifalsificación, consentimiento, finalización, permisos, persistencia del calendario, cruces, bloqueo/habilitación, retirada y ediciones simultáneas. La base se elimina al terminar. `tests/CombinedModel.Checks` comprueba que el snapshot y última migración coinciden y que la actualización conserva tablas existentes. `tests/StudentProfile.Checks` verifica perfil y CSV.

```powershell
dotnet build Zuni/Zuni.csproj --configuration BigFiveCheck -p:NuGetAudit=false
dotnet run --project tests/BigFive.Checks --configuration BigFiveCheck -p:NuGetAudit=false
dotnet run --project tests/CombinedModel.Checks --configuration BigFiveCheck -p:NuGetAudit=false
dotnet run --project tests/StudentProfile.Checks --configuration BigFiveCheck -p:NuGetAudit=false
```

La prueba HTTP requiere una conexión PostgreSQL local con permiso para crear y eliminar su propia base temporal; no escribe en la base de trabajo.
