# Cierre profesional de citas — revisión de Marvin

Rama: feature/final-integration.

## Resultado

Implementado sin ejecutar Update-Database ni conectar a PostgreSQL. Sin commit, push, merge o cherry-pick. No se modificó Big Five, Identity, asignaciones ni expedientes existentes.

**La migración está pendiente de revisión y aplicación por Marvin.** Las consultas de Agenda/Historial y Resultados ahora utilizan la tabla nueva; no desplegar esta versión sin coordinar la aplicación autorizada de la migración. No se ocultó la ausencia de esquema con datos ficticios.

## Archivos creados en esta tarea

Rutas relativas a C:/Users/USUARIO/Desktop/Seminario/ZUNI:

- Zuni/Models/Agenda/RegistroAtencionCita.cs
- Zuni/Models/Agenda/AtencionCitaViewModels.cs
- Zuni/Data/RegistroAtencionConfiguration.cs
- Zuni/Services/Agenda/AgendaCitasService.Atencion.cs
- Zuni/Views/Psicologo/RegistrarAtencion.cshtml
- Zuni/Views/Psicologo/VerAtencion.cshtml
- Zuni/wwwroot/css/atencion-cita.css
- Zuni/wwwroot/js/atencion-cita.js
- Zuni/Migrations/20261011013849_AddRegistroAtencionCita.cs
- Zuni/Migrations/20261011013849_AddRegistroAtencionCita.Designer.cs
- tests/Agenda.Offline/AtencionChecks.cs
- docs/RegistroAtencionCitaRevision.md (este informe)

## Archivos modificados en esta tarea

- Zuni/Data/ApplicationDbContext.cs: DbSet y llamada a configuración exclusiva del registro.
- Zuni/Migrations/ApplicationDbContextModelSnapshot.cs: actualizado por EF con la nueva entidad.
- Zuni/Models/Agenda/AgendaViewModels.cs: indicador de existencia del registro, sin contenido privado.
- Zuni/Services/Agenda/AgendaCitasService.cs: proyección del indicador y bloqueo del cierre por la vía antigua de GestionarCita.
- Zuni/Controllers/PsicologoController.cs: GET/POST RegistrarAtencion y GET VerAtencion.
- Zuni/Controllers/EstudianteController.cs: Resultados consulta el servicio autorizado.
- Zuni/Views/Psicologo/Agenda.cshtml: enlace Registrar atención sustituye Guardar asistencia.
- Zuni/Views/Psicologo/Historial.cshtml: enlace Ver atención cuando existe registro de cierre.
- Zuni/Views/Estudiante/Resultados.cshtml: resultados compartidos reales o estado vacío.
- tests/Agenda.Offline/Program.cs: fixtures clínicos, nueva ruta de cierre, verificación de migración/SQL y fallos transaccionales simulados.
- tests/Agenda.Offline/HttpSmoke.cs: pruebas HTTP de formulario, privacidad, antiforgery y solo lectura.

Había modificaciones previas sin commit en Agenda/Calendario/Cancelación. Se preservaron. No atribuir todo el git diff de la rama a esta tarea.

## Modelo y relación

RegistroAtencionCita contiene Id, CitaId, Asistio, ResultadoProfesionalPrivado (4000), ResenaRecomendacionesEstudiante (2000), ResultadoBreveEstudiante (2000), RecomiendaOtraCita, IndicacionesProximaCita (2000) y FechaCierreUtc (UTC).

Se agregó SolicitudAtencionId al nuevo registro para fijar la solicitud utilizada por autorización y auditoría. No se cambió Cita ni se agregaron textos clínicos a ella.

Cita 1 → 0..1 RegistroAtencionCita: FK Restrict y UNIQUE CitaId. FK adicional Restrict a SolicitudesAtencion. CHECK de coherencia asistencia/textos y recomendación/indicaciones. No hay navegación inversa desde usuarios o perfiles que precargue notas.

## Flujo del psicólogo

1. Agenda ofrece Registrar atención para Confirmada o Reservada legado, una vez terminado el horario.
2. El servidor vuelve a comprobar sesión real, titularidad y política clínica; también verifica estado y hora aunque se manipule el formulario.
3. Si asistió, exige los tres textos y limita sus longitudes. Indicaciones son obligatorias solo cuando recomienda otra cita.
4. Si no asistió, solo permite una reseña opcional; normaliza a null privado, resultado breve e indicaciones, y a false la recomendación.
5. Trim en textos persistidos. Espacios solos y longitudes excesivas se rechazan en servidor. La selección de asistencia es explícita.
6. Estado Completada/NoAsistio, registro y ATENCION_REGISTRADA se guardan juntos en SERIALIZABLE.
7. Ante fallo, rollback y limpieza del ChangeTracker. UNIQUE impide doble cierre, incluso ante concurrencia.
8. Una atención cerrada redirige al detalle de solo lectura. No existe edición del registro.
9. GestionarCita ya no permite Completada/NoAsistio; no puede eludirse este flujo por el endpoint anterior.

## Flujo del estudiante y separación de datos

/Estudiante/Resultados obtiene el perfil desde la sesión y PostgreSQL. No acepta un PerfilEstudianteId enviado por URL o formulario. Revalida cuenta, rol y SecurityStamp.

La consulta proyecta exclusivamente fecha/modalidad, resultado breve, reseña/recomendaciones, recomendación e indicaciones. **No selecciona ResultadoProfesionalPrivado en SQL**, ni lo incluye en el ViewModel o HTML.

Solo se listan atenciones con Asistio=true y Cita.Estado=Completada. La observación opcional de inasistencia queda en el detalle profesional, no se presenta como resultado de una atención completada.

Sin registros: “No tienes resultados de atención disponibles.”

RecomiendaOtraCita solo genera mensaje y enlace a /Estudiante/Citas. No reserva, confirma ni crea citas.

## Autorización y cambios de asignación

La autorización reutiliza AutorizacionClinicaService: autenticación, cuenta activa, SecurityStamp vigente, sin cambio obligatorio de contraseña, rol Psicologo real y exclusión de roles Administrador/Director/Catedratico, incluso combinados.

Además exige que la cita pertenezca al psicólogo autenticado **y** que la política clínica autorice la solicitud con consentimiento y asignación vigente.

Decisión conservadora: al reasignar al estudiante, el profesional anterior pierde acceso por no tener asignación vigente; el nuevo no obtiene acceso a notas históricas ajenas por no ser propietario de la cita. No se implementa transferencia automática de notas.

Como Cita no tiene SolicitudAtencionId, al cerrar se selecciona la última solicitud consentida del mismo perfil cuya FechaSolicitudUtc no sea posterior a FechaCreacionUtc de la cita. Se almacena el vínculo en el registro y se reutiliza en consultas posteriores. No se elige una solicitud futura para habilitar una cita antigua. Si no hay solicitud compatible/consentimiento/autorización, se deniega el cierre: no se crean expedientes ni consentimientos implícitos.

Los listados solo consultan la existencia del registro. La nota privada se carga únicamente en VerAtencion después de autorizar. Respuestas no-store y Razor codifica los textos; no se usa Html.Raw.

## Auditoría

EventoAccesoClinico conserva solo Id generado y metadatos UsuarioId, SolicitudAtencionId, FechaUtc y TipoAcceso.

- ATENCION_REGISTRADA en la misma transacción que estado y registro.
- ATENCION_CONSULTADA junto con autorización y lectura del detalle. Si no puede guardarse la auditoría, no se devuelve el contenido.

No se copian notas, recomendaciones, contraseñas ni tokens a eventos. No se generan diagnósticos ni tratamientos automáticos.

## Migración y compatibilidad

Nueva migración: 20261011013849_AddRegistroAtencionCita.

Up crea únicamente RegistrosAtencionCita e índices. No altera las tablas existentes. Se verificó mediante hashes que los 19 archivos previos de migraciones/Designer permanecieron intactos respecto al inicio de esta tarea.

Down generado por EF elimina únicamente la tabla nueva. **Si se ejecutase tras registrar atenciones, se perderían sus datos.** No se ejecutó Down ni Up; cualquier reversión posterior requerirá revisar respaldo y conservación clínica. Los eventos de auditoría existentes no se borran mediante este Down.

Citas antiguas completadas sin registro siguen visibles en el historial sin enlace de atención. No se inventa ni se rellena contenido retrospectivo. Cancelación, solicitudes, confirmación, rechazo y calendario conservan su flujo; solo el cierre Completada/NoAsistio requiere el nuevo formulario.

## Compilación y pruebas

- dotnet build Zuni/Zuni.sln -c Release --no-restore: correcto, **0 errores, 0 advertencias**.
- dotnet run --project tests/Agenda.Offline -c Release --no-restore: **275 comprobaciones correctas**.
- git diff --check: correcto.
- Scripts de avance y reversión generados con EF y configuración offline apuntando a un destino ficticio, **sin abrir conexión**. No se ejecutaron esos scripts.
- Snapshot comprobado contra el modelo EF: sincronizado.
- Pruebas en SQLite en memoria y host HTTP local efímero, no contra PostgreSQL real.

Cobertura añadida:
- Cierre autorizado, denegación a otro psicólogo y estudiante.
- Roles institucionales combinados, cuenta inactiva, stamp inválido y consentimiento ausente.
- Asistencia, inasistencia legado, indicaciones requeridas y normalización.
- Espacios, límites y selección obligatoria de asistencia.
- Cierre único a nivel servicio e índice UNIQUE.
- Citas futuras, Cancelada, Rechazada, Solicitada y finalizadas rechazadas.
- Rollback después de haber escrito los cambios dentro de la transacción: estado, registro y auditoría revertidos.
- Fallo de auditoría no devuelve detalle privado.
- Reasignación no transfiere acceso histórico.
- SQL, DTO y HTML del estudiante sin campo/contenido privado.
- URLs manipuladas no cambian al propietario.
- Razor, no-store, antiforgery, validación HTTP, solo lectura y codificación de texto.

Pendiente de validación autorizada después de revisar/aplicar la migración: ejecución real en PostgreSQL, concurrencia PostgreSQL y revisión visual manual en navegador. No se sustituyen esas verificaciones por pruebas SQLite.

## Up y Down completos

Código exacto de la nueva migración:

```csharp
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zuni.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistroAtencionCita : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegistrosAtencionCita",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CitaId = table.Column<Guid>(type: "uuid", nullable: false),
                    SolicitudAtencionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Asistio = table.Column<bool>(type: "boolean", nullable: false),
                    ResultadoProfesionalPrivado = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ResenaRecomendacionesEstudiante = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ResultadoBreveEstudiante = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RecomiendaOtraCita = table.Column<bool>(type: "boolean", nullable: false),
                    IndicacionesProximaCita = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    FechaCierreUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosAtencionCita", x => x.Id);
                    table.CheckConstraint("CK_RegistroAtencion_Asistencia", "(\"Asistio\" AND length(trim(coalesce(\"ResultadoProfesionalPrivado\", ''))) > 0 AND length(trim(coalesce(\"ResenaRecomendacionesEstudiante\", ''))) > 0 AND length(trim(coalesce(\"ResultadoBreveEstudiante\", ''))) > 0) OR (NOT \"Asistio\" AND \"ResultadoProfesionalPrivado\" IS NULL AND \"ResultadoBreveEstudiante\" IS NULL AND NOT \"RecomiendaOtraCita\")");
                    table.CheckConstraint("CK_RegistroAtencion_Recomendacion", "(\"RecomiendaOtraCita\" AND \"Asistio\" AND length(trim(coalesce(\"IndicacionesProximaCita\", ''))) > 0) OR (NOT \"RecomiendaOtraCita\" AND \"IndicacionesProximaCita\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_RegistrosAtencionCita_Citas_CitaId",
                        column: x => x.CitaId,
                        principalTable: "Citas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegistrosAtencionCita_SolicitudesAtencion_SolicitudAtencion~",
                        column: x => x.SolicitudAtencionId,
                        principalTable: "SolicitudesAtencion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAtencionCita_CitaId",
                table: "RegistrosAtencionCita",
                column: "CitaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAtencionCita_SolicitudAtencionId",
                table: "RegistrosAtencionCita",
                column: "SolicitudAtencionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegistrosAtencionCita");
        }
    }
}
```

