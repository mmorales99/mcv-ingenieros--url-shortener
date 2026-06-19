# Tasks: URL Shortener Website MVP

## Design-step plan (critical-first)
1. Finalize critical-first scope with user.
2. Confirm auth/user model and URL policy constraints.
3. Validate architecture alignment with ADRs and requirements.
4. Break plan into implementation-ready specs.

## Critical-first implementation order
1. Authentication foundation (pre-created accounts + protected pages).
2. Authenticated short-link creation with custom alias and expiration.
3. Public redirect flow with expiration enforcement.
4. User "my links" page with owner-only edit/delete.
5. Hardening and UX/a11y/SEO refinements.

## Implementation plan

### Phase 0 - Solution and architecture baseline
1. Create the .NET solution and Blazor application skeleton.
2. Establish the project structure for:
   - Web/UI
   - Application/use cases
   - Infrastructure/persistence
   - Tests
3. Configure SQLite connection management and environment-based configuration.
4. Wire shared validation, error handling, and logging conventions.
5. Add baseline test projects and verify the initial test command path.

### Phase 1 - Authentication foundation
#### Goal
Enable secure access for pre-created employee accounts and bootstrap the first admin safely.

#### Planned work
1. Data/Auth setup:
   - Configure SQLite-backed auth/user models and EF Core migrations.
   - Add user roles/permissions needed for admin-only provisioning.
   - Implement a temporary first-run setup flow for the first admin account.
   - Implement admin-only employee provisioning for pre-created employee accounts.
2. Backend contracts:
   - No self-service registration flow.
   - Login endpoint/page flow.
   - Logout flow.
   - Auth state validation for protected resources.
   - Guard the first-run setup flow so it becomes unavailable after first admin creation.
3. Frontend UX (Blazor):
   - Accessible first-run admin setup page.
   - Accessible login form (labels, keyboard, focus, errors).
   - Admin-only employee creation screen.
   - Clear success/error messaging and validation summary.
   - Route guards for protected pages.
4. Security controls:
   - Password policy baseline.
   - Framework-backed secure password hashing only.
   - Anti-forgery/session protections per ASP.NET defaults.
   - First-run setup lockout after admin creation.
   - Safe error responses (no sensitive leakage).
5. Testing (TDD-like):
   - Unit tests for auth validation and rules.
   - Integration tests for first-run setup, login/logout, and protected-route enforcement.
   - Authorization tests for admin-only employee provisioning.
   - UI/component tests for setup/login form behaviors.

#### Exit criteria
- First admin can be created exactly once through setup.
- Employees cannot self-register.
- Only admins can provision employee accounts.
- Protected routes require authentication.

### Phase 2 - Authenticated short-link creation
#### Goal
Allow authenticated employees to create short links with validated aliases and optional expiration dates.

#### Planned work
1. Data model:
   - Create the short-link entity with owner reference, normalized alias, destination URL, expiration timestamp, audit timestamps, and active state.
   - Add uniqueness constraints for normalized alias.
2. Backend contracts:
   - Create short-link command/use case.
   - Alias normalization and validation service.
   - URL validation and SSRF baseline checks.
3. Frontend UX (Blazor):
   - Create-link form with destination URL, alias, and optional expiration date.
   - Accessible validation feedback for duplicate/invalid aliases and URLs.
4. Security and rules:
   - Allow only `http/https` destinations.
   - Reject aliases outside letters, numbers, and hyphens.
   - Enforce case-insensitive uniqueness.
5. Testing (TDD-like):
   - Unit tests for alias normalization and validation.
   - Unit tests for destination URL policy.
   - Integration tests for successful and rejected create flows.

#### Exit criteria
- Authenticated users can create links with valid aliases.
- Duplicate aliases are rejected consistently.
- Invalid URLs and unsafe targets are rejected.

### Phase 3 - Public redirect flow
#### Goal
Serve public redirects safely while enforcing expiration rules.

#### Planned work
1. Backend contracts:
   - Redirect resolution endpoint for `/r/{code}`.
   - Not-found behavior for missing aliases.
   - Expired-link resolution behavior.
2. Frontend/public UX:
   - Simple expired/not-available page.
   - Basic not-found response/page for invalid aliases.
3. Security and rules:
   - Resolve aliases case-insensitively through normalized storage.
   - Prevent redirect for expired or inactive links.
4. Testing (TDD-like):
   - Integration tests for valid redirect, missing alias, and expired alias.
   - Route tests for normalized alias matching.

#### Exit criteria
- Valid active aliases redirect publicly.
- Expired aliases never redirect.
- Missing aliases return the agreed failure response.

### Phase 4 - My links management
#### Goal
Let employees manage only the links they own.

#### Planned work
1. Backend contracts:
   - Query for current user's links.
   - Update link use case for destination URL and expiration date.
   - Delete/deactivate link use case.
   - Ownership enforcement on read/update/delete operations.
2. Frontend UX (Blazor):
   - "My links" page listing only the current user's links.
   - Edit flow for destination URL and expiration.
   - Delete confirmation flow.
3. Testing (TDD-like):
   - Integration tests for list/edit/delete success by owner.
   - Authorization tests proving non-owners cannot manage other users' links.
   - UI/component tests for management form behavior.

#### Exit criteria
- Users see only their own links.
- Owners can edit/delete their own links.
- Non-owners are denied consistently.

### Phase 5 - Hardening and release readiness
#### Goal
Prepare the MVP for safe internal adoption.

#### Planned work
1. Security hardening:
   - Review auth/session/cookie configuration.
   - Confirm no sensitive information leaks in errors.
   - Verify expired-link, ownership, and setup lockout edge cases.
2. UX/a11y/SEO:
   - Review focus behavior, labels, error announcements, and keyboard paths.
   - Verify public redirect/expired pages have proper titles and minimal metadata.
3. Operations:
   - Document setup, migration, first-admin bootstrap, and admin provisioning steps.
   - Document backup/migration considerations for SQLite MVP usage.
4. Final validation:
   - Baseline regression check if needed.
   - Full test suite pass.
   - Architect review.
   - Frontend Quality Guardian review if UI changed.
   - UX Design Advisor review if UI changed.

#### Exit criteria
- MVP setup is documented.
- Security and ownership rules are verified.
- Full approval gates are satisfied.

## Quality gates
1. Run baseline test suite before any implementation edits.
2. Implement tests-first (TDD-like) per spec.
3. Run full suite and require 100% pass.
4. Architect review approval.
5. Frontend Quality Guardian approval (if UI touched).
6. UX Design Advisor review (if UI touched).
7. Conventional Commit message(s).
8. Push branch and create/update PR.
