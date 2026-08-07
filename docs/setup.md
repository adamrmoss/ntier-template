# Host Setup (Ubuntu)

Two phases: install and harden all host software first, then configure the project (settings, schema, deploy).

## Environment

Install every dependency before touching repo settings, database schema, or deploy scripts.

### .NET

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

### MySQL

Install MySQL Server:

```bash
sudo apt update
sudo apt install mysql-server
sudo systemctl enable --now mysql
```

Run the secure installation wizard (set a root password, remove anonymous users, disable remote root login):

```bash
sudo mysql_secure_installation
```

Ensure `root` can connect only from localhost:

```bash
sudo mysql <<'SQL'
DELETE FROM mysql.user
WHERE User = 'root'
  AND Host NOT IN ('localhost', '127.0.0.1', '::1');
FLUSH PRIVILEGES;
SQL
```

Do **not** create the application database or `ntier` user yet — that comes after local settings files exist (see Project setup).

### RabbitMQ

Install and start RabbitMQ:

```bash
sudo apt update
sudo apt install -y rabbitmq-server
sudo systemctl enable --now rabbitmq-server
```

Optional: enable the management UI (http://localhost:15672):

```bash
sudo rabbitmq-plugins enable rabbitmq_management
```

Verify the broker is running:

```bash
sudo rabbitmqctl status
```

Broker credentials and queue permissions are configured in Project setup after `queuesettings.json` exists.

### Node.js

Required to build the Angular client. Install Node.js 20 or later:

```bash
sudo apt update
sudo apt install -y nodejs npm
node --version
npm --version
```

If the distro packages are too old for Angular 21, use the NodeSource 22.x setup instead:

```bash
curl -fsSL https://deb.nodesource.com/setup_22.x | sudo -E bash -
sudo apt install -y nodejs
```

### nginx

Install nginx only (site config is installed in Project setup):

```bash
sudo apt update
sudo apt install -y nginx
sudo systemctl enable --now nginx
```

---

## Project setup

Complete Environment first. Then configure settings, schema, and deploy artifacts.

### Local settings

Create settings files from committed examples (`example.*.json` → matching settings file in the same directory):

```bash
./scripts/create-settings.sh
```

Edit the generated files with local values — at minimum:

| File | Purpose |
|------|---------|
| `NTierTemplate.Data/dbsettings.json` | MySQL connection for the `ntier` user |
| `NTierTemplate.Api/appsettings.json` | API production settings, JWT, SMTP |
| `NTierTemplate.Cli/clisettings.json` | CLI database and app URLs |
| `NTierTemplate.Queue/queuesettings.json` | Queue worker database and RabbitMQ |

### MySQL database and user

Create the application database and dedicated user (replace `your-password` with the same password set in `dbsettings.json`):

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

Apply migrations:

```bash
dotnet ef database update \
  --project NTierTemplate.Data/NTierTemplate.Data.csproj \
  --startup-project NTierTemplate.Api/NTierTemplate.Api.csproj
```

If you are authoring a new migration (not needed on first clone when `InitialCreate` already exists):

```bash
dotnet ef migrations add <MigrationName> \
  --project NTierTemplate.Data/NTierTemplate.Data.csproj \
  --startup-project NTierTemplate.Api/NTierTemplate.Api.csproj
```

### RabbitMQ broker user

Create a dedicated broker user (replace `your-password` with the same password in `queuesettings.json`):

```bash
sudo rabbitmqctl add_user ntier your-password
sudo rabbitmqctl set_permissions -p / ntier ".*" ".*" ".*"
```

### Web client

Install npm dependencies once per clone:

```bash
cd ntier-template-web
npm install
cd ..
```

Install the nginx site config from the repo (`nginx.conf` — apex, `www`, and `api` hosts, Cloudflare-friendly HTTP-only origin):

```bash
./scripts/install-nginx.sh
```

Deploy the production web build:

```bash
./scripts/deploy-web.sh
```

nginx proxies `api.{domain}` to the API upstream on port `5271` (see `NTierTemplate.Api/Properties/launchSettings.json`).

### systemd

Install API and queue unit files from their project directories (`NTierTemplate.Api/ntier-template-api.service`, `NTierTemplate.Queue/ntier-template-queue.service`):

```bash
./scripts/install-systemd.sh
```

Deploy and start the API:

```bash
./scripts/deploy-api.sh
./scripts/restart-api.sh
```

Follow API logs:

```bash
./scripts/log-api.sh
```

Deploy the queue worker to `/opt/ntier-template/queue`, then restart `ntier-template-queue` once RabbitMQ and `queuesettings.json` are configured.
