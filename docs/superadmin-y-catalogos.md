# Acceso del proveedor y ajustes de administración

Entrega local del 15/10/2026 en feature/director-V2.

## SuperAdmin

Se agrega el rol SuperAdmin a la autenticación existente. El acceso inicial abre /SuperAdmin. El panel ofrece tarifas, edición de precios y Mi cuenta. No ofrece accesos ni alertas institucionales. Una cuenta SuperAdmin no puede combinarse con roles institucionales: el comando de preparación lo rechaza y las rutas comerciales lo comprueban contra la base. Un middleware bloquea el acceso institucional incluso si se combinan roles por error.

La operación de preparación se ejecuta de manera manual por el operador del servidor:

```powershell
dotnet Zuni.dll --preparar-superadmin=true --correo proveedor@ejemplo.com
```

Genera una contraseña temporal aleatoria, guarda solo su hash y obliga a cambiarla en el primer acceso. Invalida sesiones y enlaces anteriores de recuperación. Si la cuenta ya es SuperAdmin, no modifica su contraseña. No se crea automáticamente ninguna cuenta al iniciar el servicio. El Administrador institucional no puede otorgar SuperAdmin, editar sus roles, desactivarlo ni restablecerle la contraseña; su titular gestiona sus datos desde Mi cuenta.

Por corrección del usuario, superadmin@gmail.com tiene exclusivamente SuperAdmin y una contraseña temporal con cambio obligatorio. admin@gmail.com conserva exclusivamente Administrador y su contraseña actual. Se invalidaron sus sesiones anteriores al retirar el privilegio del proveedor. Ninguna contraseña se guarda en archivos del proyecto.

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

La corrección incorpora la migración aditiva 20261015215836_AddConfiguracionTarifasComerciales, aplicada con respaldo previo. Los precios se guardan en ConfiguracionTarifas, con importe positivo de dos decimales, revisión de concurrencia y auditoría. Solo el proveedor puede editarlos; Administrador consulta los importes actualizados. Los ahorros se calculan contra la tarifa mensual vigente, no contra un valor fijo. No se alteran contratos ni se generan cargos. Las pruebas automatizadas usan SQLite y documentos ficticios; las visuales usan archivos locales sin acceder a la base real.
