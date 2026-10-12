namespace CompositeEmergence;

public static class Program
{
    public static int Main(string[] args)
    {
        string? output = null; var started = false;
        try
        {
            if (args.Contains("--help"))
            {
                Console.WriteLine("Composite emergence: --pilot or --campaign; --output DIRECTORY; optional --seeds N --seed-start N --discovery N --consumer N --test N --refresh-train N --refresh-test N --hazard FRACTION --probe-price N.");
                return 0;
            }
            var settings = args.Contains("--campaign") ? StudySettings.Campaign : StudySettings.Small;
            for (var i = 0; i < args.Length; i++)
            {
                var name = args[i]; if (name is "--pilot" or "--campaign") continue;
                if (++i >= args.Length) throw new ArgumentException($"Missing value for {name}.");
                var value = args[i];
                settings = name switch
                {
                    "--seeds" => settings with { Seeds = int.Parse(value) },
                    "--seed-start" => settings with { SeedStart = int.Parse(value) },
                    "--discovery" => settings with { DiscoveryTrain = int.Parse(value) },
                    "--consumer" => settings with { ConsumerTrain = int.Parse(value) },
                    "--test" => settings with { TestEpisodes = int.Parse(value) },
                    "--refresh-train" => settings with { RefreshTrain = int.Parse(value) },
                    "--refresh-test" => settings with { RefreshTest = int.Parse(value) },
                    "--hazard" => settings with { SwitchProbability = double.Parse(value, System.Globalization.CultureInfo.InvariantCulture) },
                    "--probe-price" => settings with { ProbePrice = int.Parse(value) },
                    "--output" => settings,
                    _ => throw new ArgumentException($"Unknown option {name}.")
                };
                if (name == "--output") output = Path.GetFullPath(value);
            }
            if (settings.Seeds < 1 || settings.SeedStart < 0 || settings.DiscoveryTrain < 1 || settings.ConsumerTrain < 1 ||
                settings.TestEpisodes < 1 || settings.RefreshTrain < 1 || settings.RefreshTest < 1 ||
                !double.IsFinite(settings.SwitchProbability) || settings.SwitchProbability is < 0 or > 1 || settings.ProbePrice is < 0 or > 1000)
                throw new ArgumentException("Counts must be positive, seed start nonnegative, hazard 0..1 and probe price 0..1000.");
            output ??= Path.GetFullPath(Path.Combine("experiments", "CompositeEmergence", "results", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + (settings.Pilot ? "-pilot" : "-confirmation")));
            if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any())
                throw new IOException($"Output directory already contains files: {output}. Choose a fresh directory.");
            started = true; new StudyRunner(settings, output).Run();
            Console.WriteLine($"Inspect {Path.Combine(output, "index.html")}");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            if (started && output is not null && Directory.Exists(output) && !File.Exists(Path.Combine(output, "failure.json")))
                File.WriteAllText(Path.Combine(output, "failure.json"), System.Text.Json.JsonSerializer.Serialize(new { error = exception.ToString(), utc = DateTime.UtcNow }, StudyRunner.Json));
            return 1;
        }
    }
}
