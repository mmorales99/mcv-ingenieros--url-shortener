# Design: URL Shortener Website MVP

## Foundation decisions
- Frontend: **Blazor**
- Backend: **ASP.NET Core (.NET)**
- Database: **SQLite**
- Auth: **database-backed local accounts with email/password**
- User provisioning: **admin-only user creation inside the app**
- First admin bootstrap: **temporary first-run setup page**
- Alias policy: **globally unique, case-insensitive, letters/numbers/hyphens only**
- Expired-link behavior: **show expired/not-available page**
- Delivery strategy: **most critical feature first**

## Architecture principles
In addition to SOLID, KISS, and design-pattern discipline:
- Security by default.
- Ownership boundaries by default.
- Accessibility-first UI.
- Usability-first interactions.
- SEO-aware rendering and metadata for public pages.
- Explicit error handling and observability.
- Operational independence from third-party shortener services.

## High-level architecture
- `Web` (Blazor): authenticated UI for link creation and management.
- `API/Application` (.NET): business rules, validation, use cases.
- `Infrastructure`:
  - EF Core + SQLite
  - ASP.NET Core Identity or equivalent password-hashing/auth integration backed by SQLite
  - redirect resolution + expiration enforcement persistence

## Core endpoints/features
- Auth flows (login/logout only for pre-created accounts).
- Temporary setup flow to create the first admin account.
- Admin-only employee account creation flow.
- Create short link (authenticated) with custom alias and optional expiration date.
- Redirect endpoint (`/r/{code}`) for public access.
- Expired/not-available public page for expired links.
- "My links" list.
- Edit/delete for owned links only.

## Security and frontend quality baseline
- URL validation (`http/https` only).
- Reject unsafe/private-network redirect targets (SSRF mitigation baseline).
- Store passwords using framework-backed secure hashing only; never store plain-text passwords.
- Deny login for unknown or disabled accounts.
- Disable or remove the first-run setup flow after the first admin is created.
- Enforce ownership checks on link edit/delete operations.
- Normalize aliases for uniqueness checks and route resolution.
- Prevent redirects for expired links.
- Input validation + output encoding.
- Semantic markup, keyboard support, visible focus, labels, contrast.
- Proper page titles/meta for crawlable/public pages.
