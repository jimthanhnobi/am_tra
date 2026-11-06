# Local storefront visual baselines

These reviewed fixture screenshots were captured on Windows with Chrome at
1440×900 and 390×844 (device scale factor 1). They cover public catalog,
contact form, mobile navigation and form validation states.
Fixture content is synthetic and never writes to the Development database.

After building `apps/web`, regenerate the captures with:

```powershell
$env:STOREFRONT_CAPTURE_DIR='../../artifacts/p11-run/storefront-baselines'
corepack pnpm --dir apps/web test:storefront
Push-Location apps/web
node tests/compare-visual-baselines.mjs
Pop-Location
```

The exact PNG comparison detects local changes; font rendering can differ across
operating systems or Chrome versions. Inspect changed images before updating any
baseline. The comparison script never accepts changes automatically.
