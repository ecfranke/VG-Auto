#!/usr/bin/env bash
# -----------------------------------------------------------------------------
# VG Auto control script (Linux and macOS).
#
#   sudo vgauto                  interactive menu
#   sudo vgauto status           or: start | stop | restart | logs [api|web] | health
#   sudo vgauto upgrade          back up, git pull, run deploy/install.sh
#   sudo vgauto backup           database + PDFs + configuration -> one .tar.gz
#   sudo vgauto restore FILE     restore a backup (asks for confirmation)
#   sudo vgauto baota ...        first installation / upgrade on a Baota (宝塔) panel server
#   vgauto help
#
# deploy/install.sh links this script to /usr/local/bin/vgauto. Before the first
# installation run it from the repository: sudo deploy/vgauto.sh baota ...
# On macOS run it without sudo.
# -----------------------------------------------------------------------------
set -uo pipefail

OS="$(uname -s)"
SELF="$(python3 -c 'import os,sys; print(os.path.realpath(sys.argv[1]))' "$0" 2>/dev/null || echo "$0")"
SCRIPT_REPO="$(cd "$(dirname "$SELF")/.." && pwd)"

# ---- installation settings (written by deploy/install.sh) -------------------------------
if [[ "$OS" == "Darwin" ]]; then
  PREFIX="$HOME/vg-auto"; CONFIG_DIR="$PREFIX/config"; DATA_DIR="$PREFIX/data"
  RUN_USER="$(id -un)"; SERVICE_MODE="pm2"; BACKUP_DIR="$PREFIX/backups"
else
  PREFIX="/opt/vg-auto"; CONFIG_DIR="/etc/vg-auto"; DATA_DIR="/var/lib/vg-auto"
  RUN_USER="vgauto"; SERVICE_MODE="systemd"; BACKUP_DIR="/var/backups/vg-auto"
fi
REPO_DIR="$SCRIPT_REPO"
LISTEN="all"
CONF="${VGAUTO_CONF:-$CONFIG_DIR/install.conf}"
# shellcheck disable=SC1090
[[ -f "$CONF" ]] && . "$CONF"
[[ -d "$REPO_DIR/.git" ]] || REPO_DIR="$SCRIPT_REPO"
SECRETS="$CONFIG_DIR/appsettings.Secrets.json"
WEB_ENV="$CONFIG_DIR/web.env"
ECOSYSTEM="$PREFIX/ecosystem.config.cjs"
API_PORT=15567
WEB_PORT=3000

# ---- helpers ---------------------------------------------------------------------------
if [[ -t 1 ]]; then B=$'\033[1m'; G=$'\033[32m'; R=$'\033[31m'; Y=$'\033[33m'; C=$'\033[1;34m'; N=$'\033[0m'; else B=""; G=""; R=""; Y=""; C=""; N=""; fi
log()  { printf '\n%s==> %s%s\n' "$C" "$*" "$N"; }
ok()   { printf '  %s✔%s %s\n' "$G" "$N" "$*"; }
bad()  { printf '  %s✘%s %s\n' "$R" "$N" "$*"; }
warn() { printf '%s%s%s\n' "$Y" "$*" "$N" >&2; }
die()  { printf '%sError: %s%s\n' "$R" "$*" "$N" >&2; exit 1; }

need_root() {
  if [[ "$OS" != "Darwin" && "$(id -u)" -ne 0 ]]; then
    exec sudo "$SELF" "$@"
  fi
}
installed() { [[ -f "$SECRETS" ]] || die "VG Auto is not installed here ($SECRETS not found). Run deploy/install.sh first."; }

as_user() {
  if [[ "$(id -un)" == "$RUN_USER" ]]; then "$@"; else sudo -u "$RUN_USER" -H env "PATH=$PATH" "$@"; fi
}
pm2c() { as_user pm2 "$@"; }

# Reads a value from DbOptions in appsettings.Secrets.json
db_setting() {
  python3 - "$SECRETS" "$1" <<'PY'
import json, sys
d = json.load(open(sys.argv[1])).get("DbOptions", {})
v = d.get(sys.argv[2], "")
print("" if v is None else v)
PY
}

# Finds a database client tool, also in the Baota, PostgreSQL and Homebrew locations
find_tool() {
  local name="$1" p
  for p in /www/server/pgsql/bin /www/server/mysql/bin \
           $(ls -d /usr/lib/postgresql/*/bin 2>/dev/null | sort -V -r) \
           /opt/homebrew/bin $(ls -d /opt/homebrew/opt/postgresql@*/bin /usr/local/opt/postgresql@*/bin 2>/dev/null | sort -V -r) \
           /usr/local/mysql/bin; do
    [[ -x "$p/$name" ]] && { echo "$p/$name"; return 0; }
  done
  command -v "$name" 2>/dev/null && return 0
  return 1
}

confirm() { # confirm "question"  -> requires typing yes
  local answer
  read -r -p "$1 Type 'yes' to continue: " answer
  [[ "$answer" == "yes" ]]
}

pm2_status() { # prints the pm2 status of an app, or "missing"
  pm2c jlist 2>/dev/null | python3 -c '
import json, sys
try:
    apps = json.load(sys.stdin)
except Exception:
    print("missing"); sys.exit()
for a in apps:
    if a.get("name") == sys.argv[1]:
        print(a.get("pm2_env", {}).get("status", "unknown")); break
else:
    print("missing")' "$1"
}

# ---- start / stop / restart --------------------------------------------------------------
api_ctl() { # api_ctl start|stop|restart
  if [[ "$SERVICE_MODE" == "systemd" ]]; then
    systemctl "$1" vg-auto-api
  else
    case "$1" in
      start|restart) pm2c startOrRestart "$ECOSYSTEM" --only vg-auto-api >/dev/null ;;
      stop) pm2c stop vg-auto-api >/dev/null ;;
    esac
  fi
}
web_ctl() {
  case "$1" in
    start|restart) pm2c startOrRestart "$ECOSYSTEM" --only vg-auto-web >/dev/null ;;
    stop) pm2c stop vg-auto-web >/dev/null ;;
  esac
}

cmd_start()   { need_root start;   installed; log "Starting VG Auto";   api_ctl start;   web_ctl start;   pm2c save >/dev/null; sleep 3; cmd_status_body; }
cmd_stop()    { need_root stop;    installed; log "Stopping VG Auto";   web_ctl stop;    api_ctl stop;    pm2c save >/dev/null; ok "stopped"; }
cmd_restart() { need_root restart; installed; log "Restarting VG Auto"; api_ctl restart; web_ctl restart; pm2c save >/dev/null; sleep 3; cmd_status_body; }

# ---- status / health / logs -------------------------------------------------------------------
check_health() {
  local code
  if curl -fsS -m 5 "http://127.0.0.1:$API_PORT/health" >/dev/null 2>&1; then ok "API responds      http://127.0.0.1:$API_PORT/health"; else bad "API does not respond on port $API_PORT"; fi
  code="$(curl -s -o /dev/null -m 10 -w '%{http_code}' "http://127.0.0.1:$WEB_PORT/" 2>/dev/null || true)"
  if [[ "$code" =~ ^(200|30[0-9])$ ]]; then ok "Web app responds  http://127.0.0.1:$WEB_PORT/ ($code)"; else bad "Web app does not respond on port $WEB_PORT (${code:-no answer})"; fi
}
cmd_status_body() {
  local s
  log "Services"
  if [[ "$SERVICE_MODE" == "systemd" ]]; then
    s="$(systemctl is-active vg-auto-api 2>/dev/null)"
    [[ "$s" == "active" ]] && ok "vg-auto-api  (systemd) $s" || bad "vg-auto-api  (systemd) ${s:-unknown}"
  else
    s="$(pm2_status vg-auto-api)"
    [[ "$s" == "online" ]] && ok "vg-auto-api  (pm2) $s" || bad "vg-auto-api  (pm2) $s"
  fi
  s="$(pm2_status vg-auto-web)"
  [[ "$s" == "online" ]] && ok "vg-auto-web  (pm2) $s" || bad "vg-auto-web  (pm2) $s"
  log "Health"
  check_health
  log "Installation"
  echo "  App URL:      $(grep -E '^APP_URL=' "$WEB_ENV" 2>/dev/null | cut -d= -f2-)"
  echo "  API URL:      $(grep -E '^NEXT_PUBLIC_API_URL=' "$WEB_ENV" 2>/dev/null | cut -d= -f2-)"
  echo "  Listening on: $( [[ "$LISTEN" == "local" ]] && echo "127.0.0.1 only (behind a reverse proxy)" || echo "all interfaces" )"
  echo "  Database:     $(db_setting Provider) $(db_setting Name)@$(db_setting Host)"
  echo "  Version:      $(git -C "$REPO_DIR" log -1 --format='%h %cd' --date=short 2>/dev/null || echo unknown)  ($REPO_DIR)"
}
cmd_status() { need_root status; installed; cmd_status_body; }
cmd_health() { check_health; }

cmd_logs() {
  need_root logs "$@"; installed
  case "${1:-api}" in
    api)
      if [[ "$SERVICE_MODE" == "systemd" ]]; then journalctl -u vg-auto-api -n 200 -f; else pm2c logs vg-auto-api --lines 200; fi ;;
    web) pm2c logs vg-auto-web --lines 200 ;;
    *) die "usage: vgauto logs [api|web]" ;;
  esac
}

# ---- backup / restore ------------------------------------------------------------------------
db_env() { # sets DB_* variables from the configuration
  DB_PROVIDER="$(db_setting Provider)"; DB_PROVIDER="${DB_PROVIDER:-PostgreSql}"
  DB_HOST="$(db_setting Host)"; DB_PORT="$(db_setting Port)"
  DB_USER="$(db_setting UserId)"; DB_PASS="$(db_setting Password)"; DB_NAME="$(db_setting Name)"
  if [[ "$DB_PROVIDER" == "MySql" ]]; then DB_PORT="${DB_PORT:-3306}"; else DB_PORT="${DB_PORT:-5432}"; fi
}

dump_database() { # dump_database FILE
  db_env
  if [[ "$DB_PROVIDER" == "MySql" ]]; then
    local tool; tool="$(find_tool mysqldump)" || die "mysqldump not found (install the MySQL client)."
    MYSQL_PWD="$DB_PASS" "$tool" --protocol=TCP -h "$DB_HOST" -P "$DB_PORT" -u "$DB_USER" \
      --single-transaction --routines --triggers --no-tablespaces --default-character-set=utf8mb4 \
      "$DB_NAME" > "$1"
  else
    local tool; tool="$(find_tool pg_dump)" || die "pg_dump not found (install the PostgreSQL client)."
    PGPASSWORD="$DB_PASS" "$tool" -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" \
      --clean --if-exists --no-owner --no-privileges -f "$1"
  fi
}

# load_database FILE: empties the database first (tables created by a newer version must not
# survive), then loads the dump. Only objects of the application's database user are touched.
load_database() {
  db_env
  if [[ "$DB_PROVIDER" == "MySql" ]]; then
    local tool tables; tool="$(find_tool mysql)" || die "mysql client not found."
    local -a my=("$tool" --protocol=TCP -h "$DB_HOST" -P "$DB_PORT" -u "$DB_USER" --default-character-set=utf8mb4)
    tables="$(MYSQL_PWD="$DB_PASS" "${my[@]}" -N -B -e "SELECT CONCAT('\`', table_name, '\`') FROM information_schema.tables WHERE table_schema = DATABASE()" "$DB_NAME" | paste -sd, -)" || return 1
    if [[ -n "$tables" ]]; then
      MYSQL_PWD="$DB_PASS" "${my[@]}" -e "SET FOREIGN_KEY_CHECKS=0; DROP TABLE IF EXISTS $tables; SET FOREIGN_KEY_CHECKS=1;" "$DB_NAME" || return 1
    fi
    MYSQL_PWD="$DB_PASS" "${my[@]}" "$DB_NAME" < "$1"
  else
    local tool; tool="$(find_tool psql)" || die "psql not found."
    local -a pg=("$tool" -q -v ON_ERROR_STOP=1 -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME")
    PGPASSWORD="$DB_PASS" "${pg[@]}" -c "DROP OWNED BY CURRENT_USER" >/dev/null || return 1
    PGPASSWORD="$DB_PASS" "${pg[@]}" -f "$1" >/dev/null
  fi
}

# backup [--dir DIR] [--keep N] [--label NAME]
do_backup() (   # runs in a subshell: its own EXIT trap and exit on error
  local dir="$BACKUP_DIR" keep="" label="" tmp stamp file
  while [[ $# -gt 0 ]]; do
    case "$1" in
      --dir) dir="$2"; shift 2 ;;
      --keep) keep="$2"; shift 2 ;;
      --label) label="-$2"; shift 2 ;;
      *) die "unknown backup option $1" ;;
    esac
  done
  [[ -z "$keep" || "$keep" =~ ^[0-9]+$ ]] || die "--keep needs a number"
  installed
  mkdir -p "$dir"; chmod 700 "$dir"
  tmp="$(mktemp -d)"; trap 'rm -rf "$tmp"' EXIT
  stamp="$(date +%Y%m%d-%H%M%S)"
  file="$dir/vg-auto-$stamp$label.tar.gz"
  log "Backing up to $file"
  db_env
  dump_database "$tmp/database.sql" || die "database dump failed"
  ok "database ($DB_PROVIDER $DB_NAME, $(du -h "$tmp/database.sql" | cut -f1))"
  mkdir -p "$tmp/config"
  cp -p "$SECRETS" "$WEB_ENV" "$tmp/config/" && [[ -f "$CONF" ]] && cp -p "$CONF" "$tmp/config/"
  ok "configuration"
  if [[ -d "$DATA_DIR/pdf" ]]; then tar -C "$DATA_DIR" -cf "$tmp/pdf.tar" pdf; ok "PDF files ($(find "$DATA_DIR/pdf" -type f | wc -l | tr -d ' '))"; fi
  cat > "$tmp/manifest.txt" <<EOF
created=$(date -u +%Y-%m-%dT%H:%M:%SZ)
host=$(hostname)
provider=$DB_PROVIDER
database=$DB_NAME
version=$(git -C "$REPO_DIR" rev-parse --short HEAD 2>/dev/null || echo unknown)
EOF
  ( umask 077; tar -C "$tmp" -czf "$file" . ) || die "could not write $file"
  ok "$(du -h "$file" | cut -f1)  $file"
  if [[ -n "$keep" ]]; then
    local old
    # shellcheck disable=SC2012
    old="$(ls -1t "$dir"/vg-auto-*.tar.gz 2>/dev/null | tail -n +$((keep + 1)))"
    if [[ -n "$old" ]]; then echo "$old" | while read -r f; do rm -f "$f"; echo "  removed old backup $f"; done; fi
  fi
)
cmd_backup() { need_root backup "$@"; do_backup "$@"; }

cmd_restore() {
  need_root restore "$@"; installed
  local file="" with_config=0 yes=0 tmp
  RESTORE_TMP=""; trap 'rm -rf "$RESTORE_TMP"' EXIT
  while [[ $# -gt 0 ]]; do
    case "$1" in
      --with-config) with_config=1; shift ;;
      --yes) yes=1; shift ;;
      -*) die "unknown restore option $1" ;;
      *) file="$1"; shift ;;
    esac
  done
  if [[ -z "$file" ]]; then
    echo "Backups in $BACKUP_DIR:"
    # shellcheck disable=SC2012
    ls -1t "$BACKUP_DIR"/vg-auto-*.tar.gz 2>/dev/null | head -20 | sed 's/^/  /' || true
    die "usage: vgauto restore FILE [--with-config] [--yes]"
  fi
  [[ -f "$file" ]] || die "$file not found"
  tmp="$(mktemp -d)"; RESTORE_TMP="$tmp"
  tar -C "$tmp" -xzf "$file" || die "cannot read $file"
  [[ -f "$tmp/database.sql" && -f "$tmp/manifest.txt" ]] || die "$file is not a VG Auto backup"
  db_env
  local provider; provider="$(grep '^provider=' "$tmp/manifest.txt" | cut -d= -f2)"
  [[ "$provider" == "$DB_PROVIDER" ]] || die "the backup is from $provider but this installation uses $DB_PROVIDER"

  log "Backup $file"
  sed 's/^/  /' "$tmp/manifest.txt"
  echo
  warn "This REPLACES all data in database '$DB_NAME' and the PDF files with the backup."
  [[ $with_config -eq 1 ]] && warn "It also replaces $SECRETS and $WEB_ENV."
  if [[ $yes -eq 0 ]]; then confirm "Restore now?" || die "cancelled"; fi

  log "Safety backup of the current state"
  do_backup --label before-restore >/dev/null || die "safety backup failed, nothing was changed"
  ok "saved in $BACKUP_DIR (…-before-restore.tar.gz)"

  log "Stopping VG Auto"
  web_ctl stop; api_ctl stop
  log "Restoring"
  load_database "$tmp/database.sql" || { warn "Database restore failed. The safety backup is in $BACKUP_DIR."; api_ctl start; web_ctl start; exit 1; }
  ok "database"
  if [[ -f "$tmp/pdf.tar" ]]; then
    rm -rf "$DATA_DIR/pdf.restore" && mkdir -p "$DATA_DIR/pdf.restore"
    tar -C "$DATA_DIR/pdf.restore" -xf "$tmp/pdf.tar"
    rm -rf "$DATA_DIR/pdf" && mv "$DATA_DIR/pdf.restore/pdf" "$DATA_DIR/pdf" && rm -rf "$DATA_DIR/pdf.restore"
    chown -R "$RUN_USER" "$DATA_DIR/pdf" 2>/dev/null || true
    ok "PDF files"
  fi
  if [[ $with_config -eq 1 ]]; then
    cp "$tmp/config/appsettings.Secrets.json" "$SECRETS"; cp "$tmp/config/web.env" "$WEB_ENV"
    chown "$RUN_USER" "$SECRETS" "$WEB_ENV" 2>/dev/null || true; chmod 600 "$SECRETS" "$WEB_ENV"
    ok "configuration (if the URLs changed, run: vgauto upgrade to rebuild the web app)"
  fi
  log "Starting VG Auto"
  api_ctl start; web_ctl start; pm2c save >/dev/null; sleep 3
  check_health
}

# ---- upgrade -----------------------------------------------------------------------------------
cmd_upgrade() {
  need_root upgrade "$@"; installed
  local backup=1 args=()
  for a in "$@"; do [[ "$a" == "--no-backup" ]] && backup=0 || args+=("$a"); done
  if [[ $backup -eq 1 ]]; then
    do_backup --label before-upgrade || die "backup failed; use --no-backup to upgrade anyway"
  fi
  log "Updating the source code in $REPO_DIR"
  local owner
  owner="$(stat -c %U "$REPO_DIR" 2>/dev/null || stat -f %Su "$REPO_DIR")"
  if [[ "$owner" == "$(id -un)" ]]; then git -C "$REPO_DIR" pull --ff-only; else sudo -u "$owner" git -C "$REPO_DIR" pull --ff-only; fi \
    || die "git pull failed (local changes in $REPO_DIR?)"
  "$REPO_DIR/deploy/install.sh" ${args[@]+"${args[@]}"}
}

# ---- Baota panel -----------------------------------------------------------------------------------
cmd_baota() {
  [[ "$OS" == "Linux" ]] || die "the Baota mode is for Linux servers"
  need_root baota "$@"
  command -v apt-get >/dev/null || die "only Ubuntu / Debian are supported (apt-get not found)"
  [[ -d /www/server/panel ]] || { warn "The Baota panel was not found (/www/server/panel)."; confirm "Continue anyway?" || exit 1; }

  log "Installing .NET 9, Node.js, pm2 and the PDF libraries (no nginx, no database)"
  export DEBIAN_FRONTEND=noninteractive
  apt-get update -q
  apt-get install -y -q ca-certificates curl gnupg git rsync openssl python3 sudo fonts-liberation \
    libatk-bridge2.0-0 libatk1.0-0 libcups2 libdrm2 libgbm1 libgtk-3-0 libnspr4 libnss3 \
    libxcomposite1 libxdamage1 libxfixes3 libxkbcommon0 libxrandr2 libpango-1.0-0 libcairo2 xdg-utils \
    || die "apt-get install failed"
  apt-get install -y -q libasound2t64 >/dev/null 2>&1 || apt-get install -y -q libasound2 >/dev/null 2>&1 || true

  if ! dotnet --list-sdks 2>/dev/null | grep -q '^9\.'; then
    . /etc/os-release
    curl -fsSL "https://packages.microsoft.com/config/${ID}/${VERSION_ID}/packages-microsoft-prod.deb" -o /tmp/packages-microsoft-prod.deb \
      || die "cannot download the Microsoft package repository for ${ID} ${VERSION_ID}"
    dpkg -i /tmp/packages-microsoft-prod.deb && rm -f /tmp/packages-microsoft-prod.deb
    apt-get update -q && apt-get install -y -q dotnet-sdk-9.0 || die ".NET 9 SDK installation failed"
  fi
  ok ".NET $(dotnet --version)"

  if ! command -v node >/dev/null || [[ "$(node -v | cut -d. -f1 | tr -d v)" -lt 20 ]]; then
    mkdir -p /etc/apt/keyrings
    curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key | gpg --dearmor --yes -o /etc/apt/keyrings/nodesource.gpg
    echo "deb [signed-by=/etc/apt/keyrings/nodesource.gpg] https://deb.nodesource.com/node_22.x nodistro main" > /etc/apt/sources.list.d/nodesource.list
    apt-get update -q && apt-get install -y -q nodejs || die "Node.js installation failed"
  fi
  command -v pm2 >/dev/null || npm install -g pm2 >/dev/null || die "pm2 installation failed"
  ok "Node.js $(node -v), pm2 installed"
  if dpkg -s nginx >/dev/null 2>&1 && [[ -d /www/server/nginx ]]; then
    warn "The Debian nginx package is installed next to the Baota nginx; they compete for ports 80/443."
    warn "If the Baota nginx does not start, remove it: apt-get remove nginx"
  fi

  if ! find_tool pg_dump >/dev/null && ! find_tool mysqldump >/dev/null; then
    warn "No pg_dump / mysqldump found yet; 'vgauto backup' needs the client of your database."
  fi

  log "Running deploy/install.sh --proxy (API and web app listen on 127.0.0.1 only)"
  "$REPO_DIR/deploy/install.sh" --proxy "$@" || exit $?

  cat <<EOF

${B}Next steps in the Baota panel${N}
  Website for the app domain   -> Reverse proxy -> target URL http://127.0.0.1:$WEB_PORT  (send domain \$host)
  Website for the API domain   -> Reverse proxy -> target URL http://127.0.0.1:$API_PORT (send domain \$host)
  Then request Let's Encrypt certificates for both sites and turn on "Force HTTPS".
  Details: docs/deployment.zh-CN.md, section 1.6
EOF
}

# ---- help and menu ----------------------------------------------------------------------------
cmd_help() {
  cat <<EOF
${B}VG Auto control${N}   $( [[ "$OS" == "Darwin" ]] && echo "vgauto" || echo "sudo vgauto" ) [command]

  status                    services, health check, URLs and version
  start | stop | restart    API and web app together
  logs [api|web]            follow the logs (Ctrl+C to leave)
  health                    only the health check
  upgrade [--no-backup]     back up, git pull, reinstall (options are passed to install.sh)
  backup [--dir DIR] [--keep N]
                            database + PDFs + configuration into one .tar.gz
                            (default directory $BACKUP_DIR; --keep N deletes older ones)
  restore FILE [--with-config] [--yes]
                            restore database and PDFs from a backup (makes a safety backup first)
  baota [install.sh options]
                            Baota panel server: installs .NET/Node/pm2 without nginx or a database,
                            then runs install.sh --proxy. Example for the first installation:
                              sudo deploy/vgauto.sh baota --app-url https://app.example.com \\
                                --api-url https://api.example.com --db-provider MySql \\
                                --db-host 127.0.0.1 --db-password '...' --admin-email you@example.com
  help                      this text

Without a command an interactive menu is shown.
EOF
}

cmd_menu() {
  local choice file
  while true; do
    cat <<EOF

${B}VG Auto${N}
  1) Status            5) Logs: API
  2) Start             6) Logs: web app
  3) Stop              7) Backup
  4) Restart           8) Restore a backup
                       9) Upgrade
  0) Exit
EOF
    read -r -p "Choose: " choice || exit 0
    case "$choice" in
      1) ( cmd_status ) ;;
      2) ( cmd_start ) ;;
      3) ( cmd_stop ) ;;
      4) ( cmd_restart ) ;;
      5) ( cmd_logs api ) ;;
      6) ( cmd_logs web ) ;;
      7) ( cmd_backup ) ;;
      8) # shellcheck disable=SC2012
         ls -1t "$BACKUP_DIR"/vg-auto-*.tar.gz 2>/dev/null | head -10 | nl -w2 -s') '
         read -r -p "Number or path of the backup: " file
         if [[ "$file" =~ ^[0-9]+$ ]]; then file="$(ls -1t "$BACKUP_DIR"/vg-auto-*.tar.gz 2>/dev/null | sed -n "${file}p")"; fi
         [[ -n "$file" ]] && ( cmd_restore "$file" ) ;;
      9) ( cmd_upgrade ) ;;
      0|q|"") exit 0 ;;
      *) echo "Unknown choice" ;;
    esac
  done
}

# ---- main ---------------------------------------------------------------------------------------
cmd="${1:-}"; [[ $# -gt 0 ]] && shift
case "$cmd" in
  "")        if [[ -t 0 ]]; then need_root; cmd_menu; else cmd_help; fi ;;
  status)    cmd_status ;;
  start)     cmd_start ;;
  stop)      cmd_stop ;;
  restart)   cmd_restart ;;
  logs|log)  cmd_logs "$@" ;;
  health)    cmd_health ;;
  upgrade|update) cmd_upgrade "$@" ;;
  backup)    cmd_backup "$@" ;;
  restore)   cmd_restore "$@" ;;
  baota|bt)  cmd_baota "$@" ;;
  help|-h|--help) cmd_help ;;
  *) cmd_help; exit 1 ;;
esac
