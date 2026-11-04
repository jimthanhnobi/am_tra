# CRM + Internal Sales + Channel Reporting — TeaPremium

## Scope

This extension is for a **single-operator shop with 400–500 existing contacts**.
Existing public storefront is still a showcase, without online cart, checkout, public price or payment.
Sales records are **created privately by the Admin**, after an actual consultation.

Use branch `feature/project-foundation` as the only baseline.
Retain existing Catalog, CMS, Leads, SEO, Media, Identity and CSRF behavior.
Customer/Order/Report endpoints share the established `Leads.Read` / `Leads.Manage` policies; do not grant those permissions to public users.

## Data model

- `customers`: unique Vietnamese E.164-normalized phone, initial acquisition source, origin (legacy_import/new/unknown), explicit prior-purchase indicator and bounded text tags. The customer does **not** inherit consent from their phone number.
- `customer_interactions`: handwritten consulting history with channel, kind, notes, timestamp. This is a log, not automated reading of personal Zalo messages.
- `sales_orders`: private admin-created confirmed/completed/cancelled order, stable idempotency `request_id`, unique human-readable code, immutable acquisition and returning status snapshot, optional existing Lead ID.
- `sales_order_items`: SKU/product-name snapshots, quantity, monetary unit-price and optional historical unit-cost; product link is internal and does not change the snapshot.
- Existing `leads` and `contact_events` remain intact: form submission is a Lead; click is **not** a Lead nor a completed OA conversation.

No wholesale/public prices are introduced by this extension.

## Endpoints

API root: `/api/v1`; all are **Admin-only**, cookie + CSRF on unsafe methods.

| Method | Path | Action |
|---|---|---|
| GET | `admin/customers?page=1&pageSize=20&search=` | Customer search + pagination |
| POST | `admin/customers` | New customer |
| GET | `admin/customers/{id}` | Customer detail |
| PATCH | `admin/customers/{id}` | Update name/tags and confirm prior purchase; phone/source immutable here |
| POST | `admin/customers/import` | JSON batch import, preview with `dryRun: true` |
| GET | `admin/customers/{id}/interactions` | Latest 100 notes |
| POST | `admin/customers/{id}/interactions` | Add manual note |
| GET | `admin/orders` | Order listing |
| POST | `admin/orders` | Create internal confirmed order |
| GET | `admin/orders/{id}` | Detail + immutable item snapshots |
| POST | `admin/orders/{id}/complete` | Complete a Confirmed order |
| POST | `admin/orders/{id}/cancel` | Cancel a Confirmed order |
| GET | `admin/reports/sales-overview?from=...&to=...` | Completed orders in exclusive UTC interval |

Example create customer:

```json
{
  "name": "Khách hàng mẫu",
  "phone": "0912345678",
  "origin": "legacy_import",
  "acquisitionSource": "legacy_base",
  "hadPriorPurchase": true,
  "tags": ["wholesale", "vip"]
}
```

Example import request:

```json
{
  "dryRun": true,
  "items": [
    {
      "name": "Khách hàng mẫu",
      "phone": "0912345678",
      "origin": "legacy_import",
      "acquisitionSource": "legacy_base",
      "hadPriorPurchase": false,
      "tags": ["reseller"]
    }
  ]
}
```

Create an order with a client-generated stable UUID `requestId` reused on retry.
`productId` must point to a **Published** product; price is private, chosen by admin.

```json
{
  "customerId": "REPLACE_WITH_CUSTOMER_GUID",
  "requestId": "REPLACE_WITH_UNIQUE_GUID",
  "leadId": null,
  "closingChannel": "zalo_oa",
  "campaignCode": "new-arrivals-nov",
  "items": [
    {
      "productId": "REPLACE_WITH_PUBLISHED_PRODUCT_GUID",
      "quantity": 2,
      "unitPrice": 1250000,
      "unitCost": 800000
    }
  ]
}
```

In the report: a completed order belongs to **one closing channel** and **one original acquisition source**.
These are independent *alternative breakdowns*, **not additive**. Unverified acquisition stays `unknown`.
The report counts `CompletedAtUtc` in the half-open range `[from, to)`, not draft/confirmed orders.
`KnownCost` excludes order lines without cost: `UnknownCostLines > 0` means gross profit is incomplete.
Orders with earlier imported purchases can set `HadPriorPurchase=true`; this snapshot cannot infer true purchase history from contact alone.

## Import 400–500 existing contacts

1. Export contacts to a CSV/Excel file locally. Do **not** commit the list or screenshots to Git.
2. Normalize phone numbers, remove duplicates, mark `origin=legacy_import`; choose `legacy_base` only when supported.
3. Import in a test database with `dryRun=true`. Inspect counts and invalid rows.
4. Back up database; then send `dryRun=false`. Repeat is safe for already-existing numbers (skips, does not overwrite).
5. Inspect customer list; only set `hadPriorPurchase` when actual prior purchase is known.
6. Consent for promotional messages needs its own evidence. Importing a phone does not authorize a Zalo broadcast.

## Admin usage

- `/admin/customers`: manually create/search customers, page through customers, and record consultation notes.
- `/admin/customers/import`: import UTF-8 CSV from Excel, preview with `dryRun=true`, then explicitly confirm. Only `name,phone` (or `ten,sdt`) columns are required. Phone duplicates are skipped, not overwritten.
- `/admin/orders`: enter confirmed orders from calls, OA, or personal Zalo. The initial form records one product per order; API accepts 1–50 lines.
- `/admin/reports`: reporting by **closing channel** and **acquisition source**, with completed order totals.

Use `zalo_personal` for personal Zalo, `zalo_oa` for OA, and `website` only when the purchase is actually confirmed on the website. A website lead that ends in OA stays an OA closing-channel order.

## Security / data integrity

- Customer phones are personal data; do not expose data in storefront/GA4, URLs, analytics logs or application exceptions.
- All admin routes require Identity authentication and existing anti-CSRF validation.
- Import is bounded to 1,000 records/request, no overwrites; unique indexes protect repeated numbers and request IDs.
- Error responses must use ProblemDetails 404/409/422.
- Take a backup and review the additive migration before deploying; Production API must not automatically migrate.
- Only `Confirmed -> Completed` or `Confirmed -> Cancelled` is allowed; cancelled orders have zero completed revenue.
- Refunds, partial payments, discounts, shipping, multiple currencies, Zalo OA webhook automation, campaign-spend ROI and cross-device attribution are **not included** in this first MVP. Do not present the completed-revenue number as cash received.
- Imported contacts or new OA followers are not proof of newly acquired customers.

## Verification

From repository root in an environment with .NET 10, pnpm and PostgreSQL:

```powershell
dotnet restore backend/TeaPremium.sln --locked-mode
dotnet build backend/TeaPremium.sln -c Release --no-restore
dotnet test backend/TeaPremium.sln -c Release --no-build
corepack pnpm --dir apps/web install --frozen-lockfile
corepack pnpm --dir apps/web lint
corepack pnpm --dir apps/web typecheck
corepack pnpm --dir apps/web build
```

Use an isolated database and run the reviewed EF migration manually.
Test the same `requestId` twice, duplicate import, unauthenticated and CSRF failure, different closing/source assignments, customer returning flag, cancelled/confirmed/completed report inclusion, zero/unknown costs and date boundaries.
Do not claim tests have passed unless observed in CI or local runs.

## Known MVP limitations and next iterations

- Public storefront continues to have no checkout or customer-login functionality.
- CRM import is CSV, not an XLSX file reader; Excel users should export UTF-8 CSV with comma separators.
- Customers are paginated in the Admin. The create-order product selector currently loads the first 50 Published products; product search and multi-line order editing are follow-ups.
- Existing Leads remain web-form leads; personal-Zalo consultations are recorded as customer interaction notes, not treated as website leads. Explicit lead/customer linking and lead-to-order conversion should be added when validated by real usage.
- Marketing opt-in/opt-out collection and verified OA UID association are separate follow-up modules; **do not send bulk outreach based solely on imported phone numbers**.
- Import rows receive the selected cohort tag. Confirmed historical purchases are not inferred during CSV import.
