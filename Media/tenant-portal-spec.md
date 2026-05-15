# Tenant Admin Portal — Project Specification

## 1. Project Overview

A full-stack web application for managing a residential rental property. The portal serves three user roles: a Super Admin (developer/owner), Admins (landlords/property managers), and Tenants (renters). Core features include transaction tracking, rent payment processing, contract storage, and automated email/in-app notifications.

This project is intended as both a portfolio piece and a practical tool in active use.

---

## 2. Tech Stack

### Frontend
- **Framework:** Next.js (React)
- **UI Library:** shadcn/ui
- **Hosting:** Azure Blob Storage + Azure CDN (static export)
  - *Alternative:* Azure Static Web Apps — supports SSR if needed in future

### Backend
- **Language/Runtime:** C# / .NET 9.0
- **Architecture:** Microservices in Docker containers
- **Internal Communication:** gRPC (Protobuf)
- **External API:** HTTPS REST endpoints via API Gateway service
- **Hosting:** Azure Container Apps

### Database
- **Engine:** PostgreSQL
- **Host:** Azure Database for PostgreSQL

### Infrastructure & Services
- **Secret Management:** Azure Key Vault
- **File Storage:** Azure Blob Storage (contracts/PDFs)
- **Email:** Azure Communication Services
- **DNS:** Azure DNS
- **SSL:** Azure App Service Managed Certificates or Let's Encrypt
- **Payments:** Stripe (with webhook support)
- **Domain:** Custom domain (to be purchased separately)

### Observability
- **Structured Logging:** Serilog (all services)
- **Log Aggregation:** Azure Application Insights
- **Local Dev Logging:** Seq (optional, runs as Docker container)

### Authentication
- **Method:** Custom JWT-based auth (access + refresh tokens)
- **2FA:** TOTP via Microsoft Authenticator (or any TOTP-compatible app)

---

## 3. System Architecture

```
[Next.js Frontend — Azure Blob + CDN]
            |
          HTTPS
            |
   [API Gateway Service]         ← Single public-facing .NET service
     |       |      |      |
   gRPC    gRPC   gRPC   gRPC
     |       |      |      |
[Auth]  [Transaction] [Contract] [Notification]
     \       |      /
      \      |     /
    [PostgreSQL — Azure DB]
         [Azure Blob Storage]
         [Azure Key Vault]
```

---

## 4. Microservices

### 4.1 API Gateway Service
- Single HTTPS entry point for all frontend requests
- Routes requests to internal services via gRPC
- Handles SSL termination
- Validates JWT on every incoming request before forwarding
- Applies rate limiting on auth and payment endpoints
- **Note:** Gateway JWT validation is the first layer only — each internal service also validates claims independently (defense in depth)

### 4.2 Auth Service
- User login (email + password + TOTP)
- JWT issuance and refresh token management
- Invite-based account creation flow
- Password hashing (bcrypt or Argon2)
- TOTP secret generation and verification
- Role-based access control enforcement
- Health check endpoints: `/health`, `/health/ready`, `/health/live`

### 4.3 Transaction Service
- CRUD for all transaction records
- Stripe payment intent creation and webhook handling
- External payment request workflow (submit → pending → approve/decline)
- Rent schedule management
- Automatic overdue detection and status updates
- Stripe webhook endpoint (exposed via API Gateway)
- Independently validates JWT claims and tenant ownership on every request
- Health check endpoints: `/health`, `/health/ready`, `/health/live`

### 4.4 Contract Service
- PDF upload and storage to Azure Blob Storage
- Contract metadata management (tenant association, upload date, active/archived)
- Secure download URL generation (SAS tokens, short-lived)
- File validation: PDF only, MIME type checked, max file size enforced
- Independently validates JWT claims and admin/tenant scope on every request
- Designed to accommodate DocuSign integration as a future add-on
- Health check endpoints: `/health`, `/health/ready`, `/health/live`

### 4.5 Notification Service
- Email dispatch via Azure Communication Services
- In-app notification creation and storage
- Reminder schedule management (per-user configurable)
- Overdue email cycle management
- User email notification preference management (on/off per user)
- Independently validates JWT claims and recipient ownership on every request
- Health check endpoints: `/health`, `/health/ready`, `/health/live`

---

## 5. Authentication & User Roles

### 5.1 Role Hierarchy

```
Super Admin
    └── Admin (created by Super Admin only)
            └── Tenant (created by Admin or Super Admin)
```

### 5.2 Role Permissions

| Action                          | Super Admin | Admin     | Tenant      |
|---------------------------------|-------------|-----------|-------------|
| Create Admin accounts           | ✅          | ❌        | ❌          |
| Create Tenant accounts          | ✅          | ✅        | ❌          |
| View all tenants                | ✅          | ✅        | ❌          |
| View own transactions           | ✅          | ✅        | ✅ (own)    |
| Manage rent schedule            | ✅          | ✅        | ❌          |
| Upload contracts                | ✅          | ✅        | ❌          |
| View all contracts              | ✅          | ✅        | ✅ (own)    |
| Approve/decline payment requests| ✅          | ✅        | ❌          |
| Toggle own email notifications  | ❌          | ✅        | ✅          |
| Receive any notifications       | ❌          | ✅        | ✅          |

### 5.3 Super Admin Account
- Hardcoded and database-seeded on first deployment
- Credentials stored securely in Azure Key Vault
- Not created via the invite flow
- Does not receive any notifications
- Has no notification settings

### 5.4 Account Creation Flow (Admin and Tenant)
1. Authorized user fills out a form in the portal (invitee name + email + role)
2. System creates a **pending** account record
3. Invite email is sent with a **time-limited signup link** (recommend 48-hour expiry)
4. Invitee clicks link → sets their own password → scans TOTP QR code to enroll in 2FA
5. Account becomes active

### 5.5 Login Flow
1. User submits email + password
2. Server validates credentials
3. Server prompts for TOTP code
4. User submits 6-digit code from authenticator app
5. Server issues JWT access token + refresh token on success

### 5.6 JWT Strategy
- Short-lived access tokens (e.g. 15 minutes)
- Longer-lived refresh tokens (e.g. 7 days), stored securely (httpOnly cookie recommended)
- Refresh token rotation on each use
- Tokens invalidated on logout

---

## 6. Transactions

### 6.1 Transaction Types
Comprehensive set including but not limited to:
- Monthly rent
- Security deposit
- Late fees
- Maintenance charges
- Utility charges
- Parking fees
- Pet fees
- Refunds / credits

Transaction types should be stored as an enum or configurable list to allow future expansion.

### 6.2 Transaction States

| State      | Description                                                  |
|------------|--------------------------------------------------------------|
| `Pending`  | External payment submitted by tenant, awaiting admin approval|
| `Confirmed`| Payment completed (Stripe success or admin-approved external)|
| `Declined` | Admin rejected an external payment request                   |
| `Overdue`  | Past due date with no Confirmed or Pending payment           |

### 6.3 Payment Flow A — In-App via Stripe

1. Tenant initiates payment in portal
2. Frontend calls API Gateway → Transaction Service creates Stripe Payment Intent
3. Tenant completes card payment via Stripe hosted UI or Elements
4. Stripe fires webhook to Transaction Service on success
5. Transaction auto-logged as `Confirmed`
6. Email + in-app notification sent to Admin and Tenant

### 6.4 Payment Flow B — External Payment (Zelle, check, bank transfer, etc.)

1. Tenant submits "I paid externally" request (fields: amount, payment date, method, optional note)
2. Transaction created with status `Pending`
3. Admin receives email + in-app notification to verify
4. Admin approves → status becomes `Confirmed`, Tenant notified
5. Admin declines → status becomes `Declined`, Tenant notified, overdue cycle resumes if applicable

### 6.5 Rent Schedule
- Admin or Super Admin creates the rent schedule per tenant (amount + initial due date)
- System automatically calculates the next due date as the same day the following month
- February edge case: if due date is 29, 30, or 31, clamp to last day of February (28 or 29)
- Overdue detection runs daily (scheduled job) to flag unpaid past-due transactions

### 6.6 Historical Data
- Admins and Super Admin can backfill historical transactions manually
- Initial tenant history for current tenant to be seeded directly into the database

### 6.7 Tenant View
- Tenant can view their full transaction history
- Transactions sorted by date descending by default

---

## 7. Contracts

### 7.1 Storage
- Admins upload finalized, pre-signed PDF lease agreements
- PDFs stored in Azure Blob Storage
- Metadata stored in PostgreSQL (tenant ID, upload date, uploader, active flag)

### 7.2 Contract History
- All past and current contracts are retained indefinitely
- No contracts are deleted — only archived when superseded by a new upload

### 7.3 Access Control

| Role        | Access                                      |
|-------------|---------------------------------------------|
| Super Admin | View and download all contracts, all tenants|
| Admin       | View and download all contracts, all tenants|
| Tenant      | View and download own contracts only (current + previous) |

### 7.4 Download & Viewing
- Tenants and Admins can view PDFs in-browser
- Download to device also supported
- Secure time-limited URLs generated via Azure Blob Storage (SAS tokens)

### 7.5 No Status Workflow (for now)
- Uploaded contract is considered finalized immediately
- No draft/sent/signed states at this stage

### 7.6 Future: DocuSign Integration
- Contract Service should be architected to support an e-signature flow in the future
- Recommended: Abstract contract finalization behind an interface so DocuSign can be plugged in without restructuring the service

---

## 8. Notifications

### 8.1 Channels
- **Email** — via Azure Communication Services; toggleable per user (on/off for themselves only)
- **In-App** — notification bell/inbox in the portal; always on, cannot be disabled

### 8.2 Notification Events

| Event                              | Notifies              |
|------------------------------------|-----------------------|
| Payment confirmed (Stripe)         | Admin + Tenant        |
| External payment request submitted | Admin                 |
| External request approved          | Tenant                |
| External request declined          | Tenant                |
| Rent reminder (configurable)       | Tenant (+ optionally Admin) |
| Payment overdue — day after        | Tenant                |
| Payment overdue — weekly repeat    | Tenant                |
| Account invite sent                | Invitee               |

### 8.3 Rent Reminders
- Default: 1 reminder, 7 days before due date
- Admin or Tenant can each independently add multiple reminders
- Each reminder is configurable: days in advance + specific send time
- Each user only manages their own reminder preferences

### 8.4 Overdue Email Logic

```
Due date passes
      ↓
Is there a Confirmed payment? → Stop, do nothing
      ↓
Is there a Pending external request? → Pause overdue cycle
   Admin approves → Stop cycle entirely
   Admin declines → Resume cycle from declined date
      ↓
No Confirmed or Pending payment?
   → Email tenant the day after due date
   → Email tenant again weekly on the same weekday
   → Continue until a Confirmed payment exists
```

### 8.5 User Notification Controls
- Each user can toggle email notifications on or off for themselves only
- In-app notifications cannot be toggled — always delivered
- Super Admin has no notification settings and receives no notifications
- Admins cannot toggle notifications for other users
- Tenants cannot toggle notifications for other users

---

## 9. Dashboard Views

### 9.1 Tenant Dashboard
- Current balance / next rent amount due
- Next rent due date + days remaining
- Recent transaction history (last 5–10 entries)
- Pending external payment requests (with status)
- Unread in-app notifications

### 9.2 Admin Dashboard
- List of all tenants with current payment status (Paid / Pending / Overdue)
- Pending external payment requests awaiting approval
- Upcoming rent due dates across all tenants
- Recent activity feed (payments, contract uploads, new tenants)

### 9.3 Super Admin Dashboard
- System-wide overview: total admins, total tenants, total active leases
- Full list of all admins and their tenants
- Pending account invites (admin and tenant)
- Recent activity across the entire system

---

## 10. Database Schema (High-Level)

### Users
```
users
  id (UUID, PK)
  email (string, unique)
  password_hash (string)
  totp_secret (string, encrypted)
  role (enum: super_admin, admin, tenant)
  is_active (bool)
  is_deleted (bool, default false)
  deleted_at (timestamp, nullable)
  created_at (timestamp)
  updated_at (timestamp)
  invited_by (UUID, FK → users)
```

### Properties
```
properties
  id (UUID, PK)
  name (string)
  address (string)
  is_active (bool, default true)
  is_deleted (bool, default false)
  deleted_at (timestamp, nullable)
  created_at (timestamp)
  updated_at (timestamp)
```

### Units
```
units
  id (UUID, PK)
  property_id (UUID, FK → properties)
  unit_number (string)
  bedrooms (int, nullable)
  bathrooms (decimal, nullable)
  square_feet (int, nullable)
  is_active (bool, default true)
  is_deleted (bool, default false)
  deleted_at (timestamp, nullable)
  created_at (timestamp)
  updated_at (timestamp)
```

### Tenant Unit Assignments
```
tenant_unit_assignments
  id (UUID, PK)
  tenant_id (UUID, FK → users)
  unit_id (UUID, FK → units)
  start_date (date)
  end_date (date, nullable)    ← null means currently active
  created_at (timestamp)
  updated_at (timestamp)
```

### Invite Tokens
```
invite_tokens
  id (UUID, PK)
  email (string)
  role (enum)
  token_hash (string)
  expires_at (timestamp)
  used (bool)
  created_by (UUID, FK → users)
  created_at (timestamp)
```

### Rent Schedules
```
rent_schedules
  id (UUID, PK)
  tenant_id (UUID, FK → users)
  unit_id (UUID, FK → units)
  monthly_amount (decimal)
  due_day (int, 1–31)
  start_date (date)
  created_by (UUID, FK → users)
  created_at (timestamp)
  updated_at (timestamp)
```

### Transactions
```
transactions
  id (UUID, PK)
  tenant_id (UUID, FK → users)
  unit_id (UUID, FK → units)
  type (enum: rent, deposit, late_fee, maintenance, utility, parking, pet, refund, other)
  amount (decimal)
  status (enum: pending, confirmed, declined, overdue)
  payment_method (enum: stripe, external, manual)
  external_method_note (string, nullable)
  stripe_payment_intent_id (string, nullable)
  due_date (date, nullable)
  paid_date (date, nullable)
  is_deleted (bool, default false)
  deleted_at (timestamp, nullable)
  created_by (UUID, FK → users)
  created_at (timestamp)
  updated_at (timestamp)
```

### Contracts
```
contracts
  id (UUID, PK)
  tenant_id (UUID, FK → users)
  unit_id (UUID, FK → units)
  blob_storage_path (string)
  file_name (string)
  is_current (bool)
  is_deleted (bool, default false)
  deleted_at (timestamp, nullable)
  uploaded_by (UUID, FK → users)
  uploaded_at (timestamp)
  updated_at (timestamp)
```

### Notifications
```
notifications
  id (UUID, PK)
  user_id (UUID, FK → users)
  type (enum)
  message (string)
  is_read (bool)
  created_at (timestamp)
```

### Reminder Settings
```
reminder_settings
  id (UUID, PK)
  user_id (UUID, FK → users)
  days_before (int)
  send_time (time)
  is_active (bool)
  created_at (timestamp)
  updated_at (timestamp)
```

### Notification Preferences
```
notification_preferences
  id (UUID, PK)
  user_id (UUID, FK → users)
  email_enabled (bool, default true)
  updated_at (timestamp)
```

### Audit Logs
```
audit_logs
  id (UUID, PK)
  actor_user_id (UUID, FK → users)
  action (string)               ← e.g. "contract.uploaded", "payment.approved"
  target_entity (string)        ← e.g. "contracts", "transactions"
  target_id (UUID, nullable)
  metadata (jsonb, nullable)    ← optional extra context
  created_at (timestamp)
```

### Recommended Indexes
```sql
-- Transactions
CREATE INDEX idx_transactions_tenant_id ON transactions(tenant_id);
CREATE INDEX idx_transactions_status ON transactions(status);
CREATE INDEX idx_transactions_due_date ON transactions(due_date);

-- Contracts
CREATE INDEX idx_contracts_tenant_id ON contracts(tenant_id);

-- Notifications
CREATE INDEX idx_notifications_user_id ON notifications(user_id);
CREATE INDEX idx_notifications_is_read ON notifications(user_id, is_read);

-- Tenant Unit Assignments
CREATE INDEX idx_tenant_unit_assignments_tenant_id ON tenant_unit_assignments(tenant_id);
CREATE INDEX idx_tenant_unit_assignments_unit_id ON tenant_unit_assignments(unit_id);

-- Audit Logs
CREATE INDEX idx_audit_logs_actor ON audit_logs(actor_user_id);
CREATE INDEX idx_audit_logs_target ON audit_logs(target_entity, target_id);
```

---

## 11. API Design (High-Level)

All public endpoints are HTTPS REST via the API Gateway. All internal service communication is gRPC.

### Property & Unit Endpoints
```
GET  /api/properties                  — list all properties (admin+)
POST /api/properties                  — create property (super admin)
GET  /api/properties/:id/units        — list units for a property (admin+)
POST /api/properties/:id/units        — add unit to property (admin+)
GET  /api/units/:id/assignments       — get tenant assignment history (admin+)
POST /api/units/:id/assignments       — assign tenant to unit (admin+)
PATCH /api/units/:id/assignments/:aid — update assignment end date (admin+)
```

### Auth Endpoints
```
POST /api/auth/login              — email + password + TOTP
POST /api/auth/refresh            — refresh access token
POST /api/auth/logout             — invalidate tokens
POST /api/auth/invite             — send account invite (Super Admin / Admin)
POST /api/auth/register           — complete invite registration (set password + enroll TOTP)
```

### User Endpoints
```
GET  /api/users                   — list users (role-filtered)
GET  /api/users/:id               — get user detail
PATCH /api/users/:id              — update user info
```

### Transaction Endpoints
```
GET  /api/transactions            — list transactions (role-filtered)
GET  /api/transactions/:id        — get transaction detail
POST /api/transactions            — create manual/backfill transaction (admin+)
POST /api/transactions/payment-intent — create Stripe payment intent (tenant)
POST /api/transactions/external   — submit external payment request (tenant)
PATCH /api/transactions/:id/approve — approve external request (admin+)
PATCH /api/transactions/:id/decline — decline external request (admin+)
POST /api/webhooks/stripe         — Stripe webhook receiver
```

### Rent Schedule Endpoints
```
GET  /api/rent-schedule/:tenantId — get tenant's rent schedule
POST /api/rent-schedule           — create rent schedule (admin+)
PATCH /api/rent-schedule/:id      — update rent schedule (admin+)
```

### Contract Endpoints
```
GET  /api/contracts               — list contracts (role-filtered)
GET  /api/contracts/:id/download  — get secure download URL
POST /api/contracts/upload        — upload contract PDF (admin+)
```

### Notification Endpoints
```
GET  /api/notifications           — get in-app notifications for current user
PATCH /api/notifications/:id/read — mark notification as read
GET  /api/notification-preferences — get current user's email preferences
PATCH /api/notification-preferences — update email preferences
GET  /api/reminders               — get current user's reminder settings
POST /api/reminders               — add reminder
PATCH /api/reminders/:id          — update reminder
DELETE /api/reminders/:id         — delete reminder
```

---

## 12. Deployment & Infrastructure

### Environment Separation
Three environments should be maintained:
- **Development** — local Docker Compose, local PostgreSQL, test Stripe keys
- **Staging** — full Azure deployment, mirrors production, used for testing before release
- **Production** — live environment, real Stripe keys, real tenant data

Each environment has its own Azure Key Vault, database instance, and Blob Storage account. Secrets never cross environments.

### Container Strategy
Each microservice is a separate Docker container:
- `gateway-service`
- `auth-service`
- `transaction-service`
- `contract-service`
- `notification-service`

All containers deployed to **Azure Container Apps**.

### Environment & Secrets
- All secrets (DB connection strings, JWT signing keys, Stripe keys, Azure Communication Services keys) stored in **Azure Key Vault**
- Containers access Key Vault via managed identity (no hardcoded credentials)

### Frontend Deployment
- Next.js static export
- Hosted on **Azure Blob Storage** with static website hosting enabled
- Served via **Azure CDN**
- Custom domain configured via **Azure DNS**
- SSL via Azure CDN managed certificate

### Database
- **Azure Database for PostgreSQL** (Flexible Server recommended)
- Migrations managed via Entity Framework Core

### CI/CD (Recommended)
- GitHub Actions or Azure DevOps pipelines
- Build, test, and push Docker images to Azure Container Registry on merge to main
- Auto-deploy to Azure Container Apps

---

## 13. Future Add-ons (Out of Scope for v1)

- **DocuSign integration** — e-signature flow for lease signing (Contract Service designed to accommodate)
- **Maintenance request system** — tenant submits maintenance requests, admin manages status
- **Property-scoped admin permissions** — `property_admins` join table so admins only manage assigned properties (foundation already in schema via Properties/Units)
- **Admin monthly digest** — optional email summary of all transactions
- **Tenant communication thread** — in-app messaging between admin and tenant
- **Queue-based notification processing** — replace background `IHostedService` with Azure Service Bus for retry support, fault isolation, and independent scaling
- **Payment analytics dashboard** — occupancy rates, monthly revenue, delinquency trends

---

## 14. Development Notes

- **Seed script** should create the Super Admin account on first run, reading credentials from Azure Key Vault or environment variables
- **Stripe webhook** must bypass JWT auth and instead be verified using Stripe's webhook signing secret (`Stripe-Signature` header) before processing — JWT validation will fail on Stripe's server-originated requests
- **Overdue job** should run as a scheduled background service (e.g. .NET `IHostedService`) — recommend running at midnight daily
- **TOTP** implementation: use standard RFC 6238 TOTP; any authenticator app will work, not just Microsoft Authenticator
- **SAS tokens** for Blob Storage downloads should have a short expiry (e.g. 15–30 minutes) and be generated on-demand
- **Refresh tokens** should be stored hashed in the database, not in plaintext
- **February due date** edge case: if `due_day > 28` and month is February, clamp to `DateTime.DaysInMonth(year, 2)`
- **Soft deletes** — never hard delete users, transactions, or contracts; always set `is_deleted = true` and `deleted_at = now()`; all queries should filter on `is_deleted = false` by default
- **Audit logging** — every significant action (payment approval, contract upload, admin invite, password reset, etc.) should write a record to `audit_logs`; do this from the service layer, not the database layer
- **Rate limiting** — apply to `/api/auth/login`, `/api/auth/register`, and `/api/transactions/payment-intent` at minimum; .NET 9 has built-in rate limiting middleware
- **File validation** — on contract upload, validate MIME type is `application/pdf`, check magic bytes (not just extension), enforce a max file size (e.g. 20MB)
- **Defense in depth** — even though the API Gateway validates JWTs first, every internal service must also validate the user's role and resource ownership independently; do not trust that only valid requests will ever reach internal services
- **Secrets provider abstraction** — All secret retrieval should go through a shared `ISecretsProvider` interface with two implementations:
  - `LocalSecretsProvider` — reads from a `.secrets` file or folder (local dev only, never committed to Git)
  - `AzureKeyVaultSecretsProvider` — calls Azure Key Vault (staging and production only)
  - The active implementation is swapped via environment variable or `appsettings.{Environment}.json` so no code changes are needed between environments
  - The `.secrets` file/folder must be added to `.gitignore` immediately — it should never be committed
  - Recommended structure:
    ```
    /secrets
      jwt.secret
      db.connection
      stripe.secret
      stripe.webhook
      totp.encryption.key
    ```
    Or as a single flat JSON file:
    ```json
    {
      "Jwt__SigningKey": "...",
      "ConnectionStrings__Postgres": "...",
      "Stripe__SecretKey": "...",
      "Stripe__WebhookSecret": "...",
      "Totp__EncryptionKey": "..."
    }
    ```
  - In .NET this pairs cleanly with `IConfiguration` — the local provider can back `IConfiguration` directly, and the Azure provider loads the same keys from Key Vault at startup
- **Property/Unit modeling** — even with a single property and unit today, all transactions, rent schedules, and contracts should reference a `unit_id` from the start; retrofitting this later is painful
- **Debug logging strategy** — a persistent, reviewable log store should be maintained on the backend for crash investigation and audit purposes:
  - **What to log:** every inbound request (method, route, timestamp), service-to-service gRPC calls, background job execution (overdue checks, reminder sends), exceptions and stack traces, payment state transitions, auth events (login attempts, token refresh, invite sends)
  - **What to never log:** names, emails, addresses, payment card details, JWT tokens, raw passwords, TOTP secrets, or any PII — always substitute with the entity's UUID (e.g. `TenantId: a3f2...` not `Tenant: John Smith`)
  - **Correlation ID:** every request is assigned a `CorrelationId` (UUID) at the gateway and passed through all downstream gRPC calls — every log entry includes it so a full crash can be reconstructed across all services by filtering on one ID
  - **Storage:** logs written simultaneously to:
    - `logs` table in PostgreSQL — queryable, fast, 90-day retention
    - Azure Blob Storage as rolling daily `.log` files — cold archival, 1-year retention
  - **Severity levels:** `Debug` (dev only), `Information` (normal ops), `Warning` (recoverable issues), `Error` (failures needing attention), `Critical` (service crash or data integrity issue)
  - **Log viewer:** Super Admin dashboard includes a basic log viewer filtered by severity, service name, date range, and CorrelationId
  - **Suggested `logs` table:**
    ```
    logs
      id (UUID, PK)
      correlation_id (UUID)         ← ties all logs from one request together
      service_name (string)         ← e.g. "auth-service", "transaction-service"
      level (enum: debug, info, warning, error, critical)
      message (string)
      exception (text, nullable)    ← stack trace if applicable
      user_id (UUID, nullable)      ← always UUID, never name or email
      created_at (timestamp)
    ```
