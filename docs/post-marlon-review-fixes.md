# Correcciones posteriores a la revisión de cambios (17/10/2026)

Rama: `fix/pkcs-security-update` (contiene `main` actual, incluida la actualización de `System.Security.Cryptography.Pkcs` 8.0.2).
Sin migraciones, sin cambios de esquema, sin cambios de paquetes y sin tocar PostgreSQL.

## 1. Privacidad del Director

**Antes:** el calendario del Director proyectaba el nombre del estudiante de cada cita, el Id de la cita, la modalidad y el lugar. La programación del Director listaba a todos los estudiantes con psicólogo asignado y todas las citas activas con nombre. El Director recibía notificaciones con el nombre y carné del estudiante y podía crear, reprogramar y consultar citas individuales.

**Ahora:**
- `/Director/Calendario` (menú **Agenda y citas → Carga de psicólogos**) muestra disponibilidad (franjas Disponible/Ocupado) y **bloques anónimos**: hora de inicio y fin y estado (Ocupado / Pendiente de revisar). El servidor ya no consulta nombre, carné, correo, carrera, perfil, solicitud, Id de cita, modalidad ni lugar (`AgendaCitasService.CalendarioDirector` proyecta solo fecha, hora y estado). Se agregan conteos agregados del mes: programadas o confirmadas, pendientes, completadas, canceladas o rechazadas e inasistencias.
- `/Director/Estadisticas` deja de ser un marcador de posición. Muestra indicadores agregados: solicitudes por estado, estudiantes atendidos (con al menos una atención completada), citas por estado, carga por psicólogo, atenciones completadas por mes (últimos 6) y estudiantes atendidos por carrera. Las carreras con menos de 5 estudiantes atendidos muestran «menos de 5» en lugar de la cifra.
- La protección está en el servidor, no solo en la vista:
  - `CitasProgramadasController` (Index, Editar, Guardar) solo admite el rol Psicólogo.
  - `AgendaCitasService.Programador` solo devuelve al psicólogo; el Director recibe acceso denegado aunque manipule la URL o envíe el formulario.
  - `Notificaciones` y `Leida` niegan el acceso a una cuenta Director sin rol Psicólogo o Estudiante, incluidas las notificaciones históricas. El aviso de notificaciones no se muestra al Director.
  - Al responder una propuesta histórica del Director, el psicólogo ya no genera una notificación para el Director. El estudiante sigue recibiéndola.

**Decisión sobre la programación individual:** se retiró el acceso del Director, no el código del psicólogo. Las ramas internas `Rol == "Director"` se conservan porque describen **citas históricas** ya propuestas por un Director, que el psicólogo todavía puede aceptar o rechazar. Retirarlas habría requerido reescribir la programación del psicólogo sin necesidad. La programación del Psicólogo y el flujo del Estudiante no cambian.

Se mantiene la asignación de cuestionarios del Director (`PruebasDirector/Asignar`). Lista a todos los estudiantes activos, no solo a quienes reciben atención, por lo que no revela quién está en acompañamiento.

## 2. Despliegue y configuración de Production

- `deploy/debian/institucion.env.example` incluye las variables que el código exige en Production: `ASPNETCORE_ENVIRONMENT`, `ConnectionStrings__DefaultConnection` (usuario de **ejecución**), `Zuni__InstallationId`, `Zuni__ProxyInverso`, `AllowedHosts`, `DataProtection__KeysPath`, `SeguridadClinica__ClavesPath` y `SeguridadClinica__BitacoraPath`.
- Nuevo `deploy/debian/institucion-migracion.env.example`: mismas rutas e identificador, con el usuario de **migración**, solo para `--migrar-seguridad`.
- Nuevo `deploy/debian/permisos-runtime.sql.example`: permisos mínimos del usuario de ejecución (sin `CREATE`; solo `SELECT, INSERT` en auditorías, consentimientos y aceptaciones; solo `SELECT` en `EstadoProteccion`).
- `zuni@.service`: una sola ruta escribible `/var/lib/zuni/%i`, con subcarpetas documentadas `dataprotection/`, `seguridad/` y `auditoria/`. Se eliminaron de la documentación las rutas `/var/lib/zuni-private` y `/var/lib/zuni-audit`, que el servicio no podía escribir.
- `docs/instalaciones-debian.md`: estructura por instalación, dos usuarios PostgreSQL y procedimiento ordenado (respaldo → migraciones EF → permisos → `--inicializar-seguridad` → copia segura de claves → `--migrar-seguridad` → `--exportar-seguridad`/`--verificar-seguridad` → arranque → Nginx/HTTPS → primer Administrador). También explica qué pasa si se pierden las claves.
- `docs/deployment.md` y `docs/proteccion-clinica-y-auditoria.md`: actualizados con el mismo orden, variables y rutas.
- `backup.env.example` usa el usuario de ejecución. `backup.sh` recuerda que las claves se respaldan aparte.

Ningún comando se ejecutó. No se crearon usuarios ni bases.

## 3. Avisos antes de Big Five y cuestionarios

Nuevo parcial `Views/Shared/_AvisoPrivacidadActividad.cshtml`, que aparece en Big Five (inicio y cuestionario) y en Cuestionarios indicados (listado y respuesta). Indica que la actividad se realice en un lugar tranquilo, privado y sin interrupciones, que no constituye un diagnóstico y que sus resultados son herramientas de apoyo y orientación psicoeducativa. No cambia algoritmos, respuestas ni puntuaciones.

## 4. Dirección de acceso al crear cuenta o asignar contraseña temporal

Después de **Agregar usuario** (en la ficha del usuario) y en la pantalla de **contraseña temporal generada** se muestra la dirección de inicio de sesión. Se calcula de la petición actual: esquema y host, ya restringidos por `AllowedHosts` y, detrás de Nginx, por `Zuni__ProxyInverso`. No se guarda, no se envía por correo y no incluye tokens ni contraseñas.

## 5. Textos actualizados (solo visibles; sin renombrar clases, tablas ni migraciones)

| Antes | Ahora |
|---|---|
| Expedientes disponibles / Consultar expedientes / Ver expediente | Fichas de acompañamiento / Consultar fichas / Ver ficha de acompañamiento |
| Expediente inicial | Ficha inicial de acompañamiento |
| Motivo de consulta (formulario y ficha) | Motivo de atención |
| acceso clínico al expediente | acceso profesional a la ficha de acompañamiento |
| sin copiar las notas clínicas | sin copiar las notas profesionales privadas |
| el registro clínico (programación) | el registro de atención |
| «La creación de notas clínicas aún no está disponible» (texto desactualizado) | Explica que el registro de cada atención se completa desde la Agenda, con notas profesionales privadas separadas |
| Almuerzo: «incluidas las propuestas del Director» | Se retiró la mención |

Se agregaron sus traducciones en `Localizacion/en.json`. Los avisos «sin datos clínicos» se conservan porque describen una restricción real.

## 6. Mayoría de edad y activación en un solo recorrido

- **Registro:** el formulario pide la **fecha de nacimiento** (obligatoria, con asterisco). El servidor valida 18 años o más con la fecha de Guatemala y crea en la misma transacción la constancia `VerificacionMayoriaEdad` en estado **Pendiente**, con la edad calculada y auditoría `MAYORIA_EDAD_DECLARADA`. La fecha **no se guarda**, ni se piden DPI, CUI o imágenes. Un registro de menor de edad o sin fecha se rechaza y no crea cuenta.
- **Ficha del Administrador** (Estudiantes → Gestionar): muestra «Mayoría de edad» como **Pendiente de verificación (edad declarada)**, **Verificada**, **No verificada: requiere una nueva declaración** o **Sin declaración**. Si la cuenta está pendiente de activación, la ficha muestra los dos pasos. El botón **Revisar documento de mayoría de edad** abre la revisión existente y vuelve a la ficha para **Activar registro**.
- La revisión documental admite ahora autorregistros **pendientes de activación**. Las cuentas desactivadas siguen fuera del flujo. **Verificar no activa la cuenta y activar no verifica la edad.**
- Estudiantes existentes: no se marcan como verificados, no se inventa edad y no se omite la revisión. Siguen usando Mayoría de edad → declaración, como antes.

## 7. Factores educativos guiados en el informe

En **Preparar informe educativo** aparece «Factores educativos observados o reportados», con 12 casillas opcionales: concentración, estrés académico, hábitos de estudio, organización del tiempo, carga académica, motivación, adaptación al entorno universitario, dificultades emocionales generales, problemas de sueño o descanso reportados, necesidad de acompañamiento académico, necesidad de seguimiento psicológico educativo y otro.

Las casillas **no tienen campo propio** (no se envían ni se guardan). Los botones «Agregar a barreras observadas» y «Agregar a necesidades de apoyo» insertan una línea editable: «Factores educativos observados o reportados (no constituyen un diagnóstico): …». El psicólogo la revisa antes de guardar. Se conservan la autorización por versión y destinatario, el versionado, la revocación, la auditoría y el PDF. No se copia diagnóstico, motivo, notas privadas, ficha, DPI, contactos de emergencia ni respuestas de pruebas.

## 8. Pruebas offline

- Nuevo `PrivacidadDirectorChecks.cs`:
  - el calendario del Director no contiene nombre, carné, correo, carrera, lugar, Id de perfil ni de cita, y los bloques ocupados y las métricas siguen visibles;
  - las rutas directas (`/CitasProgramadas`, `Editar`, `Notificaciones` y agenda del psicólogo) y el POST de programación se niegan, y la cita no cambia;
  - las estadísticas no muestran identidades;
  - el psicólogo conserva su programación;
  - Big Five y Cuestionarios muestran el aviso y el texto de no diagnóstico;
  - comprobaciones estáticas del despliegue: variables obligatorias, usuarios distintos, rutas dentro de `ReadWritePaths`, guía coherente y ejemplos sin contraseñas.
- Nuevo `LegadoDirector.cs`: crea citas históricas propuestas por un Director para seguir probando la respuesta del psicólogo.
- Actualizadas: `ProgramacionChecks`, `AmpliacionPortalChecks`, `AlmuerzoRevisionChecks` y `CoordinacionEducativaChecks` (el Director recibe acceso denegado), `Program.cs` (declaración en el registro, revisión desde la ficha sin activación automática, rechazo de menores y de registros sin fecha), `PrivacidadInformesChecks` (factores guiados), `CamposObligatoriosChecks` (asterisco en fecha de nacimiento) e `Infraestructura.cs` (fecha ficticia de adulto en registros de prueba).

## 9. Limitaciones pendientes

- `tests/instalaciones-debian.ps1` es anterior a la protección clínica: no configura `SeguridadClinica__*` ni separa usuarios, por lo que la aplicación del ensayo no arranca en Production. Debe actualizarse antes de repetirlo.
- Los scripts de navegador (`*.browser.cjs`) no se revisaron contra los nuevos textos. `coordinacion.browser.cjs` usaba la programación del Director.
- `Revisiones` de mayoría de edad sigue listando solo cuentas activas. Los autorregistros pendientes se revisan desde Estudiantes → Pendientes → Gestionar.
- La dirección de acceso depende de que `AllowedHosts` y el proxy estén bien configurados.
- No se compiló ni ejecutó nada en este entorno: hay que ejecutar build y suites en el equipo local.
