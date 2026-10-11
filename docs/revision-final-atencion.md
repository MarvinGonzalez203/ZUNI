# Revisión final — AddFlujoAtencionInicial

La migración sigue sin aplicarse. No se abrió ninguna conexión a PostgreSQL.

## Corrección realizada en esta revisión

Único archivo funcional ajustado: Zuni/Data/ApplicationDbContextFactory.cs.
También se actualizó docs/flujo-atencion-inicial.md y se creó este informe.
Los demás cambios pendientes del repositorio corresponden al bloque anterior.

La factory se conserva para separar las herramientas EF del arranque de Program/SeedRolesAsync. Se eliminó completamente la conexión ficticia.
EF la descubre para crear ApplicationDbContext en tiempo de diseño: afecta tanto Add-Migration como Update-Database de PMC y dotnet ef database update.
No participa en el arranque web habitual: Program sigue registrando el contexto original.

Ahora requiere --environment explícito y comprueba que contentRoot contiene Zuni.csproj.
WebApplication.CreateBuilder utiliza el nombre del assembly Zuni para cargar la misma configuración que la aplicación:
appsettings.json, appsettings.{Environment}.json, User Secrets en Development, variables de entorno y argumentos.
Obtiene ConnectionStrings:DefaultConnection; si falta, falla sin mostrar valores.
No llama a Build/Run, SeedRolesAsync, Migrate ni OpenConnection.
La configuración de las opciones/contexto no abre conexión; una orden futura database update sí lo hará.

## Futura actualización: NO ejecutada

Primero detener la depuración, confirmar respaldo, revisar de forma privada el destino de DefaultConnection y seleccionar el mismo entorno que ZUNI.
Los perfiles launchSettings no deben asumirse como fuente de configuración de EF.
Mantener credenciales en User Secrets/variables de entorno, nunca en comandos ni archivos versionados.
Los siguientes ejemplos usan Development: solo son apropiados si ese es el entorno configurado y revisado.
Especificar la migración exacta evita aplicar migraciones posteriores accidentalmente. Comprobar también que la BD no tenga una migración posterior antes de usar un destino concreto, para evitar una reversión.

Package Manager Console (proyectos Zuni):

```powershell
Update-Database -Migration 20261010033140_AddFlujoAtencionInicial -Context ApplicationDbContext -Project Zuni -StartupProject Zuni -Args '--environment Development --contentRoot "C:\Users\USUARIO\Desktop\Seminario\ZUNI\Zuni"'
```

CLI desde la raíz del repositorio, si dotnet-ef está disponible:

```powershell
dotnet ef database update 20261010033140_AddFlujoAtencionInicial --context ApplicationDbContext --project Zuni/Zuni.csproj --startup-project Zuni/Zuni.csproj -- --environment Development --contentRoot "C:\Users\USUARIO\Desktop\Seminario\ZUNI\Zuni"
```

No se ejecutó ninguno de estos comandos. No se instalaron herramientas.

## Seguridad revisada

- Estudiantes llama a ObtenerPsicologoAutorizadoAsync antes de consultar el listado; no depende solo del atributo Authorize.
- El servicio comprueba sesión, identificador, SecurityStamp, IsActive, ausencia de contraseña obligatoria y roles actuales de PostgreSQL.
- Administrador + Psicologo, Director + Psicologo y Catedratico + Psicologo se rechazan centralmente.
- Psicologo activo sin esas combinaciones puede entrar con una sesión vigente.
- El listado filtra por el identificador autenticado y FechaFinalizacionUtc == null.
- Expediente llama a PuedeConsultarExpedienteAsync, que reutiliza la misma comprobación y exige asignación vigente al perfil de la solicitud y consentimiento aceptado.
- Solicitud ajena y solicitud inexistente producen Forbid, sin mensaje que distinga existencia. Con cookies, Forbid puede materializarse como redirección a AccesoDenegado; no necesariamente como un HTTP 403 directo.
- No se modificó lógica clínica ni se duplicaron comprobaciones.

## Migración y snapshot

- Up: exactamente 6 CreateTable y 9 CreateIndex. Solo PK, FK Restrict y 9 CHECK dentro de las tablas nuevas.
- Tablas: AsignacionesEstudiantePsicologo, SolicitudesAtencion, ConsentimientosAtencion, EventosAccesoClinico, ExpedientesIniciales y ContactosEmergenciaExpediente.
- Sin AlterTable, AlterColumn, DropTable ni SQL manual en Up.
- Sin cambios sobre AspNetUsers, AspNetRoles, AspNetUserRoles, PerfilesEstudiante, PasswordResetTokens o AuditoriaUsuarios. Las FK nuevas referencian usuarios/perfiles sin alterar sus definiciones.
- Down elimina solo las seis tablas nuevas en orden válido.
- Comparación offline mediante IMigrationsModelDiffer: modelo actual, snapshot y TargetModel del Designer coinciden.
- No se creó otra migración ni se modificó la existente.

## Referencia, solicitud activa y asignación

IValidatableObject exige NombreReferente y MotivoReferencia cuando TipoIngreso es Referencia; EnumDataType rechaza valores de tipo inválidos.
El POST comprueba ModelState antes de persistir. Omitir campos, enviarlos en blanco o alterar JavaScript no evita esta validación.
IniciativaPropia no exige esos campos; el controlador los persiste como null.

El servidor busca Pendiente o Asignada y UX_SolicitudesAtencion_Activa es UNIQUE por PerfilEstudianteId WHERE Estado IN (0,1).
Impide tanto dos Pendiente como Pendiente+Asignada y dos Asignada, incluso concurrentes.

Asignación:
- 0 psicólogos activos: PendienteSinPsicologo; la solicitud ya confirmada permanece pendiente.
- 1 activo elegible: crea asignación y actualiza estado a Asignada dentro de la misma transacción.
- Más de 1 activo: PendienteMultiplesPsicologos.
- Historial sin vigente: RequiereRevision.
- Vigente: YaAsignado; sin otra fila.
- Único psicólogo con rol institucional prohibido: RequiereRevision.
- Take(2) solo distingue cardinalidad; Single() solo se usa si hay exactamente uno. No se selecciona arbitrariamente.

## Verificación y límites

Compilación de solución: dotnet build Zuni/Zuni.sln -p:OutputPath=bin/atencion-review/
Resultado: 0 errores, 0 advertencias.
Se conserva salida aislada para no sobrescribir el ejecutable abierto.

37 comprobaciones offline correctas: validación MVC, invariantes del modelo, operaciones de migración, snapshot, Designer, traducción SQL y factory.
Factory probada con configuración de prueba únicamente en memoria y conexión cerrada; no se usaron ni imprimieron credenciales reales.
El programa de comprobación externo al repositorio fue actualizado para no depender del antiguo destino ficticio de la factory.

La autorización de roles actuales y la concurrencia fueron revisadas en código; no se afirma haberlas probado contra PostgreSQL.
Las pruebas de integración quedan pendientes de una aplicación de migración expresamente autorizada.
No Update-Database, conexiones, cambios PostgreSQL, commit, push ni merge.
