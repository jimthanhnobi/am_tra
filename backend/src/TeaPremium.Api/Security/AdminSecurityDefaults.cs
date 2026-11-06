namespace TeaPremium.Api.Security;

public static class AdminSecurityDefaults
{
    public const string AntiforgeryHeaderName = "X-CSRF-TOKEN";
    public const string LoginRateLimitPolicy = "admin-login";
    public const string MediaUploadRateLimitPolicy = "media-upload";
    public const string DevelopmentAuthCookieName = "TeaPremium.Admin";
    public const string ProductionAuthCookieName = "__Host-TeaPremium.Admin";
    public const string DevelopmentAntiforgeryCookieName = "TeaPremium.Csrf";
    public const string ProductionAntiforgeryCookieName = "__Host-TeaPremium.Csrf";
}
