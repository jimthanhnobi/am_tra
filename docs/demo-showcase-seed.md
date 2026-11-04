# TeaPremium — Development demo showcase seed

This seed is an **opt-in Development fixture**, not a copy of a real customer database and not a Production import. It is designed for testing the premium-teaware Storefront UI, Admin Catalog/CRM, product-image gallery, CMS, and revenue attribution screens.

## Reference design

Visual inspiration: [IdeaToCodee/runway](https://github.com/IdeaToCodee/runway). The reference repository contains a Shopify / sportswear storefront snapshot with third-party imagery. We **do not copy any HTML, JavaScript, product text, brand marks, or images**. TeaPremium borrows only general layout ideas: larger focal product images, clean editorial rhythm, bolder product-card hierarchy and mobile-first browsing. Teaware product information remains its own design.

## Included data

| Data set | Quantity | Notes |
|---|---:|---|
| Categories | 5 | Tử sa, gang, thủy tinh, bộ trà/chén, khay trà |
| Brand | 1 | TeaPremium · Dữ liệu mẫu |
| Products | 7 | Published, valid category, brand, description, SEO, specs, cover |
| Media | 9 | Bundled, original SVG demo illustrations; two extra gallery angles |
| Articles | 3 | Published; Tiptap JSON + HTML, author from Development Admin |
| Article categories | 1 | Demo kiến thức trà cụ |
| Customers | 8 | Fake names and phone numbers, with acquisition sources |
| Interactions | 8 | Fake consultation history, recognizable [DEMO] note |
| Orders | 9 | 7 Completed, 1 Confirmed, 1 Cancelled; mixed channels, costs |
| Leads | 4 | Synthetic website form leads, marked [DEMO] |

All data is **synthetic**. Product names and images are explicitly marked `[DEMO]` / `DEMO ILLUSTRATION`. Material, origins, capacities, quoted unit prices and costs are invented **for interface testing only**. No Zalo OA messages or external requests are sent; no real sales or consent events are being represented.

### Revenue preview

The sales report is always read from actual `sales_orders` rows, not a hardcoded metric. The completed sample orders populate acquisition channels such as `legacy_base`, `google_organic`, `zalo_oa`, `referral` and `facebook`, as well as closing channels `website`, `zalo_personal`, `zalo_oa` and `phone`. You can verify repeat customers and the `unknown` purchase-history category. A completed sample order has an unknown cost to validate the reporting warning.

## Run locally with Docker Compose

Prerequisites: local PostgreSQL schema migrated to the latest EF migration, `ASPNETCORE_ENVIRONMENT=Development` (the Compose file already sets this), and an Admin user already created through `bootstrap-admin`.

Run this **only for the local Compose DB**; review your `.env` first:

```powershell
git checkout feature/project-foundation
git pull --ff-only origin feature/project-foundation

# Review .env and point it at YOUR isolated local database.
# Start PostgreSQL, run the existing manual migration, build API and Web.
docker compose --env-file .env -f infra/docker/compose.yml up -d postgres
docker compose --env-file .env -f infra/docker/compose.yml build migrate
docker compose --env-file .env -f infra/docker/compose.yml run --rm migrate
docker compose --env-file .env -f infra/docker/compose.yml up -d --build api web

# If Admin has not been bootstrapped, configure AuthBootstrap__Email and
# AuthBootstrap__Password in your shell as described in README.md.
# docker compose --env-file .env -f infra/docker/compose.yml exec -e AuthBootstrap__Email -e AuthBootstrap__Password api dotnet TeaPremium.Api.dll bootstrap-admin

# Explicit opt-in seed (not executed on application startup).
docker compose --env-file .env -f infra/docker/compose.yml exec -e TEAPREMIUM_DEMO_SEED_CONFIRM=local-demo-only api dotnet TeaPremium.Api.dll seed-demo-showcase
```

The API refuses seeding unless all these conditions are true:

1. `ASPNETCORE_ENVIRONMENT=Development`.
2. `TEAPREMIUM_DEMO_SEED_CONFIRM=local-demo-only` is set in the seed process.
3. Connection-string host is `localhost`, `127.0.0.1` or the local Docker service `postgres`.
4. Database is named `teapremium` (the standard local Compose database) or starts with `teapremium_demo`.
5. All schema migrations are applied and an Admin account exists.

Never point the local Compose database setting at a remote/Production server. Do not bypass these safeguards or enable demo seeding in a public deployment.

## Run with dotnet locally (Windows PowerShell)

Configure your existing local `ConnectionStrings__Postgres` or user secret and ensure the Admin bootstrap/migrations are complete:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:TEAPREMIUM_DEMO_SEED_CONFIRM = "local-demo-only"
dotnet run --project backend/src/TeaPremium.Api -- seed-demo-showcase
Remove-Item Env:\TEAPREMIUM_DEMO_SEED_CONFIRM
```

## URLs to inspect

- `http://localhost:3000/` — editorial hero, Published product + collection cards
- `http://localhost:3000/tim-kiem?q=DEMO` — seven product cards
- `http://localhost:3000/san-pham/demo-am-tu-sa-tay-thi` — gallery with two illustrations
- `http://localhost:3000/kien-thuc` — three seeded articles
- `http://localhost:3000/admin` — Catalog/Sales dashboard
- `http://localhost:3000/admin/customers` — demo CRM and interactions
- `http://localhost:3000/admin/orders` — synthetic orders
- `http://localhost:3000/admin/reports` — completed-order revenue and channel breakdown
- `http://localhost:3000/admin/leads` — four demo leads
- `http://localhost:3000/demo-products/am-tu-sa-tay-thi.svg` — bundled illustration, no R2 required

### Media implementation

Assets are original static `apps/web/public/demo-products/*.svg` illustrations. The corresponding database `MediaAsset.StorageKey` uses the strict `demo-products/<slug>.svg` namespace. Only that whitelisted name pattern resolves to a root-relative static path; all normal uploaded files continue to use the existing Cloudflare R2 signed-upload flow. The catalog and article APIs return these demo asset URLs even if R2 upload is unavailable.

The public API's URL parser permits exactly these bundled paths in addition to HTTPS/HTTP image URLs. The special path does not enable arbitrary uploads or local filesystem reads. No SVG is accepted through regular user media upload.

## Idempotency & limitations

- Re-running the command queries **fixed slugs**, media storage keys and normalized phones and **skips existing records**. Order `request_id` is deterministic, and demo interaction/lead identifiers use fixed message content.
- All seed operations are in one database transaction. If any SQL operation fails, the seed transaction rolls back instead of leaving half the demo set.
- If you edit a demo record after seeding, rerunning **does not reset it**. Do not rename the demo slug or repurpose demo phone values for actual customers. Use a fresh isolated local database for a clean reset.
- Do not run `down --volumes` on a database with any important data. To reset only demo rows, prefer restoring an empty disposable local DB from a snapshot. This seed deliberately includes **no destructive cleanup**.
- Reports display the latest month by default. Completed fixtures are created at seed time, so to see revenue, choose a date range that includes the day you ran the seed.
- Demo images are **illustrations, not product photographs**. Before public release, replace them with genuine photography and verify material claims, descriptions, SEO titles, real channel contacts and prices.
- `SITE_INDEXABLE` should stay `false` while any demo products remain publicly reachable.

## QA checklist

- Run seed twice and verify counts do not double.
- Open product detail and select secondary gallery thumbnail.
- Verify the `/lien-he?san-pham=...` flow prefills a message with the Published product.
- Confirm all other Catalog content remains unchanged.
- Admin reports must show only Completed orders; Cancelled and Confirmed are excluded.
- Compare `byClosingChannel` and `byAcquisitionSource`; do not add them together.
- Test 375px, 768px, and 1440px widths, navigation via keyboard and sensible no-image fallbacks.
- Run backend unit/integration tests plus frontend lint, typecheck, SEO/HTTP smoke, production build before merging.
