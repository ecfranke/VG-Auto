<p align="center"><img src="frontend/public/logo.svg" width="72" alt="VG Auto"></p>

<h1 align="center">VG Auto</h1>

<p align="center"><a href="README.en.md">English</a> | 中文</p>

VG Auto 是一套自托管的汽修厂管理系统。从接车开报价、客户确认、维修，到开发票、收款，全部在浏览器里完成。报价单和发票自动生成 PDF，并通过邮件发给客户。

本文分三部分：

1. [使用指南](#使用指南)：给前台和技师看，按日常流程一步步操作
2. [安装](#安装)：给负责部署的人看
3. [开发者文档](#开发者文档)：架构、配置、接口、数据库迁移和测试

---

## 使用指南

### 一个工单的完整流程

```
新建客户 → 登记车辆 → 新建工单（报价）→ 录入配件和工时 → 出具报价并发给客户
        → 客户接受，自动转成维修任务 → 维修完成，开发票 → 标记已收款
```

界面左侧是主菜单：**Work**（工单）、**Clients**（客户）、**Vehicles**（车辆）、**Inventory**（配件库存）、**Settings**（设置）。左下角是当前用户，点开可以进入个人资料或退出登录。

### 1. 登录

输入用户名和密码，点 **Sign in**。

![登录](docs/screenshots/login.png)

系统会往你的邮箱发一个 6 位验证码。输入后点 **Verify**。没收到可以点 **Send a new code** 重新发送，验证码 10 分钟内有效。

![输入验证码](docs/screenshots/login-code.png)

- 忘记密码：在登录页点 **Forgot password?**，输入用户名或邮箱，用收到的验证码设置新密码。
- 如果管理员启用了 Microsoft 登录，登录页会出现 **Sign in with Microsoft** 按钮。第一次使用时需要先用邮箱验证码确认一次，之后可以直接用 Microsoft 账号登录。
- 连续输错密码会被暂时锁定，稍后再试即可。
- 初始管理员第一次登录时，系统会要求先修改密码。

![找回密码](docs/screenshots/forgot-password.png)

### 2. 新建客户

**Clients → Add new**。个人客户填写姓名、电话、邮箱；企业客户打开顶部的开关后填写公司名称和税号。邮箱很重要，报价单和发票都会发到这个地址。点 **Save** 保存。

![新建客户](docs/screenshots/client-new.png)

客户列表支持按姓名、电话、邮箱搜索，点客户名可以查看详情、车辆和历史工单。

![客户列表](docs/screenshots/clients.png)

### 3. 登记车辆

**Vehicles → Add new**。填写品牌、型号、VIN、车牌号和里程，在 **Owner** 里输入客户名并从下拉列表中选择车主。

![新建车辆](docs/screenshots/vehicle-new.png)

车辆换了车主时，改 Owner 即可，系统会保留归属变更记录。

### 4. 新建工单

**Work → Add new**：

- **Start with**：打开 **Offer** 表示先给客户报价；关闭则直接开始维修任务（适合小修、不需要报价的情况）。
- **Client**：输入客户名选择。散客不想登记信息时，打开 **Undisclosed**。
- **Vehicle**：默认只列出该客户名下的车辆；打开 **Search all vehicles** 可以搜索所有车辆。右边的输入框填写本次进厂里程。
- **Mechanics**：选择负责的技师后点 **Add**；技师不在列表里时点 **New** 直接新建。
- **About**：填写客户描述的故障或本次保养内容。

![新建工单](docs/screenshots/work-new.png)

### 5. 录入配件和工时

保存工单后会直接进入报价的编辑界面。每一行是一项配件或工时：

- **Code**：输入配件编号或名称，下拉列表会显示库存里匹配的配件。选中后自动带出名称和价格。
- **Name / Price / Quantity / Unit / Discount**：名称、单价、数量、单位、折扣（%）。工时可以直接手填，比如 Code 填 `LAB`，单位填 `h`。
- **Add row** 添加一行；右侧的 ✕ 删除一行；拖动左侧的 ≡ 调整顺序。
- 右侧下拉菜单里的 **Apply discount** 可以给所有行统一打折。

![编辑报价](docs/screenshots/offer-edit.png)

点 **Save** 保存。页面下方会显示小计、税额和总计，税率在设置里配置。

![报价](docs/screenshots/offer.png)

### 6. 出具报价并发给客户

点 **Issue offer**：

- **Show vehicle information on offer**：报价单上是否显示车辆信息。
- **Send client an email**：打开后会把报价单 PDF 直接发到下面填写的客户邮箱。

![出具报价](docs/screenshots/issue-offer.png)

点 **OK** 后报价编号生成，标题旁出现 **Issued** 标记；邮件图标表示已发送。点下载或打印图标可以拿到 PDF。以后需要重发，在右下角的下拉菜单里选 **Resend offer**。

![报价已出具](docs/screenshots/offer-issued.png)

### 7. 客户接受报价，开始维修

客户确认后点 **Client accepted**，可以填写要带到维修任务里的备注，点 **OK**。

![客户接受报价](docs/screenshots/offer-accepted.png)

系统会自动新建一个 **维修任务（Repair job）**，报价里的所有项目都会复制过去。维修中如果多换了零件或多花了工时，直接在维修任务里修改并保存。

打开 **Is in progress** 表示车辆正在维修中，工单列表可以按这个状态筛选。

![维修任务](docs/screenshots/repair-job.png)

一张工单可以有多份报价和多个维修任务。工单右上角的 ⋮ 菜单里有 **Make an offer**（再做一份报价）、**Start repair job**、**Edit**、**Create a copy**、**Delete**。

### 8. 完工开票

点 **Issue invoice**：

- **Due days**：付款期限（天）
- **Payment type**：付款方式（现金、银行转账、刷卡）
- **Send client an email**：把发票 PDF 发到客户邮箱

![开发票](docs/screenshots/issue-invoice.png)

开票后工单状态变为 **Completed**，发票编号旁显示 **Unpaid**（未付款）。

![发票](docs/screenshots/invoice.png)

### 9. 登记收款

客户付款后点 **Payment received**，状态变为 **Paid**。点错了可以点 **Unpaid** 撤回。**Send invoice** 用来重新发送发票；右侧下拉菜单里的 **Delete invoice** 可以作废发票，工单会回到可编辑状态。

![已收款](docs/screenshots/invoice-paid.png)

### 10. 查找工单

**Work** 页面：

- 默认只显示未完成的工单；打开 **Completed** 开关查看已开票的工单。
- 状态筛选：**All**、**In progress**、**Closed**，以及 **Invoice overdue**（已过期未付款）。
- 可以按工单号、客户、VIN、车牌、配件（**Product or service**）、日期范围搜索。
- 列表中的下载 / 打印图标直接获取报价单或发票的 PDF。

![工单列表](docs/screenshots/work-list.png)

不需要开票就结束的工单（比如客户不修了），在工单页点 **Close** 关闭；需要时可以再点 **Open** 重新打开。

### 11. 配件库存

**Inventory → Add new**：填写配件编号、名称、价格、数量，在 **Location** 里选择库位（点 **New** 新建库位）。录入工单时，**Code** 列会从这里自动补全。

![新建配件](docs/screenshots/inventory-new.png)

![库存列表](docs/screenshots/inventory.png)

### 12. 设置

**Settings → Invoice Options** 里是公司信息（名称、地址、银行账户、税号，会打印在 PDF 上）、增值税率、发票和报价单的邮件正文等。点右下角 **Edit** 修改。

![设置](docs/screenshots/settings.png)

页面底部的 **Email delivery** 用来检查邮件是否配置正确：填一个邮箱，点 **Send test email**，系统会提示通过 SMTP 还是 Microsoft Graph 发送成功。

![测试邮件](docs/screenshots/settings-test-email.png)

### 13. 个人资料

**Settings → My Account**（或点左下角的用户名）：修改姓名、邮箱、头像，修改密码，绑定或解绑 Microsoft 账号。

![个人资料](docs/screenshots/profile.png)

### 14. 员工和登录账号

当前版本**还没有员工管理界面**：

- 只负责维修、不需要登录的技师：在新建工单时，从 **Mechanics** 旁的 **New** 直接添加。
- 需要登录系统的员工：目前只能由管理员通过 API 创建，方法见 [开发者文档 → 创建登录账号](#创建登录账号)。

---

## 安装

支持 Linux（systemd + pm2 + nginx）、macOS 和 Windows，不需要 Docker。数据库可选 PostgreSQL 13+ 或 MySQL 8.0+。

### Debian 12 / Ubuntu 22.04+

```bash
git clone https://github.com/ecfranke/VG-Auto.git && cd VG-Auto
sudo deploy/prerequisites-debian.sh --db postgresql          # 或 --db mysql
sudo deploy/install.sh --app-url https://app.example.com --api-url https://api.example.com \
     --nginx --db-provider PostgreSql --admin-email you@example.com
# 第一次运行只生成 /etc/vg-auto 下的配置：创建数据库用户、填写邮件配置后，再运行一次
sudo deploy/create-database.sh /etc/vg-auto/appsettings.Secrets.json
sudo deploy/install.sh
sudo certbot --nginx -d app.example.com -d api.example.com
```

- 首次登录：用户名 `admin`，密码在 `/etc/vg-auto/initial-admin-password`。验证码会发到 `--admin-email` 指定的邮箱。
- 服务：API 是 systemd 服务 `vg-auto-api`，前端是 pm2 进程 `vg-auto-web`。

### 宝塔面板

数据库和反向代理在宝塔里配置，其余一条命令完成（不装 Nginx，不动数据库）：

```bash
sudo deploy/vgauto.sh baota --app-url https://app.example.com --api-url https://api.example.com \
     --db-provider MySql --db-host 127.0.0.1 --db-password '<宝塔里建库时的密码>' --admin-email you@example.com
```

完整步骤见 [部署指南 1.6 节](docs/deployment.zh-CN.md#16-使用宝塔面板部署ubuntu--debian)。

### 日常运维：`vgauto`

安装后可以在任何目录使用 `sudo vgauto`（macOS 不用 sudo）。不带参数运行会出现菜单。

| 命令 | 作用 |
|---|---|
| `sudo vgauto status` | 服务状态、健康检查、访问地址和版本 |
| `sudo vgauto start` / `stop` / `restart` | 启动 / 停止 / 重启 API 和前端 |
| `sudo vgauto logs api` / `logs web` | 实时日志 |
| `sudo vgauto upgrade` | 自动备份 → `git pull` → 重新安装 |
| `sudo vgauto backup [--keep 14]` | 数据库 + PDF + 配置打包备份 |
| `sudo vgauto restore <文件>` | 从备份恢复（需要确认，恢复前自动备份当前状态） |

### macOS

```bash
deploy/install.sh --app-url http://localhost:3000 --admin-email you@example.com
```

API 和前端都由 pm2 运行（`vg-auto-api`、`vg-auto-web`）。

### Windows

用管理员身份打开 PowerShell：

```powershell
deploy\windows\install.ps1 -AdminEmail you@example.com
```

API 注册为 Windows 服务 `VGAutoApi`，前端由 pm2 运行。

### 上线前必须完成的配置

1. **邮件**：登录验证码依赖邮件。在 `appsettings.Secrets.json` 的 `Email` 节配置 SMTP 或 Microsoft Graph，然后用设置页的 **Send test email** 验证。
2. **HTTPS**：生产环境必须启用 HTTPS（登录 Cookie 只在 HTTPS 下发送）。
3. **备份**：定期运行 `sudo vgauto backup --keep 14`（可以放进 crontab 或宝塔的计划任务），并把备份复制到别的机器。

SMTP 和 Microsoft Graph 的详细设置、Microsoft 登录的应用注册、安全配置和备份方法，见 **[部署与配置指南](docs/deployment.zh-CN.md)**。

---

## 开发者文档

### 架构

```
浏览器 ──► Next.js 前端（:3000）──服务端调用──► ASP.NET Core API（:15567）──► PostgreSQL / MySQL
             Server Actions                       │
             会话 Cookie（加密）                   ├─► SMTP / Microsoft Graph（邮件）
                                                  └─► Puppeteer / Chrome（PDF）
```

- 浏览器不直接持有高权限令牌。登录由 Next.js 服务端完成：它用 `SERVER_SECRET`（等于 API 的 `JwtOptions:ConsumerSecret`）调用 `/api/auth/*`，拿到两个 JWT：
  - `jwt`：服务端令牌，带 Root 角色，只保存在加密的会话 Cookie 中，用于 Server Actions 调用 API；
  - `publicJwt`：浏览器令牌，只能访问少数接口（如头像、延长会话）。
- API 默认所有接口都需要认证；业务接口额外要求 `ServerSidePolicy`（Root 角色）。
- 数据访问：写操作用 NHibernate（领域模型），查询和列表用 Dapper。`SqlDialect` 层负责 PostgreSQL 和 MySQL 的差异（表名映射、分页、JSON 等）。

### 目录结构

| 路径 | 说明 |
|---|---|
| `backend/src/VgAuto.Http.Api` | API：控制器、PDF 的 Razor 模板、启动配置 |
| `backend/src/VgAuto.Core.Application` | 业务服务：登录（`AuthService`）、邮件（`IEmailSender`）、查询、配置 |
| `backend/src/VgAuto.Core.Persistence` | 数据访问：NHibernate 映射、`IDbConnectionFactory`、`SqlDialect` |
| `backend/src/VgAuto.Domain` | 领域模型：工单、报价、维修任务、发票、客户、车辆、配件 |
| `backend/src/VgAuto.Http.Api.Model` | DTO 和映射 |
| `backend/src/DbUp` | 数据库迁移：`scripts`（PostgreSQL）、`scripts_mysql`（MySQL） |
| `backend/tests/VgAuto.Tests` | xUnit 单元测试和集成测试 |
| `frontend/src/app/auth` | 登录、验证码、找回密码、Microsoft 登录 |
| `frontend/src/app/home` | 业务页面：`work`、`clients`、`vehicles`、`inventory`、`settings`、`profile` |
| `deploy/` | 安装脚本，systemd / nginx 模板，`windows/install.ps1` |
| `scripts/` | `setup-secrets.sh` / `.ps1`：生成开发用密钥 |

### 本地运行

需要 .NET 9 SDK、Node.js 20+、PostgreSQL 或 MySQL 8，以及 Chrome/Chromium（生成 PDF 用）。

```bash
scripts/setup-secrets.sh             # 生成 appsettings.Secrets.json 和 frontend/.env，并打印数据库密码
# 用打印出的密码创建数据库用户（或者改 appsettings.Secrets.json 里的 DbOptions）
cd backend/src/DbUp && dotnet run                  # 建表，并打印一次初始管理员密码
cd ../VgAuto.Http.Api && dotnet run                # API：http://localhost:15567
cd ../../../frontend && npm ci && npm run dev      # 前端：http://localhost:3000
```

开发环境没有邮件服务器时，可以把 `Email:Smtp` 指向本地的 SMTP 测试工具（如 MailHog、smtp4dev），在那里查看验证码。

### 配置参考

API 配置在 `appsettings.json`，敏感信息放在 `appsettings.Secrets.json`（安装后位于 `/etc/vg-auto/`）。也可以用环境变量覆盖，例如 `DbOptions__Provider=MySql`。

| 配置项 | 说明 |
|---|---|
| `JwtOptions:Secret` | JWT 签名密钥（至少 64 字节随机值） |
| `JwtOptions:ConsumerSecret` | 前端服务端的密钥，必须和前端的 `SERVER_SECRET` 一致 |
| `JwtOptions:SessionTimeout` | 会话时长，默认 `08:00:00` |
| `DbOptions:Provider` | `PostgreSql` 或 `MySql` |
| `DbOptions:Host` / `Port` / `UserId` / `Password` / `Name` | 数据库连接 |
| `DbOptions:MultiTenancy:Enabled` | 多租户，仅 PostgreSQL 支持 |
| `DefaultAdmin:UserName` / `Email` / `Password` | 初始管理员，只在首次迁移时使用；密码留空则随机生成 |
| `Email:Provider` | `Smtp` 或 `Graph` |
| `Email:FromAddress` / `FromName` | 发件人 |
| `Email:Smtp:Host` / `Port` / `User` / `Password` / `Security` | SMTP；`Security` 为 `Auto`、`SslOnConnect`、`StartTls` 或 `None` |
| `Email:Graph:TenantId` / `ClientId` / `ClientSecret` / `Sender` | Microsoft Graph（应用权限 `Mail.Send`） |
| `Authentication:EmailCode` | 登录验证码：`RequireForPasswordLogin`、`CodeLifetimeMinutes`、`MaxAttempts`、`MaxSends`、`AllowUsersWithoutEmail` |
| `Authentication:PasswordReset:Enabled` | 是否允许找回密码 |
| `Authentication:Microsoft` | Microsoft 登录：`Enabled`、`ClientId`、`ClientSecret`、`TenantId` |
| `Cors:AllowedOrigins` | 允许跨域访问 API 的前端地址 |
| `Swagger:Enabled` | 是否开启 `/swagger`（生产环境建议关闭） |
| `Errors:IncludeDetails` | 错误响应中是否包含详细信息（仅用于调试） |
| `ForwardedHeaders:KnownProxies` | 反向代理的 IP，用于获取真实客户端 IP |
| `PdfDirectory` | PDF 保存目录 |
| `PuppeteerExecutablePath` | Chrome/Chromium 的路径 |

前端配置在 `frontend/.env`（安装后位于 `/etc/vg-auto/web.env`）：

| 变量 | 说明 |
|---|---|
| `SERVER_SECRET` | 等于 API 的 `JwtOptions:ConsumerSecret` |
| `SESSION_SECRET` | 会话 Cookie 的加密密钥（32 字节随机值） |
| `API_URL` | 前端服务端访问 API 的地址（通常是内网地址） |
| `NEXT_PUBLIC_API_URL` | 浏览器访问 API 的地址 |
| `APP_URL` | 前端的对外地址（Microsoft 登录回调使用） |
| `COOKIE_SECURE` | 是否只在 HTTPS 下发送 Cookie；生产环境为 `true` |

### API 接口

开发模式下访问 `http://localhost:15567/swagger` 查看完整接口文档。主要分组：

| 路径 | 说明 |
|---|---|
| `POST /api/auth/login`、`verify`、`resend` | 密码登录 → 邮件验证码 → 获取令牌 |
| `POST /api/auth/password/forgot`、`password/reset` | 找回密码 |
| `GET /api/auth/providers`、`POST /api/auth/microsoft` | 登录方式、Microsoft 登录 |
| `/api/work/*`、`/api/pricings/*` | 工单、报价、维修任务、发票、PDF、发送邮件 |
| `/api/clients`、`/api/privateclients`、`/api/legalclients` | 客户 |
| `/api/vehicles`、`/api/spareparts`、`/api/storages` | 车辆、配件、库位 |
| `/api/employees` | 员工和登录账号 |
| `/api/options`（含 `testemail`） | 公司设置、测试邮件 |
| `/api/profile`（含 `changepassword`、`externallogins`） | 个人资料、改密码、Microsoft 账号绑定 |
| `/api/query` | 列表查询 |
| `GET /health` | 健康检查（不需要认证） |

`/api/auth/*` 的请求都必须带 `serverSecret`，只能由前端服务端或管理员调用。

### 创建登录账号

在界面提供之前，可以用下面的方式创建能登录的员工（在服务器上执行，`SECRET` 是 `JwtOptions:ConsumerSecret`）：

```bash
API=http://localhost:15567
SECRET=...   # /etc/vg-auto/appsettings.Secrets.json 中的 JwtOptions:ConsumerSecret

# 1. 用管理员账号登录，返回 challengeId，验证码会发到管理员邮箱
curl -s -X POST $API/api/auth/login -H 'Content-Type: application/json' \
  -d '{"userName":"admin","password":"<管理员密码>","serverSecret":"'$SECRET'"}'

# 2. 提交验证码，返回的 jwt 就是令牌
curl -s -X POST $API/api/auth/verify -H 'Content-Type: application/json' \
  -d '{"challengeId":"<challengeId>","code":"<验证码>","serverSecret":"'$SECRET'"}'

# 3. 创建员工；带 userName 和 password 时会同时创建登录账号
curl -s -X POST $API/api/employees -H "Authorization: Bearer <jwt>" -H 'Content-Type: application/json' \
  -d '{"firstName":"Tom","lastName":"Berg","email":"tom@example.com","phone":"","proffession":"Mechanic","description":"","userName":"tom","password":"<初始密码>"}'
```

新员工登录时，验证码会发到上面填写的 `email`，所以邮箱必须真实有效。请提醒员工登录后在个人资料里修改密码。

### 数据库迁移

迁移由 DbUp 执行，每次启动 `DbUp` 项目（安装脚本会自动运行）时按文件名顺序执行尚未执行过的脚本。新增迁移时，**两种数据库都要写**：

```
backend/src/DbUp/scripts/Script0006_描述.sql         # PostgreSQL
backend/src/DbUp/scripts_mysql/Script0006_描述.sql   # MySQL
```

- 编号必须递增，已经发布的脚本不要修改。
- PostgreSQL 使用 `domain.`、`tenant_config.` 等 schema；MySQL 没有 schema，对应表名分别是去掉前缀的表名和 `tenant_config_*`，`public.user` 对应 `app_user`。
- 在代码中写 SQL 时，通过 `SqlDialect` 处理表名和语法差异，不要直接写某一种数据库特有的语法。

### 测试

```bash
cd backend/tests/VgAuto.Tests
dotnet test                                                    # 单元测试
VGAUTO_TEST_DB_HOST=localhost VGAUTO_TEST_DB_USER=... VGAUTO_TEST_DB_PASSWORD=... dotnet test   # 加上 PostgreSQL 集成测试
VGAUTO_TEST_DB_PROVIDER=MySql VGAUTO_TEST_DB_HOST=localhost ... dotnet test                    # 针对 MySQL
cd ../../../frontend && npm run build                          # 前端构建（包含 lint 和类型检查）
```

集成测试会创建临时数据库，运行完整的迁移，并通过 `WebApplicationFactory` 测试登录流程和主要接口。

### CI

`.github/workflows/ci.yml` 在每次推送和 Pull Request 时运行：

- **Backend**：分别在 PostgreSQL 和 MySQL 上编译并运行全部测试，检查有漏洞的 NuGet 包；
- **Frontend**：`npm ci`、构建，并检查生产依赖中的严重漏洞。

### 已知限制

- 还没有员工管理界面（见上文）。
- 多租户只支持 PostgreSQL。
- MySQL 需要 8.0 及以上版本，不支持 MariaDB。
- 前端构建时需要能访问 Google Fonts。
- 界面目前只有英文。

---

## 许可证

Copyright © 2026 V. G. Global Solution Canada Inc. 保留所有权利。

本软件为专有软件，未经书面许可，不得复制、修改、分发或以其他方式使用，详见 [LICENSE](LICENSE)。项目使用的第三方开源组件及其许可证见 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)。
