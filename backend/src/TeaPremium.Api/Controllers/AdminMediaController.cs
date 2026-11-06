using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TeaPremium.Api.Security;
using TeaPremium.Application.Catalog;
using TeaPremium.Application.Media;
using TeaPremium.Application.Security;

namespace TeaPremium.Api.Controllers;

[Route("admin/media")]
public sealed class AdminMediaController(IMediaService mediaService) : AdminApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Permissions.CatalogRead)]
    public Task<PagedResult<AdminMediaAssetDto>> GetMedia(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        [FromQuery] string? search = null,
        [FromQuery] string? contentType = null,
        CancellationToken cancellationToken = default) =>
        mediaService.GetMediaAsync(
            new AdminMediaListQuery(page, pageSize, search, contentType),
            cancellationToken);

    [HttpPost("upload-intent")]
    [Authorize(Policy = Permissions.MediaWrite)]
    [EnableRateLimiting(AdminSecurityDefaults.MediaUploadRateLimitPolicy)]
    public Task<MediaUploadIntentDto> CreateUploadIntent(
        CreateMediaUploadIntentCommand command,
        CancellationToken cancellationToken) =>
        mediaService.CreateUploadIntentAsync(GetRequiredUserId(), command, cancellationToken);

    [HttpPost("{uploadId:guid}/complete")]
    [Authorize(Policy = Permissions.MediaWrite)]
    [EnableRateLimiting(AdminSecurityDefaults.MediaUploadRateLimitPolicy)]
    public Task<AdminMediaAssetDto> CompleteUpload(
        Guid uploadId,
        CancellationToken cancellationToken) =>
        mediaService.CompleteUploadAsync(GetRequiredUserId(), uploadId, cancellationToken);

    [HttpPost("uploads/cleanup")]
    [Authorize(Policy = Permissions.MediaWrite)]
    [EnableRateLimiting(AdminSecurityDefaults.MediaUploadRateLimitPolicy)]
    public Task<MediaCleanupResult> CleanupExpiredUploads(
        CancellationToken cancellationToken) =>
        mediaService.CleanupExpiredUploadsAsync(cancellationToken);

    private Guid GetRequiredUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new InvalidOperationException("The authenticated user ID is unavailable.");
    }
}
