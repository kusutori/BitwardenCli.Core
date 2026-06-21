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

## Security model

- Account CLI data is isolated by profile directory.
- Session keys and login secrets are memory-only.
- Secrets and vault JSON are not placed in process arguments.
- The library does not persist credentials.

Bitwarden is a trademark of Bitwarden Inc. This project is an independent client library and is not affiliated with Bitwarden Inc.
