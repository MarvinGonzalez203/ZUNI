# Crear diez estudiantes ficticios

Archivo: `tools/sql/crear-estudiantes-prueba.sql`.

1. En pgAdmin, selecciona la base local **zuni** y abre **Query Tool**.
2. Abre el archivo SQL y ejecuta todo el contenido con **F5**.
3. El resultado final lista los diez estudiantes y su psicólogo.
4. En ZUNI entra como psicólogo y abre **Estudiantes y seguimiento**. Actualiza la página si ya estaba abierta.

Las cuentas son `estudiante.demo01@miumg.edu.gt` hasta `estudiante.demo10@miumg.edu.gt`. Su contraseña inicial de prueba es `ZuniPrueba!2026`. Los carnés son 20269001 a 20269010. Los nombres llevan PRUEBA LOCAL, los datos son ficticios y el teléfono es un marcador 00000000. Las cuentas se vinculan a `psicologo.prueba@miumg.edu.gt`, que debe existir como profesional activo. La base debe tener sus migraciones aplicadas y el rol Estudiante existente. El correo del psicólogo se puede editar al comienzo del SQL.

Los hashes incluidos se generaron y verificaron con el PasswordHasher de ASP.NET Identity usado por la aplicación. El script inserta usuarios, roles, perfiles y vínculos dentro de una transacción. Si encuentra un correo o carné ocupado por otra cuenta, revierte el lote. No cambia cuentas previas ni mueve vínculos a otro profesional. Ejecutarlo otra vez no duplica los registros, no reinicia contraseñas ni cambia avances.

Los diez estudiantes empiezan sin resultado Big Five disponible. Para probar finalización y filtros, inicia sesión con algunas cuentas, revisa y acepta el consentimiento local y completa su cuestionario con respuestas ficticias. El script no registra consentimientos en nombre de nadie ni genera resultados. No añade citas porque su lógica todavía está pendiente.

Se verificó el SQL en una base PostgreSQL temporal: diez cuentas, roles, perfiles y vínculos; contraseñas compatibles; segunda ejecución sin duplicados ni cambios de contraseña; cero participaciones Big Five. La base temporal se eliminó. **No se ejecutó la carga en zuni.**
