namespace TeaPremium.Domain.Settings;

public sealed class SiteSettings
{
    private SiteSettings() { }

    public SiteSettings(Guid updatedByUserId, DateTimeOffset now)
    {
        Id = 1;
        UpdatedByUserId = updatedByUserId;
        UpdatedAtUtc = now.ToUniversalTime();
    }

    public int Id { get; private set; }
    public string? SiteName { get; private set; }
    public string? SiteDescription { get; private set; }
    public string? BrandTagline { get; private set; }
    public Guid? LogoAssetId { get; private set; }
    public Guid? FaviconAssetId { get; private set; }
    public string? Hotline { get; private set; }
    public string? ZaloUrl { get; private set; }
    public string? FacebookUrl { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public string? OpeningHours { get; private set; }
    public string? DefaultSeoTitle { get; private set; }
    public string? DefaultMetaDescription { get; private set; }
    public Guid? DefaultOgImageAssetId { get; private set; }
    public string? PublicWebsiteUrl { get; private set; }
    public Guid UpdatedByUserId { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void Update(
        string siteName, string? siteDescription, string? brandTagline,
        Guid? logoAssetId, Guid? faviconAssetId,
        string? hotline, string? zaloUrl, string? facebookUrl, string? email,
        string? address, string? openingHours,
        string? defaultSeoTitle, string? defaultMetaDescription,
        Guid? defaultOgImageAssetId, string? publicWebsiteUrl,
        Guid updatedByUserId, DateTimeOffset now)
    {
        SiteName = siteName;
        SiteDescription = siteDescription;
        BrandTagline = brandTagline;
        LogoAssetId = logoAssetId;
        FaviconAssetId = faviconAssetId;
        Hotline = hotline;
        ZaloUrl = zaloUrl;
        FacebookUrl = facebookUrl;
        Email = email;
        Address = address;
        OpeningHours = openingHours;
        DefaultSeoTitle = defaultSeoTitle;
        DefaultMetaDescription = defaultMetaDescription;
        DefaultOgImageAssetId = defaultOgImageAssetId;
        PublicWebsiteUrl = publicWebsiteUrl;
        UpdatedByUserId = updatedByUserId;
        UpdatedAtUtc = now.ToUniversalTime();
    }
}
