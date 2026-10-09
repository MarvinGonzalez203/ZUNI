# Administración local de ZUNI

Implementado en `feature/student-dashboard-clean`, sobre `2c7030c`. Sin commits, merges, push ni migraciones.

## Cuenta y respaldo

- Base autorizada: PostgreSQL local, `localhost:5432/ZuniDesarrollo`.
- Cuenta existente: `ealvarezr9@miumg.edu.gt`.
- Carné: `74902015193`, mostrado como `7490-20-15193`.
- Roles: Administrador y Estudiante.
- Se conservaron el mismo ID, contraseña, correo y datos del perfil. Solo cambiaron el carné, la asignación adicional de rol y los sellos de sesión; se agregó auditoría.
- Respaldo anterior a la actualización: `.visual-check/private-backups/evin-before-admin-20261009-031035.json`. Este archivo contiene datos privados y está excluido de Git.
- Se revisaron las claves foráneas a usuarios y perfiles: usan identificadores estables, que no se modificaron. No se borraron registros.

## Ingreso

Reinicia la aplicación desde tu entorno habitual. Inicia sesión otra vez con el correo anterior y tu misma contraseña: la sesión anterior fue invalidada al cambiar los roles.

El selector `/Panel` permite elegir Administrador o Estudiante. El menú «Mis paneles» permite cambiar sin cerrar sesión. Si se entra mediante una URL de retorno, el sistema conserva ese destino y ofrece el cambio desde el menú.

- `/Administrador/Tablero`: resumen administrativo.
- `/Administrador/Estudiantes`: búsqueda por nombre, correo y carné; registro, edición de nombre/carné/carrera, estado, accesos e historial.
- `/Administrador`: usuarios y accesos. La opción «Conservar los roles actuales» agrega el seleccionado sin borrar los demás. Desmarcarla solicita un reemplazo explícito; siguen vigentes las protecciones del administrador propio y del último administrador activo.
- `/Administrador/Importar`: plantilla y vista previa CSV.
- `/Estudiante`: tablero existente; Mi perfil, Evaluaciones, Resultados y Citas se conservan.

## CSV

UTF-8, encabezados `Nombre,Correo,Carne,Carrera`; máximo 100 estudiantes y 256 KB. Se admiten comillas CSV, carnés numéricos de 10/11 dígitos o con guiones 4-2-4/5, y correo institucional. Carrera es opcional. Se validan duplicados dentro del archivo y contra la base, tanto en la revisión como al confirmar. Una transacción guarda el lote completo o lo rechaza.

No se cargaron los 20 estudiantes pendientes. Las cuentas importadas reciben una contraseña aleatoria no mostrada; en desarrollo pueden establecerla mediante el flujo existente «Olvidé mi contraseña», que muestra el enlace de restablecimiento localmente. No se implementó envío de correo productivo.

## Verificación

- Compilación del proyecto: correcta, 0 errores y 0 advertencias.
- Validaciones C#: 24 comprobaciones de perfil, presentación y CSV, incluidas longitudes 10/11 y rechazo de duplicados/formato inválido.
- JavaScript existente: 4 pruebas aprobadas de cambios pendientes, validación, doble envío y recuperación desde caché.
- HTTP local: 31 comprobaciones aprobadas de rutas administrativas y estudiantiles, selector, carné y formulario, autorización, CSRF, vista previa CSV y rechazo de duplicados.
- Las pruebas HTTP usan tickets locales firmados de corta duración con el ID y sello vigente de Evin; no conocen ni cambian su contraseña. No reemplazan una prueba manual de ingreso escribiendo la contraseña.
- No se insertaron estudiantes de prueba en la base. El guardado de un CSV nuevo completo queda por comprobar con el lote autorizado que proporciones; sí se probó el rechazo de duplicados mediante el endpoint real.
- Comparación de cuenta/perfil antes y después: datos iguales salvo carné y sellos de sesión esperados.

## Archivos modificados

- `Zuni/Controllers/AdministradorController.cs`: búsqueda, visualización de varios roles, alta y conservación de roles.
- `Zuni/Controllers/CuentaController.cs`: selector de panel y carné de 11 dígitos.
- `Zuni/Controllers/PerfilController.cs`: validación y edición de carné.
- `Zuni/Models/AccountViewModels.cs`.
- `Zuni/Models/CompletarPerfilEstudianteViewModel.cs`.
- `Zuni/Models/MiPerfilViewModel.cs`.
- `Zuni/Models/Administrador/AgregarUsuarioViewModel.cs`.
- `Zuni/Models/Administrador/CambiarRolViewModel.cs`.
- `Zuni/Models/Administrador/UsuarioAdminViewModel.cs`.
- `Zuni/Views/Administrador/AgregarUsuario.cshtml`.
- `Zuni/Views/Administrador/CambiarRol.cshtml`.
- `Zuni/Views/Administrador/ConfirmarEstado.cshtml`.
- `Zuni/Views/Administrador/Index.cshtml`.
- `Zuni/Views/Cuenta/Registro.cshtml`.
- `Zuni/Views/Perfil/Completar.cshtml`.
- `Zuni/Views/Shared/_DashboardLayout.cshtml`: cambio de panel y dependencia jQuery para validación.
- `tests/StudentProfile.Checks/Program.cs`.
- `tests/StudentProfile.Checks/StudentProfile.Checks.csproj`.

## Archivos agregados

- `Zuni/Controllers/AdministradorEstudiantes.cs`.
- `Zuni/Controllers/AdministradorEdicionEstudiante.cs`.
- `Zuni/Controllers/PanelController.cs`.
- `Zuni/Models/Administrador/EstudianteAdminViewModel.cs`.
- `Zuni/Models/Administrador/EditarEstudianteViewModel.cs`.
- `Zuni/Services/EstudiantesCsv.cs`.
- `Zuni/Views/Administrador/Tablero.cshtml`.
- `Zuni/Views/Administrador/Estudiantes.cshtml`.
- `Zuni/Views/Administrador/EditarEstudiante.cshtml`.
- `Zuni/Views/Administrador/Importar.cshtml`.
- `Zuni/Views/Administrador/_Navegacion.cshtml`.
- `Zuni/Views/Administrador/_ViewStart.cshtml`.
- `Zuni/Views/Panel/Index.cshtml`.
- `docs/administracion-local.md` (este informe).

Herramientas auxiliares locales excluidas de Git: `.visual-check/role-repair` y `.visual-check/admin-http`. No ejecutar de nuevo la herramienta de actualización de cuenta: está diseñada para el estado anterior específico y aborta si no coincide.
