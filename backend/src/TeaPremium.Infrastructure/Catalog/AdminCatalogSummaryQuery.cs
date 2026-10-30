using Microsoft.EntityFrameworkCore;
using TeaPremium.Application.Catalog;
using TeaPremium.Domain.Catalog;
using TeaPremium.Infrastructure.Persistence;

namespace TeaPremium.Infrastructure.Catalog;

internal sealed class AdminCatalogSummaryQuery(AppDbContext dbContext) : IAdminCatalogSummaryQuery
{
    public async Task<AdminCatalogSummary> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        var counts = await dbContext.Products
            .AsNoTracking()
            .GroupBy(product => product.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Status, item => item.Count, cancellationToken);

        var total = counts.Values.Sum();

        return new AdminCatalogSummary(
            total,
            GetCount(PublicationStatus.Draft),
            GetCount(PublicationStatus.InReview),
            GetCount(PublicationStatus.Published),
            GetCount(PublicationStatus.Archived));

        int GetCount(PublicationStatus status) => counts.GetValueOrDefault(status);
    }
}
