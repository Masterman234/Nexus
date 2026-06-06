# Nexus: Engineering Roadmap

This document outlines the high-level milestones for bringing Nexus from concept to production.

## Vision

**Nexus is an event-driven engineering intelligence platform.** The chat UI is a
surface — the substance is a shared event spine (RabbitMQ + `ExternalEvent`)
that ingests signals from every system a team uses (GitHub, observability,
ticketing, chat) into one timeline.

The competitive bet is that **AI agents reasoning across all of those sources at
once** beat AI agents that only see one source. Slack's AI knows messages.
Linear's AI knows tickets. Copilot knows code. None of them know all of them.
Nexus's architecture is built to make that cross-context reasoning trivial.

Concretely, this means feature work prioritises:

- **Event ingestion adapters** over chat-feature parity with Slack
- **Cross-context AI features** (standup synthesis, postmortem drafting,
  smart cross-linking) over single-source AI features (summarise a thread)
- **Structured domain models** for PRs/commits/incidents/tickets over storing raw payloads
- **Read-model replay** from the event log over write-heavy bespoke endpoints

Native ticketing (EPIC-08) is a deliberate scope extension of the same thesis:
tickets become another first-class source in the event spine so the AI features
reason over code + chat + incidents + tickets together. The goal is integration
depth, not Jira feature parity — see EPIC-08's non-goals list in
[initial-epics.md](../tickets/initial-epics.md#epic-08-native-ticketing-tier-s).

When in doubt, the question is: *does this feature exploit cross-context data
in a way a single-purpose tool cannot?* If yes, build it. If no, deprioritise.



## Milestone 1: Foundations & Infrastructure (Complete)
- [x] Repository setup (monorepo, build props, linting rules).
- [x] Architecture documentation and ADRs.
- [x] Docker Compose setup for PostgreSQL, Redis, RabbitMQ.
- [x] .NET 8 Web API skeleton (Clean Architecture folders, MediatR, EF Core).
- [x] OpenTelemetry and Serilog bootstrapping.
- [x] React/Vite/TypeScript frontend monorepo scaffolding.

## Milestone 2: Identity, Collaboration & Design (Complete)
- [x] Identity Context: JWT Auth, User & Team models.
- [x] Collaboration Context: Workspaces, Channels.
- [x] SignalR Integration: Real-time chat messaging with Redis backplane.
- [x] **DESIGN-01**: Establish premium "Engineering Intelligence" design system.
- [x] Frontend: Unified Landing Page, Polished Auth Flow, Workspace Shell.

## Milestone 3: Engineering Context & VCS Integration (Wrapping up)
- [x] **NEX-15**: Structured domain entities for `Commit` and `PullRequest` (parsed from webhooks).
- [x] **NEX-11**: High-density Engineering Timeline view with real-time updates.
- [x] Background workers (RabbitMQ): Foundation configured and consumer-ready.
- [x] GitHub webhook ingestion: HMAC-verified controller, ExternalEvent persistence.
- [x] **NEX-10b**: Broadcast `PullRequestOpened` / `PullRequestMerged` / `CommitPushed` integration events across the bus for cross-context logic.

## Milestone 4: AI Insights & Reasoning (Current)
- [~] **NEX-12**: Integrate Semantic Kernel with Google Gemini 1.5 Pro.
  *(Direct Gemini call shipped via `AIService`; Semantic Kernel wrapper still pending.
  Likely close as "won't do" — SK's Gemini connector doesn't authenticate `AQ.*`
  keys, and `IAIService` already gives us the provider-swap abstraction SK was
  meant to provide.)*
- [x] **NEX-17**: "What did I do today?" AI Standup Generator using Timeline data.
  *(End-to-end working once the Gemini auth bug was fixed — `AQ.*`-format keys
  must be sent via `x-goog-api-key` header, not `?key=` query parameter.)*
- [x] **NEX-16**: `IUserActivityQuery` — per-user cross-context projection (Commits + PRs + Messages). The `Incident` and `Ticket` entities now exist (EPIC-06 / EPIC-08 below); wiring those slots into the projection is the remaining follow-up.
- [~] **NEX-18**: `/standup` slash command + scheduled dashboard widget.
  *(Slash command shipped via `IChatCommandRouter` in `Application/ChatCommands/`;
  reuses existing `GenerateStandup.Command` and posts the AI reply as a new
  `nexus-bot` system user. Same router will dispatch EPIC-08 ticketing commands.
  Scheduled posting via Hangfire still pending.)*
- [~] **EPIC-06 (Incidents) — domain landed, AI half pending.** `Incident` entity +
  `IncidentSeverity`/`IncidentStatus`, EF config, migration `AddIncidentEntities`,
  Declare/Resolve/List handlers, and the `/incident` slash command all shipped.
  Still missing: NEX-20 alert-source ingest, NEX-22 `DraftPostmortemCommand` (AI
  postmortem), NEX-23 postmortem editor UI — i.e. the AI summarization feature.
- [~] **EPIC-07 (Smart Cross-Linking) — mostly shipped.** `EntityReference` entity +
  `ReferenceExtractor` (NEX-24) wired into `SendMessage`, `CreateTicket`,
  `UpdateTicket`, `AddTicketComment`, and `GithubWebhookConsumer`; "Related"
  sidebar API (`ReferencesController` `GET {id}/related`) and frontend sidebar
  (NEX-26) live. Remaining: NEX-25 backfill job over existing `ExternalEvent` history.
- [ ] Feature: AI Code Explanation (streaming/SSE response to frontend) — not started.

## Milestone 4.5: Native Ticketing (EPIC-08, Complete)
Slotted after NEX-18 (which provides the slash-command router) and before
EPIC-06 / EPIC-07 so postmortem drafts can produce real action-item tickets
and cross-linking can resolve `NEX-#` references to actual ticket rows.
- [x] **NEX-27**: `Ticket` + `TicketComment` + `TicketStatusChange` entities, EF configs, migration. *(Entities in `Domain/Entities/`, configs in `Infrastructure/Configurations/`, migration `AddTicketingEntities`.)*
- [x] **NEX-28**: MediatR commands & queries — create, update, assign, transition, comment, list (+ get-by-id / get-by-number). Every transition writes a `TicketStatusChange` audit row. *(`Application/Tickets/Commands` + `/Queries`.)*
- [x] **NEX-29**: Integration events — `TicketCreated`, `TicketAssigned`, `TicketStatusChanged`, `TicketResolved`. Publish-after-save. *(`Application/Tickets/IntegrationEvents/`.)*
- [x] **NEX-30**: Slash commands — `/ticket new`, `/ticket NEX-#`, `/ticket assign`, `/ticket close`, `/ticket comment`, `/ticket list` — dispatched via `ChatCommandRouter`.
- [x] **NEX-31**: PR-merge auto-transition — `PullRequestMergedConsumer` parses `Closes NEX-\d+` / `Fixes NEX-\d+` and moves matching tickets to Done.
- [x] **NEX-32 (Tier A, optional)**: Kanban board UI. *(`features/tickets/TicketKanban.tsx`.)*
- [x] **NEX-33 (Tier A, optional)**: Ticket detail with comments, history timeline, and Related sidebar.

## Milestone 5: Polish & Deployment
- [ ] CI/CD Pipelines (GitHub Actions).
- [ ] Infrastructure as Code (Helm charts / Bicep).
- [ ] Final security audit and performance load testing.
- [ ] Public release / Portfolio showcase.
