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



## Milestone 1: Foundations & Infrastructure (Weeks 1-2)
- [x] Repository setup (monorepo, build props, linting rules).
- [x] Architecture documentation and ADRs.
- [x] Docker Compose setup for PostgreSQL, Redis, RabbitMQ.
- [x] .NET 8 Web API skeleton (Clean Architecture folders, MediatR, EF Core).
- [x] OpenTelemetry and Serilog bootstrapping.
- [x] React/Vite/TypeScript frontend monorepo scaffolding.

## Milestone 2: Identity & Collaboration Core (Weeks 3-5)
- [x] Identity Context: JWT Auth, User & Team models.
- [x] Collaboration Context: Workspaces, Channels.
- [x] SignalR Integration: Real-time chat messaging with Redis backplane.
- [x] Frontend: Workspace UI, Chat interface (shadcn/ui), React Query hooks.

## Milestone 3: Engineering Context & VCS Integration (Weeks 6-8)
- [ ] Engineering Context: Tickets, Pull Requests, Commits.
- [x] Background workers (RabbitMQ): Foundation configured and consumer-ready.
- [x] GitHub webhook ingestion: HMAC-verified controller, ExternalEvent persistence,
      RabbitMQ-backed `GithubWebhookConsumer` posting to a configurable channel,
      SignalR broadcast through to the frontend chat (push + pull_request events).
- [ ] Integration Events: Broadcasting `PullRequestMerged` across the bus
      (current consumer emits a chat message; needs domain modelling of PR state).
- [ ] Frontend: Kanban board, PR timeline views.

## Milestone 4: Observability & AI (Weeks 9-11)
- [ ] Observability Context: Ingesting mock incident alerts.
- [ ] AI Context (Semantic Kernel): Integrating Gemini API.
- [ ] Feature: AI Incident Summarization (background processing via RabbitMQ).
- [ ] Feature: AI Code Explanation (streaming response to frontend).

## Milestone 5: Polish & Deployment (Week 12)
- [ ] CI/CD Pipelines (GitHub Actions).
- [ ] Infrastructure as Code (Helm charts / Bicep).
- [ ] Final security audit and performance load testing.
- [ ] Public release / Portfolio showcase.