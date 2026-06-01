# GitHub Webhooks — Setup Guide

This document describes how to wire a GitHub repository's webhook deliveries into
Nexus so push / pull_request events show up as messages in the `#general` channel.

The flow:

```
GitHub  ──HTTPS POST──►  ngrok (public URL)  ──►  Nexus.Api /api/webhooks/github
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
# or: choco install ngrok
```

### One-time auth

Sign up at https://dashboard.ngrok.com (free tier is fine), copy your authtoken,
then:

```powershell
ngrok config add-authtoken <your-token>
```

### Tunnel port 5001 (HTTP)

```powershell
ngrok http 5001
```

ngrok prints something like:

```
Forwarding   https://abc123-def456.ngrok-free.app -> http://localhost:5001
```

That `https://abc123...ngrok-free.app` URL is what GitHub will POST to. **Keep
this terminal open** — closing it kills the tunnel and the URL changes on every
restart unless you have a reserved domain.

> Note: the API is configured to skip `UseHttpsRedirection` on `/api/webhooks/*`,
> so it's safe to expose the HTTP port — GitHub talks HTTPS to ngrok, ngrok
> forwards HTTP to the API. The HMAC signature still guarantees integrity.

## 3. Generate a webhook secret

Pick a long random string. Anything ≥ 32 bytes of entropy is fine:

```powershell
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
```

Save the output. You'll paste the same value into **two** places:

1. GitHub's webhook config (next step)
2. Nexus's configuration (step 5)

## 4. Configure the GitHub webhook

In your GitHub repo:

1. **Settings** → **Webhooks** → **Add webhook**
2. **Payload URL**: `https://abc123-def456.ngrok-free.app/api/webhooks/github`
   (use your ngrok URL — make sure the path is **exactly** `/api/webhooks/github`,
   no `/v1/` segment)
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

## 5. Tell Nexus the secret

Use .NET user-secrets so the value isn't checked into the repo:

```powershell
cd backend/src/Nexus.Api
dotnet user-secrets init      # first time only
dotnet user-secrets set "Webhook:GithubSecret" "<paste-the-same-string>"
```

Restart the API. From now on, any request to `/api/webhooks/github` without
a valid `X-Hub-Signature-256` header is rejected with `401`.

> In production, set the same value via an environment variable instead:
> `Webhook__GithubSecret=...` (double underscore is the .NET config convention
> for nested keys).
>
> If you leave the secret empty, the API logs a loud `WARN` and accepts every
> request unsigned — useful for very first-time debugging, but never run that
> way against the public internet.

## 6. Test end-to-end

In the GitHub webhook page, click **Recent Deliveries** → pick any delivery →
**Redeliver**. Then watch the Nexus API console:

```
>>> [API] Received Webhook! Event: push Delivery: <guid>
>>> [BOT] Consumer Start: push
>>> [BOT] Broadcasting to UI channel=<guid> id=<guid>
```

…and the message should appear in `#general` in the frontend. If you refresh
the browser it stays — it's a real `Message` row owned by the `github-bot` user.

## Diagnosing failures

Walk the pipeline in order; the first step that fails is the problem.

| Symptom in **Recent Deliveries** | Likely cause | Fix |
|---|---|---|
| 404 | URL has `/v{version}/` or wrong path | Use literal `/api/webhooks/github` |
| Timeout / connection refused | ngrok tunnel down or API not running | Restart ngrok / `dotnet run` |
| 401 `Invalid signature` | Secret in GitHub ≠ secret in Nexus | Re-paste the same string in both |
| 401 `Missing signature` | GitHub config has no secret but Nexus does | Set the secret in GitHub |
| 2xx but nothing in chat | No `#general` channel in DB, or RabbitMQ not consuming | Check API console for `[BOT] ERROR: No channel named 'general'` and RabbitMQ UI at http://localhost:15672 |
| 2xx + console shows broadcast but UI empty | SignalR not connected | DevTools → Network → WS → look for `chatHub` frames |

## What the controller actually checks

```
                       ┌────────────────────────────────┐
POST /api/webhooks/    │ 1. Read raw body (NOT parsed)  │
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
byte sequence differs and the HMAC will never match.

## Going to production

- Use a reserved ngrok domain (paid) or replace ngrok with your real public
  endpoint (Cloudflare Tunnel, an ALB, …).
- Set `Webhook__GithubSecret` via your secrets manager — never commit it.
- Keep `UseHttpsRedirection` exemption only for `/api/webhooks/*`. The rest of
  the API still forces HTTPS.
- Consider rate-limiting the webhook route specifically — GitHub will not retry
  faster than its own backoff, but a misconfigured third party could.
