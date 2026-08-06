# ntier-template

Standard template for n-tier enterprise applications: Angular client, ASP.NET Core API, CLI, and RabbitMQ worker queue.

## Solution Layout

| Project | Role |
|---------|------|
| `NTierTemplate/` | Domain — types, message contracts, DAO interfaces, entity behavior |
| `NTierTemplate.Application/` | Application — use cases, application services, common IoC wiring |
| `NTierTemplate.Data/` | Persistence — EF Core, DAO implementations, Identity |
| `NTierTemplate.Api/` | HTTP API host |
| `NTierTemplate.Cli/` | Command-line host |
| `NTierTemplate.Queue/` | RabbitMQ worker host |
| `ntier-template-web/` | Angular client — Material, NgRx; kebab-case folder |
| `NTierTemplate.Test/` | Unit tests — references Domain and Application only |

## Architecture

```
  Api · Cli · Queue
        │
   Application      use cases + common IoC wiring
        │
      Data            DAO implementations
        │
     Domain            foundation
```

- **Domain** — shared types, message contracts, DAO interfaces; entity behavior only when it fits comfortably on the entity
- **Data** — EF, DAO implementations, Identity; depends on Domain
- **Application** — use-case orchestration and common `ServiceCollection` wiring; depends on Domain and Data
- **Api, Cli, Queue** — entry-point hosts; each bootstraps IoC via Application's wiring; depend on Domain and Application only
- **ntier-template-web** — outside this graph (HTTP client to Api); Angular Material + NgRx
- **CQRS-oriented** — queries synchronous via Application; commands usually enqueued for the worker
- **DAO naming** — `IUserDao` / `UserDao`, not Repository
- **Testing** — unit tests used sparingly; all non-configuration logic lives in Domain and Application; `NTierTemplate.Test/` references those two projects only

## Template Naming

| Token | Example (`dotnet new ntier -n DietzMoss`) |
|-------|-------------------------------------------|
| `NTierTemplate` | `DietzMoss` |
| `ntier-template` | `dietz-moss` |
| `ntier-template-web` | `dietz-moss-web` |
| `ntier` (Angular prefix) | `dm` |

Override Angular prefix: `--webPrefixOverride <prefix>`

See `.cursor/rules/` for coding standards and layer boundaries.
