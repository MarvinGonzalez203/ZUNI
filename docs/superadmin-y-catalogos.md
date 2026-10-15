# Acceso del proveedor y ajustes de administración

Entrega local del 15/10/2026 en feature/director-V2.

## SuperAdmin

Se agrega el rol SuperAdmin a la autenticación existente. El acceso inicial abre /SuperAdmin. El panel ofrece tarifas, Mi cuenta y, únicamente si la cuenta posee también Administrador, un enlace para administrar esta institución. No se conceden roles de Director, Psicólogo o Estudiante automáticamente.

La operación de preparación se ejecuta de manera manual por el operador del servidor:

```powershell
dotnet Zuni.dll --preparar-superadmin=true --correo proveedor@ejemplo.com
```

Genera una contraseña temporal aleatoria, guarda solo su hash y obliga a cambiarla en el primer acceso. Invalida sesiones y enlaces anteriores de recuperación. Si la cuenta ya es SuperAdmin, no modifica su contraseña. No se crea automáticamente ninguna cuenta al iniciar el servicio. El Administrador institucional no puede otorgar SuperAdmin, editar sus roles, desactivarlo ni restablecerle la contraseña; su titular gestiona sus datos desde Mi cuenta.

Para la solicitud local se preparó admin@gmail.com, conservando su rol Administrador existente. La contraseña no se guarda en archivos del proyecto. La separación de funciones se hace por roles; esta cuenta tiene ambos por petición de conservar el acceso actual.

No hay todavía alta central de instituciones, gestión de instalaciones remotas, contratos ni pasarela de cobros. El rol no incorpora acceso clínico por sí mismo. Los cuatro roles institucionales conservan sus restricciones.

## Pantallas

- Tarifas queda penúltima en el menú del Administrador; Configuración de la institución queda última y usa engranaje.
- Solo SuperAdmin ve PRECIO PROPUESTO. Las cinco columnas de beneficios corresponden a Administrador, Director, Psicólogo, Estudiante y funciones compartidas. Se adaptan a pantallas pequeñas.
- El avatar y nombre superiores enlazan a Mi cuenta en todos los paneles.
- El catálogo permite escribir un área académica y, en otra columna, una carrera por línea; conserva la importación avanzada plegada. No duplica carreras existentes.
- Los cinco colores tienen botones del mismo tamaño. Amarillo #F9DB5C y celeste #BBF1F8 sustituyen los accesos rápidos verde y azul. El color guardado afecta botones y pestañas seleccionadas con efecto cristal y contraste para ambos temas. Se conservan valores personalizados previos hasta que el Administrador guarde otro.
- Director ve las opciones en una escala horizontal de consulta; Estudiante conserva diez celdas táctiles, tres preguntas por vista y barra de progreso.
- Director puede eliminar una prueba personalizada sin asignaciones, con confirmación, revisión vigente y antiforgery. Los tres niveles definidos están protegidos; las pruebas con asignaciones conservan su historial.

## Operación sobre datos locales

Se respaldó ZuniDb antes de preparar el usuario y eliminar exclusivamente Daniel Chavarr, ID 9177fd79-d6d6-4a1a-8d78-18eb0f11c065. Era un borrador inactivo sin asignaciones. Los tres instrumentos conservan 15, 35 y 54 preguntas. No se modificaron respuestas, atenciones o consentimientos.

La entrega no necesita nuevas tablas ni migración de esquema. Las pruebas automatizadas usan SQLite y documentos ficticios; las visuales usan archivos locales sin acceder a la base real.
