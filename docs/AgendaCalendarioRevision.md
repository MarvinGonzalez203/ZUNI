# Revisión de Agenda / Citas — ZUNI

## Estado inicial y alcance

- Rama verificada: `feature/final-integration`.
- HEAD inicial y final: `97b5d242cf58ee8a4b85f10062f92f50f77acdf7`.
- Árbol inicialmente limpio; cambios finales sin commit y preparados para revisión.
- Se confirmó la integración de Kevin: DisponibilidadPsicologo, Cita, AgendaCitasService, AgendaReglas, AgendaExceptionFilter y AddAgendaYCitas, junto con Big Five y el flujo clínico.
- Marito se consultó mediante `git show`, únicamente como referencia de calendario, modales, CSS y lista de citas. No se copió su backend ni su dependencia de Big Five.
- No se ejecutó la aplicación productiva, no se abrió PostgreSQL, no hubo Update-Database, merge, cherry-pick, rebase, reset, commit ni push.
- No se agregaron paquetes al proyecto. La restauración de los runners usó los paquetes ya disponibles en caché local.

## Archivos creados

| Archivo | Cambio |
|---|---|
| [Zuni/Models/Agenda/ConfiguracionAgendaDia.cs](C:/Users/USUARIO/Desktop/Seminario/ZUNI/Zuni/Models/Agenda/ConfiguracionAgendaDia.cs) | Excepción por psicólogo y fecha: bloqueo y selección de base semanal. |
| [Zuni/Services/Agenda/AgendaCitasService.Calendario.cs](C:/Users/USUARIO/Desktop/Seminario/ZUNI/Zuni/Services/Agenda/AgendaCitasService.Calendario.cs) | Parte del MISMO servicio: calendario mensual, intervalos diarios y bloqueos transaccionales. |
| [Zuni/Views/Shared/_AgendaCalendario.cshtml](C:/Users/USUARIO/Desktop/Seminario/ZUNI/Zuni/Views/Shared/_AgendaCalendario.cshtml) | Calendario compartido y modales Bootstrap diarios con formularios antiforgery. |
| [Zuni/Migrations/20261011004803_MejorarAgendaCalendario.cs](C:/Users/USUARIO/Desktop/Seminario/ZUNI/Zuni/Migrations/20261011004803_MejorarAgendaCalendario.cs) | Nueva migración pendiente de aplicar; Up aditivo y Down bloqueado. |
| [Zuni/Migrations/20261011004803_MejorarAgendaCalendario.Designer.cs](C:/Users/USUARIO/Desktop/Seminario/ZUNI/Zuni/Migrations/20261011004803_MejorarAgendaCalendario.Designer.cs) | Metadatos generados por EF para la nueva migración. |
| [tests/Agenda.Offline/CalendarioChecks.cs](C:/Users/USUARIO/Desktop/Seminario/ZUNI/tests/Agenda.Offline/CalendarioChecks.cs) | Pruebas de calendario, reglas diarias, compatibilidad y privacidad. |
| [docs/AgendaCalendarioRevision.md](C:/Users/USUARIO/Desktop/Seminario/ZUNI/docs/AgendaCalendarioRevision.md) | Este informe y código completo de la migración. |

## Archivos modificados

| Archivo | Cambio |
|---|---|
| [Zuni/Models/Agenda/DisponibilidadPsicologo.cs](C:/Users/USUARIO/Desktop/Seminario/ZUNI/Zuni/Models/Agenda/DisponibilidadPsicologo.cs) | FK diaria nullable y valores explícitos de EstadoCita. |
| [Zuni/Models/Agenda/AgendaViewModels.cs](C:/Users/USUARIO/Desktop/Seminario/ZUNI/Zuni/Models/Agenda/AgendaViewModels.cs) | DTOs de calendario, día, franja sin identidades y entrada de horario diario. |
| [Zuni/Data/ApplicationDbContext.cs](C:/Users/USUARIO/Desktop/Seminario/ZUNI/Zuni/Data/ApplicationDbContext.cs) | DbSet ConfiguracionesAgendaDia. |
| [Zuni/Data/AgendaCitasConfiguration.cs](C:/Users/USUARIO/Desktop/Seminario/ZUNI/Zuni/Data/AgendaCitasConfiguration.cs) | Nueva entidad, FK Restrict, duración variable y checks/índices de estados activos. |
| [Zuni/Migrations/ApplicationDbContextModelSnapshot.cs](C:/Users/USUARIO/Desktop/Seminario/ZUNI/Zuni/Migrations/ApplicationDbContextModelSnapshot.cs) | Snapshot actualizado por EF; diferencias limitadas a Agenda/Citas. |
| [Zuni/Services/Agenda/AgendaCitasService.cs](C:/Users/USUARIO/Desktop/Seminario/ZUNI/Zuni/Services/Agenda/AgendaCitasService.cs) | Solicitudes, límite diario, autorización de aceptación/rechazo, consultas mensuales e historial. |
| [Zuni/Services/Agenda/AgendaReglas.cs](C:/Users/USUARIO/Desktop/Seminario/ZUNI/Zuni/Services/Agenda/AgendaReglas.cs) | 12 meses, horas/múltiplos, duración variable, cuadrícula y transiciones. |
| [Zuni/Controllers/PsicologoController.cs](C:/Users/USUARIO/Desktop/Seminario/ZUNI/Zuni/Controllers/PsicologoController.cs) | POST AgregarHorarioDia y CambiarDia; resto del flujo clínico conservado. |
| [Zuni/Controllers/EstudianteController.cs](C:/Users/USUARIO/Desktop/Seminario/ZUNI/Zuni/Controllers/EstudianteController.cs) | Únicamente mensaje de éxito: solicitud pendiente de confirmación. |
| [Zuni/Views/Psicologo/Agenda.cshtml](C:/Users/USUARIO/Desktop/Seminario/ZUNI/Zuni/Views/Psicologo/Agenda.cshtml) | Calendario, base semanal preservada y gestión de solicitudes/asistencia. |
| [Zuni/Views/Estudiante/Citas.cshtml](C:/Users/USUARIO/Desktop/Seminario/ZUNI/Zuni/Views/Estudiante/Citas.cshtml) | Calendario del asignado y estados activos/cancelación propios. |
| [Zuni/Views/Psicologo/Historial.cshtml](C:/Users/USUARIO/Desktop/Seminario/ZUNI/Zuni/Views/Psicologo/Historial.cshtml) | Filtro y descripción incluyen Rechazada. |
| [Zuni/wwwroot/css/agenda-citas.css](C:/Users/USUARIO/Desktop/Seminario/ZUNI/Zuni/wwwroot/css/agenda-citas.css) | Estados semánticos, calendario, modales y adaptación móvil. |
| [tests/Agenda.Offline/Program.cs](C:/Users/USUARIO/Desktop/Seminario/ZUNI/tests/Agenda.Offline/Program.cs) | Regresión, estados, límite diario y metadatos de nueva migración. |
| [tests/Agenda.Offline/HttpSmoke.cs](C:/Users/USUARIO/Desktop/Seminario/ZUNI/tests/Agenda.Offline/HttpSmoke.cs) | Flujo HTTP de solicitudes, Razor/modales, IDs y formularios. |

No se eliminaron archivos. No se modificaron Program.cs, CuentaController, MiCuenta, Big Five, autorización clínica, entidades de atención, roles ni las migraciones anteriores.

## Modelo y conservación de datos

La disponibilidad semanal sigue en DisponibilidadesPsicologo. La nueva FK nullable ConfiguracionAgendaDiaId distingue:

- NULL: regla semanal de Kevin, intacta.
- Con valor: intervalo exclusivo de una fecha.

ConfiguracionesAgendaDia contiene Id, PsicologoUsuarioId, Fecha, Bloqueado y UsaBaseSemanal. Hay un índice UNIQUE (PsicologoUsuarioId, Fecha).

Sin excepción diaria se usa la base semanal. Bloquear/habilitar no copia intervalos. Solo al añadir/quitar intervalos de una fecha se materializan sus reglas semanales, una vez, para no alterar las demás fechas. Las copias y originales conservan sus identificadores; las citas previas siguen apuntando a su disponibilidad original. No se duplican por adelantado los horarios de 12 meses.

“Quitar configuración del día” establece una excepción vacía para esa fecha y desactiva sus intervalos diarios, sin borrar filas ni citas. Así la base semanal no reaparece inmediatamente. “Habilitar día” conserva/restaura los intervalos de un día bloqueado; no recrea horarios que se quitaron explícitamente. Se puede volver a añadir horarios a una excepción vacía.

Las FK nuevas tienen DeleteBehavior.Restrict; no hay borrado en cascada. Las citas conservan su FK obligatoria a DisponibilidadPsicologo.

Los horarios semanales ya almacenados conservan sus duraciones históricas (15–120 minutos) y horas; no hay normalización masiva. Las nuevas entradas se validan en servidor. El formulario diario ofrece exclusivamente 15, 30, 50 o 0 (variable).

## Experiencia del psicólogo

Calendario mensual lunes–domingo, mes actual inicial, navegación de meses hasta 12 meses, límites diarios inclusivos desde hoy hasta hoy.AddMonths(12), calculados en Guatemala. Los días pasados están deshabilitados y etiquetados “Pasado”; los posteriores al límite, “Fuera de plazo”.

Estados con texto y color:

| Estado | Criterio | Fondo |
|---|---|---|
| Disponible | Al menos una franja completa futura libre | #e3f4ea |
| Ocupado | Bloqueado o configuración sin franjas futuras libres | #fce6e6 |
| Sin horarios | No hay intervalos efectivos para la fecha | #f3f5f7 |
| Pasado | Fecha anterior a hoy, sin edición | Gris |

El modal incluye fecha, “Hora de Guatemala (UTC−6)”, horarios ordenados, modalidad, duración, estado, quitar horario, formulario de alta, bloqueo/habilitación, quitar configuración, X y Cerrar. Usa Bootstrap ya instalado, scroll y etiquetas/focus accesibles; no necesita un nuevo JavaScript.

Valores iniciales: 08:00–12:00, 50 minutos, Presencial. Admite varios intervalos sin superposición. El servidor comprueba 08:00–18:00, múltiplos de cinco incluso rechazando segundos/fracciones, fin posterior, duración permitida y que exista una franja futura completa.

La base semanal permanece accesible en una sección desplegable. La gestión muestra citas del mes y activas anteriores, para que las citas legadas no se vuelvan inaccesibles al pasar el mes.

Bloquear, quitar configuración o quitar un intervalo comprueba las citas activas dentro de SERIALIZABLE. Un horario ocupado no se modifica; los controles deshabilitados son solo ayuda visual, no la protección de seguridad.

## Duración variable

DuracionMinutos = 0 significa **sin duración fija**: el intervalo solo define el rango reservable. Cada cita elige su inicio (cada 15 minutos desde el inicio del intervalo) y su duración (15 a 120 minutos, múltiplos de 15); su fin es inicio + duración y el resto del intervalo sigue reservable. (Actualizado en `fix/registro-atencion-por-cita`; antes la cita ocupaba el intervalo completo y su fin era el fin del intervalo. Ver docs/registro-atencion-por-cita.md.)

## Estudiante y privacidad

El calendario se obtiene únicamente del psicólogo de la asignación vigente del estudiante, validada en servidor. No se reciben PsicologoUsuarioId ni PerfilEstudianteId del formulario.

Se mantienen las condiciones actuales de perfil completo y cuenta/perfil activos. Sin asignación disponible se presenta el estado vacío existente; las citas propias siguen consultables.

Las franjas ocupadas muestran tiempo, modalidad y “Reservado / ocupado”. El DTO compartido contiene únicamente InicioUtc, FinUtc, Modalidad, Ocupada y Variable: nunca nombres, correos, carnés, expedientes ni IDs de otros estudiantes.

“Ambas” permite seleccionar Presencial o Virtual; el servidor verifica la elección contra la disponibilidad efectiva. Si es Presencial o Virtual exclusivamente, no acepta la otra modalidad aunque se manipule el formulario.

## Estados y transiciones

| Valor | Estado | Reserva espacio |
|---|---|---|
| 0 | Reservada (legado) | Sí |
| 1 | Cancelada | No |
| 2 | Completada | No |
| 3 | NoAsistio | No |
| 4 | Solicitada | Sí |
| 5 | Confirmada | Sí |
| 6 | Rechazada | No |

Los valores 0–3 no fueron renumerados ni convertidos en datos existentes.

- Nueva solicitud → Solicitada.
- Solicitada → Confirmada antes de su inicio, o Rechazada.
- Solicitada / Confirmada / Reservada → Cancelada antes del inicio.
- Confirmada / Reservada → Completada / NoAsistio después del fin.
- Los estados finales no pueden reactivarse.
- Una solicitud vencida puede rechazarse; no se confirma retrospectivamente ni se registra como atendida sin confirmación.

Aceptar/rechazar usa POST antiforgery y comprueba de nuevo psicólogo autorizado, propiedad, asignación vigente y cuenta/perfil/rol real del estudiante. No permite gestionar una solicitud ajena manipulando el ID.

Reservada funciona como confirmada legada para bloqueo y asistencia. Se conserva la posibilidad de cancelar una cita propia futura aunque haya cambiado la asignación, tal como permitía Kevin.

Una cita activa por estudiante por fecha local: el servidor consulta el día de Guatemala completo, independientemente del psicólogo actual. Tras cancelar/rechazar puede solicitarse otra.

## Historial

Incluye solo Cancelada, Completada, NoAsistio y Rechazada; excluye Reservada, Solicitada y Confirmada. Conserva búsqueda, filtro, paginación y alcance al psicólogo propietario.

## Concurrencia y seguridad

Todas las escrituras continúan en transacciones SERIALIZABLE. Se leen dentro de la transacción la autorización, configuración, límite diario y solapamientos por psicólogo o estudiante antes de guardar.

Los dos índices únicos por inicio cubren ahora Estado IN (0,4,5). Son defensa adicional: no reemplazan los predicados de solapamiento o de día completo. Ante 40001, deadlock o unicidad se hace rollback, se limpia el tracking y se devuelve un mensaje seguro, sin reintento automático. Las demás excepciones también revierten y limpian el tracking.

Se conserva IAutorizacionClinicaService, SecurityStamp, IsActive, cambio obligatorio resuelto, roles reales y exclusión de Administrador/Director/Catedratico. Cada operación deriva propietario desde sesión/BD. Los POST diarios también tienen ValidateAntiForgeryToken.

Las citas se guardan en UTC. Calendario, rangos diarios y presentación usan America/Guatemala desde servidor, no la zona del navegador.

No se añadieron logs sensibles ni eventos de auditoría paralelos. No se modificó la auditoría clínica existente.

## Big Five

Sigue siendo opcional. Agenda/Citas no consulta ParticipacionesBigFive ni llama a BigFiveService. Las pruebas crean y gestionan citas sin ninguna participación Big Five.

## Reprogramación pendiente

No se implementó en esta fase, tal como permite el requisito 26. No se añadió un botón ni un flujo de “cancelar y volver a reservar” presentado como reprogramación: eso podría liberar la cita original antes de asegurar la nueva.

Para una fase posterior debe implementarse como una única operación transaccional, con autorización/propiedad, asignación vigente, validación completa de la nueva franja, exclusión controlada de la cita original en las comprobaciones y protección frente a formularios obsoletos. Un fallo debe conservar íntegra la cita original.

## Migración pendiente, NO aplicada

Nombre exacto: **20261011004803_MejorarAgendaCalendario**.

Up agrega:
- ConfiguracionesAgendaDia.
- DisponibilidadesPsicologo.ConfiguracionAgendaDiaId, nullable.
- FK Restrict al usuario y a la configuración diaria.
- Índice único psicólogo/fecha e índice de la FK diaria.
- Sustitución de los dos índices existentes por su versión filtrada IN (0,4,5).
- Ampliación del CHECK de estados a 0..6 y del CHECK de duración para permitir 0 solo en intervalos diarios.

Up no elimina tablas, columnas, citas ni disponibilidades, ni ejecuta transformaciones de datos.

**Down está deliberadamente bloqueado mediante RAISE EXCEPTION**: el código inverso generado por EF eliminaba la tabla/columna nuevas. No se dejó esa reversión destructiva automática ni una conversión de estados 4..6 a estados antiguos. Cualquier reversión futura requiere una estrategia revisada de conservación. No se ejecutó Up ni Down.

AddAgendaYCitas y su Designer siguen byte por byte iguales:
- 20261010233852_AddAgendaYCitas.cs: SHA256 C3002475DF4D8468EAD92723C2D816816D941F6CFCC03512407C16E70A285B0E.
- 20261010233852_AddAgendaYCitas.Designer.cs: SHA256 A278E119FAD4B21292EBB41C1BDB7383B6A43FA850CF23EA2835D6A9FAD0591B.

La nueva migración se generó desde el snapshot actual usando la fábrica de diseño en entorno Offline y una conexión ficticia no operativa, sin ejecutar Program ni seeds ni abrir la conexión.

### Código completo: Up y Down

```csharp
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zuni.Migrations
{
    /// <inheritdoc />
    public partial class MejorarAgendaCalendario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Disponibilidad_Duracion",
                table: "DisponibilidadesPsicologo");

            migrationBuilder.DropIndex(
                name: "UX_Citas_Estudiante_InicioReservado",
                table: "Citas");

            migrationBuilder.DropIndex(
                name: "UX_Citas_Psicologo_InicioReservado",
                table: "Citas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Citas_Estado",
                table: "Citas");

            migrationBuilder.AddColumn<Guid>(
                name: "ConfiguracionAgendaDiaId",
                table: "DisponibilidadesPsicologo",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ConfiguracionesAgendaDia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PsicologoUsuarioId = table.Column<string>(type: "text", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Bloqueado = table.Column<bool>(type: "boolean", nullable: false),
                    UsaBaseSemanal = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionesAgendaDia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfiguracionesAgendaDia_AspNetUsers_PsicologoUsuarioId",
                        column: x => x.PsicologoUsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DisponibilidadesPsicologo_ConfiguracionAgendaDiaId",
                table: "DisponibilidadesPsicologo",
                column: "ConfiguracionAgendaDiaId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Disponibilidad_Duracion",
                table: "DisponibilidadesPsicologo",
                sql: "(\"DuracionMinutos\" = 0 AND \"ConfiguracionAgendaDiaId\" IS NOT NULL) OR (\"DuracionMinutos\" BETWEEN 15 AND 120 AND \"DuracionMinutos\" % 5 = 0)");

            migrationBuilder.CreateIndex(
                name: "UX_Citas_Estudiante_InicioReservado",
                table: "Citas",
                columns: new[] { "PerfilEstudianteId", "FechaHoraInicioUtc" },
                unique: true,
                filter: "\"Estado\" IN (0, 4, 5)");

            migrationBuilder.CreateIndex(
                name: "UX_Citas_Psicologo_InicioReservado",
                table: "Citas",
                columns: new[] { "PsicologoUsuarioId", "FechaHoraInicioUtc" },
                unique: true,
                filter: "\"Estado\" IN (0, 4, 5)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Citas_Estado",
                table: "Citas",
                sql: "\"Estado\" IN (0, 1, 2, 3, 4, 5, 6)");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesAgendaDia_PsicologoUsuarioId_Fecha",
                table: "ConfiguracionesAgendaDia",
                columns: new[] { "PsicologoUsuarioId", "Fecha" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DisponibilidadesPsicologo_ConfiguracionesAgendaDia_Configur~",
                table: "DisponibilidadesPsicologo",
                column: "ConfiguracionAgendaDiaId",
                principalTable: "ConfiguracionesAgendaDia",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revertir borraría excepciones diarias y no permite representar los estados 4..6.
            // Se requiere una estrategia de conservación revisada, no una conversión automática.
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    RAISE EXCEPTION 'Reversion automatica bloqueada: conservar configuraciones diarias y estados de citas requiere una estrategia revisada.';
                END;
                $$;
                """);
        }
    }
}
```

## Verificación ejecutada

- `dotnet build Zuni/Zuni.sln -c Release --no-restore`: **0 errores y 0 advertencias**.
- `dotnet run --project tests/Agenda.Offline -c Release --no-restore`: **162 comprobaciones correctas**.
- `dotnet run --project tests/BigFive.Offline -c Release --no-restore`: **77 comprobaciones correctas**.
- `git diff --check`: sin errores.
- EF modelo/snapshot: sin diferencias, comprobado offline.
- Metadatos de migración: Up sin drops ni cambios de datos, FK Restrict, índices activos y Down protegido.
- HTTP local con SQLite en memoria: renderizado Razor y layouts, calendario/modal, IDs únicos, rutas POST, antiforgery, no-store, identidades adicionales ignoradas y exclusión institucional.
- Reglas: pasado/límite 12 meses, horas/minutos, superposición, varios intervalos, bloqueo/habilitación, quitar, citas activas, variable, modalidades, solicitudes/aceptación/rechazo, legado, una por día, ajenos, roles y ausencia de dependencia Big Five.

No se ejecutaron pruebas sobre PostgreSQL real. La comprobación de doble reserva es offline (solapamientos, estados, transacciones, índices/modelo); una carrera real de dos conexiones PostgreSQL sigue pendiente. No se afirma que SQLite reproduzca el SSI de PostgreSQL.

## Revisión manual pendiente

Después de revisar y autorizar/aplicar tú la nueva migración en un entorno de pruebas:

1. Abrir Agenda y Citas en escritorio y móvil; comprobar colores/textos, navegación, foco y scroll de modales.
2. Crear dos intervalos diarios, bloquear/habilitar y comprobar la vista del estudiante asignado.
3. Solicitar, aceptar/rechazar, cancelar y revisar historial. Confirmar que las franjas ajenas no revelan identidades.
4. Comprobar rechazo de modificaciones con una cita activa y conservación de las reglas semanales en otras fechas.
5. Validar dos reservas simultáneas con conexiones PostgreSQL independientes.
6. Verificar citas Reservada existentes y la gestión de pendientes de meses anteriores.

No se hizo una inspección visual en navegador ni una prueba sobre datos reales: se verificó renderizado HTTP y CSS responsive, sin abrir la base productiva. El nuevo código de Agenda necesita la migración pendiente antes de utilizarse contra el esquema actual.

PostgreSQL permanece sin cambios. Esperando revisión; sin commit ni push.

