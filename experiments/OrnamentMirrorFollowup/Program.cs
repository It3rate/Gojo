using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Gojo.Experiments.PrimitiveMeasure;
using Gojo.Experiments.PrimitiveMeasure.OrnamentCampaign;

namespace Gojo.Experiments.OrnamentMirrorFollowup;

internal static class Program
{
    private const string Original = "0104C2004900244374";
    private const string ParentHash = "1320D2322AB10060B67E879F975C51A3390CCB1DCB4D1DB97E1423018EA1FBAC";
    private const string Project = "experiments/OrnamentMirrorFollowup";
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    private static readonly string[] Sources = ["current", "bound", "wrong"];
    private sealed record Seal(string Parent, string Protocol, string Runner, string Kernel, string Source);
    private sealed record Candidate(int Id, string Hex, long Loss, long Attempts);
    private sealed record Winner(string Source, Candidate Training, Candidate Validation);
    private sealed record WinnerLock(string ProtocolSeal, string WinnerFile, int Winners);
    private sealed record Case(string Group, string Family, int Variant, bool Authorized, int P, int Q,
        RailInput Common, ulong AuthorizedSource, ulong Bound, ulong Wrong, string CorrectBinding, string WrongBinding)
    {
        public ulong EvaluationTarget => Common.Initial | Reflect(AuthorizedSource, Common.Width);
    }
    private readonly record struct Outcome(RailResult Physical, int Extra, int Missed, int Lost, bool Partner, bool Correct)
    {
        public int Errors => Extra + Missed;
    }

    private static int Main(string[] args)
    {
        if (args is ["execute", var input, var output])
        {
            var requests = Load<Replay[]>(input);
            var answers = requests.Select(r => OrnamentProgram.Decode(Convert.FromHexString(r.Hex)).Compile().Run(r.Input, r.Wire)).ToArray();
            Save(output, answers);
            Console.WriteLine($"Executed {answers.Length} native replays.");
            return 0;
        }
        if (args.Length != 2 || args[0] is not ("freeze" or "train" or "verify" or "assay"))
        {
            Console.Error.WriteLine("Usage: OrnamentMirrorFollowup freeze|train|verify|assay RUN_DIRECTORY");
            return 2;
        }
        string dir = Path.GetFullPath(args[1]);
        if (args[0] == "freeze") { Freeze(dir); return 0; }
        Verify(dir);
        if (args[0] == "train") Train(dir);
        if (args[0] == "assay") Assay(dir);
        if (args[0] == "verify" && File.Exists(Path.Combine(dir, "winner-lock.json"))) _ = LoadWinners(dir);
        return 0;
    }

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    private static string FileHash(string path) => Hash(File.ReadAllBytes(path));
    private static void Save<T>(string path, T item) => Write(path, JsonSerializer.Serialize(item, Json) + "\n");
    private static void Write(string path, string content)
    {
        if (File.Exists(path))
        {
            if (File.ReadAllText(path) != content) throw new IOException("Existing artifact differs: " + path);
            return;
        }
        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(temp, content, new UTF8Encoding(false)); File.Move(temp, path); }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
    private static T Load<T>(string file) => JsonSerializer.Deserialize<T>(File.ReadAllText(file))!;
    private static string Row(params object[] fields) => string.Join(',', fields.Select(field =>
        "\"" + (Convert.ToString(field, CultureInfo.InvariantCulture) ?? "").Replace("\"", "\"\"") + "\"")) + "\n";
    private static Seal Identity() => new(FileHash("artifacts/local-ornament/primary/Report.md"),
        FileHash(Path.Combine(Project, "Protocol.md")), FileHash(typeof(Program).Assembly.Location),
        FileHash(typeof(OrnamentProgram).Assembly.Location), FileHash(Path.Combine(Project, "Program.cs")));
    private static void Freeze(string dir)
    {
        if (Directory.Exists(dir) && Directory.EnumerateFileSystemEntries(dir).Any())
            throw new IOException("Freeze requires an empty directory.");
        var seal = Identity();
        if (seal.Parent != ParentHash) throw new InvalidOperationException("Primary report identity mismatch.");
        Directory.CreateDirectory(dir);
        File.Copy(Path.Combine(Project, "Protocol.md"), Path.Combine(dir, "Protocol.md"));
        Save(Path.Combine(dir, "protocol-lock.json"), seal);
        Console.WriteLine($"Frozen {dir}; protocol {seal.Protocol}, seal {FileHash(Path.Combine(dir, "protocol-lock.json"))}.");
    }
    private static void Verify(string dir)
    {
        var current = Identity();
        var saved = Load<Seal>(Path.Combine(dir, "protocol-lock.json"));
        if (current != saved || saved.Parent != ParentHash || FileHash(Path.Combine(dir, "Protocol.md")) != saved.Protocol)
            throw new InvalidOperationException("Frozen source, executable, protocol, or primary result changed.");
    }

    private static ulong Reflect(ulong value, int width)
    {
        ulong result = 0;
        for (; value != 0; value &= value - 1)
            result |= 1UL << (width - 1 - BitOperations.TrailingZeroCount(value));
        return result;
    }
    private static uint GroupHash(string group)
    {
        uint hash = 2166136261U;
        foreach (byte b in Encoding.UTF8.GetBytes(group)) hash = unchecked((hash ^ b) * 16777619U);
        return hash;
    }

    // The supplier publishes its OWN occupied sites. Sink-local lookup rows
    // bind each site to a source port. We materialize the port reads into a
    // single native rail input for efficient repeated execution. In particular
    // neither the sensed occupancy nor the rows contain the desired final ink.
    private static (ulong Read, string RowHash) Bind(ulong authorization, int width, bool shifted)
    {
        var owner = new PortStructure();
        int bits = OrderedBoundary.Bits(width);
        var rows = new EndpointRows();
        var ports = new int[width];
        for (int i = 0; i < width; i++)
            ports[i] = owner.Publish(owner.Create(0, 0, 1, (authorization >> i) & 1));
        ulong read = 0;
        for (int sink = 0; sink < width; sink++)
        {
            int peer = (width - 1 - sink + (shifted ? 1 : 0)) % width;
            var row = rows.Append(bits, sink, ports[peer]);
            var pair = rows.Read(row);
            byte support = owner.Connect(pair[1]).ReadSupport();
            if (!ReadSupport.TrySingle(support, out byte word)) throw new InvalidOperationException("Unexpected unbound source.");
            read |= (ulong)(word & 1) << pair[0];
        }
        return (read, Hash(rows.PackedBytes(2, bits)));
    }
    private static Case[] Cases(string split)
    {
        var result = new List<Case>();
        foreach (var e in CampaignScience.Episodes(split, "mirror"))
        {
            var input = e.Input;
            int p = (int)(GroupHash(e.Group) % (uint)(input.Width / 2));
            if (e.Variant == 1) p = input.Width - p - 1;
            int q = input.Width - p - 1;
            ulong bp = 1UL << p;
            ulong s = input.Initial & ~(bp | (1UL << q));
            ulong initial = s | bp;
            foreach (bool authorized in new[] { false, true })
            {
                ulong source = authorized ? initial : s;
                var bound = Bind(source, input.Width, false);
                var wrong = Bind(source, input.Width, true);
                result.Add(new(e.Group, e.Family, e.Variant, authorized, p, q,
                    input with { Initial = initial, Protected = initial, Hidden = 0,
                        InterventionBeat = -1, InterventionXor = 0, Beats = input.Width, Seed = 0 },
                    source, bound.Read, wrong.Read, bound.RowHash, wrong.RowHash));
            }
        }
        return result.ToArray();
    }
    private static RailInput Input(Case e, string source, bool ample) => e.Common with
    {
        Seed = source switch { "current" => 0, "bound" => e.Bound, "wrong" => e.Wrong, _ => throw new ArgumentException() },
        Stock = ample ? e.Common.Width : e.Common.Stock,
        EditBudget = ample ? e.Common.Width : e.Common.EditBudget
    };
    private static OrnamentProgram Genome(int id, string source) => new(
        [new((byte)(source == "current" ? 9 : 10), 2, (byte)(id & 15))],
        [new(2, 6, (byte)(id >> 4), 0, 1)]);
    private static OrnamentProgram Fixed(string source)
    {
        var original = OrnamentProgram.Decode(Convert.FromHexString(Original));
        var sensors = original.Sensor.ToArray();
        if (source != "current") sensors[0] = sensors[0] with { A = 10 };
        return new(sensors, original.Writer.ToArray(), original.Flags);
    }
    private static Outcome Run(OrnamentMachine machine, Case world, string source, bool ample, WireMode wire = WireMode.Live)
    {
        var input = Input(world, source, ample);
        var result = machine.Run(input, wire);
        int extra = BitOperations.PopCount(result.Ink & ~world.EvaluationTarget);
        int missed = BitOperations.PopCount(world.EvaluationTarget & ~result.Ink);
        int lost = BitOperations.PopCount(input.Initial & ~result.Ink);
        if (lost != 0 || BitOperations.PopCount(input.Initial) + input.Stock != BitOperations.PopCount(result.Ink) + result.Stock
            || result.Attempts > input.EditBudget || result.Edits > result.Attempts)
            throw new InvalidOperationException("Material invariant failed.");
        bool partner = (result.Ink & (1UL << world.Q)) != 0;
        return new(result, extra, missed, lost, partner, partner == world.Authorized);
    }
    private static Candidate Score(Case[] cases, string arm, int id)
    {
        var graph = Genome(id, arm);
        var machine = graph.Compile();
        long loss = 0, attempts = 0;
        foreach (var e in cases)
        {
            var observed = Run(machine, e, arm, true);
            loss += observed.Errors;
            attempts += observed.Physical.Attempts;
        }
        return new(id, Convert.ToHexString(graph.Encode()), loss, attempts);
    }
    private static Candidate[] Rank(IEnumerable<Candidate> rows) => rows.OrderBy(c => c.Loss)
        .ThenBy(c => c.Attempts).ThenBy(c => c.Id).ToArray();
    private static void Train(string dir)
    {
        if (File.Exists(Path.Combine(dir, "winner-lock.json"))) { _ = LoadWinners(dir); return; }
        var timer = Stopwatch.StartNew();
        var train = Cases("train");
        var validation = Cases("validation");
        var winners = new List<Winner>();
        var rows = new StringBuilder("arm,partition,candidate,genome_hex,error_sites,attempts,worlds\n");
        foreach (var arm in Sources)
        {
            var scores = new Candidate[256];
            Parallel.For(0, scores.Length, new ParallelOptions { MaxDegreeOfParallelism = 4 },
                id => scores[id] = Score(train, arm, id));
            foreach (var x in scores) rows.Append(Row(arm, "train", x.Id, x.Hex, x.Loss, x.Attempts, train.Length));
            var best = Rank(scores).Take(8).Select(c => Score(validation, arm, c.Id)).ToArray();
            foreach (var x in best) rows.Append(Row(arm, "validation", x.Id, x.Hex, x.Loss, x.Attempts, validation.Length));
            var selected = Rank(best)[0];
            winners.Add(new(arm, scores[selected.Id], selected));
        }
        Write(Path.Combine(dir, "candidate-scores.csv"), rows.ToString());
        var winnerFile = Path.Combine(dir, "winners.json");
        Save(winnerFile, winners);
        var locked = new WinnerLock(FileHash(Path.Combine(dir,"protocol-lock.json")), FileHash(winnerFile), winners.Count);
        Save(Path.Combine(dir,"train-timing.json"), new { TrainWorlds = train.Length, ValidationWorlds = validation.Length,
            CandidateGraphs = 3 * 256, NativeTrainExecutions = 3 * 256 * train.Length,
            NativeValidationExecutions = 3 * 8 * validation.Length, ElapsedMilliseconds = timer.ElapsedMilliseconds });
        Save(Path.Combine(dir,"winner-lock.json"), locked);
        Console.WriteLine($"Locked {winners.Count} winners in {timer.Elapsed}; winner seal {FileHash(Path.Combine(dir, "winner-lock.json"))}.");
    }
    private static Winner[] LoadWinners(string dir)
    {
        var seal = Load<WinnerLock>(Path.Combine(dir, "winner-lock.json"));
        var file = Path.Combine(dir, "winners.json");
        if (seal.ProtocolSeal != FileHash(Path.Combine(dir, "protocol-lock.json")) || seal.WinnerFile != FileHash(file) || seal.Winners != 3)
            throw new InvalidOperationException("Winner lock changed.");
        return Load<Winner[]>(file);
    }

    private sealed record Arm(string Name, string Hex, string Source, WireMode Wire);
    private sealed record RowResult(Arm Arm, Case Case, bool Ample, Outcome Observation);
    private sealed record Replay(string Hex, RailInput Input, WireMode Wire);
    private sealed record Decision(string Id, string Status, int Positive, int Contrary, int Groups, double MeanDifference);

    private static void Assay(string dir)
    {
        var winners = LoadWinners(dir);
        var views = Cases("reserve");
        string HexFor(string source) => winners.Single(w => w.Source == source).Training.Hex;
        var arms = Sources.Select(s => new Arm("learned-" + s, HexFor(s), s, WireMode.Live)).ToList();
        arms.Add(new("bound-rebound-wrong",HexFor("bound"),"wrong",WireMode.Live));
        arms.Add(new("bound-cut",HexFor("bound"),"bound",WireMode.Cut));
        arms.AddRange(Sources.Select(s => new Arm("fixed-" + s, Convert.ToHexString(Fixed(s).Encode()), s, WireMode.Live)));
        arms.Add(new("idle",Convert.ToHexString(new OrnamentProgram([new(0,0,0)],[new(0,0,0,0,0)]).Encode()),
            "current",WireMode.Live));

        var timer = Stopwatch.StartNew();
        var all = new List<RowResult>(arms.Count * views.Length * 2);
        var replays = new List<Replay>();
        var expected = new List<RailResult>();
        var csv = new StringBuilder("arm,genome,source,wire,capacity,parent,family,view,authorized,p,q,width,initial_hex,authorized_source_hex,binding_sha256,published_source_hex,evaluator_target_hex,final_hex,initial_stock,final_stock,budget,attempts,edits,extra_propagated,missed_required,initial_lost,q_occupied,partner_decision_correct,exact,trace_hash\n");
        foreach (var arm in arms)
        {
            var compiled = OrnamentProgram.Decode(Convert.FromHexString(arm.Hex)).Compile();
            foreach (var e in views)
            foreach (var ample in new[] { true, false })
            {
                var observation = Run(compiled, e, arm.Source, ample, arm.Wire);
                var input = Input(e, arm.Source, ample);
                var result = observation.Physical;
                all.Add(new(arm, e, ample, observation));
                csv.Append(Row(arm.Name, arm.Hex, arm.Source, arm.Wire, ample ? "ample" : "scarce", e.Group, e.Family,
                    e.Variant, e.Authorized, e.P, e.Q, input.Width, input.Initial.ToString("X"), e.AuthorizedSource.ToString("X"),
                    arm.Source == "current" ? "native sensor 9" : arm.Source == "wrong" ? e.WrongBinding : e.CorrectBinding,
                    input.Seed.ToString("X"), e.EvaluationTarget.ToString("X"), result.Ink.ToString("X"), input.Stock,
                    result.Stock, input.EditBudget, result.Attempts, result.Edits, observation.Extra, observation.Missed,
                    observation.Lost, observation.Partner, observation.Correct, observation.Errors == 0, result.TraceHash.ToString("X16")));
                // Include both twins per arm and resource regime in a cold executable bundle.
                if (e.Group == views[0].Group && e.Variant == 0)
                {
                    replays.Add(new(arm.Hex, input, arm.Wire));
                    expected.Add(result);
                }
            }
        }

        var summary = new StringBuilder("arm,capacity,worlds,exact,correct_partner,extra_sites,missed_sites,lost_initial,mean_edits,mean_attempts\n");
        foreach (var group in all.GroupBy(r => (r.Arm.Name, r.Ample)))
            summary.Append(Row(group.Key.Name, group.Key.Ample ? "ample" : "scarce", group.Count(),
                group.Count(r => r.Observation.Errors == 0), group.Count(r => r.Observation.Correct),
                group.Sum(r => r.Observation.Extra), group.Sum(r => r.Observation.Missed), group.Sum(r => r.Observation.Lost),
                group.Average(r => r.Observation.Physical.Edits), group.Average(r => r.Observation.Physical.Attempts)));
        var groups = new StringBuilder("question,parent,left_mean,right_mean,difference,margin\n");
        var results = new List<Decision>();
        var ampleRows = all.Where(r => r.Ample).ToArray();
        Pair("M1", "learned-bound", "learned-current", .25, x => x.Correct ? 1 : 0);
        Pair("M2", "learned-bound", "bound-rebound-wrong", .25, x => x.Correct ? 1 : 0);
        Pair("M3", "learned-bound", "bound-cut", .25, x => x.Correct ? 1 : 0);
        Pair("M4", "learned-bound", "learned-current", .5, x => -x.Errors);
        var positives = ampleRows.Where(r => r.Arm.Name == "fixed-bound").GroupBy(r => r.Case.Group)
            .Select(g => g.All(r => r.Observation.Errors == 0 && r.Observation.Lost == 0)).ToArray();
        Exact("M5", positives);
        var twins = ampleRows.Where(r => r.Arm.Name == "learned-current").GroupBy(r => r.Case.Group)
            .Select(g => g.GroupBy(r => r.Case.Variant).All(v => v.Count() == 2 &&
                v.Select(r => (r.Observation.Physical.Ink, r.Observation.Partner)).Distinct().Count() == 1)).ToArray();
        Exact("M6", twins);
        var outDir = Path.Combine(dir, "reserve"); Directory.CreateDirectory(outDir);
        Write(Path.Combine(outDir,"cases.csv"), csv.ToString());
        Write(Path.Combine(outDir,"summary.csv"), summary.ToString());
        Write(Path.Combine(outDir,"groups.csv"), groups.ToString());
        Save(Path.Combine(outDir,"verdicts.json"), results);
        Save(Path.Combine(outDir,"replay-inputs.json"), replays);
        Save(Path.Combine(outDir,"replay-expected.json"), expected);
        var files = new[] { "cases.csv","summary.csv","groups.csv","verdicts.json","replay-inputs.json","replay-expected.json" };
        var completionFile = Path.Combine(outDir,"complete.json");
        if (!File.Exists(completionFile))
            Save(completionFile, new { WinnerSeal = FileHash(Path.Combine(dir,"winner-lock.json")), Executions = all.Count,
                ResourceSchedules = 2, Arms = arms.Count, ElapsedMilliseconds = timer.ElapsedMilliseconds,
                FileHashes = files.ToDictionary(n => n, n => FileHash(Path.Combine(outDir,n))) });
        Console.WriteLine($"Assayed {all.Count} locked local world executions. {results.Count} prespecified assessments.");

        void Pair(string id, string leftArm, string rightArm, double margin, Func<Outcome,double> score)
        {
            var left = ampleRows.Where(r => r.Arm.Name == leftArm).GroupBy(r => r.Case.Group);
            var right = ampleRows.Where(r => r.Arm.Name == rightArm).GroupBy(r => r.Case.Group)
                .ToDictionary(g => g.Key, g => g.Average(r => score(r.Observation)));
            var differences = new List<double>();
            foreach (var g in left)
            {
                double a = g.Average(r => score(r.Observation)), b = right[g.Key];
                differences.Add(a - b); groups.Append(Row(id, g.Key, a, b, a - b, margin));
            }
            int pos = differences.Count(d => d >= margin), neg = differences.Count(d => d <= -margin), count = differences.Count;
            results.Add(new(id, pos * 3 >= count * 2 ? "supported" : neg * 3 >= count * 2 ? "contrary" : "inconclusive",
                pos, neg, count, differences.Average()));
        }
        void Exact(string id, bool[] checks) => results.Add(new(id, checks.All(ok => ok) ? "supported" : "contrary",
            checks.Count(ok => ok), checks.Count(ok => !ok), checks.Length, checks.Count(ok => ok) / (double)checks.Length));
    }
}
