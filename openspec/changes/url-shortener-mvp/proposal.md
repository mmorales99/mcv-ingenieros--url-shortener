# URL Shortener Website MVP

## Why
Provide an internal-owned, professional URL shortener for MCV Ingenieros so the company is not dependent on third-party tools.

## What Changes
- Build a web app with **Blazor frontend**, **.NET backend**, and **SQLite** database.
- Require authentication for link creation and management.
- Use pre-created employee accounts with email/password login.
- Implement core short-link lifecycle with custom aliases and expiration dates.

## In Scope
- User authentication with admin-provisioned email/password accounts.
- Login/logout for employees with pre-created accounts.
- Create short link from valid URL with custom alias and optional expiration date.
- Redirect short code to original URL.
- Basic "my links" management view.
- Edit/delete only for the employee who owns the link.

## Out of Scope
- Self-service user registration.
- Team/multi-tenant workspaces.
- QR generation.
- Click counting and analytics dashboards.
- Public short-link creation.

## Success Criteria
- Employees can log in only if an admin created their account in advance.
- Authenticated employees can create, edit, and delete their own short links.
- Public visitors can use short URLs for redirection.
- Links can define expiration dates and expired links stop redirecting.
- Critical-first feature path is documented and approved.
- Baseline tests run before implementation.
- Full test suite ends at 100% pass rate.
- Architect + frontend quality + UX reviews are approved.
