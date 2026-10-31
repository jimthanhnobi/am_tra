using Microsoft.EntityFrameworkCore;
using Npgsql;
using TeaPremium.Application.Catalog;
using TeaPremium.Application.Seo;
using TeaPremium.Domain.Seo;
using TeaPremium.Domain.Catalog;
using TeaPremium.Infrastructure.Persistence;

namespace TeaPremium.Infrastructure.Seo;

internal sealed class SeoRedirectStore(AppDbContext db, TimeProvider clock) : ISeoRedirectStore
{
    public async Task<string?> FindDestinationAsync(string sourcePath, CancellationToken cancellationToken)
    {
        var destination = await db.SeoRedirects.AsNoTracking().Where(x => x.SourcePath == sourcePath)
            .Select(x => x.DestinationPath).SingleOrDefaultAsync(cancellationToken);
        if (destination is null) return null;
        var slash = destination.LastIndexOf('/');
        var slug = destination[(slash + 1)..];
        var visible = destination[..slash] switch
        {
            "/san-pham" => await db.Products.AnyAsync(x => x.Slug == slug &&
                x.Status == PublicationStatus.Published && x.PublishedAt != null &&
                x.PrimaryCategory != null && x.PrimaryCategory.Status == PublicationStatus.Published,
                cancellationToken),
            "/danh-muc" => await db.Categories.AnyAsync(x => x.Slug == slug &&
                x.Status == PublicationStatus.Published, cancellationToken),
            "/kien-thuc" => await db.Articles.AnyAsync(x => x.Slug == slug &&
                x.Status == PublicationStatus.Published, cancellationToken),
            _ => false,
        };
        return visible ? destination : null;
    }

    public Task<bool> SourceExistsAsync(string sourcePath, CancellationToken cancellationToken) =>
        db.SeoRedirects.AnyAsync(x => x.SourcePath == sourcePath, cancellationToken);

    public async Task EnsurePathAvailableAsync(string path, CancellationToken cancellationToken)
    {
        if (!SeoRedirect.IsValidContentPath(path) || db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Path reservation requires a valid path and active transaction.");
        await LockAsync(cancellationToken);
        if (await SourceExistsAsync(path, cancellationToken))
            throw new CatalogConflictException("Content path is reserved by a redirect.");
    }

    public async Task RecordSlugChangeAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken)
    {
        if (!SeoRedirect.IsValidContentPath(sourcePath) || !SeoRedirect.IsValidContentPath(destinationPath))
            throw new CatalogConflictException("Redirect path is invalid.");
        if (sourcePath == destinationPath)
            throw new CatalogConflictException("Redirect cannot point to itself.");
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Slug changes require an active database transaction.");

        // Serialize redirect edits so two concurrent renames cannot create a chain.
        await LockAsync(cancellationToken);
        if (await SourceExistsAsync(sourcePath, cancellationToken) ||
            await SourceExistsAsync(destinationPath, cancellationToken) ||
            await db.SeoRedirects.AnyAsync(x => x.DestinationPath == destinationPath, cancellationToken) ||
            await ContentPathExistsAsync(destinationPath, cancellationToken))
            throw new CatalogConflictException("A redirect or public route already reserves this path.");

        var now = clock.GetUtcNow();
        var predecessors = await db.SeoRedirects.Where(x => x.DestinationPath == sourcePath)
            .ToListAsync(cancellationToken);
        foreach (var predecessor in predecessors) predecessor.Retarget(destinationPath, now);
        db.SeoRedirects.Add(new SeoRedirect(sourcePath, destinationPath, now));
    }

    private Task<int> LockAsync(CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(81722431)", cancellationToken);

    private Task<bool> ContentPathExistsAsync(string path, CancellationToken cancellationToken)
    {
        var slash = path.LastIndexOf('/');
        var slug = path[(slash + 1)..];
        return path[..slash] switch
        {
            "/san-pham" => db.Products.AnyAsync(x => x.Slug == slug, cancellationToken),
            "/danh-muc" => db.Categories.AnyAsync(x => x.Slug == slug, cancellationToken),
            "/kien-thuc" => db.Articles.AnyAsync(x => x.Slug == slug, cancellationToken),
            _ => Task.FromResult(true),
        };
    }
}
