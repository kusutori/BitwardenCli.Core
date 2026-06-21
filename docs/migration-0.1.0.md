# Migration to 0.1.0

Replace copied CLI service source with a package reference to `BitwardenCli.Core`.

1. Create one stable `BitwardenAccountProfile` per account.
2. Give every profile a unique absolute `CliDataDirectory`.
3. Create clients through a shared `BitwardenCliClientFactory`.
4. Supply passwords and API secrets through `ISecretProvider`; never persist them in profile JSON.
5. Keep UI drafts, notices, clipboard behavior and translated error messages in the host application.

The host must treat account switch, lock and logout as different operations. Switching selects another client without copying sessions. Lock clears only the selected client's memory session. Logout also updates only that profile's isolated CLI directory.

Password-protected export is not exposed because `bw export` currently has no `--passwordenv` option. Use account-key encrypted JSON or perform password export directly with the CLI after reviewing its process-argument exposure.
