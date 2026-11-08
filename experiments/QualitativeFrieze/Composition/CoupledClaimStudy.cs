using System.Text.Json;
using System.Text.Json.Serialization;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Supplied two-control apparatus. Native claims observe native words;
/// finite laws choose local responses. Host conditions select visibility and
/// pulse timing, never a target width or a fitted outline.</summary>
public static class CoupledClaimStudy
{
    public sealed record Frame(int Beat, long Width, long Height, long WidthTension, long HeightTension,
        long? Gap, long GapGoal, long? AlongDifference, long? TransverseDifference,
        bool? AxisAgreement, bool? EndCoincident, int GapInputMask, int GapOutputMask,
        int EndpointInputMask, int EndpointOutputMask, long? WidthRequest, long? HeightRequest,
        long WidthAccepted, long HeightAccepted, bool WidthConsumed, bool HeightConsumed,
        bool WidthSuppressed, bool HeightSuppressed, int[][] Upper, int[][] Lower, string Note);
    public sealed record Run(string Id, string Family, string Scenario, string Mode, int Seed,
        Frame[] Frames, bool Settled, int? FirstSettledBeat, int LiveBytes)
    {
        [JsonIgnore] public Relation? NativeSurface { get; init; }
    }

    public static Run Execute(string family = "chevron", string scenario = "reachable", string mode = "together",
        int seed = 0, bool compiled = true, bool captureNative = false)
    {
        if (seed is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(seed));
        if (!new[] { "gap-only", "endpoint-only", "together", "alternating" }.Contains(mode))
            throw new ArgumentException("Unknown response schedule.", nameof(mode));
        if (!new[] { "reachable", "height-capacity", "width-capacity", "missing-gap", "missing-endpoint",
            "ambiguous-endpoint", "incompatible", "off-grid" }.Contains(scenario) || scenario == "off-grid" && family != "inset")
            throw new ArgumentException("Unknown supplied condition.", nameof(scenario));
        var template = family switch
        {
            "cardinal" => CoupledShapeTemplate.CardinalCap(), "chevron" => CoupledShapeTemplate.ChevronCap(),
            "inset" => CoupledShapeTemplate.InsetCap(), _ => throw new ArgumentException("Unknown construction.", nameof(family))
        };
        var inset = family == "inset";
        var heightCapacity = scenario == "height-capacity" ? 1 : 10;
        var widthCapacity = scenario == "width-capacity" ? 3 : 12;
        var height = CoupledExtentMachine.Create(Math.Min(seed == 0 ? 1 : 5, heightCapacity), heightCapacity);
        var width = CoupledExtentMachine.Create(Math.Min(seed == 0 ? 8 : 2, widthCapacity), widthCapacity, unit: height.StepUnit.UnitShape);
        var upperWidth = Value(8); var upperHeight = Value(inset ? 1 : 2);
        var desired = Value(scenario == "off-grid" ? 10 : scenario == "incompatible" ? inset ? 24 : 10 : inset ? 9 : 6);
        var gap = GeometricGapClaim.Create(template.Frame, Port(desired), height,
            template.Frame.Motion.Down, template.Frame.Word(0, inset ? -3 : -1));
        var endpoint = GeometricEndpointClaim.Create(template.Frame, width,
            template.Frame.Motion.Right, template.Frame.Word(1, 0));
        var heightGate = PendingAdjustmentGate.Create(height); var widthGate = PendingAdjustmentGate.Create(width);
        var witnesses = new List<IElement>(); var frames = new List<Frame>();
        var current = Picture(); Record(0);
        for (var beat = 1; beat <= 36; beat++)
        {
            // Read both claims before either control responds. In the alternating
            // control the observations still exist, but only one pulse is admitted.
            var gapRead = GapRead(scenario != "missing-gap" || beat > 8);
            var endRead = endpoint.Observe(current.Upper.EndPort, current.Lower.EndPort,
                available: scenario != "missing-endpoint" || beat > 8,
                ambiguous: scenario == "ambiguous-endpoint" && beat <= 8, compiled: compiled);
            var h = heightGate.Evaluate(gapRead.Packet, compiled);
            var w = widthGate.Evaluate(endRead.Packet, compiled);
            var heightStep = height.Step(h.Packet, compiled,
                pulse: mode is "gap-only" or "together" || mode == "alternating" && beat % 2 == 1);
            var widthStep = width.Step(w.Packet, compiled,
                pulse: mode is "endpoint-only" or "together" || mode == "alternating" && beat % 2 == 0);
            Keep(gapRead.Witness, endRead.Witness, h.Witness, w.Witness, heightStep.Surface, widthStep.Surface);
            current = Picture();
            Record(beat, gapRead.Mask, h.OutputMask, endRead.Mask, w.OutputMask, heightStep, widthStep,
                h.Suppressed, w.Suppressed);
        }
        var live = S(template.Surface, gap.Surface, endpoint.Surface, heightGate.Surface, widthGate.Surface,
            height.Surface, width.Surface, Port(upperWidth), Port(upperHeight), Port(desired));
        var archive = captureNative ? S(live, height.ArchiveSurface(), width.ArchiveSurface(), S(witnesses.ToArray())) : null;
        var firstSettled = frames.FindIndex(IsSettled);
        return new($"{family}-{scenario}-{mode}-{seed}", family, scenario, mode, seed, frames.ToArray(),
            IsSettled(frames[^1]), firstSettled < 0 ? null : frames[firstSettled].Beat, GojoNumericFormat.Encode(live).Length)
        { NativeSurface = archive };

        Carrier Value(long value) { var c = new Carrier(height.StepUnit.UnitShape); c.Add(checked((ulong)value)); return c; }
        void Keep(params IElement[] values) { if (captureNative) witnesses.AddRange(values); }
        CoupledShapeTemplate.Realization Picture()
        {
            var ports = new[] { Port(upperWidth), width.ExtentPort, Port(upperHeight), height.ExtentPort };
            var samples = ports.Select(p => new CarrierLocation(((CarrierLocation)p.Left).Carrier, ((CarrierLocation)p.Left).Ticks)).ToArray();
            var picture = CoupledShapeTemplate.Realize(template, checked((int)samples[0].Ticks), checked((int)samples[1].Ticks),
                checked((int)samples[2].Ticks), checked((int)samples[3].Ticks), S(samples.Cast<IElement>().ToArray()));
            Keep(new Relation(S(ports.Select((port, i) => (IElement)new Relation(port, samples[i])).ToArray()), picture.Surface));
            return picture;
        }
        GeometricGapClaim.Observation GapRead(bool available = true) => gap.Observe(
            (Relation)FiniteLaw.Entries(current.Upper.Sections)[1], (Relation)FiniteLaw.Entries(current.Lower.Sections)[1],
            available: available, compiled: compiled);
        void Record(int beat, int gi = 0, int go = 0, int ei = 0, int eo = 0,
            CoupledExtentMachine.StepRead? hs = null, CoupledExtentMachine.StepRead? ws = null,
            bool hSuppressed = false, bool wSuppressed = false)
        {
            // These after-state observations belong only to the report/history.
            // Even during withheld evidence they never enter the control loop.
            var measuredGap = GapRead();
            var measuredEnd = endpoint.Observe(current.Upper.EndPort, current.Lower.EndPort, compiled: compiled);
            Keep(measuredGap.Witness, measuredEnd.Witness);
            var h = height.Snapshot(); var w = width.Snapshot();
            var note = measuredGap.Gap is null ? "The selected sections do not supply a valid gap read. No zero gap is assumed."
                : scenario == "off-grid" ? "A height step shifts the boundary by three reaches; the requested gap lies between reachable readings."
                : mode == "alternating" ? "Both claims were sampled together; odd beats allow height, even beats allow width."
                : "Both claims were sampled before either extent changed. The diagram and residuals show the resulting after-state.";
            frames.Add(new(beat, w.Value, h.Value, w.Tension, h.Tension, measuredGap.Gap, (long)desired.Value,
                measuredEnd.AlongDifference, measuredEnd.TransverseDifference, measuredEnd.AxisAgreement, measuredEnd.CoordinateCoincident,
                gi, go, ei, eo, ws?.RequestedDelta, hs?.RequestedDelta, ws?.AcceptedDelta ?? 0, hs?.AcceptedDelta ?? 0,
                ws?.ConsumedEvent ?? false, hs?.ConsumedEvent ?? false, wSuppressed, hSuppressed,
                current.Upper.Points, current.Lower.Points, note));
        }
    }

    public static int RunExperiment(string[] args)
    {
        var output = args.FirstOrDefault() ?? Path.Combine("experiments", "QualitativeFrieze", "results", "coupled-claims");
        Directory.CreateDirectory(output); var runs = new List<Run>();
        foreach (var family in new[] { "cardinal", "chevron", "inset" })
            foreach (var scenario in new[] { "reachable", "height-capacity", "width-capacity", "missing-gap",
                "missing-endpoint", "ambiguous-endpoint", "incompatible", "off-grid" })
            {
                if (scenario == "off-grid" && family != "inset") continue;
                foreach (var mode in new[] { "gap-only", "endpoint-only", "together", "alternating" })
                    for (var seed = 0; seed < 2; seed++) runs.Add(Execute(family, scenario, mode, seed));
            }
        var comparisons = new List<object>();
        foreach (var (family, scenario) in new[] { ("chevron", "reachable"), ("inset", "off-grid"), ("chevron", "incompatible") })
        {
            var native = Execute(family, scenario, compiled: false);
            var compiled = Execute(family, scenario, captureNative: true);
            var agree = JsonSerializer.Serialize(native.Frames) == JsonSerializer.Serialize(compiled.Frames);
            if (!agree) throw new InvalidOperationException("Native/compiled trace disagreement.");
            var bytes = GojoNumericFormat.Encode(compiled.NativeSurface!);
            if (GojoNumericFormat.Decode(bytes) is not [Relation]) throw new InvalidOperationException("Native archive failed to decode.");
            File.WriteAllBytes(Path.Combine(output, family + "-" + scenario + ".gojo"), bytes);
            comparisons.Add(new { family, scenario, agree, archiveBytes = bytes.Length });
        }
        var data = new { scope = "Two connected local claims, supplied motion correspondences, 176 bounded conditions. No target-outline solver or learned controller.", runs, comparisons };
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        File.WriteAllText(Path.Combine(output, "study-data.json"), json);
        var view = Path.Combine("experiments", "QualitativeFrieze", "CoupledClaimView.html");
        if (File.Exists(view)) File.WriteAllText(Path.Combine(output, "index.html"), File.ReadAllText(view).Replace("__DATA__", json));
        Console.WriteLine($"Recorded {runs.Count} runs; {runs.Count(r => r.Settled)} ended with both claims satisfied and no extent tension. {Path.GetFullPath(output)}");
        return 0;
    }
    private static bool IsSettled(Frame f) => f.Gap == f.GapGoal && f.EndCoincident == true && f.WidthTension == 0 && f.HeightTension == 0;
    private static Relation Port(Carrier carrier) => new(carrier.ValueSlot, carrier);
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
}
