using System.Diagnostics;
using Shouldly;

namespace CloudStorageORM.IntegrationTests.Azure.Aws;

public class ProgramExitAwsTests(LocalStackFixture fixture) : IClassFixture<LocalStackFixture>
{
    [Fact]
    public async Task SampleApp_ShouldExitWithCodeZero_AndRunAllProviders()
    {
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

        process.StartInfo.Environment["CLOUDSTORAGEORM_AWS_ACCESS_KEY_ID"] = fixture.AccessKeyId;
        process.StartInfo.Environment["CLOUDSTORAGEORM_AWS_SECRET_ACCESS_KEY"] = fixture.SecretAccessKey;
        process.StartInfo.Environment["CLOUDSTORAGEORM_AWS_REGION"] = fixture.Region;
        process.StartInfo.Environment["CLOUDSTORAGEORM_AWS_SERVICE_URL"] = fixture.ServiceUrl;
        process.StartInfo.Environment["CLOUDSTORAGEORM_AWS_BUCKET"] = fixture.BucketName;
        process.StartInfo.Environment["CLOUDSTORAGEORM_AWS_FORCE_PATH_STYLE"] = "true";

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

        process.ExitCode.ShouldBe(0,
            $"SampleApp should exit cleanly with AWS provider.\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
        stdout.ShouldContain("SampleApp Finished");
        stdout.ShouldContain("Running using EF InMemory Provider");
        stdout.ShouldContain("Running using EF Azure Provider");
        stdout.ShouldContain("Running using EF Aws Provider");
        stdout.ShouldContain("Cloud provider: Azure");
        stdout.ShouldContain("Cloud provider: Aws");
        stdout.ShouldContain("Clearing users before run");
        stdout.ShouldContain("sample-user-001");
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