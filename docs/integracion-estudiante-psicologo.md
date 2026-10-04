# Integración Estudiante → Psicólogo

## Revisión y alcance

Ya existen `ApplicationUser` (nombre `FullName`, correo `Email`, cuenta activa),
`PerfilEstudiante`, `ApplicationDbContext` con Npgsql, `EstudianteController`,
`PerfilController` y las vistas de consulta y captura del perfil.
`AddPerfilEstudiante` crea índices únicos para `Carne` y `UsuarioId`;
`AjustarPerfilEstudiante` agrega ciclo y relación del contacto de emergencia.
El snapshot conserva esas restricciones. No se necesita migración para editar.

El formulario antes impedía editar perfiles completos y solo solicitaba carné y
teléfono si estaban vacíos. Ahora reutiliza la misma ruta para completar y editar,
precarga los datos y conserva nombre/correo en `ApplicationUser`.
El identificador se obtiene de `NameIdentifier`; ningún campo del formulario
selecciona al usuario. Se verifica cuenta activa y rol Estudiante, con
antifalsificación y sin caché del formulario. La carrera, carné (10 dígitos ASCII)
y teléfono (8 dígitos) son obligatorios. Se conservan semestre 1–10 y ciclo 1–2
como opcionales, conforme al comportamiento existente. El carné se almacena sin
guiones y su índice único protege también contra escrituras simultáneas.

No se encontraron entidades, migraciones ni servicios de asignación/referencia.
`PsicologoController` devuelve vistas, sin búsqueda persistente ni autorización
por vínculo. El estado visual se describe en `panel-psicologo.md`.
No se modificaron autenticación central, vistas del Psicólogo ni otros paneles.
No se crearon cuentas, datos de ejemplo ni persistencia en el navegador.

## Propuesta compartida: pendiente de autorización de Marvin, NO implementada

La regla actual es un único usuario activo con rol Psicologo. Cada estudiante
se vinculará automáticamente a ese profesional, sin selector ni asignación manual.
No existe una relación reutilizable en los modelos, controladores o migraciones
revisados. Se propone `AsignacionesEstudiantePsicologo`:

| Campo | Propuesta |
| --- | --- |
| Id | UUID, clave primaria |
| PerfilEstudianteId | UUID, FK a PerfilesEstudiante.Id |
| PsicologoUsuarioId | text, FK a AspNetUsers.Id |
| Estado | Vigente o Finalizada, restricción CHECK |
| FechaAsignacionUtc | timestamp with time zone, obligatoria |
| FechaFinalizacionUtc | timestamp with time zone, nula mientras esté vigente |

FK con eliminación restringida; índice único por PerfilEstudianteId para que la
operación automática ocurra una sola vez, incluso en reintentos concurrentes.
Índice por PsicologoUsuarioId/Estado/PerfilEstudianteId para consulta. CHECK de
consistencia estado/fecha y fecha final >= fecha inicial. En esta etapa no se
recrea ni reasigna automáticamente un vínculo finalizado. Un futuro historial de
reasignaciones requeriría revisar este índice y la política con Marvin.

### Momento y algoritmo propuestos

Ejecutar un servicio idempotente después de guardar correctamente el perfil del
estudiante en `/Perfil/Completar`, sin modificar el registro o autenticación
central. En esta etapa, estudiantes nuevos quedan pendientes hasta completar el
perfil; un proceso de conciliación cubre también los perfiles ya registrados,
incluso incompletos, con usuario activo y rol Estudiante. El registro normal ya
crea PerfilEstudiante. Si se detecta una cuenta sin perfil, reportarla y esperar
su creación mediante el flujo existente; no inventar carné ni datos personales.

1. Si ya existe cualquier vínculo para ese perfil, no insertar ni reemplazarlo.
2. Consultar usuarios activos con rol actual Psicologo en Identity, contando
   usuarios distintos (no filas de roles). No cambiar roles de ninguna cuenta.
3. Si existe exactamente uno, crear vínculo vigente y fecha UTC del servidor.
4. Con cero o varios, no crear vínculo: dejar pendiente y reportar la causa
   mediante un resultado del servicio y registro operativo sin datos privados.
   Nunca usar First/FirstOrDefault para elegir arbitrariamente un profesional.
5. Guardar de forma transaccional, con aislamiento serializable y reintentos
   acotados por conflictos; la restricción única resuelve solicitudes simultáneas.
   Ante conflicto de unicidad, releer el vínculo existente sin reemplazarlo.
   Validar de nuevo elegibilidad dentro de la transacción. Cambios concurrentes
   de habilitación de psicólogos requerirán coordinación con Administración.
6. La operación debe devolver Creado, YaExistente, PendienteSinPsicologo,
   PendienteMultiplesPsicologos o ErrorReintentable. El perfil guardado permanece
   guardado aunque la vinculación esté pendiente o falle; no simular éxito del vínculo.

Para estudiantes existentes: comando administrativo de conciliación por lotes,
primero en modo diagnóstico (conteos), luego ejecución autorizada e idempotente
sobre perfiles sin vínculo. Se reutiliza el mismo servicio. Reejecutarlo cuando
se resuelva la ausencia o multiplicidad de psicólogos permite atender pendientes
sin exigir un nuevo inicio de sesión ni editar nuevamente el perfil. Un futuro
trabajo periódico podría automatizar estos reintentos, sujeto a aprobación.

Pendiente significa ausencia de relación, no una fila con psicólogo nulo. Tener
perfil completo nunca basta para conceder acceso: las consultas exigen vínculo.
Desactivar al profesional no transfiere estudiantes; el vínculo se conserva y el
acceso se deniega por cuenta inactiva. No implementar cierre, transferencia ni
selección manual en esta etapa.

En el futuro un nuevo profesional se registrará normalmente con rol Estudiante y
Director habilitará Psicologo en ajustes. Esto es contexto futuro, no código de
esta entrega. La multiplicidad resultante requiere una nueva regla institucional.

### Archivos y cambios que requieren autorización de Marvin

- Nuevo `Zuni/Models/AsignacionEstudiantePsicologo.cs`.
- `Zuni/Data/ApplicationDbContext.cs`: DbSet, claves, FK, índices y restricciones.
- Nueva migración y `ApplicationDbContextModelSnapshot.cs`; aplicación coordinada
  a PostgreSQL, sin asignar estudiantes dentro de la migración de esquema.
- Nuevo servicio de vinculación y registro de dependencias en `Zuni/Program.cs`
  (solo DI; sin tocar autenticación).
- Invocación del servicio desde `Zuni/Controllers/PerfilController.cs` después
  del guardado y comando de conciliación de perfiles existentes.
- Servicio compartido de búsqueda/detalle y DTO mínimo para el compañero de
  Psicólogo, con autorización por vínculo en cada operación.
- Pruebas de concurrencia, cardinalidad 0/1/varios, conciliación y acceso cruzado.

Primero compartir este commit de perfil y revisar la propuesta entre ambas ramas.
Después solicitar autorización a Marvin antes de implementar cualquiera de estos
cambios compartidos, crear/aplicar migraciones o integrar hacia main. No se han
modificado Administración, Director, autenticación ni el controlador/vistas de
Psicólogo. No se ha implementado la asignación automática.

## Contrato propuesto para el compañero (todavía NO hay endpoints)

`GET /Psicologo/Estudiantes/Buscar?q=texto&page=1&pageSize=20`

- Autenticación y rol Psicologo; comprobar también usuario activo y rol actual.
- Psicólogo obtenido únicamente de la sesión (`NameIdentifier`). Rechazar o
  ignorar cualquier identificador de profesional enviado por el cliente.
- `q`: trim, 2–150 caracteres; inválido devuelve 400. Buscar nombre sin distinguir
  mayúsculas y carné por fragmento, quitando guiones de la entrada de carné.
  Tratar `%`, `_` y barra inversa como caracteres literales, no comodines.
- `page` entre 1 y 1000; `pageSize` entre 1 y 50 (predeterminado 20).
- Orden estable por nombre y perfil Id; pedir pageSize + 1 para `hasMore`.
- Respuesta 200: `{ items: [{ id, nombre, carne, carrera }], page, pageSize, hasMore }`.
  `id` es PerfilEstudiante.Id, no el Id de Identity. Sin coincidencias: items vacío.
- Proyectar únicamente esos cuatro campos desde la consulta. No serializar
  entidades EF, correo, contraseñas, teléfonos, contactos, respuestas ni notas.
- No autenticado: 401; rol/cuenta sin permiso: 403. La implementación futura
  debe resolver respuestas JSON dentro de estas acciones, sin cambiar cookies
  ni autenticación central. Cache-Control: no-store.

Consulta SQL parametrizada de referencia, ejecutable solo tras aprobar y crear
la relación propuesta (nombres de tabla/columnas sujetos al acuerdo):

```sql
SELECT p."Id" AS id, u."FullName" AS nombre,
       p."Carne" AS carne, p."Carrera" AS carrera
FROM "PerfilesEstudiante" p
JOIN "AspNetUsers" u ON u."Id" = p."UsuarioId"
WHERE p."Activo" AND u."IsActive"
  AND EXISTS (
    SELECT 1 FROM "AspNetUserRoles" ur
    JOIN "AspNetRoles" r ON r."Id" = ur."RoleId"
    WHERE ur."UserId" = u."Id" AND r."NormalizedName" = 'ESTUDIANTE'
  )
  AND EXISTS (
    SELECT 1 FROM "AsignacionesEstudiantePsicologo" a
    WHERE a."PerfilEstudianteId" = p."Id"
      AND a."PsicologoUsuarioId" = @psicologoSesionId
      AND a."Estado" = 'Vigente'
      AND a."FechaFinalizacionUtc" IS NULL
      AND a."FechaAsignacionUtc" <= CURRENT_TIMESTAMP
  )
  AND (u."FullName" ILIKE @patronNombre ESCAPE E'\\'
       OR p."Carne" ILIKE @patronCarne ESCAPE E'\\')
ORDER BY u."FullName", p."Id"
LIMIT @limiteMasUno OFFSET @offset;
```

Preparar patrones escapando primero barra inversa, luego `%` y `_`, y envolver
en `%`. Usar parámetros Npgsql/EF, nunca interpolación SQL. `offset` y límite se
calculan en servidor después de validar página y tamaño. Usar EXISTS evita
duplicados por historial de vínculos. Si se acuerdan requisitos de consentimiento
o aceptación, añadirlos a este filtro antes de habilitar el servicio.

`GET /Psicologo/Estudiantes/{id}` debe reutilizar los mismos predicados de acceso
con `p.Id = @id`, sin filtro de búsqueda ni paginación. Devolver 404 tanto si no
existe como si no hay vínculo vigente. No basta haber aparecido antes en la
búsqueda: verificar de nuevo en cada solicitud, incluso tras finalizar el vínculo.
El detalle de este contrato conserva los cuatro campos; información clínica
requiere un contrato y permisos separados.

## Pruebas locales reproducibles

Usar PostgreSQL local configurado fuera del repositorio, con las migraciones
existentes aplicadas por el responsable. No aplicar migraciones a una base
compartida como parte de esta entrega.

1. Con dos cuentas locales existentes, Estudiante A y Psicólogo P, iniciar sesión
   como A, abrir /Estudiante/MiPerfil → Editar mi perfil. Guardar carné, teléfono y
   carrera válidos. Ver confirmación, cerrar sesión, entrar y comprobar persistencia.
2. Editar de nuevo un perfil completo: comprobar precarga y actualización de
   teléfono/carrera/carné. Probar campos vacíos, carné con letras o dígitos Unicode,
   teléfono distinto de 8 dígitos, carrera >150, semestre 11 y ciclo 3: deben fallar.
3. Con A, agregar UsuarioId o Id ajeno al POST: solo puede cambiar A. Quitar el
   token antifalsificación: 400. Una sesión de P no puede editar el perfil de A.
4. Para probar duplicados hace falta otro estudiante B existente: intentar su
   carné desde A y comprobar error y que los datos guardados no cambien. El índice
   único debe rechazar también una carrera entre dos solicitudes simultáneas.
5. Tras aprobar e implementar relación y endpoints, buscar A como P sin vínculo:
   lista vacía y detalle 404. El servicio automático crea el vínculo con el único psicólogo activo; buscar
   por nombre/carné debe devolver A. Finalizar vínculo y repetir: vacío/404.
6. Con otro psicólogo Q existente, buscar/abrir A sin vínculo: vacío/404. Verificar
   por inspección del JSON que solo haya id, nombre, carne y carrera.

Dos cuentas permiten probar el flujo básico. Los casos de carné duplicado y de
otro psicólogo requieren además B y Q (o cuentas locales equivalentes ya creadas).
Probar conciliación repetida sin duplicados y escenarios con cero/varios psicólogos activos: quedan pendientes. No permitir elegir profesional desde el formulario.

## Verificación y pendientes (2026-10-04)

Compilación de una copia aislada de HEAD más los cambios exactos que se publican:
`dotnet build .verification-build/profile-review/source/Zuni/Zuni.csproj -p:NuGetAudit=false`.
Resultado: correcta, 0 errores y 0 advertencias. No depende de mejoras visuales
locales pendientes. Git avisa de normalización LF → CRLF; no es error de compilación.

Pruebas reales contra PostgreSQL local con la cuenta Estudiante existente,
ejecutando el controlador y EF dentro de una transacción revertida al terminar:

| Caso | Resultado |
| --- | --- |
| Guardar perfil y releer desde PostgreSQL sin seguimiento EF | Correcto |
| Abrir perfil completo para edición y precargar datos | Correcto |
| Guardar segunda edición y releer desde PostgreSQL | Correcto |
| Rechazar carné inválido | Correcto |
| Revertir datos de prueba y conservar cuenta/perfil originales | Correcto |
| Cerrar sesión y volver a entrar mediante HTTP | Pendiente: no se proporcionaron credenciales de ZUNI |
| Antifalsificación y acceso ajeno mediante HTTP | Revisión estática; prueba HTTP pendiente |
| Carné duplicado entre dos estudiantes | Índice único revisado; prueba con segunda cuenta pendiente |
| Búsqueda, vinculación y acceso entre psicólogos | Pendiente de autorización e implementación |

Las pruebas directas del controlador usan la identidad de la cuenta existente
para ejercitar la persistencia, pero no atraviesan el middleware de autenticación,
validación MVC ni antifalsificación. No equivalen a una prueba de navegador o de
persistencia después de cerrar sesión. No se crearon usuarios ni se cambiaron
contraseñas. No se aplicaron migraciones ni se implementó el vínculo.

Archivos publicados: `Zuni/Controllers/PerfilController.cs`,
`Zuni/Views/Perfil/Completar.cshtml`, `Zuni/Views/Estudiante/MiPerfil.cshtml` y
este documento. En MiPerfil se publica únicamente el enlace de edición sobre la
vista existente en la rama; sus mejoras visuales locales se conservan sin incluir,
pues dependen de archivos fuera del alcance autorizado. Los ejecutables y scripts
de comprobación quedan locales y no forman parte del commit. No se incluyen
contraseñas, cadenas de conexión privadas ni credenciales de prueba.
