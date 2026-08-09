# Template Authoring

This file is **template meta-commentary**. It is excluded from `dotnet new` output — instantiated solutions never contain this file or any reference to being a template.

All other files in the repo (README, cursor rules, source code) are written as if they belong to a normal application. Token placeholders are replaced on instantiation.

## Install and Test

```bash
# From this repo root — install locally for development
dotnet new install .

# Create a test instance
dotnet new ntier -n DietzMoss -o /tmp/dietz-moss-test

# Prompt for optional parameters (domain, web prefix, etc.)
dotnet new ntier -n DietzMoss -o /tmp/dietz-moss-test --interactive

# Uninstall when done
dotnet new uninstall .
```

## Token Replacement

| Source token | Example (`-n DietzMoss`) | Used for |
|--------------|--------------------------|----------|
| `NTierTemplate` | `DietzMoss` | PascalCase backend projects, namespaces, types |
| `ntier-template` | `dietz-moss` | Solution name, kebab-case paths |
| `ntier-template-web` | `dietz-moss-web` | Angular project folder |
| `ntier` | `dm` | Angular component selector prefix |
| `ntier-template.com` | `dietz-moss.com` | Production domain (nginx, api-config); default `{kebab-name}.com` |

Override Angular prefix: `--webPrefixOverride <prefix>`

Override production domain: `--domainNameOverride example.com`

### Symbols in `.template.config/template.json`

| Symbol | Purpose |
|--------|---------|
| `sourceName` | Built-in — replaces `NTierTemplate` in content and file names |
| `kebabName` | Derived kebab-case of `-n`; replaces `ntier-template` |
| `kebabWebFolder` | Renames `ntier-template-web/` folder to `{kebab-name}-web` |
| `webPrefix` | Angular prefix; defaults to lowercase initials from `-n` |
| `webPrefixOverride` | Optional parameter to override prefix |
| `domainName` | Production domain; defaults to `{kebab-name}.com`, overridable via `domainNameOverride` |

`camelName` / `nTierTemplate` is defined but unused — candidate for removal.

## Excluded from Instantiation

These paths are not copied to output (see `sources` in `template.json`):

| Path | Reason |
|------|--------|
| `docs/TEMPLATE.md` | Template authoring only (this file) |

Default engine excludes also apply: `bin/`, `obj/`, `.template.config/`, `**/*.lock.json`, etc.

## Web Project Setup

Angular is pinned to **21.x** stable (not `@latest` — that resolves to 22, which lacks stable NgRx).

```bash
export NG_CLI_ANALYTICS=false

npx -y @angular/cli@21 new ntier-template-web \
  --directory ntier-template-web \
  --routing --style=scss --standalone \
  --skip-tests --skip-git --package-manager=npm \
  --prefix=ntier --ssr=false --defaults

cd ntier-template-web
npx ng add @angular/material@21 --theme=azure-blue --typography=true --animations=browser --skip-confirmation
npx ng add @ngrx/store@21.1.1 --skip-confirmation
npx ng add @ngrx/effects@21.1.1 --skip-confirmation
npx ng add @ngrx/store-devtools@21.1.1 --skip-confirmation
```

## Conventions for Template Authors

- Do not add meta-commentary about templates anywhere except this file
- Use token placeholders (`NTierTemplate`, `ntier-template`, `ntier`) in files that ship to instantiated solutions
- Repo-root `.editorconfig` applies to the whole solution; do not keep scaffold copies under `ntier-template-web/`
- Cursor rules live in `.cursor/rules/` and ship with the template
