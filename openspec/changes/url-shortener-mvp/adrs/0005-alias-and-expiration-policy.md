# ADR 0005: Use normalized aliases and expired-link landing page

## Status
Accepted

## Context
The URL shortener needs predictable alias behavior and a clear public outcome when a link has expired.

## Decision
- Short-link aliases must be globally unique.
- Alias matching is case-insensitive.
- Allowed alias characters are letters, numbers, and hyphens only.
- Expired links must not redirect; instead, they must render a simple expired/not-available page.

## Consequences
- Simplifies route resolution and avoids case-related ambiguity.
- Reduces risky or confusing alias formats.
- Provides a clearer public experience than a silent 404 when a link has expired.
