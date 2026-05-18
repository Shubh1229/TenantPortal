# Singh Resident Hub — Technical Architecture Reference

## Overview

Singh Resident Hub is a full-stack residential rental management platform built for active production use. It manages rent payments, lease contracts, and notifications for a four-tier user hierarchy: Super Admin, Admin (landlord/property manager), Tenant, and Tester.

---

## System Architecture

```
[Next.js 15 Frontend]
         |
       HTTPS
         |
  [API Gateway — YARP]          ← single public-facing service (port 5000)
         |
   ┌─────┼──────────┬──────────────────┐
   |     |          |                  |
[Auth] [Transactions] [Contracts] [Notifications]
   \       |           /               /
    \      |          /               /
     └─────┴─────────┴───────────────┘
                  |
    [PostgreSQL — one database per service]
    [Azure Blob Storage — contract PDFs]
    [Azure Key Vault — all secrets]
```

Inter-service communication uses gRPC (h2c) on a dedicated port. The Auth and Transactions services call the Notifications service via gRPC to fire emails without blocking the HTTP response path.

> **Defence in depth:** The Gateway validates JWTs before forwarding any request. Each downstream service independently re-validates the JWT and enforces resource ownership. A compromised Gateway does not grant access to internal services.

---

## Services

### API Gateway (`TenantPortal.Gateway`) — Port 5000

- Single HTTPS entry point for all frontend and external requests
- JWT validation as a first layer before YARP forwards the request
- `CorrelationIdMiddleware` — assigns `X-Correlation-ID` (UUID) to every inbound request; propagated to all downstream services for cross-service log correlation
- `TesterInterceptMiddleware` — intercepts all non-GET requests from Tester-role users, logs the action, notifies Super Admin via email, and returns 200 to the frontend without forwarding to any backend service. The `/api/auth/invite` path is an exception — Testers receive 403 outright
- Stripe webhook route (`/api/webhooks/stripe`) bypasses JWT validation — verified by Stripe HMAC signature instead

### Auth Service (`TenantPortal.Auth`) — Port 5001

- Two-step login: email + password → opaque temp token → TOTP → JWT + refresh token
- Invite-based account creation (48-hour expiring, single-use tokens)
- JWT issuance: HS256, 15-minute access tokens
- Refresh token rotation: opaque tokens stored as SHA-256 hashes, rotated on every use
- TOTP: RFC 6238 via OtpNet; secrets encrypted at rest using AES-256-GCM with `ENC:` prefix for backward compatibility
- Password hashing with BCrypt
- Role upgrade paths: Tester → Admin, Tester → Tenant, Tenant → Admin (invite re-sent, existing record updated on registration)
- System test suite (`GET /api/auth/tests/run`, SuperAdmin only): 10 live integration checks covering DB, Key Vault, TOTP, JWT, and gRPC

### Transaction Service (`TenantPortal.Transactions`) — Port 5002

- Full CRUD for transaction records (rent, fees, deposits, refunds)
- Role-scoped reads: Tenants see only their own transactions; Admins and Super Admins see all
- Stripe PaymentIntent creation and webhook processing (`payment_intent.succeeded`)
- Two payment methods: card (2.9% + $0.30) and ACH Direct Debit (0.8%, max $5, via Stripe Financial Connections)
- External payment request flow: Tenant submits → status `Pending` → Admin approves (`Confirmed`) or declines (`Declined`)
- Rent schedule management: monthly amount + due day of month with February/30-day month edge-case clamping via `DateHelper`
- `OverduePaymentJob`: nightly background service that marks unpaid past-due transactions as `Overdue`

### Contract Service (`TenantPortal.Contracts`) — Port 5003

- PDF upload to Azure Blob Storage with metadata persisted in PostgreSQL
- Blob path format: `contracts/{tenantId}/{unitId}/{contractId}`
- On new upload, all previous contracts for the same tenant/unit are archived (`IsCurrent = false`)
- All contracts retained indefinitely — soft-delete only
- Download URLs are pre-signed Azure SAS tokens (15-minute expiry, generated on demand)
- Role-scoped reads: Tenants see only their own contracts

### Notification Service (`TenantPortal.Notifications`) — Ports 5004 (HTTP) / 5005 (gRPC)

- In-app notification creation and retrieval (always delivered, cannot be disabled)
- Email dispatch via Azure Communication Services, gated by per-user `EmailEnabled` preference
- Configurable rent reminders: multiple per user, each with a custom days-before and send time
- Two Kestrel ports: 8080 for HTTP/1.1 REST, 8081 for HTTP/2 gRPC (separate ports required for h2c without TLS)
- Internal endpoint (`POST /api/notifications/internal/tester-action`) — called by the Gateway to notify Super Admin when a Tester performs a write action

---

## Authentication & Authorization

### Role Hierarchy

```
SuperAdmin
    └── Admin  (landlord / property manager — invited by SuperAdmin)
            └── Tenant  (renter — invited by Admin or SuperAdmin)
                    Tester  (read-only + simulated writes — invited by Admin or SuperAdmin)
```

**Upgrade paths:** Tester → Admin, Tester → Tenant, Tenant → Admin. Re-inviting an upgradeable user updates their role and credentials on registration; all other re-invite attempts are rejected with a descriptive error.

### Login Flow

```
1. POST /api/auth/login
   → validate email + bcrypt password
   → return short-lived opaque temp token (stored in-memory, single-use, ConcurrentDictionary)

2. POST /api/auth/login/totp
   → validate temp token + 6-digit TOTP (RFC 6238)
   → consume temp token atomically
   → persist SHA-256(refreshToken) to the database
   → return JWT access token (15 min) + opaque refresh token (7 days)

3. POST /api/auth/refresh
   → hash incoming refresh token
   → look up user by hash, check expiry
   → rotate: generate new refresh token, update hash in DB
   → return new access + refresh token pair

4. POST /api/auth/logout
   → clear refresh_token_hash and refresh_token_expires_at for the user
```

### JWT Claims

| Claim | Key | Value |
|---|---|---|
| User ID | `uid` | UUID |
| Role | `role` | `SuperAdmin` / `Admin` / `Tenant` / `Tester` |
| Email | `email` | User's email address |

### Authorization Policies

| Policy | Allowed Roles |
|---|---|
| `RequireSuperAdmin` | SuperAdmin |
| `RequireAdmin` | Admin, SuperAdmin |
| `RequireTenant` | Tenant, Admin, SuperAdmin, Tester |

---

## API Endpoints

### Auth (`/api/auth`)

| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/login` | None | Step 1: email + password → temp token |
| POST | `/login/totp` | None | Step 2: TOTP → access + refresh tokens |
| POST | `/register` | None | Complete registration via invite token |
| POST | `/refresh` | None | Exchange refresh token for new token pair |
| POST | `/invite` | Admin+ | Send account invite email |
| POST | `/logout` | Tenant+ | Revoke refresh token |
| GET | `/tests/run` | SuperAdmin | Run live system health checks |

### Transactions (`/api/transactions`)

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/` | Tenant+ | List transactions (role-scoped) |
| GET | `/{id}` | Tenant+ | Get transaction by ID |
| POST | `/` | Admin+ | Create manual/backfill transaction |
| POST | `/external` | Tenant+ | Submit external payment request |
| PATCH | `/{id}/approve` | Admin+ | Approve pending external request |
| PATCH | `/{id}/decline` | Admin+ | Decline pending external request |
| DELETE | `/{id}` | Admin+ | Soft-delete a transaction |
| POST | `/payment-intent` | Tenant+ | Create Stripe PaymentIntent |
| POST | `/api/webhooks/stripe` | Stripe sig | Stripe webhook receiver (no JWT) |

### Rent Schedule (`/api/rent-schedule`)

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/{tenantId}` | Tenant+ | Get tenant's rent schedule (own only for Tenant) |
| POST | `/` | Admin+ | Create a rent schedule |
| PATCH | `/{id}` | Admin+ | Update a rent schedule |

### Contracts (`/api/contracts`)

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/` | Tenant+ | List contracts (role-scoped) |
| GET | `/{id}` | Tenant+ | Get contract metadata |
| GET | `/{id}/download` | Tenant+ | Get 15-minute SAS download URL |
| POST | `/upload` | Admin+ | Upload a contract PDF |
| DELETE | `/{id}` | Admin+ | Soft-delete a contract |

### Notifications (`/api/notifications`)

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/` | Tenant+ | Get in-app notifications |
| GET | `/{id}` | Tenant+ | Get single notification |
| PATCH | `/{id}/read` | Tenant+ | Mark as read |
| GET | `/api/notification-preferences` | Tenant+ | Get email preference |
| PATCH | `/api/notification-preferences` | Tenant+ | Update email preference |
| GET | `/api/reminders` | Tenant+ | List active reminders |
| POST | `/api/reminders` | Tenant+ | Create reminder |
| PATCH | `/api/reminders/{id}` | Tenant+ | Update reminder |
| DELETE | `/api/reminders/{id}` | Tenant+ | Deactivate reminder |

---

## Database Schema

### Auth DB

**users**
| Column | Type | Notes |
|---|---|---|
| id | UUID PK | |
| email | varchar UNIQUE | Login identifier |
| password_hash | varchar | BCrypt |
| totp_secret | varchar | Base32, AES-256-GCM encrypted (`ENC:` prefix) |
| role | int | 0=SuperAdmin, 1=Admin, 2=Tenant, 3=Tester |
| is_active | bool | False until registration completes |
| is_deleted | bool | Soft-delete flag |
| refresh_token_hash | varchar | SHA-256 of current refresh token; null when logged out |
| refresh_token_expires_at | timestamp | UTC expiry |
| invited_by | UUID FK → users | Null for Super Admin |
| created_at / updated_at / deleted_at | timestamp | Audit fields |

**invite_tokens**
| Column | Type | Notes |
|---|---|---|
| id | UUID PK | |
| email | varchar | Invitee email |
| role | int | Role assigned on registration |
| token_hash | varchar UNIQUE | SHA-256 of the plain-text invite token |
| expires_at | timestamp | 48 hours from creation |
| used | bool | Consumed on successful registration |
| created_by | UUID FK → users | |
| created_at | timestamp | |

### Transaction DB

**transactions** — status: `Pending`, `Confirmed`, `Declined`, `Overdue`; type: `Rent`, `Fee`, `Deposit`, `Refund`; method: `Card`, `ACH`, `Cash`, `Cheque`, `BankTransfer`, `Other`

**rent_schedules** — monthly amount, due day of month, tenant + unit association, start date

**properties** / **units** / **tenant_unit_assignments** — property/unit hierarchy and tenancy history

Indexes: `transactions(tenant_id)`, `transactions(status)`, `transactions(due_date)`, `tenant_unit_assignments(tenant_id)`, `tenant_unit_assignments(unit_id)`

### Contract DB

**contracts** — metadata only; PDFs live in Azure Blob Storage. Unique index on `blob_storage_path`.

### Notification DB

**notifications** — in-app records. Indexes: `(user_id)`, `(user_id, is_read)`

**notification_preferences** — one row per user, `email_enabled` boolean. Defaults to enabled for new users with no row. Unique index on `user_id`.

**reminder_settings** — configurable rent reminders per user. Index: `(user_id)`

---

## Notification Events

| Event | Notifies |
|---|---|
| Payment confirmed (Stripe webhook) | Admin + Tenant |
| External payment request submitted | Admin |
| External payment request approved | Tenant |
| External payment request declined | Tenant |
| Rent overdue — day after due date | Tenant |
| Rent overdue — weekly repeat | Tenant |
| Account invite sent | Invitee (email) |
| Contract uploaded | Tenant |
| Tester write action intercepted | Super Admin (email) |

---

## Secrets & Configuration

All secrets are accessed through `ISecretsProvider` with two implementations:

| Implementation | Environment | Source |
|---|---|---|
| `LocalSecretsProvider` | Local development | `secrets.json` in project root |
| `AzureKeyVaultSecretsProvider` | Staging + Production | Azure Key Vault via service principal |

**Key Vault secret names:**

| Key | Used By | Description |
|---|---|---|
| `Jwt--SigningKey` | All services | HS256 signing key (must be identical across all services) |
| `Totp--EncryptionKey` | Auth | Base64-encoded 32-byte AES-256-GCM key |
| `AzureCommunicationServices--ConnectionString` | Notifications | ACS email connection string |
| `Stripe--SecretKey` | Transactions | Stripe secret API key |
| `Stripe--WebhookSecret` | Transactions | Stripe webhook signing secret (rent payments) |
| `Stripe--SubscriptionWebhookSecret` | Auth | Stripe webhook signing secret (SaaS subscriptions) |
| `Stripe--PriceId` | Auth | Stripe Price ID for the SaaS plan |

> Azure Key Vault uses `--` as the key name separator (maps to `__` in .NET config hierarchy).

---

## gRPC Architecture

The Notifications service exposes two Kestrel ports:

- **8080** — HTTP/1.1 only (REST API)
- **8081** — HTTP/2 only (gRPC / h2c)

This separation is required because ASP.NET Core cannot multiplex HTTP/1.1 and HTTP/2 on the same port without TLS. Attempting `Http1AndHttp2` on a single cleartext port silently degrades to HTTP/1.1 only, breaking gRPC.

Auth and Transactions connect to the gRPC port (`http://notifications:8081`) for sending emails. The frontend and Gateway connect to the REST port (`http://notifications:8080`).

---

## Key Design Decisions

| Decision | Rationale |
|---|---|
| Separate DB per service | True isolation — a transaction schema change cannot break the auth service |
| Opaque refresh tokens (not JWTs) | Stored as hashes; can be revoked server-side at any time |
| SHA-256 for token storage and comparison | Collision-resistant; raw tokens are never stored or logged |
| `ConcurrentDictionary` for temp tokens | Prevents race conditions on simultaneous TOTP attempts for the same account |
| AES-256-GCM for TOTP secrets | Encrypts secrets at rest; `ENC:` prefix allows future plaintext migration |
| `IsActive = true` set at registration | Invite record exists from creation but user cannot log in until registration completes |
| Soft deletes everywhere | Audit trail preserved; accidental deletes are recoverable |
| `DueDayOfMonth` clamped by `DateHelper` | Handles February and 30-day months gracefully (e.g. day 31 → last day of month) |
| Stripe webhook bypasses JWT | Stripe's servers have no user JWT — verified via `Stripe-Signature` HMAC instead |
| `X-Correlation-ID` propagation | Ties all log lines from a single request across services for incident investigation |
| gRPC on separate port from REST | `Http1AndHttp2` without TLS silently breaks HTTP/2; dedicated port guarantees correct protocol |
| Tester role intercepted at Gateway | Write calls are no-op'd before reaching any backend; single enforcement point |

---

## Frontend Routes

| Route | Role | Description |
|---|---|---|
| `/login` | All | Two-step login (password then TOTP) |
| `/register?token=...` | All | Registration via invite link |
| `/tenant` | Tenant+ | Tenant dashboard (upcoming payments, quick actions) |
| `/tenant/payment` | Tenant+ | Stripe payment form (card or ACH) |
| `/tenant/contracts` | Tenant+ | View and download lease contracts |
| `/tenant/notifications` | Tenant+ | In-app notifications, reminders, email preferences |
| `/admin` | Admin+ | Admin dashboard (revenue chart, overview) |
| `/admin/contracts` | Admin+ | Upload and manage tenant contracts |
| `/admin/rent-schedule` | Admin+ | Create and update rent schedules |
| `/super-admin` | SuperAdmin | SuperAdmin dashboard — user management, invite |
| `/super-admin/tests` | SuperAdmin | Live system health tests |

---

## Future Roadmap

- **Maintenance request system** — tenant-submitted requests with admin status tracking
- **Property-scoped admin permissions** — admins restricted to assigned properties
- **DocuSign integration** — Contract Service is structured to accept an e-signature adapter
- **Azure Service Bus** — replace `IHostedService` background jobs with a message queue for retry/fault isolation
- **Admin monthly digest** — email summary of all transactions and occupancy
- **Payment analytics dashboard** — occupancy rates, revenue trends, delinquency rates
- **Tenant messaging** — in-app direct messaging between admin and tenant
