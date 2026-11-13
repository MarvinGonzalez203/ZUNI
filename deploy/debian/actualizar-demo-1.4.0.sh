#!/bin/bash
# One-command update for the existing zuni.lat demo installation, not a new install.
set -Eeuo pipefail
umask 077
[[ $EUID -eq 0 ]] || { echo 'Ejecuta: sudo bash /home/marlon/zuni-subida/actualizacion-1.4.0/actualizar.sh'; exit 1; }
base=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)
[[ $base == /home/marlon/zuni-subida/actualizacion-1.4.0 ]] || { echo 'Ruta de carga inesperada.'; exit 1; }
exec 9>/run/lock/zuni-demo-despliegue.lock
flock -n 9 || { echo 'Ya hay una actualización en curso.'; exit 1; }
service=zuni-demo
current=/opt/zuni/demo/current
envfile=/etc/zuni/demo.env
persistent=/var/lib/zuni/demo
pg=/usr/lib/postgresql/18/bin
port=5433
database=zuni
baseline=20261111063900_PermitirAtencionProfesionalSinSolicitud
target=20261113061106_AgregarPinProfesional
stamp=$(date -u +%Y%m%dT%H%M%SZ)
stopped=0
changed=0
clone_created=0
backup=''
old_path=''
status() { printf '%s %s\n' "$(date -u +%FT%TZ)" "$1" | tee "$base/estado.txt"; chmod 644 "$base/estado.txt"; }
sql() { local db=$1; shift; runuser -u postgres -- "$pg/psql" -X -p "$port" -d "$db" -v ON_ERROR_STOP=1 "$@"; }
counts() { sql "$1" -Atq -c 'SELECT (SELECT count(*) FROM "AspNetUsers"), (SELECT count(*) FROM "PerfilesEstudiante"), (SELECT count(*) FROM "RegistrosAtencionCita")'; }
clone_cleanup() {
  if [[ $clone_created == 1 && $clone =~ ^zuni_verif_140_[0-9_]+$ && $clone != zuni ]]; then
    runuser -u postgres -- "$pg/dropdb" -p "$port" "$clone"
    clone_created=0
  fi
}
on_error() {
  local result=$? line=$1
  trap - ERR
  set +e
  status "ERROR en línea $line. Se conserva el respaldo y no se restaura ni borra la base viva."
  clone_cleanup
  if [[ $stopped == 1 ]]; then
    systemctl stop "$service"
    if [[ $changed == 1 && $old_path == /opt/zuni/demo/releases/* && -d $old_path ]]; then
      ln -s -- "$old_path" "$current.rollback-$$"
      mv -Tf -- "$current.rollback-$$" "$current"
    fi
    systemctl start "$service"
    if systemctl is-active --quiet "$service"; then status 'REVERSIÓN: servicio anterior arrancado. Revisar antes de reintentar.'; fi
  fi
  exit "$result"
}
trap 'on_error $LINENO' ERR
transient() {
  local label=$1; shift
  systemd-run --quiet --wait --pipe --collect --unit="zuni-demo-$label-$stamp-$$" \
    -p User=zuni-demo -p Group=zuni-demo -p "EnvironmentFile=$envfile" -p "WorkingDirectory=$current" \
    /usr/bin/dotnet "$@"
}
comprobar_migracion() {
  [[ $(sql "$1" -Atq -c 'SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 1') == "$target" ]]
  [[ $(sql "$1" -Atq -c "SELECT count(*) FROM information_schema.tables WHERE table_schema='public' AND table_name='PinesProfesionales'") == 1 ]]
  [[ $(sql "$1" -Atq -c "SELECT count(*) FROM information_schema.role_table_grants WHERE table_schema='public' AND table_name='PinesProfesionales' AND grantee='zuni_app' AND privilege_type IN ('SELECT','INSERT','UPDATE')") == 3 ]]
}

status 'PREPARACIÓN: comprobando paquete, servicio, versión y esquema.'
cd -- "$base"
sha256sum -c SHA256SUMS
[[ -s $envfile && -s $persistent/seguridad/claves.json && -d $persistent/dataprotection ]]
[[ $(systemctl show "$service" -p User --value) == zuni-demo ]]
[[ $(systemctl show "$service" -p WorkingDirectory --value) == "$current" ]]
systemctl is-active --quiet "$service"
[[ -L $current ]]
old_path=$(readlink -f -- "$current")
[[ $old_path == /opt/zuni/demo/releases/* && -s $old_path/release-info.json ]]
grep -Eq '"version": "1\.(3\.3|4\.0)"' "$old_path/release-info.json"
last=$(sql "$database" -Atq -c 'SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 1')
[[ $last == "$baseline" || $last == "$target" ]] || { status 'DETENIDO: historial de migraciones inesperado; no se cambia la instalación.'; exit 1; }
[[ $(sql "$database" -Atq -c "SELECT tableowner FROM pg_tables WHERE schemaname='public' AND tablename='__EFMigrationsHistory'") == zuni_migrador ]]
tar -tzf zuni-publicacion.tar.gz > "$base/contenido-paquete.txt"
if grep -Eq '(^/|(^|/)\.\.(/|$)|(^|/)(App_Data|secrets\.json|claves\.json|bitacora\.jsonl|\.local-security)(/|$))' "$base/contenido-paquete.txt"; then
  status 'DETENIDO: el paquete contiene una ruta excluida.'; exit 1
fi
install -d -m 755 -o root -g root /opt/zuni/demo/releases
install -d -m 700 -o root -g root /var/backups/zuni/demo
stage=$(mktemp -d /opt/zuni/demo/releases/1.4.0-5287a0d-XXXXXX)
tar --no-same-owner --no-same-permissions -xzf zuni-publicacion.tar.gz -C "$stage"
chown -R root:root "$stage"
chmod -R u=rwX,go=rX "$stage"
grep -Fq '"version": "1.4.0"' "$stage/release-info.json"
grep -Fq '"commit": "5287a0d' "$stage/release-info.json"
[[ -s $stage/Zuni.dll && ! -e $stage/App_Data && ! -e $stage/appsettings.Development.json ]]

status 'RESPALDO: deteniendo brevemente ZUNI para copiar base, claves y configuración consistentes.'
systemctl stop "$service"
stopped=1
backup=$(mktemp -d "/var/backups/zuni/demo/$stamp-pre-1.4.0-XXXXXX")
chmod 700 "$backup"
before=$(counts "$database")
runuser -u postgres -- "$pg/pg_dump" -p "$port" -Fc "$database" > "$backup/zuni.dump"
"$pg/pg_restore" --list "$backup/zuni.dump" > "$backup/indice-respaldo.txt"
tar -C /var/lib/zuni -czf "$backup/persistencia.tar.gz" demo
tar -C "$current" -czf "$backup/binarios.tar.gz" .
cp -a -- "$envfile" "$backup/demo.env"
cp -a -- /etc/systemd/system/zuni-demo.service "$backup/zuni-demo.service"
sha256sum "$persistent/seguridad/claves.json" > "$backup/claves-originales.sha256"
find "$persistent/dataprotection" -type f -name '*.xml' -exec sha256sum {} + >> "$backup/claves-originales.sha256"
(cd "$backup" && sha256sum zuni.dump persistencia.tar.gz binarios.tar.gz demo.env zuni-demo.service > SHA256SUMS)
tar -tzf "$backup/persistencia.tar.gz" >/dev/null
transient exportar-antes "$current/Zuni.dll" --exportar-seguridad > "$backup/seguridad-antes.log" 2>&1
transient verificar-antes "$current/Zuni.dll" --verificar-seguridad >> "$backup/seguridad-antes.log" 2>&1

status 'PRUEBA: restaurando el respaldo en una base temporal aislada y verificando la migración.'
clone="zuni_verif_140_$(date -u +%Y%m%d_%H%M%S)_$$"
runuser -u postgres -- "$pg/createdb" -p "$port" -O zuni_migrador "$clone"
clone_created=1
sql postgres -q -c "REVOKE ALL ON DATABASE \"$clone\" FROM PUBLIC;"
runuser -u postgres -- "$pg/pg_restore" -p "$port" -d "$clone" --exit-on-error --single-transaction --no-owner --no-privileges --role=zuni_migrador < "$backup/zuni.dump"
sql "$clone" -q -c 'SET ROLE zuni_migrador' -f - < "$base/migracion-pin.sql"
sql "$clone" -q -c 'SET ROLE zuni_migrador' -f - < "$base/permisos-pin.sql"
comprobar_migracion "$clone"
[[ $(counts "$clone") == "$before" ]]
clone_cleanup

status 'MIGRACIÓN: creando protección de PIN y permisos mínimos de ejecución.'
sql "$database" -q -c 'SET ROLE zuni_migrador' -f - < "$base/migracion-pin.sql"
sql "$database" -q -c 'SET ROLE zuni_migrador' -f - < "$base/permisos-pin.sql"
comprobar_migracion "$database"
[[ $(counts "$database") == "$before" ]]

status 'PUBLICACIÓN: activando 1.4.0 con la base y las claves originales.'
ln -s -- "$stage" "$current.next-$$"
mv -Tf -- "$current.next-$$" "$current"
changed=1
transient exportar-despues "$current/Zuni.dll" --exportar-seguridad > "$backup/seguridad-despues.log" 2>&1
transient verificar-despues "$current/Zuni.dll" --verificar-seguridad >> "$backup/seguridad-despues.log" 2>&1
sha256sum -c "$backup/claves-originales.sha256" > "$backup/verificacion-claves.log"
[[ $(counts "$database") == "$before" ]]
systemctl start "$service"
status 'COMPROBACIÓN: servicio, inicio, acceso, archivos estáticos y HTTPS.'
for attempt in {1..30}; do
  if curl --max-time 2 -fs -H 'Host: zuni.lat' -H 'X-Forwarded-Proto: https' http://127.0.0.1:5101/ -o /dev/null; then break; fi
  sleep 1
done
curl --max-time 20 -fsS -H 'Host: zuni.lat' -H 'X-Forwarded-Proto: https' http://127.0.0.1:5101/ -o /dev/null
curl --max-time 20 -fsS https://zuni.lat/ -o /dev/null
curl --max-time 20 -fsS https://zuni.lat/Cuenta/IniciarSesion -o /dev/null
curl --max-time 20 -fsS https://zuni.lat/css/agenda-citas.css -o /dev/null
curl --max-time 20 -fsS https://zuni.lat/js/pin-profesional.js -o /dev/null
curl --max-time 20 -fsS https://zuni.lat/PinProfesional/Recuperar -o /dev/null
systemctl is-active --quiet "$service"
stopped=0
trap - ERR
printf '%s\n' "$backup" > "$base/respaldo-ruta.txt"
chmod 644 "$base/respaldo-ruta.txt"
status 'COMPLETADO: ZUNI 1.4.0 activo, HTTPS correcto, seguridad y claves verificadas, usuarios y atenciones conservados.'
echo "Respaldo privado conservado en: $backup"
