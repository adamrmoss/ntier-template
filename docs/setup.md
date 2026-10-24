# Host Setup (Ubuntu)

Two phases: install and harden all host software first, then configure the project (settings, schema, deploy).

## Environment

Install every dependency before touching repo settings, database schema, or deploy scripts.

### .NET

The solution targets **.NET 10** (`net10.0`). EF Core and the JWT packages are on **9.0.17**, with **Pomelo.EntityFrameworkCore.MySql 9.0.0** (stable Pomelo does not yet support EF Core 10).

Add Canonical’s .NET backports PPA, then install the SDK:

```bash
sudo add-apt-repository ppa:dotnet/backports
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
dotnet --list-sdks
dotnet --version
dotnet ef --version
```

`dotnet --list-sdks` should include a **10.0.x** SDK.

For production hosts running published API or queue binaries without the SDK, install the matching ASP.NET Core runtime from the same PPA:

```bash
sudo apt install -y aspnetcore-runtime-10.0
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

The broker's Erlang node name is `rabbit@` plus the short hostname (`hostname -s`). That short name must resolve to an address where the Erlang port mapper (epmd, port 4369) is listening. On a typical host, `/etc/hosts` maps it to `127.0.1.1` or `127.0.0.1`, and the package starts on install.

Check the short name before installing:

```bash
hostname -s
getent hosts "$(hostname -s)"
```

When that name resolves to a local address, install and start RabbitMQ:

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

Clients, including `NTierTemplate.Queue`, connect separately as AMQP to `localhost` port `5672`. The node name is only the broker's internal Erlang identity.

Broker credentials and queue permissions are configured in Project setup after `queuesettings.json` exists.

#### Optional: short hostname Erlang cannot use

Skip this when `hostname -s` already resolves locally.

Some images set the hostname from the public IP, so the short name is a bare number (`9` from `9.74.148.132.host.secureserver.net`). `/etc/hosts` on those images often points that name at the reversed address (`132.148.74.9`), which is not a local interface. Erlang then treats the numeric host as an IP, epmd times out (`epmd error for host 9: timeout`), and `rabbitmq-server` exits 1. `apt` leaves the package unconfigured (`dpkg` error on `rabbitmq-server`).

Leave `/etc/hostname` alone when the provider marks it automatically provisioned. Pin the node name to `rabbit@localhost` instead. `localhost` is already `127.0.0.1` in `/etc/hosts`. AMQP clients still use port `5672` on localhost.

Two files are required, because the server and the CLI load configuration differently:

- The systemd unit runs `/usr/lib/rabbitmq/bin/rabbitmq-server`, which does not export assignments from `/etc/rabbitmq/rabbitmq-env.conf`. `Environment=` on the unit is what the broker process sees.
- `rabbitmqctl` exports every assignment in `rabbitmq-env.conf`. Without `NODENAME` there, the CLI still targets `rabbit@` plus the short hostname and reports that the node is not running.

Write both before the first successful start:

```bash
sudo mkdir -p /etc/rabbitmq /etc/systemd/system/rabbitmq-server.service.d
printf '%s\n' 'NODENAME=rabbit@localhost' | sudo tee /etc/rabbitmq/rabbitmq-env.conf
printf '%s\n' '[Service]' 'Environment=RABBITMQ_NODENAME=rabbit@localhost' | sudo tee /etc/systemd/system/rabbitmq-server.service.d/nodename.conf
sudo systemctl daemon-reload
```

If RabbitMQ is not installed yet, install it after those files exist:

```bash
sudo apt update
sudo apt install -y rabbitmq-server
sudo systemctl enable --now rabbitmq-server
```

If `apt install` already failed while configuring `rabbitmq-server`, finish that configure (do not reinstall):

```bash
sudo systemctl reset-failed rabbitmq-server.service
sudo dpkg --configure rabbitmq-server
```

`sudo rabbitmqctl status` should show node `rabbit@localhost`.

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
