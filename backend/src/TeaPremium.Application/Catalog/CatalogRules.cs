using System.Text.Json;
using System.Text.RegularExpressions;
using TeaPremium.Domain.Catalog;

namespace TeaPremium.Application.Catalog;

public static partial class CatalogRules
{
    public const int MaximumPageSize = 48;

    public static bool IsValidSlug(string? slug) =>
        !string.IsNullOrWhiteSpace(slug) &&
        slug.Length <= 200 &&
        SlugPattern().IsMatch(slug);

    public static bool IsJsonObject(JsonElement value) => value.ValueKind == JsonValueKind.Object;

    public static bool IsValidPage(int page, int pageSize) =>
        page >= 1 && pageSize is >= 1 and <= MaximumPageSize;

    public static bool TryParseStatus(string? value, out PublicationStatus status) =>
        Enum.TryParse(value, ignoreCase: true, out status) &&
        Enum.IsDefined(status);

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex SlugPattern();
}

internal sealed class CatalogValidationBag
{
    private readonly Dictionary<string, List<string>> _errors =
        new(StringComparer.OrdinalIgnoreCase);

    public void Add(string field, string message)
    {
        if (!_errors.TryGetValue(field, out var messages))
        {
            messages = [];
            _errors[field] = messages;
        }

        messages.Add(message);
    }

    public void Require(string field, string? value, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Add(field, "This field is required.");
        }
        else if (value.Length > maximumLength)
        {
            Add(field, $"This field must not exceed {maximumLength} characters.");
        }
    }

    public void OptionalMaximum(string field, string? value, int maximumLength)
    {
        if (value?.Length > maximumLength)
        {
            Add(field, $"This field must not exceed {maximumLength} characters.");
        }
    }

    public void ThrowIfInvalid()
    {
        if (_errors.Count == 0)
        {
            return;
        }

        throw new CatalogValidationException(
            _errors.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Distinct(StringComparer.Ordinal).ToArray(),
                StringComparer.OrdinalIgnoreCase));
    }
}
