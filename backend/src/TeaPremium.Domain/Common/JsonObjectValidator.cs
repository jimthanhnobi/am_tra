using System.Text.Json;

namespace TeaPremium.Domain.Common;

internal static class JsonObjectValidator
{
    public static string EnsureObject(string json, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json, parameterName);

        try
        {
            using var document = JsonDocument.Parse(json);

            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException("JSON value must be an object.", parameterName);
            }

            return json;
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("JSON value is not valid.", parameterName, exception);
        }
    }
}
