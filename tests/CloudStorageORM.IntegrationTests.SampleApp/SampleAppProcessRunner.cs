using System.Diagnostics;

namespace CloudStorageORM.IntegrationTests.SampleApp;

internal static class SampleAppProcessRunner
{
    private const string SampleAppProjectPath = "samples/CloudStorageORM.SampleApp/SampleApp.csproj";
    private const string SampleAppAssemblyName = "CloudStorageORM.SampleApp.dll";

    public static string FindRepoRoot()
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

    public static async Task<string> PublishSampleAppAsync(string repoRoot)
    {
        var publishDir = Path.Combine(Path.GetTempPath(), "CloudStorageORM.SampleApp", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(publishDir);

        using var publishProcess = new Process();
        publishProcess.StartInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        publishProcess.StartInfo.ArgumentList.Add("publish");
        publishProcess.StartInfo.ArgumentList.Add(SampleAppProjectPath);
        publishProcess.StartInfo.ArgumentList.Add("-c");
        publishProcess.StartInfo.ArgumentList.Add("Debug");
        publishProcess.StartInfo.ArgumentList.Add("-o");
        publishProcess.StartInfo.ArgumentList.Add(publishDir);

        publishProcess.Start();

        var publishStdoutTask = publishProcess.StandardOutput.ReadToEndAsync();
        var publishStderrTask = publishProcess.StandardError.ReadToEndAsync();
        await publishProcess.WaitForExitAsync();

        var publishStdout = await publishStdoutTask;
        var publishStderr = await publishStderrTask;

        if (publishProcess.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"dotnet publish failed with exit code {publishProcess.ExitCode}.\nSTDOUT:\n{publishStdout}\nSTDERR:\n{publishStderr}");
        }

        return publishDir;
    }

    public static async Task<SampleAppRunResult> RunPublishedSampleAppAsync(
        string repoRoot,
        string publishDir,
        IReadOnlyDictionary<string, string?>? environmentVariables,
        TimeSpan timeout)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        process.StartInfo.ArgumentList.Add(Path.Combine(publishDir, SampleAppAssemblyName));

        if (environmentVariables is not null)
        {
            foreach (var (key, value) in environmentVariables)
            {
                process.StartInfo.Environment[key] = value;
            }
        }

        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        var waitForExitTask = process.WaitForExitAsync();
        var timeoutTask = Task.Delay(timeout);

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
                $"Published SampleApp did not exit within {timeout}.\nSTDOUT:\n{partialOutput}\nSTDERR:\n{partialError}");
        }

        await waitForExitTask;
        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        return new SampleAppRunResult(process.ExitCode, stdout, stderr);
    }

    public static void TryDeletePublishDirectory(string publishDir)
    {
        if (string.IsNullOrWhiteSpace(publishDir) || !Directory.Exists(publishDir))
        {
            return;
        }

        try
        {
            Directory.Delete(publishDir, recursive: true);
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }
}

internal sealed record SampleAppRunResult(int ExitCode, string StdOut, string StdErr);

