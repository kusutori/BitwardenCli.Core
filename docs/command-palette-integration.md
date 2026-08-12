# Command Palette Integration

This document records the integration contract used by Bitwarden For Command Palette.

## CLI executable

Host applications choose the executable through `BitwardenCliOptions.ExecutablePath`.

- Use `bw` or a full path to `bw.exe` for the official Bitwarden CLI.
- Use `bwbio` or a full path to `bwbio.exe` to enable Windows Hello unlock through bitwarden-cli-bio.

`BitwardenCli.Core` does not detect or require a specific executable name. The host decides when to show biometric UI based on its configured executable path.

## Profile isolation

Command Palette uses one isolated profile directory per account through `BitwardenAccountProfile.CliDataDirectory`.

The default single-account profile is expected to use:

```text
%LOCALAPPDATA%\BitwardenForCommandPalette\profiles\default
```

Every CLI invocation receives this value as `BITWARDENCLI_APPDATA_DIR`. This prevents the extension from reading or mutating the user's global Bitwarden CLI profile.

## Sessions

Session keys remain memory-only inside `BitwardenSessionState`.

Host applications must not persist session keys and should not pass `BW_SESSION` manually. Command clients add the current session to CLI invocations when needed. The supported host check is:

```csharp
client.Session.IsUnlocked
```

Locking clears only the selected client's in-memory session. Logging out mutates only that profile's isolated CLI directory.

## Unlock modes

Master password unlock uses `AuthenticationClient.UnlockAsync` or `BitwardenCliClient.UnlockAsync`. The password is supplied by an `ISecretProvider` and transported to the CLI through a per-invocation secret environment variable.

Biometric unlock uses `AuthenticationClient.UnlockWithBiometricAsync` or `BitwardenCliClient.UnlockWithBiometricAsync`. The command is:

```text
unlock --raw
```

The invocation sets `NoInteraction = false` so compatible wrappers such as `bwbio.exe` can show Windows Hello. On success, stdout must contain the session key. On user cancellation, callers receive `CliErrorCode.UserInteractionCancelled` when the CLI output can be classified.

## Error handling

Hosts should map `CliResult.Error.Code` to localized UI messages instead of parsing stderr directly. Command Palette should handle at least:

- `ExecutableNotFound`
- `Unauthenticated`
- `VaultLocked`
- `InvalidMasterPassword`
- `UserInteractionCancelled`
- `NetworkUnavailable`
- `InvalidResponse`
- `Timeout`
- `Cancelled`
