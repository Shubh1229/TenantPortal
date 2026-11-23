# Singh Resident Hub — Home Server Edition

Residential rental management platform, rebuilt to run on a self-hosted Ubuntu server instead of Azure.

> **Status:** foundation only. No application code yet — development starts on this branch.
> The original Azure-hosted microservices version is preserved on the `azure` branch and the `azure-final` tag.

## Branches

| Branch | Purpose |
|---|---|
| `homeserver` | New build (this branch). Becomes `main` once it is live. |
| `azure` | Frozen Azure version: 5 ASP.NET Core microservices, YARP gateway, gRPC, Key Vault, Blob Storage, ACS email. |
| `master` | Current default; will be retired when `homeserver` becomes `main`. |

## Planned stack

- **Backend:** ASP.NET Core modular monolith — one app, one PostgreSQL database, a schema per module
- **Frontend:** Next.js, fresh UI
- **Hosting:** Docker Compose on a home Ubuntu Server box
- **Ingress:** Cloudflare Tunnel (no router ports opened)
- **Admin access:** Tailscale
- **Secrets:** local, never committed (replaces Azure Key Vault)
- **Payments:** Stripe (ported from the `azure` branch)

See [docs/DECISIONS.md](docs/DECISIONS.md) for the reasoning and [docs/ROADMAP.md](docs/ROADMAP.md) for scope.
