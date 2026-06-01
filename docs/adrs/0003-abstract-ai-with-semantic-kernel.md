# ADR 0003: Abstract AI Integrations with Semantic Kernel

**Date**: 2026-05-27
**Status**: Accepted
**Context**: Nexus relies heavily on AI to summarize incidents and explain code. Initially, we are targeting the Google Gemini API. However, the AI landscape is shifting rapidly, and tying our core domain to a specific vendor SDK poses a risk.
**Decision**: We will use Microsoft's Semantic Kernel as the abstraction layer for all AI interactions.
**Rationale**:
- **Vendor Agnosticism**: Semantic Kernel allows us to swap underlying AI providers (e.g., from Gemini to a local Llama-3 instance) with minimal code changes.
- **Plugin Architecture**: It provides a structured way to define AI "skills" and plugins that the LLM can call, fitting well with our clean architecture approach.
**Consequences**:
- We are taking a dependency on a relatively new framework, which may introduce breaking changes.
- We must map vendor-specific features (like Gemini's massive context window) through the Semantic Kernel abstractions.