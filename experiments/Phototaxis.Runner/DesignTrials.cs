using System.Diagnostics;
using System.Text.Json;
using Gojo.Core;
using Phototaxis;

internal static class DesignTrials
{
    public static void Run(string[] args)
    {
        var round = int.Parse(args[1]); var seconds = int.Parse(args[2]);
        var seeds = args[3].Split(',').Select(int.Parse).ToArray();
        var destination = Path.GetFullPath(args[4]); Directory.CreateDirectory(destination);
        var candidates = DesignedControllers.Round(round);
        var onlyPosition = Array.IndexOf(args,"--only");
        if (onlyPosition >= 0)
        {
            var names = args[onlyPosition+1].Split(',');
            candidates = candidates.Where(d => names.Contains(d.Name)).ToArray();
            if (candidates.Length != names.Length) throw new ArgumentException("Requested designs are not all present.");
        }
        var records = new List<object>();
        var scores = new List<(ControllerDesign Design, double Life, double Alive, double Births)>();
        foreach (var design in candidates)
        {
            var lives = new List<double>(); var alive = new List<double>(); var born = new List<double>();
            foreach (var seed in seeds)
            {
                var bath = new Bath(seed, 20, rocks:args.Contains("--rocks"),
                    resources:args.Contains("--live") ? null : MachineResources.Fixed(.9), pulsedLights:true,
                    configureController:DesignedControllers.Builder(design)) { Learning = false };
                var first = bath.Creatures[0];
                var operations = ExpressionGraph.Count(first.Features,
                    first.LeftMuscle.Current.Components.Concat(first.RightMuscle.Current.Components).Select(c => c.Source).ToArray());
                var timer = Stopwatch.StartNew(); var populationTicks = 0L; var energySum = 0.0;
                while (bath.Seconds < seconds && bath.Creatures.Length != 0)
                {
                    populationTicks += bath.Creatures.Length; energySum += bath.MeanEnergy * bath.Creatures.Length;
                    bath.Step();
                }
                timer.Stop(); bath.CheckConservation();
                var life = populationTicks / (double)Bath.TicksPerSecond;
                lives.Add(life); alive.Add(bath.Creatures.Length); born.Add(bath.Births);
                var record = new { design, seed, requestedSeconds = seconds, simulatedSeconds = bath.Seconds,
                    population = bath.Creatures.Length, births = bath.Births, founderSurvivors = bath.Creatures.Count(c => c.Slot < 20),
                    populationSeconds = life, meanEnergyDuringLife = populationTicks == 0 ? 0 : energySum / populationTicks,
                    caloriesAbsorbed = bath.TotalFood, foodPerCreatureSecond = life == 0 ? 0 : bath.TotalFood / life,
                    predation = bath.PredationDeaths, starvation = bath.StarvationDeaths, meanEnergy = bath.MeanEnergy,
                    stationaryShare = bath.StationaryShare, hungryTowardLight = bath.HungryTowardLight, fullAwayFromLight = bath.FullAwayFromLight,
                    hungrySamples = bath.HungryMovementSamples, fullSamples = bath.FullMovementSamples,
                    operations, feedbackCells = Phototaxis.Native.Parts(first.ControllerState).Length, rewrites = bath.Rewrites,
                    unresolved = bath.Creatures.Sum(c => c.LeftMuscle.Unresolved + c.RightMuscle.Unresolved),
                    elapsedSeconds = timer.Elapsed.TotalSeconds, measuredResources = bath.Resources.Measured,
                    computeCalories = bath.ComputeCalories, machineDuty = bath.Resources.Duty, resourceYield = bath.FoodAvailability,
                    lightSchedules = bath.TimedLights.Schedules,
                    caloriesConserved = bath.TotalCalories == 20_000_000, attentionConserved = bath.TotalAttention == 10_000_000 };
                records.Add(record); Console.WriteLine(JsonSerializer.Serialize(record));
                if (args.Contains("--archive"))
                    File.WriteAllBytes(Path.Combine(destination,$"{design.Name}-seed-{seed}.gojo"), GojoNumericFormat.Encode(bath.Root));
            }
            scores.Add((design, lives.Average(), alive.Average(), born.Average()));
        }
        var ranked = scores.OrderByDescending(s => s.Life).ThenByDescending(s => s.Alive).ThenByDescending(s => s.Births)
            .Select(s => new { design = s.Design, meanPopulationSeconds = s.Life, meanAlive = s.Alive, meanBirths = s.Births }).ToArray();
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(Path.Combine(destination, "results.json"), JsonSerializer.Serialize(records, options));
        File.WriteAllText(Path.Combine(destination, "ranking.json"), JsonSerializer.Serialize(ranked, options));
        File.WriteAllBytes(Path.Combine(destination, "winner.gojo"), new Bath(seeds[0], 1, resources:MachineResources.Fixed(.9),
            pulsedLights:true, configureController:DesignedControllers.Builder(ranked[0].design)).Creatures[0].ExportNativeGraph());
        Console.WriteLine(JsonSerializer.Serialize(new { round, winner = ranked[0] }));
    }
}
