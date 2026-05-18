# Singh Resident Hub

A full-stack residential rental management platform built on .NET 9 microservices. Manages rent payments, lease contracts, and tenant notifications for a four-tier user hierarchy: Super Admin, Admin (landlord/property manager), Tenant, and Tester.

---

## Screenshots

> Screenshots and GIFs coming soon — files will live in `Media/screenshots/`.

| | | |
|---|---|---|
| ![Login](Media/screenshots/login.png) | ![Tenant Dashboard](Media/screenshots/tenant-dashboard.png) | ![Make Payment](Media/screenshots/payment.png) |
| *Login (two-step TOTP)* | *Tenant dashboard* | *Stripe payment flow* |
| ![Admin Dashboard](Media/screenshots/admin-dashboard.png) | ![Contracts](Media/screenshots/contracts.png) | ![System Tests](Media/screenshots/system-tests.png) |
| *Admin dashboard* | *Contract management* | *SuperAdmin system tests* |

> Full demo GIF: `Media/screenshots/demo.gif` *(coming soon)*

---

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend | Next.js 15 (App Router, TypeScript, Tailwind CSS) |
| Backend | ASP.NET Core 9 microservices |
| Gateway | YARP reverse proxy |
| Database | PostgreSQL 16 (Entity Framework Core 9, one DB per service) |
| Auth | JWT (HS256, 15 min) + TOTP (RFC 6238 / OtpNet) + BCrypt |
| Payments | Stripe (PaymentIntent API + webhooks, card + ACH) |
| File Storage | Azure Blob Storage (lease contract PDFs) |
| Email | Azure Communication Services |
| Secrets | Azure Key Vault (via service principal) |
| Logging | Serilog (per-service rolling log files) |
| Containers | Docker + Docker Compose |
| Inter-service | gRPC (h2c on dedicated port) |

---

## Repository Structure

```
src/
  Gateway/
    TenantPortal.Gateway/         # API Gateway — single public entry point (port 5000)
  Services/
    TenantPortal.Auth/            # Authentication & user management (port 5001)
    TenantPortal.Transactions/    # Payments, rent schedules (port 5002)
    TenantPortal.Contracts/       # Lease PDF upload & download (port 5003)
    TenantPortal.Notifications/   # In-app + email notifications, gRPC (ports 5004 / 5005)
  Shared/
    TenantPortal.Shared/          # Cross-service constants, enums, interfaces
frontend/                         # Next.js 15 app
Media/
  architecture.md                 # (legacy) moved to ARCHITECTURE.md at root
ARCHITECTURE.md                   # Full technical architecture reference
```

---

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- An Azure Key Vault with the secrets listed below (or a local `secrets.json` for dev)

### Secrets

All secrets are loaded at runtime from Azure Key Vault via a service principal (`AZURE_CLIENT_ID`, `AZURE_CLIENT_SECRET`, `AZURE_TENANT_ID`). For local development without Key Vault, place a `secrets.json` file in the relevant service directory:

```json
{
  "Jwt__SigningKey": "a-random-string-at-least-32-characters-long",
  "Totp__EncryptionKey": "base64-encoded-32-byte-AES-256-key",
  "Stripe__SecretKey": "sk_test_...",
  "Stripe__WebhookSecret": "whsec_...",
  "AzureCommunicationServices__ConnectionString": "endpoint=https://..."
}
```

> The `Jwt__SigningKey` must be **identical across all services**. The Auth service signs tokens; every other service validates with the same key.

### Run with Docker Compose

```bash
docker compose up --build
```

Services start on:

| Service | Port |
|---|---|
| Gateway (public entry) | 5000 |
| Auth | 5001 |
| Transactions | 5002 |
| Contracts | 5003 |
| Notifications (HTTP) | 5004 |
| Notifications (gRPC) | 5005 |
| PostgreSQL | 5432 |

All API traffic goes through the Gateway on port 5000. Internal services are not publicly exposed.

### Frontend

```bash
cd frontend
npm install
npm run dev   # http://localhost:3000
```

Copy `.env.local` and fill in:

```
NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY=pk_test_...
```

---

## Authentication

Login is two-step:

1. `POST /api/auth/login` — email + password → short-lived opaque temp token
2. `POST /api/auth/login/totp` — temp token + 6-digit TOTP code → JWT (15 min) + refresh token (7 days)

New accounts are **invite-only**. Super Admin creates Admin accounts; Admins (or Super Admin) create Tenant accounts. All invites expire after 48 hours.

### Role Hierarchy

```
SuperAdmin
    └── Admin  (landlord / property manager)
            └── Tenant  (renter)
                    Tester  (read + simulated writes — no real data persisted)
```

**Tester role:** All non-GET requests are intercepted at the gateway before reaching any backend service. The action is logged and a notification email is sent to the Super Admin. The frontend receives a 200 response as if the action succeeded, so the full UI flow can be demonstrated without persisting test data.

---

## Testing Stripe Payments

Stripe is integrated for card and ACH Direct Debit payments.

### Prerequisites

1. **Stripe account** — create one at [stripe.com](https://stripe.com) (free)
2. **Stripe CLI** — [install here](https://stripe.com/docs/stripe-cli)
3. Add your keys to Azure Key Vault (or `secrets.json`):
   - `Stripe__SecretKey` → `sk_test_...` (from Stripe Dashboard → Developers → API keys)
   - `Stripe__PublishableKey` is frontend-only — add `NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY=pk_test_...` to `frontend/.env.local`

### Local Webhook Forwarding

Stripe webhooks need to reach your local machine. Use the CLI to forward them:

```bash
stripe listen --forward-to localhost:5000/api/webhooks/stripe
```

The CLI prints a webhook signing secret (`whsec_...`). Add that as `Stripe__WebhookSecret` in Key Vault or `secrets.json` for the transactions service.

### End-to-End Test Flow

1. **Log in as Admin** → go to **Rent Schedule** → create a schedule for your Tenant (you'll need the Tenant's user ID — visible in the system tests DB stats or from the JWT)
2. **Log in as Tenant** → go to **Make Payment**
3. Select **Card** or **ACH**, confirm the amount, click **Continue to payment**
4. Use Stripe test credentials:
   - **Card:** `4242 4242 4242 4242` | any future expiry | any CVC | any ZIP
   - **ACH:** select "US Bank Account" in the Stripe element and use test routing/account numbers from the [Stripe docs](https://stripe.com/docs/testing#ach-direct-debit)
5. Submit payment — Stripe fires `payment_intent.succeeded` to `/api/webhooks/stripe`
6. The transaction service marks the transaction as **Confirmed** and in-app notifications fire to both the Tenant and Admin

### Stripe Test Cards

| Scenario | Card Number |
|---|---|
| Success | `4242 4242 4242 4242` |
| Authentication required (3DS) | `4000 0025 0000 3155` |
| Decline | `4000 0000 0000 0002` |
| Insufficient funds | `4000 0000 0000 9995` |

---

## System Tests

Super Admin only. Navigate to **System Tests** in the sidebar or hit `GET /api/auth/tests/run`. Runs 10 live integration checks:

- Auth database connectivity and migrations
- User and invite token statistics
- Azure Key Vault: JWT key, TOTP key, ACS connection string
- AES-256-GCM TOTP encrypt/decrypt round-trip
- JWT sign + validate round-trip
- gRPC channel to Notifications service

---

## Further Reading

See [`ARCHITECTURE.md`](ARCHITECTURE.md) for the full technical reference: service design, API endpoints, database schema, notification events, security decisions, and the roadmap.
