# Spec: URL Shortener Core

## Requirement 1: Authenticated link creation
The system must require authenticated users to create short links.

### Scenario: authenticated user creates short link with alias and expiration
- **Given** a logged-in user, a valid `http/https` URL, a requested custom alias, and an optional expiration date
- **When** the user submits the short-link form
- **Then** the system validates the inputs, stores the link under that user, and returns the short URL for the chosen alias

### Scenario: unauthenticated create request is denied
- **Given** a user not logged in
- **When** the user attempts to create a short link
- **Then** the system denies the action and requests authentication

### Scenario: duplicate alias is rejected
- **Given** an alias that already exists
- **When** an authenticated user attempts to create a short link with that alias
- **Then** the system rejects the request with clear validation feedback

### Scenario: alias is normalized and validated
- **Given** a requested alias containing uppercase characters
- **When** an authenticated user submits the short-link form
- **Then** the system applies case-insensitive uniqueness rules and accepts only letters, numbers, and hyphens

## Requirement 2: Redirect behavior
The system must resolve active short codes and redirect to the original URL.

### Scenario: code exists
- **Given** an existing non-expired short code
- **When** a client requests `/r/{code}`
- **Then** the system redirects to the original URL

### Scenario: code missing
- **Given** a non-existing short code
- **When** a client requests `/r/{code}`
- **Then** the system returns not found

### Scenario: code is expired
- **Given** an existing short code whose expiration date has passed
- **When** a client requests `/r/{code}`
- **Then** the system does not redirect and returns a simple expired/not-available page

## Requirement 3: Owner link visibility
The system must let authenticated users view only the links they created.

### Scenario: owner views own links
- **Given** a logged-in user
- **When** they open "my links"
- **Then** they see only the links they created

## Requirement 4: Owner link management
The system must let authenticated users edit and delete only the links they own.

### Scenario: owner edits own link
- **Given** a logged-in user who owns a short link
- **When** they update the original URL or expiration date
- **Then** the system persists the changes

### Scenario: owner deletes own link
- **Given** a logged-in user who owns a short link
- **When** they delete the link
- **Then** the link is removed or deactivated per policy and no longer redirects

### Scenario: non-owner cannot manage another user's link
- **Given** a logged-in user who does not own a short link
- **When** they attempt to edit or delete that link
- **Then** the system denies the action
