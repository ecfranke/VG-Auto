# VG Auto

[English](README.en.md) | 中文

**VG Auto** 是一套自托管的汽修厂管理系统，覆盖工单、报价、维修、开票、客户、车辆和配件库存，全部在一个网页界面里完成。

![工单列表](docs/screenshots/work-list.png)

## 功能

- **工单**：一张工单可以包含多份报价和维修任务，报价被客户接受后一键转成维修任务
- **报价单和发票**：生成 PDF，可直接用邮件发给客户；可以标记已付款，并查看逾期未付的发票
- **客户和车辆**：个人客户、企业客户，车辆的归属变更记录，以及完整的维修历史
- **配件库存**：库位、价格、折扣，录入工单时自动补全
- **邮件发送**：SMTP 或 Microsoft 365（Microsoft Graph）二选一
- **登录**：密码加邮件验证码、找回密码、Microsoft 账号登录；账号连续输错会被锁定，初始管理员首次登录必须改密码
- **数据库**：PostgreSQL 或 MySQL 8，改配置即可切换
- **部署**：不需要 Docker；Linux 上用 systemd、pm2 和 nginx，另支持 macOS 和 Windows

| 工单详情 | 登录 |
|---|---|
| ![工单详情](docs/screenshots/work-details.png) | ![登录](docs/screenshots/login.png) |

## 快速安装（Debian 12 / Ubuntu 22.04+）

```bash
git clone https://github.com/ecfranke/VG-Auto.git && cd VG-Auto
sudo deploy/prerequisites-debian.sh --db postgresql          # 或 --db mysql
sudo deploy/install.sh --app-url https://app.example.com --api-url https://api.example.com \
     --nginx --db-provider PostgreSql --admin-email you@example.com
# 第一次运行会生成 /etc/vg-auto 下的配置：先创建数据库用户、填写邮件配置，再运行一次
sudo deploy/create-database.sh /etc/vg-auto/appsettings.Secrets.json
sudo deploy/install.sh
sudo certbot --nginx -d app.example.com -d api.example.com
```

首次登录用户名是 `admin`，密码在 `/etc/vg-auto/initial-admin-password` 里。登录时验证码会发到管理员邮箱，登录后必须修改密码。

以后升级只需要：`git pull && sudo deploy/install.sh`

- **macOS**：`deploy/install.sh --app-url http://localhost:3000 --admin-email you@example.com`（API 和前端都由 pm2 运行）
- **Windows**（用管理员身份打开 PowerShell）：`deploy\windows\install.ps1 -AdminEmail you@example.com`

完整说明见 **[部署与配置指南](docs/deployment.zh-CN.md)**，包括 SMTP 和 Microsoft Graph 的设置、Microsoft 登录的应用注册、数据库、安全配置和备份。

## 技术栈

| 部分 | 技术 |
|---|---|
| 前端 | Next.js 15、React 19、Tailwind CSS |
| API | ASP.NET Core (.NET 9)、NHibernate + Dapper |
| 数据库 | PostgreSQL 13+ 或 MySQL 8.0+（DbUp 迁移） |
| 邮件 | MailKit (SMTP) 或 Microsoft Graph |
| PDF | Razor 模板 + Puppeteer (Chrome) |

## 目录结构

```
backend/src/VgAuto.Http.Api          API（控制器、PDF 模板）
backend/src/VgAuto.Core.Application  业务服务：登录、邮件、查询、配置
backend/src/VgAuto.Core.Persistence  数据访问（NHibernate 映射、PostgreSQL/MySQL）
backend/src/VgAuto.Domain            领域模型（工单、报价、发票、客户、车辆……）
backend/src/DbUp                     数据库迁移（scripts = PostgreSQL，scripts_mysql = MySQL）
backend/tests/VgAuto.Tests           单元测试和集成测试
frontend/                            Next.js 前端
deploy/                              安装脚本、systemd 和 nginx 模板
```

## 开发

```bash
scripts/setup-secrets.sh                           # 生成开发用的密钥
cd backend/src/DbUp && dotnet run                  # 建表，并打印一次初始管理员密码
cd ../VgAuto.Http.Api && dotnet run                # API 跑在 :15567，开发模式下 /swagger 可以看接口文档
cd ../../../frontend && npm ci && npm run dev      # 前端跑在 :3000
```

测试命令：`cd backend/tests/VgAuto.Tests && dotnet test`。设置 `VGAUTO_TEST_DB_HOST` 后会同时跑数据库集成测试；加上 `VGAUTO_TEST_DB_PROVIDER=MySql` 则针对 MySQL。每次推送时，GitHub Actions 会在 PostgreSQL 和 MySQL 上各跑一遍全部测试。

## 许可证

VG Auto 以 [GNU AGPL v3](LICENSE) 发布，基于 rene98c 的开源项目 [CarCare](https://github.com/rene98c/carcareco)，详见 [NOTICE](NOTICE)。
按照 AGPL 的要求，如果你把修改后的版本通过网络提供给别人使用，也需要向这些用户公开源代码。首页的“Source code”链接就是为此准备的。
