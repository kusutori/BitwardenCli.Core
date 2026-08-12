# NuGet release

The `Publish NuGet package` GitHub Actions workflow builds, tests, packs, validates, and publishes `BitwardenCli.Core` when a release tag is pushed.

## Repository secret

Create a scoped API key at NuGet.org, then add it under **GitHub repository > Settings > Secrets and variables > Actions > Repository secrets**:

| Secret | Value |
| --- | --- |
| `NUGET_API_KEY` | NuGet.org API key with permission to push `BitwardenCli.Core` |

Recommended NuGet.org API key settings:

- Scope: **Push new packages and package versions**.
- Package glob pattern: `BitwardenCli.Core`.
- Expiration: the shortest operationally practical period; rotate it before expiry.

Do not add the key to `nuget.config`, source files, logs, or repository variables.

## Dry run

Open **Actions > Publish NuGet package > Run workflow**, enter a version, and leave **Publish** disabled. The generated `.nupkg` and `.snupkg` are available as workflow artifacts.

## Release

Commit all intended release changes, update `CHANGELOG.md`, and push a SemVer tag:

```powershell
git tag -a v0.1.2 -m "Release v0.1.2"
git push origin v0.1.2
```

Prerelease tags are also supported:

```powershell
git tag -a v0.2.0-preview.1 -m "Release v0.2.0-preview.1"
git push origin v0.2.0-preview.1
```

The workflow strips the leading `v` and overrides the project version during build and pack. NuGet.org package versions are immutable; correcting a release requires a new version and tag.
