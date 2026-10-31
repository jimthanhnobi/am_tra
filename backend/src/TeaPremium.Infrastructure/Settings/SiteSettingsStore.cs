using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TeaPremium.Application.Settings;
using TeaPremium.Domain.Settings;
using TeaPremium.Infrastructure.Persistence;

namespace TeaPremium.Infrastructure.Settings;

internal sealed class SiteSettingsStore(AppDbContext db, IConfiguration configuration) : ISiteSettingsStore
{
    public Task<SiteSettings?> LoadAsync(CancellationToken cancellationToken) =>
        db.SiteSettings.SingleOrDefaultAsync(x => x.Id == 1, cancellationToken);

    public async Task SaveAsync(SiteSettings settings, CancellationToken cancellationToken)
    {
        if (db.Entry(settings).State == EntityState.Detached) db.SiteSettings.Add(settings);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> AssetsExistAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        await db.MediaAssets.CountAsync(x => ids.Contains(x.Id), cancellationToken) == ids.Count;

    public async Task<PublicSiteSettingsDto> PublicAsync(CancellationToken cancellationToken)
    {
        var current = await db.SiteSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, cancellationToken);
        if (current is null) return new(false, null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null);
        var assetIds = new[] { current.LogoAssetId, current.FaviconAssetId, current.DefaultOgImageAssetId }
            .Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToArray();
        var keys = await db.MediaAssets.AsNoTracking().Where(x => assetIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.StorageKey, cancellationToken);
        string? Url(Guid? id) => id is Guid value && keys.TryGetValue(value, out var key) &&
            !string.IsNullOrWhiteSpace(configuration["R2_PUBLIC_BASE_URL"])
                ? $"{configuration["R2_PUBLIC_BASE_URL"]!.TrimEnd('/')}/{string.Join('/', key.Split('/').Select(Uri.EscapeDataString))}"
                : null;
        return new(true, current.SiteName, current.SiteDescription, current.BrandTagline,
            Url(current.LogoAssetId), Url(current.FaviconAssetId),
            current.Hotline, current.ZaloUrl, current.FacebookUrl, current.Email,
            current.Address, current.OpeningHours, current.DefaultSeoTitle,
            current.DefaultMetaDescription, Url(current.DefaultOgImageAssetId),
            current.PublicWebsiteUrl);
    }
}
