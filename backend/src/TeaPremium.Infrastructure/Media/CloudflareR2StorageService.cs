using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using TeaPremium.Application.Media;

namespace TeaPremium.Infrastructure.Media;

internal sealed class CloudflareR2StorageService(
    IAmazonS3 client,
    CloudflareR2Options options) : IObjectStorageService
{
    public bool IsConfigured => true;

    public async Task<PresignedObjectUpload> CreatePresignedUploadAsync(
        string objectKey,
        string contentType,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var url = await client.GetPreSignedURLAsync(new GetPreSignedUrlRequest
            {
                BucketName = options.BucketName,
                Key = objectKey,
                Verb = HttpVerb.PUT,
                ContentType = contentType,
                Expires = expiresAtUtc.UtcDateTime,
            });

            return new PresignedObjectUpload(
                url,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Content-Type"] = contentType,
                });
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new ObjectStorageException("Unable to create an object upload URL.", exception);
        }
    }

    public async Task<StoredObjectMetadata?> GetMetadataAsync(
        string objectKey,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await client.GetObjectMetadataAsync(
                options.BucketName,
                objectKey,
                cancellationToken);
            return new StoredObjectMetadata(
                response.ContentLength,
                response.Headers.ContentType,
                response.ETag);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new ObjectStorageException("Unable to inspect the uploaded object.", exception);
        }
    }

    public async Task<byte[]> ReadPrefixAsync(
        string objectKey,
        int maximumBytes,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.GetObjectAsync(new GetObjectRequest
            {
                BucketName = options.BucketName,
                Key = objectKey,
                ByteRange = new ByteRange(0, maximumBytes - 1),
            }, cancellationToken);
            await using var buffer = new MemoryStream(maximumBytes);
            await response.ResponseStream.CopyToAsync(buffer, cancellationToken);
            return buffer.ToArray();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new ObjectStorageException("Unable to inspect the uploaded image content.", exception);
        }
    }

    public async Task DeleteIfExistsAsync(
        string objectKey,
        CancellationToken cancellationToken)
    {
        try
        {
            await client.DeleteObjectAsync(options.BucketName, objectKey, cancellationToken);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new ObjectStorageException("Unable to delete the expired object.", exception);
        }
    }

    public string GetPublicUrl(string objectKey) =>
        $"{options.PublicBaseUrl.TrimEnd('/')}/{string.Join('/', objectKey.Split('/').Select(Uri.EscapeDataString))}";
}

internal sealed record CloudflareR2Options(
    string AccountId,
    string BucketName,
    string AccessKeyId,
    string SecretAccessKey,
    string PublicBaseUrl);
