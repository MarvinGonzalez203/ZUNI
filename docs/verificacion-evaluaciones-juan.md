# Verificación final de Evaluaciones y Resultados

Fecha: 9 de octubre de 2026. Rama: `feature/student-dashboard-clean`.
Aplicación: ASP.NET Core 8, con PostgreSQL local `ZuniDesarrollo`.
Titular: Juan Francisco Pérez Samayoa, `jperezs@miumg.edu.gt`, carné `7490-17-5760`.

## Resultado ejecutado

- Compilación: 0 errores y 0 advertencias, con dependencias previamente restauradas y auditoría NuGet desactivada. No se hizo una auditoría de vulnerabilidades de paquetes.
- 123 comprobaciones HTTP/PostgreSQL aprobadas en la ejecución completa.
- 17 comprobaciones adicionales aprobadas en Chromium (Microsoft Edge, modo headless), con formularios, JavaScript, navegación y diálogo modal reales.
- 8 pruebas JavaScript aprobadas: cuatro de Evaluaciones y cuatro del perfil.
- Ejecutado y aprobado `tests/StudentProfile.Checks`: validación y presentación del perfil, carnés de 10/11 dígitos y procesamiento CSV.
- Fallos pendientes de las pruebas ejecutadas: 0.

La salida de la ejecución completa está en `.visual-check/juan-verification/resultado.txt`, excluida de Git. La captura del comprobante obtenido durante la prueba está en `.visual-check/juan-verification/finalizada.png`. Ese comprobante temporal se retiró al restaurar los datos.

## Funciones verificadas

1. Cada evaluación de Juan aparece una sola vez en su categoría correcta, con título y estado provenientes de PostgreSQL. La interfaz actual presenta cuatro secciones por estado.
2. Asignada ofrece instrucciones y no inicia hasta habilitarse. Pendiente cambia a En proceso mediante el endpoint real.
3. Hábitos de estudio recupera su primera respuesta de prueba, guarda una segunda, muestra 67 %, cierra sesión y recupera el avance en una nueva sesión independiente. Al completar la tercera pregunta muestra 100 %.
4. Guardar y revisar persiste las respuestas antes de abrir la confirmación. El diálogo real muestra Cancelar y Confirmar y finalizar. Cancelar cierra el diálogo, conserva En proceso y conserva las tres selecciones. Confirmar genera fecha, comprobante y estado Finalizada, comprobados en PostgreSQL.
5. Una evaluación finalizada no ofrece formulario editable y rechaza modificaciones mediante POST. Repetir la finalización conserva el mismo comprobante.
6. El servidor rechaza finalizar sin confirmación, con preguntas obligatorias incompletas, guardar respuestas con identificadores manipulados o de otra evaluación y sobrescribir con una revisión antigua.
7. Bienestar académico muestra a Juan la puntuación ficticia de 9 y las observaciones autorizadas. Al ocultar temporalmente su publicación, no se entregan puntuación ni observaciones. Su publicación original fue restaurada.
8. Finalizar otra demostración no publica automáticamente su resultado. Solo aparece el aviso de publicación pendiente.
9. Evin, actuando desde las rutas estudiantiles, recibe 404 al consultar los cuatro identificadores de Juan y sus revisiones. Los intentos de iniciar, guardar y finalizar una asignación de Juan no cambian estado, respuestas ni resultados. Agregar el ID de Juan a la URL de Resultados tampoco expone sus datos.
10. Juan no accede a las páginas administrativas ni modifica asignaciones mediante su endpoint administrativo. Los POST sin antifalsificación devuelven 400.
11. Las sesiones ausentes, manipuladas, vencidas o con sello inválido no acceden a Evaluaciones, Resultados ni al comprobante. El cierre de sesión real retira el acceso. El formulario real de login rechaza una contraseña incorrecta.
12. Las rutas de estudiante, perfil, citas, administración, importación y selector de los dos roles de Evin respondieron según sus permisos. `/Panel` es exclusivo de cuentas con ambos roles; Juan usa `/Estudiante`.

## Autenticación y alcance

Las sesiones válidas de prueba se firmaron localmente usando el identificador, sello y roles vigentes de la cuenta. Pasaron por la validación de la aplicación contra PostgreSQL. No se conoció ni se cambió la contraseña de Juan. **No se ejecutó un login exitoso escribiendo su contraseña**: ese paso corresponde a la comprobación manual del propietario. Sí se ejecutaron login inválido, cierre de sesión y rechazo de sesiones inválidas.

La regresión comprueba las rutas y las validaciones indicadas; no equivale a probar todas las operaciones de cada módulo de ZUNI. La auditoría conserva las operaciones administrativas temporales de ocultar y publicar realizadas por la suite.

## Fallos encontrados y correcciones

Tres ejecuciones iniciales se detuvieron por expectativas incorrectas del nuevo test: esperaba que cerrar sesión llevara al login, aunque el controlador lleva al inicio, y esperaba acceso/redirección estudiantil en `/Panel`, aunque ese selector exige los dos roles. Se corrigieron las expectativas de las pruebas para comprobar el comportamiento implementado. Todas esas ejecuciones restauraron los datos mediante `finally`.

No se identificó un defecto del código de Evaluaciones o Resultados que requiriera modificar la aplicación. No se agregaron tablas, migraciones, usuarios ni asignaciones.

## Estado final comprobado

| Evaluación de Juan | Estado | Respuestas | Resultado |
|---|---|---|---|
| Bienestar emocional | Asignada | 0/3 | Sin publicar |
| Adaptación universitaria | Pendiente | 0/3 | Sin publicar |
| Hábitos de estudio | En proceso | 1/3; 33 % | Sin publicar |
| Bienestar académico | Finalizada | 3/3; fecha y comprobante originales | Publicado; puntuación ficticia 9 |

La restauración compara exactamente los registros JSON originales de asignaciones, respuestas y resultados de Juan. Las huellas de las otras tablas y de las evaluaciones fuera de Juan coinciden con el estado inicial, incluidas credenciales, roles, correos, perfiles, definiciones y datos de Evin. Se conservó auditoría. No se realizaron commits, merges, rebase ni push.

## Archivos agregados en esta verificación

- `tests/Evaluaciones.Juan.Integration/Evaluaciones.Juan.Integration.csproj`: ejecutable de verificación.
- `tests/Evaluaciones.Juan.Integration/Program.cs`: pruebas HTTP/PostgreSQL, instantánea, restauración y comparación de datos.
- `tests/evaluaciones-browser.cjs`: prueba de navegador real mediante Playwright, sesión privada recibida por stdin.
- `docs/verificacion-evaluaciones-juan.md`: este informe.

Las pruebas anteriores y los archivos de aplicación no se modificaron en esta continuación. Se generaron binarios y evidencias locales temporales.

## Prueba manual

1. Inicia ZUNI desde tu entorno habitual. Entra con `jperezs@miumg.edu.gt` y tu contraseña actual. Abre Estudiante → Evaluaciones.
2. En Asignadas, abre Bienestar emocional: consulta instrucciones; debe permanecer sin iniciar.
3. En Pendientes, abre Adaptación universitaria y pulsa Iniciar evaluación: debe pasar a En proceso.
4. Abre Hábitos de estudio: debe mostrar una respuesta guardada y 33 %. Completa otra pregunta, pulsa Guardar avance, cierra sesión y vuelve a entrar: debe recuperar dos respuestas y 67 %.
5. Completa las tres preguntas, pulsa Guardar y revisar respuestas y luego Finalizar evaluación. Pulsa Cancelar y Volver a editar: conserva las respuestas y sigue en proceso.
6. Vuelve a revisar y pulsa Confirmar y finalizar: aparecen fecha y comprobante. Después del envío no hay campos editables. El resultado de esta nueva finalización queda pendiente de autorización.
7. Abre la demostración original Bienestar académico en Finalizadas: muestra su fecha y comprobante. En Resultados se muestra su puntuación ficticia de 9, con aviso de demostración sin interpretación clínica.
8. Desde otra cuenta estudiantil, intenta abrir una URL de evaluación de Juan: debe devolver 404. En Resultados solo se muestran los registros de la cuenta autenticada.

Los pasos manuales 3–6 cambian tus demostraciones realmente. La restauración descrita corresponde a la suite automatizada; no se ejecuta al probar manualmente.

## Reproducción automatizada

Desde la raíz del repositorio, compila `Zuni/Zuni.csproj` hacia `.visual-check/eval-runtime` e inicia ese binario en Development, con raíz de contenido `Zuni`, en `http://127.0.0.1:7111`. La configuración debe apuntar exclusivamente a `ZuniDesarrollo` local. No uses estas pruebas mientras editas las demostraciones desde otra sesión.

Playwright debe estar disponible para Node (en este entorno se usó el paquete incluido en el runtime de Codex mediante `NODE_PATH`); Microsoft Edge debe estar instalado. Ejecuta:

```powershell
dotnet run --project tests/Evaluaciones.Juan.Integration/Evaluaciones.Juan.Integration.csproj -p:NuGetAudit=false
node --test tests/evaluaciones-ui.test.cjs tests/student-profile.test.cjs
dotnet run --project tests/StudentProfile.Checks/StudentProfile.Checks.csproj -p:NuGetAudit=false
```

El ejecutable principal guarda la instantánea antes de las operaciones y restaura en `finally`, incluso si una comprobación falla. Un cierre forzado del proceso o pérdida de conexión durante la restauración requiere revisar la instantánea privada antes de repetir.
