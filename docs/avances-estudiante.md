# Avances del módulo de Estudiante

## Alcance de esta entrega

Cambios limitados al módulo de Estudiante. La edición del perfil utiliza ahora
el tablero y mantiene seleccionada la sección Mi perfil. El formulario incluye
resumen de errores, validación de campos obligatorios, límites de longitud,
aviso del navegador al salir con cambios pendientes y bloqueo de un segundo
envío mientras se guarda. No se almacenan datos personales en localStorage.

Los tres bloques del carné y el teléfono son obligatorios en el modelo y el
formulario. Los teléfonos admiten ocho dígitos ASCII, coherentes con los
controles de entrada. Los datos académicos opcionales y el contacto de
emergencia siguen siendo opcionales. Se conserva la edición y persistencia
existente, sin cambios de esquema ni de otros módulos.

Se conservaron los avances locales existentes: indicador de progreso,
acciones rápidas, presentación móvil y redirección al tablero.

## Verificación reproducible

Desde la raíz del repositorio:

```powershell
dotnet build Zuni/Zuni.csproj --no-restore -p:NuGetAudit=false
dotnet run --project tests/StudentProfile.Checks -p:NuGetAudit=false
node --test tests/student-profile.test.cjs
```

Resultado de esta entrega: compilación con cero errores y advertencias,
15 comprobaciones de modelos y cuatro pruebas del comportamiento del script.
Las pruebas de JavaScript usan un entorno simulado: no sustituyen una prueba
de navegador con autenticación real. Las pruebas de modelos no acceden a la BD.

## Comprobación manual pendiente

- Entrar como estudiante y abrir Mi perfil → Editar mi perfil.
- Verificar menú, errores por campos vacíos y navegación por teclado.
- Modificar datos y salir sin guardar: comprobar advertencia del navegador.
- Guardar, comprobar confirmación, cerrar sesión y verificar persistencia.
- Revisar el formulario en móvil y el comportamiento con JavaScript desactivado.
- Comprobar rechazo de carné duplicado usando dos cuentas de prueba autorizadas.

## Dependencias pendientes

Evaluaciones, Resultados y Citas conservan sus estados de próxima disponibilidad.
Su funcionamiento completo requiere instrumentos oficiales y la integración con
los módulos compartidos. La vinculación al psicólogo continúa siendo la propuesta
de `integracion-estudiante-psicologo.md`, no una función implementada.
