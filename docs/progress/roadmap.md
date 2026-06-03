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
- **Structured domain models** for PRs/commits/incidents over storing raw payloads
- **Read-model replay** from the event log over write-heavy bespoke endpoints

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
- [ ] **NEX-10b**: Broadcast `PullRequestMerged` / `PullRequestOpened` integration events across the bus for cross-context logic.

## Milestone 4: AI Insights & Reasoning (Current)
- [~] **NEX-12**: Integrate Semantic Kernel with Google Gemini 1.5 Pro.
  *(Direct Gemini call shipped via `AIService`; Semantic Kernel wrapper still pending.)*
- [~] **NEX-17**: "What did I do today?" AI Standup Generator using Timeline data.
  *(Handler `GenerateStandup` shipped; actively debugging Gemini call — last 5 commits are logging additions.)*
- [ ] **NEX-16**: `IUserActivityQuery` — per-user cross-context projection (Commits + PRs + Messages + Incidents).
- [ ] **NEX-18**: `/standup` slash command + scheduled dashboard widget.
- [ ] Feature: AI Incident Summarization (background processing via RabbitMQ).
- [ ] Feature: AI Code Explanation (streaming response to frontend).

## Milestone 5: Polish & Deployment
- [ ] CI/CD Pipelines (GitHub Actions).
- [ ] Infrastructure as Code (Helm charts / Bicep).
- [ ] Final security audit and performance load testing.
- [ ] Public release / Portfolio showcase.
