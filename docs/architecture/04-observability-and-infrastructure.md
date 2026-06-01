# Observability, Infrastructure & Operations

Nexus takes an "Observability-First" approach, treating logs, metrics, and traces as mission-critical features rather than afterthoughts.

## 1. Observability Strategy (OpenTelemetry & Serilog)

We adhere strictly to the **OpenTelemetry (OTel)** standard for observability.

### 1.1 Distributed Tracing
- Every incoming HTTP request or background RabbitMQ message generates a trace context.
- We use the `X-Correlation-ID` or W3C Trace Context headers to propagate traces across HTTP clients, SignalR, and MassTransit.
- EF Core and HttpClient OTel instrumentation are enabled out-of-the-box, allowing us to see exactly how long database queries and external API calls take within a trace.

### 1.2 Structured Logging
- **Serilog** handles all structured logging, outputting JSON to the console.
- Logs are automatically enriched with the current trace and span IDs, allowing developers to jump seamlessly from a trace visualization to the specific logs emitted during that operation.

## 2. Infrastructure & Docker Strategy

The system is designed for **Cloud-Native deployment**.

### 2.1 Local Development Workflow
We provide a zero-friction local setup via **Docker Compose**:
- Running `docker-compose up` provisions PostgreSQL, Redis, and RabbitMQ.
- The `.NET` backend and `React` frontend run on the host machine using standard CLI tools (`dotnet run`, `pnpm run dev`) to enable hot-reloading and fast debug cycles.

### 2.2 Containerization
- Backend services are packaged using multi-stage Dockerfiles leveraging the official Alpine .NET runtime images for minimal attack surface and size.
- The React frontend is built as static assets and served via an Nginx container.

## 3. CI/CD Strategy

We use **GitHub Actions** for our Continuous Integration and Deployment pipelines.

### 3.1 Pull Request Pipeline
1. **Lint & Format**: Enforces `.editorconfig` and Prettier rules.
2. **Build**: Compiles `.NET` and `React` workspaces.
3. **Test**: Runs Unit and Integration tests. Generates coverage reports.
4. **Static Analysis**: Runs SonarAnalyzer for security vulnerabilities and code smells.

### 3.2 Deployment Strategy
- **Container Registry**: Merges to `main` trigger Docker image builds pushed to GitHub Container Registry (GHCR) or Azure ACR.
- **Kubernetes-Ready**: The system is designed to be deployed via Helm charts, utilizing ConfigMaps for environment variables and Secrets for sensitive credentials.

## 4. Security Strategy

- **Authentication**: JWT-based authentication. We use an OIDC-compliant provider for identity management.
- **Authorization**: Claims-based authorization handled at the API Controller/FastEndpoint level.
- **Data Protection**: Sensitive strings (tokens, API keys) are never logged. `.NET`'s Data Protection API secures cookies and CSRF tokens.