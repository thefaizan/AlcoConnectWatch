# AlcoConnect Watch

**File Watcher & Automated Compliance Report System**

A mining site compliance tool that reconciles two Excel data sources — Evac roster reports vs AlcoConnect breathalyser activity reports — to identify personnel who are on-site but have not completed alcohol testing. Built for Quenton's mining operations across three sites: Dalgaranga, Mt Magnet, and Edna May.

---

## Tech Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| Backend | ASP.NET Web API 2 | .NET Framework 4.8 |
| ORM | Entity Framework 6 | 6.5.1 (Code First, Auto Migrations) |
| Database | SQL Server Express | 2022 |
| Excel Parsing | ClosedXML | 0.102.3 |
| JSON | Newtonsoft.Json | 13.0.3 |
| Frontend | Alpine.js + Tailwind CSS | 3.14.8 / CDN |
| Icons | Bootstrap Icons | 1.11.3 |
| Hosting | IIS / IIS Express | Shared hosting via Plesk (InterServer) |

---

## Prerequisites

- **Visual Studio 2022** (Community or higher)
- **SQL Server Express 2022** — instance: `localhost\SQLEXPRESS`
- **SQL Server Management Studio (SSMS)** — for visual database browsing
- **IIS Express** (included with Visual Studio)
- **.NET Framework 4.8 Targeting Pack** (included with VS 2022)

---

## Project Structure

```
sqlbackend/
├── AlcoConnectWatch.sln              # Solution file
├── README.md
├── .gitignore
│
├── AlcoConnectWatch/                 # ASP.NET Web API project
│   ├── AlcoConnectWatch.csproj
│   ├── packages.config
│   ├── Web.config                    # Connection string, EF config, binding redirects
│   ├── Global.asax / Global.asax.cs  # App startup — DB init, file watcher start
│   │
│   ├── App_Start/
│   │   └── WebApiConfig.cs           # CORS, routes, JSON camelCase serialization
│   │
│   ├── Models/
│   │   ├── EvacRecord.cs             # Evac roster entity (12 fields)
│   │   ├── AlcoConnectRecord.cs      # Breathalyser test entity (15 fields)
│   │   ├── FileImportLog.cs          # Import audit log
│   │   ├── User.cs                   # User with site access permissions
│   │   ├── AppSetting.cs             # Key-value settings store
│   │   └── DTOs/
│   │       ├── AuthDTOs.cs           # LoginRequest/Response, UserInfo
│   │       ├── DashboardDTOs.cs      # DashboardStats, ActivityItem
│   │       ├── ReportDTOs.cs         # ReportRequest/Response, 3 record types
│   │       ├── UserDTOs.cs           # UserDTO, Create/Update requests
│   │       └── SettingsDTOs.cs       # SettingsDTO, FileMonitorStatus, RecentFileDTO
│   │
│   ├── Data/
│   │   ├── AlcoConnectWatchContext.cs # DbContext with 5 DbSets + indexes
│   │   └── DbInitializer.cs          # Seed data + SHA256 password hashing
│   │
│   ├── Migrations/
│   │   └── Configuration.cs          # Auto migrations + seed logic
│   │
│   ├── Services/
│   │   ├── ExcelParserService.cs     # Detects file type, parses Evac & AlcoConnect xlsx
│   │   ├── ComparisonEngine.cs       # Matches Evac IDs against AlcoConnect StaffIDs
│   │   ├── FileWatcherService.cs     # Singleton timer-based folder scanner
│   │   └── ExportService.cs          # Excel (3-sheet) & CSV export generation
│   │
│   ├── Controllers/
│   │   ├── AuthController.cs         # POST /api/auth/login
│   │   ├── DashboardController.cs    # GET /api/dashboard/stats, /activity
│   │   ├── ReportController.cs       # POST /api/report/generate, GET /sites, /available-dates
│   │   ├── ImportLogController.cs    # GET /api/importlogs
│   │   ├── FileMonitorController.cs  # GET /api/filemonitor/status, /recent-files, POST /restart
│   │   ├── UserController.cs         # CRUD /api/users
│   │   ├── SettingsController.cs     # GET/PUT /api/settings
│   │   └── ExportController.cs       # POST /api/export/excel, /csv
│   │
│   ├── Properties/
│   │   └── AssemblyInfo.cs
│   │
│   ├── index.html                    # Login page (Alpine.js)
│   ├── dashboard.html                # Main SPA — 6 views with sidebar navigation
│   ├── css/styles.css                # Gold mining theme, responsive design
│   └── js/app.js                     # Alpine.js app — all API calls, state management
│
└── packages/                         # NuGet packages (git-ignored)
```

---

## Getting Started

### 1. Clone & Restore Packages

```bash
git clone <repo-url>
cd sqlbackend
nuget restore AlcoConnectWatch.sln
```

Or open `AlcoConnectWatch.sln` in Visual Studio 2022 and let NuGet auto-restore.

### 2. Verify SQL Server

Ensure SQL Server Express is running:

```bash
sqlcmd -S "localhost\SQLEXPRESS" -Q "SELECT @@VERSION"
```

### 3. Build

```bash
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" AlcoConnectWatch.sln /t:Build /verbosity:minimal
```

### 4. Run

**Option A — IIS Express (command line):**

```bash
"C:\Program Files\IIS Express\iisexpress.exe" /path:"D:\Projects\sqlbackend\AlcoConnectWatch" /port:5050
```

**Option B — Visual Studio:**

Open the solution, press F5 (or Ctrl+F5 for without debugger).

### 5. Open in Browser

Navigate to **http://localhost:5050**

**Default credentials:** `admin@alcoconnectwatch.com` / `admin123`

### 6. Database

The database (`AlcoConnectWatchDB`) is created automatically on first run via Entity Framework's `MigrateDatabaseToLatestVersion` initializer. No manual SQL scripts needed.

To browse the database visually, open **SSMS** and connect to `localhost\SQLEXPRESS`.

---

## Database Schema

### Tables

| Table | Purpose | Key Indexes |
|-------|---------|-------------|
| `EvacRecords` | Daily evacuation roster data | (ExtractedId, RosterDate, WorkSite) |
| `AlcoConnectRecords` | Breathalyser test results | (StaffId, TestDate, Site), (StaffId, TestDate, Site, TestTime) |
| `FileImportLogs` | Audit trail of all file imports | (ImportedAt) |
| `Users` | Application users with site access | (Email) unique |
| `AppSettings` | Key-value configuration store | SettingKey (PK) |
| `__MigrationHistory` | EF migration tracking | — |

### Default Seed Data

- **Admin user:** admin@alcoconnectwatch.com (password: admin123, SHA256 hashed)
- **Watch folder:** `C:\AlcoConnectWatch\WatchFolder`
- **Scan interval:** 5 minutes
- **Sites:** Dalgaranga, Mt Magnet, Edna May

---

## API Endpoints

### Authentication

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/login` | Authenticate with email/password, returns JWT-like token |

### Dashboard

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/dashboard/stats` | Files processed today, active sites, compliance rate, gaps |
| GET | `/api/dashboard/activity` | Last 10 import events with icons and timestamps |

### Reports

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/report/generate` | Generate comparison report for a date + site |
| GET | `/api/report/sites` | List configured mining sites |
| GET | `/api/report/available-dates` | Dates with imported Evac data (last 30) |

### File Monitor

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/filemonitor/status` | Watcher running state, folder, interval, today's counts |
| GET | `/api/filemonitor/recent-files` | Last 20 imported files with status |
| POST | `/api/filemonitor/restart` | Restart the file watcher service |

### Import Logs

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/importlogs` | Last 100 file import logs |

### Users

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/users` | List all users (passwords masked) |
| POST | `/api/users` | Create user with email, password, site access |
| PUT | `/api/users/{id}` | Update user details |
| DELETE | `/api/users/{id}` | Delete user |

### Settings

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/settings` | Get current app settings |
| PUT | `/api/settings` | Save settings (restarts file watcher) |

### Export

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/export/excel` | Download 3-sheet Excel report (.xlsx) |
| POST | `/api/export/csv` | Download CSV for a specific report group |

---

## Core Business Logic

### ID Matching

The comparison engine matches personnel between two systems:

1. **Evac roster** has `IMAOpenINX` values like `RAM-a25099A`
2. **AlcoConnect** has `Staff ID` values like `25099`

The system strips all non-numeric characters from IMAOpenINX, trims leading zeros, then matches against AlcoConnect Staff IDs (also trimmed of leading zeros).

### Report Groups

| Group | Meaning | Action Required |
|-------|---------|-----------------|
| **Evac Only** | On roster but NOT tested | Compliance gap — needs follow-up |
| **AlcoConnect Only** | Tested but NOT on roster | May be visitor/contractor not on evac list |
| **Matched** | On roster AND tested | Fully compliant |

### File Watcher

The `FileWatcherService` runs as a singleton background service:

1. Scans the configured watch folder every N minutes
2. Detects file type by filename pattern (e.g., "Evac" in name = Evac file)
3. Parses Excel files using ClosedXML
4. Deduplicates AlcoConnect records by (StaffId + TestDate + Site + TestTime)
5. Logs each import to `FileImportLogs`
6. Moves processed files to a `Processed` subfolder

### Supported File Formats

**Evac Report** (detected by "Evac" in filename):
- Required columns: workgroup, Name, Organisation, RosterDate, Work Site, WorkStatus, Room, mobile, IMAOpenINX

**Breathalyser Report** (detected by "breathalyser" in filename):
- Required columns: Site, Staff ID, Staff Name, Date, Time, Result
- Optional: Job Title, Phone, Email, Manager, Machine Type, Serial Number, Location

---

## NuGet Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.AspNet.WebApi.Core | 5.3.0 | Web API framework |
| Microsoft.AspNet.WebApi.WebHost | 5.3.0 | IIS hosting for Web API |
| Microsoft.AspNet.WebApi.Client | 6.0.0 | HTTP formatting |
| Microsoft.AspNet.Cors | 5.3.0 | CORS support |
| Microsoft.AspNet.WebApi.Cors | 5.3.0 | CORS for Web API |
| EntityFramework | 6.5.1 | ORM |
| ClosedXML | 0.102.3 | Excel file read/write |
| Newtonsoft.Json | 13.0.3 | JSON serialization |
| Microsoft.Owin | 4.2.2 | OWIN abstraction |
| Microsoft.Owin.Host.SystemWeb | 4.2.2 | OWIN IIS integration |
| Owin | 1.0 | OWIN interface |

---

## Frontend Architecture

The frontend is a single-page application built with **Alpine.js** for reactivity and **Tailwind CSS** for styling.

- **index.html** — Login page. Posts credentials to `/api/auth/login`, stores token and user info in `localStorage`.
- **dashboard.html** — Main SPA with 6 views controlled by Alpine.js (`currentView` state):
  - Dashboard (stats + activity)
  - Reports (wizard: date → site → generate, 3-tab results)
  - File Monitor (watcher status + recent files)
  - Import Logs (filterable table)
  - Users (CRUD with modal form)
  - Settings (watch folder, scan interval, sites)
- **js/app.js** — All API calls use `fetch()` with auth token from localStorage. No mock data.
- **css/styles.css** — Custom gold mining theme with animations, sidebar, cards, and responsive design.

---

## Deployment (Shared Hosting)

Target: **InterServer.net** shared Windows hosting with Plesk control panel (ASP.NET 4.8.1 runtime).

1. Build in Release mode
2. Publish the `AlcoConnectWatch` folder contents to the hosting directory
3. Update `Web.config` connection string to point to the hosted SQL Server
4. Ensure the watch folder path is accessible from the server
5. Set `index.html` as the default document in Plesk/IIS

---

## Development Notes

- **JSON serialization** uses camelCase (configured in `WebApiConfig.cs`). Frontend properties match this casing.
- **Date format** in JSON is `yyyy-MM-dd`.
- **CORS** is wide open (`*`) for development. Restrict in production.
- **Passwords** are hashed with SHA256 (Base64 encoded). Not bcrypt — adequate for internal tooling.
- **Auth token** is a simple Base64-encoded string (`userId:email:ticks`). No JWT validation middleware — token is checked by presence only.
- **File watcher** starts automatically on `Application_Start` and stops on `Application_End`.
- **Assembly binding redirects** in Web.config handle version mismatches for System.Memory, System.Buffers, Newtonsoft.Json, and System.Net.Http.Formatting.

---

## Version History

| Date | Commit | Description |
|------|--------|-------------|
| 2026-04-30 | `7d5f0b3` | Initial commit — full backend + frontend integration |
| 2026-04-30 | `d50879d` | Fix EF6 provider type, OWIN startup, migration seed |
