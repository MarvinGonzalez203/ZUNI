# Entrega del panel Estudiante para coordinación

9 de octubre de 2026. Rama local: `feature/student-dashboard-clean`.
El trabajo de integración del compañero corresponde a `feature/student-psychologist-link`.

## Contenido de esta entrega

- Tablero y navegación estudiantil, consulta y edición del perfil, indicador de progreso del perfil y validaciones de carnés de 10/11 dígitos.
- Evaluaciones respaldadas por PostgreSQL: Asignada, Pendiente, En proceso y Finalizada; guardado manual, recuperación de respuestas, porcentaje de progreso, revisión, diálogo de confirmación, comprobante y bloqueo posterior al envío.
- Resultados filtrados por la sesión del estudiante; puntuación y observaciones solo si un administrador autoriza la publicación. Las nuevas finalizaciones quedan sin publicar.
- Servicios, configuración EF, controladores, vistas y pruebas necesarios para estos flujos. Se incluyen los cambios administrativos locales previos que los acompañan: selector Administrador/Estudiante, navegación, gestión de estudiantes y acceso a habilitación/publicación de evaluaciones, además de importación CSV con vista previa. No se incluyen herramientas privadas que cambiaron cuentas o credenciales.

## Pendientes y partes visuales

- Citas del Estudiante sigue como Próximamente: no permite solicitar o reservar citas.
- La relación Estudiante–Psicólogo y su búsqueda/acceso cruzado siguen pendientes; `integracion-estudiante-psicologo.md` es una propuesta, no una implementación.
- El resumen/gráfico de evaluaciones en el tablero es un marcador visual y no muestra estadísticas consultadas a PostgreSQL. Algunos textos y etiquetas de próximos servicios del tablero requieren coordinación para reflejar los módulos ya disponibles.
- Los instrumentos y resultados cargados son demostraciones sin interpretación clínica. No se implementaron instrumentos oficiales ni diagnósticos.
- No se implementó envío de correo productivo para recuperación de cuentas.

## Migraciones y configuración

La migración existente `20261009043711_AddEvaluaciones` agrega Evaluaciones, PreguntasEvaluacion, AsignacionesEvaluacion, RespuestasEvaluacion y ResultadosEvaluacion con sus restricciones. Se creó y aplicó en la sesión anterior a la base local `ZuniDesarrollo`; en esa sesión también se cargaron las demostraciones de Evin y Juan. Los datos locales no se transfieren con Git.

Esta publicación no genera ni aplica migraciones y no modifica la base de datos. No agrega scripts SQL independientes. Los creadores de demostraciones son utilidades C# con verificaciones de base local e identidad; no se ejecutan automáticamente al iniciar la aplicación. No deben ejecutarse en una base distinta sin preparar y aprobar las cuentas de demostración correspondientes.

La configuración de servicios registra `EvaluacionesService`. La conexión permanece en configuración privada fuera del repositorio; los `appsettings` públicos contienen únicamente logging y AllowedHosts. Se excluyen copias de bases, claves de sesiones, herramientas privadas, archivos de IDE y salidas bin/obj/verificación.

## Verificación

En esta preparación para publicar se ejecutaron nuevamente:

- Compilación `dotnet build Zuni/Zuni.csproj --no-restore -o .visual-check/publish-runtime -p:NuGetAudit=false`: 0 errores y 0 advertencias.
- `node --test tests/evaluaciones-ui.test.cjs tests/student-profile.test.cjs`: 8 aprobadas.
- `dotnet run --no-restore --project tests/StudentProfile.Checks/StudentProfile.Checks.csproj -p:NuGetAudit=false`: validaciones de perfil/carné y CSV aprobadas.

En la verificación inmediatamente anterior, ejecutada contra PostgreSQL y Chromium, pasaron 123 comprobaciones HTTP/PostgreSQL y 17 de navegador. La restauración conservó exactamente las cuatro demostraciones originales de Juan y los datos de Evin. Detalle en `verificacion-evaluaciones-juan.md`. No se repitieron pruebas que escriben en PostgreSQL durante esta publicación. El login exitoso escribiendo la contraseña personal queda como prueba manual; las sesiones de integración están firmadas localmente.

## Estado de Git antes del commit de entrega

Tras consultar GitHub, la rama remota estaba en `8c66192`, con seis commits que la rama local aún no contiene. Localmente había dos commits que no están en la rama remota:

- `067e9e0`: edición del perfil y propuesta de vinculación.
- `2c7030c`: tablero, edición y validación del perfil.

El ancestro común `36a2cf5` y anteriores contienen la versión inicial del tablero, perfil y navegación estudiantil y el panel de Psicólogo ya publicados. Los cambios de Evaluaciones/Resultados y administración acompañante estaban sin commit. La rama remota también recibió cambios de Mi Cuenta y contraseña de otro colaborador que no se incorporan en esta entrega.

Las referencias divergen: un push normal requiere que el remoto sea ancestro del commit local. Si GitHub rechaza el push, se informa y se conserva el commit local sin merge, rebase ni push forzado. El estado definitivo y el identificador del commit se comunican después del intento de publicación. Las modificaciones del panel Estudiante quedan en pausa para coordinar la integración.
