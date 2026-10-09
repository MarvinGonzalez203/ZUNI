# Demostraciones portables de Evaluaciones y Resultados

Utilidad: `tools/Evaluaciones.Demo`. Reutiliza las entidades, restricciones y patrón transaccional de las demostraciones originales, pero recibe cuentas de tu propia base. No depende de IDs, nombres, correos ni carnés de Juan/Evin. No crea usuarios, contraseñas o roles y no cambia su configuración.

## Requisitos

- SDK .NET 8 y PostgreSQL **local y no compartido**. Solo acepta hosts `localhost`, `127.0.0.1` o `::1`. `--confirm-local` confirma que el destino no es una base compartida; el programa no puede detectar que otras personas usen una base alojada en localhost.
- Las migraciones del proyecto deben estar aplicadas, incluida `20261009043711_AddEvaluaciones`. La utilidad valida el historial y nunca crea tablas ni aplica migraciones. Si faltan, debe revisarlas y aplicarlas el responsable antes, por separado.
- Un usuario existente, activo, con rol Estudiante, cuyo ID interno se pasa por `--student-id`.
- Un usuario existente, activo, con rol Administrador, cuyo ID se pasa por `--publisher-id`. Se necesita como publicador del resultado de prueba, por la relación existente de Resultados. Puede ser la misma cuenta si tiene ambos roles. No se le agrega ningún rol a nadie.
- Configura la conexión privada de la misma forma que ZUNI: User Secrets del proyecto `Zuni/Zuni.csproj` o la variable `ConnectionStrings__DefaultConnection`. Usa la configuración local que ya tienes; no copies credenciales al repositorio ni las pases como argumentos de la utilidad.

La opción `--database` debe coincidir exactamente con el nombre de la base en esa conexión. Ejecuta los comandos desde la raíz del repositorio. Los IDs son los de `AspNetUsers.Id`, no los de `PerfilesEstudiante.Id`; obtén los correspondientes a cuentas ficticias de tu entorno local. La utilidad no imprime nombres, correos, contraseñas o cadenas de conexión.

## Datos que crea

Cada lote contiene cinco evaluaciones independientes, de tres preguntas obligatorias con escala 1–5:

| Demostración | Estado | Respuestas | Resultado inicial |
|---|---|---|---|
| Bienestar emocional | Asignada | 0/3 | Ninguno |
| Adaptación universitaria | Pendiente | 0/3 | Ninguno |
| Hábitos de estudio | En proceso | 1/3; 33 % | Ninguno |
| Bienestar académico publicado | Finalizada | 3/3; fecha y comprobante | Ficticio, 9 puntos, publicado |
| Bienestar académico sin publicar | Finalizada | 3/3; fecha y comprobante | Ficticio, 9 puntos, oculto |

Hay dos finalizadas para probar a la vez publicación y ocultación. Todos los títulos indican DEMOSTRACIÓN LOCAL y un identificador de lote; preguntas y observaciones son ficticias, sin interpretación clínica ni diagnósticos. No se reutilizan ni modifican definiciones/asignaciones de otros usuarios.

## Comandos

Sustituye los IDs ficticios del ejemplo por los de tus cuentas locales y el nombre de base por el configurado. El lote debe contener 1–32 letras minúsculas, números o guiones; utiliza el mismo nombre y manifiesto al repetirlo.

```powershell
$demoArgs = @(
  '--database', 'ZuniDesarrollo',
  '--student-id', 'ID_ESTUDIANTE_LOCAL',
  '--publisher-id', 'ID_ADMIN_LOCAL',
  '--batch', 'prueba-integracion'
)

# Consulta cuentas/esquema y muestra el plan; no inserta registros.
dotnet run --project tools/Evaluaciones.Demo -- preview @demoArgs

# Carga transaccional: primera ejecución 5; repetición 0.
dotnet run --project tools/Evaluaciones.Demo -- create @demoArgs --confirm-local

# Muestra código, ID de asignación, estado, respuestas y publicación actuales.
dotnet run --project tools/Evaluaciones.Demo -- list @demoArgs

# Retira exclusivamente este lote; no borra usuarios ni sus otros datos.
dotnet run --project tools/Evaluaciones.Demo -- remove @demoArgs --confirm-local
```

Para ayuda: `dotnet run --project tools/Evaluaciones.Demo -- --help`.

El manifiesto se guarda por defecto en `.demo-data/ZUNI-LOCAL-DEMO-<huella>.json`, excluido de Git. Puedes fijar una ruta con `--manifest RUTA` en **todos** los comandos. Contiene versión, base, ID de estudiante, nombre de lote y marcador de propiedad aleatorio; no contiene conexión ni credenciales. Conserva ese archivo local: `list` y `remove` requieren el original. No sobrescribe un archivo existente. Se guarda antes de insertar para que un corte después del commit no deje el lote sin su manifiesto.

Cambiar el nombre de lote o estudiante crea un conjunto independiente. Usar los mismos valores con otro manifiesto no permite apropiarse del conjunto previo: se rechaza por propiedad diferente. `preview` describe el estado inicial previsto; no reinicia un lote que ya fue usado. Para conocer el estado actual usa `list`.

## Cómo probar desde ZUNI

1. Inicia sesión como el estudiante elegido; si la cuenta tiene cambio obligatorio de contraseña, completa su flujo habitual. Abre Estudiante → Evaluaciones.
2. Consulta la Asignada. Sigue bloqueada para iniciar hasta que un administrador la habilite.
3. Inicia la Pendiente: pasa a En proceso. Contesta una pregunta y pulsa Guardar avance; vuelve a ingresar y comprueba que la selección persiste.
4. Continúa Hábitos de estudio, que comienza con 1/3 y 33 %. Guarda una segunda pregunta y verifica 67 % al volver a abrir.
5. Completa las preguntas y pulsa Guardar y revisar respuestas → Finalizar evaluación. Cancelar conserva las respuestas guardadas y el estado. Confirmar y finalizar genera fecha/comprobante y bloquea la edición. El resultado de esa nueva finalización queda sin publicar.
6. En Resultados, la demostración publicada muestra 9 puntos ficticios. La no publicada muestra únicamente el aviso de publicación pendiente: no entrega puntuación ni observaciones.
7. Desde otro estudiante, intenta abrir un ID mostrado por `list`: debe devolver 404 y no permitir modificación. Sus Resultados no deben incluir los de la cuenta elegida.

Repetir `create` después de estas pruebas **no** reinicia avances, comprobantes ni publicación. Para empezar de cero, retira ese lote y vuelve a crearlo conservando su manifiesto. El programa no ofrece un reset que sobrescriba respuestas.

## Identificación y retirada segura

Los códigos usan el prefijo `ZUNI-LOCAL-DEMO-<huella>` derivado del estudiante/lote. El manifiesto proporciona un marcador adicional de propiedad, y se verifican los IDs exactos de definiciones, preguntas y asignaciones antes de repetir o retirar. La eliminación usa esos IDs; nunca un borrado general por título o por `EsDemostracion`.

Se retiran solo las respuestas/resultados de sus cinco asignaciones y después sus asignaciones, preguntas y definiciones. Los avances o resultados generados al usar **estas** demostraciones también pertenecen al lote y se retiran con él. La auditoría histórica se conserva.

Si alguna definición se asignó posteriormente a otro estudiante, se agregaron preguntas, falta una parte del lote o cambió el marcador, la operación aborta y no retira ningún registro. No intentes saltarte esa comprobación: revisa las referencias con el responsable de la base. Las claves foráneas y la transacción también protegen frente a nuevas referencias concurrentes. Evita usar el lote desde otra sesión mientras lo retiras.

Las inserciones y la retirada son atómicas. Una colisión de ID/código, validación fallida o error del proveedor revierte la transacción. Un manifiesto creado antes de una inserción fallida puede quedar en disco; conserva su archivo para reintentar. Sin el manifiesto original no se hace limpieza por inferencia.

## Pruebas ejecutadas

Se compiló la aplicación, utilidad y ejecutable de pruebas con 0 errores y 0 advertencias. Se ejecutaron **37 comprobaciones contra una base PostgreSQL temporal nueva**, con cuentas ficticias generadas por el test: carga, cuatro estados, resultado publicado/oculto, repetición, avance recuperado en otra conexión, obligatorias, confirmación, comprobante, envío repetido, preservación del avance al recrear, propiedad incorrecta, referencia externa, retirada repetida y comparación exacta de todas las tablas originales después de retirar.

La base temporal fue eliminada en `finally`. No se ejecutó la utilidad en ZuniDesarrollo ni en una base compartida. Se ejecutaron también la ayuda y el rechazo de escritura sin `--confirm-local`. La primera ejecución de pruebas se detuvo porque el test no reconocía el tipo de excepción del servicio al rechazar una finalización incompleta; se corrigió el test y la ejecución completa posterior aprobó las 37 comprobaciones.

Para repetir la suite aislada:

```powershell
dotnet run --project tests/Evaluaciones.Demo.Checks
```

Este **test**, a diferencia de la utilidad, requiere permiso de CREATE DATABASE en el PostgreSQL local. Lee la conexión configurada solo para acceder al servidor local mediante su base administrativa, crea una base nueva con nombre aleatorio `ZuniDemoUtilityTest_<guid>`, aplica allí las migraciones existentes y la elimina al terminar. Nunca migra ni escribe en la base de trabajo configurada de ZUNI. No lo ejecutes contra un servidor compartido. Las pruebas ejercitan EF y el servicio real; no repiten aquí la interfaz de navegador ni el login con contraseña.
