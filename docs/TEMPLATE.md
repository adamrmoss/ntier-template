# Template Authoring

This file is **template meta-commentary** for people who maintain and publish the template. It is excluded from `dotnet new` output — instantiated solutions never contain this file or any reference to being a template.

Everything else in the repo (README, cursor rules, source code) is written as if it belongs to a normal application. Token placeholders are replaced on instantiation.

## Conventions

- Do not add meta-commentary about templates anywhere except this file
- Use token placeholders (`NTierTemplate`, `ntier-template`, `ntier`, `ntier-template.com`) in files that ship to instantiated solutions
- Repo-root `.editorconfig` applies to the whole solution; do not keep scaffold copies under `ntier-template-web/`
- Cursor rules live in `.cursor/rules/` and ship with the template
- NuGet authoring files (`DietzMoss.NTierTemplate.csproj`) must stay out of instantiated output — see [Excluded from Instantiation](#excluded-from-instantiation)

## Install the Template

```bash
# Local development — from this repo root
dotnet new install .
dotnet new uninstall .

# After publishing to NuGet.org
dotnet new install DietzMoss.NTierTemplate
dotnet new uninstall DietzMoss.NTierTemplate
```

## Create an Instance

When testing, pass **every** token value explicitly — do not rely on defaults for prefix or domain.

```bash
dotnet new ntier \
  -n DietzMoss \
  -o /tmp/dietz-moss \
  --webPrefixOverride dm \
  --domainNameOverride dietz-moss.com
```

| Placeholder | Flag | Example | Used for |
|-------------|------|---------|----------|
| `NTierTemplate` | `-n` / `--name` | `DietzMoss` | PascalCase projects, namespaces, types |
| *(output path)* | `-o` / `--output` | `/tmp/dietz-moss` | Where files are written |
| `ntier` | `--webPrefixOverride` / `-w` | `dm` | Angular component selector prefix |
| `ntier-template.com` | `--domainNameOverride` / `-do` | `dietz-moss.com` | Production domain (nginx, api-config) |
| `ntier-template` | derived from `-n` | `dietz-moss` | Solution file, kebab-case paths, service names |
| `ntier-template-web` | derived from `-n` | `dietz-moss-web` | Angular project folder |

Verify the output:

```bash
ls /tmp/dietz-moss
grep -r "dietz-moss.com" /tmp/dietz-moss/nginx.conf /tmp/dietz-moss/dietz-moss-web/src/app/site/api-config.ts
grep "prefix" /tmp/dietz-moss/dietz-moss-web/angular.json
```

Symbol definitions live in `.template.config/template.json`. `camelName` / `nTierTemplate` is defined but unused — candidate for removal.

## Excluded from Instantiation

These paths are not copied to output (see `sources` in `template.json`):

| Path | Reason |
|------|--------|
| `DietzMoss.NTierTemplate.csproj` | NuGet pack project — authoring only |
| `docs/TEMPLATE.md` | Template authoring only (this file) |

Default engine excludes also apply: `bin/`, `obj/`, `.template.config/`, `**/*.lock.json`, etc.

## NuGet Publishing

The pack project is **not** in `ntier-template.sln` — adding it would leave a broken project reference in instantiated solutions even if the file itself were excluded.

### First-time NuGet.org setup

1. Sign in at [nuget.org](https://www.nuget.org/) with your Microsoft account
2. Complete your profile
3. **Account → API Keys** — create a key with push scope for `DietzMoss.NTierTemplate`

### Pack, test, and publish

```bash
dotnet pack DietzMoss.NTierTemplate.csproj -c Release

dotnet new install ./bin/Release/DietzMoss.NTierTemplate.1.0.0.nupkg
# Instantiate using the command in "Create an Instance" above, then verify kay-fabe:
#   no DietzMoss.NTierTemplate.csproj, no docs/TEMPLATE.md, no .template.config/
dotnet new uninstall DietzMoss.NTierTemplate

# Bump PackageVersion in DietzMoss.NTierTemplate.csproj before each release
dotnet nuget push ./bin/Release/DietzMoss.NTierTemplate.1.0.0.nupkg \
  --api-key YOUR_API_KEY \
  --source https://api.nuget.org/v3/index.json
```
