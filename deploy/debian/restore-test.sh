#!/bin/bash
set -euo pipefail
umask 077
[[ $# == 2 ]] || { echo 'Uso: restore-test.sh respaldo.dump base_descartable' >&2; exit 2; }
[[ $2 =~ ^zuni_restore_[a-z0-9_]+$ ]] || { echo 'El destino debe comenzar con zuni_restore_.' >&2; exit 2; }
[[ -f $1 && -f ${PGPASSFILE:-} ]] || exit 2
# createdb falla si el destino ya existe: nunca borra ni reemplaza una base.
createdb -- "$2"
pg_restore --exit-on-error --no-owner --no-privileges --dbname="$2" "$1"
psql --dbname="$2" -X --set=ON_ERROR_STOP=1 --command='SELECT count(*) AS usuarios FROM "AspNetUsers"; SELECT count(*) AS migraciones FROM "__EFMigrationsHistory";'
echo 'Restauración comprobada. La base descartable se conserva para revisión.'
