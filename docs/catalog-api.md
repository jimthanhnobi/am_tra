# Catalog API

Status: Implemented for the Catalog + Media MVP slice (2026-10-30)

All routes below are prefixed with `/api/v1`. Admin routes use the existing ASP.NET Core Identity cookie, antiforgery filter, and permission policies. Public routes are anonymous and return separate read models containing only Published catalog data.

## Admin endpoints

| Method | Route | Permission | Purpose |
|---|---|---|---|
| `GET` | `/admin/products` | `Catalog.Read` | Paged search/filter/sort list |
| `GET` | `/admin/products/{id}` | `Catalog.Read` | Product editor detail |
| `POST` | `/admin/products` | `Catalog.Write` | Create a Draft product |
| `PATCH` | `/admin/products/{id}` | `Catalog.Write` | Replace editable fields; optionally move Draft to InReview |
| `POST` | `/admin/products/{id}/publish` | `Catalog.Publish` | Publish an InReview product |
| `POST` | `/admin/products/{id}/archive` | `Catalog.Publish` | Archive a Published product |
| `POST` | `/admin/products/{id}/media` | `Catalog.Write` | Attach an existing MediaAsset |
| `PATCH` | `/admin/products/{id}/media/{mediaId}` | `Catalog.Write` | Update ALT text and sort order |
| `DELETE` | `/admin/products/{id}/media/{mediaId}` | `Catalog.Write` | Detach media; does not delete the asset |
| `POST` | `/admin/products/{id}/media/{mediaId}/cover` | `Catalog.Write` | Select the single product cover |
| `GET` | `/admin/categories` | `Catalog.Read` | Full category tree |
| `GET` | `/admin/categories/{id}` | `Catalog.Read` | Category editor detail |
| `POST` | `/admin/categories` | `Catalog.Write` | Create a Draft category |
| `PATCH` | `/admin/categories/{id}` | `Catalog.Write` | Replace editable fields and apply a valid status transition |
| `GET` | `/admin/brands` | `Catalog.Read` | List brands |
| `GET` | `/admin/brands/{id}` | `Catalog.Read` | Brand detail |
| `POST` | `/admin/brands` | `Catalog.Write` | Create a brand |
| `PATCH` | `/admin/brands/{id}` | `Catalog.Write` | Replace brand fields |
| `GET` | `/admin/catalog/summary` | `Catalog.Read` | Catalog counts for dashboard |
| `GET` | `/admin/media` | `Catalog.Read` | Paged media library |
| `POST` | `/admin/media/upload-intent` | `Media.Write` | Create R2 presigned PUT intent |
| `POST` | `/admin/media/{uploadId}/complete` | `Media.Write` | Verify uploaded object and create MediaAsset |
| `POST` | `/admin/media/uploads/cleanup` | `Media.Write` | Expire pending sessions and remove orphan objects |

`PATCH` is an editor-oriented full update, not JSON Merge Patch. Send all editable fields from the current editor model. Omit `status` to keep the current status.

Product list query parameters:

- `page` starts at 1; `pageSize` is 1–48.
- `search` matches name, slug, or SKU case-insensitively.
- `status`: `Draft`, `InReview`, `Published`, or `Archived`.
- `categoryId` and `brandId` are optional UUID filters.
- `sort`: `updated_desc`, `created_desc`, `name_asc`, `name_desc`, `status_asc`, or `published_desc`.

Example create payload:

```json
{
  "sku": "TST-001",
  "slug": "am-tu-sa-thu-cong",
  "name": "Ấm Tử Sa thủ công",
  "summary": "Mô tả ngắn phục vụ trang danh sách.",
  "description": { "type": "doc", "version": 1 },
  "descriptionHtml": "<p>Nội dung đã render từ editor.</p>",
  "productType": "Teapot",
  "materialText": "Đất Tử Sa",
  "capacityMl": 180,
  "specifications": { "capacityMl": 180 },
  "primaryCategoryId": "00000000-0000-0000-0000-000000000001",
  "brandId": null,
  "categoryIds": ["00000000-0000-0000-0000-000000000001"],
  "seo": {
    "seoTitle": "Ấm Tử Sa thủ công",
    "metaDescription": "Mô tả SEO của sản phẩm.",
    "canonicalOverride": null,
    "robotsIndex": true,
    "ogTitle": null,
    "ogDescription": null,
    "ogImageAssetId": null
  }
}
```

`description` and `specifications` must be JSON objects. HTML is sanitized in the Application use case before persistence. Slugs use lowercase ASCII kebab-case. SKU is nullable but unique when present. The primary category must also be present in `categoryIds`; this invariant is checked in the same application transaction rather than by a cyclic foreign key.

## Workflow and publish guards

Product status normally follows `Draft -> InReview -> Published -> Archived`:

- Create always produces Draft.
- Product `PATCH` can move Draft to InReview and return InReview/Archived to Draft for correction.
- Publishing and archiving use their dedicated endpoints and require `Catalog.Publish`.
- Publish requires a non-empty name, summary, structured description, sanitized HTML description, a Published primary category attached to the product, exactly one cover with non-empty ALT text, SEO title, and meta description.
- A Published product is revalidated on edits. Changing the slug of a product that has ever been
  Published creates a direct redirect, including when the product was reopened from Archived.
- `isSellable` is an Admin-only commerce flag independent from publication. It never makes a
  Draft/Archived product visible through the Public API.

Category status follows the same order through `PATCH`. Publishing requires content and SEO metadata; a parent must be Published first. Self-parenting, missing parents, and hierarchy cycles are rejected. A Published category used by a Published product cannot be archived. Published category slugs are immutable. Category and brand hard-delete endpoints intentionally do not exist.

Product media endpoints connect existing `media_assets` rows. Binary upload uses a separate presigned R2 PUT flow described in [media-storage.md](media-storage.md). Cover replacement clears the previous cover and selects the new one inside a database transaction, preserving the PostgreSQL partial unique index. Detaching media never deletes the underlying asset.

## Public endpoints

| Method | Route | Result |
|---|---|---|
| `GET` | `/public/products` | Paged Published products |
| `GET` | `/public/products/{slug}` | Published product detail |
| `GET` | `/public/categories` | Published category tree |
| `GET` | `/public/categories/{slug}` | Published category and paged products |

Public product query parameters are `page`, `pageSize` (1–48), `search`, `category` (slug), and `sort` (`latest`, `name_asc`, `name_desc`). Queries are read-only and filtered in PostgreSQL before materialization. List projections fetch only the fields they need; detail queries use split includes to avoid Cartesian expansion.

Draft, InReview, and Archived products/categories return `404` from detail routes and never appear in public lists. Public DTOs do not contain admin timestamps, workflow state, price, inventory, cart, order, checkout, or payment data. `storageKey` is an asset identifier, not a storage credential or signed upload secret.

Public queries require `Published` status. Products require a Published primary category; secondary category links only expose Published categories. Counts and pagination apply these visibility filters in PostgreSQL. `seo.robotsIndex = false` does not hide a Published entity or its products from the Public API; storefront HTML returns 200 with noindex metadata and the URL is excluded from the sitemap. Admin data remains available.

Public specifications recursively omit keys containing `price`, `cost`, `inventory`, `stock`, `cart`, `checkout`, `payment`, `order`, `admin`, `token`, `secret`, `password`, `createdby`, or `updatedby` (case insensitive), including nested objects/arrays. Admin specifications are unchanged. These reserved keys must not be used for public technical attributes. Editorial text still requires review: this projection does not detect a price typed into prose. Storefront additionally validates/strips unknown DTO fields and displays only scalar technical specifications.

## Errors and concurrency

Responses use RFC Problem Details:

- `400`: malformed request/model binding or missing/invalid antiforgery token.
- `401`: no valid Admin session.
- `403`: authenticated but missing the required permission.
- `404`: resource absent or not publicly visible.
- `409`: uniqueness/database conflict, immutable slug, or invalid workflow transition.
- `422`: semantic validation such as an invalid hierarchy, reference, JSON shape, publish guard, or pagination value.

Uniqueness is checked for useful messages and remains enforced by PostgreSQL indexes for concurrent requests. Multi-row product/category/media changes use explicit transactions. A failed operation is rolled back and the EF change tracker is cleared. No migration runs automatically during API startup.

## Cookie and antiforgery flow

For Postman or a direct development client:

1. `GET /api/v1/admin/auth/csrf` and retain its cookie plus `requestToken` response value.
2. `POST /api/v1/admin/auth/login` with that cookie and `X-CSRF-TOKEN: <requestToken>`.
3. Retain the Identity cookie, fetch `/admin/auth/csrf` again, and send the returned header/token on every unsafe Admin request.
4. Public `GET` routes need no cookie. Logout uses `POST /admin/auth/logout` with antiforgery protection.

In the browser, call the existing same-origin Next.js BFF under `/api/admin/*`; it forwards cookies and the antiforgery header. Do not copy session values into Client Components or Web Storage. See [auth.md](auth.md) for the complete security flow.

## Verification

Integration tests require an isolated real PostgreSQL database; there is no EF InMemory fallback:

```powershell
$env:TEAPREMIUM_TEST_CONNECTION_STRING = "Host=127.0.0.1;Port=<port>;Database=<test-db>;Username=<user>;Password=<password>"
dotnet test backend/tests/TeaPremium.IntegrationTests/TeaPremium.IntegrationTests.csproj
```

The tests cover create/update relationships, duplicate slug/SKU, invalid primary category rollback, category cycles, cover-required publishing, public visibility and DTO boundaries, pagination limits, 401/403 permission behavior, transaction rollback, OpenAPI paths, and the existing authentication/CSRF behavior.
