#!/usr/bin/env bash
# Generates backend/src/VgAuto.Http.Api/appsettings.Secrets.json and frontend/.env
# with random secrets. Existing files are kept unless --force is given.
set -euo pipefail

cd "$(dirname "$0")/.."

APPSETTINGS=backend/src/VgAuto.Http.Api/appsettings.Secrets.json
ENVFILE=frontend/.env
FORCE=${1:-}

if [[ -f $APPSETTINGS || -f $ENVFILE ]] && [[ "$FORCE" != "--force" ]]; then
  echo "Secrets already exist ($APPSETTINGS / $ENVFILE). Use --force to regenerate." >&2
  exit 1
fi

JWT_SECRET=$(openssl rand -hex 64)
CONSUMER_SECRET=$(openssl rand -hex 32)
SESSION_SECRET=$(openssl rand -hex 32)
DB_PASSWORD=${DB_PASSWORD:-$(openssl rand -hex 16)}

replace() { # file placeholder value
  python3 - "$1" "$2" "$3" <<'PY'
import sys
path, placeholder, value = sys.argv[1:4]
text = open(path, encoding='utf-8').read().replace(placeholder, value)
open(path, 'w', encoding='utf-8').write(text)
PY
}

cp ${APPSETTINGS}.example $APPSETTINGS
replace $APPSETTINGS "[your-jwt-secret]" "$JWT_SECRET"
replace $APPSETTINGS "[your-server-secret]" "$CONSUMER_SECRET"
replace $APPSETTINGS "[your-db-password]" "$DB_PASSWORD"
chmod 600 $APPSETTINGS

cp ${ENVFILE}.example $ENVFILE
replace $ENVFILE "[your-server-secret]" "$CONSUMER_SECRET"
replace $ENVFILE "[random-32-byte-base64]" "$SESSION_SECRET"
chmod 600 $ENVFILE

echo "Secrets initialized."
echo "Database password: $DB_PASSWORD  (create the database user with this password, or edit $APPSETTINGS)"
