# TeaPremium — Storefront UI/UX Design System (2026-11-04)

## Visual north star

**Tĩnh · Mộc · Sâu** — Modern Oriental / Premium Editorial / Minimal Luxury.
The interface is for premium teaware (ấm tử sa, ấm gang, trà cụ and future ranges), not a generic SaaS/e-commerce template.
Quiet, intentional whitespace; paper, natural clay, tea-leaf green, a sense of reading rhythm.
Do **not** use gradient-heavy SaaS cards, glassmorphism, aggressive countdowns, fake ratings, fabricated testimonials, or heavy autoplay hero videos.

### Storefront design tokens

Defined in `apps/web/src/app/globals.css` and used by the storefront:

| Token | Hex | Use |
|---|---|---|
| `--tea-ivory` | `#F7F4EC` | Background / paper |
| `--tea-paper` | `#FFFCF6` | Product/card surface |
| `--tea-forest` | `#233B32` | Navigation, headings, primary CTA |
| `--tea-charcoal` | `#202722` | Body |
| `--tea-clay` | `#925B3D` | Interactive accent / focus |
| `--tea-gold` | `#B79B6C` | Editorial rules, accents |
| `--tea-muted` | `#56635B` | Descriptions |
| `--tea-line` | `#D9D2C3` | Borders |

Display headings: **Lora** Vietnamese subset; interface/body: **Be Vietnam Pro**.
Keep visible WCAG focus rings, 44px minimum important targets, keyboard navigation,
comfortable 1.7–1.95 reading line heights, and reduced motion support.

## The customer journey

1. **Home** (`/`): one strong editorial heading, actual featured image from the Published Catalog (or typographic empty state), large collection tiles optionally using real Product cover assets, latest published products, philosophical "Tĩnh · Mộc · Sâu" section, published articles, one calm consultation CTA.
2. **Collection** (`/danh-muc/[slug]`): clear title, actual description, count, real product cards. Keep SEO canonical and pagination unchanged.
3. **Product** (`/san-pham/[slug]`): one image at a time with accessible thumbnail navigation; product name, SKU, category/brand and truthful specifications; no price, inventory, availability claim or checkout. Primary CTA links to `/lien-he?san-pham=<slug>` to preserve product context.
4. **Contact** (`/lien-he`): server validates published product slug before prefilling consultation message. If unknown, show the generic form. Contact options only appear when configured in Site Settings. Contact click remains a click, not a Lead or sale. Form submission requires consent as before.
5. **Knowledge/About/Search**: same editorial typography, spacing, visual rhythm. Preserve index/noindex rules.

Images MUST originate from actual published Product / MediaAsset records. Do not hardcode third-party images as if they belong to the shop. Image aspect ratio and ALT should reflect actual product photography. To get a polished first fold, the operator must publish at least one product with a high-quality cover image; no frontend restyling can replace real content.

## How to configure the production look without fabricated data

1. Set actual Site Settings: name, tagline, legal shop contact channels, logo/favicon and real public website URL.
2. Upload original product photos through Media, fix R2 storage permissions first if uploads are unavailable. Each visible published product needs a cover and descriptive ALT.
3. Publish product categories and several representative products. Catalog values must be accurate and verified (material, capacity, product code and description).
4. Publish at least two to three genuine knowledge articles; fill the actual About description before expecting a complete brand story.
5. Leave `SITE_INDEXABLE=false` until canonical domain, product content, contact info, sitemap and media are reviewed; then deploy as a separate SEO decision.
6. Use GA4 only with configured measurement ID and appropriate consent; never send phone/email to GA4.

## Manual UX QA matrix

| Screen | 375px mobile | 768px tablet | 1440px desktop | Keyboard | Empty data |
|---|---|---|---|---|---|
| Home | hero and cards single column | natural two-column rhythm | editorial asymmetry | nav/CTA focus | typographic hero & empty categories |
| Category | cards stack | 2-up | 3-up | product/card links | meaningful empty catalog |
| Product | visible image + thumbs | two-step image/summary | image left, info right | gallery buttons + CTA | no fake product photo |
| Contact | single-column form | form flow | side-by-side contact/form | form validation & consent | no invented contacts |
| Admin Dashboard | cards stack | 2 columns | balanced stats + shortcuts | nav & links | zero states |
| Customers | search/create/list | table scroll | search/create/history | controls & dialogs | no fake customers |
| Orders | mobile order cards | table | multi-line input + table | confirmation dialogs | no fake orders |
| Reports | bars stack | bars stack | 2 columns | date form | no fake revenue |

Do not commit screenshot capture artifacts or fake product seed images in this UI pass.

## Verification

From project root:

```powershell
corepack pnpm --dir apps/web install --frozen-lockfile
corepack pnpm --dir apps/web lint
corepack pnpm --dir apps/web typecheck
corepack pnpm --dir apps/web build
corepack pnpm --dir apps/web test:seo
corepack pnpm --dir apps/web test:storefront
dotnet test backend/TeaPremium.sln --configuration Release
```

The production HTTP fixture and published product SSR smoke are useful but not proof of visual perfection on every physical device. Conduct responsive browser review with actual Catalog photographs after real content is available.
