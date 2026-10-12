using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Phototaxis;

internal static class CognitiveTrials
{
    internal static async Task Run(string[] args)
    {
        var seconds = args.Length > 1 ? int.Parse(args[1]) : 180;
        var seeds = (args.Length > 2 ? args[2] : "7,19").Split(',').Select(int.Parse).ToArray();
        var output = Path.GetFullPath(args.Length > 3 ? args[3] : "artifacts/phototaxis/cognitive");
        Directory.CreateDirectory(output);
        var live = args.Contains("--live"); var jev = args.Contains("--jev");
        var eyeIndex = Array.IndexOf(args, "--eye-levels");
        var eyeLevels = eyeIndex >= 0 ? int.Parse(args[eyeIndex + 1]) : 256;
        var wallIndex = Array.IndexOf(args, "--wall-seconds");
        var wallLimit = wallIndex >= 0 ? double.Parse(args[wallIndex + 1], System.Globalization.CultureInfo.InvariantCulture) : 120;
        var archives = !args.Contains("--no-archives");
        var select = !args.Contains("--fixed-inheritance");
        var binocular = !args.Contains("--averaged-eyes");
        double Setting(string name, double fallback)
        {
            var index = Array.IndexOf(args, name);
            return index < 0 ? fallback : double.Parse(args[index + 1], System.Globalization.CultureInfo.InvariantCulture);
        }
        var ecology = new EcologySettings(Setting("--light-radius", 72), Setting("--light-on-scale", 1),
            checked((int)Setting("--patch-food", 0)), Setting("--light-drift", 0));
        var arenaScale = Setting("--arena-scale", 1);
        var population = checked((int)Setting("--population", 20));
        var features = new PilotFeatures(!args.Contains("--no-progress"), !args.Contains("--no-courtship"));
        var limitIndex = Array.IndexOf(args, "--max-jev");
        var requestLimit = limitIndex >= 0 ? Math.Clamp(int.Parse(args[limitIndex + 1]), 1, 20) : 20;
        string? key = null;
        if (jev)
        {
            key = Environment.GetEnvironmentVariable("TYPESAFE_API_KEY");
            var index = Array.IndexOf(args, "--key-file");
            if (string.IsNullOrWhiteSpace(key) && index >= 0 && index + 1 < args.Length)
                foreach (var line in File.ReadLines(args[index + 1]))
                    if (line.StartsWith("TYPESAFE_API_KEY=")) key = line.Split('=', 2)[1].Trim().Trim('"', '\'');
            if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException("Set TYPESAFE_API_KEY or provide --key-file.");
        }
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(25) };
        if (jev) http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
        var requests = 0; var inputTokens = 0L; var dollars = 0.0; var results = new List<object>();
        foreach (var seed in seeds)
        {
            var setup = Stopwatch.StartNew();
            var world = new Bath(seed, population, rocks: true, resources: live ? null : MachineResources.Fixed(.9),
                pulsedLights: true, cognitiveEcology: true, inquiries: jev, breedingSelection: select, eyeLevels:eyeLevels,
                ecology:ecology, binocular:binocular, arenaScale:arenaScale, pilotFeatures:features) { Learning = false };
            setup.Stop();
            var timer = Stopwatch.StartNew(); var lifetime = 0.0; var samples = new List<object>();
            var issued = 0L; var answers = 0L; var stale = 0L; var failures = 0L; var trials = 0L;
            var eyeSamples = 0L; var equalEyeSamples = 0L;
            var seen = new HashSet<InquiryQuestion>(ReferenceEqualityComparer.Instance);
            var bodies = new HashSet<CognitiveBody>(ReferenceEqualityComparer.Instance);
            var structures = new List<object>();
            var genomes = new HashSet<string>();
            var checkpoints = new Dictionary<Creature, (long Tick, double X, double Y, ulong Energy)>();
            var progressWindows = 0L; var lowProgressWindows = 0L; var mateCueSamples = 0L;
            for (var tick = 0; tick < seconds * Bath.TicksPerSecond && world.Creatures.Length != 0; tick++)
            {
                world.Step(); lifetime += world.Creatures.Length / (double)Bath.TicksPerSecond;
                foreach (var c in world.Creatures)
                {
                    // Read-only sensor diagnostics; these never enter utility or bids.
                    if (c.LeftEye.Presence.Value != 0 && c.RightEye.Presence.Value != 0)
                    {
                        eyeSamples++;
                        if (c.LeftEye.Pixels[0].Value == c.RightEye.Pixels[0].Value) equalEyeSamples++;
                    }
                    var body = c.Cognition!;
                    if (bodies.Add(body))
                    {
                        var parameters = body.Pilot.Modules.Select(m => m.Parameters.Select(Native.Held).ToArray()).ToArray();
                        var signature = JsonSerializer.Serialize(parameters); genomes.Add(signature);
                        var feedback = Native.Parts(c.ControllerState);
                        var recipes = c.LeftMuscle.Current.Components.Concat(c.RightMuscle.Current.Components).Select(p => p.Source)
                            .Concat(feedback.Cast<Relation>().Select(cell => Native.Parts(cell)[1])).ToArray();
                        structures.Add(new { id = c.Slot, generation = c.Generation.Value, parameters,
                            actuatorTerms = c.ComponentCount, feedbackCells = feedback.Length,
                            expressionOperations = ExpressionGraph.Count(c.Features, recipes) });
                        checkpoints[c] = (world.Tick, c.Px, c.Py, c.Energy.Value);
                    }
                    if (world.Tick % 4 == 0 && body.Pilot.CourtshipSignal.Value == 15 &&
                        body.Observations.LeftMate.Value + body.Observations.RightMate.Value > 0) mateCueSamples++;
                    var checkpoint = checkpoints[c];
                    if (world.Tick - checkpoint.Tick >= 160)
                    {
                        progressWindows++;
                        if (c.Energy.Value < 960 && c.Energy.Value < checkpoint.Energy &&
                            Native.Held(body.SpeedSquared) >= 64 && world.ArenaDistance(c.Px,c.Py,checkpoint.X,checkpoint.Y) < 48)
                            lowProgressWindows++;
                        checkpoints[c] = (world.Tick, c.Px, c.Py, c.Energy.Value);
                    }
                    var inquiry = body.Inquiry;
                    if (!jev || !inquiry.Available || !seen.Add(inquiry.Pending!)) continue;
                    var q = inquiry.Pending!; var preparation = world.Resources.Begin();
                    var request = NativeInquiry.Request(q, c);
                    c.Computation.Runtime.Add((ulong)world.Resources.BodyWorkSince(preparation));
                    var bytes = Encoding.UTF8.GetByteCount(request);
                    var reserve = (bytes + 2000) * .000000042;
                    if (requests >= requestLimit || bytes > 20_000 || dollars + reserve > .02)
                    { inquiry.Fail(q, "Trial transport budget exhausted"); continue; }
                    var number = ++requests;
                    await File.WriteAllTextAsync(Path.Combine(output, $"jev-{number:00}-request.json"), request);
                    try
                    {
                        using var content = new StringContent(request, Encoding.UTF8, "application/json");
                        using var response = await http.PostAsync("https://api.typesafe.ai/v1/systemone", content);
                        var reply = await response.Content.ReadAsStringAsync();
                        // These artifacts contain observations and typed replies, never authorization headers.
                        await File.WriteAllTextAsync(Path.Combine(output, $"jev-{number:00}-reply.json"), reply);
                        if (!response.IsSuccessStatusCode) { inquiry.Fail(q, "HTTP " + (int)response.StatusCode); dollars += reserve; }
                        else
                        {
                            using var doc = JsonDocument.Parse(reply);
                            var used = doc.RootElement.GetProperty("usage").GetProperty("input_tokens").GetInt64();
                            inputTokens += used; dollars += used * .000000042;
                            var admissionStart = world.Resources.Begin(); inquiry.Admit(q, reply);
                            c.Computation.Runtime.Add((ulong)world.Resources.BodyWorkSince(admissionStart));
                            Console.WriteLine($"Jev request {number}: model={doc.RootElement.GetProperty("model").GetString()}, input={used}, cumulative=${dollars:F6}");
                        }
                    }
                    catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException)
                    { inquiry.Fail(q, "Transport failed: " + e.GetType().Name); dollars += reserve; }
                }
                if (world.Tick % 200 == 0)
                {
                    samples.Add(new { seconds = world.Seconds, population = world.Creatures.Length,
                        births = world.Births, food = world.TotalFood, injuries = world.Injuries,
                        meanSpeed = world.MeanSpeed, stationary = world.StationaryShare,
                        predictionError = world.Creatures.Length == 0 ? (double?)null :
                            world.Creatures.Average(c => Native.Held(c.Cognition!.Error)) });
                    if (world.Tick % 1200 == 0) Console.WriteLine($"Seed {seed}: {world.Seconds:F0}s, {world.Creatures.Length} alive, {world.Births} births.");
                }
                if (wallLimit > 0 && timer.Elapsed.TotalSeconds > wallLimit) { Console.WriteLine($"Stopped at the {wallLimit}-second wall limit."); break; }
            }
            world.CheckConservation();
            foreach (var body in bodies)
            {
                issued += (long)body.Inquiry.Requests.Value; answers += (long)body.Inquiry.Answers.Value;
                stale += (long)body.Inquiry.Stale.Value; failures += (long)body.Inquiry.Failed.Value;
                trials += (long)body.Inquiry.AdviceTrials.Value;
            }
            timer.Stop();
            var result = new { seed, selection = select, measuredResources = live, eyeLevels, ecology, binocular,
                arenaScale, arenaWidth = world.ArenaWidth, arenaHeight = world.ArenaHeight, population, features,
                requestedSeconds = seconds, completed = world.Seconds >= seconds || world.Creatures.Length == 0,
                setupSeconds = setup.Elapsed.TotalSeconds, simulatedSeconds = world.Seconds,
                wallSeconds = timer.Elapsed.TotalSeconds, alive = world.Creatures.Length, births = world.Births,
                lifetime, food = world.TotalFood, starvation = world.StarvationDeaths, injuries = world.Injuries,
                injuryCalories = world.InjuryCalories, computeCalories = world.ComputeCalories,
                stationary = world.StationaryShare, hungryToward = world.HungryTowardLight, fullAway = world.FullAwayFromLight,
                hungryGradientAlignment = world.HungryGradientAlignment, hungryGradientSamples = world.HungryGradientSamples,
                localFoodShortfall = world.LocalFoodShortfall.Value,
                equalEyes = eyeSamples == 0 ? (double?)null : equalEyeSamples / (double)eyeSamples,
                observedBodies = bodies.Count, inquiryIssued = issued, inquiryAnswers = answers,
                stale, inquiryFailures = failures, adviceTrials = trials,
                predictionChecks = bodies.Sum(b => (long)b.PredictionChecks.Value),
                predictionAbsoluteError = bodies.Sum(b => (long)b.ErrorTotal.Value),
                progressWindows, lowProgressWindows, mateCueSamples,
                progressEscapes = bodies.Sum(b => (long)(b.Pilot.Progress?.Episodes.Value ?? 0)),
                distinctModuleCombinations = genomes.Count, structures,
                bodyUtility = world.Creatures.Select(c => new { id = c.Slot, generation = c.Generation.Value,
                    utility = Native.Held(c.Cognition!.Utility.Returns), visits = c.Cognition.Utility.Visits.Value,
                    parameters = c.Cognition.Pilot.Modules.Select(m => m.Parameters.Select(Native.Held).ToArray()).ToArray() }).ToArray() };
            results.Add(result);
            var name = "seed-" + seed;
            await File.WriteAllTextAsync(Path.Combine(output, name + ".json"), JsonSerializer.Serialize(new { result, samples }, new JsonSerializerOptions { WriteIndented = true }));
            if (archives) await File.WriteAllBytesAsync(Path.Combine(output, name + ".gojo"), GojoNumericFormat.Encode(world.Root));
            Console.WriteLine(JsonSerializer.Serialize(new { seed, world.Seconds, alive = world.Creatures.Length,
                births = world.Births, lifetime, food = world.TotalFood, stationary = world.StationaryShare, issued, answers, trials }));
        }
        await File.WriteAllTextAsync(Path.Combine(output, "summary.json"), JsonSerializer.Serialize(new { results,
            nativeAssemblyVersion = typeof(Bath).Assembly.ManifestModule.ModuleVersionId,
            jev = new { requests, inputTokens, dollars, maximumRequests = requestLimit, maximumDollars = .02 } }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
