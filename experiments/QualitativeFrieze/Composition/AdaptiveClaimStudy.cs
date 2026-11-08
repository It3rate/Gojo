using System.Text.Json;
using System.Text.Json.Serialization;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Structures;
using Gojo.Experiments.QualitativeExecution;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Supplied apparatus for alternative observations, resolution rungs
/// and finite motion evidence. Conditions supply interventions; native claims,
/// laws and retained observations choose subsequent local corrections.</summary>
public static class AdaptiveClaimStudy
{
    public sealed record Frame(int Beat, long Height, long Width, int HeightScale, int WidthScale,
        long HeightTension, long WidthTension, bool HeightFine, bool WidthFine,
        long HeightCoarse, long HeightResidue, long WidthCoarse, long WidthResidue,
        long HeightRetained, long WidthRetained, long? Gap, long? LandmarkGap, long Goal,
        long? EndpointDifference, bool UsedAlternative, int HeightMask, int WidthMask,
        long? HeightRequest, long? WidthRequest, int HeightCandidates, int WidthCandidates,
        string HeightMotion, string WidthMotion, bool HeightTrial, bool WidthTrial,
        bool HeightRefined, bool WidthRefined, bool RefinementBlocked,
        int[][] Upper, int[][] Lower, string Note);
    public sealed record Run(string Id, string Stage, string Scenario, string Mode, string Family,
        int Seed, Frame[] Frames, bool PrimarySettled, bool AlternativeSettled, bool JointSettled, int NativeBytes)
    { [JsonIgnore] public Relation? NativeSurface { get; init; } }

    public static Run Execute(string stage, string scenario, string mode, int seed = 0,
        bool compiled = true, bool captureNative = false)
    {
        if (!Cases(stage).Contains(scenario) || !Modes(stage).Contains(mode) || seed is < 0 or > 1)
            throw new ArgumentException("Unknown supplied experiment condition.");
        var alternative = mode is "alternative" or "alt-only" or "combined";
        var refine = mode is "refine" or "refine-only" or "combined";
        var learn = mode is "observed" or "learn-only" or "combined";
        var family = stage == "alternative" || stage == "combined" && scenario == "collapsed" ? "chevron" : "inset";
        var inset = family == "inset";
        var template = inset ? CoupledShapeTemplate.InsetCap() : CoupledShapeTemplate.ChevronCap();
        var heightStart = scenario == "disjoint" ? 12 - seed : seed == 0 ? 1 : 3;
        if (scenario == "blocked") heightStart = 1;
        var widthStart = scenario == "collapsed" || stage == "alternative" && scenario == "missing" ? 0 : scenario == "disjoint" ? 1 : seed == 0 ? 8 : 4;
        var hc = CoupledExtentMachine.Create(heightStart, scenario == "blocked" ? 1 : 12);
        var wc = CoupledExtentMachine.Create(widthStart, 12, unit: hc.StepUnit.UnitShape);
        var h = new Lane(template.Frame, hc, scenario == "fine-capacity" ? 4 : 36, true, inset, scenario == "ambiguous");
        var w = new Lane(template.Frame, wc, 36, false, inset, scenario == "ambiguous");
        var common = new Carrier(h.Rung.Fine.StepUnit.UnitShape);
        var upperWidth = Value(24); var upperHeight = Value(inset ? 3 : 6);
        var requested = scenario is "between-steps" or "fine-capacity" || stage == "combined" ? inset ? 30 : 19 : inset ? 27 : 18;
        var goal = Value(requested);
        var publications = new Dictionary<(bool, bool), CalibratedShapePublication>();
        var gapReaders = new Dictionary<(CoupledExtentMachine, Relation), GeometricGapAlternative>();
        var endReaders = new Dictionary<(CoupledExtentMachine, Relation), GeometricEndpointClaim>();
        var evidence = new List<IElement>(); var frames = new List<Frame>();
        var current = Picture(); Record(0);
        for (var beat = 1; beat <= 48; beat++)
        {
            var available = scenario != "missing" || beat > 8;
            var gap = Gap(h.Motion(learn), available); var end = End(w.Motion(learn), available);
            var hg = h.Gate.Evaluate(alternative ? gap.Packet : gap.PrimaryPacket, compiled);
            var wg = w.Gate.Evaluate(end.Packet, compiled);
            var hr = TryRefine(h, hg.Packet); var wr = stage == "alternative" ? null : TryRefine(w, wg.Packet);
            var didH = hr?.Activated ?? false; var didW = wr?.Activated ?? false;
            CoupledExtentMachine.StepRead? hs = null, ws = null;
            var hTrial = false; var wTrial = false; var before = current;
            if (learn && !didH && !didW && available && (h.NeedsTrial || stage != "alternative" && w.NeedsTrial))
            {
                // Supplied bounded trial, separate from the subsequent feedback
                // law. The other extent is held except in the confounding control.
                hTrial = h.NeedsTrial; wTrial = !hTrial;
                var trial = hTrial ? h : w; var other = hTrial ? w : h;
                var trialStep = trial.Machine.Step(trial.TakeTrial(), compiled);
                CoupledExtentMachine.StepRead? otherStep = scenario == "confounded"
                    ? other.Machine.Step(other.Machine.Publication(4), compiled) : null;
                if (hTrial) { hs = trialStep; ws = otherStep; } else { ws = trialStep; hs = otherStep; }
                current = Picture();
                var changed = scenario == "confounded" ? S(h.Machine.ExtentPort, w.Machine.ExtentPort) : S(trial.Machine.ExtentPort);
                var observed = trial.Learner.Observe(Sample(trial.Machine, trialStep.Before.Value), Sample(trial.Machine, trialStep.After.Value),
                    Landmark(before, hTrial), Landmark(current, hTrial), changed, trialStep.Surface);
                Keep(observed.Witness, new Relation(trial.TrialRoute, trialStep.Surface));
                h.PreviousPacket = w.PreviousPacket = null; h.PreviousStep = w.PreviousStep = null;
            }
            else
            {
                if (!didH) { hs = h.Machine.Step(hg.Packet, compiled); h.PreviousPacket = hg.Packet; h.PreviousStep = hs; }
                if (!didW && stage != "alternative")
                { ws = w.Machine.Step(wg.Packet, compiled); w.PreviousPacket = wg.Packet; w.PreviousStep = ws; }
                current = Picture();
            }
            Keep(gap.Witness, end.Witness, hg.Witness, wg.Witness);
            if (hs is not null) Keep(hs.Surface);
            if (ws is not null) Keep(ws.Surface);
            Record(beat, hs, ws, alternative && gap.UsedAlternative, hg.OutputMask, wg.OutputMask,
                hTrial, wTrial, didH, didW, hr?.Blocked == true || wr?.Blocked == true);
        }
        var last = frames[^1];
        var live = S(template.Surface, h.Archive(), w.Archive(), Port(goal), Port(upperWidth), Port(upperHeight),
            S(publications.Values.Select(p => (IElement)p.Surface).ToArray()),
            S(gapReaders.Values.Select(p => (IElement)p.Surface).ToArray()), S(endReaders.Values.Select(p => (IElement)p.Surface).ToArray()));
        var native = captureNative ? S(live, S(evidence.ToArray())) : null;
        var primaryMet = last.Gap == last.Goal && last.HeightTension == 0;
        return new($"{stage}-{scenario}-{mode}-{seed}", stage, scenario, mode, family, seed, frames.ToArray(),
            primaryMet, last.LandmarkGap == last.Goal && last.HeightTension == 0,
            primaryMet && last.EndpointDifference == 0 && current.EndCoincident && last.WidthTension == 0,
            GojoNumericFormat.Encode(live).Length) { NativeSurface = native };

        Carrier Value(long value) { var c = new Carrier(common.UnitShape); c.Add(checked((ulong)value)); return c; }
        void Keep(params IElement[] items) { if (captureNative) evidence.AddRange(items); }
        CoupledShapeTemplate.Realization Picture()
        {
            var key = (h.Rung.Active, w.Rung.Active);
            if (!publications.TryGetValue(key, out var publication))
            {
                publication = new(template, common, CalibratedShapePublication.Bind(Port(upperWidth), common, 1),
                    CalibratedShapePublication.Bind(w.Machine.ExtentPort, common, w.Scale),
                    CalibratedShapePublication.Bind(Port(upperHeight), common, 1),
                    CalibratedShapePublication.Bind(h.Machine.ExtentPort, common, h.Scale));
                publications.Add(key, publication);
            }
            var read = publication.Observe(); Keep(read.Witness);
            return read.Picture ?? throw new InvalidOperationException("The supplied whole-reach drawing calibration did not read.");
        }
        GeometricGapAlternative.Observation Gap(Relation motion, bool available = true)
        {
            var key = (h.Machine, motion);
            if (!gapReaders.TryGetValue(key, out var claim))
            {
                claim = GeometricGapAlternative.CreateCalibrated(template.Frame, Port(goal), h.Machine, template.Frame.Motion.Down, motion);
                gapReaders.Add(key, claim);
                if (learn && ReferenceEquals(motion, h.Learner.Read().Unique)) Keep(new Relation(h.Learner.Read().Witness, claim.Surface));
            }
            return claim.Observe(Section(current.Upper), Section(current.Lower), SectionStart(current.Upper), SectionStart(current.Lower),
                available: available, compiled: compiled);
        }
        GeometricEndpointClaim.Observation End(Relation motion, bool available = true)
        {
            var key = (w.Machine, motion);
            if (!endReaders.TryGetValue(key, out var claim))
            {
                claim = GeometricEndpointClaim.Create(template.Frame, w.Machine, template.Frame.Motion.Right, motion);
                endReaders.Add(key, claim);
                if (learn && ReferenceEquals(motion, w.Learner.Read().Unique)) Keep(new Relation(w.Learner.Read().Witness, claim.Surface));
            }
            return claim.Observe(current.Upper.EndPort, current.Lower.EndPort, available: available, compiled: compiled);
        }
        Relation Section(CoupledShapeTemplate.RouteRead route) => (Relation)FiniteLaw.Entries(route.Sections)[1];
        Relation SectionStart(CoupledShapeTemplate.RouteRead route)
        { var read = template.Frame.Read(Section(route)); return new Relation(read.Start, FiniteLaw.Entries(read.ControlWords)[0]); }
        Relation Landmark(CoupledShapeTemplate.Realization picture, bool height) => height ? SectionStart(picture.Lower) : picture.Lower.EndPort;
        ExtentResolutionRung.ActivationRead? TryRefine(Lane lane, Relation packet)
        {
            if (!refine || lane.Rung.Active || lane.PreviousPacket is null || lane.PreviousStep is null) return null;
            var read = lane.Rung.Reversal.Evaluate(lane.PreviousPacket, packet, lane.PreviousStep.Surface, compiled); Keep(read.Witness);
            if (!read.Suggested) return null;
            var activation = lane.Rung.Activate(read.Witness); Keep(activation.Witness);
            if (activation.Activated) { lane.PreviousPacket = null; lane.PreviousStep = null; }
            return activation;
        }
        void Record(int beat, CoupledExtentMachine.StepRead? hs = null, CoupledExtentMachine.StepRead? ws = null,
            bool used = false, int hm = 0, int wm = 0, bool ht = false, bool wt = false,
            bool hf = false, bool wf = false, bool blocked = false)
        {
            // After-state geometry is a diagnostic, never a replacement for a
            // missing learned correspondence or a withheld public message.
            var gap = Gap(h.AuthoredMotion); var end = End(w.AuthoredMotion);
            var hv = h.Machine.Snapshot(); var wv = w.Machine.Snapshot();
            var hread = h.CoarseRead(); var wread = w.CoarseRead();
            var hl = h.Learner.Read(); var wl = w.Learner.Read(); Keep(gap.Witness, end.Witness, hl.Witness, wl.Witness);
            var note = ht || wt ? scenario == "confounded" ? "Trial changed both inputs; evidence is confounded and cannot select a motion word."
                : "A supplied isolated positive trial observes actual accepted change, not the requested amount."
                : hf || wf ? "An opposed response admitted a separate finer observation rung; the coarse source is retained."
                : blocked ? "The finer rung was requested but its local admission premises did not hold."
                : used ? "Landmark separation supplied the response. This does not certify the original section gap."
                : learn && (hl.Unique is null || wl.Unique is null) ? "At least one motion correspondence is unresolved; no authored answer is substituted into its control."
                : "Claims sampled before both responses. Geometry shown is the after-state in the unchanged common reach basis.";
            frames.Add(new(beat, hv.Value, wv.Value, h.Scale, w.Scale, hv.Tension, wv.Tension, h.Rung.Active, w.Rung.Active,
                hread.Whole, hread.Residue, wread.Whole, wread.Residue, h.Rung.Coarse.Snapshot().Value, w.Rung.Coarse.Snapshot().Value,
                gap.PrimaryGap, gap.AlternativeGap, requested, end.AlongDifference, used, hm, wm, hs?.RequestedDelta, ws?.RequestedDelta,
                hl.Possible.Length, wl.Possible.Length, MotionText(h.Motion(learn)), MotionText(w.Motion(learn)), ht, wt, hf, wf, blocked,
                current.Upper.Points, current.Lower.Points, note));
        }
        string MotionText(Relation word) => template.Frame.Vector(word, out var v) ? $"({v.X}, {v.Y}) reaches / tick" : "unresolved";
    }

    private sealed class Lane
    {
        public ExtentResolutionRung Rung { get; }
        private readonly PendingAdjustmentGate[] gates;
        private readonly ObservedMotionCandidates[] learners;
        private readonly Carrier[] allowances;
        private readonly Relation[] trialRoutes;
        private readonly Relation[] authored;
        private readonly Relation unknown = StructuralConstraint.Open();
        public Relation? PreviousPacket;
        public CoupledExtentMachine.StepRead? PreviousStep;
        private int Index => Rung.Active ? 1 : 0;
        public CoupledExtentMachine Machine => Rung.Active ? Rung.Fine : Rung.Coarse;
        public int Scale => Rung.Active ? 1 : 3;
        public PendingAdjustmentGate Gate => gates[Index];
        public ObservedMotionCandidates Learner => learners[Index];
        public Relation AuthoredMotion => authored[Index];
        public Relation TrialRoute => trialRoutes[Index];
        public bool NeedsTrial => Learner.Read().Unique is null && allowances[Index].Value > 0;
        public Lane(Curve.Frame frame, CoupledExtentMachine coarse, long capacity, bool height, bool inset, bool ambiguous)
        {
            Rung = ExtentResolutionRung.Create(coarse, 3, capacity);
            gates = [PendingAdjustmentGate.Create(coarse), PendingAdjustmentGate.Create(Rung.Fine)];
            allowances = [new Carrier(), new Carrier()]; foreach (var c in allowances) c.Add(1);
            var machines = new[] { coarse, Rung.Fine };
            authored = new[] { 3, 1 }.Select(scale => height ? frame.Word(inset ? 0 : scale, -(inset ? 3 : 1) * scale) : frame.Word(scale, 0)).ToArray();
            learners = machines.Select((machine, index) =>
            {
                var candidates = new List<Relation>();
                foreach (var amount in new[] { 1, 3, 9 })
                {
                    candidates.Add(frame.Word(0, -amount)); candidates.Add(frame.Word(0, amount));
                    candidates.Add(frame.Word(amount, 0)); candidates.Add(frame.Word(-amount, 0)); candidates.Add(frame.Word(amount, -amount));
                }
                candidates.Add(frame.Word(0, 0));
                if (ambiguous) candidates.Add(S(FiniteLaw.Entries(frame.Word(1, 0)).Concat(FiniteLaw.Entries(frame.Word(-1, 0)))
                    .Concat(FiniteLaw.Entries(authored[index])).ToArray()));
                return ObservedMotionCandidates.Create(frame, machine.ExtentPort, candidates.ToArray());
            }).ToArray();
            trialRoutes = machines.Select((machine, index) => new Relation(Port(allowances[index]),
                new Relation(machine.ComparisonDomain, machine.Publication(4)))).ToArray();
        }
        public Relation Motion(bool learn) => learn ? Learner.Read().Unique ?? unknown : AuthoredMotion;
        public Relation TakeTrial()
        {
            if (allowances[Index].Value == 0) throw new InvalidOperationException("No trial allowance remains.");
            allowances[Index].Remove(1); return (Relation)((Relation)trialRoutes[Index].Right).Right;
        }
        public (long Whole, long Residue) CoarseRead()
        {
            if (!Rung.Active) return (Rung.Coarse.Snapshot().Value, 0);
            var read = Rung.ReadCoarse();
            if (!read.TryReadProjection(out var value, out var residue) || !ProjectionResidue.TryRead(residue, out _, out var n, out var d) || d != 1)
                throw new InvalidOperationException("The supplied subdivision failed its exact coarse read.");
            return (value.Ticks, n);
        }
        public Relation Archive() => S(Rung.ArchiveSurface(), S(gates.Select(g => (IElement)g.Surface).ToArray()),
            S(learners.Select(l => (IElement)l.ArchiveSurface()).ToArray()), S(trialRoutes), S(authored));
    }

    public static int RunExperiment(string[] args)
    {
        var output = args.FirstOrDefault() ?? Path.Combine("experiments", "QualitativeFrieze", "results", "adaptive-claims");
        Directory.CreateDirectory(output); var runs = new List<Run>();
        foreach (var stage in new[] { "alternative", "refinement", "motion", "combined" })
            foreach (var scenario in Cases(stage)) foreach (var mode in Modes(stage))
                for (var seed = 0; seed < 2; seed++) runs.Add(Execute(stage, scenario, mode, seed));
        var comparisons = new List<object>();
        foreach (var (stage, scenario, mode) in new[] { ("alternative", "collapsed", "alternative"),
            ("refinement", "between-steps", "refine"), ("motion", "ordinary", "observed"), ("combined", "between-steps", "combined") })
        {
            var native = Execute(stage, scenario, mode, compiled: false);
            var compiled = Execute(stage, scenario, mode, captureNative: true);
            var agree = JsonSerializer.Serialize(native.Frames) == JsonSerializer.Serialize(compiled.Frames);
            if (!agree) throw new InvalidOperationException("Native and compiled law traces disagree.");
            var bytes = GojoNumericFormat.Encode(compiled.NativeSurface!);
            if (GojoNumericFormat.Decode(bytes) is not [Relation]) throw new InvalidOperationException("Native archive failed to decode.");
            File.WriteAllBytes(Path.Combine(output, stage + ".gojo"), bytes);
            comparisons.Add(new { stage, agree, archiveBytes = bytes.Length });
        }
        var data = new { scope = "84 bounded conditions. Supplied alternatives, finer rungs, finite motion hypotheses and trial protocol. Native observations and laws select local responses.", runs, comparisons };
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        File.WriteAllText(Path.Combine(output, "study-data.json"), json);
        var view = Path.Combine("experiments", "QualitativeFrieze", "AdaptiveClaimView.html");
        if (File.Exists(view)) File.WriteAllText(Path.Combine(output, "index.html"), File.ReadAllText(view).Replace("__DATA__", json));
        Console.WriteLine($"Recorded {runs.Count} conditions; joint matches {runs.Count(r => r.JointSettled)}, primary gap matches {runs.Count(r => r.PrimarySettled)}, landmark matches {runs.Count(r => r.AlternativeSettled)}. {Path.GetFullPath(output)}");
        return 0;
    }
    private static string[] Cases(string stage) => stage switch
    {
        "alternative" => ["collapsed", "disjoint", "missing", "ordinary"], "refinement" => ["reachable", "between-steps", "fine-capacity"],
        "motion" => ["ordinary", "confounded", "blocked", "ambiguous"], "combined" => ["between-steps", "collapsed", "confounded", "fine-capacity"],
        _ => throw new ArgumentException("Unknown study stage.")
    };
    private static string[] Modes(string stage) => stage switch
    {
        "alternative" => ["primary", "alternative"], "refinement" => ["coarse", "refine"], "motion" => ["authored", "observed"],
        "combined" => ["baseline", "alt-only", "refine-only", "learn-only", "combined"], _ => throw new ArgumentException("Unknown study stage.")
    };
    private static CarrierLocation Sample(CoupledExtentMachine machine, long value) => new((Carrier)machine.ExtentPort.Right, value);
    private static Relation Port(Carrier carrier) => new(carrier.ValueSlot, carrier);
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
}
