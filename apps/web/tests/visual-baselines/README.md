# Local visual baselines

These 35 PNGs are reviewed fixture screenshots captured on Windows with Chrome at
1440×900 and 390×844 (device scale factor 1). They include public catalog,
contact form, Admin pages, mobile navigation and form validation states.
Fixture content is synthetic and never writes to the Development database.

After building `apps/web`, regenerate the captures with:

```powershell
$env:STOREFRONT_CAPTURE_DIR='../../artifacts/p11-run/storefront-baselines'
corepack pnpm --dir apps/web test:storefront
Push-Location apps/web
$env:ADMIN_CAPTURE_DIR='../../artifacts/p11-run/admin-baselines'
node tests/capture-admin-fixture.mjs
node tests/compare-visual-baselines.mjs
Pop-Location
```

The exact PNG comparison detects local changes; font rendering can differ across
operating systems or Chrome versions. Inspect changed images before updating any
baseline. The comparison script never accepts changes automatically.
