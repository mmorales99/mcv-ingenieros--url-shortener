# Spec: Authentication Foundation

## Requirement 1: Pre-created employee account access
The system must allow authentication only for users whose accounts were created in advance by an admin.

### Scenario: successful login for pre-created user
- **Given** an employee account already exists with a valid email and password
- **When** the user submits valid credentials
- **Then** the user session is established and protected pages become accessible

### Scenario: unknown user cannot self-register
- **Given** no account exists for an email address
- **When** a person attempts to access the application
- **Then** the system provides login only and does not expose self-service registration

## Requirement 2: First admin bootstrap
The system must allow secure creation of the first administrator account during initial setup only.

### Scenario: first admin is created on first run
- **Given** the application has no admin account yet
- **When** the setup page is completed with valid admin credentials
- **Then** the system creates the first admin account and enables normal authenticated administration

### Scenario: setup is unavailable after first admin exists
- **Given** at least one admin account already exists
- **When** a client requests the first-run setup page
- **Then** the system denies or disables access to that setup flow

## Requirement 3: User login/logout
The system must support secure login and logout flows.

### Scenario: successful login
- **Given** a pre-created user with valid credentials
- **When** the user logs in
- **Then** the user session is established and protected pages become accessible

### Scenario: failed login
- **Given** invalid credentials or a non-existent account
- **When** the user logs in
- **Then** login is denied with non-sensitive error feedback

### Scenario: logout
- **Given** an authenticated user
- **When** the user logs out
- **Then** the session ends and protected pages require login again

## Requirement 4: Protected resource access
The system must restrict protected application actions to authenticated users.

### Scenario: unauthenticated access to protected route
- **Given** a user not logged in
- **When** they open a protected route
- **Then** they are redirected to login or denied access per policy

### Scenario: authenticated access to protected route
- **Given** a logged-in user
- **When** they open a protected route
- **Then** access is granted

## Requirement 5: Admin-only user provisioning
The system must allow only admin users to create employee accounts.

### Scenario: admin creates employee account
- **Given** an authenticated admin user
- **When** the admin submits a valid new employee email and password
- **Then** the system creates the employee account for later login

### Scenario: non-admin cannot create employee account
- **Given** an authenticated non-admin user
- **When** they attempt to access or submit the user-creation flow
- **Then** the system denies the action

## Requirement 6: Accessibility in auth UI
Authentication pages must follow accessibility best practices.

### Scenario: keyboard and screen reader support
- **Given** auth forms
- **When** a user navigates with keyboard/screen reader
- **Then** controls are properly labeled, focus order is logical, and errors are announced clearly
