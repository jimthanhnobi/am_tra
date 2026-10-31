using TeaPremium.Domain.Settings;

namespace TeaPremium.Application.Settings;

public sealed record SiteSettingsInput(
    string? SiteName, string? SiteDescription, string? BrandTagline,
    Guid? LogoAssetId, Guid? FaviconAssetId,
    string? Hotline, string? ZaloUrl, string? FacebookUrl, string? Email,
    string? Address, string? OpeningHours,
    string? DefaultSeoTitle, string? DefaultMetaDescription,
    Guid? DefaultOgImageAssetId, string? PublicWebsiteUrl);

public sealed record PublicSiteSettingsDto(
    bool IsConfigured,
    string? SiteName, string? SiteDescription, string? BrandTagline,
    string? LogoUrl, string? FaviconUrl,
    string? Hotline, string? ZaloUrl, string? FacebookUrl, string? Email,
    string? Address, string? OpeningHours,
    string? DefaultSeoTitle, string? DefaultMetaDescription,
    string? DefaultOgImageUrl, string? PublicWebsiteUrl);

public sealed record AdminSiteSettingsDto(
    SiteSettingsInput Settings, DateTimeOffset? UpdatedAtUtc, Guid? UpdatedByUserId);

public interface ISiteSettingsStore
{
    Task<SiteSettings?> LoadAsync(CancellationToken cancellationToken);
    Task SaveAsync(SiteSettings settings, CancellationToken cancellationToken);
    Task<bool> AssetsExistAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
    Task<PublicSiteSettingsDto> PublicAsync(CancellationToken cancellationToken);
}

public sealed class SiteSettingsValidationException(string message) : Exception(message);
