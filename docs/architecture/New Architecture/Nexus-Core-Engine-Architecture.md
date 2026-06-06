# SaaS Core Engine Platform — Enterprise Architecture Document

**Project Codename:** Nexus Core Engine
**Document Version:** 1.0
**Date:** 2026-06-04
**Status:** Approved for Implementation
**Author:** Principal Software Architect
**Classification:** Internal — Engineering

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Business Goals](#2-business-goals)
3. [Functional Requirements](#3-functional-requirements)
4. [Non-Functional Requirements](#4-non-functional-requirements)
5. [Domain Model](#5-domain-model)
6. [Bounded Contexts](#6-bounded-contexts)
7. [Service Responsibilities](#7-service-responsibilities)
8. [Database Design](#8-database-design)
9. [Event-Driven Architecture](#9-event-driven-architecture)
10. [API Specifications](#10-api-specifications)
11. [Security Architecture](#11-security-architecture)
12. [Multi-Tenancy Strategy](#12-multi-tenancy-strategy)
13. [Deployment Architecture](#13-deployment-architecture)
14. [Observability Strategy](#14-observability-strategy)
15. [Architecture Decision Records (ADRs)](#15-architecture-decision-records-adrs)
16. [Development Roadmap](#16-development-roadmap)
17. [Appendix A — Repository Layout](#appendix-a--repository-layout)
18. [Appendix B — Glossary](#appendix-b--glossary)

---

## 1. Executive Summary

### 1.1 Purpose

This document defines the reference architecture for **Nexus Core Engine** — a multi-tenant SaaS foundation platform that provides the cross-cutting business capabilities (identity, organization management, authorization, auditing, notifications, and billing) on top of which vertical SaaS products are assembled. Rather than re-implementing these concerns in every product line, Nexus Core establishes a *shared platform substrate*: a set of independently deployable services exposed through a unified API Gateway, communicating via durable event streams, and instrumented end-to-end with OpenTelemetry.

### 1.2 Architectural Style

Nexus Core adopts a **modular microservices architecture** combined with **event-driven choreography** for cross-service workflows and **synchronous REST/gRPC** for query-shaped interactions. The platform is built around the principles of:

- **Domain-Driven Design (DDD)** — each bounded context maps to exactly one deployable service with a private database (Database-per-Service).
- **CQRS-lite** — read models are denormalized where latency matters (Permissions, Audit search), while command paths preserve strong consistency at the aggregate boundary.
- **Outbox + Inbox patterns** — guarantee exactly-once *effective* delivery of integration events across PostgreSQL and RabbitMQ.
- **Zero-trust networking** — services authenticate to each other via mTLS and short-lived service tokens; the gateway is the only ingress.
- **Cell-based multi-tenancy** — tenant data isolation via Row-Level Security (RLS) at the database tier, augmented by per-tenant claim propagation.

### 1.3 Technology Stack at a Glance

| Layer | Technology | Rationale |
|---|---|---|
| Application Runtime | ASP.NET Core 9 (LTS-track) | Mature, performant, native AOT-ready, first-class OpenTelemetry support |
| Persistence | PostgreSQL 16 + EF Core 9 | RLS, JSONB, logical replication, partitioning |
| Messaging | RabbitMQ 3.13 (quorum queues) | Durable, high-throughput AMQP with delivery guarantees |
| Cache / Distributed State | Redis 7.4 (with Redis Streams) | L2 cache, rate limiting, idempotency keys, ephemeral pub/sub |
| Container Runtime | Docker / OCI | Portable across local, on-prem, and managed Kubernetes |
| Telemetry | OpenTelemetry + OTLP → Tempo/Loki/Prometheus + Grafana | Vendor-neutral observability |
| Orchestration | Kubernetes (EKS/AKS/GKE) | Standard control plane for production |
| API Gateway | YARP (Yet Another Reverse Proxy) | .NET-native, programmable, OTEL-instrumented |

### 1.4 Strategic Value

The platform compresses time-to-market for new SaaS products from ~9 months (greenfield) to ~6 weeks (composition on the platform), eliminates ~70% of duplicated work across product teams, and centralizes the compliance surface (SOC 2, ISO 27001, GDPR) into a small number of audited services.

---

## 2. Business Goals

### 2.1 Strategic Goals

| ID | Goal | Measurable Outcome |
|---|---|---|
| BG-01 | Establish a reusable SaaS substrate | ≥ 3 product lines onboarded within 12 months of GA |
| BG-02 | Reduce per-product compliance scope | Centralize PII, auth, audit in ≤ 4 services; product teams inherit attestation |
| BG-03 | Enable consumption-based monetization | Native metering and billing for any product, day 1 |
| BG-04 | Support enterprise customers | SSO, SCIM, fine-grained RBAC/ABAC, audit export, data residency |
| BG-05 | Operational excellence | 99.95% gateway uptime, P95 cross-service latency < 250 ms |
| BG-06 | Developer productivity | New service scaffolded and deployable in < 1 day via templates |

### 2.2 Stakeholder Map

- **Product Owners** — compose features against platform APIs without owning cross-cutting code.
- **Customer Tenants** — administer their own org structure, users, roles, billing.
- **Internal Operators (SRE)** — single observability plane, centralized incident response.
- **Compliance / Legal** — auditable trail for every privileged action; consent and retention controls.
- **Finance** — accurate metering → invoicing without engineering effort per product.

### 2.3 Constraints

- **Regulatory:** GDPR (EU), CCPA (CA), SOC 2 Type II within 12 months of GA.
- **Commercial:** Must support per-seat, per-feature, and metered usage pricing simultaneously.
- **Operational:** Maximum acceptable RPO = 5 minutes; RTO = 30 minutes for Tier-0 services.
- **Technical:** Existing customer integrations expect REST/JSON; gRPC is internal only.

---

## 3. Functional Requirements

Numbered for traceability into ADRs, backlog, and test plans.

### 3.1 Identity Service

- **FR-ID-01** Local credential registration with Argon2id password hashing.
- **FR-ID-02** Federated login via OIDC (Google, Microsoft, Okta) and SAML 2.0 (enterprise SSO).
- **FR-ID-03** Issuance of short-lived (15 min) access tokens and rotating refresh tokens (7 day sliding).
- **FR-ID-04** Multi-factor authentication: TOTP (RFC 6238), WebAuthn/Passkeys, recovery codes.
- **FR-ID-05** SCIM 2.0 endpoints for enterprise user/group provisioning.
- **FR-ID-06** Account lifecycle: invite, activate, suspend, soft-delete (30-day grace), hard-delete.
- **FR-ID-07** Session management with revocation list backed by Redis (sub-second propagation).
- **FR-ID-08** Device/session inventory per user; remote sign-out per session.

### 3.2 Organization Service

- **FR-ORG-01** Hierarchical organizations: Tenant → Organization → Workspace → Project (configurable depth).
- **FR-ORG-02** Membership: a user can belong to N organizations with distinct role sets per org.
- **FR-ORG-03** Invitation workflow: email invite → accept → membership materialized.
- **FR-ORG-04** Org-level settings (branding, default roles, data residency choice).
- **FR-ORG-05** Org transfer/merge operations (rare but supported, audited).
- **FR-ORG-06** Custom domain mapping for white-label tenants (e.g., `acme.product.com`).

### 3.3 Permission Service

- **FR-PERM-01** RBAC: roles → permissions; roles assignable at tenant/org/workspace scopes.
- **FR-PERM-02** ABAC overlay: policy evaluation against subject, resource, action, environment attributes.
- **FR-PERM-03** Policy language: Rego (OPA) or Cedar — selected per ADR-007.
- **FR-PERM-04** `Check` API: < 10 ms P99 in-region via cached decisions.
- **FR-PERM-05** `BatchCheck` and `List` (which resources can subject X act on?) APIs.
- **FR-PERM-06** Custom roles per tenant; system-defined roles immutable.
- **FR-PERM-07** Permission inheritance along the org hierarchy with explicit deny.

### 3.4 Audit Service

- **FR-AUD-01** Append-only ledger of every privileged action (auth, permission change, billing, admin).
- **FR-AUD-02** Tamper-evidence via per-tenant Merkle-chained hash of records.
- **FR-AUD-03** Structured event schema (actor, action, resource, outcome, context, correlation_id).
- **FR-AUD-04** Search by tenant, actor, time range, action, resource (< 2 s P95 over 90-day window).
- **FR-AUD-05** Export to customer-managed S3 / Azure Blob (signed, scheduled, or on-demand).
- **FR-AUD-06** Retention policies per tenant tier (90 days standard, 7 years enterprise).
- **FR-AUD-07** Legal hold flag freezes deletion for specific tenants/scopes.

### 3.5 Notification Service

- **FR-NOT-01** Multi-channel: email (transactional), SMS, push, in-app, webhook.
- **FR-NOT-02** Template management with versioning, locale fallback, MJML compilation for email.
- **FR-NOT-03** User-level preferences and channel opt-outs (with regulatory enforcement).
- **FR-NOT-04** Delivery tracking: queued → sent → delivered → opened → bounced/failed.
- **FR-NOT-05** Rate limiting per recipient and per template (anti-spam, anti-flood).
- **FR-NOT-06** Provider abstraction: SendGrid/SES/Postmark for email, Twilio for SMS, FCM/APNs for push.
- **FR-NOT-07** Retry with exponential backoff + DLQ; manual replay tooling.

### 3.6 Billing Service

- **FR-BIL-01** Plans and pricing: flat, per-seat, tiered, metered, hybrid.
- **FR-BIL-02** Subscription lifecycle: trial → active → past_due → canceled → reactivated.
- **FR-BIL-03** Usage metering ingestion (idempotent, late-arriving event tolerant).
- **FR-BIL-04** Invoicing: monthly/annual cycles, prorations, credits, refunds.
- **FR-BIL-05** Payment processor integration via adapter pattern (Stripe primary, Adyen secondary).
- **FR-BIL-06** Dunning workflow: configurable retries, grace period, suspension trigger.
- **FR-BIL-07** Tax calculation via Stripe Tax / Avalara adapter.
- **FR-BIL-08** Entitlement materialization → Permission Service (a customer's plan grants features).

### 3.7 API Gateway

- **FR-GW-01** Route to backing services by path/host/header rules.
- **FR-GW-02** Terminate TLS; enforce TLS 1.3 inbound.
- **FR-GW-03** Validate JWTs (signature, exp, aud, iss); inject `X-Tenant-Id`, `X-User-Id`, `X-Trace-Id` to downstreams.
- **FR-GW-04** Per-tenant and per-user rate limiting (Redis sliding window).
- **FR-GW-05** Request/response logging with PII redaction.
- **FR-GW-06** Circuit breaker and bulkhead per downstream.
- **FR-GW-07** API versioning via URL segment (`/v1/`, `/v2/`) with parallel running.

---

## 4. Non-Functional Requirements

### 4.1 Performance

| Metric | Target |
|---|---|
| Gateway request → response (P50) | < 80 ms |
| Gateway request → response (P95) | < 250 ms |
| Gateway request → response (P99) | < 600 ms |
| Permission check (cached) P99 | < 10 ms |
| Permission check (cold) P99 | < 60 ms |
| Event publish → consumer ack (P95) | < 2 s |
| Sustained throughput | 5,000 RPS at gateway, scalable horizontally |

### 4.2 Availability & Resilience

- **Tier-0 services (Gateway, Identity, Permission):** 99.95% monthly SLO.
- **Tier-1 services (Organization, Audit-write, Billing):** 99.9% monthly SLO.
- **Tier-2 services (Notification, Audit-search):** 99.5% monthly SLO.
- **Multi-AZ active-active** within a region; multi-region active-passive (DR target ≤ 30 min RTO).
- **Graceful degradation:** Permission Service falls back to last-known-good cached decisions on policy store outage (max 5 min staleness, surfaced via header).

### 4.3 Security

- Encryption in transit (TLS 1.3); encryption at rest (AES-256, KMS-managed keys).
- Secrets via HashiCorp Vault or cloud KMS — never in environment variables of long-lived processes.
- OWASP ASVS Level 2 compliance.
- Quarterly penetration tests; continuous SAST/DAST in CI.

### 4.4 Scalability

- Horizontal scaling on all services; stateless where possible.
- PostgreSQL: read replicas for query-heavy services (Audit, Permission read model).
- RabbitMQ: clustered with quorum queues, mirrored across 3 nodes.
- Redis: cluster mode with sharding for high-cardinality keys (sessions, rate limits).

### 4.5 Observability

- 100% of inter-service calls traced via W3C Trace Context.
- RED metrics (Rate, Errors, Duration) per endpoint; USE metrics (Utilization, Saturation, Errors) per node.
- Structured logs (JSON) with `trace_id`, `tenant_id`, `correlation_id` on every line.
- SLO burn-rate alerts (fast/slow burn) routed to PagerDuty.

### 4.6 Maintainability

- Each service ≤ 50k LoC; single team ownership.
- API contracts as code (OpenAPI 3.1, Protobuf); breaking changes require new major version.
- Trunk-based development; feature flags via the platform's own flagging primitives.

### 4.7 Compliance

- GDPR: right-to-access, right-to-erasure, data portability — implemented via DSR workflow spanning Identity, Organization, Audit, Notification, Billing.
- Data residency: tenants can pin storage to EU, US, or APAC clusters (cell-based isolation).
- Audit immutability: 7-year retention for regulated tiers.

---

## 5. Domain Model

The domain model is expressed as DDD aggregates. Each aggregate is the consistency boundary; references across aggregates are by ID only.

### 5.1 Core Aggregates

```
┌──────────────────── Identity Context ────────────────────┐
│ User (Aggregate Root)                                    │
│   ├─ UserId (Identity)                                   │
│   ├─ Email (VO, unique global)                           │
│   ├─ PasswordCredential (VO, optional)                   │
│   ├─ MfaMethods (Collection<MfaMethod>)                  │
│   ├─ Sessions (Collection<Session>) — bounded by 50      │
│   └─ Status (Enum: Pending|Active|Suspended|Deleted)     │
│                                                          │
│ FederatedIdentity (Aggregate Root)                       │
│   ├─ Provider (Google|Microsoft|SAML:<entityId>)         │
│   ├─ Subject                                             │
│   └─ UserId (FK to User)                                 │
└──────────────────────────────────────────────────────────┘

┌──────────────────── Organization Context ────────────────┐
│ Tenant (Aggregate Root)                                  │
│   ├─ TenantId                                            │
│   ├─ DisplayName, Slug, Region                           │
│   └─ Settings (VO)                                       │
│                                                          │
│ Organization (Aggregate Root)                            │
│   ├─ OrgId, TenantId                                     │
│   ├─ ParentOrgId (nullable, for hierarchy)               │
│   ├─ Memberships (Collection<Membership>)                │
│   └─ Invitations (Collection<Invitation>)                │
│                                                          │
│ Workspace (Aggregate Root, child of Org)                 │
│   └─ Settings, Members (subset of Org members)           │
└──────────────────────────────────────────────────────────┘

┌──────────────────── Permission Context ──────────────────┐
│ Role (Aggregate Root)                                    │
│   ├─ RoleId, TenantId (or NULL = system role)            │
│   ├─ Permissions (Collection<PermissionGrant>)           │
│   └─ Scope (Tenant|Org|Workspace)                        │
│                                                          │
│ Assignment (Aggregate Root)                              │
│   ├─ SubjectId (UserId or GroupId)                       │
│   ├─ RoleId                                              │
│   ├─ ResourceScope (URN)                                 │
│   └─ ValidFrom/ValidUntil                                │
│                                                          │
│ Policy (Aggregate Root) — ABAC                           │
│   └─ Cedar/Rego document, version, tenant scope          │
└──────────────────────────────────────────────────────────┘

┌──────────────────── Audit Context ───────────────────────┐
│ AuditEvent (Aggregate, immutable)                        │
│   ├─ EventId, TenantId, OccurredAt                       │
│   ├─ Actor (VO: type, id, ip, ua)                        │
│   ├─ Action (VO: domain.action.verb)                     │
│   ├─ Resource (VO: urn)                                  │
│   ├─ Outcome (Success|Failure|Denied)                    │
│   ├─ ContextJson                                         │
│   └─ PrevHash, Hash (Merkle chain)                       │
└──────────────────────────────────────────────────────────┘

┌──────────────────── Notification Context ────────────────┐
│ NotificationTemplate (Aggregate Root)                    │
│   ├─ Key, Version, Channel, Locale                       │
│   └─ Body, Subject, Variables                            │
│                                                          │
│ NotificationDispatch (Aggregate Root)                    │
│   ├─ DispatchId, TenantId, RecipientId                   │
│   ├─ Channel, TemplateKey                                │
│   ├─ Payload (rendered)                                  │
│   ├─ Attempts (Collection<DeliveryAttempt>)              │
│   └─ TerminalState (Delivered|Failed|Suppressed)         │
│                                                          │
│ UserPreference (Aggregate Root)                          │
│   └─ Channel × Category → Opt-in/Opt-out                 │
└──────────────────────────────────────────────────────────┘

┌──────────────────── Billing Context ─────────────────────┐
│ Plan (Aggregate Root)                                    │
│   ├─ PlanId, Code, Name                                  │
│   ├─ PricingComponents (Flat|PerSeat|Tiered|Metered)     │
│   └─ Entitlements (Feature flags + limits)               │
│                                                          │
│ Subscription (Aggregate Root)                            │
│   ├─ SubscriptionId, TenantId, PlanId                    │
│   ├─ Status, CurrentPeriodStart/End                      │
│   ├─ TrialEndsAt, CanceledAt                             │
│   └─ Items (Collection<SubscriptionItem>)                │
│                                                          │
│ UsageRecord (Value Object stream)                        │
│   └─ SubscriptionItemId, Quantity, Timestamp, IdemKey    │
│                                                          │
│ Invoice (Aggregate Root)                                 │
│   ├─ InvoiceId, SubscriptionId                           │
│   ├─ LineItems, Subtotal, Tax, Total                     │
│   ├─ Status (Draft|Open|Paid|Void|Uncollectible)         │
│   └─ Payments (Collection<PaymentAttempt>)               │
└──────────────────────────────────────────────────────────┘
```

### 5.2 Universal Resource Names (URNs)

Every addressable resource carries a URN of the form:

```
urn:nexus:<service>:<region>:<tenant>:<resource-type>:<id>
e.g.,
urn:nexus:org:eu-west-1:tnt_01HX...:workspace:wks_01HY...
urn:nexus:billing:us-east-1:tnt_01HX...:subscription:sub_01HZ...
```

URNs are stable, opaque to clients, and form the canonical identifier in Permission and Audit contexts.

---

## 6. Bounded Contexts

### 6.1 Context Map

```
                                ┌──────────────────┐
                                │   API Gateway    │  (anti-corruption + ingress)
                                └────────┬─────────┘
                                         │
        ┌─────────────────┬──────────────┼──────────────┬──────────────┬──────────────┐
        ▼                 ▼              ▼              ▼              ▼              ▼
   ┌─────────┐      ┌──────────┐  ┌────────────┐  ┌────────┐   ┌────────────┐  ┌─────────┐
   │Identity │      │   Org    │  │ Permission │  │ Audit  │   │Notification│  │ Billing │
   └────┬────┘      └────┬─────┘  └─────┬──────┘  └───┬────┘   └─────┬──────┘  └────┬────┘
        │                │              │             │              │              │
        │ user.*         │ org.*        │ perm.*      │ (sink only)  │ notif.*      │ billing.*
        ▼                ▼              ▼             ▼              ▼              ▼
   ════════════════════════ RabbitMQ Event Backbone (topic exchange) ══════════════════
```

### 6.2 Context Relationships

| Upstream | Downstream | Relationship | Pattern |
|---|---|---|---|
| Identity | Organization | Customer/Supplier | Publishes `user.created`, `user.deleted` → Org materializes membership candidates |
| Identity | Permission | Conformist | Permission consumes user lifecycle to invalidate caches |
| Organization | Permission | Customer/Supplier | `org.member.added` triggers default role assignment |
| Organization | Billing | Partnership | Seat counts derived from membership; bidirectional via `billing.seat.limit.exceeded` |
| Billing | Permission | Customer/Supplier | `subscription.activated` publishes entitlements → Permission materializes feature gates |
| All | Audit | Open Host Service | Every service emits `audit.event.recorded` via outbox |
| All | Notification | Open Host Service | Services publish domain events; Notification subscribes per template subscription |

### 6.3 Ubiquitous Language (excerpt)

- **Tenant**: the billable customer entity; the top of the hierarchy.
- **Organization**: a unit within a tenant (e.g., department); has members.
- **Workspace**: collaboration unit within an organization (e.g., a project team).
- **Principal**: any authenticatable subject (user, service account, API key).
- **Permission**: an atomic verb (`org:read`, `billing:invoice:void`).
- **Role**: a named bundle of permissions.
- **Entitlement**: a permission/limit granted by an active subscription.
- **Dispatch**: a single notification send attempt across one channel to one recipient.

---

## 7. Service Responsibilities

### 7.1 API Gateway (YARP-based)

**Owns:**
- TLS termination, HTTP/2 + HTTP/3 (QUIC) ingress.
- JWT validation against Identity's JWKS endpoint (cached, refreshed every 10 min).
- Tenant resolution (from host header, JWT claim, or path).
- Rate limiting (per IP, per user, per tenant) using Redis sliding-window counters.
- Request shaping: header normalization, PII redaction in logs, correlation ID stamping.
- Routing to downstream services via service discovery (Kubernetes DNS or Consul).
- Response transformation for legacy clients (version shims).

**Does not own:**
- Business logic. The gateway never reads/writes domain data.
- Authentication (delegates to Identity service for token issuance).

**Scaling:** Stateless; horizontal pod autoscaler on CPU + RPS. Target 10k concurrent connections per pod.

### 7.2 Identity Service

**Owns:**
- User account aggregate; credential storage; MFA enrollment and verification.
- OIDC Authorization Server endpoints (`/authorize`, `/token`, `/userinfo`, `/.well-known/jwks.json`).
- SAML SP and IdP-initiated flows.
- SCIM 2.0 endpoints (`/Users`, `/Groups`).
- Session store (Redis-backed for hot reads; Postgres for durable history).
- Token signing keys (asymmetric, rotated quarterly via KMS).

**Publishes events:**
- `identity.user.registered`
- `identity.user.email_verified`
- `identity.user.mfa_enrolled`
- `identity.user.suspended`
- `identity.user.deleted`
- `identity.session.terminated`

**Consumes events:** None (root of the user lifecycle).

### 7.3 Organization Service

**Owns:**
- Tenant, Organization, Workspace, Membership, Invitation aggregates.
- Custom domain registry and verification (DNS TXT challenge).
- Org-level configuration (branding, default roles, region pin).

**Publishes events:**
- `org.tenant.created` / `org.tenant.region_locked`
- `org.organization.created` / `.renamed` / `.deleted`
- `org.member.invited` / `.joined` / `.removed` / `.role_changed`
- `org.workspace.created`

**Consumes events:**
- `identity.user.deleted` → cascade remove memberships.
- `billing.subscription.canceled` → freeze tenant (read-only mode).

### 7.4 Permission Service

**Owns:**
- Role definitions (system + custom per tenant).
- Policy documents (Cedar) versioned per tenant.
- Assignments and effective-permission read model.
- Decision cache (Redis) keyed by `(principal, action, resource, policy_version)`.

**Exposes:**
- `Check(principal, action, resource, context) → Allow|Deny|Indeterminate`
- `BatchCheck(items[]) → result[]`
- `List(principal, action) → resource[]` (reverse index lookup)
- Admin CRUD for roles and policies.

**Publishes events:**
- `perm.role.created` / `.updated` / `.deleted`
- `perm.assignment.granted` / `.revoked`
- `perm.policy.published`

**Consumes events:**
- `identity.user.*`, `org.member.*` → recompute affected effective permissions.
- `billing.subscription.activated` / `.changed` → refresh entitlements.

### 7.5 Audit Service

**Owns:**
- Append-only event log partitioned by `(tenant_id, year_month)`.
- Per-tenant Merkle chain anchor + verifiable receipts.
- Search index (PostgreSQL GIN on JSONB + BRIN on time; promoted to OpenSearch for enterprise tier).
- Export pipeline (scheduled S3/Azure Blob delivery, signed manifests).

**Publishes events:**
- `audit.event.recorded` (low-volume, for downstream analytics).
- `audit.chain.anchored` (when a tenant's daily anchor is written to immutable storage).

**Consumes events:**
- `audit.*` from every other service via a wildcard subscription on the outbox-fed exchange.

### 7.6 Notification Service

**Owns:**
- Template catalog (versioned, Markdown/MJML source → rendered).
- Subscription rules (which events → which templates → which audiences).
- Dispatch queue, attempt history, DLQ.
- User preferences and quiet hours.
- Provider adapters (Email: SendGrid/SES; SMS: Twilio; Push: FCM/APNs; Webhook: HTTP signer).

**Publishes events:**
- `notification.dispatch.created`
- `notification.dispatch.delivered` / `.failed` / `.bounced`

**Consumes events:**
- Subscriber to a configurable set of domain events from all services.

### 7.7 Billing Service

**Owns:**
- Plan catalog, Subscription, Invoice, UsageRecord aggregates.
- Idempotent meter ingest endpoint.
- Pricing engine (rate-cards, prorations, credits).
- Payment processor adapters (Stripe primary).
- Dunning state machine.
- Entitlement projection (Plan + Subscription → feature/limit set).

**Publishes events:**
- `billing.subscription.created` / `.activated` / `.changed` / `.canceled`
- `billing.invoice.issued` / `.paid` / `.failed`
- `billing.usage.recorded`
- `billing.entitlement.updated`

**Consumes events:**
- `org.member.joined` / `.removed` → seat count adjustments.
- `org.tenant.created` → provision default trial subscription.

---

## 8. Database Design

### 8.1 Strategy

**Database-per-Service.** Each service owns a dedicated PostgreSQL logical database within a shared physical cluster (early stage) or a dedicated cluster per service (enterprise stage). No cross-service joins; cross-context queries materialize via events.

### 8.2 Identity Schema (`identity_db`)

```sql
CREATE TABLE users (
    user_id         UUID PRIMARY KEY,
    email           CITEXT NOT NULL UNIQUE,
    email_verified  BOOLEAN NOT NULL DEFAULT FALSE,
    status          TEXT NOT NULL CHECK (status IN ('pending','active','suspended','deleted')),
    password_hash   TEXT,           -- nullable: federated-only users
    password_algo   TEXT,           -- 'argon2id'
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at      TIMESTAMPTZ
);
CREATE INDEX users_status_idx ON users(status) WHERE status <> 'deleted';

CREATE TABLE mfa_methods (
    method_id   UUID PRIMARY KEY,
    user_id     UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    kind        TEXT NOT NULL,      -- 'totp','webauthn','recovery'
    secret_enc  BYTEA NOT NULL,     -- envelope-encrypted via KMS DEK
    metadata    JSONB NOT NULL DEFAULT '{}',
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE federated_identities (
    fed_id      UUID PRIMARY KEY,
    user_id     UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    provider    TEXT NOT NULL,
    subject     TEXT NOT NULL,
    UNIQUE (provider, subject)
);

CREATE TABLE sessions (
    session_id    UUID PRIMARY KEY,
    user_id       UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    issued_at     TIMESTAMPTZ NOT NULL,
    expires_at    TIMESTAMPTZ NOT NULL,
    refresh_hash  TEXT NOT NULL,
    user_agent    TEXT,
    ip_inet       INET,
    revoked_at    TIMESTAMPTZ
);
CREATE INDEX sessions_user_active_idx ON sessions(user_id) WHERE revoked_at IS NULL;

CREATE TABLE outbox (
    id              BIGSERIAL PRIMARY KEY,
    occurred_at     TIMESTAMPTZ NOT NULL DEFAULT now(),
    aggregate_id    UUID NOT NULL,
    event_type      TEXT NOT NULL,
    payload         JSONB NOT NULL,
    headers         JSONB NOT NULL,
    published_at    TIMESTAMPTZ
);
CREATE INDEX outbox_unpublished_idx ON outbox(id) WHERE published_at IS NULL;
```

### 8.3 Organization Schema (`org_db`) — RLS Enabled

```sql
CREATE TABLE tenants (
    tenant_id   UUID PRIMARY KEY,
    slug        TEXT NOT NULL UNIQUE,
    region      TEXT NOT NULL,
    status      TEXT NOT NULL,
    settings    JSONB NOT NULL DEFAULT '{}',
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE organizations (
    org_id      UUID PRIMARY KEY,
    tenant_id   UUID NOT NULL REFERENCES tenants(tenant_id),
    parent_org_id UUID REFERENCES organizations(org_id),
    name        TEXT NOT NULL,
    slug        TEXT NOT NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (tenant_id, slug)
);

CREATE TABLE memberships (
    membership_id UUID PRIMARY KEY,
    tenant_id     UUID NOT NULL,
    org_id        UUID NOT NULL REFERENCES organizations(org_id) ON DELETE CASCADE,
    user_id       UUID NOT NULL,
    role_ids      UUID[] NOT NULL,   -- denormalized for fast permission joins
    joined_at     TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (org_id, user_id)
);

-- Row-Level Security
ALTER TABLE organizations ENABLE ROW LEVEL SECURITY;
ALTER TABLE memberships  ENABLE ROW LEVEL SECURITY;

CREATE POLICY tenant_isolation_orgs ON organizations
    USING (tenant_id = current_setting('app.current_tenant')::uuid);

CREATE POLICY tenant_isolation_members ON memberships
    USING (tenant_id = current_setting('app.current_tenant')::uuid);
```

Every connection-pool checkout sets `SET LOCAL app.current_tenant = '<uuid>'` from the JWT claim. The platform's EF Core interceptor enforces this — see §11.

### 8.4 Permission Schema (`permission_db`)

```sql
CREATE TABLE roles (
    role_id     UUID PRIMARY KEY,
    tenant_id   UUID,            -- NULL = system role
    code        TEXT NOT NULL,
    name        TEXT NOT NULL,
    scope       TEXT NOT NULL,   -- 'tenant'|'org'|'workspace'
    UNIQUE (tenant_id, code)
);

CREATE TABLE role_permissions (
    role_id     UUID REFERENCES roles(role_id) ON DELETE CASCADE,
    permission  TEXT NOT NULL,   -- 'org:read', 'billing:invoice:void'
    PRIMARY KEY (role_id, permission)
);

CREATE TABLE assignments (
    assignment_id UUID PRIMARY KEY,
    tenant_id     UUID NOT NULL,
    subject_id    UUID NOT NULL,
    subject_type  TEXT NOT NULL,   -- 'user'|'group'|'service'
    role_id       UUID NOT NULL REFERENCES roles(role_id),
    resource_urn  TEXT NOT NULL,
    valid_from    TIMESTAMPTZ NOT NULL DEFAULT now(),
    valid_until   TIMESTAMPTZ
);
CREATE INDEX assignments_lookup_idx
    ON assignments (tenant_id, subject_id, resource_urn);

CREATE TABLE policies (
    policy_id   UUID PRIMARY KEY,
    tenant_id   UUID,
    version     INT NOT NULL,
    cedar_text  TEXT NOT NULL,
    published_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Materialized read model (refreshed by event consumer)
CREATE MATERIALIZED VIEW effective_permissions AS
SELECT a.tenant_id, a.subject_id, rp.permission, a.resource_urn
FROM assignments a
JOIN role_permissions rp ON rp.role_id = a.role_id
WHERE a.valid_until IS NULL OR a.valid_until > now();
CREATE INDEX eff_perm_idx ON effective_permissions (tenant_id, subject_id, permission);
```

### 8.5 Audit Schema (`audit_db`) — Partitioned

```sql
CREATE TABLE audit_events (
    event_id      UUID NOT NULL,
    tenant_id     UUID NOT NULL,
    occurred_at   TIMESTAMPTZ NOT NULL,
    actor_type    TEXT NOT NULL,
    actor_id      TEXT NOT NULL,
    actor_ip      INET,
    action        TEXT NOT NULL,
    resource_urn  TEXT NOT NULL,
    outcome       TEXT NOT NULL,
    context       JSONB NOT NULL DEFAULT '{}',
    prev_hash     BYTEA,
    hash          BYTEA NOT NULL,
    PRIMARY KEY (tenant_id, occurred_at, event_id)
) PARTITION BY RANGE (occurred_at);

-- Monthly partitions created by pg_partman
CREATE INDEX audit_actor_idx ON audit_events (tenant_id, actor_id, occurred_at DESC);
CREATE INDEX audit_action_idx ON audit_events (tenant_id, action, occurred_at DESC);
CREATE INDEX audit_context_gin ON audit_events USING GIN (context jsonb_path_ops);

CREATE TABLE chain_anchors (
    tenant_id    UUID NOT NULL,
    anchor_date  DATE NOT NULL,
    root_hash    BYTEA NOT NULL,
    anchored_to  TEXT,            -- e.g., 'aws:qldb:ledger:...'
    PRIMARY KEY (tenant_id, anchor_date)
);
```

### 8.6 Notification Schema (`notification_db`)

```sql
CREATE TABLE templates (
    key         TEXT NOT NULL,
    version     INT NOT NULL,
    channel     TEXT NOT NULL,
    locale      TEXT NOT NULL,
    subject     TEXT,
    body        TEXT NOT NULL,
    variables   JSONB NOT NULL,
    PRIMARY KEY (key, version, channel, locale)
);

CREATE TABLE dispatches (
    dispatch_id  UUID PRIMARY KEY,
    tenant_id    UUID NOT NULL,
    recipient_id UUID NOT NULL,
    channel      TEXT NOT NULL,
    template_key TEXT NOT NULL,
    payload      JSONB NOT NULL,
    status       TEXT NOT NULL,
    created_at   TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at   TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE delivery_attempts (
    attempt_id   UUID PRIMARY KEY,
    dispatch_id  UUID NOT NULL REFERENCES dispatches(dispatch_id) ON DELETE CASCADE,
    attempted_at TIMESTAMPTZ NOT NULL,
    provider     TEXT NOT NULL,
    response     JSONB NOT NULL,
    outcome      TEXT NOT NULL
);

CREATE TABLE preferences (
    user_id   UUID NOT NULL,
    tenant_id UUID NOT NULL,
    channel   TEXT NOT NULL,
    category  TEXT NOT NULL,
    opted_in  BOOLEAN NOT NULL,
    PRIMARY KEY (user_id, tenant_id, channel, category)
);
```

### 8.7 Billing Schema (`billing_db`)

```sql
CREATE TABLE plans (
    plan_id    UUID PRIMARY KEY,
    code       TEXT NOT NULL UNIQUE,
    name       TEXT NOT NULL,
    status     TEXT NOT NULL,
    components JSONB NOT NULL,        -- pricing rules
    entitlements JSONB NOT NULL,
    version    INT NOT NULL
);

CREATE TABLE subscriptions (
    subscription_id UUID PRIMARY KEY,
    tenant_id       UUID NOT NULL,
    plan_id         UUID NOT NULL REFERENCES plans(plan_id),
    status          TEXT NOT NULL,
    current_period_start TIMESTAMPTZ NOT NULL,
    current_period_end   TIMESTAMPTZ NOT NULL,
    trial_ends_at   TIMESTAMPTZ,
    canceled_at     TIMESTAMPTZ,
    metadata        JSONB NOT NULL DEFAULT '{}'
);

CREATE TABLE subscription_items (
    item_id         UUID PRIMARY KEY,
    subscription_id UUID NOT NULL REFERENCES subscriptions(subscription_id) ON DELETE CASCADE,
    feature_code    TEXT NOT NULL,
    quantity        NUMERIC(18,4) NOT NULL,
    unit_price      NUMERIC(18,4) NOT NULL,
    currency        CHAR(3) NOT NULL
);

CREATE TABLE usage_records (
    usage_id        UUID PRIMARY KEY,
    item_id         UUID NOT NULL REFERENCES subscription_items(item_id),
    recorded_at     TIMESTAMPTZ NOT NULL,
    quantity        NUMERIC(18,4) NOT NULL,
    idempotency_key TEXT NOT NULL,
    UNIQUE (item_id, idempotency_key)
) PARTITION BY RANGE (recorded_at);

CREATE TABLE invoices (
    invoice_id   UUID PRIMARY KEY,
    subscription_id UUID NOT NULL,
    tenant_id    UUID NOT NULL,
    period_start TIMESTAMPTZ NOT NULL,
    period_end   TIMESTAMPTZ NOT NULL,
    subtotal     NUMERIC(18,4) NOT NULL,
    tax          NUMERIC(18,4) NOT NULL,
    total        NUMERIC(18,4) NOT NULL,
    currency     CHAR(3) NOT NULL,
    status       TEXT NOT NULL,
    issued_at    TIMESTAMPTZ
);
```

### 8.8 Cross-Cutting Tables

Every service has:
- `outbox` (as in §8.2) — transactional event publication.
- `inbox` — deduplication of incoming events.
- `migration_history` — Flyway/EF migrations record.

---

## 9. Event-Driven Architecture

### 9.1 Topology

RabbitMQ is structured around **one topic exchange per bounded context**, plus a global `nx.events` topic exchange for cross-context broadcasts. Consumers bind quorum queues with routing keys.

```
Exchanges (topic, durable):
  nx.identity      — routing: identity.<entity>.<event>
  nx.org           — routing: org.<entity>.<event>
  nx.permission    — routing: perm.<entity>.<event>
  nx.audit         — routing: audit.<entity>.<event>
  nx.notification  — routing: notification.<entity>.<event>
  nx.billing       — routing: billing.<entity>.<event>
  nx.events        — fanout-like global re-publish (for Audit & Notification subscribers)

Queues (quorum, durable, per consumer service):
  permission.consumer.identity   binds nx.identity   #
  permission.consumer.org        binds nx.org        org.member.*
  permission.consumer.billing    binds nx.billing    billing.subscription.* billing.entitlement.*
  audit.ingest                   binds nx.events     #
  notification.dispatcher        binds nx.events     <configurable per template>
  billing.consumer.org           binds nx.org        org.member.* org.tenant.created
```

### 9.2 Event Envelope (CloudEvents 1.0)

```json
{
  "specversion": "1.0",
  "id": "evt_01HZ...",
  "source": "/services/identity",
  "type": "identity.user.registered",
  "subject": "user/usr_01HX...",
  "time": "2026-06-04T10:32:11.421Z",
  "datacontenttype": "application/json",
  "tenantid": "tnt_01HX...",
  "traceparent": "00-<trace>-<span>-01",
  "correlationid": "corr_01HY...",
  "schemaversion": "1",
  "data": {
    "userId": "usr_01HX...",
    "email": "alice@example.com",
    "createdAt": "2026-06-04T10:32:11Z"
  }
}
```

### 9.3 Outbox Pattern (Producers)

Every state-changing command writes domain state and outbox row in the **same DB transaction**. A background `OutboxRelay` (one per service replica, leader-elected via Postgres advisory lock to prevent duplicate publish) polls unpublished rows, publishes to RabbitMQ with publisher confirms, and stamps `published_at`.

```csharp
// Application layer (simplified)
public async Task<Result> Handle(RegisterUser cmd, CancellationToken ct)
{
    await using var tx = await _db.BeginTransactionAsync(ct);
    var user = User.Create(cmd.Email, cmd.Password);
    _db.Users.Add(user);
    _db.Outbox.Add(OutboxMessage.From(
        new UserRegistered(user.Id, user.Email, user.CreatedAt),
        traceContext: _trace.Current));
    await _db.SaveChangesAsync(ct);
    await tx.CommitAsync(ct);
    return Result.Ok();
}
```

### 9.4 Inbox Pattern (Consumers)

Each consumer records `(event_id, consumer_name)` on first processing, in the same DB transaction as the side-effect. Re-delivery is detected and acknowledged without re-applying.

### 9.5 Delivery Guarantees

- **Publisher → broker:** confirmed (RabbitMQ publisher confirms + outbox); at-least-once.
- **Broker → consumer:** manual ack after DB commit; at-least-once with inbox-based dedupe → **exactly-once effective**.
- **Ordering:** preserved per `subject` via single-active-consumer queues only where needed (e.g., per-user session events). Most flows are commutative.

### 9.6 Sagas / Choreographed Workflows

**Example — Tenant Onboarding Saga:**

```
1. User signs up         → identity.user.registered
2. Org bootstraps tenant ← consumes → org.tenant.created
3. Billing trial         ← consumes org.tenant.created → billing.subscription.created (trial)
4. Permission defaults   ← consumes org.tenant.created → grants owner role to first user
5. Notification welcome  ← consumes identity.user.registered → email dispatched
```

Compensating actions (e.g., trial creation fails → tenant marked `provisioning_failed`) are encoded as event consumers — no central orchestrator.

### 9.7 Dead-Letter Handling

Each consumer queue has a paired DLX (dead-letter exchange) and DLQ. Messages exceeding `max_retries=5` with exponential backoff (via per-message TTL on a delay queue) land in DLQ. The platform exposes a **Replay UI** in the operator console for selective re-dispatch after fixes.

---

## 10. API Specifications

### 10.1 Conventions

- REST/JSON over HTTP/2 externally; gRPC internally between services where chatter dominates (e.g., Permission `Check`).
- Resource paths: `/v1/<resource-plural>/{id}`; sub-resources nested ≤ 2 levels.
- Timestamps: RFC 3339 UTC.
- IDs: 26-char ULID (sortable, URL-safe), prefixed by type (`usr_`, `org_`, `sub_`).
- Pagination: cursor-based (`?cursor=<opaque>&limit=50`), `next_cursor` in response.
- Errors: RFC 7807 Problem Details, with `code`, `traceId`, `tenantId`.
- Idempotency: clients send `Idempotency-Key` header on POST/PATCH for non-idempotent operations; servers persist for 24 h.

### 10.2 Identity Service (excerpt)

```yaml
openapi: 3.1.0
info: { title: Nexus Identity API, version: 1.0.0 }
paths:
  /v1/users:
    post:
      summary: Register user
      requestBody: { $ref: '#/components/requestBodies/Register' }
      responses:
        '201': { $ref: '#/components/responses/User' }
        '409': { description: Email exists }

  /v1/auth/token:
    post:
      summary: OAuth2 token endpoint (grant_type=password|refresh_token|authorization_code)
      responses:
        '200':
          content:
            application/json:
              schema:
                properties:
                  access_token:  { type: string }
                  refresh_token: { type: string }
                  token_type:    { const: Bearer }
                  expires_in:    { type: integer }

  /v1/auth/mfa/totp/enroll: { post: { ... } }
  /v1/auth/mfa/totp/verify: { post: { ... } }
  /v1/sessions:             { get: ..., delete: ... }

  /scim/v2/Users:           { get: ..., post: ... }
  /scim/v2/Groups:          { get: ..., post: ... }

  /.well-known/openid-configuration: { get: ... }
  /.well-known/jwks.json:            { get: ... }
```

### 10.3 Permission Service — Internal gRPC

```proto
service PermissionService {
  rpc Check       (CheckRequest)      returns (CheckResponse);
  rpc BatchCheck  (BatchCheckRequest) returns (BatchCheckResponse);
  rpc ListAllowed (ListRequest)       returns (ListResponse);
}

message CheckRequest {
  string tenant_id    = 1;
  string principal_id = 2;
  string action       = 3;
  string resource_urn = 4;
  map<string, Value> context = 5;
}

message CheckResponse {
  Decision decision     = 1;  // ALLOW | DENY | INDETERMINATE
  string  policy_version = 2;
  string  reason         = 3;
  google.protobuf.Duration cache_ttl = 4;
}
```

### 10.4 Billing Service (excerpt)

```yaml
paths:
  /v1/subscriptions:
    post:
      summary: Create subscription
      responses: { '201': { ... } }
  /v1/subscriptions/{id}:
    get:    { ... }
    patch:  { summary: Change plan / quantity, supports proration }
    delete: { summary: Cancel (at period end by default) }

  /v1/usage:
    post:
      summary: Record usage event
      parameters:
        - in: header
          name: Idempotency-Key
          required: true
          schema: { type: string }

  /v1/invoices:
    get:  { ... }
  /v1/invoices/{id}:
    get:  { ... }
  /v1/invoices/{id}/pay: { post: { ... } }
```

---

## 11. Security Architecture

### 11.1 Identity & Tokens

- **Access token (JWT):** RS256-signed by Identity using KMS-managed keys. Claims: `sub`, `tid` (tenant), `aud`, `iss`, `exp` (15 min), `iat`, `jti`, `amr` (auth methods), `scope`, `entitlements_hash`. Compact (< 1 KB).
- **Refresh token:** opaque, 256-bit random, stored Redis + hashed in Postgres. Sliding 7-day; absolute 30-day max.
- **Service-to-service:** mTLS with SPIFFE IDs (e.g., `spiffe://nexus.cluster.local/ns/prod/sa/permission`) + short-lived (5 min) SVIDs issued by SPIRE.

### 11.2 Authorization Pipeline

```
[Client] → JWT → [Gateway: validate sig, exp, aud] →
   inject X-Tenant-Id, X-User-Id, X-Auth-Methods →
     [Service: ASP.NET Core authn middleware (PassThrough scheme)] →
       [Authorization middleware → PermissionClient.Check()] →
         [Decision cached in Redis, keyed by (sub,action,resource,policy_version)]
```

Resource-level checks use a declarative attribute:

```csharp
[HttpPatch("/v1/organizations/{orgId}")]
[RequiresPermission("org:update", resource: "org:{orgId}")]
public async Task<IActionResult> UpdateOrg(string orgId, UpdateOrgRequest req) { ... }
```

### 11.3 Data Protection

- **Encryption at rest:** PostgreSQL TDE via cloud provider (EBS/Disk encryption + cluster-level pgcrypto for column-level PII where needed).
- **Encryption in transit:** TLS 1.3 everywhere; mTLS for service mesh.
- **Field-level encryption:** PII columns (email recovery hints, phone, payment hints) encrypted via envelope encryption (KMS CMK → DEK → ciphertext).
- **Secrets:** HashiCorp Vault with Kubernetes auth method; secrets mounted via CSI driver, not env vars.
- **Key rotation:** signing keys quarterly, DEKs annually, refresh-token-store keys on suspected compromise.

### 11.4 Threat Mitigations

| Threat | Mitigation |
|---|---|
| Credential stuffing | Argon2id + per-IP and per-account rate limits + breach-password check (HIBP k-anonymity) |
| Token theft | Short-lived JWTs + refresh rotation + binding to user agent fingerprint |
| Privilege escalation | Permission Service is the *only* allow-deny authority; Cedar policies signed and versioned |
| Tenant data leakage | RLS + interceptor sets `app.current_tenant` on every connection checkout; integration test asserts cross-tenant queries return zero rows |
| Replay attacks | Idempotency-Key with 24 h TTL; outbox guarantees one-publish-per-commit |
| SSRF/Webhook abuse | Outbound webhooks egress through proxy with IP allowlist; payload signed (HMAC-SHA256) with per-tenant secret |
| Audit tampering | Per-tenant Merkle chain; daily root anchored to immutable store (AWS QLDB / signed S3 object lock) |

### 11.5 Compliance Controls Mapping

- **SOC 2 CC6 (Logical Access):** Identity + Permission.
- **SOC 2 CC7 (System Operations):** Audit + Observability.
- **GDPR Art. 17 (Erasure):** DSR orchestrator service consumes a `user.deletion_requested` event; each service implements a `DeleteSubjectData(userId)` consumer with a published completion confirmation.
- **GDPR Art. 30 (Records of Processing):** Audit service is the system of record.

---

## 12. Multi-Tenancy Strategy

### 12.1 Isolation Model

Nexus adopts a **hybrid model**:

| Tier | Isolation | Use Case |
|---|---|---|
| **Pooled** (default) | Shared DB, Row-Level Security | Free + Standard tenants; majority of customers |
| **Bridged** | Shared cluster, dedicated schema | Enterprise without strict isolation requirements |
| **Siloed** | Dedicated database (and optionally dedicated services) | Regulated / Enterprise+; data residency commitments |
| **Cell-based** | Dedicated cluster cell (compute + data) per residency region | EU vs. US vs. APAC pinning |

### 12.2 Tenant Resolution

Resolution order at gateway:
1. JWT claim `tid` (post-auth requests).
2. Host header → tenant slug map (white-label domains).
3. First path segment `/v1/t/{tenantSlug}/...` (rare, admin tools).

Resolved tenant is stamped as `X-Tenant-Id` header (signed with a gateway secret) and propagated to all downstreams.

### 12.3 Data-Layer Enforcement

EF Core interceptor (every service):

```csharp
public class TenantConnectionInterceptor : DbConnectionInterceptor
{
    public override async Task ConnectionOpenedAsync(DbConnection conn,
        ConnectionEndEventData ed, CancellationToken ct)
    {
        var tenantId = _tenantContext.CurrentTenantId
            ?? throw new InvalidOperationException("No tenant context");
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT set_config('app.current_tenant', @tid, true)";
        cmd.Parameters.Add(new NpgsqlParameter("tid", tenantId.ToString()));
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
```

Every query against an RLS-protected table is automatically scoped. **Defense-in-depth:** application-level filters also include `WHERE tenant_id = @tid` so an accidental privilege escalation in DB role does not breach isolation.

### 12.4 Routing to Cells

- Cells are named (e.g., `cell-eu-west-1`, `cell-us-east-1`).
- A small **Tenant Directory Service** (replicated globally, read-mostly) maps `tenant_id → cell`. Cached at the gateway with 5 min TTL.
- Cross-region writes prohibited; cross-region reads via deliberate APIs only.

### 12.5 Noisy-Neighbor Protection

- Per-tenant rate quotas at gateway (configurable per plan).
- Per-tenant connection pool ceilings in services (`Npgsql` reserved buckets).
- Per-tenant RabbitMQ priorities (low-tier tenants on lower-priority queues during contention).

---

## 13. Deployment Architecture

### 13.1 Topology (Production)

```
                       ┌───────────────────────────────────┐
                       │   CDN / WAF (CloudFront/AFD)      │
                       └──────────────┬────────────────────┘
                                      │
                       ┌──────────────▼────────────────────┐
                       │  Cell: cell-eu-west-1 (k8s)       │
                       │  ┌─────────────────────────────┐  │
                       │  │  Ingress (NGINX) → YARP GW  │  │
                       │  └────────────┬────────────────┘  │
                       │  ┌────────────▼────────────────┐  │
                       │  │  Service Mesh (Linkerd)     │  │
                       │  │   ┌──────┐ ┌──────┐ ┌─────┐ │  │
                       │  │   │ Idn  │ │ Org  │ │Perm │ │  │
                       │  │   └──────┘ └──────┘ └─────┘ │  │
                       │  │   ┌──────┐ ┌──────┐ ┌─────┐ │  │
                       │  │   │ Aud  │ │ Notf │ │Bill │ │  │
                       │  │   └──────┘ └──────┘ └─────┘ │  │
                       │  └─────────────────────────────┘  │
                       │  ┌─────────────────────────────┐  │
                       │  │  Stateful tier:             │  │
                       │  │   PG (StackGres/CNPG)       │  │
                       │  │   RabbitMQ cluster (3 nodes)│  │
                       │  │   Redis (Sentinel/Cluster)  │  │
                       │  └─────────────────────────────┘  │
                       └───────────────────────────────────┘
```

### 13.2 Container Strategy

- One image per service, multi-stage Dockerfile (SDK build → runtime). Distroless or chiseled Ubuntu base.
- Trivy + Grype scans in CI; fail on HIGH/CRITICAL CVEs.
- SBOM (CycloneDX) produced per build, attached to release artifact.
- Signed with cosign; admission policy (Kyverno) rejects unsigned images.

### 13.3 Kubernetes Resources (per service)

- `Deployment` with `minReplicas=3`, anti-affinity across nodes/zones.
- `HorizontalPodAutoscaler` on CPU + custom metric (RPS for gateway, queue depth for consumers).
- `PodDisruptionBudget` `minAvailable=2`.
- `NetworkPolicy` default-deny + explicit allow per dependency.
- `ServiceAccount` bound to SPIFFE identity.

### 13.4 Local Developer Experience

`docker compose` brings up the full stack:

```yaml
services:
  postgres:    { image: postgres:16, ... }
  rabbitmq:    { image: rabbitmq:3.13-management, ... }
  redis:       { image: redis:7.4-alpine, ... }
  jaeger:      { image: jaegertracing/all-in-one, ... }
  identity:    { build: ./services/identity, depends_on: [postgres, rabbitmq] }
  organization:{ build: ./services/organization, ... }
  permission:  { build: ./services/permission, ... }
  audit:       { build: ./services/audit, ... }
  notification:{ build: ./services/notification, ... }
  billing:     { build: ./services/billing, ... }
  gateway:     { build: ./services/gateway, ports: ["8080:8080"] }
```

Migrations run via init containers; seed data via `dotnet run --project tools/Seed`.

### 13.5 CI/CD Pipeline

```
PR open  → Lint + Unit tests + SAST + License scan
         → Build image (no push)
         → Integration tests (Testcontainers: PG, Rabbit, Redis)
         → Architecture tests (NetArchTest: layer boundaries)

Merge    → Build & push image (semver tag + commit SHA)
         → Helm chart bumped via GitOps PR (ArgoCD watches)
         → Deploy to `dev` (auto), `staging` (auto), `prod` (manual gate)

Prod     → Canary 5% → 25% → 100% via Argo Rollouts
         → SLO check between steps (error rate, P95 latency)
         → Automatic rollback on regression
```

### 13.6 Backup & DR

- PostgreSQL: continuous WAL archive to object storage + nightly base backup; PITR to any second in last 14 days. Restore drills monthly.
- RabbitMQ: quorum queues replicate across 3 nodes; messages survive node loss. Definitions backed up daily.
- Redis: ephemeral by design — what must survive (refresh tokens, sessions) is mirrored to Postgres on write.
- Cross-region DR: read replicas in DR region; failover runbook with RTO 30 min, RPO 5 min.

---

## 14. Observability Strategy

### 14.1 The Three Pillars (Unified via OpenTelemetry)

All services emit via the **OpenTelemetry SDK for .NET** → OTLP exporter → an in-cluster **OTel Collector** (gateway mode) → backend sinks.

```
[Service] ──OTLP──▶ [Collector Sidecar] ──OTLP──▶ [Collector Gateway]
                                                    ├── traces  → Tempo
                                                    ├── metrics → Prometheus (via remote_write)
                                                    └── logs    → Loki
                                                          │
                                                       Grafana
```

### 14.2 Tracing

- **W3C Trace Context** propagated through HTTP, gRPC, and RabbitMQ message headers (`traceparent`).
- Auto-instrumentation: ASP.NET Core, HttpClient, EF Core, Npgsql, StackExchange.Redis, MassTransit/RabbitMQ.
- Custom spans on outbox publish, inbox handle, permission check, policy compile.
- Sampling: head-based 10% for read endpoints, 100% for write endpoints + error-tail sampling at collector.

### 14.3 Metrics

Per service, exposed at `/metrics` (Prometheus format) **and** OTLP:

| Metric | Type | Labels |
|---|---|---|
| `http.server.request.duration` | histogram | route, method, status, tenant |
| `db.client.command.duration` | histogram | operation, table |
| `messaging.outbox.lag` | gauge | service |
| `messaging.consumer.handle.duration` | histogram | queue, event_type |
| `permission.check.duration` | histogram | cached(bool), decision |
| `billing.usage.ingest.count` | counter | feature, status |

### 14.4 Logs

- Serilog → OTLP log exporter. JSON output. Required fields: `timestamp`, `level`, `message`, `service.name`, `trace_id`, `span_id`, `tenant_id`, `correlation_id`.
- PII scrubber middleware redacts emails, tokens, payment hints before emission.
- Sensitive endpoints log only event/outcome, never payload.

### 14.5 SLOs and Alerting

| SLO | Target | Window | Alert |
|---|---|---|---|
| Gateway availability | 99.95% | 30 d | Fast-burn 14.4× → page in 5 min |
| Gateway P95 latency | < 250 ms | 30 d | Slow-burn 6× → ticket |
| Permission check P99 | < 10 ms (cached) | 30 d | Fast-burn → page |
| Outbox lag | < 30 s | 5 min rolling | > 60 s → page |
| Notification delivery success | > 98% | 24 h | < 95% → page |

Alerts route to PagerDuty; warnings to Slack; informational to Grafana annotation only.

### 14.6 Runbooks

Every alert links to a markdown runbook in the operator console. Runbooks describe: trigger, diagnostic queries (pre-canned in Grafana), known causes, remediation steps, escalation.

---

## 15. Architecture Decision Records (ADRs)

Each ADR follows: **Context → Decision → Status → Consequences → Alternatives Considered.** Below are the foundational ADRs; the full set lives under `docs/architecture/adr/`.

### ADR-001: Microservices Decomposition by Bounded Context

- **Status:** Accepted (2026-05-12)
- **Decision:** Decompose along DDD bounded contexts: Identity, Organization, Permission, Audit, Notification, Billing — each independently deployable, with private data store.
- **Consequences:** Higher operational surface (six services + gateway) vs. monolith; mitigated by shared platform libraries and templates. Independent scaling and team ownership unlocked.
- **Alternatives:** Modular monolith (rejected: blocks team scaling); finer split (rejected: Permission and Identity initially considered separate but kept together where consistency needs demanded — see ADR-006).

### ADR-002: PostgreSQL as Primary Datastore for All Services

- **Decision:** PostgreSQL 16 across the board. JSONB columns for flexible schemas (audit context, plan components). No NoSQL in v1.
- **Rationale:** Reduces operational diversity; ACID per service is sufficient; RLS supports multi-tenancy; JSONB handles flexibility cases without separate document store.
- **Consequences:** Audit search at very high scale (> 1B rows/tenant/year) may require promotion to OpenSearch — deferred.

### ADR-003: RabbitMQ for Integration Messaging

- **Decision:** RabbitMQ with quorum queues for durability; CloudEvents 1.0 envelope.
- **Alternatives considered:**
  - Kafka — rejected for v1 (operational cost too high for current volumes; replay model nice-to-have but achievable via outbox replay tool).
  - NATS JetStream — promising but less mature .NET tooling.
- **Reconsider trigger:** sustained > 50k msgs/sec or need for partitioned ordered logs.

### ADR-004: Outbox Pattern for Reliable Event Publication

- **Decision:** Every event publication goes through a service-local `outbox` table written in the same transaction as the state change; a relay publishes to RabbitMQ asynchronously with publisher confirms.
- **Rationale:** Avoids the dual-write problem; gives at-least-once delivery with idempotent consumers reaching exactly-once effective semantics.

### ADR-005: Cedar for ABAC Policies

- **Decision:** Cedar (vs. Rego/OPA) as the policy language.
- **Rationale:** Cedar's analyzability (formal verification of policies, "is policy A more permissive than B?") materially improves enterprise auditability. AWS-stewarded with managed .NET bindings available.
- **Consequences:** Smaller community than OPA; mitigated by Cedar's compact spec and our in-house policy authoring tooling.

### ADR-006: Identity and Sessions Co-located

- **Decision:** Sessions stored in Redis (hot) + Postgres (cold) inside the Identity service rather than a separate "Session Service".
- **Rationale:** Strong consistency between user-state changes and session revocation; latency-sensitive token verification path collapses cross-service hop.

### ADR-007: Row-Level Security as the Multi-Tenancy Default

- **Decision:** Postgres RLS, with application-level `tenant_id` predicate as defense-in-depth.
- **Alternatives:** Schema-per-tenant (rejected — migration cost at 10k tenants); DB-per-tenant (used only for siloed tier).
- **Risk addressed:** A single forgotten `WHERE tenant_id =` in a query still cannot return another tenant's rows.

### ADR-008: YARP for the API Gateway

- **Decision:** Custom gateway built on YARP rather than an off-the-shelf gateway (Kong, APISIX).
- **Rationale:** First-class .NET integration; can share types/code with services (auth, telemetry); avoids Lua scripting; OpenTelemetry instrumented natively.
- **Trade-off:** We own more gateway code; mitigated by YARP's maturity and our team's .NET skill depth.

### ADR-009: OpenTelemetry as the Sole Telemetry SDK

- **Decision:** No service-specific or vendor-specific SDKs. OTLP everywhere.
- **Rationale:** Vendor neutrality; one mental model; replaceable backends.

### ADR-010: ULIDs Over GUIDs/Sequences for Public IDs

- **Decision:** 26-char ULIDs, prefixed by entity type (`usr_`, `org_`).
- **Rationale:** Sortable by time, URL-safe, no central coordinator, opaque to clients.

### ADR-011: Cell-Based Multi-Region Isolation

- **Decision:** Per-region cells with a global Tenant Directory; cross-cell calls forbidden in v1.
- **Rationale:** Compliance (residency) + blast radius reduction. Accepts duplicated control-plane assets per cell.

### ADR-012: SPIFFE/SPIRE for Service Identity

- **Decision:** Service-to-service auth via SPIFFE IDs delivered as X.509 SVIDs (mTLS).
- **Rationale:** Standardized workload identity, short-lived credentials, integrates with service mesh.

### ADR-013: Stripe as Primary Payment Processor (Adapter Pattern)

- **Decision:** Stripe (primary) with an Adapter interface allowing Adyen/Braintree (secondary).
- **Rationale:** Time-to-market, tax engine quality, mature webhook reliability.

---

## 16. Development Roadmap

The roadmap is organized into four phases over 12 months. Each phase ends with a hardening sprint focused on observability, security review, and load testing.

### Phase 0 — Foundations (Weeks 0–4)

- Repo monorepo layout (`/services/*`, `/libs/*`, `/contracts/*`, `/deploy/*`).
- Shared libraries: `Nexus.Platform.Hosting`, `Nexus.Platform.Telemetry`, `Nexus.Platform.Messaging` (outbox + Rabbit + CloudEvents), `Nexus.Platform.MultiTenancy`, `Nexus.Platform.Auth`.
- Local stack via `docker compose`; Testcontainers-based integration test harness.
- CI pipeline (GitHub Actions or Azure DevOps): build, test, sign, scan.
- ADRs 001–005 ratified.

### Phase 1 — Walking Skeleton (Weeks 4–10)

- Identity Service: registration, email/password login, JWT issuance, JWKS endpoint, refresh tokens.
- API Gateway: routing, JWT validation, OTel tracing end-to-end.
- Audit Service: ingest endpoint + event consumer; basic search.
- Permission Service: RBAC only (no Cedar yet); seed system roles; `Check` API.
- Organization Service: tenants + single-level orgs + memberships.
- **Exit criteria:** A user can sign up → tenant created → organization created → role assigned → permission check enforced at gateway → action audited. All traces visible end-to-end in Grafana.

### Phase 2 — Productionization (Weeks 10–22)

- MFA (TOTP, then WebAuthn).
- OIDC federation (Google, Microsoft).
- Notification Service: email channel, templates, preferences.
- Billing Service: plans, trial subscriptions, Stripe integration, basic invoicing.
- Cedar policy engine integrated into Permission Service (ABAC overlay).
- RLS rollout to all services; cross-tenant breach tests added to CI.
- Kubernetes production deployment in single cell (`cell-eu-west-1`).
- SLOs defined and dashboards published.
- **GA Milestone (end of Phase 2): SOC 2 Type I evidence collection begins.**

### Phase 3 — Enterprise (Weeks 22–36)

- SAML SSO + SCIM provisioning.
- Custom roles, custom domains.
- Audit export pipeline + Merkle anchoring.
- Notification: SMS, push, webhooks.
- Billing: metered usage, tiered pricing, prorations, dunning.
- Multi-cell deployment (`cell-us-east-1`) + Tenant Directory Service.
- DSR (GDPR) orchestrator.
- **Exit criteria:** First enterprise design-partner onboarded against the platform.

### Phase 4 — Scale & Polish (Weeks 36–52)

- Permission Service: ListAllowed reverse-index optimization; sub-10ms P99 cached.
- Audit promotion to OpenSearch for tenants exceeding row thresholds.
- DR region failover drill completed.
- Chaos engineering rollout (Litmus): pod kill, broker partition, DB failover.
- SOC 2 Type II audit window completes.
- API v2 planning, deprecation policy published.
- **Exit:** Platform ready for general onboarding of internal product teams.

### Risk Register (top 5)

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Cedar tooling immaturity in .NET | Medium | Medium | Maintain Rego fallback path; contribute upstream |
| RabbitMQ throughput ceiling at scale | Low | High | Capacity model + Kafka migration plan as ADR-future |
| RLS misuse by service developers | Medium | High | Mandatory cross-tenant test suite; lint rule banning raw SQL |
| Stripe outage during peak invoicing | Low | High | Adyen adapter ready; idempotent retry; backoff with operator alert |
| OTel collector bottleneck | Low | Medium | Sharded collector deployment; sampling tuned at source |

---

## Appendix A — Repository Layout

```
/contracts/                # OpenAPI + Protobuf, the source of truth
/libs/
  Nexus.Platform.Hosting/
  Nexus.Platform.Telemetry/
  Nexus.Platform.Messaging/
  Nexus.Platform.MultiTenancy/
  Nexus.Platform.Auth/
  Nexus.Platform.Testing/
/services/
  Gateway/
  Identity/
  Organization/
  Permission/
  Audit/
  Notification/
  Billing/
/deploy/
  docker-compose.yml
  helm/<service>/
  argo/
/docs/
  architecture/
    overview.md
    adr/000X-*.md
    diagrams/
  runbooks/
/tools/
  Seed/
  PolicyTester/
  ReplayConsole/
```

---

## Appendix B — Glossary

- **Aggregate Root** — DDD entity that owns a consistency boundary.
- **Cell** — A self-contained deployment unit (compute + data) typically per region.
- **CQRS** — Command Query Responsibility Segregation.
- **DLQ** — Dead-Letter Queue.
- **DSR** — Data Subject Request (GDPR).
- **Entitlement** — A right granted by an active subscription (feature flag or limit).
- **HSM/KMS** — Hardware Security Module / Key Management Service.
- **OIDC** — OpenID Connect.
- **RLS** — Postgres Row-Level Security.
- **SVID** — SPIFFE Verifiable Identity Document.
- **URN** — Uniform Resource Name (Nexus-canonical resource id).

---

**End of Document.**

*This architecture is a living artifact. Each substantive change requires an ADR; ADRs are reviewed at the Architecture Review Board (ARB) monthly.*
