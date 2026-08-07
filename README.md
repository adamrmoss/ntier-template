# ntier-template

N-tier enterprise application: Angular client, ASP.NET Core API, CLI, and RabbitMQ worker queue.

```
                    ┌──────────────┐
                    │ Presentation │
                    └──────────────┘
           ┌─────────────────────────────────┐
           │ Entry hosts (Api · Cli · Queue) │
           └─────────────────────────────────┘
        ┌───────────────────────────────────────┐
        │             Application               │
        └───────────────────────────────────────┘
    ┌───────────────────────────────────────────────┐
    │                    Data                       │
    └───────────────────────────────────────────────┘    
┌───────────────────────────────────────────────────────┐
│                       Domain                          │
└───────────────────────────────────────────────────────┘
```

## Solution Layout

| Project | Role |
|---------|------|
| `NTierTemplate/` | Domain — types, message contracts, entity behavior |
| `NTierTemplate.Application/` | Application — use cases, application services, common IoC wiring |
| `NTierTemplate.Data/` | Persistence — EF Core, DAO interfaces and implementations, Identity |
| `NTierTemplate.Api/` | HTTP API host |
| `NTierTemplate.Cli/` | Command-line host |
| `NTierTemplate.Queue/` | RabbitMQ worker host |
| `ntier-template-web/` | Angular client — Material, NgRx |
| `NTierTemplate.Test/` | Unit tests — references Domain, Application, and Data (DAO interfaces for mocks) |

## Architecture

```
  Api · Cli · Queue
        │
   Application      use cases + common IoC wiring
        │
      Data            DAO interfaces and implementations
        │
     Domain            foundation
```

- **Domain** — shared types, message contracts; entity behavior only when it fits comfortably on the entity
- **Data** — EF, DAO interfaces and implementations, Identity; depends on Domain
- **Application** — use-case orchestration and common `ServiceCollection` wiring; depends on Domain and Data
- **Api, Cli, Queue** — entry-point hosts; each bootstraps IoC via Application's wiring; depend on Domain and Application only
- **ntier-template-web** — outside this graph (HTTP client to Api); Angular Material + NgRx
- **CQRS-oriented** — queries synchronous via Application; commands usually enqueued for the worker
- **DAO naming** — `IUserDao` / `UserDao`, not Repository
- **Testing** — unit tests used sparingly; all non-configuration logic lives in Domain and Application; `NTierTemplate.Test/` references Domain, Application, and Data

See `.cursor/rules/` for coding standards and layer boundaries.

## Host Setup

Ubuntu install steps for .NET, MySQL, RabbitMQ, Node.js, nginx, database migrations, and web deploy: **[docs/setup.md](docs/setup.md)**.

Local settings from committed examples:

```bash
./scripts/create-settings.sh
```

## Queue Worker

The queue host (`NTierTemplate.Queue/`) runs as a .NET worker (`Microsoft.Extensions.Hosting`) and consumes messages from RabbitMQ. It bootstraps Application services the same way as the API and CLI.

Ensure local settings exist (includes `RabbitMq` and database connection sections):

```bash
./scripts/create-settings.sh
```

Edit `NTierTemplate.Queue/queuesettings.json` with your MySQL and RabbitMQ credentials.

Run the worker:

```bash
./scripts/queue.sh
```

Or directly:

```bash
dotnet run --project NTierTemplate.Queue/NTierTemplate.Queue.csproj
```

The worker declares a durable queue named in `RabbitMq:QueueName` (default `ntier-template`) and waits for messages. Command handlers deserialize domain contracts and call Application services as they are added.
