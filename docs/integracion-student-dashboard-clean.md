# Integración de feature/student-dashboard-clean

9 de octubre de 2026. Merge de `origin/feature/student-dashboard-clean` sobre la rama local del mismo nombre. No se integró a main ni se utilizó rebase o push forzado.

## Respaldo e historiales

El estado original `c91b0212e2378c7377342e9b67ef72173f14cf01` queda respaldado en la rama local `codex/backup-student-dashboard-clean-c91b021`.

Antes del merge, los commits exclusivos locales eran `067e9e0`, `2c7030c` y `c91b021`. Los exclusivos remotos eran `eee7211`, `b46aa29`, `58328eb`, `fc8c334`, `9504c3a` y `8c66192`. Ambos historiales se conservan como padres del merge. No se encontraron archivos AGENTS.md ni README/CONTRIBUTING con instrucciones dentro del repositorio.

## Conflictos y decisiones

Se encontraron 17 archivos con conflictos:

- `.gitignore`: se combinó la exclusión de artefactos, bin/obj, cachés y archivos de IDE; los temporales privados siguen fuera de Git.
- `CuentaController.cs`, `AccountViewModels.cs`, `AgregarUsuarioViewModel.cs`, `AgregarUsuario.cshtml` y `Registro.cshtml`: se adoptó el CarneHelper remoto (8–12 dígitos, bloques 4-2-2..6). Se conservó el selector local Administrador/Estudiante y las rutas administrativas locales. Cerrar sesión lleva al login, como implementó el remoto.
- `PerfilController.cs`, `CompletarPerfilEstudianteViewModel.cs`, `MiPerfil.cshtml` y `Perfil/Completar.cshtml`: se adoptó MiCuenta como único editor y se retiraron el formulario/modelo anteriores. El controlador antiguo solo redirige GET /Perfil/Completar a /MiCuenta/Editar; GET /Estudiante/MiPerfil redirige a /MiCuenta. No queda una segunda vía POST que permita evadir las restricciones de MiCuenta.
- `MiPerfilViewModel.cs`, `_ProgresoPerfil.cshtml` e `Index.cshtml`: se conservaron la estructura y enlaces a MiCuenta del remoto y el progreso del perfil. Se conservaron las acciones de acceso a Evaluaciones y Resultados disponibles.
- `Citas.cshtml` y `estudiante.css`: se conservó la presentación remota, incluidos ajustes móviles y foco visible. Citas permanece como Próximamente.
- `Evaluaciones.cshtml` y `Resultados.cshtml`: se conservaron las vistas locales respaldadas por PostgreSQL, estados, comprobantes y publicación autorizada, evitando reemplazarlas por marcadores de próxima disponibilidad.

Los controladores de Estudiante/Administrador, navegación, layout y Program se fusionaron automáticamente y se revisaron: MiCuenta permanece accesible, el layout conserva ambos paneles y el servicio de Evaluaciones coexiste con el middleware remoto de contenido privado sin caché. La edición administrativa y el importador CSV se adaptaron al formato de carné de CarneHelper para no rechazar datos válidos del modelo remoto.

## Modelo y migración aplicada

ApplicationDbContext, el snapshot y las migraciones existentes no se modificaron respecto de `c91b021`. El registro ConfigurarEvaluaciones permanece junto a Identity, perfiles, auditoría y tokens.

La prueba offline compara el modelo de diseño EF con el snapshot y el TargetModel de `20261009043711_AddEvaluaciones`: no hay diferencias. La migración aparece una sola vez y Up contiene exclusivamente creación de las cinco tablas de Evaluaciones y sus índices. No elimina ni altera tablas anteriores. No se generó, editó ni aplicó ninguna migración en esta integración.

## Verificación ejecutada sin actualizar PostgreSQL

- Compilación de Zuni: 0 errores y 0 advertencias (NuGetAudit=false; no se ejecutó auditoría de vulnerabilidades).
- `tests/CombinedModel.Checks`: 26 comprobaciones de modelo/snapshot/TargetModel, tablas anteriores, claves, concurrencia, SQL por titular/publicación, permisos, antifalsificación y redirección antigua.
- `tests/StudentProfile.Checks`: 35 comprobaciones adaptadas a MiCuenta, validación de datos, carné, progreso del perfil, edición administrativa y CSV.
- 8 pruebas JavaScript: cuatro de Evaluaciones y cuatro del script de perfil anterior conservado. Estas últimas son regresión de ese archivo aislado; MiCuenta no utiliza ese script y su formulario fue validado por compilación Razor/modelos.
- Los dos ejecutables HTTP/PostgreSQL de Evaluaciones se compilaron y sus rutas de perfil se adaptaron a MiCuenta. La prueba Playwright ahora espera el login al cerrar sesión.

No se inició la aplicación contra la base ni se ejecutaron las suites HTTP/PostgreSQL/Chromium durante esta integración: incluyen guardados, finalizaciones y publicación temporal. Por la instrucción de no actualizar la base, esas pruebas requieren repetición posterior autorizada. Los 123 controles HTTP/PostgreSQL y 17 de Chromium aprobados anteriormente corresponden al estado anterior al merge y no se presentan como aprobación del modelo integrado. Falta repetir especialmente edición de MiCuenta, persistencia tras login, permisos HTTP, cancelación/finalización y restauración real de demostraciones bajo la aplicación combinada.

## Migraciones para otra computadora

El repositorio incluye, en orden:

1. `20260902032536_InitialIdentity`
2. `20260906120000_AddPasswordResetTokens`
3. `20260909210956_AddPerfilEstudiante`
4. `20260909230525_AjustarPerfilEstudiante`
5. `20260930223405_AddAuditoriaUsuarios`
6. `20261003010457_AddDebeCambiarContrasena`
7. `20261009043711_AddEvaluaciones`

Requisitos: SDK .NET 8, PostgreSQL accesible y paquetes NuGet del proyecto. Configura `ConnectionStrings:DefaultConnection` mediante User Secrets o variables de entorno; el repositorio no lleva la conexión privada. Restaura dependencias y compila. El responsable de la otra base debe revisar su historial y aplicar únicamente las migraciones faltantes, por ejemplo con `dotnet ef database update --project Zuni/Zuni.csproj` usando dotnet-ef 8 compatible. Este comando no se ejecutó en esta integración. No hay scripts SQL independientes nuevos.

Los registros de demostración locales de Juan y Evin no viajan con Git. Sus utilidades requieren ZuniDesarrollo local y las identidades originales; no se cargan automáticamente. Para reproducir pruebas completas en otra computadora, prepara cuentas/datos de prueba autorizados o una restauración autorizada de la base. No copies claves de sesión ni credenciales desde Git.

Comprobaciones offline desde la raíz:

```powershell
dotnet build Zuni/Zuni.csproj -p:NuGetAudit=false
dotnet run --project tests/CombinedModel.Checks -p:NuGetAudit=false
dotnet run --project tests/StudentProfile.Checks -p:NuGetAudit=false
node --test tests/evaluaciones-ui.test.cjs tests/student-profile.test.cjs
```

Las suites HTTP requieren compilar primero Zuni en `.visual-check/eval-runtime`, iniciar la aplicación Development con raíz de contenido Zuni en 127.0.0.1:7111 y disponer de las demostraciones originales. Chromium requiere Node, Playwright accesible mediante NODE_PATH y Microsoft Edge. No ejecutar estas suites junto a otra sesión que modifique las demostraciones.

Los informes anteriores se conservan como evidencia histórica. La estructura vigente del perfil después de integrar es MiCuenta, según este documento. Citas, conexión Estudiante–Psicólogo y estadísticas del tablero siguen pendientes. Las modificaciones del panel Estudiante quedan pausadas después de publicar para coordinar la siguiente integración.
