# Media storage (Cloudflare R2)

## Configuration

The API uses `IObjectStorageService` with `CloudflareR2StorageService`. Set all five variables in the API process through environment/secret management:

| Variable | Meaning |
|---|---|
| `R2_ACCOUNT_ID` | Cloudflare account ID |
| `R2_BUCKET_NAME` | Private bucket name |
| `R2_ACCESS_KEY_ID` | R2 API token access key |
| `R2_SECRET_ACCESS_KEY` | R2 API token secret |
| `R2_PUBLIC_BASE_URL` | Public CDN/custom-domain base URL for catalog images |

Set the same `R2_PUBLIC_BASE_URL` in the Next.js build/runtime environment so `next/image` allows and optimizes that CDN host. Do not set R2 credentials in `apps/web` or any `NEXT_PUBLIC_*` variable. The web process only needs `API_BASE_URL` and `R2_PUBLIC_BASE_URL`. API settings `MediaStorage__MaximumFileSizeBytes` (default 10 MB), `MediaStorage__UploadUrlLifetimeMinutes` (default 10), and `MediaStorage__InspectionPrefixBytes` (default 256 KB) are optional. The current frontend 10 MB precheck must be adjusted if the API limit changes.

For local Compose, copy the root `.env.example` to ignored `.env` and fill the five `R2_*` values there. `R2_ACCOUNT_ID` is the account ID from the S3 API endpoint; the endpoint itself is **not** `R2_PUBLIC_BASE_URL`. Use the bucket's read-only custom domain or its [public development URL](https://developers.cloudflare.com/r2/buckets/public-buckets/) as the public base. The latter is for development and is rate-limited. Set `R2_PUBLIC_BASE_URL` to the same URL for API and web, then rebuild/restart both images so Next.js `remotePatterns` includes the host. Never put the access key or secret in `apps/web/.env.local`, docs, test output, or `NEXT_PUBLIC_*` variables.

For a Development bucket, set its [R2 CORS policy](https://developers.cloudflare.com/r2/buckets/cors/) for the **exact** browser origin, for example:

```json
[
  {
    "AllowedOrigins": ["http://localhost:3000"],
    "AllowedMethods": ["PUT"],
    "AllowedHeaders": ["Content-Type"],
    "ExposeHeaders": ["ETag"],
    "MaxAgeSeconds": 3600
  }
]
```

If the web runs on another port, add that origin explicitly without a trailing slash. The application signs `Content-Type` in its short-lived presigned PUT URL and the browser sends the same header. Confirm the browser's `OPTIONS` preflight and `PUT` in DevTools, then `complete` creates a `MediaAsset` only after the API checks object metadata and image bytes. Verify the asset's public URL with an HTTP GET and its image through Next.js. Do not use the S3 API endpoint as a public read URL. Bucket CORS does not grant write access by itself; the presigned URL authorizes the specific upload.

Create a private R2 bucket and a public custom domain for read-only delivery. Do not use the public URL for uploads. Create an R2 API token limited to the bucket with object read/write/delete permissions needed for upload verification and expired-object cleanup. Store the token in a secret manager; never commit it or expose it to the browser.

Configure bucket CORS for the exact storefront/Admin origin(s), `PUT`, and the `Content-Type` request header. Allow the browser to use the presigned R2 URL and preflight it; do not use wildcard origins for a production bucket. CORS is a browser restriction, not authorization: the presigned URL remains short-lived and scoped to a server-generated object key.

## Upload flow

1. Browser requests `POST /api/admin/media/upload-intent` with file name, MIME and size through the same-origin BFF. ASP.NET Core requires `Media.Write`, CSRF, rate limiting and validates JPEG/PNG/WebP, size and server-generated key.
2. Browser performs `PUT` directly to the presigned R2 URL with every `requiredHeaders` value. The UI shows upload progress.
3. Browser requests `POST /api/admin/media/{uploadId}/complete` through the BFF. The API checks object presence, size, content type and image signature/dimensions before creating `MediaAsset`. A second completion is rejected.
4. The product editor attaches the returned asset via `POST /api/admin/products/{id}/media`. Cover, ALT and order use the Catalog API. Detaching leaves the MediaAsset and R2 object intact.

The `AddMediaUploadSessions` migration stores pending upload sessions and seeds `Media.Write` for Admin, Editor and Publisher. It is separate from `InitialCatalog` and `AddAdminIdentity`. The `POST /api/v1/admin/media/uploads/cleanup` endpoint expires pending sessions and removes their orphan objects; schedule it with trusted server-side credentials and monitor failures. No automatic Production migration or cleanup scheduling is included.

## Production checks and limits

- Confirm the CDN domain serves only intended public images, while the bucket write endpoint remains private.
- Verify a browser upload from the real HTTPS Admin origin, including the CORS preflight and `Content-Type` signed header.
- Verify malformed files, over-limit files, duplicate completion, missing permission, and expired intents are rejected.
- Confirm credentials are absent from HTML, JavaScript bundles, logs, and public API DTOs.
- Configure CDN caching, R2 lifecycle and recovery/backup policy for the actual shop.

Without R2 credentials, the API uses an unavailable adapter and upload returns 503; the UI can be built and navigated but live R2 upload verification is blocked. The Media Library has no physical-delete action. Image crop and derived thumbnails are not implemented.
