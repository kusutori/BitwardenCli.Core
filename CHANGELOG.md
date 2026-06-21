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
