namespace TeaPremium.Application.Catalog;

public interface IAdminCatalogSummaryQuery
{
    Task<AdminCatalogSummary> ExecuteAsync(CancellationToken cancellationToken = default);
}

public sealed record AdminCatalogSummary(
    int Total,
    int Draft,
    int InReview,
    int Published,
    int Archived);
