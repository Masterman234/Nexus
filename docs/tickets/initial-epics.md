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
- [ ] **NEX-11**: Build the frontend Engineering Timeline view.

## EPIC-04: AI-Assisted Insights
**Goal**: Integrate the Gemini API to provide automated value on top of engineering data.
- **NEX-12**: Implement Semantic Kernel abstraction layer in `Nexus.Infrastructure`.
- **NEX-13**: Implement `GenerateIncidentSummaryCommand` (triggered via RabbitMQ when an incident occurs).
- **NEX-14**: Build frontend AI chat window with Server-Sent Events (SSE) streaming support.