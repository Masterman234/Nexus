# ADR 0002: Use RabbitMQ and MassTransit for Async Messaging

**Date**: 2026-05-27
**Status**: Accepted
**Context**: Nexus features discrete bounded contexts (Collaboration, Engineering, Observability). These contexts need to share data (e.g., when a user is created, or a PR is merged) without creating tight database-level coupling. Furthermore, AI tasks require background processing to avoid blocking HTTP requests.
**Decision**: We will use RabbitMQ as our message broker, abstracted through the MassTransit library.
**Rationale**:
- **Decoupling**: Publish/Subscribe patterns allow contexts to react to integration events independently.
- **Reliability**: MassTransit provides built-in retry policies, Dead Letter Queues (DLQ), and outbox pattern support, ensuring messages are not lost if a service restarts.
- **Ecosystem**: RabbitMQ is a proven, production-grade broker that is easy to host locally via Docker.
**Consequences**:
- Eventual consistency: UIs must be designed to handle slight delays in data propagation.
- Operational overhead: Requires maintaining a RabbitMQ cluster in production.