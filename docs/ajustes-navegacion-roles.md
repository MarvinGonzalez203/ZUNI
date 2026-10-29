# Navegación, guías y privacidad por rol

Cambios posteriores al commit `c7bde9e`, trabajados únicamente en Director V2 y localhost.

- Guías disponibles para Administrador, Director, Psicólogo y Estudiante desde el enlace situado encima de los controles inferiores. El servidor determina el rol a partir de la sesión y de la cuenta vigente: añadir `rol=SuperAdmin` a la URL no amplía el catálogo.
- SuperAdmin conserva el catálogo completo. Seleccionar un rol envía el filtro al servidor; la búsqueda se combina con ese filtro.
- Barra lateral comprimible en escritorio, con preferencia por cuenta/rol, iconos sobre un eje estable, nombres accesibles y títulos para los controles compactos. El menú conserva el desplazamiento; la V lateral indica contenido restante y permite avanzar.
- La V de los modales dispone de una fila en el pie, separada de los textos y de las acciones. Se mantienen el desplazamiento habitual y la preferencia de movimiento reducido.
- Corregidas superficies claras con texto claro en agenda y distintivos, así como la alineación de tarjetas comerciales y guías en tema oscuro.
- El tablero del Director incorpora acceso directo a Proponer cita y Mis propuestas. No se restituyó la programación individual ni la vista de expedientes: el flujo existente permite proponer y el profesional acepta, reprograma o rechaza. Las notificaciones institucionales del Director siguen limitadas a sus propias propuestas.
- Las notificaciones generales requieren tanto el destinatario como la relación de la cita con el estudiante o psicólogo de la sesión. Un registro histórico mal dirigido a una cuenta ajena al caso no aparece en el listado, no incrementa su campana y no puede marcarse leído desde esa cuenta. Las novedades de versión son avisos generales del producto y su lectura es individual.

## UMG y separación de instituciones

El responsable confirmó un contrato anual UMG del **01/10/2026 al 30/09/2027**. El formulario puede prepararse sin guardar mediante `/SuperAdmin/EditarCentro?nombreInicial=UMG&inicioInicial=2026-10-01&mesesInicial=12`; la confirmación utiliza el flujo comercial y su auditoría. Abrir el formulario no registra un contrato ni ejecuta cobros.

Los estudiantes actuales de la instalación con dominio `@miumg.edu.gt` pertenecen al contexto institucional UMG indicado por el responsable. El dominio ayuda a identificar ese conjunto, pero no es una frontera de autorización ni concede permisos. No se cambian cuentas ni se mueven fichas por una coincidencia de correo.

La arquitectura continúa siendo **una instalación y base por institución**, según `instalaciones-debian.md`. Los otros centros que se anoten en el registro comercial no reciben acceso a la base UMG ni crean espacios de estudiantes en ella. Un nuevo centro necesita su propia base, credenciales de ejecución, carpetas persistentes y claves. El registro comercial no implementa un sistema multiinstitución dentro de una misma base.

No cambiar el identificador criptográfico ni regenerar claves de la instalación vigente para renombrarla como UMG: impediría recuperar información ya cifrada. La etiqueta comercial y la identidad criptográfica son conceptos distintos.

## Verificación

Compilación Release y suite Seguridad.Offline completas correctas con SQLite temporal. Incluye las comprobaciones existentes de propuestas del Director y dos nuevas de notificaciones mal dirigidas. Se añadieron pruebas de guías por rol y del formulario comercial preparado. Navegador aislado: 40 comprobaciones de menú, iconos, guías y modales; 97 de continuidad de idioma y experiencia móvil. Catálogo de traducciones y sintaxis JavaScript comprobados.

No se añadieron migraciones para estos ajustes. No se publicaron cambios en zuni.lat.
