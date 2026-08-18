# Behsazan

[Home](README.md) | **English** | [فارسی](README.fa.md)

A web app for managing customers, projects, invoices, payments, and project finances.

## What this project solves

Before this system, daily work depended on Excel files, manual data entry, hand-made invoices, and separate PDF files. Tracking who owed what for each project was slow and needed one person who knew the process.

Behsazan replaces that scattered workflow with one browser-based system:

```text
Customer → Project → Invoice → Payment (deposit) → Project ledger / financial reports
```

The app is **not** a full ERP or accounting system. It is a practical tool for:

- Customers and phone numbers
- Projects (joist type and address)
- Invoices with line items, auto numbering, Excel and PDF export
- Payments (deposits) on a project
- Project ledger (debit / credit / remaining balance)
- Dashboard and financial charts

## Technologies

| Area | Technology |
|---|---|
| Language / runtime | C#, .NET 9 |
| Web UI | ASP.NET Core, Blazor Server |
| UI components | MudBlazor (Persian RTL) |
| Architecture | Clean Architecture (Domain, Application, Infrastructure, Presentation) |
| Database | SQL Server, Entity Framework Core 9 |
| Auth | JWT, BCrypt |
| Excel | ClosedXML |
| PDF | QuestPDF |

## Requirements

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB, Express, or full)
- [EF Core tools](https://learn.microsoft.com/ef/core/cli/dotnet) for migrations:

```bash
dotnet tool install --global dotnet-ef
```

## Build and run (local)

1. Clone the repository and open a terminal in the project root.

2. Set the SQL Server connection in `src/Presentation/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=BehsazanDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

Change `Server=.` if your SQL Server instance has another name (for example `Server=.\\SQLEXPRESS`).

3. Create the database and apply migrations:

```bash
dotnet ef database update --project src/Infrastructure --startup-project src/Presentation
```

4. Run the app:

```bash
dotnet run --project src/Presentation
```

Or open `Behsazan.slnx` in Visual Studio / Cursor and start the **https** profile.

5. Open:

- HTTP: `http://localhost:5214`
- HTTPS: `https://localhost:7172`

6. Login (first run creates this user automatically):

| Username | Password |
|---|---|
| `admin` | `Admin@123` |

Change this password in a real environment.

## Deploy

There is no Docker or automatic deploy script. Publish the Blazor Server app, then host it on IIS or Kestrel.

### 1. Publish

```bash
dotnet publish src/Presentation/Behsazan.Presentation.csproj -c Release -o ./publish
```

### 2. Production settings

In the publish folder, set production values (or use environment variables / `appsettings.Production.json`):

- `ConnectionStrings:DefaultConnection` — SQL Server on the server
- `Jwt:Key` — a long unique secret (do not use the development key)

Apply migrations on the production database from a machine that can reach it:

```bash
dotnet ef database update --project src/Infrastructure --startup-project src/Presentation
```

### 3. IIS (Windows Server)

1. Install the [.NET 9 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/9.0).
2. Copy the `publish` folder to the server (for example `C:\inetpub\behsazan`).
3. In IIS, create a site pointing to that folder.
4. Set the application pool to **No Managed Code**.
5. Give the app pool identity read/write access to the folder if needed.
6. Confirm SQL Server allows the app (Windows auth or a SQL login in the connection string).
7. Browse the site URL and sign in.

### 4. Kestrel (simple)

On the server:

```bash
cd publish
dotnet Behsazan.Presentation.dll --urls "http://0.0.0.0:8080"
```

Put a reverse proxy (IIS / Nginx) in front of it if you need HTTPS.
