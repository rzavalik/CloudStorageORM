using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Azure;
using Microsoft.EntityFrameworkCore;

namespace CloudStorageORM.Infrastructure;

/// <summary>
/// Classifies exceptions that are safe to retry at CloudStorageORM's shared execution boundaries.
/// </summary>
internal static class CloudStorageTransientExceptionClassifier
{
    private static readonly HashSet<int> TransientStatusCodes = [408, 429, 500, 502, 503, 504];

    private static readonly HashSet<string> AwsTransientErrorCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "RequestTimeout",
        "RequestTimeoutException",
        "Throttling",
        "ThrottlingException",
        "SlowDown",
        "InternalError",
        "ServiceUnavailable",
        "TooManyRequestsException"
    };

    /// <summary>
    /// Determines whether the provided exception should be treated as transient.
    /// </summary>
    /// <param name="exception">The exception to classify.</param>
    /// <returns><see langword="true" /> when the exception is considered transient; otherwise <see langword="false" />.</returns>
    public static bool IsTransient(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            StoragePreconditionFailedException or DbUpdateConcurrencyException => false,
            TimeoutException or HttpRequestException or IOException or TaskCanceledException => true,
            RequestFailedException requestFailedException => TransientStatusCodes.Contains(
                requestFailedException.Status),
            AmazonS3Exception s3Exception => IsAwsTransientStatusCode(s3Exception.StatusCode) ||
                                             IsAwsTransientErrorCode(s3Exception.ErrorCode),
            AmazonServiceException amazonServiceException =>
                IsAwsTransientStatusCode(amazonServiceException.StatusCode) ||
                IsAwsTransientErrorCode(amazonServiceException.ErrorCode),
            _ => exception.InnerException is not null && IsTransient(exception.InnerException)
        };
    }

    private static bool IsAwsTransientStatusCode(HttpStatusCode statusCode)
    {
        return statusCode != 0 && TransientStatusCodes.Contains((int)statusCode);
    }

    private static bool IsAwsTransientErrorCode(string? errorCode)
    {
        return !string.IsNullOrWhiteSpace(errorCode) && AwsTransientErrorCodes.Contains(errorCode);
    }
}