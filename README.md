# Nexus: AI-Powered Engineering Collaboration

Nexus is a production-grade, AI-assisted real-time engineering collaboration and observability platform. Designed for modern software teams, Nexus bridges the gap between code repositories, incident management, and real-time developer communication. 

This repository serves as a **portfolio-quality example** of cloud-native architecture, pragmatic DDD, and observability-first engineering.

## 🎯 Project Goals

- **Production-Grade Architecture**: Pragmatic Domain-Driven Design (DDD), Clean Architecture, and CQRS patterns.
- **Observability-First**: Built with OpenTelemetry, Serilog, and centralized distributed tracing from Day 1.
- **Scalable Real-time**: Robust async messaging and WebSocket-based event streaming using RabbitMQ and SignalR.
- **AI-Assisted Workflows**: Seamless integration with the Gemini API for incident summaries, code explanations, and engineering assistance.
- **Educational Value**: Meticulously documented tradeoffs, Architectural Decision Records (ADRs), and deep-dive engineering strategies.

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
3. **[Engineering Tickets](./docs/tickets/)**: The initial backlog formatted as actionable engineering epics.

## 🚀 Quick Start (Local Development)

*(Coming soon: Full Docker Compose local development setup instructions)*

## 🛡️ Governance and Standards

- **Package Governance**: Managed centrally via `Directory.Packages.props`.
- **Linting & Formatting**: Enforced globally using `.editorconfig` and Roslyn Analyzers (`SonarAnalyzer`, `NetAnalyzers`).
- **Nullability**: Strict Nullable Reference Types (`<Nullable>enable</Nullable>`) enforced at the build level.
- **Testing**: We mandate high coverage across Unit, Integration, and Contract tests.

## 📝 License

This project is licensed under the MIT License.