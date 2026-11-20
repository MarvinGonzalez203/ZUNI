# Entrega 1.11.0 — Denis Corp y cinco productos

Fecha: 2026-11-20. Alineación excepcional solicitada por el propietario: ZUNI, Academy, Fulleo, Colegios y Logística pasan a 1.11.0. Desde esta entrega cada aplicación vuelve a incrementar su versión independientemente. POS queda totalmente excluido, tanto en código como en numeración.

## Navegación y aspecto

La cabecera permanece visible al desplazarse. El menú de escritorio separa identidad fija arriba, opciones con desplazamiento propio y pie fijo con asistente y versión centrados. La identidad muestra el producto, cliente o usuario disponible; no se sustituye por el nombre de un cliente concreto en las portadas SaaS. Los grupos con subopciones se comportan como acordeón. La navegación móvil conserva los permisos y abre las opciones desde Más o desde la identidad del cliente, con tarjetas y cierre accesible.

Se agregó el regreso directo a iniciar sesión en la recuperación de Logística; ZUNI ya disponía de ese acceso. La V queda centrada, conserva animación y respeta movimiento reducido. Se ajustaron dimensiones de la barra móvil para no tapar el asistente. El asistente de Fulleo se integra al pie del menú y su panel aprovecha la parte inferior disponible. La ayuda de Academy reconoce sonido, sonidos, volumen, voz, auriculares, micrófono y equivalentes en inglés; explica cómo revisar el audio y lleva a la práctica correspondiente. El catálogo sigue limitado por los permisos del usuario.

## Portadas

Academy y Fulleo abren primero una portada pública, con identidad de Denis Corp, capacidades reales, idioma, modo día/noche y accesos al producto. Academy incluye funcionalidades, acceso/planes, contacto y blog, con exploración de lecciones y acceso de cuenta separados. Fulleo explica comparación, fecha/fuente, ubicación, cuenta y publicación revisada; el comparador se abre mediante una acción explícita. Se conserva el regreso al padre mediante by Denis Corp.

Inicio corporativo: http://localhost:5001/ . Academy: http://localhost:5186/#/ . Fulleo: http://localhost:4180/deniscorp/fulleo/ . Los enlaces de Denis Corp llevan a estas portadas. No se publicó en nube.

## Accesos locales y contraseña

Por instrucción expresa se prepararon las cuentas de superadmin y cliente en Academy, Fulleo y Logística. ZUNI y Colegios conservan sus cuentas existentes y sus contraseñas. No se sobrescriben cuentas preexistentes. Las contraseñas temporales son aleatorias, distintas para cada acceso y están en un archivo local privado fuera de Git: C:/Denis Corp/deniscorporation/.local/accesos-1.11.0/LEEME.txt. No se incluyen sus valores en informes, parches, capturas, commits o logs.

Academy y Fulleo utilizan scrypt con sal aleatoria individual de 16 bytes, N=32768, r=8, p=1 y derivación de 64 bytes; al cambiar la contraseña se renueva la sal. Logística utiliza el hasher de ASP.NET Core Identity. Las tres aplicaciones exigen el cambio desde el servidor antes de permitir operaciones protegidas, validan los cinco requisitos y su confirmación en la interfaz y rechazan contraseñas débiles. Tras cambiar la clave se revocan las sesiones y se exige un inicio nuevo. Hay límites de intentos; no se amplían permisos a partir de campos enviados por el cliente.

Academy agrega roles learner y superadmin. El administrador puede consultar cuentas y activar/desactivar cuentas learner, sin mostrar hashes ni progreso privado. El usuario no puede concederse roles desde registro. Fulleo separa miembros de superadmin: solo este último publica o revisa datos. Se conserva compatibilidad con la autenticación administrativa anterior configurada de forma privada. Logística asigna al cliente operaciones sobre sus propios registros; el superadmin puede consultar todos los registros cuando cumple la política de 2FA. Ambos deben configurar el autenticador antes de utilizar Personas; no se desactivó esa protección para facilitar la prueba.

## Migraciones y futuros entornos

Academy incorpora backend/migrations/002_accounts.sql; Fulleo, backend/postgres-migrations/002_accounts.sql. Deben aplicarse con el usuario de migración antes de arrancar 1.11.0. No se modifica el contenido ni el checksum de 001. La cuenta de ejecución de Fulleo necesita SELECT, INSERT, UPDATE y DELETE sobre fulleo.nas_accounts, además de sus permisos anteriores. En esta instalación local se otorgó ese permiso a su usuario de ejecución existente, sin conceder acceso a bases de otros productos. Logística usa claims de Identity para el requisito temporal y no necesita tablas nuevas.

El CLI de Logística --preparar-accesos --archivo-privado exige Development y recibe un archivo privado; no acepta contraseñas impresas en la línea de comandos. No se crea una cuenta predeterminada durante el despliegue. La preparación de los accesos solicitados se ejecutó solo en las bases de desarrollo. No se copiaron cuentas temporales a producción ni se modificaron datos institucionales de LSR o ZUNI.

## Pruebas

- Compilación de ZUNI, Academy, Colegios y Logística; sintaxis y regresión dirigida de Fulleo.
- Regresión completa de ZUNI aprobada, con contador principal de 195 y suites adicionales. SQLite aislado, sin abrir PostgreSQL desde esa suite.
- Logística: 171 comprobaciones aisladas de permisos, CSRF, alcance, contraseña temporal débil/válida, retirada del requisito temporal y cierre de sesión anterior.
- Academy y Fulleo: 20 comprobaciones de API con cuentas sintéticas eliminadas al terminar. Incluyen bloqueo de operaciones, rechazo de clave débil, cambio válido, invalidación de clave anterior, sal nueva, sesiones revocadas y denegación de administración a clientes.
- Seis accesos solicitados comprobados sin consumir ni cambiar sus contraseñas temporales: marcados para cambio y bloqueados por el servidor.
- Fulleo: cinco pruebas de autenticación anterior, prefijos, CSRF, proxy, publicación y persistencia, con fixtures aisladas.
- Edge: siete pasos y repetición en cinco productos, español e inglés, tema oscuro, preguntas, cierre, recarga y borrado de conversación. Tamaños 320, 390, 768 y 1440 px sin desbordamiento horizontal en los casos probados.
- Medición del regreso de recuperación, V centrada, cabecera visible, identidad y pie independientes del desplazamiento, asistente centrado y cierre automático de otros grupos.

Los JSON y capturas registran el alcance exacto. No se probaron teléfonos físicos ni todos los módulos administrativos de cada producto. Se conserva el aviso NU1900 de ZUNI por falta de acceso al servicio NuGet de vulnerabilidades y dos avisos previos de nulabilidad de Persona en Logística. No se repitió una auditoría de dependencias.

## Versiones activas y trazabilidad

Las notas de actualización, package.json/locks propios, versiones de ensamblado, catálogo y changelogs reflejan 1.11.0. La API de Colegios estaba ejecutándose con otra cuenta de Windows; se verificó su relación con el supervisor y se reinició únicamente el proceso de esa API. Se confirmó posteriormente que responde 1.11.0 y PostgreSQL sigue activo. No se reinició Windows ni se detuvo POS.

Los commits de ZUNI y Logística incluyen solo los cambios de esta entrega. Se conservan fuera de ellos las modificaciones previas de privacidad de ZUNI. Academy, Fulleo y Colegios quedan documentados mediante parches respecto al estado inicial de sus carpetas, con versiones, hashes y evidencia en el repositorio privado corporativo. Cada parche se aplica en un árbol temporal y se compara con la fuente antes de registrarse.
