# ADR 0004: Use admin-managed provisioning with temporary first-run bootstrap

## Status
Accepted

## Context
Short-link creation must be restricted to employees with pre-created accounts. The system therefore needs both an ongoing account-provisioning model and a way to create the first admin safely.

## Decision
- Employee accounts will be created only by authenticated admin users inside the application.
- The very first admin account will be created through a temporary first-run setup page.
- The first-run setup flow must become unavailable after the first admin exists.

## Consequences
- Supports controlled employee access without exposing public registration.
- Adds an initialization path that must be protected against reuse after setup.
- Keeps routine user provisioning inside the application instead of direct database edits.
