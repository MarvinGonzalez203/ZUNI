# Informe para Marvin — Agenda y Citas

Fecha de revisión: 10 de octubre de 2026.
Rama de trabajo: `feature/final-integration`.
Estado: cambios locales preparados para revisión; sin commit, push, merge ni actualización de PostgreSQL.

## 1. Base utilizada

HEAD inicial y final: `0493949d74d547f5687b439fb56c3e4f651d03ea`.
La rama estaba limpia y alineada con `origin/feature/final-integration` al comenzar, tras la actualización realizada por Kevin. Al reanudar se conservaron todos los cambios locales de esta integración. No se modificó el historial de Git.

## 2. Referencias de Marito

Referencia consultada: `origin/feature/student-psychologist-link`, commit `ce688f5795915e58e5022c564b70d42a8eca6c9e` (`modulos revisados`).

- `2fbfc913f917740d5ecd7efd07de0356c8b4baf2`: Limitar citas diarias y compactar seguimiento y gestion.
- `5924d708a862fcc6c1d624a696b7ac555750e223`: Conectar reservas de citas y registro de atencion profesional.
- `c5bd95c15cc047ec35099b7eaf88afd6907b3a6b`: Corregir alta de horarios y admitir duracion variable y ambas modalidades.

Se tomaron como referencia conceptual la disponibilidad, modalidades, reserva, gestión y presentación. No se hizo merge, cherry-pick ni copia de migraciones, controladores completos, Big Five o evaluaciones antiguas. La disponibilidad nueva es semanal, conforme al prompt, y no replica las dependencias del sistema anterior.

## 3. Funcionalidades integradas

- Disponibilidad semanal propia del psicólogo, persistida mediante EF/PostgreSQL, con activación y desactivación.
- Consulta de agenda por fecha y calendario mensual; citas exclusivas del profesional autenticado.
- Horarios libres del psicólogo asignado al estudiante y reserva con validación en servidor.
- Consulta paginada de reservas propias y cancelación futura del estudiante.
- Cancelación por psicólogo, registro de Completada y NoAsistio.
- Historial real por psicólogo, búsqueda por nombre, filtro por estado y páginas de 20 resultados.
- Formularios antiforgery, confirmación de cancelación, mensajes seguros, estados vacíos y estilos crema/lila del layout existente.

Estas funciones requieren que Marvin revise y aplique posteriormente la migración. No están habilitadas en la base actual por este trabajo.

## 4–5. Archivos

La lista exacta de archivos creados y modificados está al final del informe. No se eliminaron archivos. `bin/`, `obj/` y demás salidas de compilación no se incorporan al cambio. `Zuni/Zuni.csproj.user` ya está versionado en la rama base y permanece sin cambios; no se volvió a agregar. No se agregó `users.json` ni `.verification-build/`.

## 6–8. Modelos, enums y servicios

Modelos nuevos: `DisponibilidadPsicologo` y `Cita`. Se relacionan con usuarios y perfiles existentes; no duplican estudiantes, profesionales ni expedientes.

Enums:

- `ModalidadDisponibilidad`: Presencial, Virtual, Ambas.
- `ModalidadCita`: Presencial, Virtual.
- `EstadoCita`: Reservada, Cancelada, Completada, NoAsistio.

Servicio nuevo: `AgendaCitasService`. Usa `IAutorizacionClinicaService` existente para validar al psicólogo. `AgendaReglas` concentra validaciones y conversión horaria; `AgendaExceptionFilter` convierte denegaciones y errores operativos en respuestas sin datos internos. ViewModels específicos limitan los campos aceptados. `Program.cs` solo incorpora el servicio y `TimeProvider.System`; el contexto solo incorpora dos DbSet y la configuración nueva.

## 9. EstudianteController

Se reemplazó únicamente el placeholder de Citas por lectura real y se añadieron POST de reserva y cancelación. La identidad del estudiante y del psicólogo nunca procede de campos del formulario. No se alteraron SolicitarAtencion, EstadoSolicitud, perfil, Big Five ni la lógica de asignación existente.

## 10. PsicologoController

Agenda e Historial consumen el servicio nuevo. Se añadieron POST para agregar/activar/desactivar disponibilidad y gestionar estados de citas. Estudiantes y Expediente conservan la autorización y auditoría previas, incluida la transacción del expediente. Atencion sigue dirigiendo a estudiantes asignados y al expediente oficial, sin nueva ficha ni notas.

## 11–13. Agenda, Citas e Historial

Agenda muestra disponibilidad semanal propia y citas del día elegido. La consulta admite el último año y los próximos 90 días; el historial permite consultar registros anteriores. Para reemplazar un horario se desactiva y se crea otro, preservando las citas y su referencia al horario original.

Citas requiere cuenta/perfil activos. Ofrece horarios únicamente cuando hay perfil completo y asignación vigente a un psicólogo elegible. Cada bloque tiene la duración configurada; el sobrante de una ventana no forma un bloque incompleto. Las reservas propias siguen visibles aunque cambie o termine la asignación. El estudiante puede cancelar una cita propia futura de una asignación anterior.

Historial contiene únicamente registros Cancelada, Completada o NoAsistio del profesional autenticado. Los accesos a expedientes no se convierten en sesiones. No se muestran notas clínicas ni puntuaciones psicológicas.

## 14. Reglas de disponibilidad

Día de semana válido, horas completas al minuto, fin posterior al inicio y sin cruce de medianoche. Duración de 15 a 120 minutos, múltiplo de 5, que quepa al menos una vez en la ventana. Modalidad válida. No se permite superposición entre ventanas activas del mismo profesional. La desactivación se bloquea mientras haya citas Reservada cuyo horario aún no finalizó. Reactivar también comprueba superposición.

## 15. Reglas de reserva y gestión

Horario futuro, dentro de los próximos 90 días, alineado a la cuadrícula de la ventana activa y modalidad permitida. La duración y los IDs se obtienen del servidor. Se revisa que no haya cruce con otra reserva del psicólogo o del mismo estudiante, incluso si cambió su asignación.

Solo Reservada puede pasar a un estado final. Cancelar exige que la cita todavía no inicie. Completada y NoAsistio solo se registran después del fin. No se permite repetir ni revertir estados finales. El motivo de cancelación es opcional, administrativo y limitado a 300 caracteres.

## 16. Doble reserva y concurrencia

Todas las escrituras del servicio se ejecutan en una transacción SERIALIZABLE, incluyendo la revalidación de identidad, asignación, disponibilidad, cruces y estado. Los predicados de conflicto cubren tanto al psicólogo como al estudiante. Hay además dos índices únicos filtrados por Estado = Reservada para impedir inicios duplicados por profesional o estudiante.

Los conflictos PostgreSQL 40001 (serialización), 40P01 (deadlock) y 23505 (unicidad) provocan rollback y una respuesta de conflicto que pide revisar el estado actual, sin reintento automático. Los índices por sí solos no impiden intervalos parcialmente superpuestos: esa protección depende del predicado y del aislamiento SERIALIZABLE. Cualquier futuro escritor de citas debe respetar esta misma disciplina.

Se verificaron aislamiento solicitado, metadatos y comportamiento secuencial en pruebas offline. No se afirma haber probado carreras reales en PostgreSQL: queda pendiente esa validación en una base descartable autorizada.

## 17–19. Autorización y roles combinados

Estudiante: sesión autenticada, rol Estudiante en claims y en BD, identificador, SecurityStamp vigente, cuenta activa, sin cambio obligatorio pendiente y perfil activo. Para reservar, también perfil completo y asignación vigente. Se deniega la combinación con Administrador, Director o Catedratico. Las operaciones propias filtran por PerfilEstudianteId derivado de la sesión.

Psicólogo: se reutiliza `ObtenerPsicologoAutorizadoAsync`, sin modificarlo. Comprueba sesión, rol, cuenta activa, cambio obligatorio, SecurityStamp y roles persistidos. Administrador + Psicologo, Director + Psicologo y Catedratico + Psicologo quedan denegados aunque se omita el rol institucional de los claims. Cada escritura comprueba nuevamente propiedad de la cita o disponibilidad.

Todos los POST nuevos tienen antiforgery y validación de datos. Se conservan las políticas de no-cache de los controladores. IDs inexistentes y ajenos reciben la misma denegación.

## UTC

Citas y creación se persisten como DateTime UTC / PostgreSQL `timestamp with time zone`. Las ventanas semanales usan DayOfWeek y TimeOnly en `America/Guatemala`. La interfaz indica Guatemala (UTC−6). El formulario envía el inicio ISO 8601 con zona; se convierte a UTC y se vuelve a validar en el servidor. No se utiliza la zona arbitraria del navegador ni del equipo para definir horarios.

## 20. Migración

`20261010233852_AddAgendaYCitas`, generada desde el snapshot actual posterior a `20261010055626_AddBigFiveApoyo`, mediante la fábrica de contexto y un entorno de diseño sin credenciales reales ni conexión abierta.

Solo crea `DisponibilidadesPsicologo`, `Citas`, sus índices y FK Restrict hacia tablas existentes y entre ambas tablas. No altera ni elimina columnas/tablas anteriores. El snapshot tiene 135 líneas añadidas y ninguna eliminada. Up y Down completos se incluyen al final.

## 23–25. Preservación y base de datos

Big Five, sus modelos/servicios/recursos/controladores/migraciones y auditoría `BIGFIVE_CONSULTADO` no fueron reemplazados. El flujo clínico oficial y sus servicios tampoco. MiCuenta, CuentaController, ApplicationUser, PerfilEstudiante, DbInitializer, Administrador, Director y Catedratico no se modificaron. No se agregaron resultados o expedientes a roles institucionales.

No se ejecutó Update-Database, no se aplicó SQL y no se modificó PostgreSQL. No se abrió la aplicación real durante las pruebas para evitar su inicialización de datos. No se completó el backup: el acceso a la configuración privada de conexión no estuvo disponible; se continuó con código según lo autorizado por Kevin. El respaldo debe verificarse antes de cualquier aplicación posterior autorizada.

## 26–28. Verificación, errores y advertencias

- Compilación final de Zuni y Agenda.Offline: 0 errores, 0 advertencias.
- Agenda.Offline: 91 comprobaciones aprobadas, incluyendo pruebas HTTP de vistas/layouts reales, protección antiforgery, no-store, identidad manipulada y denegación de roles combinados.
- BigFive.Offline: 77 comprobaciones del código existente aprobadas. Se compiló sin modificar su fuente mediante un ejecutor temporal con SQLite 8.0.23, ya instalado en el equipo. Compilación de ese ejecutor: 0 errores, 0 advertencias.
- Comprobación de migración: solo tablas nuevas, FK Restrict, índices únicos filtrados y snapshot sincronizado.
- Consulta de historial traducida a SQL Npgsql sin abrir conexión.
- No se agregaron dependencias a Zuni.csproj. El nuevo proyecto de pruebas utiliza SQLite 8.0.23 disponible localmente.

Incidencias de preparación resueltas: la configuración NuGet personal no era accesible, así que la restauración se hizo con configuración aislada y caché local; la referencia inicial de pruebas a SQLite 8.0.0 se fijó a la versión 8.0.23 instalada para eliminar NU1603. Las llaves del host de pruebas quedaron exclusivamente en memoria para no leer el almacén privado del equipo. La configuración real de protección de datos no fue alterada.

La revisión detectó que `_PsicologoLayout` no propagaba la sección Styles de las vistas; se añadió esa propagación y se comprobó mediante HTTP. No quedan errores de compilación o de estas pruebas.

## 29. Pendientes para revisión e integración

- Revisión de Marvin y respaldo verificado antes de aplicar la migración. No se realizó commit ni push.
- Aplicación autorizada de la migración en entorno adecuado y pruebas funcionales con PostgreSQL, especialmente concurrencia, rollback y reservas simultáneas.
- Prueba visual manual en navegador y móvil con datos autorizados tras habilitar el esquema. Las vistas se renderizaron mediante HTTP offline, pero no se hizo una prueba visual completa contra la aplicación real.
- Definir si se requieren excepciones por fecha, feriados, límite diario, plazo mínimo de reserva/cancelación, reprogramación, recordatorios o enlaces de videollamada. No se inventaron estas políticas.
- Notas de sesión, seguimiento clínico adicional y evaluaciones genéricas de Marito siguen fuera de alcance. No se duplicó el expediente ni se importaron datos, cuentas o resultados de demostración a PostgreSQL.

## Archivos creados

- `Zuni/Data/AgendaCitasConfiguration.cs`
- `Zuni/Migrations/20261010233852_AddAgendaYCitas.Designer.cs`
- `Zuni/Migrations/20261010233852_AddAgendaYCitas.cs`
- `Zuni/Models/Agenda/AgendaViewModels.cs`
- `Zuni/Models/Agenda/Cita.cs`
- `Zuni/Models/Agenda/DisponibilidadPsicologo.cs`
- `Zuni/Services/Agenda/AgendaCitasService.cs`
- `Zuni/Services/Agenda/AgendaExceptionFilter.cs`
- `Zuni/Services/Agenda/AgendaReglas.cs`
- `Zuni/Views/Shared/AgendaError.cshtml`
- `Zuni/Views/Shared/_AgendaFecha.cshtml`
- `Zuni/wwwroot/css/agenda-citas.css`
- `docs/agenda-citas-integracion.md`
- `tests/Agenda.Offline/Agenda.Offline.csproj`
- `tests/Agenda.Offline/HttpSmoke.cs`
- `tests/Agenda.Offline/Program.cs`
- `tests/Agenda.Offline/README.md`

## Archivos modificados

- `Zuni/Controllers/EstudianteController.cs`
- `Zuni/Controllers/PsicologoController.cs`
- `Zuni/Data/ApplicationDbContext.cs`
- `Zuni/Migrations/ApplicationDbContextModelSnapshot.cs`
- `Zuni/Program.cs`
- `Zuni/Views/Estudiante/Citas.cshtml`
- `Zuni/Views/Estudiante/_Navegacion.cshtml`
- `Zuni/Views/Psicologo/Agenda.cshtml`
- `Zuni/Views/Psicologo/Historial.cshtml`
- `Zuni/Views/Psicologo/_PsicologoLayout.cshtml`

## 21–22. Up y Down completos

```csharp
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zuni.Migrations
{
    /// <inheritdoc />
    public partial class AddAgendaYCitas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DisponibilidadesPsicologo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PsicologoUsuarioId = table.Column<string>(type: "text", nullable: false),
                    DiaSemana = table.Column<int>(type: "integer", nullable: false),
                    HoraInicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    HoraFin = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    DuracionMinutos = table.Column<int>(type: "integer", nullable: false),
                    Modalidad = table.Column<int>(type: "integer", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisponibilidadesPsicologo", x => x.Id);
                    table.CheckConstraint("CK_Disponibilidad_Dia", "\"DiaSemana\" BETWEEN 0 AND 6");
                    table.CheckConstraint("CK_Disponibilidad_Duracion", "\"DuracionMinutos\" BETWEEN 15 AND 120 AND \"DuracionMinutos\" % 5 = 0");
                    table.CheckConstraint("CK_Disponibilidad_Horas", "\"HoraFin\" > \"HoraInicio\"");
                    table.CheckConstraint("CK_Disponibilidad_Modalidad", "\"Modalidad\" IN (0, 1, 2)");
                    table.ForeignKey(
                        name: "FK_DisponibilidadesPsicologo_AspNetUsers_PsicologoUsuarioId",
                        column: x => x.PsicologoUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Citas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PerfilEstudianteId = table.Column<Guid>(type: "uuid", nullable: false),
                    PsicologoUsuarioId = table.Column<string>(type: "text", nullable: false),
                    DisponibilidadPsicologoId = table.Column<Guid>(type: "uuid", nullable: false),
                    FechaHoraInicioUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaHoraFinUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Modalidad = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    FechaCreacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MotivoCancelacion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Citas", x => x.Id);
                    table.CheckConstraint("CK_Citas_Estado", "\"Estado\" IN (0, 1, 2, 3)");
                    table.CheckConstraint("CK_Citas_Fechas", "\"FechaHoraFinUtc\" > \"FechaHoraInicioUtc\"");
                    table.CheckConstraint("CK_Citas_Modalidad", "\"Modalidad\" IN (0, 1)");
                    table.ForeignKey(
                        name: "FK_Citas_AspNetUsers_PsicologoUsuarioId",
                        column: x => x.PsicologoUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Citas_DisponibilidadesPsicologo_DisponibilidadPsicologoId",
                        column: x => x.DisponibilidadPsicologoId,
                        principalTable: "DisponibilidadesPsicologo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Citas_PerfilesEstudiante_PerfilEstudianteId",
                        column: x => x.PerfilEstudianteId,
                        principalTable: "PerfilesEstudiante",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Citas_DisponibilidadPsicologoId",
                table: "Citas",
                column: "DisponibilidadPsicologoId");

            migrationBuilder.CreateIndex(
                name: "IX_Citas_PsicologoUsuarioId_Estado_FechaHoraInicioUtc",
                table: "Citas",
                columns: new[] { "PsicologoUsuarioId", "Estado", "FechaHoraInicioUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_Citas_Estudiante_InicioReservado",
                table: "Citas",
                columns: new[] { "PerfilEstudianteId", "FechaHoraInicioUtc" },
                unique: true,
                filter: "\"Estado\" = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Citas_Psicologo_InicioReservado",
                table: "Citas",
                columns: new[] { "PsicologoUsuarioId", "FechaHoraInicioUtc" },
                unique: true,
                filter: "\"Estado\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_DisponibilidadesPsicologo_PsicologoUsuarioId_DiaSemana_Acti~",
                table: "DisponibilidadesPsicologo",
                columns: new[] { "PsicologoUsuarioId", "DiaSemana", "Activo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Citas");

            migrationBuilder.DropTable(
                name: "DisponibilidadesPsicologo");
        }
    }
}
```
