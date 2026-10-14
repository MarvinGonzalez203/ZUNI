# Navegación, administración y práctica aislada

Cambios del 14 de octubre de 2026 en `feature/director-V2`.

- La barra lateral conserva desplazamiento, oculta su scrollbar y recuerda la posición por perfil durante la sesión del navegador. Los grupos usan una V de CSS con posición fija.
- Roles retirados y fichas de usuarios marcan la sección correspondiente. Las tarjetas administrativas con destino son enlaces completos que admiten clic, teclado y touch.
- Se separaron acciones de atención imprevista, programación de citas, cuestionarios e informes.
- El alta administrativa exige clasificar mayor/menor de edad. La columna nullable `AspNetUsers.MayorEdadDeclarada` no sustituye la verificación documental ni habilita atención a menores. Las cuentas existentes conservan null.
- El administrador corrige nombres desde la ficha de otro usuario, con auditoría y revisión de concurrencia. El cambio invalida las sesiones que conservaban el nombre anterior. Para su propia cuenta utiliza Mi cuenta.
- Mi cuenta permite al administrador editar su correo y teléfono de ocho dígitos. El correo se normaliza, debe ser único y su cambio invalida sesiones y tokens de recuperación previos. No se marca como verificado automáticamente.
- Institución ofrece violeta, negro, rojo, verde y azul, además del selector de color. El primer plano se calcula según luminancia, con contraste mínimo AA para texto normal.
- Se retiró el logo solamente del panel lateral de acceso y se centró el texto en ese panel.

## Registro público

La instancia local no tiene una política de privacidad publicada. `Cuenta/Registro` muestra `RegistroNoDisponible` hasta que exista una política vigente. El administrador debe revisar y publicar los datos institucionales en Política de privacidad. El registro continúa sujeto a dominios permitidos y activación administrativa. No se publicó una política ni se cambiaron esos criterios automáticamente.

## Recorrido ficticio

La cuenta que el usuario confirmó como ficticia tiene una marca administrativa `CUENTA_DEMO_HABILITADA`. En Development se muestra la opción **Demostración · Nivel 1**; `/Demostracion` valida también cuenta activa, rol y sesión vigente. Fuera de Development o para cuentas sin marca, no está disponible.

La página presenta la cita simulada del 13/10/2026, 14:00–14:15 de Guatemala, y las 15 preguntas propuestas del nivel 1. La participación se confirma al responder. No guarda respuestas, citas, consentimientos, inducciones ni registros de atención; no activa el instrumento para otros estudiantes. La representación visual de una atención completada no acredita una atención real.

La revisión automática rechazó la alternativa de insertar atenciones e inducciones ficticias en tablas clínicas y activar globalmente el instrumento. Se sustituyó por este recorrido aislado, sin ejecutar el script rechazado.

## Verificación

- Release: compila sin errores ni advertencias.
- Seguridad offline: 585 comprobaciones, incluidas 31 de administración y demostración.
- Agenda offline: 316 comprobaciones.
- Edge: 124 comprobaciones, incluidas tarjetas por touch, posición lateral, colores con contraste y login centrado.
- Migración `20261014222256_AddClasificacionEdadAdministrativa` aplicada a PostgreSQL local después de respaldarlo; EF confirma que no hay cambios pendientes del modelo.
- Comprobación local: marca demo presente, cero citas/solicitudes clínicas nuevas para el perfil ficticio y los tres instrumentos conservan su estado inactivo.

Las pruebas HTTP usan SQLite ficticio. No constituyen una prueba de concurrencia real de PostgreSQL. La comprobación de vulnerabilidades NuGet quedó sin conexión al origen remoto; no impidió compilar ni ejecutar las pruebas.
