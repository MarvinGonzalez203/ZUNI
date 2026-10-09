# Big Five: estudiante y psicólogo

Implementación del 9 de octubre de 2026 en `feature/student-psychologist-link`.

## Alcance y fuentes

Se implementó el IPIP-50 de marcadores de Goldberg: 50 afirmaciones, 10 por dimensión. No se usaron NEO-PI-R, IPIP-NEO-120 ni Mini-IPIP ni se mezclaron sus claves. La elección inicial de versión corresponde al proyecto; cambiarla requiere revisión del profesional, un catálogo/versionado nuevo y pruebas. El estudiante no elige versiones arbitrarias.

Fuentes primarias consultadas:

- [IPIP: permiso de uso y dominio público](https://ipip.ori.org/).
- [Cuestionario original con 50 ítems y claves](https://ipip.ori.org/New_IPIP-50-item-scale.htm).
- [Claves de las cinco escalas](https://ipip.ori.org/newBigFive5broadKey.htm).
- [Traducciones y adaptaciones](https://ipip.ori.org/newItemTranslations.htm).
- [Colegio de Psicólogos de Guatemala: confidencialidad y consentimiento](https://www.colegiodepsicologos.org.gt/wp-content/uploads/2024/05/Revista-No-6_compressed.pdf).

IPIP permite copiar, traducir y adaptar sus ítems. Esto no equivale a validar cualquier traducción, certificar cumplimiento legal en Guatemala o autorizar el manejo de datos sin consentimiento. La transcripción adjunta contiene afirmaciones legales sin fuentes identificables y su aviso de responsabilidad no "blinda" legalmente una plataforma. No se incorporó una renuncia a acciones o derechos. La institución debe revisar su política, consentimiento y práctica profesional antes de uso real.

`Ipip50.cs` conserva cada original inglés, su adaptación española, número y clave. La traducción es una adaptación propia ZUNI v1, NO una versión validada en Guatemala. El catálogo no se recorta: sus ítems relacionados son parte de la escala; eliminarlos por parecer redundantes cambia el instrumento. Las traducciones a idiomas mayas requieren un proceso de traducción, revisión cultural y validación separado.

## Puntuación

Respuesta de 1 a 5 según cuánto describe a la persona. Ítem inverso: `6 - respuesta`. Se requieren las 50 respuestas; no hay imputación ni cálculo con faltantes. La media de los diez valores corregidos da cada dimensión: O, C, E, A y N. Se guardan las cinco medias, sin total global, umbrales, percentiles, diagnóstico ni decisiones académicas automatizadas.

El factor IV oficial mide **estabilidad emocional**, no Neuroticismo. Para ofrecer OCEAN, se invierte la orientación de sus diez claves: estrés, irritación y desánimo son directos para N; calma y desánimo infrecuente son inversos. El ejemplo adjunto asignaba inversamente "Me estreso con facilidad" mientras lo llamaba N; eso habría mostrado la dirección contraria.

Las barras representan `(media - 1) / 4` en el rango de la escala, NO porcentaje de personalidad ni percentil poblacional. Los extremos son descripciones orientativas del continuo; deben contextualizarse por el profesional. Una puntuación alta o baja no es buena ni mala. No se infiere capacidad intelectual del factor intelecto/imaginación.

## Flujo y permisos

- Estudiante → Evaluaciones → Conocer el cuestionario (`/Estudiante/BigFive`). Introducción, aviso no diagnóstico, referencia por categoría y motivo breve. No se piden relatos traumáticos, sexo, orientación ni detalles adicionales para calcular personalidad.
- Consentimiento explícito, declaración de mayoría de edad, versión/texto completo/fecha conservados. El flujo de menores no está habilitado ni se sustituye el consentimiento de un responsable con una casilla.
- Se verifica el perfil activo y el rol vigente. Se reutiliza el servicio de asignación: exactamente un psicólogo activo permite crear el vínculo; cero o varios dejan pendiente. No se elige arbitrariamente un profesional. Un vínculo existente se conserva.
- El profesional disponible se informa antes de comenzar y se verifica nuevamente al aceptar.
- Catálogo compartido versionado y una participación por estudiante/versión. Orden mezclado estable por asignación, intercalado, con dos ítems de cada dimensión en cada bloque de diez. Cinco bloques, progreso, guardado manual, recuperación de avance, advertencia al salir y confirmación final. Animación CSS breve y respeto a `prefers-reduced-motion`.
- El guardado/finalización reutiliza transacciones, bloqueo y revisión del módulo existente. El cálculo y comprobante se guardan junto con la finalización. No se vuelve a abrir mediante Administración.
- Psicólogo → Resultados (`/Psicologo/Resultados`): las cinco medias, fecha, identidad del estudiante y motivo/referencia. Proyección de datos limitada: no preguntas, respuestas individuales ni entidades EF. Se muestran hasta 100 perfiles recientes.
- Cada lectura exige consentimiento no retirado, estudiante y profesional activos con roles actuales, vínculo vigente y coincidencia con el profesional registrado en la participación. No se transfiere el acceso a un nuevo psicólogo automáticamente. Se registra cada lectura en `AccesosBigFive`, sin copiar respuestas o motivo al registro de acceso.
- Administrador no lista ni modifica Big Five desde Evaluaciones. Director, docentes, otro estudiante y otro psicólogo no tienen acceso por sus rutas. La ruta genérica del cuestionario no entrega el Big Five; las escrituras también verifican consentimiento y vínculo en el servicio.
- Retirar consentimiento suspende cuestionario y lecturas del profesional; no elimina automáticamente datos/auditoría. Una nueva participación después del retiro requiere un flujo institucional futuro. No se finge que se borraron los datos.
- Los operadores con acceso directo a PostgreSQL y los respaldos requieren permisos, cifrado, controles y acuerdos institucionales. Los permisos MVC no pueden impedir que un superusuario de la base lea sus tablas.

No se repite automáticamente el test para cada cita. `PuedeSolicitarCita` verifica finalización, consentimiento y vínculo vigentes para el futuro flujo de reservas. Citas muestra ese estado, pero sigue sin calendario ni reservas reales. Su futura acción POST deberá repetir la verificación en servidor dentro de la operación de reserva; ocultar un botón no es suficiente. No se definió una caducidad arbitraria del perfil. La institución y el psicólogo deberán definir reaplicación y alternativas de atención para quienes no participen; el Big Five no detecta urgencias.

## Configuración y activación

En `appsettings.json`:

```json
"BigFive": {
  "Habilitado": false,
  "PruebaLocal": false,
  "RevisionProfesionalAprobada": false,
  "Institucion": "",
  "ContactoPrivacidad": "",
  "Conservacion": "",
  "UbicacionDatos": ""
}
```

Development activa `PruebaLocal` únicamente con PostgreSQL en localhost. Las rutas de cuestionario y resultados de prueba aceptan solicitudes de loopback. El aviso exige datos ficticios. Las participaciones se marcan `PruebaLocal` y se excluyen del flujo institucional; no se pueden reutilizar como aplicaciones reales al cambiar configuración.

Producción permanece cerrada hasta completar responsable, contacto para privacidad/quejas, conservación y ubicación de almacenamiento, aprobar profesionalmente la adaptación y habilitarla explícitamente. No marcar una revisión como aprobada sin realizarla. Estos textos son configurables; no implementan por sí mismos borrado por plazo, cifrado de respaldos, atención de quejas o procedimientos de acceso/eliminación. Deben definirse y ejecutarse institucionalmente.

## Base de datos y otra computadora

Migración nueva: `20261009223523_AddBigFiveConsentimiento`. Solo crea `ParticipacionesBigFive` y `AccesosBigFive`, sus restricciones e índices. Conserva intactas las migraciones ya publicadas/aplicadas. No siembra respuestas ni crea usuarios. El catálogo de 50 preguntas se crea transaccionalmente cuando un estudiante acepta; los registros originales nunca se sobrescriben al repetir la solicitud.

En otra computadora: SDK .NET 8, PostgreSQL y conexión privada `ConnectionStrings:DefaultConnection` en User Secrets o entorno. Revisar el historial, compilar y aplicar migraciones faltantes con `dotnet-ef` 8 compatible:

```powershell
dotnet ef database update --project Zuni/Zuni.csproj
```

No generar otra migración para duplicar esta ni aplicar SQL manual equivalente además del comando. No importar las bases temporales de pruebas. Las cuentas/contraseñas y datos locales no viajan con Git. Reiniciar el proceso de la aplicación para cargar los nuevos controladores, vistas y servicios.

## Verificación

```powershell
dotnet run --project tests/BigFive.Checks --configuration BigFiveCheck -p:NuGetAudit=false
dotnet run --project tests/CombinedModel.Checks --configuration BigFiveCheck -p:NuGetAudit=false
dotnet run --project tests/StudentProfile.Checks --configuration BigFiveCheck -p:NuGetAudit=false
node --test tests/bigfive-ui.test.cjs tests/evaluaciones-ui.test.cjs tests/student-profile.test.cjs tests/disponibilidad.test.cjs
```

`BigFive.Checks` crea una base con nombre aleatorio `ZuniBigFiveTest_*`, aplica primero Evaluaciones y después el resto, crea cuentas ficticias, inicia una instancia de la aplicación en localhost:7129, prueba login real, antifalsificación, persistencia, claves de puntuación, aislamiento y retiro. El proceso y la base temporal se eliminan al terminar, incluso ante fallos. Requiere permiso local para crear bases; nunca migra la base de trabajo. No ejecutarlo contra un servidor compartido. Las pruebas generan instantáneas HTML ficticias en `.visual-check/bigfive`, excluidas de Git.

Se revisaron esas vistas en navegador en escritorio y a 390 × 844: bloques de diez, avance al inicio del bloque y resumen de cinco dimensiones sin desbordamiento visible. Esa revisión de vistas estáticas complementa las pruebas HTTP; no sustituye una prueba de todo el flujo de guardado desde navegador.

## Funcionalidades posteriores

Calendario persistente y reserva sin colisiones; disponibilidad presencial/virtual; perfil profesional con foto/biografía/enfoque; token de asistencia (un token no prueba por sí solo identidad); enlace privado de videollamada y política de acceso; consentimiento terapéutico y flujo de menores; política de conservación y atención de solicitudes; reaplicación indicada por el psicólogo; recorrido general del resto de la plataforma. Ninguna de estas funciones se presenta como terminada en esta entrega. No se implementa detección de suicidio por palabras, historia clínica ni diagnóstico automático.

## Resultado de esta entrega

- Compilación final en configuración BigFiveCheck: 0 errores y 0 advertencias. La instancia Debug que estaba en ejecución bloqueaba sus binarios; se compiló en otra configuración sin detenerla.
- 85 comprobaciones Big Five/PostgreSQL/HTTP, 30 de modelo/migraciones, 35 de perfil/CSV y 19 pruebas JavaScript aprobadas.
- Las pruebas temporales se eliminaron. No se cargaron sus cuentas ni respuestas en zuni.
- La migración nueva se aplicó en zuni local: 0 migraciones pendientes, 0 participaciones Big Five creadas. Se verificó un único psicólogo activo: psicologo.prueba@miumg.edu.gt. El estudiante deberá aceptar por sí mismo y usar datos ficticios.
- No se hizo push ni merge a main. El proceso que ya estaba abierto debe reiniciarse para cargar esta versión.
