using System.Net;
using System.Text;
using System.Text.Json;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using CloudStorageORM.Abstractions;
using CloudStorageORM.Enums;
using CloudStorageORM.Infrastructure;
using CloudStorageORM.Interfaces.StorageProviders;
using CloudStorageORM.Options;

namespace CloudStorageORM.Providers.Aws.StorageProviders;

/// <summary>
/// Amazon S3 implementation of <see cref="IStorageProvider" />.
/// </summary>
public class AwsS3StorageProvider : IStorageProvider
{
    private readonly string _bucketName;
    private readonly IAmazonS3 _s3Client;
    private readonly SemaphoreSlim _bucketInitLock = new(1, 1);
    private bool _isBucketInitialized;

    private static Func<CloudStorageOptions, IAmazonS3> OptionsS3ClientFactory { get; set; } = options =>
    {
        var config = new AmazonS3Config
        {
            ForcePathStyle = options.Aws.ForcePathStyle
        };

        if (!string.IsNullOrWhiteSpace(options.Aws.Region))
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(options.Aws.Region);
        }

        if (!string.IsNullOrWhiteSpace(options.Aws.ServiceUrl))
        {
            config.ServiceURL = options.Aws.ServiceUrl;
            config.AuthenticationRegion = string.IsNullOrWhiteSpace(options.Aws.Region)
                ? "us-east-1"
                : options.Aws.Region;
        }

        var credentials = new BasicAWSCredentials(options.Aws.AccessKeyId, options.Aws.SecretAccessKey);
        return new AmazonS3Client(credentials, config);
    };

    private static Func<IAmazonS3, string, Task<bool>> BucketExistsAsyncFactory { get; set; }
        = AmazonS3Util.DoesS3BucketExistV2Async;

    /// <summary>
    /// Creates a new Amazon S3 storage provider from CloudStorageORM options.
    /// </summary>
    /// <param name="options">Validated CloudStorageORM options containing AWS credentials, region, and bucket name.</param>
    /// <example>
    /// <code>
    /// var provider = new AwsS3StorageProvider(options);
    /// </code>
    /// </example>
    public AwsS3StorageProvider(CloudStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _bucketName = options.ContainerName;
        _s3Client = OptionsS3ClientFactory(options);
    }

    internal AwsS3StorageProvider(CloudStorageOptions options, IAmazonS3 s3Client)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(s3Client);
        _bucketName = options.ContainerName;
        _s3Client = s3Client;
    }

    /// <inheritdoc />
    public CloudProvider CloudProvider => CloudProvider.Aws;

    /// <inheritdoc />
    public async Task DeleteContainerAsync()
    {
        try
        {
            await _s3Client.DeleteBucketAsync(new DeleteBucketRequest
            {
                BucketName = _bucketName
            });
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Match Azure delete-if-exists behavior.
        }
    }

    /// <inheritdoc />
    public async Task CreateContainerIfNotExistsAsync()
    {
        if (await BucketExistsAsyncFactory(_s3Client, _bucketName))
        {
            return;
        }

        await _s3Client.PutBucketAsync(new PutBucketRequest
        {
            BucketName = _bucketName
        });

        _isBucketInitialized = true;
    }

    /// <inheritdoc />
    public async Task SaveAsync<T>(string path, T entity)
    {
        await SaveAsync(path, entity, ifMatchETag: null);
    }

    /// <inheritdoc />
    public async Task<string?> SaveAsync<T>(string path, T entity, string? ifMatchETag)
    {
        await EnsureBucketExistsAsync();

        var json = JsonSerializer.Serialize(entity);

        try
        {
            if (!string.IsNullOrWhiteSpace(ifMatchETag))
            {
                var currentEtag = await GetObjectETagAsync(path);
                if (!ETagMatches(currentEtag, ifMatchETag))
                {
                    throw new StoragePreconditionFailedException(path);
                }
            }

            var response = await _s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = path,
                ContentBody = json,
                ContentType = "application/json",
                IfMatch = string.IsNullOrWhiteSpace(ifMatchETag) ? null : ifMatchETag
            });

            if (!string.IsNullOrWhiteSpace(response?.ETag))
            {
                return response.ETag;
            }

            return await GetObjectETagAsync(path);
        }
        catch (AmazonS3Exception ex) when (IsPreconditionFailed(ex))
        {
            throw new StoragePreconditionFailedException(path, ex);
        }
    }

    /// <inheritdoc />
    public async Task<T> ReadAsync<T>(string path)
    {
        var storageObject = await ReadWithMetadataAsync<T>(path);
        return storageObject.Value!;
    }

    /// <inheritdoc />
    public async Task<StorageObject<T>> ReadWithMetadataAsync<T>(string path)
    {
        await EnsureBucketExistsAsync();

        try
        {
            var response = await _s3Client.GetObjectAsync(new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = path
            });

            using var reader = new StreamReader(response.ResponseStream);
            var json = await reader.ReadToEndAsync();
            var etag = response.ETag;
            if (!string.IsNullOrWhiteSpace(etag))
            {
                return new StorageObject<T>(JsonSerializer.Deserialize<T>(json), etag, true);
            }

            etag = await GetObjectETagAsync(path);

            return new StorageObject<T>(JsonSerializer.Deserialize<T>(json), etag, true);
        }
        catch (AmazonS3Exception ex) when (
            ex.StatusCode == HttpStatusCode.NotFound ||
            string.Equals(ex.ErrorCode, "NoSuchKey", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(ex.ErrorCode, "NoSuchBucket", StringComparison.OrdinalIgnoreCase))
        {
            return new StorageObject<T>(default, null, false);
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string path)
    {
        await DeleteAsync(path, ifMatchETag: null);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string path, string? ifMatchETag)
    {
        await EnsureBucketExistsAsync();

        try
        {
            await _s3Client.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = path,
                IfMatch = string.IsNullOrWhiteSpace(ifMatchETag) ? null : ifMatchETag
            });
        }
        catch (AmazonS3Exception ex) when (IsPreconditionFailed(ex))
        {
            throw new StoragePreconditionFailedException(path, ex);
        }
    }

    /// <inheritdoc />
    public async Task<List<string>> ListAsync(string folderPath)
    {
        await EnsureBucketExistsAsync();

        var result = new List<string>();
        string? continuationToken = null;

        do
        {
            var response = await _s3Client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = folderPath,
                ContinuationToken = continuationToken
            });

            var objects = response.S3Objects ?? [];
            result.AddRange(objects.Select(x => x.Key));
            continuationToken = response.IsTruncated == true ? response.NextContinuationToken : null;
        } while (!string.IsNullOrEmpty(continuationToken));

        return result;
    }

    /// <inheritdoc />
    public string SanitizeBlobName(string rawName)
    {
        var invalidChars = new[] { '\\', '/', '?', '#', '[', ']', ' ', '+', '`', '"' };
        var sanitizedName = new StringBuilder(rawName.Length);

        foreach (var c in rawName)
        {
            sanitizedName.Append(invalidChars.Contains(c) ? '_' : c);
        }

        return sanitizedName.ToString().ToLowerInvariant();
    }

    private async Task EnsureBucketExistsAsync()
    {
        if (_isBucketInitialized)
        {
            return;
        }

        await _bucketInitLock.WaitAsync();
        try
        {
            if (_isBucketInitialized)
            {
                return;
            }

            await CreateContainerIfNotExistsAsync();
            _isBucketInitialized = true;
        }
        finally
        {
            _bucketInitLock.Release();
        }
    }

    private static bool IsPreconditionFailed(AmazonS3Exception ex)
    {
        return ex.StatusCode == HttpStatusCode.PreconditionFailed
               || string.Equals(ex.ErrorCode, "PreconditionFailed", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string?> GetObjectETagAsync(string path)
    {
        var metadataTask = _s3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
        {
            BucketName = _bucketName,
            Key = path
        });

        if (metadataTask is null)
        {
            return null;
        }

        var metadata = await metadataTask;
        return metadata?.ETag;
    }

    private static bool ETagMatches(string? currentETag, string expectedETag)
    {
        if (string.IsNullOrWhiteSpace(currentETag) || string.IsNullOrWhiteSpace(expectedETag))
        {
            return false;
        }

        var normalizedCurrent = currentETag.Trim().Trim('"');
        var normalizedExpected = expectedETag.Trim().Trim('"');
        return string.Equals(normalizedCurrent, normalizedExpected, StringComparison.Ordinal);
    }
}