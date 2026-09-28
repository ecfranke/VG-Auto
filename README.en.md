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

The main menu on the left has **Home**, **Work**, **Clients**, **Vehicles**, **Inventory** and **Settings**. After signing in you land on **Home**: buttons for new work, client and vehicle, and the 10 most recently updated jobs (finished ones included). Work numbers look like `RP_TF_2019_HC_2026_09_28_15`: type (RP repair / OF offer only) _ client initials _ vehicle year _ manufacturer and model initials _ start date _ work number, X for missing parts; the code follows the current data (an accepted offer becomes RP) and pasting it into the search finds the work. The **Work** list shows work number, type (repair job / offer), status, client, vehicle, mechanics, start date and note. The current user is shown at the bottom left; click it to open your profile or sign out.

### 1. Sign in

Enter your username and password and click **Sign in**.

![Sign in](docs/screenshots/login.png)

A 6 digit code is sent to your email address. Enter it and click **Verify**. If it does not arrive, click **Send a new code**. Codes are valid for 10 minutes. After a code was entered, the same browser signs in with username and password only for **7 days**; another browser, cleared cookies or a changed password need a new code (`Authentication:EmailCode:RememberDeviceDays`, 0 asks every time).

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

**Vehicles → Add new**. Enter manufacturer, model, year (optional; shown as "2019 Honda Civic" and printed on estimates and invoices), VIN, license plate and odometer, then type the client's name in **Owner** and pick the owner from the list.

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
- **Name / Price / Quantity / Unit / Discount**: the price is **before tax**; discount is in %. Labour can be entered by hand, for example code `LAB` with unit `h`.
- **Add row** adds a row, ✕ removes one, and dragging ≡ on the left reorders rows.
- **Apply discount** in the drop-down on the right applies a discount to all rows.

![Edit the estimate](docs/screenshots/offer-edit.png)

Click **Save**. The subtotal before tax, each tax (for example GST and PST on separate lines) and the total are shown below the rows; the taxes are set in the settings (section 12).

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

**Inventory → Add new**: enter the part code, name, price before tax and quantity, and choose a storage **Location** (**New** creates one). The **Code** column in estimates and repair jobs autocompletes from here.

![New part](docs/screenshots/inventory-new.png)

![Inventory](docs/screenshots/inventory.png)

### 12. Settings

**Settings → Invoice Options** holds the company details printed on the PDFs (name, address, bank account, Reg No, Tax ID), the taxes, and the email texts for invoices and estimates. Click **Edit** to change them:

- Everybody can change phone, address, email, bank account, the invoice options and the offer options.
- **Company name, Reg No, Tax ID and currency** can only be changed by an administrator on the **Companies** page of the administration (section 14); in the app they are shown greyed out.
- These are the settings of **your own company**; with several companies, each has its own settings.

**Taxes**: all prices are entered **before tax**; the taxes are added to the subtotal, and each tax is a separate line on estimates and invoices.
- The **place of registration** (country + province/state) is set by an administrator. Choosing the province fills in its taxes: GST 5% (AB, NT, NU, YT), GST 5% + PST 7% (BC), GST 5% + RST 7% (MB), GST 5% + PST 6% (SK), GST 5% + QST 9.975% (QC), HST 13% (ON), HST 14% (NS), HST 15% (NB, NL, PE). For the US and other countries enter the tax name and rate.
- Tax names and rates can be changed afterwards (by everybody; the place of registration only by administrators). Up to two taxes.
- For companies registered in Canada the Tax ID is printed as **GST/HST No.**
- Issued estimates and invoices keep the taxes they were issued with; documents issued before the upgrade keep the old tax-included display (one VAT line).

The **currency** is set here too: one currency per company, Canadian dollar (CAD) by default, with common alternatives such as USD, EUR, CNY, GBP, HKD and JPY. Amounts are formatted the way the currency is written (`$1,234.50`, `1.234,50 €`, `¥1,234.50`); only the label changes, there is no conversion. Estimates and invoices keep the currency they were issued in, so changing the setting does not affect documents already issued.

![Settings](docs/screenshots/settings.png)

**Email delivery** at the bottom (visible to administrators only, also on the Companies page of the administration) checks the email setup: enter an address and click **Send test email**. The message tells you whether it went out through SMTP or Microsoft Graph.

![Test email](docs/screenshots/settings-test-email.png)

### 13. Profile

**Settings → My Account** (or click your name at the bottom left): change name, email and avatar, change your password, and link or unlink a Microsoft account.

![Profile](docs/screenshots/profile.png)

### 14. User administration (/admin)

Administrators manage companies, employees and logins at `https://your-domain/admin`, also reachable through **Administration** in the user menu at the bottom left. The administration is available in English and Chinese (switch at the top right).

| Role | Can do |
|---|---|
| User | Works in the application (work, clients, vehicles, inventory); changes contact details, invoice and offer options, but not the company name, Reg No, Tax ID or currency; no access to the administration |
| Administrator | Additionally creates companies, manages all settings of **every company** (including name, Reg No, Tax ID, currency) and sends test emails; moves employees to another company; creates normal accounts, edits details, resets passwords, unlocks, disables/enables normal users, unlinks Microsoft accounts |
| Super administrator | Additionally creates administrators and super administrators, changes roles and manages administrator accounts |

- The initial account `admin` is the **owner** (a super administrator): only its owner can change it, and it cannot be disabled or demoted.
- Nobody changes their own role or status in the administration; your own password is changed in your profile.
- **New user**: enter name and email, tick "Create a login for this employee", then username and role. Leave the password empty to get a temporary password, **shown only once**; it must be changed at the first sign in. Sign in codes go to the email address.
- Mechanics who do not sign in: leave the login unticked (or add them with **New** next to **Mechanics** when creating work). A login can be added later.
- **Disable account**: for employees who leave. They are signed out at once and cannot sign in again; work and history are kept. Employees with a login cannot be deleted, only disabled.
- **Reset password**: sets a new temporary password and unlocks the account; it must be changed at the next sign in.
- **Companies**: several companies (branches, workshops) can share one installation and one database:
  - Every company has its own clients, vehicles, work, inventory, estimates and invoices, its own numbering (work, estimate and invoice numbers start at 1 per company), its own settings and currency.
  - Every employee belongs to one company and only sees the data of that company.
  - The **Companies** page lists all companies (Reg No, currency, employees, logins), creates new ones (name and currency), and **Edit** opens all details, currency, invoice and offer options of that company.
  - Choose the company when creating a user. **Company** in the employee details of the user page moves an employee to another company when saved. They then work with the new company's data; work already done stays with the old company. You cannot move yourself.
  - **Company details** further down the user page edit the name, Reg No, Tax ID, currency, phone, email, address and bank account of that user's company (for the whole company); invoice and offer options are under **All settings of this company**.
  - Data that existed before the upgrade belongs to the first company.
- **Audit log**: who created accounts, changed details, reset passwords, disabled/enabled users, changed roles, created companies, changed company settings, moved users to another company, sent test emails, and when.

![Companies](docs/screenshots/admin-companies.png)

![Edit company](docs/screenshots/admin-company.png)

![Users](docs/screenshots/admin-users.png)

| Temporary password after creating a user | User details |
|---|---|
| ![Temporary password](docs/screenshots/admin-user-created.png) | ![User details](docs/screenshots/admin-user.png) |

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
- Services: the API is the systemd service `vg-auto-api`, the web app is the pm2 process `vg-auto-web`.

### Baota panel

The database and the reverse proxy are set up in the Baota panel; one command does the rest (no nginx, the database is not touched):

```bash
sudo deploy/vgauto.sh baota --app-url https://app.example.com --api-url https://api.example.com \
     --db-provider MySql --db-host 127.0.0.1 --db-password '<password set in Baota>' --admin-email you@example.com
```

Step by step (Chinese): [deployment guide, section 1.6](docs/deployment.zh-CN.md#16-使用宝塔面板部署ubuntu--debian).

### Day-to-day: `vgauto`

After installation `sudo vgauto` works from any directory (no sudo on macOS). Without arguments it shows a menu.

| Command | Purpose |
|---|---|
| `sudo vgauto status` | Services, health check, URLs and version |
| `sudo vgauto start` / `stop` / `restart` | Start / stop / restart the API and the web app |
| `sudo vgauto logs api` / `logs web` | Follow the logs |
| `sudo vgauto upgrade` | Back up → `git pull` → reinstall |
| `sudo vgauto backup [--keep 14]` | Database + PDFs + configuration in one archive |
| `sudo vgauto restore <file>` | Restore a backup (asks for confirmation, backs up the current state first) |
| `sudo vgauto pdf-setup` | Installs what PDFs need (system libraries, CJK fonts, browser). Run it when downloading or emailing PDFs fails |

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
3. **Backups**: run `sudo vgauto backup --keep 14` regularly (crontab or a Baota scheduled task) and copy the backups to another machine.

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
| `PuppeteerExecutablePath` | Path to Chrome/Chromium. When empty: a Chrome already downloaded into `PuppeteerPath`, then an installed Chrome/Chromium/Edge, and only then a download |

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
| `/api/employees` | Employees (mechanics); creating logins requires an administrator |
| `/api/admin/*` | User administration: `me`, `users` (create, edit, `account`, `password`, `unlock`, `disable`, `enable`, `role`, `microsoft`, `company`), `companies` (list, create, read and change `{id}/options`), `audit` |
| `/api/options` (with `testemail`) | Settings of the signed in user's company, test email |
| `/api/profile` (with `changepassword`, `externallogins`) | Profile, password, Microsoft account link |
| `/api/query` | List queries |
| `GET /health` | Health check (no authentication) |

Every `/api/auth/*` request must carry `serverSecret`, so only the web server or an administrator can call them.

### Database migrations

DbUp runs every script that has not run yet, in file name order, each time the `DbUp` project starts (the installer runs it automatically). A new migration **needs both databases**:

```
backend/src/DbUp/scripts/Script0006_description.sql         # PostgreSQL
backend/src/DbUp/scripts_mysql/Script0006_description.sql   # MySQL
```

- Numbers must increase; never change a script that has been released.
- PostgreSQL uses the `domain.` and `tenant_config.` schemas; MySQL has no schemas, so the tables are named without the prefix and `tenant_config_*`, and `public.user` becomes `app_user`.
- SQL in code goes through `SqlDialect` for table names and syntax differences; do not write syntax specific to one database.
- **Company isolation**: business tables have a `company_id` column. NHibernate queries are filtered to the current company automatically (`CompanyFilter`) and new rows get the current company; hand-written SQL (Dapper, page queries) must filter on `company_id` itself (`PageResultQuery.ForCompany`, `this.CompanyId()`). New business tables need a `company_id` column and `ApplyFilter<CompanyFilter>` in their mapping.

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

- Multi-tenancy is PostgreSQL only.
- MySQL 8.0 or newer; MariaDB is not supported.
- The web app build needs access to Google Fonts.
- The user interface is in English only.

---

## License

Copyright © 2026 V. G. Global Solution Canada Inc. All rights reserved.

VG Auto is proprietary software. It may not be copied, modified, distributed or otherwise used without written permission; see [LICENSE](LICENSE). The third-party open source components it uses, and their licenses, are listed in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
