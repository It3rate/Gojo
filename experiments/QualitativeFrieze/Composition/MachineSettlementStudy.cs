using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Supplied experiment apparatus around local native response laws.
/// Targets and diagnostics stay outside the actuator; only the selected public
/// comparison reaches it. No search over image coordinates selects a move.</summary>
public static class MachineSettlementStudy
{
    public sealed record Scenario(string Id, string Name, string Note);
    public sealed record State(long Value, long Tension, long Phase);
    public sealed record Drawing(int[][] Upper, int[][] Lower, int[][] TargetUpper,
        int[][] TargetLower, int AcceptedSeams, int BlockedSeams, int ZeroSections);
    public sealed record Frame(int Beat, int GoalWidth, int GoalHeight, State[] Owners,
        int[] Supports, long?[] Requests, long[] Accepted, bool[] Resolved,
        bool[] Blocked, int AgreementError, int GoalError, Drawing? Drawing = null)
    {
        [JsonIgnore] public Relation? PublicSamples { get; init; }
        [JsonIgnore] public Relation? PublicationWitness { get; init; }
        [JsonIgnore] public Relation? NativeDrawingSurface { get; init; }
    }
    public sealed record Run(string Id, string Family, string Mode, string Scenario,
        int Seed, Frame[] Frames, bool GoalReached, bool PairAgrees, bool Quiet,
        int Changes, int Missing, int Ambiguous, int Blocked, long FinalTension,
        int GraphBytes, double Milliseconds)
    {
        [JsonIgnore] public Relation? NativeSurface { get; init; }
        public bool JointGoalsMet => GoalReached && PairAgrees && FinalTension == 0;
    }
    public sealed record Summary(string Id, string Family, string Mode, string Scenario,
        int Seed, bool GoalReached, bool PairAgrees, bool Quiet, int AgreementError,
        int GoalError, int Changes, int Missing, int Ambiguous, int Blocked,
        long FinalTension, int GraphBytes, double Milliseconds)
    {
        public bool JointGoalsMet => GoalReached && PairAgrees && FinalTension == 0;
    }

    public static readonly Scenario[] Scenarios =
    [
        new("stretch", "Stretch the opening", "The desired flat extent changes from two to six shared steps at beat four."),
        new("diamond", "Shrink the flat extent to zero", "The desired flat extent becomes zero. Zero-length phases and their separate sites remain present."),
        new("taller", "Increase the separation", "The desired vertical reach changes from two to four shared steps."),
        new("both", "Two pressures together", "The flat extent and the vertical reach both increase. Separate exposed roles carry the two requests."),
        new("reverse", "Change the goal again", "The requested flat extent increases to six, then falls to one at beat twelve."),
        new("missing", "Temporarily missing signal", "The follower's comparisons are absent on beats four through nine, then resume."),
        new("ambiguous", "Temporarily ambiguous signal", "The follower admits all three comparison outcomes on beats four through nine. These are alternatives, not three commands."),
        new("hidden", "Signal never returns", "The follower receives no comparisons after beat three. An evaluator can still inspect its actual state, but that state does not repair the missing channel."),
        new("delay", "Three-beat-old comparison", "The follower receives each comparison three beats late. Its sign was correct when observed; it may be stale now."),
        new("capacity", "The receiver cannot stretch far enough", "The follower's flat-extent capacity is three while the desired extent becomes six. Each admitted request remains accounted for."),
        new("conflict", "Two incompatible desired extents", "On alternate beats, a second supplied goal asks the follower for flat extent one. The other beats ask it to agree with the first pen."),
        new("release", "A smaller goal after blocked growth", "The follower first accumulates blocked growth at capacity three. At beat twelve the desired extent becomes one; old tension is not erased.")
    ];
    public static readonly string[] Families = ["chevron", "cardinal", "inset"];
    public static readonly string[] Modes = ["independent", "clock", "feedback"];

    // The selected publication uses the receiver's ordered comparison domain.
    // The sensor's scalar inputs are explicitly advertised sampled values.
    // It publishes direction only, never the size of the target error.
    public static byte Compare(long current, long target) => current > target ? (byte)1 :
        current == target ? (byte)2 : (byte)4;

    public static Run Execute(string family, string mode, string scenario, int seed,
        bool compiled = true, int beats = 28)
    {
        if (!Families.Contains(family) || !Modes.Contains(mode) || !Scenarios.Any(s => s.Id == scenario) || seed is < 0 or > 2)
            throw new ArgumentException("Unknown member of the declared experiment family.");
        var watch = Stopwatch.StartNew();
        var unit = new Carrier().UnitShape;
        var starts = new[] { (2, 5), (0, 3), (5, 1) }[seed];
        var followerPolicy = mode == "independent" ? "hold" : mode;
        var capacity = scenario is "capacity" or "release" ? 3 : 8;
        var machines = new[] {
            CoupledExtentMachine.Create(starts.Item1, 8, unit: unit),
            CoupledExtentMachine.Create(Math.Min(starts.Item2, capacity), capacity, followerPolicy, unit: unit),
            CoupledExtentMachine.Create(2, 6, unit: unit),
            CoupledExtentMachine.Create(seed == 2 ? 3 : 1, 6, followerPolicy, unit: unit)
        };
        var observationBasis = new Carrier(unit);
        var goalWidth = new Carrier(unit); goalWidth.Add(2);
        var goalHeight = new Carrier(unit); goalHeight.Add(2);
        var alternateGoal = new Carrier(unit); alternateGoal.Add(1);
        Relation Port(Carrier c) => new(c.ValueSlot, c);
        var links = new[] {
            CoupledComparisonLink.Create(Port(goalWidth), machines[0]),
            CoupledComparisonLink.Create(machines[0].ExtentPort, machines[1]),
            CoupledComparisonLink.Create(Port(goalHeight), machines[2]),
            CoupledComparisonLink.Create(machines[2].ExtentPort, machines[3])
        };
        var conflictingLink = CoupledComparisonLink.Create(Port(alternateGoal), machines[1]);
        var composition = StructuralConstraint.Closed(
            StructuralConstraint.Closed(machines.Select(m => (IElement)m.Surface).ToArray()),
            StructuralConstraint.Closed(links.Select(l => (IElement)l.Surface).ToArray()), conflictingLink.Surface,
            new Relation(machines[0].Owner, machines[2].Owner), new Relation(machines[1].Owner, machines[3].Owner));
        var queue = new Queue<(Relation[] Packets, int[] Masks)>();
        var frames = new List<Frame>();
        var sensorHistory = new List<IElement>();
        var changed = 0; var missing = 0; var ambiguous = 0; var blocked = 0;
        frames.Add(Read(0, 2, 2, [0, 0, 0, 0], [null, null, null, null], [0, 0, 0, 0], [false, false, false, false], [false, false, false, false]));
        for (var beat = 1; beat <= beats; beat++)
        {
            var (width, height) = Goal(scenario, beat);
            goalWidth.Set(width); goalHeight.Set(height);
            var absent = scenario == "hidden" && beat >= 4 || scenario == "missing" && beat is >= 4 and <= 9;
            var ambiguousNow = scenario == "ambiguous" && beat is >= 4 and <= 9;
            // Observe all advertised ports before any response is admitted.
            // The private sensor witness has the samples; its transmitted packet
            // has only the comparison support and an opaque observation identity.
            var observations = links.Select((link, i) =>
                (scenario == "conflict" && i == 1 && beat >= 4 && beat % 2 == 0 ? conflictingLink : link)
                .Observe(available: i % 2 == 0 || !absent, ambiguous: i % 2 == 1 && ambiguousNow)).ToArray();
            var supports = observations.Select(o => (int)o.Mask).ToArray();
            var publications = observations.Select(o => o.Packet).ToArray();
            sensorHistory.AddRange(observations.Select(o => (IElement)o.Witness));
            if (scenario == "delay")
            {
                queue.Enqueue(([publications[1], publications[3]], [supports[1], supports[3]]));
                if (queue.Count > 3)
                {
                    var old = queue.Dequeue(); publications[1] = old.Packets[0]; publications[3] = old.Packets[1];
                    supports[1] = old.Masks[0]; supports[3] = old.Masks[1];
                }
                else
                {
                    supports[1] = supports[3] = 0;
                    publications[1] = machines[1].Publication(0);
                    publications[3] = machines[3].Publication(0);
                }
            }
            var steps = machines.Select((m, i) => m.Step(publications[i], compiled: compiled)).ToArray();
            changed += steps.Count(s => s.Changed); missing += supports.Count(s => s == 0);
            ambiguous += supports.Count(s => s == 7); blocked += steps.Count(s => s.Blocked);
            frames.Add(Read(beat, width, height, supports, steps.Select(s => s.RequestedDelta).ToArray(),
                steps.Select(s => s.AcceptedDelta).ToArray(), steps.Select(s => s.Resolved).ToArray(), steps.Select(s => s.Blocked).ToArray()));
        }
        var last = frames[^1];
        var bytes = GojoNumericFormat.Encode(composition).Length;
        watch.Stop();
        return new($"{family}-{scenario}-{mode}-{seed}", family, mode, scenario, seed, frames.ToArray(),
            last.GoalError == 0, last.AgreementError == 0,
            frames.TakeLast(4).All(f => f.Accepted.All(a => a == 0)) && last.Owners.All(s => s.Tension == 0),
            changed, missing, ambiguous, blocked, last.Owners.Sum(s => Math.Abs(s.Tension)), bytes, watch.Elapsed.TotalMilliseconds) {
            NativeSurface = StructuralConstraint.Closed(composition,
                StructuralConstraint.Closed(machines.Select(m => (IElement)m.ArchiveSurface()).ToArray()),
                StructuralConstraint.Closed(sensorHistory.ToArray()),
                StructuralConstraint.Closed(frames.Select(f => (IElement)f.PublicationWitness!).ToArray()))
        };

        Frame Read(int beat, int w, int h, int[] supports, long?[] requests, long[] accepted, bool[] resolved, bool[] rejected)
        {
            var snapshots = machines.Select(m => m.Snapshot()).ToArray();
            var s = snapshots.Select(s => new State((long)s.Value, s.Tension, (long)s.Phase)).ToArray();
            var samples = StructuralConstraint.Closed(s.Select(v => (IElement)new CarrierLocation(observationBasis, v.Value)).ToArray());
            return new(beat, w, h, s, supports, requests, accepted, resolved, rejected,
                (int)(Math.Abs(s[0].Value - s[1].Value) + Math.Abs(s[2].Value - s[3].Value)),
                (int)(Math.Abs(s[0].Value - w) + Math.Abs(s[2].Value - h))) {
                PublicSamples = samples,
                PublicationWitness = new Relation(new Relation(composition, StructuralConstraint.Closed(snapshots.Select(v => (IElement)v.Surface).ToArray())), samples)
            };
        }
    }

    public static (int Width, int Height) Goal(string scenario, int beat)
    {
        if (beat < 4) return (2, 2);
        if (scenario == "diamond") return (0, 2);
        if (scenario == "taller") return (2, 4);
        if (scenario == "both") return (6, 4);
        if (scenario is "reverse" or "release" && beat >= 12) return (1, 2);
        return (6, 2);
    }

    public static Summary Summarize(Run r) => new(r.Id, r.Family, r.Mode, r.Scenario, r.Seed,
        r.GoalReached, r.PairAgrees, r.Quiet, r.Frames[^1].AgreementError, r.Frames[^1].GoalError,
        r.Changes, r.Missing, r.Ambiguous, r.Blocked, r.FinalTension, r.GraphBytes, r.Milliseconds);

    public static CoupledShapeTemplate.Template Template(string family) => family switch {
        "chevron" => CoupledShapeTemplate.ChevronCap(), "cardinal" => CoupledShapeTemplate.CardinalCap(),
        "inset" => CoupledShapeTemplate.InsetCap(), _ => throw new ArgumentException("Unknown authored seed.") };

    public static Run WithDrawings(Run run)
    {
        var template = Template(run.Family);
        var frames = run.Frames.Select(frame =>
        {
            var values = frame.Owners.Select(o => checked((int)o.Value)).ToArray();
            var current = CoupledShapeTemplate.Realize(template, values[0], values[1], values[2], values[3], frame.PublicSamples);
            var target = CoupledShapeTemplate.Realize(template, frame.GoalWidth, frame.GoalWidth, frame.GoalHeight, frame.GoalHeight);
            var seam = CoupledShapeTemplate.DeclareSeams(current);
            var drawing = new Drawing(current.Upper.Points, current.Lower.Points, target.Upper.Points, target.Lower.Points,
                Gojo.Experiments.QualitativeExecution.FiniteLaw.Entries(seam.Accepted).Length,
                Gojo.Experiments.QualitativeExecution.FiniteLaw.Entries(seam.Blocked).Length,
                current.Upper.ZeroSections + current.Lower.ZeroSections);
            return frame with { Drawing = drawing, NativeDrawingSurface = new Relation(frame.PublicationWitness!,
                new Relation(current.Surface, seam.Surface)) };
        }).ToArray();
        return run with { Frames = frames, NativeSurface = new Relation(run.NativeSurface!,
            StructuralConstraint.Closed(frames.Select(f => (IElement)f.NativeDrawingSurface!).ToArray())) };
    }

    public static int RunExperiment(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "machine-settlement");
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else throw new ArgumentException("Usage: --machine-settlement [--output DIRECTORY]");
        Directory.CreateDirectory(output);
        var runs = (from family in Families from scenario in Scenarios from mode in Modes from seed in Enumerable.Range(0, 3)
                    select Execute(family, mode, scenario.Id, seed)).ToArray();
        var selected = runs.Where(r => r.Seed == 0).ToArray();
        var agreementChecks = new List<object>();
        foreach (var r in selected.Where(r => r.Family == "chevron"))
        {
            var interpreted = Execute(r.Family, r.Mode, r.Scenario, r.Seed, compiled: false);
            var agrees = JsonSerializer.Serialize(r.Frames) == JsonSerializer.Serialize(interpreted.Frames);
            agreementChecks.Add(new { r.Id, Agrees = agrees });
            if (!agrees) throw new InvalidOperationException("Native/compiled law disagreement: " + r.Id);
        }
        var retention = ExtentRouteRetention.Report();
        var detailed = selected.Select(WithDrawings).ToArray();
        var archives = new[] { "stretch", "capacity" }.Select(scenario => {
            var exemplar = detailed.Single(r => r.Family == "chevron" && r.Scenario == scenario && r.Mode == "feedback" && r.Seed == 0);
            var payload = GojoNumericFormat.Encode(exemplar.NativeSurface!);
            File.WriteAllBytes(Path.Combine(output, scenario + ".gojo"), payload);
            return new { File = scenario + ".gojo", Bytes = payload.Length };
        }).ToArray();
        var report = new {
            Schema = "gojo.machine-settlement.v1", Scenarios, Families, Modes,
            Scope = "Authored native comparison/response machines and paired drawing templates. Bounded whole-step extent correction; no image fitting, discovered topology or universal geometric settlement.",
            ControllerConditions = runs.Length / Families.Length, DrawingCases = runs.Length,
            Runs = runs.Select(Summarize), Detailed = detailed, AgreementChecks = agreementChecks,
            Retention = retention, Archives = archives,
            Guidance = new[] { "Seed phases and mirrored-frame construction", "Shared step unit and finite capacities", "Desired extent inputs and sensor contracts", "Hold, clock and feedback law candidates", "Disturbance schedule and independent evaluation" }
        };
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var json = JsonSerializer.Serialize(report, options);
        File.WriteAllText(Path.Combine(output, "study-data.json"), json);
        var view = Path.Combine("experiments", "QualitativeFrieze", "MachineSettlementView.html");
        if (File.Exists(view)) File.WriteAllText(Path.Combine(output, "index.html"), File.ReadAllText(view).Replace("__STUDY_DATA__", json));
        Console.WriteLine($"Recorded {runs.Length} controlled runs and {agreementChecks.Count} native/compiled comparisons at {Path.GetFullPath(output)}");
        return 0;
    }
}
