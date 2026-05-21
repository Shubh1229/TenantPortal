# TenantPortal — Frontend

Next.js (App Router) frontend for the TenantPortal residential rental management platform. Communicates exclusively through the API Gateway (`localhost:5000` in local dev).

## Stack

- **Next.js** (App Router, `'use client'` components throughout)
- **TypeScript**
- **Tailwind CSS v4**
- **shadcn/ui** — dark zinc theme
- **Stripe.js / React Stripe Elements** — card and ACH payment UI
- **Lucide React** — icons

## Local Development

```bash
cd frontend
npm install
npm run dev
```

Runs at `http://localhost:3000`. The backend gateway must be running at `http://localhost:5000`.

Set `NEXT_PUBLIC_API_URL` in `.env.local` if your gateway runs on a different port:

```env
NEXT_PUBLIC_API_URL=http://localhost:5000
NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY=pk_test_...
```

## Dev Login (No TOTP Required)

Three hardcoded test accounts are available in development via the `/api/auth/dev-login` endpoint. The login page detects the dev environment and surfaces a one-click login for each.

| Account | Email | Password | Role |
|---|---|---|---|
| fakeadmin | `fakeadmin@example.com` | `admin123` | Admin |
| faketenant | `faketenant@example.com` | `tenant123` | Tenant |
| faketester | `faketester@example.com` | `tester123` | Tester |

The SuperAdmin role can also switch into any lower role from the dashboard for UI testing.

## Route Structure

```
app/
  (auth)/
    login/              — two-step login (password → TOTP)
    register/           — invite-based registration + TOTP QR enroll
  (dashboard)/
    layout.tsx          — auth guard, profile-completion gate, role-based nav
    profile-setup/      — forced first-time profile completion
    profile/            — view/edit personal info + notification emails
    settings/           — primary email, password, account deletion
    tenant/
      dashboard/        — balance, next due date, recent transactions
      payment/          — Stripe card / ACH / external payment flow
      payment/complete/ — Stripe redirect landing page
      transactions/     — full transaction history
      contracts/        — view + in-browser PDF preview of own contracts
    admin/
      dashboard/        — tenant status overview, pending approvals
      properties/       — property + unit CRUD, tenant assignment, unit/tenant detail views
      transactions/     — all transactions, approve/decline external requests
      contracts/        — upload, in-browser preview, delete contracts
      rent-schedule/    — create, edit (inline), delete rent schedules
    superadmin/
      dashboard/        — system overview
      admins/           — admin management
      settings/         — Stripe subscription portal
```

## Authentication Flow

1. Tokens stored in `localStorage` (`accessToken`, `refreshToken`)
2. `apiRequest()` in `lib/api/client.ts` attaches `Authorization: Bearer` header automatically
3. On 401, attempts one silent refresh via `POST /api/auth/refresh`
4. On refresh failure, redirects to `/login`
5. `layout.tsx` checks `isProfileComplete` on every load — redirects to `/profile-setup` if false

## Profile Completion Gate

After registration, users are required to complete their profile (first name, last name, phone number, optional emergency contact) before accessing any dashboard page. This is enforced client-side in `layout.tsx` via the `GET /api/auth/account/profile` response.

## Payment Flow

Three distinct payment paths on the tenant payment page:

- **Card** — Stripe Elements, direct charge, no admin approval needed
- **ACH Direct Debit** — Stripe Elements, bank account debit, no admin approval needed
- **External** (Zelle, check, etc.) — submits a pending request that requires admin approval

Stripe processing fees are shown transparently to the tenant before confirming (2.9% + $0.30 for card; 0.8% capped at $5 for ACH).

## Key Files

| File | Purpose |
|---|---|
| `lib/api/client.ts` | Base `apiRequest` with auth header + refresh token retry |
| `lib/api/auth.ts` | Auth, profile, notification email, account endpoints |
| `lib/api/transactions.ts` | Transactions, properties, units, rent schedules |
| `lib/api/contracts.ts` | Contract list, upload, download, delete |
| `lib/hooks/useAuth.ts` | Auth state hook — reads JWT claims from localStorage |
| `lib/utils/stripe.ts` | Fee calculation helpers (`cardTotalWithFee`, `achTotalWithFee`) |
| `types/index.ts` | All shared TypeScript interfaces and enums |
| `components/ui/` | shadcn/ui primitives (Button, Card, Input, Badge, etc.) |
