namespace TeaPremium.Domain.Common;

public sealed class SeoMetadata
{
    internal SeoMetadata()
    {
    }

    public SeoMetadata(
        string? seoTitle,
        string? metaDescription,
        string? canonicalOverride = null,
        bool robotsIndex = true,
        string? ogTitle = null,
        string? ogDescription = null,
        Guid? ogImageAssetId = null)
    {
        SeoTitle = seoTitle;
        MetaDescription = metaDescription;
        CanonicalOverride = canonicalOverride;
        RobotsIndex = robotsIndex;
        OgTitle = ogTitle;
        OgDescription = ogDescription;
        OgImageAssetId = ogImageAssetId;
    }

    public string? SeoTitle { get; private set; }

    public string? MetaDescription { get; private set; }

    public string? CanonicalOverride { get; private set; }

    public bool RobotsIndex { get; private set; } = true;

    public string? OgTitle { get; private set; }

    public string? OgDescription { get; private set; }

    public Guid? OgImageAssetId { get; private set; }
}
