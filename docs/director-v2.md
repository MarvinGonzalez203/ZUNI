# Director V2

Rama local `feature/director-V2`, creada desde `origin/main` (`19bab69`). Se trasladó de
`feature/director-dashboard` el nombre completo del Director desde `ApplicationUser`.
El panel y su navegación ya estaban en main. No se trasladaron versiones antiguas de
seguridad, administración o agenda, ni el cambio de espacios del layout general.

## Alcance de la asignación de Marlon

- Panel Director con navegación propia y enlace al catálogo.
- Crear, visualizar y editar instrumentos inactivos sin asignaciones.
- Crear, editar y quitar dimensiones, preguntas y opciones desde el editor del instrumento.
- Guardar fuente, propósito, instrucciones e idioma. Se permiten borradores sin preguntas;
  activar requiere al menos una pregunta y dos opciones por pregunta.
- Activar y desactivar. La desactivación impide nuevas asignaciones y conserva las existentes.
- Asignar a perfiles activos con usuario activo y rol Estudiante. La selección excluye
  estudiantes ya asignados; un índice único protege también frente a duplicados.
- Conservar contenido de instrumentos asignados. Una modificación posterior requiere crear
  otro instrumento; las revisiones controlan cambios simultáneos.

Ruta: `/PruebasDirector`. Se usa el rol Director, la cookie y los usuarios existentes.
Todos los POST exigen antiforgery. No hay otra autenticación, nuevas tablas Identity ni ML.NET.
Las cinco nuevas tablas pertenecen al catálogo y las asignaciones, no a autenticación.

## Puesta en marcha

La migración `AddPruebasDirector` está incluida, junto con su snapshot. Antes de abrir el
catálogo, aplicar en la base del entorno elegido desde la raíz del repositorio:

```powershell
dotnet ef database update --project Zuni -- --environment Development --contentRoot "$PWD\Zuni"
```

La implementación no aplica migraciones automáticamente ni cambia conexiones. Durante esta
tarea no se ejecutó `database update` ni se modificó PostgreSQL.

## Límite del flujo preparado

La asignación queda registrada como «Asignada». No implica que el estudiante haya iniciado
o completado la prueba. La presentación al estudiante, consentimiento del instrumento,
respuestas y seguimiento de realización corresponden a la futura integración con ese módulo.
El catálogo es independiente de Big Five; no modifica sus reglas, datos ni rutas actuales.
Los valores de opciones son configuración, no algoritmos de evaluación ni diagnósticos.

El contexto aportado del psicólogo se tomó como referencia de diseño: el Director no consulta
expedientes, motivos de consulta, contactos de emergencia, respuestas o resultados individuales.
Solo se muestran nombre/carné para preparar asignaciones. Registrar una fuente o activar un
instrumento no certifica su validación clínica. El equipo profesional debe revisar pertinencia,
consentimiento, licencia e idioma antes de aplicarlo.

## Verificación

```powershell
dotnet build Zuni/Zuni.csproj --no-restore
dotnet run --project tests/Seguridad.Offline --no-restore
dotnet ef migrations has-pending-model-changes --project Zuni --no-build -- --environment Development --contentRoot "$PWD\Zuni"
```

`PruebasChecks` usa el host HTTP de seguridad y SQLite en memoria. Cubre páginas Razor,
permisos, antiforgery, creación, reemplazo de estructura, conflictos de edición, activación,
desactivación, asignación, duplicados y bloqueo de cambios tras asignar. No abre PostgreSQL.
Las pruebas existentes de seguridad se conservan. La concurrencia y la migración en PostgreSQL
requieren validación adicional en una base descartable; SQLite no reproduce todos sus detalles.
