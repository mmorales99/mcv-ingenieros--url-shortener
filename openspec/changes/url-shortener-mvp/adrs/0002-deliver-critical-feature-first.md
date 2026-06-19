# ADR 0002: Deliver most critical feature first

## Status
Accepted

## Context
The user selected critical-first delivery over shortest vertical slice.

## Decision
Prioritize implementation order by business/operational criticality:
1. Authentication foundation
2. Authenticated short-link creation
3. Public redirect + expiration enforcement

## Consequences
- Early risk reduction on security and ownership boundaries.
- Slightly longer time to first full vertical demo than slice-first.
