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
# Este respaldo NO incluye claves. Conservar aparte, cifradas y bajo custodia separada,
# /var/lib/zuni/<id>/seguridad (cifrado clínico) y /var/lib/zuni/<id>/dataprotection.
# Sin claves.json los textos clínicos del respaldo no se pueden leer.
# Cifrar y enviar a almacenamiento independiente según política institucional.
