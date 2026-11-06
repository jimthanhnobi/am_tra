using System.Text.RegularExpressions;

namespace TeaPremium.Domain.Seo;

public sealed partial class SeoRedirect
{
    private SeoRedirect() { }

    public SeoRedirect(string sourcePath, string destinationPath, DateTimeOffset now)
    {
        Validate(sourcePath, destinationPath);
        Id = Guid.NewGuid();
        SourcePath = sourcePath;
        DestinationPath = destinationPath;
        CreatedAtUtc = UpdatedAtUtc = now.ToUniversalTime();
    }

    public Guid Id { get; private set; }
    public string SourcePath { get; private set; } = string.Empty;
    public string DestinationPath { get; private set; } = string.Empty;
    public int RedirectType { get; private set; } = 301;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void Retarget(string destinationPath, DateTimeOffset now)
    {
        Validate(SourcePath, destinationPath);
        DestinationPath = destinationPath;
        UpdatedAtUtc = now.ToUniversalTime();
    }

    public static string ContentPath(string kind, string slug)
    {
        var path = $"/{kind}/{slug}";
        if (!IsValidContentPath(path)) throw new ArgumentException("Invalid public content path.", nameof(slug));
        return path;
    }

    public static bool IsValidContentPath(string? path) =>
        path is { Length: <= 220 } && ContentPathPattern().IsMatch(path);

    public static bool CanonicalPointsToSource(string? canonical, string sourcePath) =>
        !string.IsNullOrWhiteSpace(canonical) &&
        Uri.TryCreate(canonical, UriKind.Absolute, out var uri) &&
        uri.AbsolutePath.TrimEnd('/') == sourcePath;

    private static void Validate(string source, string destination)
    {
        if (!IsValidContentPath(source) || !IsValidContentPath(destination) ||
            source == destination || source.Split('/')[1] != destination.Split('/')[1])
            throw new ArgumentException("Redirect paths must be distinct normalized local content paths of the same type.");
    }

    [GeneratedRegex("^/(san-pham|danh-muc|kien-thuc)/[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex ContentPathPattern();
}
