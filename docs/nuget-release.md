# NuGet release

The `Publish NuGet package` GitHub Actions workflow builds, tests, packs, validates, and publishes `BitwardenCli.Core` when a release tag is pushed.

## NuGet.org trusted publishing policy

Sign in to NuGet.org, open **Trusted Publishing**, and add a GitHub Actions policy with these exact values:

| Policy field | Value |
| --- | --- |
| Repository owner | `kusutori` |
| Repository | `BitwardenCli.Core` |
| Workflow file | `publish-nuget.yml` |
| Environment | `nuget-release` |

Enter only the workflow file name, not `.github/workflows/publish-nuget.yml`. Select the NuGet.org user or organization that owns `BitwardenCli.Core` as the policy owner.

The workflow requests a GitHub OIDC token and exchanges it through `NuGet/login@v1` for a one-hour temporary API key immediately before publishing. No long-lived NuGet API key is stored in GitHub.

## GitHub configuration

Create a GitHub Environment named `nuget-release`. Optional environment protection rules can require approval before publishing a tagged release.

Add this repository secret under **Settings > Secrets and variables > Actions**:

| Secret | Value |
| --- | --- |
| `NUGET_USER` | NuGet.org username/profile name that owns the trusted publishing policy; do not use an email address |

The workflow has `id-token: write` permission only so GitHub can issue the short-lived OIDC token. It retains read-only repository content access.

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
