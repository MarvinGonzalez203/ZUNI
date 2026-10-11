# ZUNI — Endurecimiento previo al despliegue

Rama: `feature/predeploy-hardening` (base `d4b11bc`). Sin commit, sin push.

**Estado de verificación:** estos cambios se escribieron sin una terminal .NET disponible.
**No se ha compilado ni ejecutado ninguna prueba con estos cambios.** La verificación debe hacerse con los
comandos de la sección 14.

No se ejecutó `Update-Database`, no se abrió ni modificó PostgreSQL, no se crearon ni modificaron migraciones,
Designer ni snapshot, no se borraron datos ni claves de Data Protection y no se agregaron paquetes NuGet
a la aplicación.

## 1. Archivos creados

| Archivo | Propósito |
|---|---|
| `Zuni/Controllers/AdministradorSolicitudesController.cs` | Rutas `/Administrador/SolicitudesAtencion` (listado, detalle, asignar, reasignar, finalizar, cerrar). |
| `Zuni/Services/Administracion/GestionSolicitudesService.cs` | Lógica transaccional (SERIALIZABLE) de asignación manual, reasignación, finalización y cierre. |
| `Zuni/Services/Administracion/AccionesAuditoria.cs` | Nombres de acciones de auditoría y `EstadoCuenta` (pendiente de activación sin columna nueva). |
| `Zuni/Models/Administrador/SolicitudesAtencionAdminViewModels.cs` | Modelos administrativos sin campos clínicos. |
| `Zuni/Models/Administrador/ContrasenaTemporalViewModels.cs` | Confirmación y resultado de contraseña temporal. |
| `Zuni/Views/AdministradorSolicitudes/Index.cshtml`, `Detalle.cshtml`, `_ViewStart.cshtml` | Pantallas de solicitudes. |
| `Zuni/Views/Administrador/ContrasenaTemporal.cshtml`, `ContrasenaTemporalGenerada.cshtml` | Confirmación y visualización única de la contraseña temporal. |
| `Zuni/Helpers/ContrasenaTemporalHelper.cs` | Generador criptográfico de contraseñas temporales. |
| `Zuni/Seguridad/SesionZuni.cs` | Política de cookie: inactividad 30 min, máximo absoluto 8 h, revalidación de SecurityStamp. |
| `Zuni/Seguridad/PoliticasAcceso.cs` | Rate limiting nativo .NET 8 y respuesta 429. |
| `Zuni/Seguridad/CabecerasSeguridadMiddleware.cs` | Cabeceras de seguridad. |
| `Zuni/Data/PrimerAdministrador.cs` | Comando manual `--crear-primer-admin=true`. |
| `Zuni/wwwroot/js/sesion-activa.js` | Mantiene la sesión mientras se escribe en un formulario. |
| `tests/Seguridad.Offline/*` | Nuevo proyecto de comprobaciones offline (Program.cs, Infraestructura.cs, README.md, .csproj). |
| `docs/deployment.md` | Guía de despliegue. |
| `docs/predeploy-hardening.md` | Este informe. |

## 2. Archivos modificados

| Archivo | Cambio |
|---|---|
| `Zuni/Program.cs` | Cadena de conexión obligatoria (sin secretos), Data Protection con carpeta configurable, cookie vía `SesionZuni`, antiforgery `Secure` en Production, rate limiting, cabeceras, proxy inverso opcional, comando de primer administrador, registro de `GestionSolicitudesService`. |
| `Zuni/Controllers/CuentaController.cs` | Bloqueo por intentos, hash ficticio, mensajes genéricos, autorregistro pendiente, rate limiting, sliding session, `MantenerSesion`, reinicio de bloqueo al restablecer. |
| `Zuni/Controllers/AdministradorController.cs` | Bloqueo de cambio de rol con asignaciones vigentes, contraseña temporal, filtro y estado "pendiente de activación", `LockoutEnabled` en altas. |
| `Zuni/Controllers/MiCuentaController.cs` | Al actualizar el nombre, la cookie conserva la renovación por actividad (`AllowRefresh = true`). |
| `Zuni/Models/Administrador/UsuarioAdminViewModel.cs`, `CambiarEstadoUsuarioViewModel.cs` | Propiedad `PendienteActivacion`. |
| `Zuni/Models/Administrador/HistorialUsuarioViewModel.cs` | Etiquetas de las nuevas acciones de auditoría. |
| `Zuni/Views/Administrador/_Navegacion.cshtml` | Entrada "Solicitudes de atención". |
| `Zuni/Views/Administrador/Index.cshtml`, `ConfirmarEstado.cshtml` | Estado "Pendiente de activación", "Activar registro", "Asignar contraseña temporal". |
| `Zuni/Views/Cuenta/Registro.cshtml` | Aviso de activación por administrador. |
| `Zuni/Views/Shared/_DashboardLayout.cshtml` | Incluye `sesion-activa.js` solo para usuarios autenticados. |
| `.gitignore` | Refuerzo para datos sensibles y carpetas de comprobación. |

No se modificaron: BigFiveService, AgendaCitasService, AutorizacionClinicaService, AsignacionPsicologoService,
RegistroAtencionCita, expediente, consentimiento, SolicitudAtencion (modelo), ApplicationDbContext, migraciones,
Designer ni snapshot. Las vistas de Agenda, Big Five y Atención no cambiaron.

## 3. A1 — Solicitudes y asignaciones

Pantalla `/Administrador/SolicitudesAtencion` (menú "Solicitudes de atención"):

- Filtros por estado (Pendiente, Asignada, Cerrada) y búsqueda por nombre o carné (acepta guiones). Paginación de 25.
- Muestra solo: estudiante, carné, carrera, fecha de solicitud, estado, psicólogo vigente (y si su cuenta está inactiva),
  fecha de asignación, número de citas activas e historial de asignaciones.
- No muestra ni consulta: motivo de consulta, expediente, dirección, contactos de emergencia, Big Five, notas ni resultados.

Operaciones (todas en una transacción SERIALIZABLE, revalidando que el actor sea Administrador activo):

- **Asignar** (solo Pendiente): el Administrador elige explícitamente el psicólogo. Crea la asignación vigente y pasa la solicitud a Asignada.
- **Reasignar** (solo Asignada): finaliza la asignación anterior (`FechaFinalizacionUtc`), guarda, crea la nueva vigente; la solicitud sigue Asignada. Rollback completo ante cualquier fallo.
- **Finalizar asignación** (solo Asignada): finaliza la asignación y **devuelve la solicitud a Pendiente** para asignar a otro psicólogo.
- **Cerrar solicitud** (Pendiente o Asignada, con confirmación): finaliza cualquier asignación vigente y marca Cerrada. Una cerrada no se reasigna, no vuelve a Pendiente y sigue visible en el listado. El estudiante puede iniciar una nueva solicitud.
- Psicólogos elegibles: cuenta activa, rol real Psicologo, sin Administrador/Director/Catedratico. Las cuentas con cambio de contraseña pendiente **se incluyen** (igual que la asignación automática) y se marcan en la lista: no podrán atender hasta cambiarla.
- El índice `UX_AsignacionesEstudiantePsicologo_Vigente` sigue impidiendo dos asignaciones vigentes; no se borra ninguna asignación.
- Un `SolicitudId` del formulario distinto al de la ruta se rechaza (400).

**Decisión de seguridad clínica:** no se permite reasignar, finalizar ni cerrar mientras el estudiante tenga citas activas
(Reservada, Solicitada o Confirmada). Primero deben cancelarse o registrarse. Así ninguna cita queda con un psicólogo que perdió el acceso clínico.

**Cambio de rol:** `CambiarRol` rechaza en servidor cualquier rol distinto de Psicologo para un usuario con asignaciones vigentes, con el mensaje:
"Este psicólogo tiene estudiantes asignados actualmente. Finaliza o reasigna sus asignaciones antes de cambiar el rol."

**Auditoría** (`AuditoriaUsuarios`, usuario afectado = estudiante): `SOLICITUD_ASIGNADA`, `SOLICITUD_REASIGNADA`,
`ASIGNACION_FINALIZADA`, `SOLICITUD_CERRADA`. Solo guarda Id de solicitud, estado, Id de psicólogo e Id de asignación, más el motivo administrativo opcional.

## 4. A2 — Protección del inicio de sesión

- 5 intentos fallidos consecutivos → `LockoutEnd = ahora + 15 min` y el contador vuelve a 0. Columnas existentes `AccessFailedCount`, `LockoutEnd`, `LockoutEnabled` (sin columnas nuevas). El incremento es atómico (`ExecuteUpdate`).
- Mientras `LockoutEnd > ahora` no se verifica la contraseña real.
- Un inicio correcto reinicia contador y bloqueo. Restablecer contraseña y asignar contraseña temporal también lo reinician.
- Un único mensaje para correo inexistente, contraseña incorrecta y cuenta bloqueada:
  "Correo o contraseña incorrectos. Si fallaste varias veces, espera unos minutos antes de intentarlo de nuevo."
- Correo inexistente, cuenta bloqueada o sin hash: se verifica contra un hash ficticio del mismo algoritmo para igualar tiempos. No se crea ningún registro.
- La política se aplica a todas las cuentas; `LockoutEnabled` se marca `true` al registrar un fallo y en cuentas nuevas.

**Rate limiting** (nativo, sin paquetes): ventana fija por IP y política, solo en los POST de
`/Cuenta/IniciarSesion` (30/min), `/Cuenta/Registro` (20/10 min), `/Cuenta/RecuperarContrasena` y `/Cuenta/RestablecerContrasena` (10/10 min).
Configurable en `Zuni:LimitesAcceso:*`. Al excederse: HTTP 429, `Retry-After`, página simple sin detalles internos. Los GET no se limitan.

## 5. A3 — Contraseña temporal por Administrador

Administrador → Usuarios → "Asignar contraseña temporal" → confirmación → POST:

- 16 caracteres generados con `RandomNumberGenerator` (mayúsculas, minúsculas, dígitos y símbolos; sin caracteres ambiguos).
- Hash con el mismo `IPasswordHasher<ApplicationUser>`; `DebeCambiarContrasena = true`; nuevo `SecurityStamp` (cierra todas las sesiones); tokens de recuperación pendientes marcados como usados; bloqueo reiniciado.
- **No cambia `IsActive`**: una cuenta inactiva o pendiente sigue sin acceso.
- Auditoría `CONTRASENA_TEMPORAL_ASIGNADA` sin contraseña ni hash.
- La contraseña se muestra una sola vez en la respuesta directa del POST (sin redirección, sin TempData, sin logs, con `no-store`).
- El Administrador no puede asignársela a sí mismo (debe usar Mi cuenta).

## 6. Activación de cuentas autorregistradas (M3)

- El autorregistro crea la cuenta con `IsActive = false` y no inicia sesión. Mensaje: la cuenta queda pendiente de activación.
- Sin columnas nuevas: "pendiente de activación" = `IsActive = false` **y** el último evento de estado de la cuenta en
  `AuditoriaUsuarios` es `REGISTRO_PENDIENTE_ACTIVACION`. En ese evento, `AdministradorId` = el propio usuario.
  Una cuenta desactivada por un Administrador tiene como último evento `USUARIO_DESACTIVADO` y se muestra como "Inactivo".
- Administrador → Usuarios: estado "Pendiente de activación", filtro "Pendientes de activación" y botón "Activar registro" (usa la acción Reactivar existente, que registra `USUARIO_REACTIVADO`).
- Inicio de sesión de una cuenta pendiente: con contraseña correcta, "Tu cuenta está pendiente de activación por un administrador."; con contraseña incorrecta, el mensaje genérico (no se revela el estado).
- Las cuentas creadas por el Administrador mantienen el comportamiento anterior (activas y con cambio obligatorio).
- **Limitación:** si se borran filas de `AuditoriaUsuarios`, una cuenta pendiente se mostraría como "Inactivo". Las cuentas autorregistradas antes de este cambio siguen activas.

## 7. Sesión (M4)

- `SlidingExpiration = true` con 30 minutos de inactividad (`AllowRefresh = true`).
- Máximo absoluto de 8 horas: claim `Zuni.InicioSesionUtc` emitido al iniciar sesión y validado en `OnValidatePrincipal`.
  Las cookies emitidas antes de este cambio no tienen ese claim: **todos los usuarios deberán iniciar sesión una vez después del despliegue**.
- Se mantienen HttpOnly, SameSite=Lax, Secure fuera de Development, revalidación de usuario activo y SecurityStamp en cada petición, cambio obligatorio y no-store.
- `sesion-activa.js`: mientras se escribe en un formulario POST, cada 5 minutos se llama a `GET /Cuenta/MantenerSesion` (204, no envía datos). Esto evita perder una nota de atención larga; no extiende el máximo de 8 horas.
- Nota: la renovación de ASP.NET Core ocurre cuando ha pasado más de la mitad de la ventana, por eso el aviso se hace cada 5 minutos.

## 8. Cabeceras de seguridad

En todas las respuestas (sin sobrescribir valores ya puestos por una acción):

- `X-Content-Type-Options: nosniff`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `X-Frame-Options: SAMEORIGIN` (compatible con `frame-ancestors 'self'`)
- `Content-Security-Policy: frame-ancestors 'self'; object-src 'none'; base-uri 'self'`
- `Permissions-Policy: camera=(), microphone=(), geolocation=()`

No se agregó `script-src`, `style-src` ni `form-action`: las vistas actuales usan scripts y estilos inline.

## 9. Higiene de archivos sensibles

`.gitignore` ahora también excluye `**/App_Data/`, `**/App_Data/keys/`, `**/users.json`, `secrets.json`, `*.pfx`, `*.pem`, `*.key`,
`.env`, `.env.*`, `.*-check/` y `.verification-*/`. No afecta a ningún archivo versionado.

Archivos sensibles presentes en tu PC (no se borró ninguno, no se muestran contenidos):

- `Zuni/App_Data/keys/key-dda85722-….xml`: clave de Data Protection sin cifrar (no versionada).
- `.visual-check/App_Data/users.json` y `.clean-check/App_Data/users.json`: nombres, correos y hashes de una versión antigua (no versionados).
- Historial de Git: `users.json` con hashes fue agregado en el commit `7cd14ed` y retirado en `9504c3a`; sigue en la historia.

## 10. Configuración de producción

- Cadena de conexión: solo desde configuración externa (`ConnectionStrings__DefaultConnection`); la app se detiene si falta.
- Cookies de sesión y antiforgery `Secure` en Production.
- Recuperación por enlace: sigue disponible solo en Development; en Production se indica contactar al administrador (que ahora puede asignar contraseña temporal).
- Data Protection: `DataProtection:KeysPath` configurable; por defecto `App_Data/keys` (las claves actuales siguen válidas). No se duplicó la configuración.
- Proxy inverso opcional: `Zuni:ProxyInverso=true`.
- No hay secretos en el código.
- Detalle en `docs/deployment.md`.

## 11. Primer Administrador

Antes no existía un procedimiento (solo se crean roles). Se agregó un comando manual:
`dotnet Zuni.dll --crear-primer-admin=true --correo <correo>@miumg.edu.gt --nombre "<Nombre>"`.
Solo actúa si no hay ningún Administrador activo, genera una contraseña temporal mostrada una vez en consola,
obliga a cambiarla y registra `PRIMER_ADMIN_CREADO`. No usa credenciales en código ni en variables de entorno.
No tiene prueba automatizada (requiere ejecutar Program); probarlo en una base descartable.

## 12. Pruebas agregadas — `tests/Seguridad.Offline`

Asignaciones (servicio y HTTP), contraseña temporal, inicio de sesión, rate limiting, autorregistro, sesión y cabeceras.
El detalle está en `tests/Seguridad.Offline/README.md`. Usa SQLite en memoria y un host local efímero con la misma configuración
de seguridad que `Program.cs`. No abre PostgreSQL.

## 13. Limitaciones y pendientes

- **Sin compilar ni probar en esta sesión** (sin terminal .NET). Ejecutar la sección 14.
- El Administrador no puede cancelar citas: si un estudiante tiene citas activas, el estudiante o el psicólogo deben cancelarlas (o registrarse la atención) antes de reasignar, finalizar o cerrar.
- Desactivar un psicólogo con asignaciones vigentes **no** se bloquea (para no impedir una respuesta de seguridad urgente). La pantalla de solicitudes marca "cuenta inactiva" para reasignar o finalizar.
- Al finalizar o reasignar, el psicólogo anterior pierde el acceso clínico a esa solicitud (incluidas sus notas previas), porque la autorización clínica exige asignación vigente. Es la regla existente; no se cambió.
- Una solicitud nueva de un estudiante que ya tuvo asignaciones queda Pendiente (la asignación automática devuelve "requiere revisión") y se asigna desde la pantalla.
- Big Five permite una participación por estudiante y versión; una nueva solicitud no habilita otra participación (regla existente).
- La concurrencia SERIALIZABLE real de PostgreSQL no se reproduce en SQLite.
- La lógica de roles sigue duplicada en varios servicios (M6), fechas mixtas UTC/local en vistas antiguas (M9): pendientes para después de la presentación.

## 14. Pasos manuales antes del despliegue

1. Ejecutar los comandos de verificación:
   ```
   dotnet restore Zuni\Zuni.sln
   dotnet build Zuni\Zuni.sln -c Release
   dotnet run --project tests\Agenda.Offline -c Release
   dotnet run --project tests\BigFive.Offline -c Release
   dotnet run --project tests\Seguridad.Offline -c Release
   dotnet list Zuni\Zuni.csproj package --vulnerable --include-transitive
   ```
2. Prueba manual contra tu PostgreSQL local:
   - autorregistro → activación;
   - asignación manual con dos psicólogos, reasignación, finalización y cierre;
   - contraseña temporal;
   - bloqueo tras 5 intentos;
   - registro de atención de una cita finalizada.
3. En el servidor: claves de Data Protection nuevas en una carpeta persistente (no copiar las de tu PC).
4. Si el repositorio es o fue público: cambiar las contraseñas de las cuentas que aparecían en `users.json`.
5. No distribuir ZIPs con `App_Data`, `.visual-check`, `.clean-check`, `bin` u `obj`.
6. Respaldo, script idempotente de migraciones y creación del primer administrador según `docs/deployment.md`.
7. Avisar a los usuarios que deberán iniciar sesión de nuevo tras el despliegue y que los autorregistros requieren activación.
