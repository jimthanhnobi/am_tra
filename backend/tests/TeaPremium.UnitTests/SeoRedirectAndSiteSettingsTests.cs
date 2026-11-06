using TeaPremium.Application.Settings;
using TeaPremium.Domain.Seo;

namespace TeaPremium.UnitTests;

public sealed class SeoRedirectAndSiteSettingsTests
{
    [Theory]
    [InlineData("https://other.example/path")]
    [InlineData("//other.example/path")]
    [InlineData("/san-pham/upperCase")]
    [InlineData("/san-pham/a/")]
    [InlineData("/admin/settings")]
    [InlineData("/san-pham/a?next=evil")]
    public void RedirectRejectsNonCanonicalOrExternalPaths(string path)
    {
        Assert.False(SeoRedirect.IsValidContentPath(path));
        Assert.Throws<ArgumentException>(() =>
            new SeoRedirect(path, "/san-pham/valid", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void RedirectRejectsSelfAndCrossTypeTargets()
    {
        Assert.Throws<ArgumentException>(() => new SeoRedirect(
            "/san-pham/old", "/san-pham/old", DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() => new SeoRedirect(
            "/san-pham/old", "/danh-muc/new", DateTimeOffset.UtcNow));
        Assert.True(SeoRedirect.CanonicalPointsToSource(
            "https://catalog.test/san-pham/old", "/san-pham/old"));
        Assert.False(SeoRedirect.CanonicalPointsToSource(
            "https://catalog.test/san-pham/new", "/san-pham/old"));
    }

    [Fact]
    public void SettingsRejectUnsafeLinksAndAcceptEmptyOptionalFields()
    {
        var valid = new SiteSettingsInput("Tea test", null, null, null, null,
            null, null, null, null, null, null, null, null, null, null);
        SiteSettingsService.Validate(valid);
        Assert.Throws<SiteSettingsValidationException>(() => SiteSettingsService.Validate(
            valid with { FacebookUrl = "https://evil.example/redirect" }));
        Assert.Throws<SiteSettingsValidationException>(() => SiteSettingsService.Validate(
            valid with { PublicWebsiteUrl = "http://example.com" }));
        Assert.Throws<SiteSettingsValidationException>(() => SiteSettingsService.Validate(
            valid with { Hotline = "......" }));
        Assert.Throws<SiteSettingsValidationException>(() => SiteSettingsService.Validate(
            valid with { SiteName = "  " }));
    }
}
