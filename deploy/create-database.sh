#!/usr/bin/env bash
# Creates the database user and database described in appsettings.Secrets.json
# on a PostgreSQL or MySQL server running on this machine. Run with sudo.
#
#   sudo deploy/create-database.sh /etc/carcare/appsettings.Secrets.json
set -euo pipefail
SECRETS="${1:-/etc/carcare/appsettings.Secrets.json}"
[[ -f "$SECRETS" ]] || { echo "Not found: $SECRETS" >&2; exit 1; }

read_setting() { python3 -c 'import json,sys; d=json.load(open(sys.argv[1]))["DbOptions"]; print(d.get(sys.argv[2], ""))' "$SECRETS" "$1"; }
PROVIDER="$(read_setting Provider)"; PROVIDER="${PROVIDER:-PostgreSql}"
NAME="$(read_setting Name)"
USER_NAME="$(read_setting UserId)"
PASSWORD="$(read_setting Password)"

[[ "$NAME" =~ ^[A-Za-z0-9_-]+$ && "$USER_NAME" =~ ^[A-Za-z0-9_]+$ ]] || { echo "Database and user names may only contain letters, digits, _ and -" >&2; exit 1; }
SQL_PASSWORD="${PASSWORD//\'/\'\'}"

if [[ "$PROVIDER" == "MySql" ]]; then
  echo "Creating MySQL user '$USER_NAME' and database '$NAME'"
  mysql -u root <<SQL
CREATE DATABASE IF NOT EXISTS \`$NAME\` CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
CREATE USER IF NOT EXISTS '$USER_NAME'@'localhost' IDENTIFIED BY '$SQL_PASSWORD';
CREATE USER IF NOT EXISTS '$USER_NAME'@'127.0.0.1' IDENTIFIED BY '$SQL_PASSWORD';
ALTER USER '$USER_NAME'@'localhost' IDENTIFIED BY '$SQL_PASSWORD';
ALTER USER '$USER_NAME'@'127.0.0.1' IDENTIFIED BY '$SQL_PASSWORD';
GRANT ALL PRIVILEGES ON \`$NAME\`.* TO '$USER_NAME'@'localhost';
GRANT ALL PRIVILEGES ON \`$NAME\`.* TO '$USER_NAME'@'127.0.0.1';
FLUSH PRIVILEGES;
SQL
else
  echo "Creating PostgreSQL role '$USER_NAME' and database '$NAME'"
  sudo -u postgres psql -v ON_ERROR_STOP=1 -q <<SQL
DO \$\$ BEGIN
  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = '$USER_NAME') THEN
    CREATE ROLE "$USER_NAME" LOGIN PASSWORD '$SQL_PASSWORD';
  ELSE
    ALTER ROLE "$USER_NAME" LOGIN PASSWORD '$SQL_PASSWORD';
  END IF;
END \$\$;
SQL
  if ! sudo -u postgres psql -tAc "SELECT 1 FROM pg_database WHERE datname = '$NAME'" | grep -q 1; then
    sudo -u postgres createdb -O "$USER_NAME" "$NAME"
  fi
fi
echo "Done."
