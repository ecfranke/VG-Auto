#!/usr/bin/env bash
# Installs what CarCare needs on Debian 12 / Ubuntu 22.04+ (run with sudo):
#   .NET 9 SDK, Node.js 22 + pm2, nginx + certbot, libraries for the PDF renderer (Chrome),
#   and optionally a database server.
#
#   sudo deploy/prerequisites-debian.sh --db postgresql   (or --db mysql, or --db none)
set -euo pipefail
DB="postgresql"
while [[ $# -gt 0 ]]; do
  case "$1" in
    --db) DB="$2"; shift 2 ;;
    *) echo "Unknown option $1" >&2; exit 1 ;;
  esac
done

. /etc/os-release
export DEBIAN_FRONTEND=noninteractive
apt-get update
apt-get install -y ca-certificates curl gnupg rsync openssl python3 sudo nginx certbot python3-certbot-nginx \
  fonts-liberation libasound2t64 libatk-bridge2.0-0 libatk1.0-0 libcups2 libdrm2 libgbm1 libgtk-3-0 libnspr4 libnss3 \
  libxcomposite1 libxdamage1 libxfixes3 libxkbcommon0 libxrandr2 libpango-1.0-0 libcairo2 xdg-utils 2>/dev/null \
|| apt-get install -y ca-certificates curl gnupg rsync openssl python3 sudo nginx certbot python3-certbot-nginx \
  fonts-liberation libasound2 libatk-bridge2.0-0 libatk1.0-0 libcups2 libdrm2 libgbm1 libgtk-3-0 libnspr4 libnss3 \
  libxcomposite1 libxdamage1 libxfixes3 libxkbcommon0 libxrandr2 libpango-1.0-0 libcairo2 xdg-utils

# .NET 9 SDK (Microsoft package repository)
if ! dotnet --list-sdks 2>/dev/null | grep -q '^9\.'; then
  curl -fsSL "https://packages.microsoft.com/config/${ID}/${VERSION_ID}/packages-microsoft-prod.deb" -o /tmp/packages-microsoft-prod.deb
  dpkg -i /tmp/packages-microsoft-prod.deb && rm /tmp/packages-microsoft-prod.deb
  apt-get update && apt-get install -y dotnet-sdk-9.0
fi

# Node.js 22 (NodeSource) and pm2
if ! command -v node >/dev/null || [[ "$(node -v | cut -d. -f1 | tr -d v)" -lt 20 ]]; then
  mkdir -p /etc/apt/keyrings
  curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key | gpg --dearmor -o /etc/apt/keyrings/nodesource.gpg
  echo "deb [signed-by=/etc/apt/keyrings/nodesource.gpg] https://deb.nodesource.com/node_22.x nodistro main" > /etc/apt/sources.list.d/nodesource.list
  apt-get update && apt-get install -y nodejs
fi
command -v pm2 >/dev/null || npm install -g pm2

case "$DB" in
  postgresql) apt-get install -y postgresql ;;
  mysql)
    # MySQL 8 is required (MariaDB is not supported: the schema uses the utf8mb4_0900_ai_ci collation).
    if ! apt-get install -y mysql-server; then
      echo "mysql-server is not available in this distribution (Debian ships MariaDB)." >&2
      echo "Add the MySQL APT repository first: https://dev.mysql.com/downloads/repo/apt/" >&2
      exit 1
    fi ;;
  none) ;;
  *) echo "--db must be postgresql, mysql or none" >&2; exit 1 ;;
esac
echo "Prerequisites installed."
