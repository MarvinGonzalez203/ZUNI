# Diseño de acceso 1.7.0 — 2026-11-19

Referencia visual: cabecera compacta, controles de idioma horizontales, tema circular, botones de 52 px, campos de 56 px, acceso de dos columnas y formulario oscuro. Móvil apila la presentación y el formulario. Identidad: ZUNI violeta, Academy azul, Fulleo amarillo, Denis Corp/Colegios/Logística arena de día y rojo de noche. POS excluido.

Se conservan los campos, permisos, políticas de registro, validación de servidor, antifalsificación y vínculos corporativos de cada sistema. Academy incorpora confirmación de contraseña. Logística exige ocho caracteres y las cinco reglas en registro y recuperación; no invalida contraseñas existentes al iniciar sesión. Fulleo mantiene cuentas administradas y consulta pública sin registro: la segunda pestaña solicita acceso por contacto, sin crear administradores. Los proveedores de recuperación no configurados no se simulan.

Cada repositorio recibe el mismo contrato CSS denis-ui.css; su atributo data-dc-product aplica la paleta. Los cambios se mantienen separados de POS y de los cambios previos de privacidad. El manifiesto y parches de esta entrega permiten reproducir los cambios en cada proyecto. No se modifican bases, usuarios ni secretos.

Las pruebas visuales y funcionales de esta entrega se documentan en el informe central de ZUNI.
