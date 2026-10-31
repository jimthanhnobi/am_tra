# Admin Catalog UI

## Routes and workflow

All `/admin/*` pages are `noindex`. The protected layout checks the Identity session on the server; each page checks `Catalog.Read` or `Catalog.Write` before fetching data. ASP.NET Core policies remain the authority for every API request.

| Route | Purpose |
|---|---|
| `/admin` | Live catalog counts and recently updated products |
| `/admin/products` | Server-paged list, search, status/category filter, sort, publish/archive |
| `/admin/products/new` | Create a Draft |
| `/admin/products/[id]/edit` | Edit fields, categories, specifications, gallery, SEO and status |
| `/admin/categories` | Category tree, create/edit, parent, status and SEO |
| `/admin/brands` | Search, create and edit brands |
| `/admin/media` | Search/filter and upload media, inspect metadata |

The product editor saves a new product as Draft before allowing media attachment. The description input is plain text; the UI produces a small structured JSON document and HTML-escaped paragraphs. An existing structured description and sanitized HTML are preserved until the description field is edited. Saving does not publish. An existing Draft can be submitted for review; an InReview product can be published; a Published product can be archived. Published slugs are locked in the editor because the redirect workflow is not available.

The UI shows `application/problem+json` validation fields and API conflict messages. Unsafe requests go through the existing `/api/admin/*` BFF with an identity-bound CSRF token. Image bytes go directly to R2 using the API-provided presigned URL. Gallery removal only detaches the asset; no physical deletion is performed.

## Permissions

| Action | Permission | Initial roles |
|---|---|---|
| Read catalog and media list | `Catalog.Read` | Admin, Editor, Publisher |
| Edit product/category/brand and gallery | `Catalog.Write` | Admin, Editor, Publisher |
| Publish/archive product | `Catalog.Publish` | Admin, Publisher |
| Create/complete media upload | `Media.Write` | Admin, Editor, Publisher |

Category status changes currently use the backend's `Catalog.Write` policy. No separate category publish policy exists in this API contract.

## Local verification

1. Apply reviewed migrations to a local PostgreSQL database and bootstrap a Development Admin as described in [auth.md](auth.md).
2. Configure `API_BASE_URL` in `apps/web/.env.local`; configure R2 and the matching `R2_PUBLIC_BASE_URL` on the API and web process for image previews.
3. Run `dotnet run --project backend/src/TeaPremium.Api --launch-profile http` and `corepack pnpm --dir apps/web dev` from the repo root.
4. Open `http://localhost:3000/admin/login`. Create a Published category, Draft product, upload an image, set its ALT and cover, submit for review, then publish. Inspect the public API and repeat with an Editor to confirm publish returns 403.

See [media-storage.md](media-storage.md) for R2 setup. The storefront product pages, contact flow and SEO redirect workflow are outside Prompt 4B.
