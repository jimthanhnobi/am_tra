namespace TeaPremium.Infrastructure.Media;

internal static class DemoMediaPath
{
    private const string Prefix = "demo-products/";

    // Public, bundled illustrations only. User uploads continue through verified R2.
    internal static string? Resolve(string key)
    {
        if (!key.StartsWith(Prefix, StringComparison.Ordinal)) return null;
        var filename = key[Prefix.Length..];
        if (filename.Length is < 5 or > 100 || !filename.EndsWith(".svg", StringComparison.Ordinal) ||
            filename.Any(character => character is not (>= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '.')) ||
            filename.Contains("..", StringComparison.Ordinal))
            return null;
        return "/demo-products/" + filename;
    }
}
