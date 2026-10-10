-- SOLO PRUEBAS LOCALES EN zuni. Ejecutar completo en Query Tool de pgAdmin.
-- 10 cuentas ficticias: estudiante.demo01@miumg.edu.gt ... estudiante.demo10@miumg.edu.gt
-- Contraseña inicial de prueba para todas: ZuniPrueba!2026
-- Los hashes se generaron con PasswordHasher<ApplicationUser> de ASP.NET Identity.
-- Repetir conserva las cuentas, contraseñas, perfiles y avances existentes.
-- No crea respuestas, resultados ni consentimientos Big Five.
BEGIN;
DO $seed$
DECLARE
    psicologo_correo text := 'psicologo.prueba@miumg.edu.gt';
    psicologo_id text;
    rol_id text;
    r record;
    usuario_id text;
    perfil_id uuid;
    vinculo_id uuid;
    correo text;
    carne text;
BEGIN
    IF current_database() <> 'zuni' THEN
        RAISE EXCEPTION 'Selecciona la base local zuni antes de ejecutar este script.';
    END IF;
    PERFORM pg_advisory_xact_lock(803501503);
    SELECT u."Id" INTO STRICT psicologo_id
    FROM "AspNetUsers" u
    JOIN "AspNetUserRoles" ur ON ur."UserId"=u."Id"
    JOIN "AspNetRoles" ro ON ro."Id"=ur."RoleId"
    WHERE u."NormalizedEmail"=upper(psicologo_correo) AND u."IsActive" AND ro."NormalizedName"='PSICOLOGO';
    SELECT "Id" INTO STRICT rol_id FROM "AspNetRoles" WHERE "NormalizedName"='ESTUDIANTE';

    FOR r IN SELECT * FROM (VALUES
        (1,'Ana Demo','Ingeniería en Sistemas','1','AQAAAAIAAYagAAAAENyqOEFRGaqiQh64QkDopPgvRURRDL/kB9Js9EVpdEinJ+QZYxNT+qIun6sTHyIlKA=='),
        (2,'Luis Demo','Psicología','2','AQAAAAIAAYagAAAAENB8m6jiC70jyAeAj9hAyfe9RSw415gK/6Fa9szZehTkuEtLEIctRlg2VHfVKYE0yA=='),
        (3,'Sofía Demo','Administración de Empresas','3','AQAAAAIAAYagAAAAEIrSIZOp+aaixoW6fxHmf4SgMaWRmcVz9uutJbE8f5eVjpgJHD0OswpqI2b/NWPqDA=='),
        (4,'Carlos Demo','Ingeniería en Sistemas','4','AQAAAAIAAYagAAAAEDzVuwZPD8OsVH1lyyl35iLceSOc/zH0Nb4yuAGwVOjFKjBB1txVkbiyU67l08k+ZQ=='),
        (5,'María Demo','Psicología','5','AQAAAAIAAYagAAAAEAI7y15e1yv9UgRwDOsOHRx9rAoX5GDquxnzrxOlcAqlYRuCOeRFOAtOVOgZVWODsw=='),
        (6,'Diego Demo','Administración de Empresas','6','AQAAAAIAAYagAAAAENNW5ls2zHFsblkhZy/3SVpgVYIMB7dG0sjfK34HhdN0D5w7fZBDQF0cnksGH9ZEAg=='),
        (7,'Lucía Demo','Ingeniería en Sistemas','7','AQAAAAIAAYagAAAAEBb3KFyQwNMTtTu6sOLe2b9qwKXpS+Kgbp6PggY09QZFotE4rgG/o9eq4OqzZVlXxg=='),
        (8,'José Demo','Psicología','8','AQAAAAIAAYagAAAAEFhJ511RZWpUVBJ/PVZB7BmC1ahJZNhu8JEUdptPTRwt6F+03yJoe/1B7TYnImWoTg=='),
        (9,'Valeria Demo','Administración de Empresas','1','AQAAAAIAAYagAAAAEO8C5Wj7nB0PcuXPeqAnSt5veNinXnGrREDaCu7s9o4F59m9i5dtqxbuW9n+eFk4xw=='),
        (10,'Andrés Demo','Ingeniería en Sistemas','2','AQAAAAIAAYagAAAAEFZZO1KDwMqri7i071QcnCVVfm/1iVbMySfSbHLpAWbCJoVF8bAjr0y71v44VJGbpQ==')
    ) AS datos(numero,nombre,carrera,semestre,password_hash)
    LOOP
        usuario_id := 'zuni-demo-estudiante-' || lpad(r.numero::text,2,'0');
        correo := 'estudiante.demo' || lpad(r.numero::text,2,'0') || '@miumg.edu.gt';
        carne := (20269000+r.numero)::text;
        perfil_id := ('a4416100-0000-4000-8000-' || lpad(r.numero::text,12,'0'))::uuid;
        vinculo_id := ('a4416200-0000-4000-8000-' || lpad(r.numero::text,12,'0'))::uuid;
        IF EXISTS(SELECT 1 FROM "AspNetUsers" WHERE ("NormalizedEmail"=upper(correo) OR "NormalizedUserName"=upper(correo)) AND "Id"<>usuario_id)
            OR EXISTS(SELECT 1 FROM "AspNetUsers" WHERE "Id"=usuario_id AND ("NormalizedEmail" IS DISTINCT FROM upper(correo) OR "NormalizedUserName" IS DISTINCT FROM upper(correo))) THEN
            RAISE EXCEPTION 'Conflicto con la cuenta %. No se alteró ningún usuario ajeno.',correo;
        END IF;
        INSERT INTO "AspNetUsers" ("Id","FullName","CreatedAtUtc","IsActive","DebeCambiarContrasena",
            "UserName","NormalizedUserName","Email","NormalizedEmail","EmailConfirmed","PasswordHash",
            "SecurityStamp","ConcurrencyStamp","PhoneNumberConfirmed","TwoFactorEnabled","LockoutEnabled","AccessFailedCount")
        VALUES(usuario_id,'PRUEBA LOCAL · ' || r.nombre,now(),true,false,correo,upper(correo),correo,upper(correo),false,r.password_hash,
            md5(random()::text || clock_timestamp()::text),md5(random()::text || clock_timestamp()::text),false,false,true,0)
        ON CONFLICT("Id") DO NOTHING;

        INSERT INTO "AspNetUserRoles"("UserId","RoleId") VALUES(usuario_id,rol_id) ON CONFLICT DO NOTHING;
        IF EXISTS(SELECT 1 FROM "PerfilesEstudiante" WHERE ("Carne"=carne OR "UsuarioId"=usuario_id) AND "Id"<>perfil_id)
            OR EXISTS(SELECT 1 FROM "PerfilesEstudiante" WHERE "Id"=perfil_id AND ("UsuarioId"<>usuario_id OR "Carne"<>carne)) THEN
            RAISE EXCEPTION 'Conflicto con el perfil/carné %. Se revierte el lote.',carne;
        END IF;
        INSERT INTO "PerfilesEstudiante"("Id","UsuarioId","Carne","Carrera","Semestre","Telefono","FechaCreacionUtc","Activo")
        VALUES(perfil_id,usuario_id,carne,r.carrera,r.semestre,'00000000',now(),true)
        ON CONFLICT("Id") DO NOTHING;
        IF EXISTS(SELECT 1 FROM "AsignacionesEstudiantePsicologo" WHERE "PerfilEstudianteId"=perfil_id AND "FechaFinalizacionUtc" IS NULL AND "PsicologoUsuarioId"<>psicologo_id) THEN
            RAISE EXCEPTION 'El estudiante % ya tiene otro psicólogo. No se cambia su vínculo.',correo;
        END IF;
        IF NOT EXISTS(SELECT 1 FROM "AsignacionesEstudiantePsicologo" WHERE "PerfilEstudianteId"=perfil_id AND "FechaFinalizacionUtc" IS NULL) THEN
            IF EXISTS(SELECT 1 FROM "AsignacionesEstudiantePsicologo" WHERE "Id"=vinculo_id) THEN
                RAISE EXCEPTION 'El vínculo inicial de % terminó anteriormente; no se reactiva automáticamente.',correo;
            END IF;
            INSERT INTO "AsignacionesEstudiantePsicologo"("Id","PerfilEstudianteId","PsicologoUsuarioId","FechaAsignacionUtc","FechaFinalizacionUtc")
            VALUES(vinculo_id,perfil_id,psicologo_id,now(),NULL);
        END IF;
    END LOOP;
END $seed$;
COMMIT;

SELECT u."FullName" AS estudiante,u."Email" AS correo,p."Carne" AS carne,p."Carrera" AS carrera,
    psicologo."Email" AS psicologo
FROM "AspNetUsers" u JOIN "PerfilesEstudiante" p ON p."UsuarioId"=u."Id"
JOIN "AsignacionesEstudiantePsicologo" v ON v."PerfilEstudianteId"=p."Id" AND v."FechaFinalizacionUtc" IS NULL
JOIN "AspNetUsers" psicologo ON psicologo."Id"=v."PsicologoUsuarioId"
WHERE u."Id" IN (SELECT 'zuni-demo-estudiante-' || lpad(n::text,2,'0') FROM generate_series(1,10) n)
ORDER BY u."Email";
