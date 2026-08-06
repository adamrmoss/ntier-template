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
| `repository-pattern.mdc` | `**/*.cs` | Repository interfaces and implementations |
| `entity-framework.mdc` | `NTierTemplate.Data/**/*.cs` | EF migrations and DbContext |
| `domain-decomposition.mdc` | `**/*.{cs,ts}` | Avoid `Helper` / `Extensions` type names |
| `documentation-standards.mdc` | `**/*.cs,**/*.ts,**/*.tsx` | Comments and docs |
| `frontend-standards.mdc` | `**/*.ts,**/*.tsx,**/*.scss,**/*.css` | TypeScript, Angular, SCSS |
| `angular-template-simplicity.mdc` | `NTierTemplate.Web/**/*.html,NTierTemplate.Web/**/*.component.ts` | Simple templates; view logic in getters |

## Solution Layout

| Project | Role |
|---------|------|
| `NTierTemplate/` | Domain |
| `NTierTemplate.Data/` | Persistence |
| `NTierTemplate.Api/` | HTTP API |
| `NTierTemplate.Cli/` | CLI |
| `NTierTemplate.Web/` | Angular client |
