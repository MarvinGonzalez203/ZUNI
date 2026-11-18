#!/bin/bash
# Code rollback only. Never drops/restores the live database or replaces keys.
set -Eeuo pipefail
umask 077
[[ $EUID == 0 && $# == 2 && $2 == --aplicar ]] || { echo 'Uso: sudo bash revertir.sh /var/backups/zuni/demo/<respaldo> --aplicar'; exit 2; }
backup=$(realpath -e -- "$1")
[[ $backup =~ ^/var/backups/zuni/demo/[0-9]{8}_[0-9]{6}-pre-1\.6\.2-[A-Za-z0-9]+$ && ! -L $1 && $(stat -c %u "$backup") == 0 && $(stat -c %a "$backup") == 700 ]]
install -d -m 700 -o root -g root /run/zuni-despliegue
[[ ! -L /run/zuni-despliegue ]]
exec 9>/run/zuni-despliegue/demo.lock
flock -n 9 || { echo 'Otra operación de despliegue está en curso.'; exit 1; }
(cd -- "$backup" && sha256sum --check --quiet SHA256SUMS)
mapfile -t paths < <(python3 - "$backup/despliegue.json" <<'PY'
import json,re,sys
p=json.load(open(sys.argv[1]))
for key in ['previous','candidate']:
 assert re.fullmatch(r'/opt/zuni/demo/releases/[a-zA-Z0-9_.-]+',p[key])
 print(p[key])
PY
)
[[ ${#paths[@]} == 2 ]]
old=${paths[0]}; candidate=${paths[1]}; current=/opt/zuni/demo/current
[[ -L $current && $(readlink -f -- "$current") == "$candidate" && -d $old && ! -L $old && -s $old/release-info.json ]]
version=$(python3 -c 'import json,sys;print(json.load(open(sys.argv[1]))["version"])' "$old/release-info.json")
sql() { runuser -u postgres -- env -u PGSERVICE -u PGOPTIONS -u PGPASSFILE /usr/lib/postgresql/18/bin/psql -X -h /var/run/postgresql -p 5433 -U postgres -d zuni -v ON_ERROR_STOP=1 -Atq -c "$1"; }
compatible() {
    case "$version" in
      1.5.*|1.6.*) return 0;;
      1.3.3|1.4.0)
        if [[ $version == 1.3.3 ]]; then [[ $(sql 'SELECT EXISTS(SELECT 1 FROM "PinesProfesionales" WHERE "HashProtegido" IS NOT NULL)') == f ]] || return 1; fi
        [[ $(sql 'SELECT EXISTS(SELECT 1 FROM "AspNetUsers" WHERE "TwoFactorEnabled") OR EXISTS(SELECT 1 FROM "Citas" WHERE "Estado"=7 OR "SustituyeCitaId" IS NOT NULL)') == f ]];;
      *) return 1;;
    esac
}
compatible || { echo 'DETENIDO: existen datos de PIN/2FA/ausencias que requieren la versión nueva. No se cambia el servicio ni se evaden protecciones.'; exit 1; }
[[ $(systemctl show zuni-demo -p WorkingDirectory --value) == "$current" ]]
changed=0
on_error() {
    local code=$?
    trap - ERR INT TERM
    set +e
    if [[ $changed == 1 ]]; then
        systemctl stop zuni-demo
        ln -s -- "$candidate" "$current.recovery-$$" && mv -Tf -- "$current.recovery-$$" "$current"
    fi
    systemctl start zuni-demo
    echo 'La reversión no se completó. Se conserva la versión nueva y la base sin restaurar; revisar el servicio.'
    exit "$code"
}
trap on_error ERR
trap 'false' INT TERM
systemctl stop zuni-demo
# Recheck while offline to avoid a 2FA enrollment between inspection and stop.
if ! compatible; then systemctl start zuni-demo; trap - ERR INT TERM; echo 'DETENIDO: apareció un estado incompatible; versión nueva reiniciada.'; exit 1; fi
changed=1
ln -s -- "$old" "$current.rollback-$$"
mv -Tf -- "$current.rollback-$$" "$current"
systemctl start zuni-demo
for attempt in {1..45}; do
    if curl --max-time 2 -fsS https://zuni.lat/Cuenta/IniciarSesion -o /dev/null; then break; fi
    sleep 1
done
curl --max-time 20 -fsS https://zuni.lat/Cuenta/IniciarSesion -o /dev/null
systemctl is-active --quiet zuni-demo
trap - ERR INT TERM
echo "REVERTIDO: código $version activo. Base, claves y configuración conservadas; no hubo restauración de datos."
