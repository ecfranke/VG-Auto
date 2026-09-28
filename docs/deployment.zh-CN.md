# VG Auto 部署与配置指南（不使用 Docker）

VG Auto 由三部分组成：

| 组件 | 技术 | 默认端口 | 运行方式 |
|---|---|---|---|
| API | ASP.NET Core (.NET 9) | 15567 | Linux：systemd；macOS/Windows：pm2 或 Windows 服务 |
| Web 前端 | Next.js 15 | 3000 | pm2 |
| 数据库 | PostgreSQL 13+ **或** MySQL 8.0+ | 5432 / 3306 | 数据库自己的服务 |

另外还需要：Nginx（Linux 上做反向代理和 HTTPS；用宝塔面板的见 [1.6 节](#16-使用宝塔面板部署ubuntu--debian)）、一个能发信的邮箱（SMTP 或 Microsoft 365 / Graph）。

> 默认开启了“密码 + 邮件验证码”登录。**邮件没配好，就没人能登录。** 请先配好发信，
> 或者在 `Authentication:EmailCode:RequireForPasswordLogin` 里临时关闭验证码。

---

## 1. Linux（Debian 12 / Ubuntu 22.04+）：systemd + pm2 + nginx

### 1.1 安装依赖

```bash
git clone https://github.com/ecfranke/VG-Auto.git /opt/src/vg-auto
cd /opt/src/vg-auto
sudo deploy/prerequisites-debian.sh --db postgresql     # 或 --db mysql，或 --db none（数据库在别的机器上）
```

这个脚本会安装：.NET 9 SDK、Node.js 22 和 pm2、nginx 和 certbot、生成 PDF 用的 Chrome 所需的系统库，以及你选的数据库。

> 使用 MySQL 时：必须是 **MySQL 8.0+**，不支持 MariaDB（建表用了 `utf8mb4_0900_ai_ci` 排序规则）。
> Debian 自带的是 MariaDB，需要先添加 [MySQL APT 源](https://dev.mysql.com/downloads/repo/apt/)；Ubuntu 可以直接用 `mysql-server`。

### 1.2 第一次安装

```bash
sudo deploy/install.sh \
  --app-url https://app.example.com \
  --api-url https://api.example.com \
  --nginx \
  --db-provider PostgreSql \
  --admin-email boss@example.com
```

第一次运行时，脚本会生成配置并暂停，提示你完成两件事：

1. 创建数据库用户和数据库：
   ```bash
   sudo deploy/create-database.sh /etc/vg-auto/appsettings.Secrets.json
   ```
2. 编辑 `/etc/vg-auto/appsettings.Secrets.json`，填写 `Email` 部分（见第 5 节）。

然后再运行一次同样的 `install.sh`。它会依次完成下面这些事：

- 编译 API 和数据库迁移工具，装到 `/opt/vg-auto/api` 和 `/opt/vg-auto/dbup`
- 执行数据库迁移，第一次会创建管理员 `admin`
- 安装并启动 systemd 服务 `vg-auto-api`
- 在本机编译前端（`/opt/vg-auto/web`），用 pm2 启动 `vg-auto-web`，并设置开机自启
- 写入并启用 nginx 站点 `/etc/nginx/sites-available/vg-auto`（已存在时保留不动，以免覆盖 certbot 的修改）
- 创建全局控制命令 `vgauto`（见 1.5 节）

加了 `--nginx` 时，API 和前端只监听 127.0.0.1，外网只能通过 nginx 访问。
用别的反向代理（宝塔、Caddy 等）时改用 `--proxy`，效果相同，只是不写 nginx 配置。

最后配置 HTTPS：

```bash
sudo certbot --nginx -d app.example.com -d api.example.com
```

### 1.3 第一次登录

- 用户名：`admin`
- 密码：保存在 `/etc/vg-auto/initial-admin-password`，只有 root 能读
- 登录时会把验证码发到 `--admin-email` 指定的邮箱
- 首次登录必须修改密码（至少 10 位，不能包含用户名）。改完后请删除上面那个密码文件。

### 1.4 目录与文件

| 路径 | 内容 |
|---|---|
| `/etc/vg-auto/appsettings.Secrets.json` | API 配置，包括密钥、数据库、邮件、登录方式（权限 600，属主 vgauto） |
| `/etc/vg-auto/web.env` | 前端配置（权限 600） |
| `/opt/vg-auto/api`、`/opt/vg-auto/dbup`、`/opt/vg-auto/web` | 程序文件 |
| `/var/lib/vg-auto/pdf`、`/var/lib/vg-auto/puppeteer` | 生成的 PDF、PDF 渲染用的 Chrome |

### 1.5 日常运维：`vgauto` 控制命令

安装脚本会把 `deploy/vgauto.sh` 链接成全局命令 `vgauto`。直接运行 `sudo vgauto` 会出现数字菜单；也可以带参数运行，方便写进计划任务：

| 命令 | 作用 |
|---|---|
| `sudo vgauto status` | 服务状态、健康检查、访问地址、数据库和版本 |
| `sudo vgauto start` / `stop` / `restart` | 同时启动 / 停止 / 重启 API 和前端 |
| `sudo vgauto logs api` / `logs web` | 实时查看 API 或前端日志（Ctrl+C 退出） |
| `sudo vgauto upgrade` | 先自动备份，再 `git pull`，然后重新安装（配置保留，数据库迁移自动执行） |
| `sudo vgauto backup` | 把数据库、PDF 和配置打包成一个 `.tar.gz`，默认放在 `/var/backups/vg-auto` |
| `sudo vgauto restore <文件>` | 从备份恢复数据库和 PDF（要输入 `yes` 确认，恢复前会自动再备份一次当前状态） |
| `sudo vgauto pdf-setup` | 安装 PDF 需要的系统库和中文字体，准备浏览器（下载或发送报价单/发票 PDF 失败时运行） |
| `sudo vgauto help` | 全部命令和选项 |

- 备份：`--dir <目录>` 指定目录，`--keep 14` 只保留最近 14 份（更早的会被删除）。
  每天自动备份可以在 root 的 crontab 里加一行：`30 2 * * * /usr/local/bin/vgauto backup --keep 14`
- 备份文件里有数据库密码等配置，权限是 600，请妥善保存，最好再复制一份到别的机器。
- 恢复：默认只恢复数据库和 PDF，保留当前服务器的配置；在新服务器上整体迁移时加 `--with-config`，连配置一起恢复。
  备份和恢复必须是同一种数据库（PostgreSQL 的备份不能恢复到 MySQL）。
- 备份需要数据库客户端工具（`pg_dump` / `psql` 或 `mysqldump` / `mysql`），脚本也会在宝塔的安装目录里查找。

安装时的选项（安装目录、运行账号、API 的运行方式、是否只监听本机）保存在 `/etc/vg-auto/install.conf`。
以后升级不用再写这些参数，`sudo vgauto upgrade` 或 `git pull && sudo deploy/install.sh` 都会沿用它们。

修改 `appsettings.Secrets.json` 后执行 `sudo vgauto restart`。修改 `web.env` 后要看改的是哪一项：

- 改了 `NEXT_PUBLIC_*`：这些值是编译进前端的，需要执行 `sudo vgauto upgrade`（或重新运行 `install.sh`）重新编译前端。
- 只改了其他项：`sudo vgauto restart` 即可。

### 1.6 使用宝塔面板部署（Ubuntu / Debian）

适合服务器上已经装了宝塔面板的情况。分工如下：

| 部分 | 由谁负责 |
|---|---|
| 网站、反向代理、HTTPS 证书 | 宝塔（宝塔自带的 Nginx） |
| 数据库（MySQL 或 PostgreSQL） | 宝塔软件商店安装，在宝塔里建库 |
| VG Auto 的 API 和前端 | `vgauto baota`：API 作为 systemd 服务 `vg-auto-api`，前端由 pm2 运行 `vg-auto-web`，两者都只监听 127.0.0.1 |

> **注意**：不要运行 1.1 节的 `prerequisites-debian.sh`，也不要加 `--nginx`。前者会用 apt 另装一个 Nginx，
> 和宝塔的 Nginx 抢 80/443 端口。`vgauto baota` 只安装需要的部分，不碰 Nginx 和数据库。
>
> 宝塔各版本的菜单名称略有不同，下面以宝塔 Linux 面板 9.x 为准。

#### 1.6.1 在宝塔里准备环境

1. **Nginx**：在「软件商店」里安装 Nginx（任意稳定版本）。
2. **数据库**：二选一。
   - **MySQL**：在「软件商店」里安装 **MySQL 8.0 或更高版本**。宝塔默认推荐的可能是 5.7，安装时要手动选择 8.0。
     不能用 MariaDB。
   - **PostgreSQL**：在「软件商店」里搜索并安装「PostgreSQL 管理器」，再在里面安装 PostgreSQL 13 或更高版本。
3. **不要**用宝塔的「Node.js 版本管理器」装 Node。它装在 `/www/server/nodejs` 下，用 `sudo` 运行时找不到。
   `vgauto baota` 会用系统的包管理器安装 Node.js。

#### 1.6.2 在宝塔里创建数据库

先想好一个数据库密码（下面记作 `<数据库密码>`），安装时要用。

**MySQL**：「数据库」→「MySQL」→「添加数据库」：

| 项目 | 填写 |
|---|---|
| 数据库名 | `vgauto` |
| 用户名 | `vgauto` |
| 密码 | `<数据库密码>` |
| 访问权限 | 本地服务器 |
| 编码 / 字符集 | `utf8mb4` |

**PostgreSQL**：「数据库」→「PgSQL」（或打开「PostgreSQL 管理器」）→「添加数据库」，
数据库名和用户名都填 `vgauto`，密码填 `<数据库密码>`。

建好后，表由安装脚本自动创建，不需要导入任何 SQL。

> MySQL 常见问题：安装时如果报 `Access denied for user 'vgauto'@'127.0.0.1'`，是因为宝塔的 MySQL 默认开启了
> `skip-name-resolve`，“本地服务器”权限只匹配 `localhost`。在「数据库」列表里点这个库的「权限」，
> 改成「指定 IP」`127.0.0.1`，再重新运行安装命令即可。

#### 1.6.3 一键安装

先把两个子域名（例如 `app.你的域名` 和 `api.你的域名`）的 A 记录解析到这台服务器，然后用 SSH（或宝塔的「终端」）执行：

```bash
sudo apt-get install -y git
sudo git clone https://github.com/ecfranke/VG-Auto.git /opt/src/vg-auto
cd /opt/src/vg-auto

# MySQL
sudo deploy/vgauto.sh baota \
  --app-url https://app.你的域名 \
  --api-url https://api.你的域名 \
  --db-provider MySql --db-host 127.0.0.1 --db-password '<数据库密码>' \
  --admin-email 管理员邮箱

# PostgreSQL：把 --db-provider 那一行换成
#   --db-provider PostgreSql --db-host 127.0.0.1 --db-password '<数据库密码>' \
```

`vgauto baota` 会依次：

1. 安装 .NET 9 SDK、Node.js 22、pm2，以及生成 PDF 所需的系统库（**不装** Nginx，**不装也不建**数据库）；
2. 以 `--proxy` 方式运行 `install.sh`：API 和前端只监听 `127.0.0.1`，外网只能通过宝塔的反向代理访问。

第一次运行时，`install.sh` 会生成配置，然后问 `Continue with the installation now?`：

- 先输入 `N` 退出，编辑 `/etc/vg-auto/appsettings.Secrets.json`，填好 `Email` 部分（见第 5 节）。
  数据库已经在宝塔里建好，**不需要**运行 `create-database.sh`。
- 再运行一次同样的命令，这次输入 `y`。脚本会编译、建表、启动 API 和前端，并创建全局命令 `vgauto`。

完成后执行 `sudo vgauto status`，两项服务都应显示 online，健康检查都应通过。

#### 1.6.4 在宝塔里配置反向代理和 HTTPS

需要两个站点，一个给前端，一个给 API。

1. 「网站」→「添加站点」，域名填 `app.你的域名`，PHP 版本选「纯静态」，不创建数据库。
2. 打开这个站点的「设置」→「反向代理」→「添加反向代理」：
   - 目标 URL：`http://127.0.0.1:3000`
   - 发送域名：`$host`
3. 同样再添加站点 `api.你的域名`，反向代理的目标 URL 填 `http://127.0.0.1:15567`，发送域名 `$host`。
4. 分别在两个站点的「SSL」里申请 Let's Encrypt 证书，并打开「强制 HTTPS」。

新版宝塔也可以直接在「网站」→「反向代理」→「添加反代」里创建，填写的内容相同。

宝塔默认的反向代理配置会带上 `X-Real-IP` 和 `X-Forwarded-For`，VG Auto 用它们来识别客户端 IP（登录限流和账号锁定），不用额外修改。

#### 1.6.5 防火墙

在宝塔的「安全」页面，只放行 80、443、SSH 端口和宝塔面板端口，**不需要**放行 3000 和 15567。
这两个端口只监听在 127.0.0.1 上，就算放行了外网也连不上。云服务器的安全组同样只开放 80 和 443 即可。

#### 1.6.6 日常运维

用 1.5 节的 `vgauto` 命令：`sudo vgauto status`、`restart`、`logs`、`backup`、`upgrade` 等。
升级时直接 `sudo vgauto upgrade`，会沿用宝塔模式（只监听本机），不会装 Nginx。

- 宝塔的「PM2 管理器」看不到 `vg-auto-web`：它由系统用户 `vgauto` 运行，请用 `sudo vgauto status` / `sudo vgauto logs web` 查看。
- 备份：`sudo vgauto backup` 会同时备份数据库、PDF 和配置。可以在宝塔「计划任务」里添加一个 Shell 脚本任务，
  内容是 `/usr/local/bin/vgauto backup --keep 14`，每天执行一次。

---

### 1.7 PDF 下载或发送失败

报价单和发票的 PDF 由服务器上的无头 Chrome 生成。安装时会自动准备浏览器：优先用系统里已经安装的 Chrome / Chromium / Edge，没有的话从 `storage.googleapis.com` 下载一份 Chrome 到 `/var/lib/vg-auto/puppeteer`。

- 页面上提示 “no browser is available on the server”：服务器无法下载 Chrome（例如网络访问不到 Google）。运行 `sudo vgauto pdf-setup`；仍然失败时先手动安装 Google Chrome 或 Chromium，再运行一次，或在 `appsettings.Secrets.json` 里设置 `PuppeteerExecutablePath` 指向浏览器。
- 提示 “the browser on the server does not start”：缺少系统库。运行 `sudo vgauto pdf-setup` 安装。
- PDF 里的中文显示成方框：缺少中文字体，`sudo vgauto pdf-setup` 会安装 `fonts-noto-cjk`。
- 详细错误在 `sudo vgauto logs api` 里。

## 2. macOS

1. 安装依赖：
   ```bash
   brew install dotnet@9 node postgresql@16     # 或者 mysql
   brew services start postgresql@16
   npm install -g pm2
   ```
2. 安装。API 和前端都用 pm2 运行，默认装到 `~/vg-auto`：
   ```bash
   deploy/install.sh --app-url http://localhost:3000 --admin-email you@example.com
   ```
   第一次运行时，按提示创建数据库用户，并填写 `~/vg-auto/config/appsettings.Secrets.json` 里的 `Email` 部分，然后再运行一次。
3. 开机自启：运行 `pm2 startup`，再执行它打印出来的那条命令。
4. 日常运维用 `vgauto`（不用 sudo）：`vgauto status`、`vgauto restart`、`vgauto backup` 等，见 1.5 节。
   备份默认放在 `~/vg-auto/backups`。如果安装脚本没能创建 `/usr/local/bin/vgauto`（没有写权限），就用仓库里的 `deploy/vgauto.sh`。

---

## 3. Windows 10/11 或 Windows Server

1. 安装 .NET 9 SDK、Node.js 22，以及 PostgreSQL 或 MySQL 8。
2. 在仓库目录里打开**管理员** PowerShell：
   ```powershell
   powershell -ExecutionPolicy Bypass -File deploy\windows\install.ps1 -DbProvider MySql -AppUrl http://workshop-pc:3000 -AdminEmail boss@example.com
   ```
   第一次运行时，脚本会在 `C:\VGAuto\config` 生成配置然后退出。按提示创建数据库用户、填写邮件配置，再运行一次。
3. 运行方式：
   - API 注册为 Windows 服务 `VGAutoApi`，开机自动启动。
   - 前端由 pm2 运行，另有一个计划任务“VG Auto pm2”，在登录时恢复 pm2 进程。
4. 如果其他电脑也要访问，需要在 Windows 防火墙里放行 3000 和 15567 端口。
5. 修改 `C:\VGAuto\config` 下的配置后，需要重新运行安装脚本，它会把配置复制到程序目录并重启服务。

---

## 4. 数据库

在配置文件的 `DbOptions:Provider` 里二选一。

**PostgreSQL（默认）**
```json
"DbOptions": { "Provider": "PostgreSql", "Host": "localhost", "Port": 5432, "UserId": "vgauto", "Password": "...", "Name": "vgauto" }
```

**MySQL 8**
```json
"DbOptions": { "Provider": "MySql", "Host": "127.0.0.1", "Port": 3306, "UserId": "vgauto", "Password": "...", "Name": "vgauto" }
```

- 数据库迁移工具 `DbUp` 会在数据库不存在时自动创建（前提是数据库用户有建库权限），然后执行对应的建表脚本：PostgreSQL 用 `backend/src/DbUp/scripts`，MySQL 用 `backend/src/DbUp/scripts_mysql`。
- 以后要改表结构，两套脚本都要各加一个（编号接着往后排）。
- 所有时间都以 UTC 存储，打印的单据按服务器所在时区显示日期。
- 多租户（演示租户）功能只支持 PostgreSQL，默认关闭。
- 数据库备份：请配置每天自动备份，例如在 crontab 里加一行：
  ```bash
  # PostgreSQL
  0 2 * * * sudo -u postgres pg_dump vgauto | gzip > /var/backups/vg-auto-$(date +\%F).sql.gz
  # MySQL
  0 2 * * * mysqldump --single-transaction vgauto | gzip > /var/backups/vg-auto-$(date +\%F).sql.gz
  ```

---

## 5. 邮件：SMTP 或 Microsoft Graph

以下邮件都走同一套发信配置：报价单和发票（附 PDF）、登录验证码、找回密码验证码。设置页有“Send test email”按钮，可以用来测试。

### 5.1 SMTP

```json
"Email": {
  "Provider": "Smtp",
  "FromAddress": "noreply@yourshop.com",
  "FromName": "Your Shop",
  "Smtp": { "Host": "smtp.example.com", "Port": 587, "User": "noreply@yourshop.com", "Password": "...", "Security": "Auto" }
}
```

`Security` 有四个取值：

| 取值 | 含义 |
|---|---|
| `Auto` | 端口 465 用 SSL 直连，其他端口在服务器支持时自动用 STARTTLS |
| `SslOnConnect` | 强制 SSL 直连 |
| `StartTls` | 强制 STARTTLS |
| `None` | 不加密，只适合本机测试 |

`FromAddress` 留空时，会用“设置 → 公司信息”里的邮箱作为发件人。多数邮件服务商要求发件人和登录账号一致，建议填写。

### 5.2 Microsoft Graph（Microsoft 365 邮箱）

1. 在 [Entra 管理中心](https://entra.microsoft.com) 打开“应用注册 → 新注册”，名称随意，账户类型选“仅此组织目录”。
2. 在“证书和密码”里新建客户端密码，并记下这个值。
3. 在“API 权限”里添加 Microsoft Graph → **应用程序权限** → `Mail.Send`，然后点“授予管理员同意”。
4. 强烈建议把这个应用限制为只能用一个邮箱发信（Exchange Online PowerShell）：
   ```powershell
   New-ApplicationAccessPolicy -AppId <ClientId> -PolicyScopeGroupId noreply@yourshop.com -AccessRight RestrictAccess -Description "VG Auto sender"
   ```
5. 填写配置：
   ```json
   "Email": {
     "Provider": "Graph",
     "FromName": "Your Shop",
     "Graph": { "TenantId": "<目录(租户) ID>", "ClientId": "<应用程序(客户端) ID>", "ClientSecret": "<密码值>", "Sender": "noreply@yourshop.com", "SaveToSentItems": true }
   }
   ```

---

## 6. 登录方式

```json
"Authentication": {
  "EmailCode": { "RequireForPasswordLogin": true, "AllowUsersWithoutEmail": false, "CodeLifetimeMinutes": 10, "MaxAttempts": 5, "MaxSends": 3 },
  "PasswordReset": { "Enabled": true },
  "Microsoft": { "Enabled": true, "ClientId": "...", "ClientSecret": "...", "TenantId": "common" }
}
```

- **密码 + 邮件验证码**：输入正确的密码后，系统会给账号邮箱发一个 6 位验证码，10 分钟内有效，每个验证码最多试 5 次、最多重发 3 次。输错的验证码也会计入账号锁定次数：累计错 10 次，锁定 15 分钟。
  - 没有邮箱的账号：默认不能用密码登录，需要管理员先在“员工”里给他补上邮箱。设置 `AllowUsersWithoutEmail: true` 可以让这类账号跳过验证码（不推荐）。
- **找回密码**：登录页的“Forgot password?”，输入用户名或邮箱，收到验证码后设置新密码。无论账号是否存在，页面的反应都一样，所以不会泄露哪些账号存在。
- **Microsoft 账号登录**：任何工作/学校账号或个人 Microsoft 账号都能用，但前提是系统里已经有一个邮箱相同的用户。
  - **第一次**用某个 Microsoft 账号登录时，系统会向该用户在本系统里登记的邮箱发验证码，输入后才完成绑定。这是为了防止别人伪造邮箱声明冒充用户。
  - 绑定之后再登录就不需要验证码了。已绑定的账号可以在“个人资料”页解绑。
  - 应用注册步骤：
    1. 新建应用注册，账户类型选“任何组织目录中的帐户和个人 Microsoft 帐户”。
    2. 在“身份验证”里添加平台“Web”，重定向 URI 填 `https://app.example.com/auth/microsoft/callback`，要和 `web.env` 里的 `APP_URL` 一致。
    3. 在“证书和密码”里新建客户端密码。
    4. `TenantId` 保持 `common`。如果只允许自己公司的账号登录，就填你的租户 ID。
    5. 建议在“令牌配置”里添加可选声明 `email`。
- **强制改密码**：初始管理员第一次登录必须修改密码。旧版本数据库里如果还在用默认密码 `carcare`，升级后也会被要求修改。

---

## 7. 安全相关配置

| 配置 | 说明 |
|---|---|
| `JwtOptions:Secret` / `ConsumerSecret` | 安装脚本会随机生成。长度不够或还是占位符时，API 拒绝启动。`ConsumerSecret` 必须和 `web.env` 里的 `SERVER_SECRET` 相同。 |
| `Cors:AllowedOrigins` | 允许访问 API 的前端地址，填 `APP_URL`。`Cors:Mode=open` 只允许在开发环境使用。 |
| `Swagger:Enabled` | API 文档，生产环境默认关闭。 |
| `Errors:IncludeDetails` | 是否向前端返回异常详情，生产环境默认关闭。 |
| `Demo:Enabled` | 匿名创建演示租户，默认关闭。 |
| `ForwardedHeaders:KnownProxies` | 前端或 nginx 不在本机时，把它们的 IP 填在这里，否则登录限流会按代理的 IP 计数。 |
| `COOKIE_SECURE`（web.env） | 使用 HTTPS 时设为 `true`。 |

---

## 8. 本地开发（不用 Docker）

```bash
scripts/setup-secrets.sh                   # 生成开发用的密钥（Windows 用 scripts/setup-secrets.ps1）
# 编辑 backend/src/VgAuto.Http.Api/appsettings.Secrets.json：填写数据库和邮件
# （AllowedOrigins 默认已包含 http://localhost:3000）
cd backend/src/DbUp && dotnet run          # 建库、建表，并打印一次初始管理员密码
cd ../VgAuto.Http.Api && ASPNETCORE_ENVIRONMENT=Development dotnet run
cd ../../../frontend && npm ci && npm run dev
```

本地收邮件可以用 [Mailpit](https://mailpit.axllent.org/)，SMTP 端口 1025，`Security` 设为 `None`。

## 9. 测试

```bash
cd backend/tests/VgAuto.Tests
dotnet test                                                     # 只跑单元测试
VGAUTO_TEST_DB_HOST=localhost VGAUTO_TEST_DB_PASSWORD=... dotnet test                                         # PostgreSQL 集成测试
VGAUTO_TEST_DB_PROVIDER=MySql VGAUTO_TEST_DB_HOST=127.0.0.1 VGAUTO_TEST_DB_PASSWORD=... dotnet test          # MySQL 集成测试
```

集成测试会为每次运行新建一个临时数据库，跑完自动删除。
