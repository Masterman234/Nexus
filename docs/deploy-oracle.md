# Deploying Nexus to an Oracle Cloud Always Free VM

This is the runbook for the public portfolio demo. The whole stack — Postgres,
Redis, RabbitMQ, the API, the web SPA (nginx), and Caddy (TLS edge) — runs as a
single Docker Compose stack (`docker-compose.prod.yml`) on one always-free VM,
reachable over HTTPS at a free DuckDNS subdomain. Cost: **$0**.

```
Internet ──443──▶ Caddy ──▶ nexus-web (nginx) ──▶ nexus-api ──▶ Postgres / Redis / RabbitMQ
            (Let's Encrypt)        (SPA + /api, /chatHub proxy)
```

Two ways to ship images to the VM:
- **Manual / local-build:** the VM builds the images itself (`up --build`).
- **CD (recommended):** GitHub Actions (`.github/workflows/cd.yml`) builds and
  pushes images to GHCR on every push to `main`, then SSHes in to pull + restart.
  The bulk of this doc sets up the VM so that CD just works.

---

## 1. Provision the VM

1. Oracle Cloud → **Compute → Instances → Create**.
2. Shape: an **Always Free** eligible shape (e.g. `VM.Standard.A1.Flex`, 1–4
   OCPU / 6–24 GB Ampere ARM, or `VM.Standard.E2.1.Micro` x86). ARM has far more
   free headroom; our images are multi-arch-friendly (.NET 8 + node alpine).
3. Image: **Ubuntu 22.04 LTS**.
4. Add your SSH public key (you'll use this key for both manual access and CD).
5. After creation, note the **public IP**.

### Open the firewall

Oracle blocks everything but SSH by default — at **both** layers:

1. **VCN Security List / NSG** (Networking → your VCN → Security Lists): add
   ingress rules allowing `0.0.0.0/0` on TCP **80** and **443**.
2. **Host firewall** (Ubuntu ships `iptables` rules):
   ```bash
   sudo iptables -I INPUT 6 -m state --state NEW -p tcp --dport 80 -j ACCEPT
   sudo iptables -I INPUT 6 -m state --state NEW -p tcp --dport 443 -j ACCEPT
   sudo netfilter-persistent save
   ```

---

## 2. Install Docker

```bash
sudo apt-get update
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker $USER
# log out and back in so the group takes effect
docker compose version   # verify the v2 plugin is present
```

---

## 3. Point DuckDNS at the VM

1. Sign in at <https://www.duckdns.org> (free, GitHub/Google login).
2. Create a subdomain, e.g. `nexus-demo`, and set its IP to the VM's public IP.
3. The full domain `nexus-demo.duckdns.org` is what goes in `NEXUS_DOMAIN` below.
   Caddy will provision a Let's Encrypt cert for it automatically — this only
   works once ports 80/443 are reachable (step 1) and DNS resolves to the VM.

---

## 4. Lay down the app directory

CD copies `docker-compose.prod.yml` and `Caddyfile` to `~/nexus` on the host, but
the **`.env` (secrets) must already exist there** — it is never committed or shipped
by CI. Create the directory and env file once:

```bash
mkdir -p ~/nexus && cd ~/nexus
# Grab the example to fill in (or scp it up from your machine):
curl -fsSL https://raw.githubusercontent.com/Masterman234/Nexus/main/.env.prod.example -o .env
```

Edit `~/nexus/.env` and fill in real values. Generate strong secrets on the host:

```bash
openssl rand -base64 48   # use for JWT_SECRET, JWT_REFRESH_PEPPER, and DB/MQ passwords
```

Key settings (see `.env.prod.example` for the full list):

| Variable | Notes |
|---|---|
| `POSTGRES_PASSWORD` | strong random |
| `RABBITMQ_USER` / `RABBITMQ_PASS` | **not** guest/guest |
| `JWT_SECRET` | ≥ 32 bytes |
| `JWT_REFRESH_PEPPER` | random |
| `GEMINI_API_KEY` | for AI standup summaries (optional) |
| `SEED_DEMO` | `true` for the public demo (seeds guest user + demo workspace) |
| `NEXUS_DOMAIN` | `nexus-demo.duckdns.org` |

---

## 5. First deploy

### Option A — CD (recommended)

Add these repo secrets (**Settings → Secrets and variables → Actions**):

| Secret | Value |
|---|---|
| `SSH_HOST` | VM public IP |
| `SSH_USER` | `ubuntu` (or your login user) |
| `SSH_KEY` | the **private** key matching the public key on the VM (full PEM) |

Then push to `main` (or run the **CD** workflow manually via *Actions →
workflow_dispatch*). It will build/push `ghcr.io/<owner>/nexus-api` and
`nexus-web`, scp the compose file + Caddyfile to `~/nexus`, then `pull` + `up -d`.

> **GHCR visibility:** the first push creates the packages as **private**. The VM
> can't pull private images with the ephemeral `GITHUB_TOKEN`. Either make the two
> packages **public** (GHCR → package → Package settings → Change visibility), or
> log in once on the host with a read:packages PAT:
> ```bash
> echo "$GHCR_PAT" | docker login ghcr.io -u <github-username> --password-stdin
> ```

### Option B — manual, VM builds locally

```bash
cd ~/nexus
# need the source for a local build; either git clone the repo or scp it up.
docker compose -f docker-compose.prod.yml up -d --build
```

---

## 6. Verify

```bash
cd ~/nexus
docker compose -f docker-compose.prod.yml ps          # all services healthy
docker compose -f docker-compose.prod.yml logs -f nexus-api   # watch startup + migrations
curl -fsS https://nexus-demo.duckdns.org/health       # API health through the edge
```

Then open `https://nexus-demo.duckdns.org` and click **Try the Demo** — the
one-click guest login should drop you into the seeded demo workspace.

> **Schema note:** the API runs `DatabaseInitializer.InitializeAsync` at startup,
> which applies EF migrations via `MigrateAsync()` against Postgres (the tests use
> SQLite `EnsureCreated()`, which does **not** catch migration drift). If the API
> logs a `42703: column ... does not exist` style error on a fresh DB, a migration
> is missing/incomplete — fix the migration, don't hand-patch the DB. See the
> migration-drift notes before touching the schema.

---

## 7. Operations

```bash
cd ~/nexus

# Update to the latest pushed images (CD does this automatically):
docker compose -f docker-compose.prod.yml pull nexus-api nexus-web
docker compose -f docker-compose.prod.yml up -d

# Roll back to a specific commit's images:
API_IMAGE=ghcr.io/<owner>/nexus-api:<sha> \
WEB_IMAGE=ghcr.io/<owner>/nexus-web:<sha> \
  docker compose -f docker-compose.prod.yml up -d nexus-api nexus-web

# Back up the database:
docker compose -f docker-compose.prod.yml exec nexus-db \
  pg_dump -U nexus nexus > backup-$(date +%F).sql

# Tail logs / restart a single service:
docker compose -f docker-compose.prod.yml logs -f caddy
docker compose -f docker-compose.prod.yml restart nexus-api
```

### Troubleshooting

| Symptom | Likely cause |
|---|---|
| TLS cert never issues | ports 80/443 not open at VCN **and** host firewall, or DNS not pointing at the VM yet |
| `docker pull` denied on the VM | GHCR packages still private — make public or `docker login` with a PAT (§5) |
| API exits on boot, secret error | a required `${VAR:?}` is missing from `~/nexus/.env` |
| `column "X" does not exist` | EF migration drift on fresh Postgres (see schema note, §6) |
| 502 from nginx | `nexus-api` not healthy yet — check its logs |
