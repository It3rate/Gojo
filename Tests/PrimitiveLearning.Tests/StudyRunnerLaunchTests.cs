using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;

namespace PrimitiveLearning.Tests;

public sealed class StudyRunnerLaunchTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LaunchOutsideRepositoryRootRecordsSourcesAndCompletes(bool unrelatedWorkingDirectory)
    {
        var temporary = Directory.CreateTempSubdirectory("gojo-primitive-learning-launch-");
        var output = Path.Combine(temporary.FullName, "results");
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = unrelatedWorkingDirectory ? temporary.FullName : AppContext.BaseDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (var argument in new[] { typeof(StudyRunner).Assembly.Location, "--pilot", "--seeds", "1",
            "--train", "2", "--test", "2", "--warmup", "1", "--output", output })
            start.ArgumentList.Add(argument);

        try
        {
            using var process = Process.Start(start)!;
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            try { await process.WaitForExitAsync(timeout.Token); }
            finally
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    await process.WaitForExitAsync();
                }
            }
            Assert.True(process.ExitCode == 0, $"{await stdout}\n{await stderr}");

            using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "manifest.json")));
            var hashes = manifest.RootElement.GetProperty("sourceHashes");
            var sources = Directory.GetFiles(SourceDirectory(), "*.cs");
            Assert.Equal(sources.Length, hashes.EnumerateObject().Count());
            foreach (var source in sources)
                Assert.Equal(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(source))).ToLowerInvariant(),
                    hashes.GetProperty(Path.GetFileName(source)).GetString());

            using var run = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "run.json")));
            Assert.True(run.RootElement.GetProperty("rows").GetInt32() > 0);
            Assert.Equal(output, run.RootElement.GetProperty("settings").GetProperty("output").GetString());
            Assert.True(File.Exists(Path.Combine(output, "index.html")));
        }
        finally { temporary.Delete(recursive: true); }
    }

    private static string SourceDirectory([CallerFilePath] string testFile = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(testFile)!, "..", "..", "experiments", "PrimitiveLearning"));
}
