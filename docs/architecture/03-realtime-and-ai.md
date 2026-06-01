# Real-Time and AI Strategy

Nexus represents a new generation of tooling by combining real-time collaboration with deeply integrated AI assistance.

## 1. Real-Time Communication (SignalR)

Real-time updates are critical for the collaboration and observability features of Nexus. 

### 1.1 Technology Choice

We use **ASP.NET Core SignalR** with a **Redis Backplane** to manage WebSockets and fallback transports (Server-Sent Events, Long Polling).

### 1.2 Hub Architecture

We maintain focused SignalR Hubs to limit message broadcasting scope:
- `WorkspaceHub`: Scoped to a specific team's engineering workspace. Handles chat, cursor positions, and live whiteboard updates.
- `IncidentHub`: Scoped to live observability dashboards for immediate alerting.

### 1.3 State Management & Optimistic UI

Instead of sending full state payloads over WebSockets, the server broadcasts lightweight notification events (e.g., `{ type: "TicketUpdated", id: "123" }`). 
1. The frontend receives the event via SignalR.
2. The frontend invalidates the specific **React Query** cache key.
3. The UI seamlessly refetches the updated data, ensuring state remains perfectly synchronized without complex client-side reducers.

## 2. AI Integration Strategy

AI is a first-class citizen in Nexus, assisting engineers by summarizing context, explaining code, and helping resolve incidents.

### 2.1 Provider Abstraction

We use the **Semantic Kernel** library as an abstraction layer over our AI providers. 
- *Why?* While we default to the **Gemini API** for its massive context window and reasoning capabilities, Semantic Kernel ensures our core domain is not tightly coupled to a single vendor's SDK. If we need to support localized OSS models or alternative cloud providers in the future, the integration code remains unchanged.

### 2.2 Core AI Features

- **Incident Summarization**: Automatically generating post-mortem drafts from OpenTelemetry traces and chat logs.
- **Code Context Assistant**: Explaining complex Pull Requests or identifying potential regressions based on historical git data.
- **Semantic Search**: Using embeddings to find related engineering tickets or past architectural decisions.

### 2.3 Performance and UX Considerations

- **Streaming Responses**: AI generation can be slow. All AI endpoints utilize Server-Sent Events (SSE) or SignalR streaming to stream tokens to the frontend, ensuring the user sees immediate progress.
- **Rate Limiting & Cost Control**: AI routes are strictly rate-limited per user and tenant. Caching via Redis is employed for identical semantic queries (e.g., fetching the summary of a specific PR).