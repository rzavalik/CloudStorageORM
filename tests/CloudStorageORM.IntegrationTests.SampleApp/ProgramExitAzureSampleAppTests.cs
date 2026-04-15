using CloudStorageORM.IntegrationTests.Azure;
using Shouldly;

namespace CloudStorageORM.IntegrationTests.SampleApp;

public class ProgramExitAzureSampleAppTests(StorageFixture fixture)
    : IClassFixture<StorageFixture>
{
    [Fact]
    public async Task SampleApp_ShouldExitWithCodeZero_AndRunAllProviders()
    {
        fixture.ShouldNotBeNull();
        fixture.EnsureAvailableOrSkip();

        var repoRoot = SampleAppProcessRunner.FindRepoRoot();
        var env = new Dictionary<string, string?>
        {
            ["CLOUDSTORAGEORM_AZURE_CONNECTION_STRING"] = fixture.ConnectionString,
            ["CLOUDSTORAGEORM_CONTAINER_NAME"] = fixture.ContainerName
        };

        string? publishDir = null;
        try
        {
            publishDir = await SampleAppProcessRunner.PublishSampleAppAsync(repoRoot);
            var runResult = await SampleAppProcessRunner.RunPublishedSampleAppAsync(
                repoRoot,
                publishDir,
                env,
                TimeSpan.FromMinutes(2));

            var stdout = runResult.StdOut;
            var stderr = runResult.StdErr;

            runResult.ExitCode.ShouldBe(0, $"SampleApp should exit cleanly.\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
            stdout.ShouldContain("SampleApp Finished");
            stdout.ShouldContain("Running using EF InMemory Provider");
            stdout.ShouldContain("Running using EF Azure Provider");
            stdout.ShouldContain("Running using EF Aws Provider");
            stdout.ShouldContain("Clearing users before run");
            stdout.ShouldContain("sample-user-001");
            stdout.ShouldContain("Rollback verification passed");
            stdout.ShouldContain("Commit verification passed");
            stdout.ShouldNotContain("verification failed");
            stdout.ShouldNotContain("An error occurred");

            stdout.IndexOf("Running using EF InMemory Provider", StringComparison.Ordinal)
                .ShouldBeLessThan(stdout.IndexOf("Running using EF Azure Provider", StringComparison.Ordinal));
            stdout.IndexOf("Running using EF Azure Provider", StringComparison.Ordinal)
                .ShouldBeLessThan(stdout.IndexOf("Running using EF Aws Provider", StringComparison.Ordinal));
        }
        finally
        {
            if (publishDir is not null)
            {
                SampleAppProcessRunner.TryDeletePublishDirectory(publishDir);
            }
        }
    }
}