#!/bin/bash
# Existing zuni.lat installation only. Default is a read-only service/database preflight.
set -Eeuo pipefail
umask 077
mode=${1:---comprobar}
[[ $mode == --comprobar || $mode == --aplicar ]] || { echo 'Uso: actualizar.sh --comprobar | --aplicar'; exit 2; }
[[ $EUID == 0 ]] || { echo 'Ejecuta con sudo desde MobaXterm.'; exit 2; }
base=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)
service=zuni-demo
current=/opt/zuni/demo/current
persistent=/var/lib/zuni/demo
envfile=/etc/zuni/demo.env
pg=/usr/lib/postgresql/18/bin
port=5433
database=zuni
target=20261117015815_AgregarDobleFactorYAusencias
stamp=$(date -u +%Y%m%d_%H%M%S)
work='' stage='' backup='' old_path='' clone='' clone_state='' transient_unit=''
clone_created=0 stopped=0 changed=0 migrated=0
status() { printf '%s %s\n' "$(date -u +%FT%TZ)" "$1"; }
fail() { status "DETENIDO: $1" >&2; return 1; }
sql() { local db=$1; shift; runuser -u postgres -- env -u PGSERVICE -u PGOPTIONS -u PGPASSFILE "$pg/psql" -X -h /var/run/postgresql -p "$port" -U postgres -d "$db" -v ON_ERROR_STOP=1 "$@"; }
pgtool() { local name=$1; shift; runuser -u postgres -- env -u PGSERVICE -u PGOPTIONS -u PGPASSFILE "$pg/$name" -h /var/run/postgresql -p "$port" -U postgres "$@"; }
transient() {
    local label=$1 directory=$2; shift 2
    transient_unit="zuni-demo-deploy-$label-$stamp-$$"
    systemd-run --quiet --wait --pipe --collect --unit="$transient_unit" \
      -p User=zuni-demo -p Group=zuni-demo -p UMask=0077 -p Environment=ASPNETCORE_ENVIRONMENT=Production -p "EnvironmentFile=$envfile" -p "WorkingDirectory=$directory" /usr/bin/dotnet "$@"
    transient_unit=''
}
counts() { sql "$1" -Atq -c 'SELECT (SELECT count(*) FROM "AspNetUsers"),(SELECT count(*) FROM "PerfilesEstudiante"),(SELECT count(*) FROM "Citas"),(SELECT count(*) FROM "RegistrosAtencionCita")'; }
old_can_run() {
    local version
    version=$(python3 -c 'import json,sys;print(json.load(open(sys.argv[1]))["version"])' "$old_path/release-info.json")
    case "$version" in
      1.5.*|1.6.*) return 0;;
      1.3.3|1.4.0)
        if [[ $version == 1.3.3 && $(sql "$database" -Atq -c 'SELECT to_regclass($$public."PinesProfesionales"$$) IS NOT NULL') == t ]]; then
            [[ $(sql "$database" -Atq -c 'SELECT EXISTS(SELECT 1 FROM "PinesProfesionales" WHERE "HashProtegido" IS NOT NULL)') == f ]] || return 1
        fi
        [[ $(sql "$database" -Atq -c 'SELECT EXISTS(SELECT 1 FROM "AspNetUsers" WHERE "TwoFactorEnabled") OR EXISTS(SELECT 1 FROM "Citas" WHERE "Estado"=7)') == f ]] || return 1
        if [[ $(sql "$database" -Atq -c 'SELECT EXISTS(SELECT 1 FROM information_schema.columns WHERE table_schema=$$public$$ AND table_name=$$Citas$$ AND column_name=$$SustituyeCitaId$$)') == t ]]; then
            [[ $(sql "$database" -Atq -c 'SELECT EXISTS(SELECT 1 FROM "Citas" WHERE "SustituyeCitaId" IS NOT NULL)') == f ]] || return 1
        fi
        return 0;;
      *) return 1;;
    esac
}
cleanup_clone() {
    if [[ -n $transient_unit ]]; then
        if ! systemctl stop "$transient_unit" 2>/dev/null && systemctl is-active --quiet "$transient_unit"; then return 1; fi
    fi
    if [[ $clone_created == 1 ]]; then
        [[ $clone =~ ^zuni_verif_162_[0-9]{8}_[0-9]{6}_[0-9]+$ && $clone != "$database" ]] || return 1
        pgtool dropdb "$clone" || return 1
        clone_created=0
    fi
    if [[ -n $clone_state && -d $clone_state ]]; then
        [[ $clone =~ ^zuni_verif_162_[0-9]{8}_[0-9]{6}_[0-9]+$ && ! -L $clone_state && $(realpath -e -- "$clone_state") == "/var/lib/zuni/ensayos-demo/$clone" ]] || return 1
        [[ -f $clone_state/.zuni-deploy-owned && $(cat "$clone_state/.zuni-deploy-owned") == "$clone" ]] || return 1
        rm -rf --one-file-system -- "$clone_state"
    fi
}
on_error() {
    local code=$1 line=$2
    trap - ERR INT TERM
    set +e
    status "ERROR en línea $line. Respaldo conservado; la base viva no se restaura ni se borra."
    if [[ -n $backup && -n $clone_state && -d $clone_state ]]; then
        for log in http.log integridad.log; do [[ ! -f $clone_state/$log ]] || cp -- "$clone_state/$log" "$backup/ensayo-$log"; done
    fi
    cleanup_clone
    if [[ $stopped == 1 ]]; then
        systemctl stop "$service"
        if [[ $migrated == 0 ]] || old_can_run; then
            if [[ $changed == 1 ]]; then ln -s -- "$old_path" "$current.rollback-$$" && mv -Tf -- "$current.rollback-$$" "$current"; fi
            systemctl start "$service"
            if systemctl is-active --quiet "$service"; then status 'REVERSIÓN: versión anterior iniciada; datos originales conservados.'; fi
        else
            status 'MANTENIMIENTO: existen estados de PIN, 2FA o ausencias incompatibles con el código anterior. Se conserva el código nuevo y el respaldo. Requiere revisión; no se evaden protecciones ni se restaura una base automáticamente.'
        fi
    fi
    exit "$code"
}
trap 'on_error $? $LINENO' ERR
trap 'on_error 130 $LINENO' INT
trap 'on_error 143 $LINENO' TERM

status 'PREPARACIÓN: paquete, Debian 13, runtime, servicio y esquema; valores privados omitidos.'
for command in python3 sha256sum systemctl systemd-run runuser flock tar curl realpath find stat df du ss; do command -v "$command" >/dev/null; done
[[ $(. /etc/os-release; printf '%s:%s' "$ID" "$VERSION_ID") == debian:13 ]] || fail 'Este perfil requiere Debian 13.'
install -d -m 700 -o root -g root /run/zuni-despliegue
[[ ! -L /run/zuni-despliegue && $(stat -c %u /run/zuni-despliegue) == 0 ]]
exec 9>/run/zuni-despliegue/demo.lock
flock -n 9 || fail 'Hay otra operación de despliegue en curso.'
[[ -s $base/SHA256SUMS && -s $base/zuni-publicacion.tar.gz && -d $base/comprobador ]]
while IFS= read -r line; do
    [[ $line =~ ^[a-f0-9]{64}\ \ ([a-zA-Z0-9][a-zA-Z0-9_.-]*|comprobador/([a-zA-Z0-9][a-zA-Z0-9_.-]*/)*[a-zA-Z0-9][a-zA-Z0-9_.-]*)$ ]] || fail 'Manifiesto SHA inválido.'
    name=${line:66}
    [[ -f $base/$name && ! -L $base/$name ]] || fail 'Archivo ausente o enlace en la carga.'
done < "$base/SHA256SUMS"
(cd -- "$base" && sha256sum --check --quiet SHA256SUMS)
[[ -x /usr/bin/dotnet ]] || fail 'Instala ASP.NET Core Runtime 10 antes de continuar; consulta LEEME-MOBA.md.'
/usr/bin/dotnet --list-runtimes | grep -Eq '^Microsoft.AspNetCore.App 10\.0\.' || fail 'Falta ASP.NET Core Runtime 10; no se detuvo ZUNI.'
[[ -x $pg/psql && -s $envfile && ! -L $envfile && $(stat -c '%u:%a' "$envfile") == 0:600 ]] || fail 'Revisa el perfil PostgreSQL y el archivo de entorno privado.'
[[ -d $persistent && ! -L $persistent && -L $current ]]
[[ $(systemctl show "$service" -p User --value) == zuni-demo && $(systemctl show "$service" -p WorkingDirectory --value) == "$current" ]]
[[ $(systemctl show "$service" -p FragmentPath --value) == /etc/systemd/system/zuni-demo.service && -z $(systemctl show "$service" -p DropInPaths --value) ]] || fail 'Unidad o drop-ins diferentes del perfil revisado.'
exec_start=$(systemctl show "$service" -p ExecStart --value)
[[ $exec_start == *'/usr/bin/dotnet'* && $exec_start == *'/opt/zuni/demo/current/Zuni.dll'* ]] || fail 'ExecStart no utiliza el enlace de publicación esperado.'
systemctl is-active --quiet "$service"
old_path=$(readlink -f -- "$current")
[[ $old_path == /opt/zuni/demo/releases/* && -d $old_path && -s $old_path/release-info.json && -s $old_path/Zuni.runtimeconfig.json ]]
python3 - "$old_path/release-info.json" <<'PY'
import json,sys
v=json.load(open(sys.argv[1]))['version']
assert v in ['1.3.3','1.4.0','1.5.0','1.5.1','1.5.2','1.5.3','1.6.0','1.6.1','1.6.2'], 'Release anterior fuera de este perfil'
print('Versión anterior documentada:',v)
PY
python3 - "$old_path/Zuni.runtimeconfig.json" <<'PY' | while IFS= read -r pattern; do /usr/bin/dotnet --list-runtimes | grep -Eq "$pattern"; done
import json,sys
c=json.load(open(sys.argv[1]))['runtimeOptions']
for f in c.get('frameworks',[c.get('framework')]):
 if f: print('^'+f['name'].replace('.','\\.')+' '+'.'.join(f['version'].split('.')[:2]).replace('.','\\.')+'\\.')
PY
[[ $(sql "$database" -Atq -c 'SELECT tableowner FROM pg_tables WHERE schemaname=$$public$$ AND tablename=$$__EFMigrationsHistory$$') == zuni_migrador ]]
last=$(sql "$database" -Atq -c 'SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 1')
case "$last" in 20261111063900_PermitirAtencionProfesionalSinSolicitud|20261113061106_AgregarPinProfesional|20261117015815_AgregarDobleFactorYAusencias) ;; *) fail 'Historial de migraciones inesperado.';; esac
if find "$persistent" \( -type l -o \( ! -type f -a ! -type d \) \) -print -quit | grep -q .; then fail 'Persistencia con enlaces u objetos especiales: requiere revisión.'; fi
if command -v ss >/dev/null && ss -H -lnt 'sport = :15162' | grep -q .; then fail 'El puerto privado de ensayo está ocupado.'; fi
dbsize=$(sql "$database" -Atq -c 'SELECT pg_database_size(current_database())')
state_size=$(du -sb "$persistent" | cut -f1)
binary_size=$(du -sb "$old_path" | cut -f1)
required=$((dbsize * 4 + state_size * 3 + binary_size * 3 + 536870912))
for location in /opt/zuni/demo /var/lib/zuni /var/backups /var/lib/postgresql; do
    available=$(df -B1 --output=avail "$location" | tail -1 | tr -d ' ')
    ((available >= required)) || fail 'Falta espacio para respaldo, clon y publicación.'
done
work=$(mktemp -d /var/lib/zuni-despliegue-162.XXXXXX)
chown root:zuni-demo "$work"; chmod 750 "$work"
while IFS= read -r line; do name=${line:66};install -D -m 640 -o root -g zuni-demo "$base/$name" "$work/$name"; done < "$base/SHA256SUMS"
cp -- "$base/SHA256SUMS" "$work/SHA256SUMS"
find "$work" -type d -exec chown root:zuni-demo {} + -exec chmod 750 {} +
(cd -- "$work" && sha256sum --check --quiet SHA256SUMS)
/usr/bin/dotnet "$work/comprobador/Zuni.Despliegue.dll" validar-tar "$work/zuni-publicacion.tar.gz"
transient entorno "$current" "$work/comprobador/Zuni.Despliegue.dll" entorno
if [[ $mode == --comprobar ]]; then
    trap - ERR INT TERM
    status 'PREPARADO: no se detuvo el servicio ni se modificó la base. Para activar, ejecuta el mismo script con --aplicar.'
    exit 0
fi

install -d -m 755 -o root -g root /opt/zuni/demo/releases
install -d -m 700 -o root -g root /var/backups/zuni/demo
[[ ! -L /opt/zuni/demo/releases && ! -L /var/backups/zuni/demo && $(stat -c %u "$old_path") == 0 ]]
stage=$(mktemp -d /opt/zuni/demo/releases/1.6.2-XXXXXX)
tar --extract --gzip --file="$work/zuni-publicacion.tar.gz" --directory="$stage" --no-same-owner --no-same-permissions
chown -R root:root "$stage"; find "$stage" -type d -exec chmod 755 {} +; find "$stage" -type f -exec chmod 644 {} +
python3 - "$stage/release-info.json" <<'PY'
import json,sys
m=json.load(open(sys.argv[1]));assert m['version']=='1.6.2' and m['framework']=='net10.0' and m['excludedLocalPrivacyChanges'] is True
assert len(m['commit'])==40 and all(c in '0123456789abcdef' for c in m['commit'])
PY
[[ -s $stage/Zuni.dll && ! -e $stage/App_Data && ! -e $stage/appsettings.Development.json ]]
status 'RESPALDO: inicio de la ventana de mantenimiento; deteniendo ZUNI.'
stopped=1
systemctl stop "$service"
[[ $(sql "$database" -Atq -c 'SELECT count(*) FROM pg_stat_activity WHERE datname=current_database() AND usename=$$zuni_app$$') == 0 ]] || fail 'Otro proceso utiliza el rol de ejecución; no se aplicaron migraciones.'
backup=$(mktemp -d "/var/backups/zuni/demo/$stamp-pre-1.6.2-XXXXXX")
chmod 700 "$backup"
transient exportar-antes "$old_path" "$old_path/Zuni.dll" --exportar-seguridad > "$backup/integridad-antes.log" 2>&1
transient verificar-antes "$old_path" "$old_path/Zuni.dll" --verificar-seguridad >> "$backup/integridad-antes.log" 2>&1
before=$(counts "$database")
pgtool pg_dump -Fc "$database" > "$backup/zuni.dump"
"$pg/pg_restore" --list "$backup/zuni.dump" > "$backup/indice-respaldo.txt"
tar -C /var/lib/zuni -czf "$backup/persistencia.tar.gz" demo
tar -C "$old_path" -czf "$backup/binarios.tar.gz" .
cp -a -- "$envfile" "$backup/demo.env"
cp -a -- /etc/systemd/system/zuni-demo.service "$backup/zuni-demo.service"
sha256sum "$persistent/seguridad/claves.json" > "$backup/claves-originales.sha256"
find "$persistent/dataprotection" -type f -name '*.xml' -exec sha256sum {} + >> "$backup/claves-originales.sha256"
python3 - "$backup" "$old_path" "$stage" <<'PY'
import json,sys
json.dump({'previous':sys.argv[2],'candidate':sys.argv[3],'databaseRestoreAutomatic':False},open(sys.argv[1]+'/despliegue.json','w'),indent=2)
PY
(cd -- "$backup" && sha256sum zuni.dump persistencia.tar.gz binarios.tar.gz demo.env zuni-demo.service despliegue.json > SHA256SUMS)
tar -tzf "$backup/persistencia.tar.gz" >/dev/null
status 'ENSAYO: restauración aislada, migraciones y arranque privado con una copia de las claves.'
clone="zuni_verif_162_${stamp}_$$"
pgtool createdb -O zuni_migrador "$clone"
clone_created=1
sql postgres -q -c "REVOKE ALL ON DATABASE \"$clone\" FROM PUBLIC; GRANT CONNECT ON DATABASE \"$clone\" TO zuni_app;"
pgtool pg_restore -d "$clone" --exit-on-error --single-transaction --no-owner --no-privileges --role=zuni_migrador < "$backup/zuni.dump"
# Restore omits ACLs: recreate the existing restricted permissions inside the clone only.
# Root opens private package files; postgres receives SQL over stdin, without
# granting it access to the deployment directory. Keep one transaction per call.
sql "$clone" -q -1 -c 'SET ROLE zuni_migrador' -f - < "$work/permisos-clon.sql"
cat -- "$work/migraciones.sql" "$work/permisos-nuevos.sql" | sql "$clone" -q -1 -c 'SET ROLE zuni_migrador' -f -
[[ $(counts "$clone") == "$before" ]]
install -d -m 711 -o root -g root /var/lib/zuni/ensayos-demo
[[ ! -L /var/lib/zuni/ensayos-demo ]]
clone_state="/var/lib/zuni/ensayos-demo/$clone"
[[ ! -e $clone_state ]]
install -d -m 700 -o zuni-demo -g zuni-demo "$clone_state"
cp -a -- "$persistent/." "$clone_state/"
printf '%s\n' "$clone" > "$clone_state/.zuni-deploy-owned"
chown -R zuni-demo:zuni-demo "$clone_state"
transient ensayo "$stage" "$work/comprobador/Zuni.Despliegue.dll" ensayo "$stage" "$clone" "$clone_state" > "$backup/ensayo.log" 2>&1
cp -- "$clone_state/http.log" "$backup/ensayo-http.log"
cp -- "$clone_state/integridad.log" "$backup/ensayo-integridad.log"
cleanup_clone
status 'MIGRACIÓN: ensayo aprobado; aplicando únicamente PIN/2FA/ausencias pendientes y permisos mínimos.'
cat -- "$work/migraciones.sql" "$work/permisos-nuevos.sql" | sql "$database" -q -1 -c 'SET ROLE zuni_migrador' -f -
migrated=1
[[ $(counts "$database") == "$before" ]]
[[ $(sql "$database" -Atq -c 'SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 1') == "$target" ]]
transient verificar-nuevo "$stage" "$stage/Zuni.dll" --verificar-seguridad > "$backup/integridad-nueva.log" 2>&1
sha256sum --check "$backup/claves-originales.sha256" > "$backup/verificacion-claves.log"
status 'ACTIVACIÓN: nueva portada Denis Corp y ZUNI 1.6.2; direcciones de acceso existentes conservadas.'
changed=1
ln -s -- "$stage" "$current.next-$$"
mv -Tf -- "$current.next-$$" "$current"
systemctl start "$service"
for attempt in {1..45}; do
    if curl --max-time 2 -fs -H 'Host: zuni.lat' -H 'X-Forwarded-Proto: https' http://127.0.0.1:5101/ -o /dev/null; then break; fi
    sleep 1
done
for route in / /Cuenta/IniciarSesion /deniscorp/zuni /deniscorp/blog /deniscorp/contacto /css/deniscorp.css /js/pin-profesional.js; do
    code=$(curl --max-time 20 --fail --silent --show-error --write-out '%{http_code}' "https://zuni.lat$route" -o /dev/null)
    [[ $code == 200 ]] || fail 'Una ruta pública no devolvió HTTPS 200.'
done
curl --max-time 20 --max-filesize 2097152 -fsS https://zuni.lat/ -o "$backup/comprobacion-publica.html"
grep -Fq 'data-portal="denis-corp"' "$backup/comprobacion-publica.html"
systemctl is-active --quiet "$service"
sha256sum --check "$backup/claves-originales.sha256" >> "$backup/verificacion-claves.log"
stopped=0
trap - ERR INT TERM
printf '%s\n' "$backup" > "$work/respaldo-ruta.txt"
status 'COMPLETADO: ZUNI 1.6.2 y portada Denis Corp activos, HTTPS y claves comprobados. No se activaron aplicaciones adicionales.'
echo "Respaldo privado: $backup"
echo "Registro del paquete: $work"
