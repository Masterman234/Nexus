# Engineering Standards and Workflows

To maintain portfolio quality and production-grade stability, all code contributed to Nexus must adhere to the following standards.

## 1. Git Workflow & Branching Strategy

We utilize a simplified **Trunk-Based Development** model.

- **Main Branch**: `main` is always deployable and represents the current state of production.
- **Branching**: Developers branch off `main` using the format `type/ticket-number-description` (e.g., `feat/NEX-12-add-ai-summary`, `fix/NEX-14-redis-timeout`).
- **Pull Requests (PRs)**: All changes require a PR.
  - PRs must pass all CI checks (Build, Tests, Linter).
  - PRs require at least one code review approval.
  - Squash and Merge is used to keep the `main` history linear and clean.

## 2. Testing Strategy

Code is a liability without comprehensive tests. We aim for high confidence over arbitrary coverage percentages.

### 2.1 Test Tiers
1. **Unit Tests (xUnit + FluentAssertions + Moq)**: Fast, isolated tests for domain logic, value objects, and pure functions.
2. **Integration Tests (Testcontainers)**: We use **Testcontainers** to spin up actual ephemeral PostgreSQL, Redis, and RabbitMQ Docker containers during test runs. We do *not* mock the database with in-memory providers, as this masks real-world SQL translation errors.
3. **Architecture Tests (NetArchTest)**: Automated tests enforce our Clean Architecture boundaries. (e.g., `Domain` project cannot reference `Infrastructure` project).

## 3. Coding Conventions

- **C# Formatting**: Strictly governed by our `.editorconfig`. Violations break the build via `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>`.
- **Naming**: Interfaces begin with `I`, private fields are `_camelCase`.
- **Nullability**: `<Nullable>enable</Nullable>` is mandatory. If you are confident a value is not null, use explicit checks or the `!` operator with a comment explaining why. Do not disable the feature.
- **Immutability**: Prefer immutable data structures. Use `record` types for DTOs, Commands, Queries, and Value Objects.

## 4. API Design Guidelines

- **RESTful**: Utilize standard HTTP verbs and status codes.
- **Versioning**: All external APIs are versioned (e.g., `v1`).
- **Idempotency**: Commands that mutate state (POST, PUT, DELETE) should ideally be idempotent, passing an Idempotency-Key header where required.
- **Contracts**: OpenAPI (Swagger) is generated automatically. It acts as the source of truth for the frontend `api-client` generation.