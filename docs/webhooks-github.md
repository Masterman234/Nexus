# GitHub Webhooks — Setup Guide

This document describes how to wire a GitHub repository's webhook deliveries into
Nexus so push / pull_request events show up as messages in a configured channel.

The flow:

```
GitHub  ──HTTPS POST──►  ngrok (public URL)  ──►  Nexus.Api /api/v1/webhooks/github
                                                       │
                                                       ▼
                                            HMAC verify → DB → RabbitMQ
                                                       │
                                                       ▼
                                            GithubWebhookConsumer
                                                       │
                                            persist Message + SignalR broadcast
                                                       │
                                                       ▼
                                                    Frontend
```

## 1. Run Nexus locally

Start the backing services and the API:

```powershell
docker compose up -d            # postgres, redis, rabbitmq
dotnet run --project backend/src/Nexus.Api
```

Confirm it's healthy:

```powershell
curl http://localhost:5001/health
```

On first start, `DatabaseInitializer` runs migrations and seeds the `github-bot`
user (Guid `11111111-1111-1111-1111-111111111111`). You should see:

```
>>> [SEED] Created system user 'github-bot' (11111111-...).
```

## 2. Expose your local API with ngrok

GitHub cannot reach `localhost`. ngrok publishes a public HTTPS URL that tunnels
to your machine.

### Install

```powershell
winget install ngrok.ngrok
# or download from https://ngrok.com/download if winget ships an outdated build
```

`ngrok version` should report 3.30 or newer. Older v3 builds (e.g. 3.3.1) ship
with a config-schema that the current dashboard token format rejects.

### One-time auth

Sign up at https://dashboard.ngrok.com (free tier is fine), copy your authtoken,
then:

```powershell
ngrok config add-authtoken <your-token>
```

The authtoken belongs under `agent:` in `~/AppData/Local/ngrok/ngrok.yml`. If
you see `field authtoken not found in type config.v3yamlConfig`, delete the
file and re-run `ngrok config add-authtoken` so the v3 schema is written fresh.

### Reserve a free dev domain (recommended)

Without a reserved domain, the URL changes on every restart and you'd have to
edit GitHub's webhook each time. The free tier gives you one stable subdomain:

1. https://dashboard.ngrok.com/domains → your dev domain is already listed
2. Copy the value (e.g. `widish-nonadmissibly-cheyenne.ngrok-free.dev`)

### Tunnel port 5001 (HTTP)

```powershell
ngrok http --url=https://<your-domain>.ngrok-free.dev 5001
```

(The `--url` flag replaces the deprecated `--domain`.)

ngrok prints something like:

```
Forwarding   https://<your-domain>.ngrok-free.dev -> http://localhost:5001
```

That HTTPS URL is what GitHub will POST to. **Keep this terminal open** —
closing it kills the tunnel.

> Why HTTP and not HTTPS to localhost? Tunneling HTTPS-to-HTTPS forces ngrok
> to bridge two TLS sessions and some intermediate proxies drop request headers
> like `X-GitHub-Event` in transit. HTTP through the tunnel + `UseForwardedHeaders`
> in the API + HMAC signature verification gives the same security guarantees
> with none of the breakage.

### Verify the tunnel

```
https://<your-domain>.ngrok-free.dev/health
```

Should return the same JSON as `http://localhost:5001/health`. The ngrok
inspector at http://127.0.0.1:4040 will also show the request — keep that tab
open for debugging.

## 3. Generate a webhook secret

Pick a long random string. Anything ≥ 32 bytes of entropy is fine:

```powershell
[Convert]::ToBase64String((1..32 | %{ [byte](Get-Random -Max 256) }))
```

Save the output. You'll paste the same value into **two** places:

1. GitHub's webhook config (next step)
2. Nexus's configuration (step 5)

## 4. Configure the GitHub webhook

In your GitHub repo:

1. **Settings** → **Webhooks** → **Add webhook**
2. **Payload URL**:
   `https://<your-domain>.ngrok-free.dev/api/v1/webhooks/github`
   (note the `v1` — the route is versioned via `[ApiVersion("1.0")]`)
3. **Content type**: `application/json` (required — the controller uses
   `[Consumes("application/json")]`)
4. **Secret**: paste the string from step 3
5. **SSL verification**: Enable (ngrok provides a valid cert)
6. **Which events?** Select individual events → check **Pushes** and
   **Pull requests** (or "Send me everything" if you want to see all event
   types as raw notifications).
7. **Active**: ✅
8. Click **Add webhook**

GitHub immediately sends a `ping` event. Scroll to **Recent Deliveries** to
see the result. A green check = your endpoint responded 2xx.

## 5. Tell Nexus the secret + target channel

Nexus reads configuration from `appsettings.json`, then `.env` (via DotNetEnv),
then environment variables. For local dev, `.env` is the right place — your
`.gitignore` already excludes it.

```powershell
# from repo root
Copy-Item .env.example .env
notepad .env
```

Fill in:

```
Webhook__GithubSecret=<paste-the-same-string-from-step-3>
Webhook__GithubTargetChannelId=<channel-id-from-DB>
```

To find the target channel id:

```powershell
docker exec nexus-db psql -U nexus -d nexus -c 'SELECT "Id", "Name", "WorkspaceId" FROM nexus.channels;'
```

Pick the channel you want the bot to post into (typically the `general` channel
of your workspace). Restart the API after editing `.env`.

> Why an env var and not user-secrets? `.env` is a single source of config the
> frontend, Docker Compose, and the API can all read. ASP.NET's environment-
> variable configuration source automatically maps `Webhook__GithubSecret` to
> `configuration["Webhook:GithubSecret"]` — the `__` is the standard convention
> for nested keys.
>
> In production, set the same vars via your secrets manager or platform env vars.
>
> If `Webhook__GithubSecret` is empty, the API logs a loud `WARN` and accepts
> every request unsigned — useful for very first-time debugging, but never run
> that way against the public internet.
>
> If `Webhook__GithubTargetChannelId` is empty, the consumer falls back to the
> oldest channel named `general` and logs a `WARN`. Fine for first-run, but
> set it explicitly once you have more than one workspace.

## 6. Test end-to-end

In the GitHub webhook page, click **Recent Deliveries** → pick any delivery →
**Redeliver**. Then watch the Nexus API console:

```
>>> [API] Received Webhook! Event: push Delivery: <guid>
>>> [BOT] Consumer Start: push
>>> [BOT] Broadcasting to UI channel=<guid> id=<guid>
```

…and the message should appear in the target channel in the frontend. If you
refresh the browser it stays — it's a real `Message` row owned by the
`github-bot` user.

Real `git push` to the repo fires the same pipeline. You don't have to redeliver
manually after the first sanity check.

## Diagnosing failures

Walk the pipeline in order; the first step that fails is the problem.

| Symptom in **Recent Deliveries** | Likely cause | Fix |
|---|---|---|
| 404 | URL has `/api/webhooks/github` (unversioned) or wrong path | Use the versioned `/api/v1/webhooks/github` |
| Timeout / connection refused | ngrok tunnel down, wrong port, or API not running | Restart ngrok / `dotnet run`; check the ngrok inspector at 127.0.0.1:4040 |
| 307 loop (browser/test only — GitHub follows the redirect) | `UseHttpsRedirection` firing for the webhook path | Ensure `UseForwardedHeaders` is registered before `UseHttpsRedirection` and the path exemption covers `/api/webhooks/*` |
| 401 `Invalid signature` | Secret in GitHub ≠ secret in `.env` | Re-paste the same string in both, restart the API |
| 401 `Missing signature` | GitHub config has no secret but Nexus does | Set the secret in GitHub |
| 2xx but nothing in chat | Channel id misconfigured / no `#general` in DB / RabbitMQ not consuming | Look for `[BOT] ERROR: Webhook:GithubTargetChannelId is set to ... but no channel with that id exists` in API console, and check RabbitMQ UI at http://localhost:15672 |
| 2xx + console shows broadcast but UI empty | SignalR not connected, or user is viewing a different channel | DevTools → Network → WS or polling → look for `chatHub` frames; check you're on the channel matching `Webhook__GithubTargetChannelId` |

## What the controller actually checks

```
                       ┌────────────────────────────────┐
POST /api/v1/webhooks/ │ 1. Read raw body (NOT parsed)  │
github                 │ 2. Read X-Hub-Signature-256    │
                       │ 3. Compute HMAC-SHA256(body,   │
                       │    Webhook:GithubSecret)       │
                       │ 4. Constant-time compare       │
                       │ 5. 401 if mismatch             │
                       │ 6. ExternalEvent row + publish │
                       │    GithubWebhookReceived       │
                       └────────────────────────────────┘
```

The raw body is critical — if you parse the JSON first and re-serialize, the
byte sequence differs and the HMAC will never match. `WebhooksController` calls
`Request.EnableBuffering()` and reads the stream directly to preserve bytes.

## Going to production

- Use a reserved ngrok domain (free tier supports one) or replace ngrok with
  your real public endpoint (Cloudflare Tunnel, an ALB, …).
- Set `Webhook__GithubSecret` and `Webhook__GithubTargetChannelId` via your
  secrets/config manager — never commit them.
- Keep the `UseHttpsRedirection` exemption only for `/api/webhooks/*`. The rest
  of the API still forces HTTPS.
- Consider rate-limiting the webhook route specifically — GitHub will not retry
  faster than its own backoff, but a misconfigured third party could.
- Verify `UseForwardedHeaders` is configured to trust *only* your real proxy's
  CIDR in production; the current dev config clears `KnownNetworks`/`KnownProxies`
  for convenience.
