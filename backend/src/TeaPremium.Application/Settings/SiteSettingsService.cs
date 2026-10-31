using System.Net.Mail;
using System.Text.RegularExpressions;
using TeaPremium.Domain.Settings;

namespace TeaPremium.Application.Settings;

public sealed partial class SiteSettingsService(ISiteSettingsStore store, TimeProvider clock)
{
    public async Task<AdminSiteSettingsDto> GetAdminAsync(CancellationToken cancellationToken)
    {
        var current = await store.LoadAsync(cancellationToken);
        return current is null
            ? new AdminSiteSettingsDto(new SiteSettingsInput(null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null), null, null)
            : new AdminSiteSettingsDto(ToInput(current), current.UpdatedAtUtc, current.UpdatedByUserId);
    }

    public Task<PublicSiteSettingsDto> GetPublicAsync(CancellationToken cancellationToken) =>
        store.PublicAsync(cancellationToken);

    public async Task<AdminSiteSettingsDto> UpdateAsync(SiteSettingsInput input, Guid actorId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        Validate(input);
        var ids = new[] { input.LogoAssetId, input.FaviconAssetId, input.DefaultOgImageAssetId }
            .Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToArray();
        if (!await store.AssetsExistAsync(ids, cancellationToken))
            throw new SiteSettingsValidationException("A selected media asset does not exist.");
        var current = await store.LoadAsync(cancellationToken) ?? new SiteSettings(actorId, clock.GetUtcNow());
        current.Update(input.SiteName!.Trim(), Trim(input.SiteDescription), Trim(input.BrandTagline),
            input.LogoAssetId, input.FaviconAssetId, Trim(input.Hotline), Trim(input.ZaloUrl),
            Trim(input.FacebookUrl), Trim(input.Email), Trim(input.Address), Trim(input.OpeningHours),
            Trim(input.DefaultSeoTitle), Trim(input.DefaultMetaDescription),
            input.DefaultOgImageAssetId, Trim(input.PublicWebsiteUrl), actorId, clock.GetUtcNow());
        await store.SaveAsync(current, cancellationToken);
        return new AdminSiteSettingsDto(ToInput(current), current.UpdatedAtUtc, current.UpdatedByUserId);
    }

    private static SiteSettingsInput ToInput(SiteSettings value) => new(
        value.SiteName, value.SiteDescription, value.BrandTagline,
        value.LogoAssetId, value.FaviconAssetId, value.Hotline, value.ZaloUrl,
        value.FacebookUrl, value.Email, value.Address, value.OpeningHours,
        value.DefaultSeoTitle, value.DefaultMetaDescription,
        value.DefaultOgImageAssetId, value.PublicWebsiteUrl);

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static void Validate(SiteSettingsInput input)
    {
        if (string.IsNullOrWhiteSpace(input.SiteName) || input.SiteName.Trim().Length > 160 ||
            input.SiteDescription?.Trim().Length > 500 || input.BrandTagline?.Trim().Length > 200 ||
            input.Address?.Trim().Length > 500 || input.OpeningHours?.Trim().Length > 300 ||
            input.DefaultSeoTitle?.Trim().Length > 200 || input.DefaultMetaDescription?.Trim().Length > 500 ||
            (Trim(input.Hotline) is { } phone && !PhonePattern().IsMatch(phone)) ||
            (Trim(input.Email) is { } email && !ValidEmail(email)) ||
            !ValidSocial(input.ZaloUrl, ["zalo.me", "www.zalo.me"]) ||
            !ValidSocial(input.FacebookUrl, ["facebook.com", "www.facebook.com", "m.facebook.com", "fb.com"]) ||
            !ValidWebsite(input.PublicWebsiteUrl))
            throw new SiteSettingsValidationException("Site settings contain invalid fields or URLs.");
    }

    private static bool ValidEmail(string value)
    {
        if (value.Length > 254 || value.Contains('\n') || value.Contains('\r')) return false;
        try { return new MailAddress(value).Address == value; }
        catch (FormatException) { return false; }
    }

    private static bool ValidSocial(string? value, string[] hosts)
    {
        if (Trim(value) is not { } text) return true;
        return text.Length <= 2048 && Uri.TryCreate(text, UriKind.Absolute, out var uri) &&
            uri.Scheme == "https" && uri.UserInfo.Length == 0 && hosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase);
    }

    private static bool ValidWebsite(string? value)
    {
        if (Trim(value) is not { } text) return true;
        return text.Length <= 2048 && Uri.TryCreate(text, UriKind.Absolute, out var uri) &&
            uri.Scheme == "https" && uri.UserInfo.Length == 0 && uri.AbsolutePath == "/" &&
            uri.Query.Length == 0 && uri.Fragment.Length == 0 &&
            !uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) &&
            !uri.Host.EndsWith(".example", StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex("^(?=.*[0-9])\\+?[0-9\\s().-]{6,25}$", RegexOptions.CultureInvariant)]
    private static partial Regex PhonePattern();
}
