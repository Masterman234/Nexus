# Data and Messaging Strategy

Nexus relies heavily on asynchronous communication to maintain performance, decouple bounded contexts, and support real-time user experiences. This document outlines our data persistence and messaging strategies.

## 1. Database Strategy (PostgreSQL)

We use **PostgreSQL** as the primary relational data store.

- **Entity Framework Core**: Used as the ORM.
- **Schema per Context**: Even though we deploy a monolithic database locally for developer convenience, each Bounded Context (Collaboration, Engineering, Observability) manages its own schema. Direct queries across schemas are forbidden; data must be eventually consistent via messaging.
- **Migrations**: EF Core migrations are executed sequentially during deployment via a dedicated migration runner, not auto-applied on application startup to ensure safe deployments in Kubernetes.

## 2. Caching Strategy (Redis)

**Redis** serves a dual purpose in our architecture:

1. **Distributed Caching**: Used to cache expensive queries (e.g., heavily requested User profiles, AI-generated summaries, and workspace configurations).
   - Pattern: Cache-Aside pattern.
   - Expiration: All keys must have a TTL to prevent stale data.
2. **SignalR Backplane**: Redis is used to scale out our WebSocket connections horizontally. If a user is connected to App Instance A and a message is generated on App Instance B, the Redis backplane ensures the message reaches Instance A.

## 3. Event-Driven Architecture & Messaging (RabbitMQ)

To decouple contexts and handle background tasks, we use an **Event-Driven Architecture** backed by **RabbitMQ** and abstracted via **MassTransit**.

### 3.1 Use Cases for Messaging

- **Integration Events**: When an entity state changes (e.g., `PullRequestMergedEvent`), an integration event is published. The Observability or AI contexts can subscribe to this without the Engineering context needing to know about them.
- **Background Processing**: Heavy tasks (e.g., AI document analysis, synchronizing external git repositories) are dispatched as asynchronous commands to RabbitMQ queues, ensuring HTTP requests remain fast.

### 3.2 Communication Patterns

- **Synchronous (HTTP/REST)**: Used for UI interactions requiring immediate feedback (e.g., creating a ticket, fetching lists).
- **Asynchronous (Pub/Sub)**: Used for inter-context communication and eventual consistency (e.g., `UserCreated`, `IncidentTriggered`).
- **Worker Queues (Send/Receive)**: Used for targeted, heavy background jobs routed to specific worker consumers.

### 3.3 Resiliency

MassTransit is configured with strict resiliency policies:
- **Retries**: Immediate and exponential backoff retry policies for transient database or external API failures.
- **Dead-Letter Queues (DLQ)**: Poison messages that fail repeatedly are routed to error queues. These are monitored by OpenTelemetry metrics for engineering alerts.
- **Outbox Pattern**: When a domain entity is saved to PostgreSQL, any resulting Domain/Integration events are saved to an EF Core Outbox table in the same transaction. A background process publishes them to RabbitMQ. This guarantees exactly-once delivery semantics even if the app crashes between the DB commit and the message publish.