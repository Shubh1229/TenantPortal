# TenantPortal

A full-stack residential rental management platform built with .NET 9 microservices. Manages rent transactions, lease contracts, and tenant notifications for a three-tier user hierarchy: Super Admin, Admin (landlord/property manager), and Tenant.

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core 9 (microservices) |
| Gateway | YARP reverse proxy |
| Database | PostgreSQL (Entity Framework Core, one DB per service) |
| Auth | JWT (HS256) + TOTP (RFC 6238) + BCrypt |
| Payments | Stripe (PaymentIntent + webhooks) |
| File Storage | Azure Blob Storage (contract PDFs) |
| Email | Azure Communication Services |
| Secrets | Azure Key Vault (prod) / `secrets.json` (local dev) |
| Logging | Serilog |
| Containers | Docker + Docker Compose |

## Repository Structure

```
src/
  Services/
    TenantPortal.Gateway/         # API Gateway — single public entry point (port 5000)
    TenantPortal.Auth/            # Authentication & user management (port 5001)
    TenantPortal.Transactions/    # Payments, rent schedules (port 5002)
    TenantPortal.Contracts/       # Lease PDF upload & download (port 5003)
    TenantPortal.Notifications/   # In-app + email notifications (port 5004)
  Shared/
    TenantPortal.Shared/          # Cross-service constants, DTOs, interfaces
Media/
  architecture.md                 # Full technical architecture reference
```

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### 1. Create `secrets.json`

Each service reads credentials from a `secrets.json` file at startup (local dev only — never commit this file). Place one in each service project directory, or in the repo root when running with `dotnet run`:

```json
{
  "Jwt__SigningKey": "a-random-string-at-least-32-characters-long",
  "Stripe__SecretKey": "sk_test_...",
  "Stripe__WebhookSecret": "whsec_...",
  "AzureCommunicationServices__ConnectionString": "endpoint=https://..."
}
```

> The `Jwt__SigningKey` **must be identical across all services**. Auth signs tokens with it; every other service validates with it.

### 2. Run with Docker Compose

```bash
docker compose up --build
```

Services start on:

| Service | Port |
|---|---|
| Gateway | 5000 |
| Auth | 5001 |
| Transactions | 5002 |
| Contracts | 5003 |
| Notifications | 5004 |
| PostgreSQL | 5432 |

### 3. Run Database Migrations

Each service owns its own database. Run EF Core migrations from the repo root:

```bash
dotnet ef database update --project src/Services/TenantPortal.Auth
dotnet ef database update --project src/Services/TenantPortal.Transactions
dotnet ef database update --project src/Services/TenantPortal.Contracts
dotnet ef database update --project src/Services/TenantPortal.Notifications
```

All API traffic goes through the Gateway on port 5000. Internal services are not publicly exposed.

## Authentication

Login is two-step:

1. `POST /api/auth/login` — email + password → short-lived temp token
2. `POST /api/auth/login/totp` — temp token + 6-digit TOTP code → JWT access token (15 min) + refresh token (7 days)

New accounts are invite-only. Admins are created by Super Admin; Tenants are created by Admin or Super Admin via `POST /api/auth/invite`.

## Further Reading

See [`Media/architecture.md`](Media/architecture.md) for the full technical reference: service responsibilities, API endpoints, database schema, notification events, design decisions, and the future roadmap.
