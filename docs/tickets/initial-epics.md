# Initial Engineering Epics

The following epics form the initial product backlog for Nexus. They are designed to be picked up by the engineering team sequentially.

## EPIC-01: Infrastructure & API Scaffolding
**Goal**: Establish the base backend and frontend architecture so feature work can begin in parallel.
- [x] **NEX-01**: Configure standard `docker-compose.yml` for local dependencies (Postgres, Redis, RabbitMQ).
- [x] **NEX-02**: Scaffold the ASP.NET Core Clean Architecture layers (Domain, App, Infra, Api).
- [x] **NEX-03**: Configure OpenTelemetry tracing and Serilog structured logging.
- [x] **NEX-04**: Scaffold the React/Vite monorepo and install Tailwind/shadcn.

## EPIC-02: Real-time Collaboration MVP
**Goal**: Users can authenticate, join a workspace, and send real-time chat messages.
- [x] **NEX-05**: Implement Identity models and JWT authentication.
- [x] **NEX-06**: Implement Chat Hub via SignalR and Redis Backplane.
- [x] **NEX-07**: Build frontend Chat UI with optimistic updates using Zustand/React Query.

## EPIC-03: Event-Driven Engineering Data
**Goal**: Nexus can ingest and display code repository data asynchronously.
- [x] **NEX-08**: Configure MassTransit and RabbitMQ in the backend.
- [x] **NEX-09**: Create the Github Webhook ingest controller.
- [x] **NEX-10**: Implement the `ProcessGithubWebhookCommand` as a background worker.
- [x] **NEX-11**: Build the frontend Engineering Timeline view.
- [ ] **NEX-10b**: Publish `PullRequestMerged` / `PullRequestOpened` integration events from `GithubWebhookConsumer` so other contexts (AI, Incidents) can subscribe.

## EPIC-04: AI-Assisted Insights
**Goal**: Integrate the Gemini API to provide automated value on top of engineering data.
- [~] **NEX-12**: Implement Semantic Kernel abstraction layer in `Nexus.Infrastructure`. *(Partially shipped as `AIService` calling Gemini directly; SK wrapper still pending.)*
- [ ] **NEX-13**: Implement `GenerateIncidentSummaryCommand` (triggered via RabbitMQ when an incident occurs).
- [ ] **NEX-14**: Build frontend AI chat window with Server-Sent Events (SSE) streaming support.

---

# Differentiator Epics

The epics above ship the platform's plumbing. The epics below are what makes
Nexus *interesting*. Each one explicitly exercises the cross-context event spine
in a way a single-purpose Slack/Linear/Sentry competitor architecturally can't.

## EPIC-05: AI Standup Generator (Tier S)
**Goal**: Each user can ask Nexus "what did I do yesterday?" and get a synthesised
standup pulled from PRs they touched, commits they authored, incidents they
triaged, and chat threads they participated in.

This is the canonical demo of the cross-context architecture — a single Gemini
call ingests four data sources unified by `UserId` and timestamp.

- [x] **NEX-15**: Domain model for `Commit` and `PullRequest` entities; populate
  from `GithubWebhookConsumer` so the data is queryable, not just JSON in
  `ExternalEvent`. *(Entities + EF configs + migration `AddEngineeringEntities` shipped.)*
- [ ] **NEX-16**: `IUserActivityQuery` projection that returns a user's activity
  across all contexts within a time window. SQL-level, not via AI.
  *(Current `GetEngineeringActivity` is workspace-scoped, not per-user cross-context.)*
- [~] **NEX-17**: `GenerateStandupCommand` MediatR handler — calls Semantic Kernel
  with the activity projection as context, returns markdown.
  *(Implementation landed in `Application/Engineering/Commands/GenerateStandup`;
  currently debugging Gemini integration — see commits `5faafd3`, `8b2910e`, `23b5314`.)*
- [ ] **NEX-18**: `/standup` slash command in chat + a dashboard widget that runs
  on schedule (Hangfire / cron) and posts each user's standup to a configured
  channel.

## EPIC-06: Postmortem Assistant (Tier S)
**Goal**: When an incident is resolved, Nexus auto-drafts a postmortem from the
chat thread during the incident window, the deploys/commits in that window,
the PRs that touched the affected files, and the alert payload.

This is the killer feature for SRE/DevOps audiences.

- [ ] **NEX-19**: Domain model for `Incident` (status, severity, affected services,
  start/end timestamps).
- [ ] **NEX-20**: Ingest adapter for at least one alerting source (CloudWatch,
  PagerDuty, or a mock JSON endpoint) → emit `IncidentCreated` /
  `IncidentResolved` integration events.
- [ ] **NEX-21**: `IncidentTimelineQuery` — given an incident, gather correlated
  messages, commits, PRs by time window + service tag.
- [ ] **NEX-22**: `DraftPostmortemCommand` handler — Semantic Kernel prompt that
  outputs a structured markdown postmortem (Timeline, Root Cause, Impact,
  Action Items).
- [ ] **NEX-23**: Postmortem editor UI — drafted doc is the starting point; user
  edits in-app, exports to GitHub Issue or markdown file.

## EPIC-07: Smart Cross-Linking (Tier S)
**Goal**: Surface the implicit graph in the event log — commit messages link to
ticket IDs, PRs link to incidents touching the same files, incidents link to
chat threads mentioning the same service.

No literal graph DB; just SQL queries over the structured event spine. The UX
is what sells the architecture.

- [ ] **NEX-24**: Reference extractor — parse `NEX-\d+` / `#\d+` / `SEV-\d+` style
  references out of commit messages, PR titles, chat messages on save.
  Persist as a `EntityReference` join row.
- [ ] **NEX-25**: Backfill job — run the extractor against the existing
  `ExternalEvent` history.
- [ ] **NEX-26**: "Related" sidebar on every entity view (PR, ticket, incident,
  message) showing what else references it.

---

# Backlog (Tier A, no ticket numbers yet)

Ship these after the Tier-S epics. Each one *demonstrates* a piece of
distributed-systems literacy in a portfolio-readable way.

- **Generic webhook adapter pattern.** Extract `IWebhookAdapter<TSource>` from
  `GithubWebhookConsumer`. Add adapters for Stripe, AWS CloudWatch, Linear,
  PagerDuty. Each is ~50 lines and proves the architecture extends.
- **Cross-context search.** Postgres full-text search over messages + commits +
  tickets + incidents. Single endpoint, faceted by source.
- **Slash commands.** `/incident new`, `/deploy status`, `/standup`,
  `/explain <sha>`. Each routes to a MediatR command; user-facing CQRS.
- **Event replay.** `ExternalEvent` is already an event log — add a worker
  that replays it to rebuild read models. Proves event-sourcing literacy.

---

# Explicitly out of scope

Listed here so we don't waste cycles on Slack-parity features that don't
differentiate:

- Threading, voice channels, file uploads, mentions, themes, presence polish.
  (Every chat app has these. They make the project look more "done" but don't
  improve the architectural story.)
- Multi-tenancy / org isolation. (Only worth the complexity if real users are
  signing up.)
- E2E encryption, plugin marketplace.