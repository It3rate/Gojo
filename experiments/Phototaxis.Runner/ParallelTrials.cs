using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text.Json;
using Phototaxis;

/// <summary>Host experiment scheduler. Each child owns an independent Gojo world.</summary>
internal static class ParallelTrials
{
    public static async Task Run(string[] args, NativeExecutionBackend backend)
    {
        var seconds = Number(args, "--seconds", 30);
        var warmup = Number(args, "--warmup", 5);
        var population = Number(args, "--population", 20);
        var count = Number(args, "--count", 4);
        var parallel = Number(args, "--parallel", 1);
        var seeds = (Option(args, "--seeds") ?? "7,19,31,43").Split(',')
            .Select(s => int.Parse(s, CultureInfo.InvariantCulture)).ToArray();
        var output = Option(args, "--output");
        if (seconds <= 0 || population <= 0 || count <= 0 || warmup < 0 || parallel is < 1 or > 4 || seeds.Length == 0)
            throw new ArgumentException("Trials require positive seconds, population and count, nonnegative warmup, and --parallel between 1 and 4.");

        using var cancellation = new CancellationTokenSource();
        var userCancelled = 0;
        ConsoleCancelEventHandler cancel = (_, e) =>
        {
            e.Cancel = true;
            Interlocked.Exchange(ref userCancelled, 1);
            cancellation.Cancel();
        };
        Console.CancelKeyPress += cancel;
        try
        {
            using var slots = new SemaphoreSlim(parallel);
            var batch = Stopwatch.StartNew();
            var pending = Enumerable.Range(0, count).Select(async index =>
            {
                await slots.WaitAsync(cancellation.Token);
                try
                {
                    return await RunChild(index, seeds[index % seeds.Length], seconds, warmup, population, backend, cancellation.Token);
                }
                catch { cancellation.Cancel(); throw; }
                finally { slots.Release(); }
            }).ToArray();
            var trials = await Task.WhenAll(pending);
            batch.Stop();
            var sameSeedDigestsConsistent = trials.GroupBy(t => t.Seed).All(g =>
                g.Select(t => t.Profile.GetProperty("stateDigest").GetString()).Distinct().Count() == 1);
            if (!sameSeedDigestsConsistent)
                throw new InvalidOperationException("Repeated trials of the same seed produced different observed state digests.");

            var json = JsonSerializer.Serialize(new
            {
                backend = backend.ToString(), secondsToRun = seconds, warmupSeconds = warmup,
                initialPopulation = population, trialCount = count, maxParallel = parallel, seeds,
                fixedResources = true, cognitive = true, roving = true, isolatedProcesses = true,
                host = HostInfo(), batchWallSeconds = batch.Elapsed.TotalSeconds,
                // Startup, warmup, output and process exit are deliberately included in the denominator.
                endToEndSimulatedSpeed = count * (double)seconds / batch.Elapsed.TotalSeconds,
                includingWarmupSimulatedSpeed = count * (double)(seconds + warmup) / batch.Elapsed.TotalSeconds,
                summedTimedWallSeconds = trials.Sum(t => t.Profile.GetProperty("wallSeconds").GetDouble()),
                summedTimedCpuSeconds = trials.Sum(t => t.Profile.GetProperty("cpuSeconds").GetDouble()),
                summedTimedAllocatedBytes = trials.Sum(t => t.Profile.GetProperty("allocatedBytes").GetInt64()),
                sameSeedDigestsConsistent,
                trials = trials.Select(t => new { index = t.Index + 1, seed = t.Seed,
                    childArguments = t.Arguments, childEndToEndSeconds = t.EndToEndSeconds, profile = t.Profile })
            }, new JsonSerializerOptions { WriteIndented = true });
            if (output is not null)
            {
                var path = Path.GetFullPath(output);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                await File.WriteAllTextAsync(path, json, cancellation.Token);
            }
            Console.WriteLine(json);
        }
        catch (OperationCanceledException) when (Volatile.Read(ref userCancelled) != 0)
        {
            // Task.WhenAll has already observed every trial; each launched child
            // is terminated and awaited by RunChild's cleanup before this returns.
            Console.WriteLine(JsonSerializer.Serialize(new { cancelled = true, backend = backend.ToString(),
                secondsToRun = seconds, warmupSeconds = warmup, initialPopulation = population,
                trialCount = count, maxParallel = parallel }));
            Environment.ExitCode = 130;
        }
        finally { Console.CancelKeyPress -= cancel; }
    }

    public static object HostInfo() => new
    {
        logicalProcessors = Environment.ProcessorCount,
        processor = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER"),
        framework = RuntimeInformation.FrameworkDescription,
        architecture = RuntimeInformation.ProcessArchitecture.ToString(),
        os = RuntimeInformation.OSDescription,
        vector128Accelerated = Vector128.IsHardwareAccelerated,
        vector256Accelerated = Vector256.IsHardwareAccelerated,
        avx2Supported = Avx2.IsSupported
    };

    private static async Task<Trial> RunChild(int index, int seed, int seconds, int warmup, int population,
        NativeExecutionBackend backend, CancellationToken cancellation)
    {
        var start = new ProcessStartInfo
        {
            FileName = Environment.ProcessPath ?? throw new InvalidOperationException("Cannot locate the running executable."),
            UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true,
            CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden
        };
        // A framework-dependent invocation needs its entry DLL; an apphost already knows its DLL.
        if (string.Equals(Path.GetFileNameWithoutExtension(start.FileName), "dotnet", StringComparison.OrdinalIgnoreCase))
            start.ArgumentList.Add(Assembly.GetEntryAssembly()!.Location);
        foreach (var value in new[] { "profile", seconds.ToString(CultureInfo.InvariantCulture),
            population.ToString(CultureInfo.InvariantCulture), "--cognitive", "--roving", "--fixed-resources",
            "--backend", backend.ToString(), "--warmup", warmup.ToString(CultureInfo.InvariantCulture),
            "--seed", seed.ToString(CultureInfo.InvariantCulture) }) start.ArgumentList.Add(value);
        var arguments = start.ArgumentList.ToArray();
        using var process = new Process { StartInfo = start };
        var elapsed = Stopwatch.StartNew();
        if (!process.Start()) throw new InvalidOperationException("Could not start a trial process.");
        var stdout = process.StandardOutput.ReadToEndAsync(cancellation);
        var stderr = process.StandardError.ReadToEndAsync(cancellation);
        try
        {
            await process.WaitForExitAsync(cancellation);
            var raw = await stdout;
            var errors = await stderr;
            elapsed.Stop();
            if (process.ExitCode != 0)
                throw new InvalidOperationException($"Trial {index + 1}, seed {seed}, exited {process.ExitCode}: {errors}");
            using var document = JsonDocument.Parse(raw);
            var profile = document.RootElement.Clone();
            if (!profile.GetProperty("fixedResources").GetBoolean() || profile.GetProperty("seed").GetInt32() != seed)
                throw new InvalidOperationException("Child profile did not use the requested seed and fixed resources.");
            return new(index, seed, arguments, elapsed.Elapsed.TotalSeconds, profile);
        }
        finally
        {
            if (!process.HasExited)
            {
                try { process.Kill(entireProcessTree: true); }
                catch (InvalidOperationException) when (process.HasExited) { }
                catch (System.ComponentModel.Win32Exception) when (process.HasExited) { }
                await process.WaitForExitAsync();
            }
        }
    }

    private static int Number(string[] args, string name, int fallback) =>
        Option(args, name) is { } value ? int.Parse(value, CultureInfo.InvariantCulture) : fallback;

    private static string? Option(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);
        if (index < 0) return null;
        if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
            throw new ArgumentException($"{name} requires a value.");
        return args[index + 1];
    }

    private sealed record Trial(int Index, int Seed, string[] Arguments, double EndToEndSeconds, JsonElement Profile);
}
