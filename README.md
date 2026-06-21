# BitwardenCli.Core

`BitwardenCli.Core` is a UI-independent .NET client for the official Bitwarden CLI (`bw`). It provides structured command results, secure secret transport, cancellation and isolated multi-account profiles through `BITWARDENCLI_APPDATA_DIR`.

The package targets .NET 10 and does not depend on WinUI, Reactor, WPF or any other UI framework.

## Status

This package is under active development. Version `0.1.0` is the first local integration target for BitwardenForReactor.

## Build

```powershell
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet pack src/BitwardenCli.Core/BitwardenCli.Core.csproj -c Release -o artifacts
```

## Quick start

```csharp
using BitwardenCli.Core;
using BitwardenCli.Core.Accounts;
using BitwardenCli.Core.Authentication;

var profile = new BitwardenAccountProfile
{
    Id = Guid.NewGuid(),
    DisplayName = "Personal",
    CliDataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MyApp", "accounts", "personal", "cli")
};

var client = new BitwardenCliClientFactory(new BitwardenCliOptions
{
    ExecutablePath = "bw"
}).Create(profile);

var status = await client.GetStatusAsync();
var secrets = DelegateSecretProvider.FromMasterPassword("prompted by the host UI");
var unlocked = await client.UnlockAsync(secrets);
if (unlocked.IsSuccess)
{
    var items = await client.Vault.ListItemsAsync();
}
```

Create one stable profile per account and give every profile a different absolute
`CliDataDirectory`. The library injects `BITWARDENCLI_APPDATA_DIR` into every process.
Sessions remain in memory and are scoped to the resulting client instance.

Command groups are available through `Authentication`, `Synchronization`, `Vault`,
`Folders`, `Attachments`, `Organizations`, `Generator`, `ImportExport`, `Sends`, and
`Administration`. Every operation accepts a `CancellationToken` and returns a
`CliResult` with a stable error category.

See [the 0.1.0 migration guide](docs/migration-0.1.0.md) when replacing a copied
application service.

## Security model

- Account CLI data is isolated by profile directory.
- Session keys and login secrets are memory-only.
- Secrets and vault JSON are not placed in process arguments.
- The library does not persist credentials.
- Password-protected export is intentionally unavailable because the current CLI has
  no `--passwordenv` option for export. Account-key encrypted JSON remains available.

Bitwarden is a trademark of Bitwarden Inc. This project is an independent client library and is not affiliated with Bitwarden Inc.
