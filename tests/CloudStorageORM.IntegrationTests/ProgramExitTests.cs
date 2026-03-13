using System.Diagnostics;
using Shouldly;

namespace CloudStorageORM.IntegrationTests.Azure;

public class ProgramExitTests(StorageFixture fixture) : IClassFixture<StorageFixture>
{
    [Fact]
    public async Task SampleApp_ShouldExitWithCodeZero()
    {
        fixture.ShouldNotBeNull();
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