#!/bin/sh
# Optional new job; the existing backup.sh is retained for migration review, not silently changed.
set -eu
umask 077
[ "$#" -eq 2 ] || { echo 'Usage: backup-encrypted.sh installation-id private-archive-dir' >&2; exit 2; }
case "$1" in ''|*[!a-z0-9-]*) exit 2;; esac
[ -n "${PGDATABASE:-}" ] && [ -n "${PGUSER:-}" ] && [ -f "${PGPASSFILE:-}" ] || { echo 'Configure the private PostgreSQL environment.' >&2; exit 2; }
[ -f "${DENIS_VAULT_TOOL:-}" ] && [ -f "${DENIS_BACKUP_KEY_FILE:-}" ] || { echo 'Configure the reviewed vault tool and separate private key.' >&2; exit 2; }
command -v node >/dev/null && command -v pg_dump >/dev/null && command -v pg_restore >/dev/null
mkdir -p "$2"
stage=$(mktemp -d "${TMPDIR:-/var/tmp}/zuni-snapshot.XXXXXXXX")
cleanup(){ case "$stage" in */zuni-snapshot.????????) rm -rf -- "$stage";; *) echo 'Unsafe staging cleanup rejected' >&2;; esac; }
trap cleanup EXIT
trap 'exit 1' HUP INT TERM
pg_dump --format=custom --file="$stage/zuni.dump"
pg_restore --list "$stage/zuni.dump" >/dev/null
archive="$2/zuni-$1-$(date -u +%Y%m%dT%H%M%S)-$$.vault"
node "$DENIS_VAULT_TOOL" encrypt zuni "$stage" "$archive" "$DENIS_BACKUP_KEY_FILE"
echo '{"event":"zuni_backup_encrypted","externalCopyVerified":false,"restoreVerified":false}'
# Keys/Data Protection are a SEPARATE vault with a separate key and custody.
# A successful local archive is not a verified off-site copy or a restore.
