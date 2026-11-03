# Maintaining and publishing these docs

## Source of truth

Edit Markdown pages under `docs/guides`, add navigation entries in `docs/guides/toc.yml`,
and update public API XML comments alongside the implementation.
The home page is `docs/index.md`; DocFX configuration is `docs/docfx.json`.

Do not commit generated `docs/_site`, API YAML files or DocFX caches.
`docs/api/index.md` is handwritten and should remain tracked.
The tool version is pinned in `.config/dotnet-tools.json`.

## Build locally

Run from the repository root with the .NET SDK selected by `global.json`:

```powershell
pwsh -File docs\build.ps1
```

Use PowerShell 7 (`pwsh`); on Linux and macOS the script path is `docs/build.ps1`.
The same script is used by GitHub Actions. It restores the pinned tool, builds only
the library project graph, and invokes DocFX with `--warningsAsErrors`.
The build emits XML comments alongside the assemblies without creating NuGet packages.
Only the missing-XML-comment compiler
warning (CS1591) is suppressed, since not all existing public members have comments.
DocFX reads the four library assemblies and their XML comments, generates public API
metadata, then builds the HTML site into `docs/_site`. Using assemblies avoids
design-time project-loading issues across the .NET Standard and .NET 10 targets.
The site build treats documentation warnings,
including unresolved internal links, as failures.

Preview it locally:

```powershell
dotnet docfx serve docs\_site
```

Open the localhost URL printed by DocFX and stop the process when finished.
The generated site uses relative links and can be served under the GitHub project
path or a custom domain without changing the guide links.

## Enable GitHub Pages

Repository maintainers must perform this one-time setup:

1. In repository **Settings > Pages**, choose **GitHub Actions** as the build source.
2. Ensure GitHub Actions and the `github-pages` deployment environment are allowed
   by repository or organization policies.
3. Merge these files into `develop`, or run the **Documentation** workflow manually
   on `develop`.

The workflow builds documentation for relevant pull requests and pushes to `develop`.
Only successful builds on `develop` from a push or manual invocation are deployed.
Pull request builds do not receive Pages write permissions and do not deploy.
The deployment uses GitHub's built-in `GITHUB_TOKEN` and OIDC; no personal access
token or additional repository secret is required.

The expected default address is
`https://renepd.github.io/DotNet.Rene.Utils.Db/` once Pages is enabled and a deployment
succeeds. The deployment job reports the actual URL.
If the documentation release branch changes, update the workflow's branch trigger
and deployment condition together.

## GitHub Wiki versus this site

This implementation publishes a documentation website on GitHub Pages, not the
repository's Wiki tab. The Wiki has a separate Git repository and would need its
own synchronization. Keep `docs` authoritative rather than editing a second copy.

## Documentation coverage

Guides describe the APIs available on the branch that generates the site.
When adding new commands or convenience methods, update the corresponding guides
and examples in the same change; the API reference will be regenerated automatically.
Generating reference pages does not invent descriptions for public members that
do not yet have XML documentation comments.
