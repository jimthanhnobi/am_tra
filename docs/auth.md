# Authentication & Authorization ADR

Status: Accepted for MVP (2026-10-30)

## Decision

- ASP.NET Core Identity is the only authentication authority. Users and roles use `Guid` keys in the existing PostgreSQL `AppDbContext`; permissions are Identity role claims with claim type `permission`.
- Identity store schema is pinned to Version 2 for this password-and-cookie MVP. .NET 10 passkey schema Version 3 and its extra table are deferred until a passkey flow is explicitly designed and tested with PostgreSQL.
- The browser calls same-origin Next.js Route Handlers under `/api/admin/*`. The handler forwards the allowlisted request headers, body, and incoming cookies to `/api/v1/admin/*`, then relays all `Set-Cookie` headers. No access or refresh token is created, returned to a Client Component, or stored in Web Storage.
- Server Components call ASP.NET Core directly from the Next.js server and forward the incoming cookie. This avoids an unnecessary self-HTTP hop while preserving the BFF trust boundary: the browser still only talks to Next.js.
- ASP.NET Core remains the enforcement point. Every `/api/v1/admin/*` controller must inherit `AdminApiControllerBase`, which requires authentication and antiforgery validation for unsafe methods; anonymous auth actions opt out explicitly. Each sensitive admin operation additionally requires a named permission policy. Public controllers remain separate and explicit. Next.js redirects are only UX guards.
- Cookie authentication uses `HttpOnly`, `SameSite=Lax`, `Path=/`, a 30-minute sliding lifetime, and `Secure=Always` with a `__Host-` name outside Development. Development permits HTTP localhost and uses a non-`__Host-` name. No cookie domain is configured.
- Unsafe admin requests require ASP.NET Core antiforgery validation. A same-origin `GET` obtains a request token and HttpOnly antiforgery cookie; the BFF forwards the request token in `X-CSRF-TOKEN`. The token is refreshed after login because antiforgery tokens are identity-bound. The BFF also rejects unsafe requests whose `Origin` does not equal its own origin.
- Login has a fixed-window rate limit partitioned by the peer address observed by ASP.NET Core, plus per-account Identity lockout. In the default BFF topology that peer can be the Next.js service, so production proxy/client-IP partitioning must be reviewed rather than trusting arbitrary forwarded headers. Authentication failures use one generic response so callers cannot distinguish an unknown account, a wrong password, or a locked account.
- `POST /api/v1/admin/users` requires `Users.Manage` plus CSRF and creates only Editor or Publisher accounts; there is no public self-registration. Identity validates the password, and user creation plus role assignment are committed together. A future role/permission editing use case must update affected users' security stamps so revoked claims are rejected at the next validation interval. Authentication tickets remain limited to 30 minutes and are revalidated every 5 minutes.
- There is no CORS policy for Admin cookies because browsers never call ASP.NET Core cross-origin. If that topology changes, only explicit trusted origins with credentials may be configured; wildcard CORS is forbidden.

## Roles and initial permissions

| Role | Permissions |
|---|---|
| `Admin` | All permissions, including `Media.Write` |
| `Editor` | `Catalog.Read`, `Catalog.Write`, `Media.Write`, `Content.Read`, `Content.Write`, `SEO.Manage` |
| `Publisher` | Editor permissions plus `Catalog.Publish`, `Content.Publish` |

Admin-only permissions are `Leads.Read`, `Leads.Manage`, `Settings.Manage`, and `Users.Manage`. The role and role-claim rows are seeded by migration; no user or privileged account is seeded by migration or production startup.

## Local privileged bootstrap

The explicit `bootstrap-admin` command is Development-only. It reads `AuthBootstrap__Email` and `AuthBootstrap__Password` from process environment or the API user-secrets store, creates the user if absent, and grants the existing `Admin` role. It never runs during normal startup and requires migrations to have been applied separately.

Example with user-secrets (run from the repository root after applying the reviewed migration):

```powershell
dotnet user-secrets set --project backend/src/TeaPremium.Api "AuthBootstrap:Email" "your-admin@example.com"
dotnet user-secrets set --project backend/src/TeaPremium.Api "AuthBootstrap:Password" "use-a-unique-local-secret"
dotnet run --project backend/src/TeaPremium.Api -- bootstrap-admin
```

Remove or rotate the bootstrap values after use. The command is idempotent for an existing user and role assignment; it does not expose the configured password in its output.

## Request flow

```text
Browser
  -> Next.js /api/admin/auth/csrf (same origin)
  -> ASP.NET Core /api/v1/admin/auth/csrf
  <- antiforgery Set-Cookie + request token

Browser
  -> Next.js /api/admin/auth/login (cookie + X-CSRF-TOKEN)
  -> ASP.NET Core Identity (password, lockout, rate limit)
  <- HttpOnly Identity Set-Cookie

Browser / Server Component
  -> Next.js BFF/server data access (Identity cookie)
  -> ASP.NET Core authentication + permission policy
  <- narrow current-user DTO or 401/403
```

## Rejected alternatives

- JWT/refresh tokens in browser storage: unnecessary for the same-origin Admin and increases token exposure and refresh complexity.
- Next.js-only authorization: cannot protect the .NET API and can be bypassed by direct requests.
- `SameSite=None`: not required for the selected same-origin topology and broadens cookie delivery.
- Automatic production admin seeding or startup migrations: unsafe and conflicts with controlled deployment.

## Verification boundary

Integration tests must use the real Identity password hasher, cookie handler, antiforgery system, policies, and PostgreSQL schema. Production `Secure` cookie behavior, TLS termination, forwarded headers, and `__Host-` acceptance require an HTTPS staging/domain check; local HTTP tests cannot prove them.

Before production release, verify on the actual HTTPS domain that the browser accepts only the `__Host-TeaPremium.Admin` and `__Host-TeaPremium.Csrf` cookies, both cookies are `Secure`, the auth cookie is `HttpOnly` and `SameSite=Lax`, logout expires it, and no admin response is cached. Confirm the reverse proxy preserves the original scheme/host, the internal `API_BASE_URL` uses the intended TLS topology, rate-limit client partitioning matches that proxy topology, and Data Protection keys persist securely across restarts/instances.

## Primary references

- [ASP.NET Core Identity](https://learn.microsoft.com/aspnet/core/security/authentication/identity?view=aspnetcore-10.0)
- [Identity configuration and lockout](https://learn.microsoft.com/aspnet/core/security/authentication/identity-configuration?view=aspnetcore-10.0)
- [Policy-based authorization](https://learn.microsoft.com/aspnet/core/security/authorization/policies?view=aspnetcore-10.0)
- [ASP.NET Core antiforgery](https://learn.microsoft.com/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0)
- [ASP.NET Core rate limiting](https://learn.microsoft.com/aspnet/core/performance/rate-limit?view=aspnetcore-10.0)
- [Next.js authentication](https://nextjs.org/docs/app/guides/authentication)
- [Next.js Backend for Frontend](https://nextjs.org/docs/app/guides/backend-for-frontend)
