# MCV Ingenieros URL Shortener

Internal-owned URL shortener for MCV Ingenieros, built with **Blazor**, **ASP.NET Core**, and **SQLite**.

## Current MVP status

Implemented so far:
- first-run admin bootstrap
- admin-only employee provisioning
- employee login/logout
- forced password change on first login for admin-created employees
- authenticated short-link creation
- public redirect resolution
- owner-only "My links" management

## Requirements

- .NET SDK 9

## Run locally

```powershell
dotnet restore UrlShortener.slnx
dotnet run --project src\UrlShortener.Web\UrlShortener.Web.csproj
```

Default development URLs are defined in:

- `src\UrlShortener.Web\Properties\launchSettings.json`

## Database and migrations

The application uses SQLite at:

- `src\UrlShortener.Web\Data\app.db`

EF Core migrations are applied automatically at startup.

To add a new migration later:

```powershell
dotnet ef migrations add <MigrationName> --project src\UrlShortener.Web\UrlShortener.Web.csproj --startup-project src\UrlShortener.Web\UrlShortener.Web.csproj
```

## First-time setup

1. Start the application.
2. Open the site.
3. If no admin exists yet, use `/setup/admin` to create the first administrator account.
4. Sign in as that administrator.
5. Use **Create employee** to provision employee accounts.
6. Share temporary passwords securely; employees must change them on first login.

## Main routes

- `/setup/admin` - first administrator bootstrap
- `/Account/Login` - employee login
- `/dashboard` - authenticated dashboard
- `/links/create` - create short links
- `/links/my` - manage owned short links
- `/r/{alias}` - public redirect endpoint

## Test

```powershell
dotnet test UrlShortener.slnx
```

## Operational notes

- Public redirects are case-insensitive.
- Expired links return an expired/not-available response.
- Deleted links are deactivated and stop resolving publicly.
- Prefer idempotent changes and keep project memories in `.github\memories.md`.
