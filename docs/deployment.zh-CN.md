# VG-Auto 部署与配置指南（不使用 Docker）

本项目基于开源的 CarCare（AGPL-3.0）。系统由三部分组成：

| 组件 | 技术 | 默认端口 | 运行方式 |
|---|---|---|---|
| API | ASP.NET Core (.NET 9) | 15567 | Linux：systemd；macOS/Windows：pm2 或 Windows 服务 |
| Web 前端 | Next.js 15 | 3000 | pm2 |
| 数据库 | PostgreSQL 13+ **或** MySQL 8.0+ | 5432 / 3306 | 数据库自己的服务 |

另外还需要：Nginx（Linux 上做反向代理和 HTTPS）、一个能发信的邮箱（SMTP 或 Microsoft 365 / Graph）。

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
   sudo deploy/create-database.sh /etc/carcare/appsettings.Secrets.json
   ```
2. 编辑 `/etc/carcare/appsettings.Secrets.json`，填写 `Email` 部分（见第 5 节）。

然后再运行一次同样的 `install.sh`。它会依次完成下面这些事：

- 编译 API 和数据库迁移工具，装到 `/opt/carcare/api` 和 `/opt/carcare/dbup`
- 执行数据库迁移，第一次会创建管理员 `admin`
- 安装并启动 systemd 服务 `carcare-api`
- 在本机编译前端（`/opt/carcare/web`），用 pm2 启动 `carcare-web`，并设置开机自启
- 写入并启用 nginx 站点 `/etc/nginx/sites-available/carcare`

最后配置 HTTPS：

```bash
sudo certbot --nginx -d app.example.com -d api.example.com
```

### 1.3 第一次登录

- 用户名：`admin`
- 密码：保存在 `/etc/carcare/initial-admin-password`，只有 root 能读
- 登录时会把验证码发到 `--admin-email` 指定的邮箱
- 首次登录必须修改密码（至少 10 位，不能包含用户名）。改完后请删除上面那个密码文件。

### 1.4 目录与文件

| 路径 | 内容 |
|---|---|
| `/etc/carcare/appsettings.Secrets.json` | API 配置，包括密钥、数据库、邮件、登录方式（权限 600，属主 carcare） |
| `/etc/carcare/web.env` | 前端配置（权限 600） |
| `/opt/carcare/api`、`/opt/carcare/dbup`、`/opt/carcare/web` | 程序文件 |
| `/var/lib/carcare/pdf`、`/var/lib/carcare/puppeteer` | 生成的 PDF、PDF 渲染用的 Chrome |

### 1.5 日常运维

```bash
sudo systemctl status carcare-api          # 查看 API 状态
journalctl -u carcare-api -f               # API 日志
sudo -u carcare pm2 logs carcare-web       # 前端日志
curl http://127.0.0.1:15567/health         # 健康检查

# 升级：拉取新代码后重新运行安装脚本（配置会保留，数据库迁移会自动执行）
cd /opt/src/vg-auto && git pull && sudo deploy/install.sh
```

修改 `appsettings.Secrets.json` 后需要执行 `sudo systemctl restart carcare-api`。

修改 `web.env` 后要看改的是哪一项：

- 改了 `NEXT_PUBLIC_*`：这些值是编译进前端的，需要重新运行 `install.sh`，它会重新编译前端。
- 只改了其他项：执行 `sudo -u carcare pm2 restart carcare-web` 即可。

---

## 2. macOS

1. 安装依赖：
   ```bash
   brew install dotnet@9 node postgresql@16     # 或者 mysql
   brew services start postgresql@16
   npm install -g pm2
   ```
2. 安装。API 和前端都用 pm2 运行，默认装到 `~/carcare`：
   ```bash
   deploy/install.sh --app-url http://localhost:3000 --admin-email you@example.com
   ```
   第一次运行时，按提示创建数据库用户，并填写 `~/carcare/config/appsettings.Secrets.json` 里的 `Email` 部分，然后再运行一次。
3. 开机自启：运行 `pm2 startup`，再执行它打印出来的那条命令。

---

## 3. Windows 10/11 或 Windows Server

1. 安装 .NET 9 SDK、Node.js 22，以及 PostgreSQL 或 MySQL 8。
2. 在仓库目录里打开**管理员** PowerShell：
   ```powershell
   powershell -ExecutionPolicy Bypass -File deploy\windows\install.ps1 -DbProvider MySql -AppUrl http://workshop-pc:3000 -AdminEmail boss@example.com
   ```
   第一次运行时，脚本会在 `C:\CarCare\config` 生成配置然后退出。按提示创建数据库用户、填写邮件配置，再运行一次。
3. 运行方式：
   - API 注册为 Windows 服务 `CarCareApi`，开机自动启动。
   - 前端由 pm2 运行，另有一个计划任务“CarCare pm2”，在登录时恢复 pm2 进程。
4. 如果其他电脑也要访问，需要在 Windows 防火墙里放行 3000 和 15567 端口。
5. 修改 `C:\CarCare\config` 下的配置后，需要重新运行安装脚本，它会把配置复制到程序目录并重启服务。

---

## 4. 数据库

在配置文件的 `DbOptions:Provider` 里二选一。

**PostgreSQL（默认）**
```json
"DbOptions": { "Provider": "PostgreSql", "Host": "localhost", "Port": 5432, "UserId": "carcare", "Password": "...", "Name": "carcare" }
```

**MySQL 8**
```json
"DbOptions": { "Provider": "MySql", "Host": "127.0.0.1", "Port": 3306, "UserId": "carcare", "Password": "...", "Name": "carcare" }
```

- 数据库迁移工具 `DbUp` 会在数据库不存在时自动创建（前提是数据库用户有建库权限），然后执行对应的建表脚本：PostgreSQL 用 `backend/src/DbUp/scripts`，MySQL 用 `backend/src/DbUp/scripts_mysql`。
- 以后要改表结构，两套脚本都要各加一个（编号接着往后排）。
- 所有时间都以 UTC 存储，打印的单据按服务器所在时区显示日期。
- 多租户（演示租户）功能只支持 PostgreSQL，默认关闭。
- 数据库备份：请配置每天自动备份，例如在 crontab 里加一行：
  ```bash
  # PostgreSQL
  0 2 * * * sudo -u postgres pg_dump carcare | gzip > /var/backups/carcare-$(date +\%F).sql.gz
  # MySQL
  0 2 * * * mysqldump --single-transaction carcare | gzip > /var/backups/carcare-$(date +\%F).sql.gz
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
   New-ApplicationAccessPolicy -AppId <ClientId> -PolicyScopeGroupId noreply@yourshop.com -AccessRight RestrictAccess -Description "CarCare sender"
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
# 编辑 backend/src/Carmasters.Http.Api/appsettings.Secrets.json：填写数据库和邮件
# （AllowedOrigins 默认已包含 http://localhost:3000）
cd backend/src/DbUp && dotnet run          # 建库、建表，并打印一次初始管理员密码
cd ../Carmasters.Http.Api && ASPNETCORE_ENVIRONMENT=Development dotnet run
cd ../../../frontend && npm ci && npm run dev
```

本地收邮件可以用 [Mailpit](https://mailpit.axllent.org/)，SMTP 端口 1025，`Security` 设为 `None`。

## 9. 测试

```bash
cd backend/tests/Carmasters.Tests
dotnet test                                                     # 只跑单元测试
CARCARE_TEST_DB_HOST=localhost CARCARE_TEST_DB_PASSWORD=... dotnet test                                         # PostgreSQL 集成测试
CARCARE_TEST_DB_PROVIDER=MySql CARCARE_TEST_DB_HOST=127.0.0.1 CARCARE_TEST_DB_PASSWORD=... dotnet test          # MySQL 集成测试
```

集成测试会为每次运行新建一个临时数据库，跑完自动删除。
