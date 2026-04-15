using CloudStorageORM.Observability;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace CloudStorageORM.Tests.Observability;

public class CloudStorageOrmEventIdsTests
{
    [Fact]
    public void EventIds_MatchExpectedValues()
    {
        var all = GetAll();

        all.ShouldContain(x =>
            x == CloudStorageOrmEventIds.ConfigurationInitialized && x.Id == 1001 &&
            x.Name == nameof(CloudStorageOrmEventIds.ConfigurationInitialized));
        all.ShouldContain(x =>
            x == CloudStorageOrmEventIds.QueryExecutionStarting && x.Id == 2001 &&
            x.Name == nameof(CloudStorageOrmEventIds.QueryExecutionStarting));
        all.ShouldContain(x =>
            x == CloudStorageOrmEventIds.QueryExecutionCompleted && x.Id == 2002 &&
            x.Name == nameof(CloudStorageOrmEventIds.QueryExecutionCompleted));
        all.ShouldContain(x =>
            x == CloudStorageOrmEventIds.QueryExecutionFailed && x.Id == 2003 &&
            x.Name == nameof(CloudStorageOrmEventIds.QueryExecutionFailed));
        all.ShouldContain(x =>
            x == CloudStorageOrmEventIds.SaveChangesStarting && x.Id == 3001 &&
            x.Name == nameof(CloudStorageOrmEventIds.SaveChangesStarting));
        all.ShouldContain(x =>
            x == CloudStorageOrmEventIds.SaveChangesCompleted && x.Id == 3002 &&
            x.Name == nameof(CloudStorageOrmEventIds.SaveChangesCompleted));
        all.ShouldContain(x =>
            x == CloudStorageOrmEventIds.SaveChangesFailed && x.Id == 3003 &&
            x.Name == nameof(CloudStorageOrmEventIds.SaveChangesFailed));
        all.ShouldContain(x =>
            x == CloudStorageOrmEventIds.EntitySaved && x.Id == 3004 &&
            x.Name == nameof(CloudStorageOrmEventIds.EntitySaved));
        all.ShouldContain(x =>
            x == CloudStorageOrmEventIds.TransactionBeginning && x.Id == 4001 &&
            x.Name == nameof(CloudStorageOrmEventIds.TransactionBeginning));
        all.ShouldContain(x =>
            x == CloudStorageOrmEventIds.TransactionCommitted && x.Id == 4002 &&
            x.Name == nameof(CloudStorageOrmEventIds.TransactionCommitted));
        all.ShouldContain(x =>
            x == CloudStorageOrmEventIds.TransactionRolledBack && x.Id == 4003 &&
            x.Name == nameof(CloudStorageOrmEventIds.TransactionRolledBack));
        all.ShouldContain(x =>
            x == CloudStorageOrmEventIds.ConcurrencyConflict && x.Id == 5001 &&
            x.Name == nameof(CloudStorageOrmEventIds.ConcurrencyConflict));
        all.ShouldContain(x =>
            x == CloudStorageOrmEventIds.BlobUploadStarting && x.Id == 6001 &&
            x.Name == nameof(CloudStorageOrmEventIds.BlobUploadStarting));
        all.ShouldContain(x =>
            x == CloudStorageOrmEventIds.BlobUploadCompleted && x.Id == 6002 &&
            x.Name == nameof(CloudStorageOrmEventIds.BlobUploadCompleted));
        all.ShouldContain(x =>
            x == CloudStorageOrmEventIds.BlobDownloadStarting && x.Id == 6003 &&
            x.Name == nameof(CloudStorageOrmEventIds.BlobDownloadStarting));
        all.ShouldContain(x =>
            x == CloudStorageOrmEventIds.BlobDownloadCompleted && x.Id == 6004 &&
            x.Name == nameof(CloudStorageOrmEventIds.BlobDownloadCompleted));
        all.ShouldContain(x =>
            x == CloudStorageOrmEventIds.BlobDeletionStarting && x.Id == 6005 &&
            x.Name == nameof(CloudStorageOrmEventIds.BlobDeletionStarting));
        all.ShouldContain(x =>
            x == CloudStorageOrmEventIds.BlobDeletionCompleted && x.Id == 6006 &&
            x.Name == nameof(CloudStorageOrmEventIds.BlobDeletionCompleted));
        all.ShouldContain(x =>
            x == CloudStorageOrmEventIds.ValidationStarting && x.Id == 7001 &&
            x.Name == nameof(CloudStorageOrmEventIds.ValidationStarting));
        all.ShouldContain(x =>
            x == CloudStorageOrmEventIds.ValidationCompleted && x.Id == 7002 &&
            x.Name == nameof(CloudStorageOrmEventIds.ValidationCompleted));
        all.ShouldContain(x =>
            x == CloudStorageOrmEventIds.ValidationFailed && x.Id == 7003 &&
            x.Name == nameof(CloudStorageOrmEventIds.ValidationFailed));
    }

    [Fact]
    public void EventIds_AreUnique()
    {
        var ids = GetAll().Select(x => x.Id).ToList();

        ids.Count.ShouldBe(ids.Distinct().Count());
    }

    private static IReadOnlyList<EventId> GetAll() =>
    [
        CloudStorageOrmEventIds.ConfigurationInitialized,
        CloudStorageOrmEventIds.QueryExecutionStarting,
        CloudStorageOrmEventIds.QueryExecutionCompleted,
        CloudStorageOrmEventIds.QueryExecutionFailed,
        CloudStorageOrmEventIds.SaveChangesStarting,
        CloudStorageOrmEventIds.SaveChangesCompleted,
        CloudStorageOrmEventIds.SaveChangesFailed,
        CloudStorageOrmEventIds.EntitySaved,
        CloudStorageOrmEventIds.TransactionBeginning,
        CloudStorageOrmEventIds.TransactionCommitted,
        CloudStorageOrmEventIds.TransactionRolledBack,
        CloudStorageOrmEventIds.ConcurrencyConflict,
        CloudStorageOrmEventIds.BlobUploadStarting,
        CloudStorageOrmEventIds.BlobUploadCompleted,
        CloudStorageOrmEventIds.BlobDownloadStarting,
        CloudStorageOrmEventIds.BlobDownloadCompleted,
        CloudStorageOrmEventIds.BlobDeletionStarting,
        CloudStorageOrmEventIds.BlobDeletionCompleted,
        CloudStorageOrmEventIds.ValidationStarting,
        CloudStorageOrmEventIds.ValidationCompleted,
        CloudStorageOrmEventIds.ValidationFailed
    ];
}