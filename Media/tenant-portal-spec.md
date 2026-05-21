# Tenant Admin Portal — Project Specification

## 1. Project Overview

A full-stack web application for managing a residential rental property. The portal serves three user roles: a Super Admin (developer/owner), Admins (landlords/property managers), and Tenants (renters). Core features include transaction tracking, rent payment processing, contract storage, and automated email/in-app notifications.

This project is intended as both a portfolio piece and a practical tool in active use.

---

## 2. Tech Stack

### Frontend
- **Framework:** Next.js (App Router)
- **UI Library:** shadcn/ui (dark zinc theme)
- **Styling:** Tailwind CSS v4
- **Payments:** Stripe.js + React Stripe Elements
- **Hosting:** Azure Blob Storage + Azure CDN (static export)

### Backend
- **Language/Runtime:** C# / .NET 9.0
- **Architecture:** Microservices in Docker containers
- **Internal Communication:** HTTP proxy via YARP (API Gateway)
- **External API:** HTTPS REST endpoints via API Gateway service
- **Hosting:** Azure Container Apps

### Database
- **Engine:** PostgreSQL
- **Host:** Azure Database for PostgreSQL
- **ORM:** Entity Framework Core (Npgsql provider)

### Infrastructure & Services
- **Secret Management:** Azure Key Vault (via `ISecretsProvider` abstraction; `LocalSecretsProvider` in dev)
- **File Storage:** Azure Blob Storage (contracts/PDFs)
- **Email:** Azure Communication Services
- **Payments:** Stripe (card, ACH, webhook support)
- **Structured Logging:** Serilog → Azure Application Insights

### Authentication
- **Method:** Custom JWT-based auth (HS256 access tokens + opaque refresh tokens)
- **2FA:** TOTP via any RFC 6238-compatible authenticator app
- **Dev bypass:** `POST /api/auth/dev-login` skips TOTP for the three hardcoded test accounts

---

## 3. System Architecture

```
[Next.js Frontend — Azure Blob + CDN]
            |
          HTTPS
            |
   [API Gateway Service]         ← Single public-facing .NET service (YARP)
     |       |      |      |
  (HTTP)  (HTTP)  (HTTP)  (HTTP)
     |       |      |      |
[Auth]  [Transactions] [Contracts] [Notifications]
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
- YARP reverse proxy routes to internal services
- JWT validation on every inbound request before forwarding (first layer)
- Assigns `X-Correlation-ID` UUID to every request
- Stripe webhook (`/api/webhooks/stripe`) bypasses JWT — verified by Stripe signature

### 4.2 Auth Service
- Two-step login: email + password → TOTP → JWT + refresh token
- Invite-based account creation (48-hour invite tokens)
- JWT issuance (HS256, 15-minute access tokens); refresh token rotation
- TOTP (RFC 6238, OtpNet); TOTP secret encrypted at rest
- Password hashing (BCrypt)
- User profiles (separate table): first name, last name, phone, emergency contact
- Multiple notification email addresses per user (separate table)
- `IsProfileComplete` flag: new users are redirected to profile setup before accessing the dashboard
- Public profile endpoint for admin views
- Role-switching: SuperAdmin can issue a downgraded-role token for UI testing

### 4.3 Transaction Service
- CRUD for all transaction records
- Stripe PaymentIntent creation and webhook handling
- External payment request workflow (submit → pending → approve/decline)
- Rent schedule management with two billing modes:
  - **PerTenant:** each co-tenant has an independent schedule
  - **SharedUnit:** one schedule covers the entire unit
- Multiple tenants per unit simultaneously; tenant-in-one-unit constraint enforced
- Nightly `OverduePaymentJob` marks unpaid past-due transactions as Overdue
- Full property and unit CRUD with soft-delete guard rails

### 4.4 Contract Service
- PDF upload and storage to Azure Blob Storage
- Two pre-signed SAS URL types per contract:
  - **DownloadUrl** — standard read URL
  - **PreviewUrl** — read URL with `ContentDisposition: inline` for in-browser rendering
- New upload archives all prior contracts for the same tenant/unit
- Soft-delete only; all contracts retained indefinitely
- Admin can delete contracts via the admin UI

### 4.5 Notification Service
- Email dispatch via Azure Communication Services
- In-app notification creation and storage (always on, cannot be disabled)
- Configurable rent reminders (multiple per user, custom days-before + send time)
- Per-user email opt-in/out

---

## 5. Authentication & User Roles

### 5.1 Role Hierarchy

```
Super Admin
    └── Admin (created by Super Admin only)
            └── Tenant (created by Admin or Super Admin)
```

There is also a **Tester** role used for QA that has the same permissions as Admin but is flagged distinctly in the UI.

### 5.2 Role Permissions

| Action                             | Super Admin | Admin       | Tenant       |
|------------------------------------|-------------|-------------|--------------|
| Create Admin accounts              | ✅          | ❌          | ❌           |
| Create Tenant accounts             | ✅          | ✅          | ❌           |
| View all tenants                   | ✅          | ✅          | ❌           |
| View tenant public profile         | ✅          | ✅          | ❌           |
| View own transactions              | ✅          | ✅          | ✅ (own)     |
| Manage rent schedules              | ✅          | ✅          | ❌           |
| Upload/delete contracts            | ✅          | ✅          | ❌           |
| View contracts                     | ✅          | ✅          | ✅ (own)     |
| Approve/decline payment requests   | ✅          | ✅          | ❌           |
| Submit external payment request    | ❌          | ❌          | ✅           |
| Make Stripe payment                | ❌          | ❌          | ✅           |
| Manage own profile                 | ✅          | ✅          | ✅           |
| Toggle own email notifications     | ❌          | ✅          | ✅           |
| Receive notifications              | ❌          | ✅          | ✅           |
| Manage properties and units        | ✅          | ✅          | ❌           |
| Switch role (for testing)          | ✅          | ❌          | ❌           |

### 5.3 Super Admin Account
- Seeded at first deployment; credentials stored in Azure Key Vault
- Not created via the invite flow
- Does not receive any notifications; has no notification settings
- Can switch into any lower role to test the UI without creating separate test accounts

### 5.4 Account Creation Flow
1. Admin fills out an invite form (email + role)
2. Pending account record created; invite email sent with a 48-hour signup link
3. Invitee clicks link → sets password → scans TOTP QR code
4. Account becomes active (`IsActive = true`)
5. On first login, user is redirected to `/profile-setup` to enter name and phone before accessing the dashboard

### 5.5 Login Flow
1. Submit email + password → receive opaque temp token
2. Submit TOTP code + temp token → receive JWT access token (15 min) + refresh token (7 days)
3. Access token sent as `Authorization: Bearer` header on every API call
4. On 401, client silently retries once via `POST /api/auth/refresh`
5. Refresh failure → redirect to login

### 5.6 Profile Completion Gate
- `IsProfileComplete` is set to `false` on account creation
- Set to `true` when the user submits the profile form (first + last name, phone required; emergency contact optional)
- The dashboard layout checks this on every load and redirects to `/profile-setup` if false
- Fake dev accounts have profiles seeded on startup so they bypass this gate

---

## 6. Transactions

### 6.1 Transaction Types
- Monthly rent, security deposit, late fees, maintenance, utility, parking, pet fees, refunds/credits, other

### 6.2 Transaction States

| State      | Description                                                  |
|------------|--------------------------------------------------------------|
| `Pending`  | External payment submitted by tenant, awaiting admin approval|
| `Confirmed`| Payment completed (Stripe success or admin-approved external)|
| `Declined` | Admin rejected an external payment request                   |
| `Overdue`  | Past due date with no Confirmed or Pending payment           |

### 6.3 Payment Methods

There are three distinct payment paths:

**A — Stripe Card**
1. Tenant selects Card on the payment page; Stripe Elements collects card details
2. Frontend calls Transaction Service to create a PaymentIntent
3. Stripe processes the charge directly; on success, webhook auto-confirms the transaction
4. No admin approval needed; payment either succeeds or fails immediately

**B — Stripe ACH Direct Debit**
1. Tenant selects ACH on the payment page; Stripe Elements collects bank account + routing number
2. Same PaymentIntent flow as card; Stripe handles the debit
3. No admin approval needed

**C — External (Zelle, check, bank transfer, etc.)**
1. Tenant submits a payment request (amount, payment date, method note)
2. Transaction created with status `Pending`
3. Admin receives notification to verify and approve/decline
4. Approve → `Confirmed`, tenant notified; Decline → `Declined`, overdue cycle resumes

Stripe processing fees are shown transparently to the tenant before confirming:
- Card: 2.9% + $0.30
- ACH: 0.8%, capped at $5.00

### 6.4 Rent Schedules

Two billing modes can be set per unit at creation time:

- **PerTenant** — each co-tenant in the unit has their own independent rent schedule. Required for roommates paying separately.
- **SharedUnit** — one schedule covers the entire unit. Used when one party pays full rent or tenants split informally outside the system.

Admins create schedules on the Rent Schedule page. For PerTenant units, a specific tenant must be selected. For SharedUnit units, the schedule belongs to the unit (no tenant specified).

When a tenant calls `GET /api/rent-schedule/my`, the system checks for a per-tenant schedule first, then falls back to the unit's shared schedule.

### 6.5 Overdue Detection
- Nightly background job runs at midnight
- Marks transactions whose `due_date` has passed with no Confirmed or Pending payment as `Overdue`

### 6.6 Historical Data
- Admins can backfill historical transactions manually
- Transactions sorted by date descending by default

---

## 7. Properties & Units

### 7.1 Property Management
- Admins create and manage properties (name + address)
- Properties can be edited or soft-deleted
- A property cannot be deleted while it has active (non-deleted) units

### 7.2 Unit Management
- Units belong to a property; unit numbers must be unique within a property
- Fields: unit number, bedrooms, bathrooms, square feet, billing mode
- Units can be edited or soft-deleted
- A unit cannot be deleted while it has active tenant assignments
- Multiple tenants can be simultaneously assigned to one unit (co-tenancy)
- A single tenant cannot be assigned to more than one unit at a time

### 7.3 Admin Detail Views
- Clicking a unit opens a detail panel: unit specs, all current tenants with full public profiles (name, phone, emergency contact), and the unit's rent schedule
- Clicking a tenant from within a unit or the tenant list opens a tenant detail panel: profile info + all units they're currently assigned to
- Everything cross-links; admins can navigate from property → unit → tenant and back

---

## 8. Contracts

### 8.1 Storage
- Admins upload finalized PDF lease agreements
- PDFs stored in Azure Blob Storage; metadata in PostgreSQL
- Blob path: `contracts/{tenantId}/{unitId}/{contractId}`

### 8.2 Contract History
- All past and current contracts retained indefinitely (soft-delete only)
- New upload archives prior contracts for the same tenant/unit (`IsCurrent = false`)
- Admin can delete a contract (soft-delete removes it from all views)

### 8.3 Access Control

| Role        | Access                                               |
|-------------|------------------------------------------------------|
| Super Admin | View, preview, download, delete all contracts        |
| Admin       | View, preview, download, delete own uploaded contracts |
| Tenant      | View, preview, download own contracts (current + archived) |

### 8.4 Viewing
- **In-browser preview:** iframe rendered using a SAS URL with `ContentDisposition: inline`
- **Download:** SAS URL without content-disposition header (browser prompts download)
- Listing URLs have a 1-hour expiry; on-demand download URLs have a 15-minute expiry

---

## 9. Notifications

### 9.1 Channels
- **Email** — Azure Communication Services; toggleable per user
- **In-App** — notification bell/inbox; always on, cannot be disabled

### 9.2 Notification Events

| Event                              | Notifies              |
|------------------------------------|-----------------------|
| Payment confirmed (Stripe)         | Admin + Tenant        |
| External payment request submitted | Admin                 |
| External request approved          | Tenant                |
| External request declined          | Tenant                |
| Rent reminder (configurable)       | Tenant                |
| Payment overdue — day after        | Tenant                |
| Payment overdue — weekly repeat    | Tenant                |
| Account invite sent                | Invitee               |
| Contract uploaded                  | Tenant                |

### 9.3 Rent Reminders
- Default: 1 reminder, 7 days before due date
- Admin or Tenant can independently add multiple reminders
- Each reminder: days in advance + specific send time

### 9.4 Overdue Email Logic

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

### 9.5 Notification Email Addresses
- Each user can register multiple notification email addresses (separate from primary login email)
- Managed on the profile page: add or remove individual addresses
- At least one notification email is required at all times

---

## 10. Dashboard Views

### 10.1 Tenant Dashboard
- Current balance / next rent amount due
- Next rent due date + days remaining
- Recent transaction history
- Pending external payment requests (with status)
- Unread in-app notifications

### 10.2 Admin Dashboard
- List of all tenants with current payment status (Paid / Pending / Overdue)
- Pending external payment requests awaiting approval
- Upcoming rent due dates across all tenants
- Recent activity feed

### 10.3 Super Admin Dashboard
- System-wide overview: total admins, total tenants, total active leases
- Full list of all admins and their tenants
- Pending account invites

---

## 11. Database Schema (High-Level)

### Auth DB

**users**
```
id (UUID, PK)
email (varchar, unique)
password_hash (varchar)
totp_secret (varchar, encrypted)
role (enum: super_admin, admin, tenant, tester)
is_profile_complete (bool, default false)
is_active (bool)
is_deleted (bool)
deleted_at (timestamp, nullable)
refresh_token_hash (varchar, nullable)
refresh_token_expires_at (timestamp, nullable)
invited_by (UUID, FK → users, nullable)
created_at / updated_at (timestamp)
```

**user_profiles**
```
id (UUID, PK)
user_id (UUID, unique FK → users)
first_name (text)
last_name (text)
phone_number (text)
emergency_contact_name (text, nullable)
emergency_contact_phone (text, nullable)
created_at / updated_at (timestamp)
```

**user_notification_emails**
```
id (UUID, PK)
user_id (UUID, FK → users)
email (varchar, 256)
created_at (timestamp)
UNIQUE INDEX (user_id, email)
```

**invite_tokens**
```
id (UUID, PK)
email (varchar)
role (enum)
token_hash (varchar, unique)
expires_at (timestamp)
used (bool)
created_by (UUID, FK → users)
created_at (timestamp)
```

### Transaction DB

**properties**
```
id (UUID, PK)
name (text)
address (text)
is_active (bool, default true)
is_deleted (bool, default false)
deleted_at (timestamp, nullable)
created_by (UUID)
created_at / updated_at (timestamp)
```

**units**
```
id (UUID, PK)
property_id (UUID, FK → properties)
unit_number (varchar)
bedrooms (int, nullable)
bathrooms (decimal, nullable)
square_feet (int, nullable)
billing_mode (enum: per_tenant=0, shared_unit=1)
is_active (bool, default true)
is_deleted (bool, default false)
deleted_at (timestamp, nullable)
created_at / updated_at (timestamp)
PARTIAL UNIQUE INDEX (property_id, unit_number) WHERE is_deleted = false
```

**tenant_unit_assignments**
```
id (UUID, PK)
tenant_id (UUID)
unit_id (UUID, FK → units)
start_date (timestamp UTC)
end_date (timestamp UTC, nullable)   ← null = currently active
created_at / updated_at (timestamp)
```

**rent_schedules**
```
id (UUID, PK)
tenant_id (UUID, nullable)           ← null for SharedUnit; set for PerTenant
unit_id (UUID, FK → units)
monthly_amount (decimal)
due_day_of_month (int, 1–28)
start_date (timestamp UTC)
created_by (UUID)
created_at / updated_at (timestamp)
```

**transactions**
```
id (UUID, PK)
tenant_id (UUID)
unit_id (UUID)
type (enum: rent, deposit, late_fee, maintenance, utility, parking, pet, refund, other)
amount (decimal)
status (enum: pending, confirmed, declined, overdue)
payment_method (enum: stripe, ach, external, manual)
external_method_note (text, nullable)
stripe_payment_intent_id (varchar, nullable)
due_date (timestamp, nullable)
paid_date (timestamp, nullable)
is_deleted (bool, default false)
deleted_at (timestamp, nullable)
created_by (UUID)
created_at / updated_at (timestamp)
```

### Contract DB

**contracts**
```
id (UUID, PK)
tenant_id (UUID)
unit_id (UUID)
blob_storage_path (text, unique)
file_name (text)
is_current (bool)
is_deleted (bool, default false)
deleted_at (timestamp, nullable)
uploaded_by (UUID)
uploaded_at / updated_at (timestamp)
```

### Notification DB

**notifications**
```
id, user_id, type (enum), message, is_read, created_at
```

**notification_preferences**
```
id, user_id (unique), email_enabled (bool), updated_at
```

**reminder_settings**
```
id, user_id, days_before, send_time, is_active, created_at, updated_at
```

---

## 12. API Design (Current State)

### Auth & Account
```
POST /api/auth/login                          — email + password → temp token
POST /api/auth/login/totp                     — TOTP → access + refresh tokens
POST /api/auth/register                       — complete invite registration
POST /api/auth/refresh                        — rotate refresh token
POST /api/auth/logout                         — revoke refresh token
POST /api/auth/invite                         — send account invite (admin+)
POST /api/auth/dev-login                      — TOTP-skipping login (dev only)
POST /api/auth/switch-role                    — SuperAdmin role-switch token
GET  /api/auth/users                          — list users (admin-scoped)
GET  /api/auth/users/{id}/public-profile      — public profile (admin+)
GET  /api/auth/account/profile                — own full profile
PUT  /api/auth/account/profile                — update own profile
POST /api/auth/account/notification-emails    — add notification email
DELETE /api/auth/account/notification-emails/{id} — remove notification email
PUT  /api/auth/account/primary-email          — change login email
PUT  /api/auth/account/password               — change password
DELETE /api/auth/account                      — delete own account
```

### Properties & Units
```
GET    /api/properties                        — list (admin-scoped)
POST   /api/properties                        — create
PUT    /api/properties/{id}                   — update name/address
DELETE /api/properties/{id}                   — soft-delete (blocked if active units)
GET    /api/units                             — list with currentTenantIds
POST   /api/units                             — create (with billingMode)
PUT    /api/units/{id}                        — update details/billing mode
DELETE /api/units/{id}                        — soft-delete (blocked if active tenants)
POST   /api/units/{id}/assign-tenant          — assign tenant
DELETE /api/units/{id}/remove-tenant/{tid}    — end tenant assignment
GET    /api/units/{id}/rent-schedule          — shared-unit schedule (TenantId=null)
```

### Transactions
```
GET    /api/transactions                      — list (role-scoped)
GET    /api/transactions/{id}                 — get by ID
POST   /api/transactions                      — create manual/backfill (admin+)
POST   /api/transactions/external             — submit external payment request
POST   /api/transactions/payment-intent       — create Stripe PaymentIntent
PATCH  /api/transactions/{id}/approve         — approve external request
PATCH  /api/transactions/{id}/decline         — decline external request
DELETE /api/transactions/{id}                 — soft-delete
POST   /api/webhooks/stripe                   — Stripe webhook (no JWT)
```

### Rent Schedules
```
GET    /api/rent-schedule/my                  — own schedule (per-tenant first, then shared)
GET    /api/rent-schedule/{tenantId}          — by tenant (own only for Tenant role)
GET    /api/rent-schedules                    — all (admin-scoped)
POST   /api/rent-schedule                     — create
PATCH  /api/rent-schedule/{id}                — update amount/due day
DELETE /api/rent-schedule/{id}                — delete
```

### Contracts
```
GET    /api/contracts                         — list (role-scoped); includes PreviewUrl
GET    /api/contracts/{id}                    — single contract
GET    /api/contracts/{id}/download           — fresh 15-min SAS download URL
POST   /api/contracts/upload                  — upload PDF (admin+)
DELETE /api/contracts/{id}                    — soft-delete (admin+)
```

### Notifications
```
GET    /api/notifications
PATCH  /api/notifications/{id}/read
GET    /api/notification-preferences
PATCH  /api/notification-preferences
GET    /api/reminders
POST   /api/reminders
PATCH  /api/reminders/{id}
DELETE /api/reminders/{id}
```

---

## 13. Deployment & Infrastructure

- **Backend:** Azure Container Apps, one container per service
- **Frontend:** Azure Blob Storage static website + Azure CDN + custom domain
- **Database:** Azure Database for PostgreSQL (Flexible Server)
- **Secrets:** Azure Key Vault (managed identity, no hardcoded credentials)
- **CI/CD:** GitHub Actions — build, test, push Docker images to Azure Container Registry, deploy to Container Apps on merge to main

---

## 14. Future Add-ons (Out of Scope for v1)

- **Payment failure notifications** — admin alert when Stripe card/ACH fails (`payment_intent.payment_failed` webhook)
- **Transaction detail views** — full history filterable by tenant or unit; visible from unit/tenant detail panels
- **DocuSign integration** — e-signature flow (Contract Service designed to accommodate via interface)
- **Maintenance request system** — tenant-submitted requests, admin status management
- **Property-scoped admin permissions** — admins restricted to assigned properties
- **Admin monthly digest** — email summary of all transactions
- **Tenant communication thread** — in-app messaging between admin and tenant
- **Queue-based notification processing** — Azure Service Bus for retry and fault isolation
- **Payment analytics dashboard** — occupancy rates, monthly revenue, delinquency trends

---

## 15. Development Notes

- **Stripe webhook** bypasses JWT; verified via `Stripe-Signature` HMAC header before processing
- **Overdue job** runs as .NET `IHostedService` at midnight daily
- **SAS tokens:** listing URLs are 1-hour; on-demand download URLs are 15-minute; preview URLs add `ContentDisposition: inline`
- **Refresh tokens** stored as SHA-256 hashes; rotated on every use; single active token per user
- **Soft deletes:** `is_deleted = true` + `deleted_at = now()` everywhere; all queries filter `is_deleted = false`
- **February edge case:** if `due_day_of_month > 28`, `DateHelper.GetAdjustedDueDate` clamps to `DateTime.DaysInMonth`
- **Defense in depth:** every internal service validates JWT and resource ownership independently of the Gateway
- **Secrets abstraction:** `ISecretsProvider` with `LocalSecretsProvider` (dev) and `AzureVaultSecretsProvider` (prod); `secrets.json` is never committed
- **Correlation ID:** `X-Correlation-ID` UUID set at Gateway, logged on every service request, enables cross-service trace reconstruction
- **Enum serialization:** all services use `JsonStringEnumConverter` so enums serialize as strings (e.g. `"PerTenant"` not `0`)
- **Profile gate:** `IsProfileComplete` checked client-side in `layout.tsx`; redirects to `/profile-setup` before any dashboard content loads
