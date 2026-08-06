# Cursor Rules Index

Project rules live in `.cursor/rules/`. Rules with `alwaysApply: true` are included in every session.

## Always-Applied Rules

| File | Description |
|------|-------------|
| `agent-decision-guardrails.mdc` | Minimal fix first, pattern matching, constraint checking |
| `classic-oo-ddd.mdc` | Classic OO, GoF, and DDD design preferences |
| `error-ownership-and-recovery.mdc` | Own generated-code failures and recover without burdening the user |
| `general-guidelines.mdc` | Core guidelines — answering, paths, changes, solution structure |
| `linq-to-ef.mdc` | No in-memory collections or untranslatable methods in `IQueryable` |
| `shell-commands.mdc` | Propose shell commands; do not run them |

## File-Specific Rules

| File | Globs | Description |
|------|-------|-------------|
| `csharp-standards.mdc` | `**/*.cs` | C# coding standards |
| `one-type-per-file.mdc` | `**/*.cs` | One nontrivial type per source file |
| `entity-architecture.mdc` | `**/*.cs` | Domain vs EF entity split |
| `application-layer.mdc` | Application, Api, Cli, Queue | Use cases, IoC wiring, CQRS |
| `dao-pattern.mdc` | `**/*.cs` | DAO interfaces and implementations |
| `entity-framework.mdc` | `NTierTemplate.Data/**/*.cs` | EF migrations and DbContext |
| `queue-layer.mdc` | `NTierTemplate.Queue/**/*.cs` | RabbitMQ worker host |
| `testing.mdc` | `NTierTemplate.Test/**/*.cs` | Unit test scope and conventions |
| `domain-decomposition.mdc` | `**/*.{cs,ts}` | Avoid `Helper` / `Extensions` type names |
| `documentation-standards.mdc` | `**/*.cs,**/*.ts,**/*.tsx` | Comments and docs |
| `frontend-standards.mdc` | `ntier-template-web/**` | Angular, Material, NgRx, SCSS |
| `angular-template-simplicity.mdc` | `ntier-template-web/**/*.html,ntier-template-web/**/*.component.ts` | Simple templates; view logic in getters |

## Solution Layout

| Project | Role |
|---------|------|
| `NTierTemplate/` | Domain — types, message contracts, DAO interfaces, entity behavior |
| `NTierTemplate.Application/` | Application — use cases, application services, common IoC wiring |
| `NTierTemplate.Data/` | Persistence — EF, DAO implementations, Identity |
| `NTierTemplate.Api/` | HTTP API host |
| `NTierTemplate.Cli/` | CLI host |
| `NTierTemplate.Queue/` | RabbitMQ worker host |
| `ntier-template-web/` | Angular client (Material, NgRx) |
| `NTierTemplate.Test/` | Unit tests (Domain, Application) |

## Layer Dependencies

```
  Api · Cli · Queue          entry-point hosts (IoC bootstrap per host)
            │
       Application           use cases + common ServiceCollection wiring
            │
          Data                 DAO implementations, EF, Identity
            │
         Domain                 types, message contracts, entity behavior

Web ──HTTP──▶ Api
```

| Project | Project references |
|---------|-------------------|
| `NTierTemplate/` | (none) |
| `NTierTemplate.Data/` | Domain |
| `NTierTemplate.Application/` | Domain, Data |
| `NTierTemplate.Api/` | Domain, Application |
| `NTierTemplate.Cli/` | Domain, Application |
| `NTierTemplate.Queue/` | Domain, Application |
| `NTierTemplate.Test/` | Domain, Application |
| `ntier-template-web/` | *(HTTP only)* |

Entry points must not reference **Data** directly. Application owns common IoC wiring. `NTierTemplate.Test/` sits outside the runtime graph — references Domain and Application only; see `testing.mdc`.
