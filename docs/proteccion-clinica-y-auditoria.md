# Protección de textos clínicos, auditoría y consentimiento

## Alcance y límites

La aplicación cifra antes de enviar a PostgreSQL los campos enumerados en `Zuni/Data/CifradoClinico.cs`: textos del expediente inicial y sus contactos, resultado profesional privado, textos compartidos de la atención, contenido y datos personales de las versiones de informes, autorización del informe, detalles de solicitudes de derechos y datos ampliados de auditoría. No cambia los identificadores, relaciones, índices de agenda ni las reglas de autorización clínica.

Se utiliza AES-256-GCM, nonce aleatorio de 96 bits, etiqueta de 128 bits y datos asociados que distinguen instalación y campo. Un dato sin cifrar, con otra clave o cuya etiqueta no verifica causa un error: no se devuelve como texto ni se acepta una recuperación parcial. El formato `zenc1` incorpora identificador de clave para conservar versiones anteriores al rotar. El cifrado usa claves independientes de las de las cookies.

Esto es **cifrado de campos**, no de toda la base. Cuentas, metadatos de agenda y respuestas/puntuaciones estructuradas de cuestionarios no se convierten en esta intervención. No se pueden hacer búsquedas, concatenaciones ni comparaciones SQL sobre los textos cifrados: primero filtrar por identificadores y permisos, después materializar las columnas necesarias. Nunca descargar todos los expedientes para filtrar en memoria.

El administrador de PostgreSQL no obtiene las claves al copiar la base. Quien controla también el proceso, las claves y el sistema operativo de ZUNI puede eludir esta separación. La custodia de claves debe pertenecer a una cuenta de servicio distinta del administrador de la base. No representa cifrado de extremo a extremo ni garantía frente a un propietario absoluto del servidor.

## Configuración y custodia

Variables de producción obligatorias, con rutas absolutas fuera de la publicación:

```text
SeguridadClinica__ClavesPath=/var/lib/zuni-private/claves.json
SeguridadClinica__BitacoraPath=/var/lib/zuni-audit/bitacora.jsonl
Zuni__InstallationId=<identificador-unico-de-institucion>
```

En Development, los archivos se sitúan bajo los datos locales del usuario, `ZUNI/seguridad/<instalación>/`. No se guardan en Git, en `wwwroot`, en PostgreSQL ni en el directorio de publicación. Las carpetas se restringen a la cuenta ejecutora (0700) y los archivos a lectura/escritura de esa cuenta (0600); en Windows se aplica ACL sin herencia. Ejecutar la provisión como la cuenta de servicio o entregar después la propiedad a esa cuenta por un procedimiento controlado. No usar una carpeta compartida con archivos de otros servicios.

```text
dotnet Zuni.dll --inicializar-seguridad
```

La orden utiliza creación exclusiva: **no reemplaza una clave existente**. Respaldar las claves de forma cifrada, fuera de PostgreSQL y con custodia separada. Conservar todas las claves históricas necesarias. Cambiar `InstallationId`, eliminar el archivo o regenerar claves no es un procedimiento de recuperación: impedirá leer los datos.

Para una rotación controlada, agregar nuevas claves aleatorias independientes de 32 bytes al anillo, con otro identificador, cambiar `Activa` y reiniciar todas las réplicas coordinadamente. Conservar las anteriores para lectura y verificación. No editar ni retirar claves referenciadas por datos o bitácoras. La recodificación completa y la destrucción de claves requieren un procedimiento adicional; no están automatizadas.

## Migración de una instalación existente

1. Detener todas las instancias web y exportadores de esa instalación; trabajar en mantenimiento.
2. Respaldar PostgreSQL completo, verificar que se restaura y proteger el respaldo con cifrado y permisos. Los respaldos anteriores pueden contener texto plano y no se cifran retroactivamente.
3. Provisionar y respaldar las claves. Probar primero con un clon aislado y una bitácora distinta.
4. Revisar el script de `20261016024040_AddProteccionClinicaYEvidencias`. Generarlo desde el proyecto con herramientas EF y entorno/contentRoot explícitos; aplicar el esquema con un **rol de migración**, diferente del de ejecución. No usar `migrations remove --force` ni retroceder migraciones.
5. Con la conexión del propietario de las tablas y la misma configuración de claves/instalación:

```text
dotnet Zuni.dll --migrar-seguridad
dotnet Zuni.dll --exportar-seguridad
dotnet Zuni.dll --verificar-seguridad
```

La conversión bloquea las tablas afectadas, procesa lotes de 200 y se confirma en una transacción serializable. Comprueba el retorno del cifrado, conserva nulos, reconoce sobres existentes e instala restricciones que impiden nuevas escrituras en claro. Un error revierte la conversión y el cambio temporal de trigger. La marca de finalización impide iniciar la web antes de completar el proceso. Una segunda ejecución completada no vuelve a cifrar los datos.

Las auditorías anteriores se firman como `HISTORICO_MIGRADO`: acreditan el estado encontrado al migrar, **no** que fueran inalterables antes. No se inventan direcciones IP, navegadores ni aceptaciones históricas. Las aceptaciones antiguas conservan su fecha y versión originales, pero no reciben una IP retroactiva.

La migración no permite un `Down` automático, porque reducir columnas, borrar evidencias o devolver texto plano no constituye una reversión segura. Ante un incidente, detener la web, conservar evidencias y restaurar una copia coherente de aplicación, base, claves y bitácora en un entorno aislado antes de decidir la recuperación. Nunca volver simplemente al binario antiguo contra la base cifrada.

## Bitácora e integridad

Cada alta en `AuditoriaUsuarios` o `EventosAccesoClinico` genera una evidencia `RegistrosSeguridad` en el **mismo SaveChanges y transacción**. El contenido se cifra y se firma con HMAC-SHA-256 y una clave independiente. No se registran contraseñas, cookies o notas clínicas en logs de consola. La fecha se normaliza a UTC con precisión de microsegundos, compatible con PostgreSQL.

La aplicación rechaza modificaciones/borrados de las evidencias, auditorías y aceptaciones. PostgreSQL añade triggers `BEFORE UPDATE OR DELETE OR TRUNCATE`, habilitados como `ALWAYS`. El rol de ejecución debe tener únicamente `SELECT, INSERT` en tablas de auditoría/aceptaciones y `SELECT` en `EstadoProteccion`; no ser propietario, superusuario ni tener permisos de DDL, TRIGGER o cambio a roles privilegiados. Producción comprueba y rechaza privilegios peligrosos. El operador de migraciones conserva privilegios especiales solo para mantenimiento.

El exportador toma lotes de 200, verifica firmas, escribe JSONL cifrado con una cadena de sellos y fuerza la escritura a disco antes del acuse en `ExportacionesSeguridad`. Los pendientes se seleccionan por ausencia de acuse, no por un cursor que perdería transacciones confirmadas fuera de orden. Un fallo entre archivo y acuse puede producir duplicados idénticos; el verificador los reconoce. Se reintenta cada cinco segundos y se drenan lotes completos sin esperar ese intervalo.

Una exclusión mediante advisory lock evita dos exportadores simultáneos por base. Todas las réplicas de la misma instalación deben apuntar a **la misma bitácora persistente compartida** y al mismo anillo de claves. No compartir estos archivos entre instalaciones distintas. El archivo se verifica al abrirlo o cambiar su longitud; una modificación del mismo tamaño se detecta mediante la verificación completa, no mediante el cache del exportador.

Programar operativamente `--verificar-seguridad` en mantenimiento o contra un corte consistente, y vigilar los mensajes críticos del exportador y el volumen de pendientes:

```sql
SELECT count(*) FROM "RegistrosSeguridad" r
WHERE NOT EXISTS (SELECT 1 FROM "ExportacionesSeguridad" e WHERE e."EventoId" = r."EventoId");
```

La verificación comprueba cadena externa, firmas, coincidencia de eventos y estado de los originales. También detecta eventos sin evidencia y eventos borrados de la base que permanecen fuera. No ejecutar contra datos que cambian continuamente esperando una fotografía coherente; detener escritores/exportador para una comprobación probatoria.

El archivo externo local es una segunda copia frente al administrador de la base, **no almacenamiento remoto inmutable**. En producción deben copiarse regularmente los archivos a almacenamiento separado con retención inmutable (WORM/Object Lock), administrado por otra cuenta, y establecer alertas, retención, backups y pruebas de restauración. Antes de exportarse existe una ventana de hasta varios segundos; un propietario absoluto de PostgreSQL puede deshabilitar triggers. El sistema no puede probar eventos que un adversario eliminó de todas las copias antes de que fueran preservados. No truncar ni rotar manualmente el archivo activo: se perdería la cadena comprobable.

## Evidencia de consentimiento

Se conservan evidencias nuevas para atención inicial, aceptación de política de privacidad, Big Five, primera participación en las pruebas indicadas y decisión/revocación de autorización de informes. Incluyen texto y versión, aviso del registro, identificador del usuario y referencia, decisión, idioma, fecha/hora UTC, IP del transporte y navegador limitado a 512 caracteres. También distinguen sesión autenticada, registro web sin sesión y operación sin petición HTTP. La evidencia se confirma junto con la operación; un rollback revierte ambas.

La IP procede de `HttpContext.Connection.RemoteIpAddress`, después de la política de proxy confiable. No se acepta desde el formulario ni se lee directamente un `X-Forwarded-For` arbitrario. Con Nginx local usar `Zuni__ProxyInverso=true`; con proxy remoto configurar explícitamente sus IP conocidas y comprobar la IP observada antes de publicar. No confiar en cualquier proxy o red. Sin IP disponible se conserva `null`; no se inventa una.

La IP y el navegador son datos personales, no pruebas inequívocas de identidad. El aviso se muestra antes de confirmar. La institución debe definir finalidad, acceso y plazo de conservación en su política. Estos registros no son una firma electrónica avanzada ni un estampado cronológico certificado y no garantizan cumplimiento legal o exención de responsabilidad.

## Verificación realizada y referencias técnicas

La suite de seguridad usa SQLite efímero, HTTP local, claves en memoria y casos de alteración de cifrado, clave, propósito, firma, archivo y datos; comprueba rollback y ausencia de texto clínico en la copia externa. Las pruebas PostgreSQL optativas requieren una conexión explícita y rechazan bases fuera del prefijo `zuni_seguridad_prueba_` o un host no local. Verifican conversión de un expediente ficticio, restricciones, nueva atención y triggers reales.

- [Conversores de valores de EF Core y sus límites](https://learn.microsoft.com/en-us/ef/core/modeling/value-conversions).
- [AES-GCM en .NET](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aesgcm).
- [Triggers de PostgreSQL](https://www.postgresql.org/docs/16/sql-createtrigger.html).
- [Privilegios de PostgreSQL](https://www.postgresql.org/docs/17/ddl-priv.html).
