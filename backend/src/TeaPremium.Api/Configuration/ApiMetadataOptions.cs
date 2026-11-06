namespace TeaPremium.Api.Configuration;

public sealed class ApiMetadataOptions
{
    public const string SectionName = "ApiMetadata";

    public string Name { get; init; } = string.Empty;

    public string Version { get; init; } = string.Empty;
}

