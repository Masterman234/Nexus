# Nexus: An Event-Driven Engineering Intelligence Platform

Slack knows your messages. Linear knows your tickets. GitHub Copilot knows your
code. Sentry knows your errors. **None of them know all of them** — and that's
the wall every team's AI assistant currently hits.

Nexus is an event-driven engineering intelligence platform built around a single
thesis: an AI agent that can reason across PRs, incidents, tickets, and chat at
once is qualitatively more useful than four single-source agents. The Slack-style
chat UI is the surface; the real substance is a shared event spine (RabbitMQ +
`ExternalEvent` audit log) that ingests signals from every system a team uses
into one timeline an agent can query.

## 🎯 What makes it different

- **Cross-context AI reasoning.** Features like the [AI Standup Generator](./docs/tickets/initial-epics.md#epic-05-ai-standup-generator-tier-s)
  and [Postmortem Assistant](./docs/tickets/initial-epics.md#epic-06-postmortem-assistant-tier-s)
  query PRs + commits + incidents + chat in one Semantic Kernel call, surfacing
  insights a single-source tool architecturally cannot produce.
- **Event-spine architecture.** GitHub webhooks land in `ExternalEvent`, get
  published as integration events on RabbitMQ, and fan out to consumers in each
  bounded context. The pattern extends to any source — Stripe, CloudWatch,
  PagerDuty — with one adapter per provider.
- **Domain-Driven Design done pragmatically.** Clean Architecture layers,
  bounded contexts (Collaboration, Engineering, Observability, AI), CQRS via
  MediatR, integration events between contexts.
- **Observability-first.** OpenTelemetry tracing across context boundaries from
  Day 1 — you can follow a `git push` from webhook arrival through RabbitMQ to
  SignalR broadcast in one trace.
- **Real-time on a backplane.** SignalR + Redis backplane, so the system
  horizontally scales without losing message ordering inside a channel.
- **Documented tradeoffs.** [Architecture Decision Records (ADRs)](./docs/adrs/)
  explain why each pattern was chosen — not just what was built.

---

## 🏗️ Technology Stack

### Backend
- **Framework**: ASP.NET Core 8
- **Architecture**: Clean Architecture, Pragmatic DDD, CQRS
- **Database**: PostgreSQL (Relational), Redis (Caching / PubSub)
- **Messaging**: RabbitMQ (MassTransit)
- **Real-time**: SignalR
- **Observability**: OpenTelemetry, Serilog
- **Libraries**: MediatR, FluentValidation, EF Core

### Frontend
- **Framework**: React 18, TypeScript
- **Styling**: Tailwind CSS, shadcn/ui
- **State Management**: Zustand, React Query
- **Tooling**: Vite, pnpm (Monorepo)

### Infrastructure
- **Containerization**: Docker, Docker Compose
- **Orchestration**: Kubernetes-ready design
- **CI/CD**: GitHub Actions
- **AI Integration**: Gemini API (with abstraction layer for multi-provider support)

---

## 📂 Project Structure

```text
Nexus/
├── backend/                  # .NET 8 Backend Solution
│   ├── src/                  # Application source code
│   └── tests/                # Unit, Integration, and Architecture tests
├── frontend/                 # React frontend monorepo
│   ├── apps/                 # Client applications (Web, Dashboard)
│   └── packages/             # Shared UI components and utilities
├── docs/                     # Engineering documentation
│   ├── architecture/         # System design and strategy docs
│   ├── adrs/                 # Architecture Decision Records
│   ├── api-contracts/        # OpenAPI and AsyncAPI specifications
│   ├── tickets/              # Initial engineering epics and task breakdown
│   └── progress/             # Roadmaps and milestone tracking
├── docker/                   # Local development environment definitions
└── scripts/                  # CI/CD and automation scripts
```

## 📖 Engineering Documentation

Nexus is built like a real engineering startup. All critical decisions, structural patterns, and coding standards are documented.

1. **[Architecture Docs](./docs/architecture/)**: Deep dives into our DDD approach, event-driven messaging, and AI strategies.
2. **[Architecture Decision Records (ADRs)](./docs/adrs/)**: Historical context for "WHY" specific technologies and patterns were chosen.
3. **[Engineering Tickets](./docs/tickets/initial-epics.md)**: Initial scaffolding epics plus the [differentiator epics](./docs/tickets/initial-epics.md#differentiator-epics) (AI Standup, Postmortem Assistant, Smart Cross-Linking) that exercise the cross-context architecture.
4. **[Roadmap & Vision](./docs/progress/roadmap.md)**: Milestones, the cross-context AI bet, and what's explicitly out of scope.
5. **[GitHub Webhook Setup](./docs/webhooks-github.md)**: End-to-end guide for the GitHub integration — ngrok tunnel, HMAC verification, config wiring.

## 📊 Status

**Shipped** (foundations + ingestion spine + first cross-context feature):
- Identity & JWT auth, workspaces, channels
- Real-time chat (SignalR + Redis backplane), persisted history
- RabbitMQ + MassTransit message bus
- **GitHub webhook ingestion** — HMAC-verified, raw-body preserving, `ExternalEvent` audit log, RabbitMQ-backed consumer broadcasting to chat
- `Commit` / `PullRequest` domain entities populated from webhook payloads (NEX-15)
- Frontend Engineering Timeline view (NEX-11)
- **Cross-context integration events** — `PullRequestOpened` / `PullRequestMerged` / `CommitPushed` published from the webhook consumer (NEX-10b)
- **`IUserActivityQuery`** — single per-user projection joining Commits + PRs + Messages by time window (NEX-16)
- **AI Standup Generator** — `GenerateStandup` MediatR handler calling Gemini through `IAIService` abstraction (NEX-17)
- OpenTelemetry tracing, Serilog structured logging, health checks
- Clean Architecture layering (Domain / Application / Infrastructure / Api) enforced via NetArchTest

**Up next** (user-facing surface for the AI features):
- `/standup` slash command in chat + scheduled dashboard widget (NEX-18) — also lays the slash-command router used by ticketing
- Native Ticketing (EPIC-08) — thin chat-first ticketing context that shares the event spine with code, chat, and incidents; AI features then reason over tickets too without changing the standup query shape
- Postmortem Assistant (EPIC-06) — auto-draft from correlated chat + deploys + alerts; emits draft action-item tickets once EPIC-08 lands
- Smart Cross-Linking (EPIC-07) — implicit graph over the event log; `NEX-#` references resolve to real ticket rows once EPIC-08 lands

## 🚀 Quick Start (Local Development)

*(Coming soon: Full Docker Compose local development setup instructions)*

## 🛡️ Governance and Standards

- **Package Governance**: Managed centrally via `Directory.Packages.props`.
- **Linting & Formatting**: Enforced globally using `.editorconfig` and Roslyn Analyzers (`SonarAnalyzer`, `NetAnalyzers`).
- **Nullability**: Strict Nullable Reference Types (`<Nullable>enable</Nullable>`) enforced at the build level.
- **Testing**: We mandate high coverage across Unit, Integration, and Contract tests.

## 📝 License

This project is licensed under the MIT License.
