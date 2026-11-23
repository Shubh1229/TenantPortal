# Architecture Decisions

## 001 — Self-host instead of Azure
**Decided:** 2026-11-22

Run on a home Ubuntu Server machine (former Alienware desktop: 16 GB DDR4, single 2 TB HDD) instead of an Azure VM. Removes the monthly VM cost, and the first release serves family properties only.

**Consequences**
- A single disk means automated off-machine Postgres + file backups are required, not optional.
- Home power or internet outages take the site down — acceptable for family use; revisit (UPS at minimum) before onboarding other landlords.
- Use Cloudflare Tunnel so no inbound ports are opened on the home router and the changing home IP doesn't matter.

## 002 — Modular monolith instead of microservices
**Decided:** 2026-11-22

One ASP.NET Core application with modules (Auth, Payments, Leases, Notifications, …) and one PostgreSQL database.

**Why:** one box to run it on; real database transactions across features (split rent touches tenants, leases and payments at once); one place to look for logs; one database to back up. The microservices version already exists on the `azure` branch.

**Rules to keep it modular**
- Each module owns its own Postgres schema; no module queries another module's tables.
- Modules talk to each other only through public interfaces (or in-process events), never through internal classes.
- These boundaries keep the option open to split a module into a service later.

## 003 — New project, ported logic
**Decided:** 2026-11-22

Fresh project structure, but proven logic from the `azure` branch is carried over and cleaned up rather than rewritten blind: TOTP login, refresh-token rotation, Stripe payments, Stripe Connect, webhook verification.

## 004 — Fresh Next.js frontend
**Decided:** 2026-11-22

New Next.js project with a redesigned UI, not a copy of the existing pages.
