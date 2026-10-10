-- Ejecutar en la base LOCAL zuni. Solo cuentas de prueba y datos ficticios.
-- Crea dos citas confirmadas de 50 minutos: ayer y anteayer.
-- No cambia las fechas de las citas existentes ni completa atención automáticamente.
BEGIN;
DO $$
DECLARE
    alumno text;
    profesional text;
    registro record;
    fecha date;
BEGIN
    IF current_database() <> 'zuni' OR inet_server_addr() IS NULL OR inet_server_addr() NOT IN ('127.0.0.1'::inet,'::1'::inet) THEN
        RAISE EXCEPTION 'Este script requiere la base zuni en PostgreSQL local.';
    END IF;
    SELECT u."Id" INTO alumno FROM "AspNetUsers" u
    WHERE u."NormalizedEmail"='ESTUDIANTE.PRUEBA@MIUMG.EDU.GT' AND u."IsActive"
      AND EXISTS (SELECT 1 FROM "AspNetUserRoles" ur JOIN "AspNetRoles" r ON r."Id"=ur."RoleId"
                  WHERE ur."UserId"=u."Id" AND r."NormalizedName"='ESTUDIANTE');
    SELECT u."Id" INTO profesional FROM "AspNetUsers" u
    WHERE u."NormalizedEmail"='PSICOLOGO.PRUEBA@MIUMG.EDU.GT' AND u."IsActive"
      AND EXISTS (SELECT 1 FROM "AspNetUserRoles" ur JOIN "AspNetRoles" r ON r."Id"=ur."RoleId"
                  WHERE ur."UserId"=u."Id" AND r."NormalizedName"='PSICOLOGO');
    IF alumno IS NULL OR profesional IS NULL THEN
        RAISE EXCEPTION 'Falta una cuenta de prueba activa con su rol correspondiente.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM "AsignacionesEstudiantePsicologo" a JOIN "PerfilesEstudiante" p ON p."Id"=a."PerfilEstudianteId"
                   WHERE p."UsuarioId"=alumno AND p."Activo" AND a."PsicologoUsuarioId"=profesional AND a."FechaFinalizacionUtc" IS NULL) THEN
        RAISE EXCEPTION 'Las cuentas de prueba deben tener un vínculo vigente.';
    END IF;
    PERFORM pg_advisory_xact_lock(803501504);
    FOR registro IN SELECT * FROM (VALUES
        ('b7394100-0000-4000-8000-000000000001'::uuid,'b7394200-0000-4000-8000-000000000001'::uuid,1),
        ('b7394100-0000-4000-8000-000000000002'::uuid,'b7394200-0000-4000-8000-000000000002'::uuid,2)
    ) AS demo(id,eventoid,dias) LOOP
        IF EXISTS (SELECT 1 FROM "Citas" WHERE "Id"=registro.id AND ("EstudianteId"<>alumno OR "PsicologoId"<>profesional OR NOT "PruebaLocal")) THEN
            RAISE EXCEPTION 'Identificador ocupado por otra cuenta o entorno; no se modifica.';
        END IF;
        -- En una segunda ejecución conserva fecha, avances y atención ya registrada.
        IF EXISTS (SELECT 1 FROM "Citas" WHERE "Id"=registro.id) THEN CONTINUE; END IF;
        fecha := (CURRENT_TIMESTAMP AT TIME ZONE 'America/Guatemala')::date - registro.dias;
        IF EXISTS (SELECT 1 FROM "Citas" WHERE "EstudianteId"=alumno AND "PruebaLocal" AND "Fecha"=fecha AND "Estado" IN (0,1,2,5)) THEN
            RAISE EXCEPTION 'Ya hay una cita del estudiante en %. No se modifica.',fecha;
        END IF;
        IF EXISTS (SELECT 1 FROM "Citas" WHERE "PsicologoId"=profesional AND "PruebaLocal" AND "Fecha"=fecha AND "Estado" IN (0,1)
                   AND "Inicio"<'09:50'::time AND "Fin">'09:00'::time) THEN
            RAISE EXCEPTION 'El profesional ya tiene una cita en el horario de prueba.';
        END IF;
        INSERT INTO "Citas" ("Id","EstudianteId","PsicologoId","HorarioOriginalId","Fecha","Inicio","Fin","InicioUtc","FinUtc",
            "DuracionVariable","PruebaLocal","Modalidad","Estado","Revision","SolicitudUtc","ResultadoClinicoPrivado","ResenaEstudiante",
            "ResultadoPublicable","RecomiendaProximaCita","IndicacionesProximaCita")
        VALUES (registro.id,alumno,profesional,registro.id,fecha,'09:00','09:50',
            (fecha+'09:00'::time) AT TIME ZONE 'America/Guatemala',(fecha+'09:50'::time) AT TIME ZONE 'America/Guatemala',
            false,true,'Presencial',1,1,CURRENT_TIMESTAMP,'','','',false,'');
        INSERT INTO "EventosCita" ("Id","CitaId","ActorId","Accion","Motivo","FechaUtc")
        VALUES (registro.eventoid,registro.id,profesional,'Demostración local','Cita pasada FICTICIA para probar asistencia y cierre; no corresponde a una atención real.',CURRENT_TIMESTAMP);
    END LOOP;
END $$;
COMMIT;
SELECT "Id","Fecha","Inicio","Fin","Estado" FROM "Citas"
WHERE "Id" IN ('b7394100-0000-4000-8000-000000000001','b7394100-0000-4000-8000-000000000002');
