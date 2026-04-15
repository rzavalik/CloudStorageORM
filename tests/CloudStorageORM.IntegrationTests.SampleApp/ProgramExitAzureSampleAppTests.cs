using System.Diagnostics;
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

        var repoRoot = FindRepoRoot();

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        process.StartInfo.ArgumentList.Add("run");
        process.StartInfo.ArgumentList.Add("--project");
        process.StartInfo.ArgumentList.Add("samples/CloudStorageORM.SampleApp/SampleApp.csproj");
        process.StartInfo.ArgumentList.Add("-c");
        process.StartInfo.ArgumentList.Add("Debug");

        process.StartInfo.Environment["CLOUDSTORAGEORM_AZURE_CONNECTION_STRING"] = fixture.ConnectionString;
        process.StartInfo.Environment["CLOUDSTORAGEORM_CONTAINER_NAME"] = fixture.ContainerName;

        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        var waitForExitTask = process.WaitForExitAsync();
        var timeoutTask = Task.Delay(TimeSpan.FromMinutes(2));

        var completedTask = await Task.WhenAny(waitForExitTask, timeoutTask);
        if (completedTask == timeoutTask)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Ignore kill failures: the process may have already exited.
            }

            var partialOutput = await stdoutTask;
            var partialError = await stderrTask;
            throw new TimeoutException(
                $"SampleApp did not exit within 2 minutes.\nSTDOUT:\n{partialOutput}\nSTDERR:\n{partialError}");
        }

        await waitForExitTask;
        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        process.ExitCode.ShouldBe(0, $"SampleApp should exit cleanly.\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
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

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var solutionFile = Path.Combine(current.FullName, "CloudStorageORM.sln");
            if (File.Exists(solutionFile))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate workspace root containing CloudStorageORM.sln.");
    }
}