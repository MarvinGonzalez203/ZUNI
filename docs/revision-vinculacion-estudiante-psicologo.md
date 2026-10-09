# Vinculación estudiante–psicólogo: base para revisión

## Base Git

Rama: `feature/student-psychologist-link`.
Base comprobada antes de implementar: `8c66192e317074892a51e53ff64dbbe0e4c574ad`, idéntica a `main`, `origin/main` y al main consultado en GitHub. La rama comenzó limpia y sin commits pendientes de subir. El trabajo anterior permanece en la rama del psicólogo, en `e5fef97c6baca3e01dfcce5b065f158cab396d50`.

## Archivos

Creados:

- `Zuni/Models/AsignacionEstudiantePsicologo.cs`.
- `Zuni/Services/AsignacionPsicologoService.cs` (incluye el enum de resultados).
- `Zuni/Migrations/20261009060000_AddAsignacionesEstudiantePsicologo.cs`.
- `Zuni/Migrations/20261009060000_AddAsignacionesEstudiantePsicologo.Designer.cs`.
- Este documento de revisión.

Modificados:

- `Zuni/Data/ApplicationDbContext.cs`: DbSet y configuración de la nueva entidad.
- `Zuni/Program.cs`: importación del namespace y registro scoped del servicio.
- `Zuni/Migrations/ApplicationDbContextModelSnapshot.cs`: únicamente metadatos de la nueva entidad y sus relaciones.

## Entidad y configuración

`AsignacionEstudiantePsicologo` contiene `Guid Id`, `Guid PerfilEstudianteId`, `string PsicologoUsuarioId` obligatorio, `DateTime FechaAsignacionUtc` y `DateTime? FechaFinalizacionUtc`. Sus navegaciones son `PerfilEstudiante` y `PsicologoUsuario` (`ApplicationUser`). No se modificaron las entidades relacionadas para agregar colecciones inversas.

DbSet y tabla: `AsignacionesEstudiantePsicologo`. PK: `Id`.

Las FK apuntan a `PerfilesEstudiante.Id` y `AspNetUsers.Id`, ambas con `DeleteBehavior.Restrict`. Las fechas usan `timestamp with time zone`; el servicio genera la fecha de asignación con `DateTime.UtcNow`.

Índice compuesto: `PsicologoUsuarioId, FechaFinalizacionUtc`.

Índice único parcial: `UX_AsignacionesEstudiantePsicologo_PerfilEstudiante_Vigente`, sobre `PerfilEstudianteId`, con filtro PostgreSQL `"FechaFinalizacionUtc" IS NULL`. Impide dos asignaciones vigentes y permite conservar varias finalizadas.

CHECK: `CK_AsignacionesEstudiantePsicologo_Fechas`, con expresión `"FechaFinalizacionUtc" IS NULL OR "FechaFinalizacionUtc" >= "FechaAsignacionUtc"`.

## Servicio

Entrada: `AsignarAsync(Guid perfilEstudianteId, CancellationToken ct)`.

Verifica perfil activo, usuario asociado activo y rol actual `Estudiante` mediante las tablas Identity. Si no cumple, devuelve `NoElegible`. Una asignación vigente devuelve `YaAsignado` y se conserva; si solo hay historial finalizado, devuelve `RequiereRevision` sin crear una nueva.

Para un estudiante elegible sin historial, obtiene hasta dos identificadores distintos de usuarios activos con rol actual `Psicologo`. Cero devuelve `PendienteSinPsicologo`; dos o más devuelven `PendienteMultiplesPsicologos`. Solo uno permite crear la asignación y devolver `Asignado`. No se crean filas pendientes ni identificadores ficticios.

Resultados tipados: `Asignado`, `YaAsignado`, `PendienteSinPsicologo`, `PendienteMultiplesPsicologos`, `RequiereRevision`, `NoElegible`, `ErrorReintentable`.

La operación utiliza una transacción serializable limitada a estas consultas y la inserción. Requiere un contexto sin cambios pendientes y sin transacción activa para no guardar trabajo ajeno del llamador. La futura integración debe respetar esta precondición.

El índice parcial protege finalmente contra duplicados. Solo la violación de unicidad con el nombre exacto de ese índice se trata como conflicto de asignación: se revierte la transacción, se desprende la entidad intentada y se vuelve a consultar fuera de la transacción abortada. Una asignación vigente devuelve `YaAsignado`; su ausencia devuelve `ErrorReintentable`. Los conflictos PostgreSQL de serialización o deadlock siguen la misma recuperación. No hay reintentos automáticos ilimitados ni conversión general de errores a `YaAsignado`; otros errores se propagan.

No se registran datos personales ni se reemplazan o finalizan asignaciones existentes. El servicio está registrado con `AddScoped<AsignacionPsicologoService>()` y ningún controlador lo invoca todavía.

## Migración y snapshot

Nombre exacto: `20261009060000_AddAsignacionesEstudiantePsicologo`.

`Up()` únicamente crea la tabla nueva, su PK, ambas FK restrictivas, el CHECK y los dos índices. No altera tablas o columnas existentes ni contiene inserciones o actualizaciones de datos. `Down()` únicamente elimina la tabla nueva y, con ella, sus restricciones e índices.

El snapshot añade exclusivamente las cinco propiedades de la nueva entidad, PK, tabla, tipos de columnas, índices, CHECK y las dos relaciones/navegaciones. No se detectaron cambios ajenos al modelo autorizado. Npgsql abrevia automáticamente algunos nombres de FK e índice compuesto para respetar el límite de identificadores PostgreSQL.

Para generar la migración se utilizó una fábrica temporal de diseño con configuración ficticia, sin conexión a PostgreSQL y sin arrancar Program ni su inicialización de roles. La fábrica se eliminó al terminar y se volvió a compilar; no forma parte de los archivos entregados.

## Validación y límites

Compilación final: `dotnet build Zuni/Zuni.csproj --no-restore`, correcta con **0 errores y 0 advertencias**. `git diff --check` correcto. Migración y diff del snapshot inspeccionados.

No se ejecutaron pruebas de integración o concurrencia contra PostgreSQL porque esta fase prohíbe modificar la base. Compilar y revisar el código no sustituye esas pruebas, que quedan para la fase autorizada posterior.

Confirmaciones:

- La rama parte del main actualizado comprobado antes de implementar.
- El módulo Perfil antiguo no regresó.
- MiCuenta y todos los controladores funcionales permanecen sin cambios.
- ApplicationUser, PerfilEstudiante y CarneHelper permanecen sin cambios.
- PostgreSQL no fue modificado; no se ejecutó Update-Database ni SQL manual.
- No se insertaron asignaciones ni se ejecutó conciliación.
- No se conectó el servicio al registro, altas administrativas, búsquedas o cambios de rol.
- No se hizo commit, push ni merge.

Siguiente paso: revisión de Marvin del modelo, servicio y migración. Aplicar la migración, probar PostgreSQL e integrar los puntos de entrada pertenecen a una fase posterior.
