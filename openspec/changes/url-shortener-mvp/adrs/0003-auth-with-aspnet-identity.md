# ADR 0003: Use database-backed pre-created local accounts for MVP authentication

## Status
Accepted

## Context
Link creation must require authentication. The selected auth approach is email/password checked against the application database, and user accounts must be created in advance by an admin.

## Decision
Use a database-backed local authentication approach in the .NET application, with secure password hashing and login/logout support. Do not allow self-service registration in MVP; only pre-created employee accounts may authenticate.

## Consequences
- Supports the required employee-only creation flow without exposing public registration.
- Enables per-user ownership for “my links” scope.
- Requires an admin bootstrap or provisioning flow for user creation.
- Future external provider integration remains possible if requirements change.
