using CloudStorageORM.Infrastructure;
using CloudStorageORM.Options;
using Shouldly;

namespace CloudStorageORM.Tests.Infrastructure;

public class CloudStorageRetryExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_WithRetryEnabled_RetriesTransientFailureUntilSuccess()
    {
        var options = new CloudStorageRetryOptions
        {
            Enabled = true,
            MaxRetries = 3,
            BaseDelay = TimeSpan.Zero,
            MaxDelay = TimeSpan.Zero,
            JitterFactor = 0
        };

        var attempts = 0;
        var executor = new CloudStorageRetryExecutor(
            options,
            _ => true,
            static (_, _) => Task.CompletedTask,
            static () => 0.5d);

        await executor.ExecuteAsync(async _ =>
        {
            attempts++;
            if (attempts < 3)
            {
                throw new HttpRequestException("temporary");
            }

            await Task.CompletedTask;
        });

        attempts.ShouldBe(3);
    }

    [Fact]
    public async Task ExecuteAsync_WithRetryDisabled_DoesNotRetry()
    {
        var options = new CloudStorageRetryOptions
        {
            Enabled = false,
            MaxRetries = 5,
            BaseDelay = TimeSpan.Zero,
            MaxDelay = TimeSpan.Zero,
            JitterFactor = 0
        };

        var attempts = 0;
        var executor = new CloudStorageRetryExecutor(
            options,
            _ => true,
            static (_, _) => Task.CompletedTask,
            static () => 0.5d);

        await Should.ThrowAsync<HttpRequestException>(() => executor.ExecuteAsync(_ =>
        {
            attempts++;
            throw new HttpRequestException("temporary");
        }));

        attempts.ShouldBe(1);
    }

    [Fact]
    public async Task ExecuteAsync_WithNonTransientException_DoesNotRetry()
    {
        var options = new CloudStorageRetryOptions
        {
            Enabled = true,
            MaxRetries = 5,
            BaseDelay = TimeSpan.Zero,
            MaxDelay = TimeSpan.Zero,
            JitterFactor = 0
        };

        var attempts = 0;
        var executor = new CloudStorageRetryExecutor(
            options,
            _ => false,
            static (_, _) => Task.CompletedTask,
            static () => 0.5d);

        await Should.ThrowAsync<InvalidOperationException>(() => executor.ExecuteAsync(_ =>
        {
            attempts++;
            throw new InvalidOperationException("non-transient");
        }));

        attempts.ShouldBe(1);
    }

    [Fact]
    public async Task ExecuteAsync_StopsAtConfiguredRetryBudget()
    {
        var options = new CloudStorageRetryOptions
        {
            Enabled = true,
            MaxRetries = 2,
            BaseDelay = TimeSpan.Zero,
            MaxDelay = TimeSpan.Zero,
            JitterFactor = 0
        };

        var attempts = 0;
        var executor = new CloudStorageRetryExecutor(
            options,
            _ => true,
            static (_, _) => Task.CompletedTask,
            static () => 0.5d);

        await Should.ThrowAsync<HttpRequestException>(() => executor.ExecuteAsync(_ =>
        {
            attempts++;
            throw new HttpRequestException("temporary");
        }));

        attempts.ShouldBe(3);
    }
}