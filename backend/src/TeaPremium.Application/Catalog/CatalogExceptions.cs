namespace TeaPremium.Application.Catalog;

public abstract class CatalogException(string message) : Exception(message);

public sealed class CatalogNotFoundException(string resourceName, object key)
    : CatalogException($"{resourceName} '{key}' was not found.")
{
    public string ResourceName { get; } = resourceName;

    public object Key { get; } = key;
}

public sealed class CatalogConflictException(string message) : CatalogException(message);

public sealed class CatalogValidationException : CatalogException
{
    public CatalogValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more catalog business rules were not satisfied.")
    {
        Errors = errors;
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
