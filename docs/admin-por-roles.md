# Panel del Administrador por roles

Rama de trabajo: `fix/registro-atencion-por-cita` (sin commit). No se cambió esquema, migraciones, PostgreSQL, Big Five, Agenda ni lógica clínica.
**No compilado ni probado en la sesión que lo escribió.**

## Menú

- PANEL ADMINISTRATIVO: Tablero, Agregar usuario
- USUARIOS POR ROL: Administradores, Directores, Psicólogos, Catedráticos, Estudiantes
- GESTIÓN: Solicitudes de atención
- CUENTA: Mi cuenta (Cerrar sesión sigue en el pie del menú)

## Rutas

| Ruta | Contenido |
|---|---|
| `/Administrador` | Tablero: tarjetas por rol (cantidad + "Ver usuarios"), total/activos/inactivos, pendientes de activación, solicitudes pendientes. Sin tabla de usuarios. |
| `/Administrador/Administradores`, `/Directores`, `/Psicologos`, `/Catedraticos`, `/Estudiantes` | Listado de un solo rol, con búsqueda, filtro de estado (Activos / Inactivos / Pendientes de activación) y paginación de 25. Columnas: Nombre, Correo, Estado y "Gestionar". Estudiantes: además carné y carrera. Psicólogos: además asignaciones vigentes. |
| `/Administrador/SinRol` | Cuentas sin rol. Solo aparece en el tablero si existen, para que ninguna cuenta quede inaccesible. |
| `/Administrador/Gestionar/{id}` | Ficha del usuario: datos, rol actual, estado, carné/carrera o asignaciones vigentes, y las acciones Cambiar rol, Activar/Desactivar, Asignar contraseña temporal y Ver historial. |

Las acciones existentes (`CambiarRol`, `ConfirmarEstado`, `Reactivar`, `Desactivar`, `ContrasenaTemporal`, `Historial`, `AgregarUsuario`) no cambiaron su lógica.
Ahora vuelven a la ficha del usuario en lugar del tablero.
Se mantienen:

- `[Authorize(Roles = "Administrador")]` más la revalidación de administrador activo en cada acción;
- antiforgery y auditoría;
- el bloqueo de CambiarRol para psicólogos con asignaciones vigentes;
- la contraseña temporal;
- la activación de autorregistros.

## Pruebas (`tests/Seguridad.Offline`)

- El tablero no contiene tabla ni usuarios.
- Tablero y menú enlazan a las cinco vistas.
- Cada vista lista solo su rol, incluidas las cuentas con dos roles y el cambio de rol de psi1.
- Asignaciones vigentes por psicólogo y carné/carrera sin datos clínicos.
- Filtros de estado y búsqueda por carné; filtro inválido devuelve 400.
- Ficha con todas las acciones; ficha propia sin acciones peligrosas; usuario inexistente devuelve 404.
- Un no Administrador es redirigido a Acceso denegado.
- Las cuentas pendientes de activación siguen identificándose.
- CambiarRol sigue bloqueado con asignaciones vigentes y vuelve a la ficha tras un cambio permitido.
