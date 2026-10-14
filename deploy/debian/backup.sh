#!/bin/bash
set -euo pipefail
umask 077
[[ $# == 2 ]] || { echo 'Uso: backup.sh identificador directorio-respaldo' >&2; exit 2; }
id=$1
[[ $id =~ ^[a-z][a-z0-9-]{1,39}$ ]] || exit 2
# Credenciales y conexión en el entorno del operador, no en argumentos.
[[ -n ${PGDATABASE:-} && -n ${PGUSER:-} ]] || { echo 'Define PGDATABASE, PGUSER y PGPASSFILE.' >&2; exit 2; }
[[ -f ${PGPASSFILE:-} ]] || { echo 'Falta PGPASSFILE.' >&2; exit 2; }
mkdir -p -- "$2"
target="$2/zuni-$id-$(date -u +%Y%m%dT%H%M%S)-$$.dump"
pg_dump --format=custom --file="$target"
pg_restore --list "$target" >/dev/null
echo "Respaldo verificado: $target"
# Conservar además las claves de /var/lib/zuni/<id>/keys bajo acceso restringido.
# Cifrar y enviar a almacenamiento independiente según política institucional.
