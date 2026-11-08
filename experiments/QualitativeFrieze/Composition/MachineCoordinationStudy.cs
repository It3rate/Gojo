using System.Text.Json;
using System.Text.Json.Serialization;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Bounded supplied apparatus for four local mechanisms. Goals,
/// delivery times and diagnostics belong here; native connected laws choose
/// responses. Drawing reads never feed a scalar target into an actuator.</summary>
public static class MachineCoordinationStudy
{
    public sealed record Frame(int Beat, long Value, long Tension, long Goal, long? OtherGoal,
        int InputMask, int OutputMask, long? Request, long Accepted, bool Consumed,
        bool Waiting, bool Acknowledged, bool Suppressed, bool Conflict, long? Gap,
        long? GapGoal, int[][] Upper, int[][] Lower, bool EndCoincident, string Note);
    public sealed record Run(string Id, string Stage, string Scenario, string Mode, string Family,
        int Seed, Frame[] Frames, bool GoalReached, bool? OtherGoalReached, long FinalTension,
        int Unresolved, int Requests, int Suppressed, int Conflicts, bool Waiting, int LiveBytes)
    {
        [JsonIgnore] public Relation? NativeSurface { get; init; }
    }

    public static Run Execute(string stage, string scenario, string mode, int seed = 0,
        string family = "chevron", bool compiled = true, bool captureNative = false) => stage switch
        {
            "pending" => Pending(scenario, mode, seed, compiled, captureNative),
            "channel" => Channel(scenario, mode, seed, compiled, captureNative),
            "arbitration" => Arbitration(scenario, mode, seed, compiled, captureNative),
            "gap" => Gap(scenario, mode, seed, family, compiled, captureNative),
            _ => throw new ArgumentException("Unknown supplied study stage.", nameof(stage))
        };

    private static Run Pending(string scenario, string mode, int seed, bool compiled, bool capture)
    {
        Check(scenario, ["capacity", "release", "hidden-pending", "missing-signal"]);
        Check(mode, ["repeat", "pending-gate"]); CheckSeed(seed, 3);
        var machine = CoupledExtentMachine.Create(new[] { 0, 2, 3 }[seed], scenario == "missing-signal" ? 8 : 3);
        var goal = Goal(machine, 6); var link = CoupledComparisonLink.Create(Port(goal), machine);
        var gate = PendingAdjustmentGate.Create(machine);
        var trace = new Trace("pending", scenario, mode, "chevron", seed, machine, capture);
        trace.GoalPort = Port(goal);
        trace.Components.AddRange([Port(goal), link.Surface, gate.Surface]);
        trace.Record(0, 6);
        for (var beat = 1; beat <= 28; beat++)
        {
            goal.Set(scenario == "release" && beat >= 12 ? 1 : 6);
            var observation = link.Observe(available: scenario != "missing-signal" || beat is < 5 or > 10);
            var filtered = mode == "pending-gate" ? gate.Evaluate(observation.Packet, compiled,
                pendingAvailable: scenario != "hidden-pending" || beat is < 5 or > 10) : null;
            var step = machine.Step(filtered?.Packet ?? observation.Packet, compiled);
            trace.Keep(observation.Witness, filtered?.Witness);
            trace.Record(beat, (long)goal.Value, step: step, input: observation.Mask,
                output: filtered?.OutputMask ?? observation.Mask, suppressed: filtered?.Suppressed ?? false,
                note: filtered?.Pending is null && filtered is not null ? "Pending adjustment is unavailable; no zero is assumed." : "");
        }
        return trace.Finish();
    }

    private static Run Channel(string scenario, string mode, int seed, bool compiled, bool capture)
    {
        Check(scenario, ["fixed", "reverse", "lost-ack", "capacity", "moving"]);
        Check(mode, ["pipeline", "acknowledgement", "acknowledgement-gate"]); CheckSeed(seed, 3);
        var capacity = scenario == "capacity" ? 3 : 8;
        var machine = CoupledExtentMachine.Create(Math.Min(new[] { 0, 2, 5 }[seed], capacity), capacity);
        var goal = Goal(machine, 6); var link = CoupledComparisonLink.Create(Port(goal), machine);
        var gate = PendingAdjustmentGate.Create(machine); var channel = AcknowledgedComparisonChannel.Create(machine);
        var trace = new Trace("channel", scenario, mode, "chevron", seed, machine, capture);
        trace.GoalPort = Port(goal);
        trace.Components.AddRange([Port(goal), link.Surface, gate.Surface]);
        // This queue is the intentionally unregulated old transport control.
        // The new channel retains its actual pending packet/receipt in native structure.
        var pipeline = new Queue<(int Due, CoupledComparisonLink.Observation Observation)>();
        var sendAt = -1; var receiptAt = -1;
        trace.Record(0, 6);
        for (var beat = 1; beat <= 72; beat++)
        {
            goal.Set(scenario == "reverse" && beat >= 22 ? 1 : scenario == "moving" && (beat / 8) % 2 == 1 ? 1 : 6);
            CoupledExtentMachine.StepRead? step = null; var input = 0; var output = 0; var ack = false; var suppressed = false;
            var note = "";
            if (mode == "pipeline")
            {
                var sample = link.Observe(); pipeline.Enqueue((beat + 3, sample)); trace.Keep(sample.Witness);
                if (pipeline.TryPeek(out var old) && old.Due <= beat)
                {
                    pipeline.Dequeue(); input = output = old.Observation.Mask;
                    step = machine.Step(old.Observation.Packet, compiled);
                }
                note = "A new observation is sent every beat; three old comparisons remain in flight.";
            }
            else
            {
                if (receiptAt == beat)
                {
                    var receipt = channel.Snapshot().Receipt!;
                    if (scenario != "lost-ack") { var accepted = channel.Acknowledge(receipt); ack = accepted.Accepted; trace.Keep(accepted.Witness); }
                    else note = "The issued receipt was not returned. The occupied slot remains visible.";
                    receiptAt = -1;
                }
                if (!channel.Snapshot().Waiting)
                {
                    var request = channel.Request(link); trace.Keep(request.Witness, request.Observation?.Witness);
                    if (request.Accepted) sendAt = beat;
                }
                if (sendAt >= 0 && beat == sendAt + 3)
                {
                    var delivery = channel.Dispatch(); trace.Keep(delivery.Witness);
                    if (delivery.Accepted)
                    {
                        input = Mask(machine, delivery.Packet!);
                        var filtered = mode == "acknowledgement-gate" ? gate.Evaluate(delivery.Packet!, compiled) : null;
                        output = filtered?.OutputMask ?? input; suppressed = filtered?.Suppressed ?? false;
                        step = machine.Step(filtered?.Packet ?? delivery.Packet!, compiled); trace.Keep(filtered?.Witness);
                        var receipt = channel.Receipt(machine, step); trace.Keep(receipt.Witness);
                        if (receipt.Accepted) receiptAt = beat + 3;
                    }
                    sendAt = -1;
                }
                if (note.Length == 0) note = "Three beats outward, three beats for the receipt. New samples wait for the previous receipt.";
            }
            trace.Record(beat, (long)goal.Value, step: step, input: input, output: output,
                waiting: mode != "pipeline" && channel.Snapshot().Waiting, acknowledged: ack, suppressed: suppressed, note: note);
        }
        if (mode != "pipeline") trace.Components.Add(channel.ArchiveSurface());
        return trace.Finish();
    }

    private static Run Arbitration(string scenario, string mode, int seed, bool compiled, bool capture)
    {
        Check(scenario, ["opposed", "same", "first-missing", "first-ambiguous", "capacity"]);
        Check(mode, ["alternating", "agreement", "prefer-first", "prefer-second"]); CheckSeed(seed, 3);
        var capacity = scenario == "capacity" ? 3 : 8;
        var machine = CoupledExtentMachine.Create(Math.Min(new[] { 0, 3, 7 }[seed], capacity), capacity);
        var firstGoal = Goal(machine, scenario == "capacity" ? 6 : scenario == "same" ? 4 : 2);
        var secondGoal = Goal(machine, scenario == "capacity" ? 8 : scenario == "same" ? 4 : 6);
        var first = CoupledComparisonLink.Create(Port(firstGoal), machine);
        var second = CoupledComparisonLink.Create(Port(secondGoal), machine);
        var policy = mode == "prefer-first" ? "first" : mode == "prefer-second" ? "second" : "agreement";
        var arbiter = ExtentResponseArbiter.Create(machine, policy); var gate = PendingAdjustmentGate.Create(machine);
        var trace = new Trace("arbitration", scenario, mode, "chevron", seed, machine, capture);
        trace.GoalPort = Port(firstGoal);
        trace.Components.AddRange([first.Surface, second.Surface, arbiter.Surface, gate.Surface]);
        trace.Record(0, (long)firstGoal.Value, (long)secondGoal.Value);
        for (var beat = 1; beat <= 28; beat++)
        {
            var a = first.Observe(available: scenario != "first-missing" || beat < 4,
                ambiguous: scenario == "first-ambiguous" && beat >= 4);
            var b = second.Observe();
            var read = mode == "alternating" ? null : arbiter.Evaluate(a.Packet, b.Packet, compiled);
            var packet = read?.Packet ?? (beat % 2 == 0 ? a.Packet : b.Packet);
            var selected = Mask(machine, packet); var guarded = gate.Evaluate(packet, compiled);
            var step = machine.Step(guarded.Packet, compiled);
            var conflict = read?.Conflict ?? (a.Mask is 1 or 2 or 4 && b.Mask is 1 or 2 or 4 && a.Mask != b.Mask);
            trace.Keep(a.Witness, b.Witness, read?.Witness, guarded.Witness);
            trace.Record(beat, (long)firstGoal.Value, (long)secondGoal.Value, step, selected, guarded.OutputMask,
                suppressed: guarded.Suppressed, conflict: conflict,
                note: $"First offer: {a.Mask}; second offer: {b.Mask}. Retained offers are read residue, not additional physical debt.");
        }
        return trace.Finish();
    }

    private static Run Gap(string scenario, string mode, int seed, string family, bool compiled, bool capture)
    {
        Check(scenario, ["reachable", "off-grid", "capacity", "missing"]);
        Check(mode, ["observe-only", "gap-feedback"]); CheckSeed(seed, 2);
        var template = Template(family); var inset = family == "inset";
        var upperHeight = inset ? 1 : 2; var positiveMotion = template.Frame.Word(0, inset ? -3 : -1);
        var capacity = scenario == "capacity" ? 1 : 6;
        var machine = CoupledExtentMachine.Create(Math.Min(seed + 1, capacity), capacity);
        var desired = Goal(machine, inset ? scenario == "off-grid" ? 10 : 9 : scenario == "off-grid" ? 5 : 6);
        var fixedWidth = Goal(machine, 8); var fixedUpperHeight = Goal(machine, upperHeight);
        var claim = GeometricGapClaim.Create(template.Frame, Port(desired), machine, template.Frame.Motion.Down, positiveMotion);
        var gate = PendingAdjustmentGate.Create(machine);
        var trace = new Trace("gap", scenario, mode, family, seed, machine, capture);
        trace.Components.AddRange([claim.Surface, gate.Surface, template.Surface]);
        var current = Picture(); var measured = Observe(current);
        trace.Keep(current.Surface, measured.Witness);
        trace.Record(0, (long)desired.Value, gap: measured.Gap, gapGoal: (long)desired.Value, drawing: current);
        for (var beat = 1; beat <= 28; beat++)
        {
            // Control reads the actual native section words, never rendered
            // point arrays. Visibility is an explicit observation condition.
            var observation = Observe(current, scenario != "missing" || beat is < 5 or > 10);
            var guarded = gate.Evaluate(observation.Packet, compiled);
            var step = mode == "gap-feedback" ? machine.Step(guarded.Packet, compiled) : null;
            trace.Keep(observation.Witness, guarded.Witness);
            current = Picture(); measured = Observe(current);
            trace.Keep(current.Surface, measured.Witness);
            trace.Record(beat, (long)desired.Value, step: step, input: observation.Mask, output: guarded.OutputMask,
                suppressed: guarded.Suppressed, gap: measured.Gap, gapGoal: (long)desired.Value, drawing: current,
                note: inset ? "An extent step moves this boundary by three shared reaches; only gaps on that spacing can be reached."
                    : "The claim observes the overlapping horizontal sections. Increasing the lower height increases the selected gap.");
        }
        return trace.Finish();

        CoupledShapeTemplate.Realization Picture() => PublishedPicture(template,
            Port(fixedWidth), Port(fixedWidth), Port(fixedUpperHeight), machine.ExtentPort, evidence => trace.Keep(evidence));
        GeometricGapClaim.Observation Observe(CoupledShapeTemplate.Realization picture, bool available = true) => claim.Observe(
            (Relation)FiniteLaw.Entries(picture.Upper.Sections)[1], (Relation)FiniteLaw.Entries(picture.Lower.Sections)[1],
            available: available, compiled: compiled);
    }

    private sealed class Trace(string stage, string scenario, string mode, string family, int seed,
        CoupledExtentMachine machine, bool capture)
    {
        private readonly List<Frame> frames = [];
        private readonly List<IElement> witnesses = [];
        public readonly List<IElement> Components = [];
        public Relation? GoalPort;
        private readonly CoupledShapeTemplate.Template template = Template(family);
        private readonly Carrier fixedHeight = Goal(machine, 2);
        private int unresolved; private int requests;
        public void Keep(params IElement?[] evidence)
        { if (capture) witnesses.AddRange(evidence.Where(item => item is not null).Cast<IElement>()); }
        public void Record(int beat, long goal, long? other = null, CoupledExtentMachine.StepRead? step = null,
            int input = 0, int output = 0, bool waiting = false, bool acknowledged = false,
            bool suppressed = false, bool conflict = false, long? gap = null, long? gapGoal = null,
            CoupledShapeTemplate.Realization? drawing = null, string note = "")
        {
            var now = machine.Snapshot();
            drawing ??= PublishedPicture(template, GoalPort ?? throw new InvalidOperationException("A displayed goal needs its exposed source."),
                machine.ExtentPort, Port(fixedHeight), Port(fixedHeight), evidence => Keep(evidence));
            Keep(drawing.Surface, step?.Surface);
            if (step is { Resolved: false }) unresolved++;
            if (step is { ConsumedEvent: true, IntroducedDelta: not 0 }) requests++;
            frames.Add(new(beat, now.Value, now.Tension, goal, other, input, output, step?.RequestedDelta,
                step?.AcceptedDelta ?? 0, step?.ConsumedEvent ?? false, waiting, acknowledged,
                suppressed, conflict, gap, gapGoal, drawing.Upper.Points, drawing.Lower.Points, drawing.EndCoincident, note));
        }
        public Run Finish()
        {
            var final = frames[^1]; var live = StructuralConstraint.Closed([machine.Surface, .. Components]);
            var archive = capture ? StructuralConstraint.Closed(live, machine.ArchiveSurface(),
                StructuralConstraint.Closed(witnesses.ToArray())) : null;
            return new($"{stage}-{scenario}-{mode}-{family}-{seed}", stage, scenario, mode, family, seed,
                frames.ToArray(), (stage == "gap" ? final.Gap == final.GapGoal : final.Value == final.Goal) && final.Tension == 0,
                final.OtherGoal is null ? null : final.Value == final.OtherGoal && final.Tension == 0,
                final.Tension, unresolved, requests, frames.Count(f => f.Suppressed), frames.Count(f => f.Conflict),
                final.Waiting, GojoNumericFormat.Encode(live).Length) { NativeSurface = archive };
        }
    }

    public static int RunExperiment(string[] args)
    {
        var output = args.FirstOrDefault() ?? Path.Combine("experiments", "QualitativeFrieze", "results", "machine-coordination");
        Directory.CreateDirectory(output);
        var runs = new List<Run>();
        foreach (var scenario in new[] { "capacity", "release", "hidden-pending", "missing-signal" })
            foreach (var mode in new[] { "repeat", "pending-gate" })
                for (var seed = 0; seed < 3; seed++) runs.Add(Execute("pending", scenario, mode, seed));
        foreach (var scenario in new[] { "fixed", "reverse", "lost-ack", "capacity", "moving" })
            foreach (var mode in new[] { "pipeline", "acknowledgement", "acknowledgement-gate" })
                for (var seed = 0; seed < 3; seed++) runs.Add(Execute("channel", scenario, mode, seed));
        foreach (var scenario in new[] { "opposed", "same", "first-missing", "first-ambiguous", "capacity" })
            foreach (var mode in new[] { "alternating", "agreement", "prefer-first", "prefer-second" })
                for (var seed = 0; seed < 3; seed++) runs.Add(Execute("arbitration", scenario, mode, seed));
        foreach (var family in new[] { "cardinal", "chevron", "inset" })
            foreach (var scenario in new[] { "reachable", "off-grid", "capacity", "missing" })
                foreach (var mode in new[] { "observe-only", "gap-feedback" })
                    for (var seed = 0; seed < 2; seed++) runs.Add(Execute("gap", scenario, mode, seed, family));
        var selected = new[] { ("pending", "release", "pending-gate", "chevron"),
            ("channel", "fixed", "acknowledgement-gate", "chevron"),
            ("arbitration", "opposed", "prefer-first", "chevron"), ("gap", "reachable", "gap-feedback", "inset") };
        var comparisons = new List<object>();
        foreach (var (stage, scenario, mode, family) in selected)
        {
            var native = Execute(stage, scenario, mode, family: family, compiled: false);
            var compiled = Execute(stage, scenario, mode, family: family, captureNative: true);
            var agree = JsonSerializer.Serialize(native.Frames) == JsonSerializer.Serialize(compiled.Frames);
            if (!agree) throw new InvalidOperationException($"Native/compiled disagreement in {stage}.");
            var archive = GojoNumericFormat.Encode(compiled.NativeSurface!);
            File.WriteAllBytes(Path.Combine(output, stage + ".gojo"), archive);
            // Decode each actual exported graph before reporting it as a native archive.
            if (GojoNumericFormat.Decode(archive) is not [Relation]) throw new InvalidOperationException("Archive root did not restore.");
            comparisons.Add(new { stage, agree, archiveBytes = archive.Length });
        }
        var data = new { scope = "Supplied local laws, 177 bounded conditions. No learned topology, picture fitting, or general geometric interpolation.", runs, nativeComparisons = comparisons };
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        File.WriteAllText(Path.Combine(output, "study-data.json"), json);
        var view = Path.Combine("experiments", "QualitativeFrieze", "MachineCoordinationView.html");
        if (File.Exists(view)) File.WriteAllText(Path.Combine(output, "index.html"), File.ReadAllText(view).Replace("__STUDY_DATA__", json));
        Console.WriteLine($"Recorded {runs.Count} conditions, four native/compiled comparisons and restored native archives in {Path.GetFullPath(output)}");
        return 0;
    }

    private static int Mask(CoupledExtentMachine machine, Relation packet)
    {
        if (!QualitativePublication.TryRead(packet, out _, out _, out _, out _, out _, out var support, out _) ||
            !StructuralConstraint.TryReadScope(support, out var entries, out var seam, out var tail) || !seam.Equals(tail)) return 0;
        var states = FiniteLaw.Entries(machine.ComparisonDomain); var mask = 0;
        foreach (var value in entries)
        {
            var index = Array.FindIndex(states, item => item is CarrierLocation a && value is CarrierLocation b &&
                ReferenceEquals(a.Carrier, b.Carrier) && a.SameTickRefAs(b));
            if (index < 0) return 0; mask |= 1 << index;
        }
        return mask;
    }
    private static Carrier Goal(CoupledExtentMachine receiver, long value)
    { var goal = new Carrier(receiver.StepUnit.UnitShape); goal.Add(checked((ulong)value)); return goal; }
    private static Relation Port(Carrier carrier) => new(carrier.ValueSlot, carrier);
    private static CoupledShapeTemplate.Realization PublishedPicture(CoupledShapeTemplate.Template template,
        Relation upperWidth, Relation lowerWidth, Relation upperHeight, Relation lowerHeight, Action<IElement> retain)
    {
        var ports = new[] { upperWidth, lowerWidth, upperHeight, lowerHeight };
        var samples = ports.Select(port => new CarrierLocation(((CarrierLocation)port.Left).Carrier,
            ((CarrierLocation)port.Left).Ticks)).ToArray();
        var drawing = CoupledShapeTemplate.Realize(template, checked((int)samples[0].Ticks), checked((int)samples[1].Ticks),
            checked((int)samples[2].Ticks), checked((int)samples[3].Ticks), StructuralConstraint.Closed(samples.Cast<IElement>().ToArray()));
        var sampling = StructuralConstraint.Closed(ports.Select((port, i) => (IElement)new Relation(port, samples[i])).ToArray());
        retain(new Relation(sampling, drawing.Surface));
        return drawing;
    }
    private static CoupledShapeTemplate.Template Template(string family) => family switch
    { "chevron" => CoupledShapeTemplate.ChevronCap(), "cardinal" => CoupledShapeTemplate.CardinalCap(), "inset" => CoupledShapeTemplate.InsetCap(), _ => throw new ArgumentException("Unknown construction.") };
    private static void Check(string value, string[] legal)
    { if (!legal.Contains(value)) throw new ArgumentException("Unknown supplied condition: " + value); }
    private static void CheckSeed(int seed, int count)
    { if (seed < 0 || seed >= count) throw new ArgumentOutOfRangeException(nameof(seed)); }
}
