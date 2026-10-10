# ZUNI — Núcleo del flujo de atención inicial

## Resultado y alcance

Rama: feature/final-integration.

Migración creada: **20261010033140_AddFlujoAtencionInicial**.
**No aplicada. No se ejecutó Update-Database ni database update. No se inició la aplicación ni se abrió una conexión a PostgreSQL para esta implementación.**
No hubo commit, push ni merge.

El código queda preparado para revisión. Las nuevas pantallas requieren las seis tablas nuevas: no probar el flujo sobre la base actual antes de revisar y aplicar la migración en una fase autorizada.

## Archivos creados

Rutas relativas a la raíz del repositorio:

- Zuni/Data/ApplicationDbContextFactory.cs
- Zuni/Models/Atencion/EstadoSolicitudAtencion.cs
- Zuni/Models/Atencion/SolicitudAtencion.cs
- Zuni/Models/Atencion/ConsentimientoAtencion.cs
- Zuni/Models/Atencion/ExpedienteInicial.cs
- Zuni/Models/Atencion/ContactoEmergenciaExpediente.cs
- Zuni/Models/Atencion/AsignacionEstudiantePsicologo.cs
- Zuni/Models/Atencion/EventoAccesoClinico.cs
- Zuni/Models/Atencion/SolicitarAtencionViewModel.cs
- Zuni/Models/Atencion/ConsultaClinicaViewModels.cs
- Zuni/Resources/ConsentimientoInformado.cs
- Zuni/Services/IAsignacionPsicologoService.cs
- Zuni/Services/AsignacionPsicologoService.cs
- Zuni/Services/IAutorizacionClinicaService.cs
- Zuni/Services/AutorizacionClinicaService.cs
- Zuni/Views/Estudiante/SolicitarAtencion.cshtml
- Zuni/Views/Psicologo/Expediente.cshtml
- Zuni/Migrations/20261010033140_AddFlujoAtencionInicial.cs
- Zuni/Migrations/20261010033140_AddFlujoAtencionInicial.Designer.cs
- docs/flujo-atencion-inicial.md (este informe, incluida la migración completa)

## Archivos modificados

- Zuni/Controllers/EstudianteController.cs: formulario, transacción de solicitud y consulta de estado.
- Zuni/Controllers/PsicologoController.cs: listado real y consulta autorizada/auditada.
- Zuni/Data/ApplicationDbContext.cs: seis DbSet y configuración Fluent API.
- Zuni/Migrations/ApplicationDbContextModelSnapshot.cs: únicamente entidades nuevas y sus relaciones.
- Zuni/Models/EstudianteDashboardViewModel.cs: estado de solicitud nullable.
- Zuni/Program.cs: únicamente registro scoped de los dos servicios.
- Zuni/Views/Estudiante/Index.cshtml: acceso y estado de atención.
- Zuni/Views/Estudiante/_Navegacion.cshtml: acceso a Solicitar atención.
- Zuni/Views/Psicologo/Estudiantes.cshtml: listado real, mínimo y sin contenido clínico.

No se modificaron ApplicationUser, PerfilEstudiante, CuentaController, autenticación, middleware ni migraciones anteriores.

## Modelos, campos y nulabilidad

Todos los Id son Guid con inicializador Guid.NewGuid(). Las fechas son DateTime UTC, mapeadas como timestamp with time zone. Los valores iniciales de Id y fechas se generan en C#, no mediante DEFAULT SQL.

| Tabla | Campos |
| --- | --- |
| SolicitudesAtencion | Id, PerfilEstudianteId, FechaSolicitudUtc, Estado, TipoIngreso, UsuarioReferenteId nullable |
| ConsentimientosAtencion | Id, SolicitudAtencionId, VersionConsentimiento (50), Aceptado, FechaRespuestaUtc |
| ExpedientesIniciales | Id, SolicitudAtencionId, Edad, EsMayorEdad, Sexo nullable (50), Direccion (300), IdiomaPreferido (80), MotivoConsulta (2000), NombreReferente nullable (150), MotivoReferencia nullable (1000), ConsideracionesAtencion nullable (1500), FechaCreacionUtc |
| ContactosEmergenciaExpediente | Id, ExpedienteInicialId, Nombre (150), Relacion (60), Telefono (8), Orden |
| AsignacionesEstudiantePsicologo | Id, PerfilEstudianteId, PsicologoUsuarioId, FechaAsignacionUtc, FechaFinalizacionUtc nullable |
| EventosAccesoClinico | Id, UsuarioId, SolicitudAtencionId, TipoAcceso (50), FechaUtc |

Los identificadores de usuarios conservan el tipo string/text de AspNetUsers.Id.
Estado: Pendiente=0, Asignada=1, Cerrada=2. TipoIngreso: IniciativaPropia=0, Referencia=1.
MotivoConsulta existe exclusivamente en ExpedientesIniciales.

## Relaciones

Todas las FK nuevas usan **Restrict**, sin cascadas:

- SolicitudAtencion → PerfilEstudiante: muchas a uno.
- SolicitudAtencion → ApplicationUser referente: opcional.
- ConsentimientoAtencion → SolicitudAtencion: uno a uno.
- ExpedienteInicial → SolicitudAtencion: uno a uno.
- ContactoEmergenciaExpediente → ExpedienteInicial: muchos a uno.
- AsignacionEstudiantePsicologo → PerfilEstudiante y ApplicationUser psicólogo.
- EventoAccesoClinico → ApplicationUser y SolicitudAtencion.

No se agregaron navegaciones a ApplicationUser ni PerfilEstudiante. Las nuevas entidades tienen identificadores escalares; las relaciones se definen por Fluent API. Esto evita cargar información clínica mediante navegaciones administrativas existentes.

La creación conjunta garantiza que cada solicitud enviada tenga consentimiento, expediente y al menos un contacto. Una FK uno-a-uno por sí sola no exige que todo principal tenga un dependiente; esa obligatoriedad se garantiza en la transacción del flujo, no mediante triggers.

## Índices y restricciones

Nueve índices nuevos:

1. UNIQUE PerfilEstudianteId en asignaciones WHERE FechaFinalizacionUtc IS NULL.
2. PsicologoUsuarioId + FechaFinalizacionUtc.
3. UNIQUE SolicitudAtencionId en consentimientos.
4. UNIQUE SolicitudAtencionId en expedientes.
5. UNIQUE ExpedienteInicialId + Orden en contactos.
6. UsuarioId + FechaUtc en eventos.
7. SolicitudAtencionId + FechaUtc en eventos.
8. UsuarioReferenteId en solicitudes.
9. UNIQUE PerfilEstudianteId en solicitudes WHERE Estado IN (0, 1).

Nueve CHECK:

- Estado IN (0, 1, 2).
- TipoIngreso IN (0, 1).
- Consentimiento Aceptado = true.
- Edad entre 18 y 120 y EsMayorEdad = true.
- Dirección, idioma y motivo no vacíos.
- Orden de contacto entre 1 y 3.
- Teléfono de contacto: exactamente ocho dígitos ASCII.
- Nombre y relación de contacto no vacíos.
- FechaFinalizacionUtc nula o mayor/igual a FechaAsignacionUtc.

Orden entre 1 y 3 + índice único por expediente limita a tres contactos en BD. El mínimo de uno se valida en servidor y se guarda dentro de la transacción.

## Flujo del estudiante y consentimiento

GET/POST /Estudiante/SolicitarAtencion usa un ViewModel específico, antiforgery y validación del servidor.
El perfil se obtiene desde el identificador de sesión y se comprueban usuario/perfil activos, rol Estudiante actual, SecurityStamp y ausencia de cambio obligatorio de contraseña.

Se muestra el consentimiento **ZUNI-CONSENT-1**, constante versionada. Incluye finalidad, almacenamiento en ZUNI, acceso restringido, información correcta, mayoría de edad, aceptación voluntaria, carácter provisional académico y:
“Este sistema no sustituye servicios de emergencia.”
No debe editarse el texto de una versión ya aceptada; debe publicarse otra versión.

Edad, confirmación de mayoría y consentimiento son obligatorios. Si TipoIngreso es Referencia, se exigen nombre y motivo. Se admiten de uno a tres contactos completos; no se copian automáticamente desde PerfilEstudiante.

Solicitud, consentimiento, expediente y contactos se guardan en una transacción Serializable. Un índice parcial impide solicitudes activas duplicadas incluso ante envíos concurrentes.

Después del commit se intenta la asignación en una transacción independiente. Un fallo de asignación no elimina la solicitud. El dashboard solo muestra Pendiente/Asignada y mensajes operativos; no muestra datos clínicos ni el nombre del psicólogo.

## Asignación automática

Servicio idempotente con transacción Serializable y protección final mediante índice único parcial:

- Perfil inexistente/inactivo o usuario no activo/no Estudiante: PerfilNoElegible.
- Asignación vigente: YaAsignado; actualiza las solicitudes pendientes a Asignada.
- Historial sin asignación vigente: RequiereRevision, sin reasignar.
- Cero psicólogos activos: PendienteSinPsicologo.
- Más de uno: PendienteMultiplesPsicologos.
- Exactamente uno: asignación y cambio de estado en la misma transacción.
- Si ese único psicólogo también posee Administrador, Director o Catedratico: RequiereRevision.
- Hasta tres intentos ante serialización, deadlock o conflicto del índice de asignación; después ErrorTemporal.

Take(2) se utiliza únicamente para distinguir cero/uno/múltiples; Single() solo se ejecuta cuando hay exactamente uno. No se escoge arbitrariamente un profesional.

No se implementó aún interfaz de revisión/asignación manual ni tarea en segundo plano. Las solicitudes pendientes quedan almacenadas para una fase posterior.

## Autorización clínica, psicólogo y auditoría

/Psicologo/Estudiantes comprueba el psicólogo actual en PostgreSQL y muestra únicamente asignaciones vigentes de esa cuenta. Campos visibles: nombre, carné, carrera y fecha UTC. El enlace usa la solicitud más reciente que tenga expediente y consentimiento aceptado.

/Psicologo/Expediente/{solicitudId} comprueba mediante IAutorizacionClinicaService:

- sesión autenticada con rol Psicologo;
- usuario real activo, SecurityStamp vigente y sin cambio obligatorio pendiente;
- rol Psicologo actual en PostgreSQL;
- ausencia de Administrador, Director y Catedratico en los roles actuales;
- consentimiento aceptado;
- asignación vigente para el mismo PerfilEstudiante y el psicólogo de la sesión.

Administrador + Psicologo, Director + Psicologo y Catedratico + Psicologo quedan denegados. Estudiante no se trata como rol administrativo/institucional de gestión en esta lista explícita.

Se devuelve Forbid tanto para solicitud ajena como inexistente; no se autoriza por parámetros de usuario/perfil enviados por el navegador. No hay acceso administrativo alternativo.

Autorización de recurso, lectura y registro de acceso comparten transacción. Antes de devolver el contenido se guarda EXPEDIENTE_CONSULTADO con Id, UsuarioId, SolicitudAtencionId, TipoAcceso y FechaUtc. Si no se puede confirmar la auditoría, no se devuelve el expediente.

Las consultas son AsNoTracking y proyectan ViewModels. La vista es solo lectura, usa codificación Razor y no admite notas ni edición. Las respuestas son no-store. No se escribieron logs de motivos, contactos, contraseñas ni tokens.

## Decisiones adicionales

- Edad máxima 120 como límite de validación de captura, además del mínimo solicitado de 18.
- Teléfonos de emergencia de ocho dígitos, coherentes con los formularios existentes de ZUNI.
- UsuarioReferenteId permanece null en este formulario: se captura referencia declarada, no se acepta un Id de cuenta ajena del cliente.
- Se agregó índice parcial de solicitudes activas como protección adicional a la consulta previa.
- No se asigna una cuenta cuyo rol institucional le impediría consultar el expediente.
- Las dos vistas reales del psicólogo usan el layout compartido del dashboard; no arrastran avisos/modales de la maqueta anterior.
- ApplicationDbContextFactory genera el contexto sin ejecutar Program ni SeedRolesAsync. Tras la revisión final se eliminó la conexión ficticia: ahora usa los mismos proveedores de configuración de ZUNI y exige un entorno explícito y la raíz correcta del proyecto. Consultar docs/revision-final-atencion.md antes de una futura actualización autorizada. No se añadió ningún secreto ni se cambió la conexión de ejecución normal.

## Verificación

Compilación final de la solución:
dotnet build Zuni/Zuni.sln -p:OutputPath=bin/atencion-review/

Resultado: **0 errores, 0 advertencias**.

La compilación normal inicial encontró Zuni.exe bloqueado por una instancia abierta (MSB3027/MSB3021, dos errores y diez avisos de reintento). No se detuvo tu aplicación; se usó una salida separada dentro de bin ignorado por Git. No eran errores de C#.

Se ejecutaron 33 comprobaciones offline en un pequeño programa externo al repositorio, sin paquetes nuevos ni datos persistidos:
- validación MVC real del ViewModel y contactos;
- casos válidos/inválidos de consentimiento, edad, referencia, enum y teléfonos;
- seis entidades, FK Restrict, ausencia de navegaciones clínicas inversas;
- índices parciales y fechas;
- operaciones Up/Down y nueve CHECK;
- snapshot idéntico al modelo;
- generación de SQL Npgsql sin ejecución;
- traducción SQL del listado sin MotivoConsulta;
- rechazo temprano de anónimo y Administrador sin Psicologo.

El programa de verificación está fuera de ZUNI, en el workspace de Codex: atencion-checks/AtencionChecks.csproj y atencion-checks/Program.cs. Los valores sintéticos existieron solo en memoria, no se creó seed ni se insertaron datos de prueba.

git diff --check no encontró errores de whitespace. Los avisos LF/CRLF de Git no son advertencias de compilación.

No se hicieron pruebas de integración con PostgreSQL ni verificación visual en navegador de las pantallas nuevas, porque la migración no se aplica en esta fase.

## Pruebas posteriores a autorizar/aplicar la migración

1. Estudiante: denegar envío sin consentimiento, menor de edad, referencia incompleta, cero contactos o teléfono inválido.
2. Envío válido: exactamente una solicitud, consentimiento, expediente y uno/tres contactos; sin copiar datos de PerfilEstudiante.
3. Doble envío concurrente: una sola solicitud activa.
4. Cero/uno/múltiples psicólogos activos: pendiente/asignada/pendiente.
5. Repetir servicio con asignación vigente: no crear otra; historial cerrado: requiere revisión.
6. Dos asignaciones concurrentes: una sola vigente; ninguna solicitud eliminada.
7. Psicólogo asignado: lista propia y expediente legible; un evento de acceso sin contenido clínico.
8. Otro psicólogo, Administrador, Director, Catedratico y combinaciones institucionales con Psicologo: acceso denegado, aun pegando URL.
9. Sesión desactivada, stamp cambiado o contraseña obligatoria: mantener los controles existentes.
10. Forzar fallo de auditoría en entorno de pruebas: no devolver expediente.
11. Revisar formularios en móvil/escritorio, teclado y mensajes de validación.

## Migración completa: Up y Down

Revisada: Up contiene seis CreateTable y nueve CreateIndex, sin AlterTable/AlterColumn/DropTable ni cambios de datos. No altera AspNetUsers, AspNetRoles, PerfilesEstudiante, PasswordResetTokens o AuditoriaUsuarios; solamente las nuevas FK hacen referencia a las tablas existentes.
Down elimina únicamente las seis tablas nuevas en orden compatible con las FK. Ejecutarlo después de utilizarlas eliminaría sus datos; no se ejecutó.
Los nombres terminados en ~ son el truncamiento de identificadores a 63 caracteres generado por Npgsql.

A continuación se incluye el archivo generado íntegro:

```csharp
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zuni.Migrations
{
    /// <inheritdoc />
    public partial class AddFlujoAtencionInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AsignacionesEstudiantePsicologo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PerfilEstudianteId = table.Column<Guid>(type: "uuid", nullable: false),
                    PsicologoUsuarioId = table.Column<string>(type: "text", nullable: false),
                    FechaAsignacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaFinalizacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsignacionesEstudiantePsicologo", x => x.Id);
                    table.CheckConstraint("CK_AsignacionesEstudiantePsicologo_Fechas", "\"FechaFinalizacionUtc\" IS NULL OR \"FechaFinalizacionUtc\" >= \"FechaAsignacionUtc\"");
                    table.ForeignKey(
                        name: "FK_AsignacionesEstudiantePsicologo_AspNetUsers_PsicologoUsuari~",
                        column: x => x.PsicologoUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AsignacionesEstudiantePsicologo_PerfilesEstudiante_PerfilEs~",
                        column: x => x.PerfilEstudianteId,
                        principalTable: "PerfilesEstudiante",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SolicitudesAtencion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PerfilEstudianteId = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaSolicitudUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    TipoIngreso = table.Column<int>(type: "integer", nullable: false),
                    UsuarioReferenteId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesAtencion", x => x.Id);
                    table.CheckConstraint("CK_SolicitudesAtencion_Estado", "\"Estado\" IN (0, 1, 2)");
                    table.CheckConstraint("CK_SolicitudesAtencion_TipoIngreso", "\"TipoIngreso\" IN (0, 1)");
                    table.ForeignKey(
                        name: "FK_SolicitudesAtencion_AspNetUsers_UsuarioReferenteId",
                        column: x => x.UsuarioReferenteId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesAtencion_PerfilesEstudiante_PerfilEstudianteId",
                        column: x => x.PerfilEstudianteId,
                        principalTable: "PerfilesEstudiante",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConsentimientosAtencion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SolicitudAtencionId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionConsentimiento = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Aceptado = table.Column<bool>(type: "boolean", nullable: false),
                    FechaRespuestaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsentimientosAtencion", x => x.Id);
                    table.CheckConstraint("CK_ConsentimientosAtencion_Aceptado", "\"Aceptado\" = TRUE");
                    table.ForeignKey(
                        name: "FK_ConsentimientosAtencion_SolicitudesAtencion_SolicitudAtenci~",
                        column: x => x.SolicitudAtencionId,
                        principalTable: "SolicitudesAtencion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventosAccesoClinico",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<string>(type: "text", nullable: false),
                    SolicitudAtencionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoAcceso = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosAccesoClinico", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventosAccesoClinico_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventosAccesoClinico_SolicitudesAtencion_SolicitudAtencionId",
                        column: x => x.SolicitudAtencionId,
                        principalTable: "SolicitudesAtencion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExpedientesIniciales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SolicitudAtencionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Edad = table.Column<int>(type: "integer", nullable: false),
                    EsMayorEdad = table.Column<bool>(type: "boolean", nullable: false),
                    Sexo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Direccion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    IdiomaPreferido = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    MotivoConsulta = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    NombreReferente = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    MotivoReferencia = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ConsideracionesAtencion = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: true),
                    FechaCreacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpedientesIniciales", x => x.Id);
                    table.CheckConstraint("CK_ExpedientesIniciales_MayorEdad", "\"Edad\" BETWEEN 18 AND 120 AND \"EsMayorEdad\" = TRUE");
                    table.CheckConstraint("CK_ExpedientesIniciales_Requeridos", "length(btrim(\"Direccion\")) > 0 AND length(btrim(\"IdiomaPreferido\")) > 0 AND length(btrim(\"MotivoConsulta\")) > 0");
                    table.ForeignKey(
                        name: "FK_ExpedientesIniciales_SolicitudesAtencion_SolicitudAtencionId",
                        column: x => x.SolicitudAtencionId,
                        principalTable: "SolicitudesAtencion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContactosEmergenciaExpediente",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpedienteInicialId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Relacion = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Telefono = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactosEmergenciaExpediente", x => x.Id);
                    table.CheckConstraint("CK_ContactosEmergenciaExpediente_NombreRelacion", "length(btrim(\"Nombre\")) > 0 AND length(btrim(\"Relacion\")) > 0");
                    table.CheckConstraint("CK_ContactosEmergenciaExpediente_Orden", "\"Orden\" BETWEEN 1 AND 3");
                    table.CheckConstraint("CK_ContactosEmergenciaExpediente_Telefono", "\"Telefono\" ~ '^[0-9]{8}$'");
                    table.ForeignKey(
                        name: "FK_ContactosEmergenciaExpediente_ExpedientesIniciales_Expedien~",
                        column: x => x.ExpedienteInicialId,
                        principalTable: "ExpedientesIniciales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesEstudiantePsicologo_PsicologoUsuarioId_FechaFin~",
                table: "AsignacionesEstudiantePsicologo",
                columns: new[] { "PsicologoUsuarioId", "FechaFinalizacionUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_AsignacionesEstudiantePsicologo_Vigente",
                table: "AsignacionesEstudiantePsicologo",
                column: "PerfilEstudianteId",
                unique: true,
                filter: "\"FechaFinalizacionUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ConsentimientosAtencion_SolicitudAtencionId",
                table: "ConsentimientosAtencion",
                column: "SolicitudAtencionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContactosEmergenciaExpediente_ExpedienteInicialId_Orden",
                table: "ContactosEmergenciaExpediente",
                columns: new[] { "ExpedienteInicialId", "Orden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventosAccesoClinico_SolicitudAtencionId_FechaUtc",
                table: "EventosAccesoClinico",
                columns: new[] { "SolicitudAtencionId", "FechaUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EventosAccesoClinico_UsuarioId_FechaUtc",
                table: "EventosAccesoClinico",
                columns: new[] { "UsuarioId", "FechaUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpedientesIniciales_SolicitudAtencionId",
                table: "ExpedientesIniciales",
                column: "SolicitudAtencionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesAtencion_UsuarioReferenteId",
                table: "SolicitudesAtencion",
                column: "UsuarioReferenteId");

            migrationBuilder.CreateIndex(
                name: "UX_SolicitudesAtencion_Activa",
                table: "SolicitudesAtencion",
                column: "PerfilEstudianteId",
                unique: true,
                filter: "\"Estado\" IN (0, 1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AsignacionesEstudiantePsicologo");

            migrationBuilder.DropTable(
                name: "ConsentimientosAtencion");

            migrationBuilder.DropTable(
                name: "ContactosEmergenciaExpediente");

            migrationBuilder.DropTable(
                name: "EventosAccesoClinico");

            migrationBuilder.DropTable(
                name: "ExpedientesIniciales");

            migrationBuilder.DropTable(
                name: "SolicitudesAtencion");
        }
    }
}
```
