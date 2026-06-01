# Nexus: Engineering Roadmap

This document outlines the high-level milestones for bringing Nexus from concept to production.

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