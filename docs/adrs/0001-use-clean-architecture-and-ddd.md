# ADR 0001: Use Clean Architecture and Pragmatic DDD

**Date**: 2026-05-27
**Status**: Accepted
**Context**: We need to define the foundational structure for the Nexus backend. The system will handle complex domain logic spanning real-time collaboration, code review metadata, and observability.
**Decision**: We will use Clean Architecture with a Pragmatic Domain-Driven Design (DDD) approach.
**Rationale**:
- **Separation of Concerns**: Clean Architecture ensures that our core domain logic is not polluted by infrastructure concerns (like EF Core or RabbitMQ).
- **Testability**: By isolating the domain and application layers, we can easily write fast, isolated unit tests.
- **Pragmatism**: Strict DDD can lead to overengineering. We will use DDD patterns (Entities, Value Objects, Domain Events) where complexity warrants it, but we will not force CRUD operations through complex aggregates if simple queries suffice (hence, CQRS).
**Consequences**: 
- Developers must understand the boundary rules (e.g., Domain cannot reference Infrastructure).
- Slight increase in initial boilerplate compared to a simple N-Tier architecture.