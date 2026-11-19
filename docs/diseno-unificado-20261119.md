# Diseño unificado Denis Corp — 2026-11-19

Se aplicó la referencia visual del propietario al portal y a ZUNI, Academy, Fulleo, Colegios y Logística. POS quedó excluido. Accesos en dos columnas con presentación propia y formulario oscuro; cabecera compacta, idioma horizontal con icono, tema circular, pestañas de acceso/registro, botones de 52 px y campos de 56 px. En móvil se apilan las columnas y los controles conservan áreas táctiles de 48 px.

| Programa | Versión local | Color |
|---|---|---|
| ZUNI | 1.7.0 | Violeta |
| Academy | 1.5.0 | Azul |
| Fulleo | 0.12.0 | Amarillo |
| Colegios | 1.9.0 | Arena / rojo |
| Logística | 0.5.0 | Arena / rojo |

## Funciones y conservación

Se conservan datos institucionales, roles, CSRF, políticas de registro y vínculos de regreso a Denis Corp. Se añadieron confirmación de contraseña en Academy y controles de visibilidad en recuperación/cambio de Colegios. ZUNI y Logística comparten controles nativos de visibilidad; Logística valida los cinco requisitos tanto con Identity como en registro/restablecimiento. Los indicadores cambian al escribir: número, mayúscula, minúscula, símbolo y mínimo ocho caracteres. Los límites máximos existentes se conservan. Las contraseñas de cuentas existentes no se invalidan durante el acceso.

Fulleo pasa de ventana modal a página de acceso: la consulta es pública y las cuentas de publicación son administradas. Solicitar acceso abre el contacto corporativo; no se crean administradores automáticamente. Academy ofrece el contacto real de soporte; esta entrega no inventa envío de correo ni Google. ZUNI conserva recuperación por correo configurada y la de Colegios sigue el proceso autorizado por Dirección. Los controles V se mantienen en los nuevos accesos.

El registro público de la institución local de ZUNI exige publicar su política de privacidad. Se respetó ese bloqueo; para revisar el formulario completo se exportaron vistas del host de pruebas con una institución ficticia y SQLite aislado, sin modificar configuración ni política local.

## Verificación

Compilación de los cuatro proyectos compilables y comprobación sintáctica de Fulleo. Logística: 154 comprobaciones aisladas de autorización, alcance, CSRF, permisos y contraseña. Seis homes revisados en 320/390/768/1440 px, español/día e inglés/noche, enlaces de entrada a cinco hijos y regreso a la portada. Acceso y registro revisados con Edge y emulación de tamaños; cinco estados de acceso y cuatro registros, con estados de reglas pendientes, parciales y completas. Resultados y capturas registrados en la entrega corporativa.

La compilación de ZUNI conserva un aviso NU1900 por consulta de vulnerabilidades NuGet sin conexión disponible; Logística conserva dos avisos de nulabilidad anteriores en Persona. No hay errores de compilación. No se repitió la auditoría de dependencias ni se probaron teléfonos físicos. Tampoco se crearon cuentas, se enviaron formularios de registro ni se cambiaron las bases locales.

## Abrir

Portada: http://localhost:5001/ . Se conserva el iniciador C:/Denis Corp/Iniciar Denis Corp.cmd. Para ver los archivos nuevos en una pestaña abierta, recargar con Ctrl+F5; Academy mantiene su actualización PWA. No se publica en nube.

El manifiesto de esta entrega conserva hashes y parches respecto al estado inicial de cada carpeta. Se excluyen datos, claves y archivos privados. Los cambios anteriores de privacidad de ZUNI y el trabajo de POS se conservan fuera del commit de esta entrega.
