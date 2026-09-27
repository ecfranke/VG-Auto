<p align="center"><img src="frontend/public/logo.svg" width="72" alt="VG Auto"></p>

<h1 align="center">VG Auto</h1>

<p align="center">English | <a href="README.md">中文</a></p>

VG Auto is a self-hosted management system for car repair shops. Everything from the first estimate through client approval, the repair, the invoice and the payment happens in the browser. Estimates and invoices are generated as PDF and emailed to the client.

This document has three parts:

1. [User guide](#user-guide): for front desk staff and mechanics, following the daily workflow step by step
2. [Installation](#installation): for whoever deploys the system
3. [Developer documentation](#developer-documentation): architecture, configuration, API, database migrations and tests

---

## User guide

### The life of a work order

```
Create client → Register vehicle → Create work (estimate) → Add parts and labour → Issue the estimate and email it
        → Client accepts, a repair job is created → Work is done, issue the invoice → Mark as paid
```

The main menu on the left has **Work**, **Clients**, **Vehicles**, **Inventory** and **Settings**. The current user is shown at the bottom left; click it to open your profile or sign out.

### 1. Sign in

Enter your username and password and click **Sign in**.

![Sign in](docs/screenshots/login.png)

A 6 digit code is sent to your email address. Enter it and click **Verify**. If it does not arrive, click **Send a new code**. Codes are valid for 10 minutes.

![Enter the code](docs/screenshots/login-code.png)

- Forgot your password: click **Forgot password?** on the sign in page, enter your username or email address, and set a new password with the code you receive.
- If the administrator enabled Microsoft sign in, the page shows a **Sign in with Microsoft** button. The first time, you confirm it once with an email code; after that the Microsoft account signs you in directly.
- Too many wrong passwords lock the account for a while. Try again later.
- The initial administrator must change the password at the first sign in.

![Reset password](docs/screenshots/forgot-password.png)

### 2. Create a client

**Clients → Add new**. For a private person enter the name, phone and email address; for a business turn on the switch at the top and enter the company name and registration details. The email address matters: estimates and invoices are sent to it. Click **Save**.

![New client](docs/screenshots/client-new.png)

The client list can be searched by name, phone or email. Click a client to see details, vehicles and past work.

![Clients](docs/screenshots/clients.png)

### 3. Register a vehicle

**Vehicles → Add new**. Enter make, model, VIN, registration number and odometer, then type the client's name in **Owner** and pick the owner from the list.

![New vehicle](docs/screenshots/vehicle-new.png)

When a vehicle changes hands, just change the owner. The ownership history is kept.

### 4. Create work

**Work → Add new**:

- **Start with**: turn on **Offer** to start with an estimate for the client; turn it off to start a repair job right away (small jobs that need no estimate).
- **Client**: type to search and select. For walk-in clients who do not want to be registered, turn on **Undisclosed**.
- **Vehicle**: lists the client's vehicles; turn on **Search all vehicles** to search every vehicle. The field on the right is the odometer reading for this visit.
- **Mechanics**: select a mechanic and click **Add**; click **New** to add a mechanic who is not in the list yet.
- **About**: the problem the client described, or the planned service.

![New work](docs/screenshots/work-new.png)

### 5. Add parts and labour

After saving, the estimate opens in edit mode. Each row is a part or a labour item:

- **Code**: type a part code or name; matching parts from the inventory are listed. Picking one fills in name and price.
- **Name / Price / Quantity / Unit / Discount**: discount is in %. Labour can be entered by hand, for example code `LAB` with unit `h`.
- **Add row** adds a row, ✕ removes one, and dragging ≡ on the left reorders rows.
- **Apply discount** in the drop-down on the right applies a discount to all rows.

![Edit the estimate](docs/screenshots/offer-edit.png)

Click **Save**. Subtotal, tax and total are shown below the rows; the tax rate is set in the settings.

![Estimate](docs/screenshots/offer.png)

### 6. Issue the estimate and send it to the client

Click **Issue offer**:

- **Show vehicle information on offer**: print the vehicle details on the estimate.
- **Send client an email**: email the estimate PDF to the address below.

![Issue the estimate](docs/screenshots/issue-offer.png)

After **OK** the estimate gets a number and an **Issued** badge; the envelope icon means it was emailed. The download and print icons give you the PDF. To send it again, choose **Resend offer** in the drop-down at the bottom right.

![Estimate issued](docs/screenshots/offer-issued.png)

### 7. The client accepts, the repair starts

When the client agrees, click **Client accepted**, optionally add notes for the repair job, and click **OK**.

![Client accepted](docs/screenshots/offer-accepted.png)

A **Repair job** is created with every item from the estimate. If more parts or time are needed during the repair, edit the repair job and save.

Turn on **Is in progress** while the car is being worked on; the work list can filter on it.

![Repair job](docs/screenshots/repair-job.png)

One work order can hold several estimates and repair jobs. The ⋮ menu at the top right has **Make an offer**, **Start repair job**, **Edit**, **Create a copy** and **Delete**.

### 8. Finish the work and issue the invoice

Click **Issue invoice**:

- **Due days**: payment term in days
- **Payment type**: cash, bank transfer or card payment
- **Send client an email**: email the invoice PDF to the client

![Issue the invoice](docs/screenshots/issue-invoice.png)

The work becomes **Completed** and the invoice shows **Unpaid**.

![Invoice](docs/screenshots/invoice.png)

### 9. Record the payment

When the client pays, click **Payment received**; the invoice becomes **Paid**. **Unpaid** reverts it. **Send invoice** sends it again, and **Delete invoice** in the drop-down cancels the invoice so the work can be edited again.

![Paid](docs/screenshots/invoice-paid.png)

### 10. Find work

On the **Work** page:

- Unfinished work is shown by default; turn on **Completed** to see invoiced work.
- Status filters: **All**, **In progress**, **Closed** and **Invoice overdue**.
- Search by work number, client, VIN, registration number, part (**Product or service**) and date range.
- The download and print icons in the list give the estimate or invoice PDF directly.

![Work list](docs/screenshots/work-list.png)

Work that ends without an invoice (for example the client decided against the repair) is closed with **Close** on the work page, and can be reopened with **Open**.

### 11. Inventory

**Inventory → Add new**: enter the part code, name, price and quantity, and choose a storage **Location** (**New** creates one). The **Code** column in estimates and repair jobs autocompletes from here.

![New part](docs/screenshots/inventory-new.png)

![Inventory](docs/screenshots/inventory.png)

### 12. Settings

**Settings → Invoice Options** holds the company details printed on the PDFs (name, address, bank account, tax ID), the VAT rate, and the email texts for invoices and estimates. Click **Edit** to change them.

![Settings](docs/screenshots/settings.png)

**Email delivery** at the bottom checks the email setup: enter an address and click **Send test email**. The message tells you whether it went out through SMTP or Microsoft Graph.

![Test email](docs/screenshots/settings-test-email.png)

### 13. Profile

**Settings → My Account** (or click your name at the bottom left): change name, email and avatar, change your password, and link or unlink a Microsoft account.

![Profile](docs/screenshots/profile.png)

### 14. Employees and user accounts

This version has **no employee management screen** yet:

- Mechanics who do not sign in: add them with **New** next to **Mechanics** when creating work.
- Employees who need to sign in: an administrator creates them through the API, see [Developer documentation → Creating user accounts](#creating-user-accounts).

---

## Installation

Runs on Linux (systemd + pm2 + nginx), macOS and Windows without Docker. The database is PostgreSQL 13+ or MySQL 8.0+.

### Debian 12 / Ubuntu 22.04+

```bash
git clone https://github.com/ecfranke/VG-Auto.git && cd VG-Auto
sudo deploy/prerequisites-debian.sh --db postgresql          # or --db mysql
sudo deploy/install.sh --app-url https://app.example.com --api-url https://api.example.com \
     --nginx --db-provider PostgreSql --admin-email you@example.com
# the first run only writes the configuration to /etc/vg-auto: create the database user, configure Email, then run again
sudo deploy/create-database.sh /etc/vg-auto/appsettings.Secrets.json
sudo deploy/install.sh
sudo certbot --nginx -d app.example.com -d api.example.com
```

- First sign in: user `admin`, password in `/etc/vg-auto/initial-admin-password`. The code goes to the `--admin-email` address.
- Upgrade: `git pull && sudo deploy/install.sh`
- Services: the API is the systemd service `vg-auto-api`, the web app is the pm2 process `vg-auto-web`.

### macOS

```bash
deploy/install.sh --app-url http://localhost:3000 --admin-email you@example.com
```

Both the API and the web app run under pm2 (`vg-auto-api`, `vg-auto-web`).

### Windows

In an elevated PowerShell:

```powershell
deploy\windows\install.ps1 -AdminEmail you@example.com
```

The API is installed as the Windows service `VGAutoApi`; the web app runs under pm2.

### Required before going live

1. **Email**: sign in codes depend on it. Configure SMTP or Microsoft Graph in the `Email` section of `appsettings.Secrets.json` and check it with **Send test email**.
2. **HTTPS**: required in production (the session cookie is only sent over HTTPS).
3. **Backups**: back up the database and the PDF directory (`PdfDirectory`) regularly.

SMTP and Microsoft Graph details, the app registration for Microsoft sign in, security settings and backups are covered in the **[deployment guide](docs/deployment.zh-CN.md)** (Chinese).

---

## Developer documentation

### Architecture

```
Browser ──► Next.js web app (:3000) ──server side──► ASP.NET Core API (:15567) ──► PostgreSQL / MySQL
             Server Actions                             │
             encrypted session cookie                   ├─► SMTP / Microsoft Graph (email)
                                                        └─► Puppeteer / Chrome (PDF)
```

- The browser never holds the privileged token. Sign in runs on the Next.js server, which calls `/api/auth/*` with `SERVER_SECRET` (equal to the API's `JwtOptions:ConsumerSecret`) and receives two JWTs:
  - `jwt`: the server token with the Root role, kept only in the encrypted session cookie and used by Server Actions;
  - `publicJwt`: the browser token, limited to a few endpoints (profile picture, session extension).
- Every API endpoint requires authentication by default; business endpoints also require `ServerSidePolicy` (Root role).
- Data access: writes go through NHibernate (domain model), queries and lists through Dapper. The `SqlDialect` layer handles the differences between PostgreSQL and MySQL (table names, paging, JSON).

### Project layout

| Path | Contents |
|---|---|
| `backend/src/VgAuto.Http.Api` | API: controllers, Razor templates for PDFs, startup |
| `backend/src/VgAuto.Core.Application` | Services: sign in (`AuthService`), email (`IEmailSender`), queries, options |
| `backend/src/VgAuto.Core.Persistence` | Data access: NHibernate mappings, `IDbConnectionFactory`, `SqlDialect` |
| `backend/src/VgAuto.Domain` | Domain model: work, estimates, repair jobs, invoices, clients, vehicles, parts |
| `backend/src/VgAuto.Http.Api.Model` | DTOs and mapping |
| `backend/src/DbUp` | Migrations: `scripts` (PostgreSQL), `scripts_mysql` (MySQL) |
| `backend/tests/VgAuto.Tests` | xUnit unit and integration tests |
| `frontend/src/app/auth` | Sign in, codes, password reset, Microsoft sign in |
| `frontend/src/app/home` | App pages: `work`, `clients`, `vehicles`, `inventory`, `settings`, `profile` |
| `deploy/` | Install scripts, systemd / nginx templates, `windows/install.ps1` |
| `scripts/` | `setup-secrets.sh` / `.ps1`: development secrets |

### Running locally

Requires the .NET 9 SDK, Node.js 20+, PostgreSQL or MySQL 8, and Chrome/Chromium for PDFs.

```bash
scripts/setup-secrets.sh             # writes appsettings.Secrets.json and frontend/.env, prints the database password
# create the database user with that password (or edit DbOptions in appsettings.Secrets.json)
cd backend/src/DbUp && dotnet run                  # creates the schema, prints the initial admin password once
cd ../VgAuto.Http.Api && dotnet run                # API: http://localhost:15567
cd ../../../frontend && npm ci && npm run dev      # web app: http://localhost:3000
```

Without a mail server, point `Email:Smtp` at a local SMTP test tool (MailHog, smtp4dev) and read the codes there.

### Configuration reference

The API reads `appsettings.json`; secrets go in `appsettings.Secrets.json` (in `/etc/vg-auto/` after installation). Environment variables override both, for example `DbOptions__Provider=MySql`.

| Key | Meaning |
|---|---|
| `JwtOptions:Secret` | JWT signing key (at least 64 random bytes) |
| `JwtOptions:ConsumerSecret` | Secret of the web server; must equal `SERVER_SECRET` of the web app |
| `JwtOptions:SessionTimeout` | Session length, default `08:00:00` |
| `DbOptions:Provider` | `PostgreSql` or `MySql` |
| `DbOptions:Host` / `Port` / `UserId` / `Password` / `Name` | Database connection |
| `DbOptions:MultiTenancy:Enabled` | Multi-tenancy, PostgreSQL only |
| `DefaultAdmin:UserName` / `Email` / `Password` | Initial administrator, used by the first migration only; an empty password is generated randomly |
| `Email:Provider` | `Smtp` or `Graph` |
| `Email:FromAddress` / `FromName` | Sender |
| `Email:Smtp:Host` / `Port` / `User` / `Password` / `Security` | SMTP; `Security` is `Auto`, `SslOnConnect`, `StartTls` or `None` |
| `Email:Graph:TenantId` / `ClientId` / `ClientSecret` / `Sender` | Microsoft Graph (application permission `Mail.Send`) |
| `Authentication:EmailCode` | Sign in codes: `RequireForPasswordLogin`, `CodeLifetimeMinutes`, `MaxAttempts`, `MaxSends`, `AllowUsersWithoutEmail` |
| `Authentication:PasswordReset:Enabled` | Allow password reset |
| `Authentication:Microsoft` | Microsoft sign in: `Enabled`, `ClientId`, `ClientSecret`, `TenantId` |
| `Cors:AllowedOrigins` | Web app origins allowed to call the API |
| `Swagger:Enabled` | Serve `/swagger` (keep it off in production) |
| `Errors:IncludeDetails` | Include error details in responses (debugging only) |
| `ForwardedHeaders:KnownProxies` | Reverse proxy IPs, used to get the real client IP |
| `PdfDirectory` | Where PDFs are stored |
| `PuppeteerExecutablePath` | Path to Chrome/Chromium |

The web app reads `frontend/.env` (`/etc/vg-auto/web.env` after installation):

| Variable | Meaning |
|---|---|
| `SERVER_SECRET` | Equals the API's `JwtOptions:ConsumerSecret` |
| `SESSION_SECRET` | Encryption key for the session cookie (32 random bytes) |
| `API_URL` | API address used by the web server (usually internal) |
| `NEXT_PUBLIC_API_URL` | API address used by the browser |
| `APP_URL` | Public address of the web app (used for the Microsoft sign in callback) |
| `COOKIE_SECURE` | Send cookies over HTTPS only; `true` in production |

### API

In development the full API is documented at `http://localhost:15567/swagger`. Main groups:

| Path | Purpose |
|---|---|
| `POST /api/auth/login`, `verify`, `resend` | Password → email code → tokens |
| `POST /api/auth/password/forgot`, `password/reset` | Password reset |
| `GET /api/auth/providers`, `POST /api/auth/microsoft` | Sign in options, Microsoft sign in |
| `/api/work/*`, `/api/pricings/*` | Work, estimates, repair jobs, invoices, PDFs, email |
| `/api/clients`, `/api/privateclients`, `/api/legalclients` | Clients |
| `/api/vehicles`, `/api/spareparts`, `/api/storages` | Vehicles, parts, storage locations |
| `/api/employees` | Employees and user accounts |
| `/api/options` (with `testemail`) | Company settings, test email |
| `/api/profile` (with `changepassword`, `externallogins`) | Profile, password, Microsoft account link |
| `/api/query` | List queries |
| `GET /health` | Health check (no authentication) |

Every `/api/auth/*` request must carry `serverSecret`, so only the web server or an administrator can call them.

### Creating user accounts

Until there is a screen for it, create employees who can sign in like this (on the server; `SECRET` is `JwtOptions:ConsumerSecret`):

```bash
API=http://localhost:15567
SECRET=...   # JwtOptions:ConsumerSecret from /etc/vg-auto/appsettings.Secrets.json

# 1. Sign in as administrator; returns a challengeId and emails a code to the administrator
curl -s -X POST $API/api/auth/login -H 'Content-Type: application/json' \
  -d '{"userName":"admin","password":"<admin password>","serverSecret":"'$SECRET'"}'

# 2. Submit the code; the returned jwt is the token
curl -s -X POST $API/api/auth/verify -H 'Content-Type: application/json' \
  -d '{"challengeId":"<challengeId>","code":"<code>","serverSecret":"'$SECRET'"}'

# 3. Create the employee; userName and password also create a user account
curl -s -X POST $API/api/employees -H "Authorization: Bearer <jwt>" -H 'Content-Type: application/json' \
  -d '{"firstName":"Tom","lastName":"Berg","email":"tom@example.com","phone":"","proffession":"Mechanic","description":"","userName":"tom","password":"<initial password>"}'
```

The new employee's sign in codes go to that `email`, so it must be a real address. Ask the employee to change the password in the profile after the first sign in.

### Database migrations

DbUp runs every script that has not run yet, in file name order, each time the `DbUp` project starts (the installer runs it automatically). A new migration **needs both databases**:

```
backend/src/DbUp/scripts/Script0006_description.sql         # PostgreSQL
backend/src/DbUp/scripts_mysql/Script0006_description.sql   # MySQL
```

- Numbers must increase; never change a script that has been released.
- PostgreSQL uses the `domain.` and `tenant_config.` schemas; MySQL has no schemas, so the tables are named without the prefix and `tenant_config_*`, and `public.user` becomes `app_user`.
- SQL in code goes through `SqlDialect` for table names and syntax differences; do not write syntax specific to one database.

### Tests

```bash
cd backend/tests/VgAuto.Tests
dotnet test                                                    # unit tests
VGAUTO_TEST_DB_HOST=localhost VGAUTO_TEST_DB_USER=... VGAUTO_TEST_DB_PASSWORD=... dotnet test   # plus PostgreSQL integration tests
VGAUTO_TEST_DB_PROVIDER=MySql VGAUTO_TEST_DB_HOST=localhost ... dotnet test                    # against MySQL
cd ../../../frontend && npm run build                          # web app build (includes lint and type check)
```

The integration tests create a temporary database, run all migrations, and exercise the sign in flow and the main endpoints through `WebApplicationFactory`.

### CI

`.github/workflows/ci.yml` runs on every push and pull request:

- **Backend**: build and run all tests on PostgreSQL and on MySQL, check for vulnerable NuGet packages;
- **Frontend**: `npm ci`, build, and check production dependencies for critical vulnerabilities.

### Known limitations

- No employee management screen yet (see above).
- Multi-tenancy is PostgreSQL only.
- MySQL 8.0 or newer; MariaDB is not supported.
- The web app build needs access to Google Fonts.
- The user interface is in English only.

---

## License

VG Auto is released under the [GNU AGPL v3](LICENSE). It is based on [CarCare](https://github.com/rene98c/carcareco) by rene98c; see [NOTICE](NOTICE). If you offer a modified version to users over a network, the AGPL requires you to offer them its source code as well (the "Source code" link on the home page).
