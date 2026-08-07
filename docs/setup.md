# Host Setup (Ubuntu)

Infrastructure for running ntier-template on an Ubuntu host: .NET, MySQL, RabbitMQ, Node.js, nginx, and the static web deploy path used by `./scripts/deploy-web.sh`.

## .NET

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

## MySQL

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

Create local settings files from committed examples (`example.*.json` → matching settings file in the same directory):

```bash
./scripts/create-settings.sh
```

Edit the generated files with local values — for example, set the real password in `NTierTemplate.Data/dbsettings.json`.

Apply migrations:

```bash
dotnet ef migrations add InitialCreate \
  --project NTierTemplate.Data/NTierTemplate.Data.csproj \
  --startup-project NTierTemplate.Api/NTierTemplate.Api.csproj

dotnet ef database update \
  --project NTierTemplate.Data/NTierTemplate.Data.csproj \
  --startup-project NTierTemplate.Api/NTierTemplate.Api.csproj
```

## RabbitMQ

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

Create a dedicated broker user (replace `your-password` with a strong password):

```bash
sudo rabbitmqctl add_user ntier your-password
sudo rabbitmqctl set_permissions -p / ntier ".*" ".*" ".*"
```

Verify the broker is running:

```bash
sudo rabbitmqctl status
```

## Node.js

Required to build the Angular client (`./scripts/deploy-web.sh`). Install Node.js 20 or later and npm:

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

Install web dependencies once per clone:

```bash
cd ntier-template-web
npm install
```

## nginx

Install nginx:

```bash
sudo apt update
sudo apt install -y nginx
sudo systemctl enable --now nginx
```

Install the site config from the repo (see `nginx.conf` at the repo root — apex, `www`, and `api` hosts, Cloudflare-friendly HTTP-only origin):

```bash
./scripts/install-nginx.sh
```

Deploy the production web build (from the repo root):

```bash
./scripts/deploy-web.sh
```

Ensure the API is running locally on port `5271` (see `NTierTemplate.Api/Properties/launchSettings.json`). nginx proxies `api.{domain}` to that upstream.

## systemd

Install API and queue unit files from their project directories (`NTierTemplate.Api/ntier-template-api.service`, `NTierTemplate.Queue/ntier-template-queue.service`):

```bash
./scripts/install-systemd.sh
```

Create local settings if needed (`./scripts/create-settings.sh`) and edit `NTierTemplate.Api/appsettings.json` with production values before deploy.

Deploy the API build (from the repo root):

```bash
./scripts/deploy-api.sh
./scripts/restart-api.sh
```

Follow API logs:

```bash
./scripts/log-api.sh
```

The queue worker unit is enabled by the same install step; deploy and restart it after RabbitMQ and `queuesettings.json` are configured.
