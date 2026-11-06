namespace TeaPremium.Application.Seo;

public interface ISeoRedirectStore
{
    Task<string?> FindDestinationAsync(string sourcePath, CancellationToken cancellationToken);
    Task<bool> SourceExistsAsync(string sourcePath, CancellationToken cancellationToken);
    Task EnsurePathAvailableAsync(string path, CancellationToken cancellationToken);
    Task RecordSlugChangeAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken);
}
