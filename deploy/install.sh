#!/usr/bin/env bash
# -----------------------------------------------------------------------------
# VG Auto install / upgrade without Docker (Linux and macOS).
#
#   Linux (Debian/Ubuntu):  API as a systemd service, web app under pm2, optional nginx site.
#   macOS:                  API and web app under pm2 (pm2 startup -> launchd).
#
# Run it from a checkout of the repository. Re-running it upgrades an existing
# installation (configuration is kept, database migrations are applied).
#
#   sudo deploy/install.sh --app-url https://app.example.com --api-url https://api.example.com \
#        --nginx --db-provider PostgreSql --admin-email you@example.com
#
#   deploy/install.sh --help
#
# Options that shape the installation (directories, service account, how the API runs,
# whether the apps listen only on 127.0.0.1) are saved in <config-dir>/install.conf and
# reused by later runs, so an upgrade is simply: git pull && sudo deploy/install.sh
# (or: sudo vgauto upgrade). deploy/vgauto.sh reads the same file.
# -----------------------------------------------------------------------------
set -euo pipefail

REPO_DIR="$(cd "$(dirname "$0")/.." && pwd)"
OS="$(uname -s)"

# ---- defaults ----------------------------------------------------------------
if [[ "$OS" == "Darwin" ]]; then
  PREFIX="${HOME}/vg-auto"
  CONFIG_DIR="${PREFIX}/config"
  DATA_DIR="${PREFIX}/data"
  SERVICE_MODE="pm2"
  RUN_USER="$(id -un)"
else
  PREFIX="/opt/vg-auto"
  CONFIG_DIR="/etc/vg-auto"
  DATA_DIR="/var/lib/vg-auto"
  SERVICE_MODE="systemd"
  RUN_USER="vgauto"
fi
APP_URL="http://localhost:3000"
API_URL=""
DB_PROVIDER="PostgreSql"
DB_HOST="localhost"
DB_PORT=""
DB_NAME="vgauto"
DB_USER="vgauto"
DB_PASSWORD=""
ADMIN_EMAIL=""
NGINX=0
LISTEN=""        # local (127.0.0.1, behind a reverse proxy) or all; saved in install.conf
SKIP_WEB=0
SKIP_API=0
SKIP_MIGRATIONS=0

usage() {
  cat <<EOF
Usage: $0 [options]

  --prefix DIR           install directory            (default: $PREFIX)
  --config-dir DIR       configuration directory      (default: $CONFIG_DIR)
  --data-dir DIR         pdf / browser cache          (default: $DATA_DIR)
  --user NAME            service account              (default: $RUN_USER)
  --service systemd|pm2  how the API is run           (default: $SERVICE_MODE)

  First installation only (written to the configuration files):
  --app-url URL          URL users open               (default: $APP_URL)
  --api-url URL          API URL reachable from users' browsers
                         (default: http://<app host>:15567, or https://api.<domain> with --nginx)
  --db-provider NAME     PostgreSql | MySql           (default: $DB_PROVIDER)
  --db-host HOST         (default: $DB_HOST)
  --db-port PORT         (default: 5432 / 3306)
  --db-name NAME         (default: $DB_NAME)
  --db-user NAME         (default: $DB_USER)
  --db-password PASS     (default: random, create the database user with it)
  --admin-email EMAIL    email of the initial administrator (login codes are sent there)

  --nginx                write and enable an nginx site for the app and API domains (Linux);
                         an existing site is kept (certbot edits it). Implies --proxy.
  --proxy                API and web app listen on 127.0.0.1 only, for a reverse proxy on this
                         machine that this script does not manage (Baota panel, Caddy, ...)
  --listen-all           API and web app listen on all interfaces (default without a proxy)
  --skip-web | --skip-api | --skip-migrations
EOF
}

# ---- saved options of an existing installation ------------------------------------
for ((i = 1; i <= $#; i++)); do
  if [[ "${!i}" == "--config-dir" ]]; then j=$((i + 1)); CONFIG_DIR="${!j}"; fi
done
if [[ -f "$CONFIG_DIR/install.conf" ]]; then
  # shellcheck disable=SC1091
  . "$CONFIG_DIR/install.conf"
  REPO_DIR="$(cd "$(dirname "$0")/.." && pwd)"   # always the checkout this script runs from
elif [[ "$OS" != "Darwin" && -e /etc/nginx/sites-enabled/vg-auto ]]; then
  LISTEN="local"   # installed with --nginx before install.conf existed
fi

while [[ $# -gt 0 ]]; do
  case "$1" in
    --prefix) PREFIX="$2"; shift 2 ;;
    --config-dir) CONFIG_DIR="$2"; shift 2 ;;
    --data-dir) DATA_DIR="$2"; shift 2 ;;
    --user) RUN_USER="$2"; shift 2 ;;
    --service) SERVICE_MODE="$2"; shift 2 ;;
    --app-url) APP_URL="${2%/}"; shift 2 ;;
    --api-url) API_URL="${2%/}"; shift 2 ;;
    --db-provider) DB_PROVIDER="$2"; shift 2 ;;
    --db-host) DB_HOST="$2"; shift 2 ;;
    --db-port) DB_PORT="$2"; shift 2 ;;
    --db-name) DB_NAME="$2"; shift 2 ;;
    --db-user) DB_USER="$2"; shift 2 ;;
    --db-password) DB_PASSWORD="$2"; shift 2 ;;
    --admin-email) ADMIN_EMAIL="$2"; shift 2 ;;
    --nginx) NGINX=1; LISTEN="local"; shift ;;
    --proxy) LISTEN="local"; shift ;;
    --listen-all) LISTEN="all"; shift ;;
    --skip-web) SKIP_WEB=1; shift ;;
    --skip-api) SKIP_API=1; shift ;;
    --skip-migrations) SKIP_MIGRATIONS=1; shift ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Unknown option: $1" >&2; usage; exit 1 ;;
  esac
done

log() { printf '\n\033[1;34m==> %s\033[0m\n' "$*"; }
die() { printf '\033[1;31mError: %s\033[0m\n' "$*" >&2; exit 1; }
need() { command -v "$1" >/dev/null 2>&1 || die "$1 is required. $2"; }
as_user() { if [[ "$(id -un)" == "$RUN_USER" ]]; then "$@"; else sudo -u "$RUN_USER" -H env "PATH=$PATH" "$@"; fi; }
random_hex() { openssl rand -hex "$1"; }
url_host() { python3 -c 'import sys,urllib.parse as u; print(u.urlparse(sys.argv[1]).hostname or "")' "$1"; }

case "$DB_PROVIDER" in
  PostgreSql|postgresql|postgres) DB_PROVIDER="PostgreSql"; DB_PORT="${DB_PORT:-5432}" ;;
  MySql|mysql) DB_PROVIDER="MySql"; DB_PORT="${DB_PORT:-3306}" ;;
  *) die "--db-provider must be PostgreSql or MySql" ;;
esac
[[ "$SERVICE_MODE" == "systemd" || "$SERVICE_MODE" == "pm2" ]] || die "--service must be systemd or pm2"
LISTEN="${LISTEN:-all}"
[[ "$LISTEN" == "local" || "$LISTEN" == "all" ]] || die "LISTEN must be local or all"
[[ "$SERVICE_MODE" == "systemd" && "$OS" == "Darwin" ]] && die "systemd is not available on macOS, use --service pm2"

if [[ "$OS" != "Darwin" && "$(id -u)" -ne 0 && "$SERVICE_MODE" == "systemd" ]]; then
  die "run with sudo (systemd service, $CONFIG_DIR and $PREFIX need root)"
fi

need dotnet "Install the .NET 9 SDK (deploy/prerequisites-debian.sh or https://dot.net)."
dotnet --list-sdks | grep -q '^9\.' || die ".NET 9 SDK not found (dotnet --list-sdks)."
need openssl ""
need python3 ""
if [[ $SKIP_WEB -eq 0 || "$SERVICE_MODE" == "pm2" ]]; then
  need node "Install Node.js 20 or newer."
  need npm ""
  command -v pm2 >/dev/null 2>&1 || { log "Installing pm2"; npm install -g pm2; }
fi
command -v rsync >/dev/null 2>&1 || die "rsync is required."

# ---- service account ----------------------------------------------------------
if [[ "$OS" != "Darwin" ]] && ! id "$RUN_USER" >/dev/null 2>&1; then
  log "Creating service account $RUN_USER"
  useradd --system --create-home --home-dir "/home/$RUN_USER" --shell /usr/sbin/nologin "$RUN_USER"
fi

mkdir -p "$PREFIX" "$CONFIG_DIR" "$DATA_DIR/pdf" "$DATA_DIR/puppeteer"
chown -R "$RUN_USER" "$DATA_DIR" 2>/dev/null || true

# ---- configuration (first run only) -------------------------------------------
SECRETS="$CONFIG_DIR/appsettings.Secrets.json"
WEB_ENV="$CONFIG_DIR/web.env"

if [[ -z "$API_URL" ]]; then
  APP_HOST="$(url_host "$APP_URL")"
  if [[ "$LISTEN" == "local" ]]; then API_URL="https://api.${APP_HOST#www.}"; else API_URL="http://${APP_HOST}:15567"; fi
fi

if [[ ! -f "$SECRETS" ]]; then
  log "Writing $SECRETS"
  [[ -n "$DB_PASSWORD" ]] || DB_PASSWORD="$(random_hex 16)"
  CONSUMER_SECRET="$(random_hex 32)"
  python3 - "$SECRETS" <<PY
import json, sys
settings = {
  "JwtOptions": {"Secret": "$(random_hex 64)", "ConsumerSecret": "$CONSUMER_SECRET"},
  "DbOptions": {"Provider": "$DB_PROVIDER", "Host": "$DB_HOST", "Port": int("$DB_PORT"), "UserId": "$DB_USER",
                "Password": "$DB_PASSWORD", "Name": "$DB_NAME", "MultiTenancy": {"Enabled": False}},
  "DefaultAdmin": {"UserName": "admin", "Email": "$ADMIN_EMAIL", "Password": ""},
  "Email": {"Provider": "Smtp", "FromAddress": "", "FromName": "",
            "Smtp": {"Host": "", "Port": 587, "User": "", "Password": "", "Security": "Auto"},
            "Graph": {"TenantId": "", "ClientId": "", "ClientSecret": "", "Sender": "", "SaveToSentItems": True}},
  "Authentication": {"EmailCode": {"RequireForPasswordLogin": True},
                     "Microsoft": {"Enabled": False, "ClientId": "", "ClientSecret": "", "TenantId": "common"}},
  "Cors": {"Mode": "restricted", "AllowedOrigins": ["$APP_URL"]},
}
with open(sys.argv[1], "w") as f:
    json.dump(settings, f, indent=2)
PY
  COOKIE_SECURE=false; [[ "$APP_URL" == https://* ]] && COOKIE_SECURE=true
  cat > "$WEB_ENV" <<EOF
SERVER_SECRET=$CONSUMER_SECRET
SESSION_SECRET=$(random_hex 32)
API_URL=http://127.0.0.1:15567
NEXT_PUBLIC_API_URL=$API_URL
APP_URL=$APP_URL
COOKIE_SECURE=$COOKIE_SECURE
NEXT_PUBLIC_SESSION_TIMEOUT=1500
NEXT_PUBLIC_SESSION_DIALOG_TIMEOUT=120
EOF
  CREATED_CONFIG=1
else
  log "Keeping existing configuration in $CONFIG_DIR"
  CREATED_CONFIG=0
fi
chown "$RUN_USER" "$SECRETS" "$WEB_ENV" 2>/dev/null || true
chmod 600 "$SECRETS" "$WEB_ENV"

cat > "$CONFIG_DIR/install.conf" <<CONF
# Saved by deploy/install.sh, read by later runs and by deploy/vgauto.sh.
REPO_DIR="$REPO_DIR"
PREFIX="$PREFIX"
CONFIG_DIR="$CONFIG_DIR"
DATA_DIR="$DATA_DIR"
RUN_USER="$RUN_USER"
SERVICE_MODE="$SERVICE_MODE"
LISTEN="$LISTEN"
CONF
chmod 644 "$CONFIG_DIR/install.conf"
if [[ "$LISTEN" == "local" ]]; then BIND_HOST="127.0.0.1"; else BIND_HOST="0.0.0.0"; fi

if [[ $CREATED_CONFIG -eq 1 ]]; then
  cat <<EOF

Configuration created. Before continuing make sure the database user exists:
  deploy/create-database.sh $SECRETS        (PostgreSQL or MySQL on this machine)
Email (SMTP or Microsoft Graph) must be configured in $SECRETS,
otherwise nobody receives the login codes.

EOF
  if [[ -t 0 ]]; then read -r -p "Continue with the installation now? [y/N] " answer; [[ "$answer" =~ ^[Yy] ]] || exit 0; fi
fi

# ---- API and migrations ---------------------------------------------------------
BUILD_DIR="$(mktemp -d)"
trap 'rm -rf "$BUILD_DIR"' EXIT

if [[ $SKIP_API -eq 0 ]]; then
  log "Building the API and the migration tool"
  # the projects expect a secrets file at build time (it is not deployed, the symlink below is used)
  touch "$REPO_DIR/backend/src/VgAuto.Http.Api/appsettings.Secrets.json"
  dotnet publish "$REPO_DIR/backend/src/VgAuto.Http.Api/VgAuto.Http.Api.csproj" -c Release -o "$BUILD_DIR/api" --nologo -v quiet
  dotnet publish "$REPO_DIR/backend/src/DbUp/DbUp.csproj" -c Release -o "$BUILD_DIR/dbup" --nologo -v quiet

  mkdir -p "$PREFIX/api" "$PREFIX/dbup"
  rsync -a --delete --exclude appsettings.Secrets.json --exclude pdf --exclude puppeteer "$BUILD_DIR/api/" "$PREFIX/api/"
  rsync -a --delete --exclude appsettings.Secrets.json "$BUILD_DIR/dbup/" "$PREFIX/dbup/"
  ln -sfn "$SECRETS" "$PREFIX/api/appsettings.Secrets.json"
  ln -sfn "$SECRETS" "$PREFIX/dbup/appsettings.Secrets.json"
  chown -R root:root "$PREFIX/api" "$PREFIX/dbup" 2>/dev/null || true
fi

if [[ $SKIP_MIGRATIONS -eq 0 ]]; then
  log "Applying database migrations"
  # The initial administrator password (only used when the database has no users yet) is kept in a
  # file readable by root only, so it is not lost when the installer output scrolls away.
  ADMIN_PASSWORD_FILE="$CONFIG_DIR/initial-admin-password"
  if [[ ! -f "$ADMIN_PASSWORD_FILE" ]]; then
    ADMIN_PW_CREATED=1
    ( umask 077; openssl rand -base64 18 | tr -d '/+=' | cut -c1-16 > "$ADMIN_PASSWORD_FILE" )
  fi
  (cd "$PREFIX/dbup" && as_user env DOTNET_NOLOGO=1 "DefaultAdmin__Password=$(cat "$ADMIN_PASSWORD_FILE")" dotnet DbUp.dll) \
    || die "migrations failed, see the output above"
fi

if [[ $SKIP_API -eq 0 ]]; then
  if [[ "$SERVICE_MODE" == "systemd" ]]; then
    log "Installing systemd service vg-auto-api"
    API_BIND="http://$BIND_HOST:15567"
    sed -e "s|@PREFIX@|$PREFIX|g" -e "s|@USER@|$RUN_USER|g" -e "s|@DATA@|$DATA_DIR|g" \
        -e "s|@API_URL_BIND@|$API_BIND|g" -e "s|@DOTNET@|$(command -v dotnet)|g" \
        "$REPO_DIR/deploy/templates/vg-auto-api.service" > /etc/systemd/system/vg-auto-api.service
    systemctl daemon-reload
    systemctl enable vg-auto-api >/dev/null
    systemctl restart vg-auto-api
  fi
fi

# ---- web app -------------------------------------------------------------------
if [[ $SKIP_WEB -eq 0 ]]; then
  log "Building the web app"
  mkdir -p "$PREFIX/web"
  rsync -a --delete --exclude node_modules --exclude .next --exclude .env "$REPO_DIR/frontend/" "$PREFIX/web/"
  ln -sfn "$WEB_ENV" "$PREFIX/web/.env"
  chown -R "$RUN_USER" "$PREFIX/web"
  # NEXT_PUBLIC_* values are compiled into the app, so it is built on the target machine
  (cd "$PREFIX/web" && as_user npm ci --no-audit --no-fund && as_user npm run build)
fi

# ---- pm2 ---------------------------------------------------------------------------
ECOSYSTEM="$PREFIX/ecosystem.config.cjs"
{
  echo "// generated by deploy/install.sh"
  echo "module.exports = { apps: ["
  if [[ $SKIP_WEB -eq 0 || -d "$PREFIX/web/.next" ]]; then
    cat <<EOF
  {
    name: 'vg-auto-web',
    cwd: '$PREFIX/web',
    script: 'node_modules/next/dist/bin/next',
    args: 'start -p 3000 -H $BIND_HOST',
    env: { NODE_ENV: 'production' },
    max_memory_restart: '600M',
  },
EOF
  fi
  if [[ "$SERVICE_MODE" == "pm2" ]]; then
    cat <<EOF
  {
    name: 'vg-auto-api',
    cwd: '$PREFIX/api',
    script: '$(command -v dotnet)',
    args: 'VgAuto.Http.Api.dll',
    interpreter: 'none',
    env: {
      ASPNETCORE_ENVIRONMENT: 'Production',
      Kestrel__Endpoints__Http__Url: 'http://$BIND_HOST:15567',
      PdfDirectory: '$DATA_DIR/pdf',
      PuppeteerPath: '$DATA_DIR/puppeteer',
    },
  },
EOF
  fi
  echo "] };"
} > "$ECOSYSTEM"
chown "$RUN_USER" "$ECOSYSTEM" 2>/dev/null || true

if grep -q "name:" "$ECOSYSTEM"; then
  log "Starting apps with pm2"
  as_user pm2 startOrReload "$ECOSYSTEM" --update-env
  as_user pm2 save
  if [[ "$OS" == "Darwin" ]]; then
    echo "To start VG Auto at login run the command printed by:  pm2 startup"
  else
    env PATH="$PATH:$(dirname "$(command -v node)")" pm2 startup systemd -u "$RUN_USER" --hp "$(eval echo ~"$RUN_USER")" >/dev/null
    systemctl enable "pm2-$RUN_USER" >/dev/null 2>&1 || true
  fi
fi

# ---- nginx ---------------------------------------------------------------------------
if [[ $NGINX -eq 1 ]]; then
  need nginx "apt install nginx"
  APP_DOMAIN="$(url_host "$APP_URL")"
  API_DOMAIN="$(url_host "$API_URL")"
  if [[ -f /etc/nginx/sites-available/vg-auto ]]; then
    log "Keeping the existing nginx site /etc/nginx/sites-available/vg-auto"
  else
    log "Writing nginx site for $APP_DOMAIN and $API_DOMAIN"
    sed -e "s|@APP_DOMAIN@|$APP_DOMAIN|g" -e "s|@API_DOMAIN@|$API_DOMAIN|g" \
        "$REPO_DIR/deploy/templates/nginx-vg-auto.conf" > /etc/nginx/sites-available/vg-auto
    echo "HTTPS: sudo certbot --nginx -d $APP_DOMAIN -d $API_DOMAIN"
  fi
  ln -sfn /etc/nginx/sites-available/vg-auto /etc/nginx/sites-enabled/vg-auto
  nginx -t && systemctl reload nginx
fi

# ---- control command ------------------------------------------------------------------
chmod +x "$REPO_DIR/deploy/vgauto.sh"
if ln -sfn "$REPO_DIR/deploy/vgauto.sh" /usr/local/bin/vgauto 2>/dev/null; then
  CONTROL="vgauto"
else
  CONTROL="$REPO_DIR/deploy/vgauto.sh"
  echo "Could not create /usr/local/bin/vgauto (no permission), use $CONTROL instead."
fi

log "Done"
# show the effective URLs of this installation
APP_URL="$(grep -E '^APP_URL=' "$WEB_ENV" | cut -d= -f2- || echo "$APP_URL")"
API_URL="$(grep -E '^NEXT_PUBLIC_API_URL=' "$WEB_ENV" | cut -d= -f2- || echo "$API_URL")"
if [[ "${ADMIN_PW_CREATED:-0}" -eq 1 ]]; then
  echo "  First login:    user 'admin', password in $ADMIN_PASSWORD_FILE (you must change it; delete the file afterwards)"
fi
cat <<EOF
  App:            $APP_URL
  API:            $API_URL   (health: /health)
  Configuration:  $SECRETS, $WEB_ENV
  Logs:           $( [[ "$SERVICE_MODE" == "systemd" ]] && echo "journalctl -u vg-auto-api -f" || echo "pm2 logs vg-auto-api" ),  pm2 logs vg-auto-web
  Listening on:   $BIND_HOST (web :3000, API :15567)
  Control:        $( [[ "$OS" == "Darwin" ]] && echo "$CONTROL" || echo "sudo $CONTROL" )   (start | stop | restart | status | logs | upgrade | backup | restore)
EOF
