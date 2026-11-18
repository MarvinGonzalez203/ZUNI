// Disposable PostgreSQL 18 lab only; never accepts a production endpoint or database.
import {spawn} from 'node:child_process';import {readFile,writeFile,mkdtemp,mkdir} from 'node:fs/promises';import path from 'node:path';import {fileURLToPath} from 'node:url';import {randomBytes} from 'node:crypto';import assert from 'node:assert/strict';
const repo=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..'),pg=process.env.DENIS_PG_BINDIR||'C:/Program Files/PostgreSQL/18/bin',dotnet=process.env.DENIS_DOTNET||path.join(repo,'.verification-sdk/dotnet10/dotnet.exe'),tool=path.join(repo,'tools/Despliegue/bin/Release/net10.0/Zuni.Despliegue.dll');
const suffix=randomBytes(5).toString('hex'),owner='deploy_owner_'+suffix,app='deploy_app_'+suffix,database='zuni_seguridad_prueba_deploy162_'+suffix,failedDb=database+'_atomic';
const created=[],dir=await mkdtemp(path.join(repo,'.visual-check/deploy-pg-')),connection=['-h','127.0.0.1','-p','57420','-U','auditoria'];let rolesCreated=false,checks=0;
function run(exe,args,input){return new Promise((resolve,reject)=>{const child=spawn(exe,args,{windowsHide:true,stdio:['pipe','pipe','pipe']}),chunks=[],errors=[];child.stdout.on('data',x=>chunks.push(x));child.stderr.on('data',x=>errors.push(x));child.on('error',reject);child.on('close',code=>{const stdout=Buffer.concat(chunks).toString(),stderr=Buffer.concat(errors).toString();if(code===0)resolve(stdout.trim());else reject(Object.assign(new Error('Fixture command failed'),{code,stderr}));});child.stdin.end(input);});}
const executable=name=>path.join(pg,name+(process.platform==='win32'?'.exe':''));
const sql=(db,text)=>run(executable('psql'),[...connection,'-X','-Atq','-v','ON_ERROR_STOP=1','-v','VERBOSITY=sqlstate','-d',db],text);
const check=(value,message)=>{assert.ok(value,message);checks++;};
const migrate=(db,files)=>run(executable('psql'),[...connection,'-X','-q','-1','-v','ON_ERROR_STOP=1','-d',db,'-c','SET ROLE '+owner,...files.flatMap(file=>['-f',file])]);
async function rejected(db,text,state){let blocked=false;try{await sql(db,text);}catch(error){blocked=error.stderr.includes(state);}check(blocked,'Expected SQLSTATE '+state);}
try{
 const location=(await sql('postgres','SHOW data_directory')).replaceAll('\\','/').toLowerCase();check(location===path.join(repo,'.visual-check/remediacion-01-06/pg-upgrade/data').replaceAll('\\','/').toLowerCase(),'Refuse non-lab PostgreSQL');
 check(await sql('postgres','SELECT current_user')==='auditoria','Known fixture owner');
 await run(dotnet,[tool,'sql-base-prueba',path.join(dir,'baseline.sql')]);await run(dotnet,[tool,'sql',path.join(dir,'migration.sql')]);
 const migration=await readFile(path.join(dir,'migration.sql'),'utf8');check(!/^\s*(?:COMMIT|START TRANSACTION|BEGIN)\s*;/mi.test(migration),'Single outer transaction only');
 await sql('postgres',`BEGIN;CREATE ROLE ${owner} NOLOGIN;CREATE ROLE ${app} NOLOGIN;COMMIT;`);rolesCreated=true;
 for(const db of [database,failedDb]){assert.match(db,/^zuni_seguridad_prueba_deploy162_[a-f0-9]{10}(?:_atomic)?$/);await run(executable('createdb'),[...connection,'-O',owner,db]);created.push(db);await migrate(db,[path.join(dir,'baseline.sql')]);}
 await sql(database,`SET ROLE ${owner};
 INSERT INTO "AspNetUsers" ("Id","FullName","CreatedAtUtc","IsActive","EmailConfirmed","PhoneNumberConfirmed","TwoFactorEnabled","LockoutEnabled","AccessFailedCount") VALUES ('deploy-student','Persona ficticia',now(),true,false,false,false,true,0),('deploy-psychologist','Profesional ficticio',now(),true,false,false,false,true,0);
 INSERT INTO "PerfilesEstudiante" ("Id","UsuarioId","Carne","Carrera","FechaCreacionUtc","Activo") VALUES ('11111111-1111-4111-8111-111111111111','deploy-student','9999-26-123456','Solo laboratorio',now(),true);
 INSERT INTO "Citas" ("Id","PerfilEstudianteId","PsicologoUsuarioId","FechaHoraInicioUtc","FechaHoraFinUtc","Modalidad","Estado","FechaCreacionUtc","EsImprevista") VALUES ('22222222-2222-4222-8222-222222222222','11111111-1111-4111-8111-111111111111','deploy-psychologist','2026-11-20T15:00:00Z','2026-11-20T16:00:00Z',0,6,now(),true);`);
 const fingerprint=`SELECT (SELECT md5(string_agg(row_to_json(u)::text,'|' ORDER BY "Id")) FROM "AspNetUsers" u),(SELECT count(*) FROM "PerfilesEstudiante"),(SELECT "Estado" FROM "Citas" WHERE "Id"='22222222-2222-4222-8222-222222222222')`;
 const before=await sql(database,fingerprint);
 const permissions=(await readFile(path.join(repo,'deploy/debian/permisos-1.6.2.sql'),'utf8')).replaceAll('zuni_app',app);await writeFile(path.join(dir,'permissions.sql'),permissions);
 await migrate(database,[path.join(dir,'migration.sql'),path.join(dir,'permissions.sql')]);check(await sql(database,fingerprint)===before,'Users, profile and legacy appointment unchanged');
 check(await sql(database,'SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 1')==='20261117015815_AgregarDobleFactorYAusencias','Latest migration');
 await migrate(database,[path.join(dir,'migration.sql'),path.join(dir,'permissions.sql')]);check(await sql(database,fingerprint)===before,'Idempotent second application preserves data');
 for(const table of ['PinesProfesionales','DobleFactor']){
  check(await sql(database,`SELECT has_table_privilege('${app}','public."${table}"','SELECT'),has_table_privilege('${app}','public."${table}"','INSERT'),has_table_privilege('${app}','public."${table}"','UPDATE'),has_table_privilege('${app}','public."${table}"','DELETE')`)==='t|t|t|f','Minimal rights '+table);
 }
 await rejected(database,`SET ROLE ${app};INSERT INTO "DobleFactor" ("UsuarioId","CodigosHash","UltimoPaso","Fallos","Recordar","Revision") VALUES ('missing-user','[]',-1,0,false,'33333333-3333-4333-8333-333333333333');`,'23503');
 await sql(database,`SET ROLE ${owner};UPDATE "Citas" SET "Estado"=7 WHERE "Id"='22222222-2222-4222-8222-222222222222';`);
 check(await sql(database,'SELECT "Estado" FROM "Citas"')==='7','New absence state accepted');
 await rejected(database,`SET ROLE ${owner};UPDATE "Citas" SET "SustituyeCitaId"='44444444-4444-4444-8444-444444444444';`,'23503');
 await sql(database,`SET ROLE ${owner};UPDATE "AspNetUsers" SET "TwoFactorEnabled"=true WHERE "Id"='deploy-student';`);
 check(await sql(database,'SELECT EXISTS(SELECT 1 FROM "AspNetUsers" WHERE "TwoFactorEnabled") OR EXISTS(SELECT 1 FROM "Citas" WHERE "Estado"=7 OR "SustituyeCitaId" IS NOT NULL)')==='t','Rollback to pre-2FA version must be blocked');
 const failure=path.join(dir,'failure.sql');await writeFile(failure,'SELECT 1/0;\n');let aborted=false;try{await migrate(failedDb,[path.join(dir,'migration.sql'),failure]);}catch{aborted=true;}check(aborted,'Injected error aborts transaction');
 check(await sql(failedDb,`SELECT to_regclass('public."DobleFactor"') IS NULL,to_regclass('public."PinesProfesionales"') IS NULL`)==='t|t','No partial new tables after rollback');
 check(await sql(failedDb,`SELECT count(*) FROM information_schema.columns WHERE table_schema='public' AND table_name='Citas' AND column_name='SustituyeCitaId'`)==='0','No partial column after rollback');
 check(await sql(failedDb,'SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 1')==='20261111063900_PermitirAtencionProfesionalSinSolicitud','History rolled back with DDL');
 const result={checks,fixtureOnly:true,productionTouched:false,atomicRollback:true,idempotent:true,legacyDataPreserved:true,minimumNewPermissions:true};await writeFile(path.join(repo,'.visual-check/despliegue-postgres-result.json'),JSON.stringify(result,null,2));console.log(JSON.stringify(result));
}finally{
 for(const db of created.reverse()){assert.ok([database,failedDb].includes(db));await run(executable('dropdb'),[...connection,db]);}
 if(rolesCreated)await sql('postgres',`DROP ROLE ${app};DROP ROLE ${owner};`);
}
