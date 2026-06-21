# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Added

- Initial solution, package and test structure.
- Secure process runner with argument lists, stdin, redacted diagnostics, cancellation and timeouts.
- Structured CLI results, stable error categories and installed version inspection.
- Isolated account profiles backed by a dedicated `BITWARDENCLI_APPDATA_DIR` per account.
- Memory-only session handling with per-account command serialization.
- Password, API key and SSO login flows, plus unlock, lock and logout operations.
- Status, server configuration and synchronization commands.
- Unit and opt-in real-CLI integration tests for profile isolation.
- Vault item list/search/get/create/edit/clone/delete/restore/archive commands.
- Scalar username, password, URI, notes, TOTP and exposed-password queries.
- Collection assignment and organization move commands.
- Nullable-safe vault DTOs with forward-compatible unknown JSON fields.
- Folder create, read, update, delete and search commands.
- Attachment upload, download and delete commands with absolute-path validation.
- Organization, collection and organization-collection listing.
- Password and passphrase generation with typed options.
- Vault import format discovery, import and passwordless export commands.
- Send list/get/create/edit/delete/receive and password-removal commands.
- Device approval and organization member confirmation commands.
- Status-driven profile metadata refresh and non-interactive MFA code support.
- Template, fingerprint, organization member and organization collection commands.
- Send file download and receive operations.
