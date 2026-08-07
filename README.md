# ntier-template

N-tier enterprise application: Angular client, ASP.NET Core API, CLI, and RabbitMQ worker queue.

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

## .NET Setup (Ubuntu)

Add the Microsoft package feed and install the .NET 10 SDK (includes the `dotnet` CLI):

```bash
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

sudo apt update
sudo apt install -y dotnet-sdk-10.0
```

Install the EF Core CLI as a global tool:

```bash
dotnet tool install --global dotnet-ef
```

Ensure global tools are on your `PATH` (add to `~/.bashrc` if needed):

```bash
export PATH="$PATH:$HOME/.dotnet/tools"
```

Verify:

```bash
dotnet --version
dotnet ef --version
```

## Database Setup (Ubuntu)

Install MySQL Server:

```bash
sudo apt update
sudo apt install mysql-server
sudo systemctl enable --now mysql
```

Secure the installation and set a root password when prompted:

```bash
sudo mysql_secure_installation
```

Create the application database and dedicated user (replace `your-password` with a strong password):

```bash
sudo mysql <<'SQL'
CREATE DATABASE IF NOT EXISTS `ntier-template`
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

CREATE USER IF NOT EXISTS 'ntier'@'localhost' IDENTIFIED BY 'your-password';
GRANT ALL PRIVILEGES ON `ntier-template`.* TO 'ntier'@'localhost';
FLUSH PRIVILEGES;
SQL
```

Create local database settings from the committed example file:

```bash
./scripts/create-dbsettings.sh
```

Edit `NTierTemplate.Data/dbsettings.json` and replace `CHANGE_ME` with the `ntier` user password.

After the API project exists, generate the initial migration:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```
