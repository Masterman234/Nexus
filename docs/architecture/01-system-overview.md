# High-Level Architecture & System Design

## 1. Executive Summary

Nexus is designed as a cloud-native, real-time collaboration and observability platform. To meet the goals of high scalability, maintainability, and educational value, the system is architected around **Pragmatic Domain-Driven Design (DDD)** and **Clean Architecture**.

The system avoids "overengineering" by adopting patterns like CQRS and Event-Driven messaging only where they solve concrete business problems, ensuring the architecture remains understandable yet production-ready.

## 2. System Context (C4 Model)

At a macro level, Nexus acts as an intelligent intermediary between engineering teams, code repositories, and observability tools.

- **Users**: Software Engineers, DevOps, Engineering Managers.
- **Core System**: Nexus Monolithic API (structured internally as modular bounded contexts).
- **External Dependencies**:
  - LLM Provider (Google Gemini API via abstraction layer) for AI insights.
  - Identity Provider (e.g., OAuth/OIDC for enterprise SSO).
  - Webhook sources (GitHub/GitLab, Datadog/PagerDuty).

## 3. Bounded Contexts

To maintain loose coupling, the system is divided into logical domains. These domains share data asynchronously via integration events rather than direct database joins.

1. **Collaboration Context**: Manages real-time chats, whiteboards, and threaded engineering discussions.
2. **Engineering Context**: Integrates with external VCS (Pull Requests, Commits) and tracks engineering tickets.
3. **Observability Context**: Ingests incident alerts, logs, and telemetry to correlate system health with engineering activities.
4. **Identity Context**: Manages Users, Teams, Roles, and Permissions.
5. **AI Context**: A supportive domain providing text summarization, incident root-cause analysis, and code explanations.

## 4. Backend Architecture Strategy (ASP.NET Core)

The backend follows **Clean Architecture** to decouple the core business rules from infrastructure and delivery mechanisms.

### 4.1 Layers
- **Domain**: Contains Entities, Value Objects, Domain Events, and Repository Interfaces. (Zero external dependencies).
- **Application**: Contains Use Cases (Commands/Queries), MediatR handlers, and FluentValidation rules.
- **Infrastructure**: Implementations of EF Core Repositories, RabbitMQ publishers, and external API clients.
- **Presentation**: The ASP.NET Core API layer (Controllers or FastEndpoints) and SignalR hubs.

### 4.2 Cross-Cutting Strategies

- **API Versioning**: Implemented at the controller level using URL versioning (e.g., `/api/v1/workspaces`). This ensures long-term contract stability.
- **Rate Limiting**: Applied via `Microsoft.AspNetCore.RateLimiting`. Standard sliding window limit per IP/User token to prevent abuse, especially on computationally expensive AI routes.
- **Correlation IDs**: Every HTTP request generates or propagates an `X-Correlation-ID`. This ID is injected into Serilog, attached to OpenTelemetry traces, and forwarded along with all RabbitMQ messages for end-to-end trace mapping.

## 5. Frontend Architecture Strategy (React/TypeScript)

The frontend is built as a modular monorepo using pnpm workspaces.

- **Apps**: `frontend/apps/web` (The main application).
- **Packages**: `frontend/packages/ui` (shadcn/ui + Tailwind components), `frontend/packages/api-client` (generated React Query hooks).

### 5.1 State Management
- **Server State**: Managed strictly by **React Query**. We do not sync server state into global client stores.
- **Client/UI State**: Managed by **Zustand** for transient states (e.g., currently selected team, open modals).
- **Real-time Synchronization**: SignalR messages trigger React Query cache invalidation or optimistic updates rather than manual state mutations.

## 6. Solution Structure Mapping

```text
backend/src/
├── Nexus.Domain/             # Core entities & interfaces
├── Nexus.Application/        # MediatR CQRS, Validation
├── Nexus.Infrastructure/     # EF Core, MassTransit, Redis, AI clients
└── Nexus.Api/                # REST endpoints, SignalR Hubs, OTel setup
```

## 7. Tradeoffs and Decisions

- **Why a Modular Monolith over Microservices?**
  - *Decision*: We deploy a single ASP.NET Core process containing all bounded contexts.
  - *Tradeoff*: Reduces operational overhead and Kubernetes complexity while allowing us to enforce logical boundaries using namespaces and project references. If a context (e.g., the AI Service) requires independent scaling later, the Clean Architecture makes it easy to extract.
- **Why MediatR?**
  - *Decision*: MediatR enforces a strict request/response pattern for Use Cases.
  - *Tradeoff*: Introduces slight indirection, but forces the application layer to remain extremely clean, decoupled, and highly testable.