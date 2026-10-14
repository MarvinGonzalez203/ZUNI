# Configuración institucional — Primera entrega

Implementada en `feature/director-V2` el 13 de octubre de 2026.

## Cómo utilizarla

Ingresar con una cuenta activa de **Administrador** y abrir **Configuración de la institución** en el menú lateral. Ruta: `/Institucion`.

Se pueden guardar:

- Nombre de la institución, mostrado en Inicio, inicio de sesión y registro.
- Correo de soporte opcional, mostrado como contacto en las pantallas de acceso.
- Política de registro y correo.
- Hasta 20 dominios autorizados, separados por líneas, comas o punto y coma, sin `@`, direcciones web ni comodines.

| Política | Registro público | Alta desde Administración |
|---|---|---|
| Dominios autorizados | Solo correos del listado; cuenta pendiente de activación. | Solo correos del mismo listado. |
| Cualquier correo válido | Se admiten correos personales; cuenta pendiente de activación. | Cualquier correo válido. |
| Solo cuentas creadas por el Administrador | Se oculta el formulario y se rechazan también los envíos de formularios anteriores. | Cualquier correo válido. |

Un dominio se compara completo. Autorizar `colegio.edu.gt` no autoriza automáticamente `sub.colegio.edu.gt` ni `colegio.edu.gt.otro.test`.

Para conservar el comportamiento existente, el valor inicial permite `miumg.edu.gt`. No se cambia la política automáticamente al instalar esta entrega. El primer Administrador puede crearse con un correo válido de otra institución mediante el comando de aprovisionamiento existente; no se generan otras autenticaciones ni tablas Identity.

Los cambios no desactivan usuarios, no cambian contraseñas ni roles y no impiden que una cuenta existente ingrese porque su dominio haya dejado de estar autorizado. Aceptar un correo tampoco verifica su propiedad ni la pertenencia institucional: el registro público sigue requiriendo activación administrativa. El correo de soporte es un contacto; no configura un servicio de envío.

Si otra persona modificó la configuración mientras estaba abierto el formulario, se solicita revisar la versión actual. Los cambios quedan en la auditoría administrativa con fecha, actor y valores anteriores y nuevos.

## Base de datos y despliegue

Migración: `20261014043220_AddConfiguracionInstitucion`. Añade únicamente la tabla `ConfiguracionInstitucion`, con una configuración por instalación y una revisión para evitar sobrescrituras concurrentes. No modifica tablas Identity, expedientes ni cuentas.

Aplicar esta migración **antes de iniciar la versión nueva** en cada entorno, usando su procedimiento de respaldo y migración. Las pantallas públicas consultan la configuración y necesitan esa tabla.

Para una base local configurada mediante User Secrets, desde la raíz del repositorio:

```powershell
dotnet ef database update --project Zuni -- --environment Development --contentRoot "$PWD\Zuni"
```

En esta entrega se creó un respaldo de `localhost/ZuniDb` y se aplicó allí la nueva migración. No se conectó ni modificó Linode; la publicación sigue prevista para el 17 de octubre.

## Alcance y límites

Esta entrega implementa la política institucional de correo, nombre y contacto. El logotipo propio, la personalización del formato de carné, la retirada de Catedrático, las marcas de obligatoriedad, la verificación documental de edad, el informe educativo PDF, las videollamadas y las suscripciones siguen pendientes. El carné y el teléfono mantienen sus validaciones actuales; el cambio de correo por sí solo no hace compatible cualquier formato de identificación escolar.

## Verificación

Las comprobaciones de `tests/Seguridad.Offline/InstitucionChecks.cs` utilizan SQLite en memoria y un host HTTP efímero. Cubren formularios reales, los tres modos, dominio exacto, normalización, entradas inválidas, permisos, antiforgery, auditoría, revisión obsoleta, registro pendiente, bloqueo de formularios antiguos y conservación del acceso de cuentas existentes.

La transacción de configuración y la creación de cuentas se ejecutan con aislamiento SERIALIZABLE. Las pruebas en SQLite no equivalen a una prueba de concurrencia de PostgreSQL; los conflictos de versión o serialización se manejan sin presentar los cambios como guardados.
