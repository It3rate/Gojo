using System.Text.Json;
using System.Text.Json.Serialization;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Bounded apparatus for partial action, coordinated trials and local
/// whole-pass utility. Source construction, offered words, clock and evaluation
/// law are supplied. Native response, trial and pass-memory surfaces make the
/// subsequent choices; the presentation never feeds a private correct answer.</summary>
public static class PartialDecisionStudy
{
    public const int Passes = 6;
    public const int BeatsPerPass = 6;
    public sealed record Frame(int Beat, int Pass, int Step, long Height, long Width,
        long HeightTension, long WidthTension, long? Gap, long? LandmarkGap, long Goal,
        long? EndpointDifference, int HeightCandidates, int WidthCandidates,
        string HeightAction, string WidthAction, long? HeightRequest, long? WidthRequest,
        bool HeightTrial, bool WidthTrial, bool TrialWaiting, bool TrialInformative,
        bool DiagnosticAlternative, bool Available, int[][] Upper, int[][] Lower, string Note);
    public sealed record PassRead(int Pass, long Goal, long? GapBefore, long? GapAfter,
        long? EndBefore, long? EndAfter, string HeightEvaluation, string WidthEvaluation,
        int HeightChoice, int WidthChoice, int SharedActs, int ProvisionalActs,
        int Trials, int InformativeTrials, int RetainedEvaluations, long DroppedEvaluations);
    public sealed record Run(string Id, string Scenario, string Mode, string Family,
        Frame[] Frames, PassRead[] PassResults, bool PrimarySettled, bool LandmarkSettled,
        bool JointSettled, int NativeBytes)
    { [JsonIgnore] public Relation? NativeSurface { get; init; } }

    public static readonly string[] Scenarios = ["equivalent", "opposed", "capacity", "delayed",
        "quiet-refused", "confounded", "goal-change", "withheld", "lost-section"];
    public static readonly string[] Modes = ["unique", "shared", "fixed", "passes", "adaptive"];
    public static readonly string[] Families = ["inset", "chevron"];

    /// <summary>Supplied apparatus changes for paired transfer experiments.
    /// Policy hooks run only at construction and export, never during action
    /// selection or pass evaluation. Affine reversal is an actual publication
    /// relation, not a change to the receiver's hidden response code.</summary>
    public sealed record Configuration(long InitialHeight = 1, long InitialWidth = 10,
        long UpperWidth = 12, long? Goal = null, long HeightCapacity = 12,
        bool ReverseHeightProjection = false, bool ReverseOptionOrder = false,
        int? UnavailablePass = null,
        Action<PassPolicyMemory, bool>? InitializePolicy = null,
        Action<PassPolicyMemory, bool>? ExportPolicy = null);

    public static Run Execute(string scenario, string mode, string family = "inset",
        bool compiled = true, bool captureNative = false, Configuration? configuration = null)
    {
        if (!Scenarios.Contains(scenario) || !Modes.Contains(mode) || !Families.Contains(family))
            throw new ArgumentException("Unknown supplied partial-decision condition.");
        var allowChoice = mode is "fixed" or "passes" or "adaptive";
        var allowTrials = mode == "adaptive";
        var requireUnique = mode == "unique";
        var inset = family == "inset";
        var setup = configuration ?? new Configuration();
        var template = inset ? CoupledShapeTemplate.InsetCap() : CoupledShapeTemplate.ChevronCap();
        var frame = template.Frame;
        var polarity = setup.ReverseHeightProjection ? -1 : 1;
        var h = new Lane(frame, CoupledExtentMachine.Create(setup.InitialHeight, scenario == "capacity" ? 1 : setup.HeightCapacity),
            frame.Word(polarity * (inset ? 0 : 1), polarity * (inset ? -3 : -1)), scenario == "equivalent", setup.ReverseOptionOrder);
        var w = new Lane(frame, CoupledExtentMachine.Create(scenario == "lost-section" ? 0 : setup.InitialWidth, 20,
            unit: h.Machine.StepUnit.UnitShape), frame.Word(1, 0), scenario == "equivalent", setup.ReverseOptionOrder);
        h.Trial = LocalTrialMachine.Create(h.Machine, w.Quiet.PublicPort, allowance: 2);
        w.Trial = LocalTrialMachine.Create(w.Machine, h.Quiet.PublicPort, allowance: 2);
        var common = new Carrier(h.Machine.StepUnit.UnitShape);
        Carrier Value(long v) { var c = new Carrier(common.UnitShape); c.Add((ulong)v); return c; }
        var upperWidth = Value(setup.UpperWidth); var upperHeight = Value(1); var goal = Value(setup.Goal ?? (inset ? 12 : 4));
        var heightBinding = setup.ReverseHeightProjection
            ? new Relation(h.Machine.ExtentPort, new Relation(
                Relation.Segment((Carrier)h.Machine.ExtentPort.Right, 0, setup.HeightCapacity),
                Relation.Segment(common, setup.HeightCapacity, 0)))
            : CalibratedShapePublication.Bind(h.Machine.ExtentPort, common, 1);
        var publication = new CalibratedShapePublication(template, common,
            CalibratedShapePublication.Bind(Port(upperWidth), common, 1),
            CalibratedShapePublication.Bind(w.Machine.ExtentPort, common, 1),
            CalibratedShapePublication.Bind(Port(upperHeight), common, 1),
            heightBinding);
        var gaps = new Dictionary<Relation, GeometricGapAlternative>(ReferenceEqualityComparer.Instance);
        var ends = new Dictionary<Relation, GeometricEndpointClaim>(ReferenceEqualityComparer.Instance);
        var evidence = new List<IElement>(); var oldContexts = new List<IElement>();
        var frames = new List<Frame>(); var passes = new List<PassRead>();
        var cadence = new Carrier(); cadence.Attach(new Relation(Relation.Point(cadence, 2), Relation.Point(cadence, 0)));
        var passClock = new Carrier(); var beatClock = new Carrier();
        beatClock.Attach(new Relation(Relation.Point(beatClock, BeatsPerPass), Relation.Point(beatClock, 0)));
        var schedule = S(Port(passClock), Port(beatClock), Port(cadence), new CarrierLocation(passClock, Passes));
        var picture = Picture(); SetContexts(false); Record(0, 0, 0);

        // A matched calibration prelude isolates response agreement from
        // identification. Both words are supported by real accepted changes;
        // every mode receives the same supplied prior observations.
        if (scenario == "equivalent")
        {
            Calibrate(h, true); Calibrate(w, false); Record(0, 0, 0, note: "Two supplied calibration trials support distinct words with the same displacement.");
        }

        for (var pass = 1; pass <= Passes; pass++)
        {
            if (scenario == "goal-change" && pass == 4)
            {
                var before = new CarrierLocation(goal, (long)goal.Value);
                goal.Add((ulong)(inset ? 3 : 1));
                Keep(new Relation(before, new CarrierLocation(goal, (long)goal.Value)));
                SetContexts(true);
            }
            var available = (scenario != "withheld" || pass != 2) && setup.UnavailablePass != pass;
            var startGap = Gap(h.Authored, available); var startEnd = End(w.Authored, available);
            h.BeginPass(); w.BeginPass();
            var shared = 0; var provisional = 0; var trials = 0; var informative = 0;
            for (var step = 1; step <= BeatsPerPass; step++)
            {
                var beforePicture = picture;
                var hg = Observe(h, true, available); var wg = Observe(w, false, available);
                var hp = Decide(h, hg); var wp = Decide(w, wg);
                var trialLane = cadence.Value == 0 ? h : w;
                var other = ReferenceEquals(trialLane, h) ? w : h;
                var trialRead = ReferenceEquals(trialLane, h) ? hg : wg;
                var isHeight = ReferenceEquals(trialLane, h);
                Relation? trialPacket = null; var waiting = false;
                if (allowTrials)
                {
                    var selection = trialLane.Trial.Select(Offer(trialLane, 1), Offer(trialLane, -1),
                        trialLane.Trial.Need(available ? !trialRead.Shared : null, trialRead.Witness), compiled);
                    Keep(selection.Witness);
                    if (selection.Selected && selection.Request is not null)
                    {
                        trialLane.WaitBeats++;
                        if (scenario != "quiet-refused" && (scenario != "delayed" || trialLane.WaitBeats >= 3))
                        {
                            var grant = other.Quiet.Grant(selection.Request); Keep(grant.Witness);
                            if (grant.Accepted)
                            {
                                var dispatch = trialLane.Trial.Dispatch(other.Quiet, grant.Grant!); Keep(dispatch.Witness);
                                if (dispatch.Accepted) trialPacket = dispatch.Packet;
                            }
                        }
                        waiting = trialPacket is null;
                    }
                }
                if (trialPacket is not null)
                {
                    // The neighbour's actual traffic crosses its quiet gate.
                    // The negative control deliberately bypasses that route,
                    // and explicitly lists both changed inputs as confounded.
                    var quiet = other.Quiet.Filter(ReferenceEquals(other, h) ? hp.Packet : wp.Packet, compiled);
                    var otherPacket = scenario == "confounded" ? other.Machine.Publication(4) : quiet.Packet;
                    var trialStep = trialLane.Machine.Step(trialPacket, compiled);
                    var otherStep = other.Machine.Step(otherPacket, compiled);
                    trialLane.PassUses.Add(new Relation(trialLane.Trial.ArchiveSurface(), trialStep.Surface));
                    other.PassUses.Add(new Relation(quiet.Witness, otherStep.Surface));
                    picture = Picture();
                    var otherChanged = otherStep.AcceptedDelta != 0 || otherStep.PendingDelta != 0;
                    var completion = trialLane.Trial.Complete(trialLane.Machine, trialStep, other.Quiet); Keep(completion.Witness);
                    var changed = !completion.Accepted ? StructuralConstraint.Open(completion.Witness)
                        : otherChanged ? S(h.Machine.ExtentPort, w.Machine.ExtentPort) : S(trialLane.Machine.ExtentPort);
                    var observation = trialLane.Learner.Observe(Sample(trialLane, trialStep.Before.Value), Sample(trialLane, trialStep.After.Value),
                        Landmark(beforePicture, isHeight), Landmark(picture, isHeight), changed, S(trialStep.Surface, otherStep.Surface, quiet.Witness));
                    if (completion.Accepted && completion.Grant is not null) Keep(other.Quiet.Release(completion.Grant).Witness);
                    Keep(observation.Witness, quiet.Witness, trialStep.Surface, otherStep.Surface);
                    trials++; if (observation.Informative) informative++;
                    trialLane.WaitBeats = 0;
                    Record((pass - 1) * BeatsPerPass + step, pass, step,
                        isHeight ? trialStep : otherStep, isHeight ? otherStep : trialStep,
                        isHeight ? "trial" : otherChanged ? "coupled" : "quiet", isHeight ? otherChanged ? "coupled" : "quiet" : "trial",
                        isHeight, !isHeight, false, observation.Informative, available,
                        scenario == "confounded" ? "Both inputs changed; the trial retains confounding instead of identifying a motion word." : "A granted quiet interval admits one trial; only the actual accepted change can identify motion.");
                }
                else
                {
                    var hq = h.Quiet.Filter(hp.Packet, compiled); var wq = w.Quiet.Filter(wp.Packet, compiled);
                    var hs = h.Machine.Step(hq.Packet, compiled); var ws = w.Machine.Step(wq.Packet, compiled);
                    CountUse(h, hp, hs, hq.Quiet); CountUse(w, wp, ws, wq.Quiet);
                    Keep(hg.Witness, wg.Witness, hp.Witness, wp.Witness, hq.Witness, wq.Witness, hs.Surface, ws.Surface);
                    picture = Picture();
                    Record((pass - 1) * BeatsPerPass + step, pass, step, hs, ws,
                        hp.Kind, wp.Kind, false, false, waiting, false, available,
                        waiting ? "A quiet request is outstanding. Any ordinary movement remains a separate provisional/shared response, not an isolated trial."
                        : hp.Kind == "provisional" || wp.Kind == "provisional" ? "A permitted guess moves the current world. Competing explanations remain; utility is assessed at the end of the pass."
                        : "Responses use current published claims. An unresolved or unavailable read is not a zero.");
                }
                cadence.Add(1); beatClock.Add(1);

                void CountUse(Lane lane, (Relation Packet, string Kind, Relation Witness, Relation? Choice, Relation? Candidate) decision,
                    CoupledExtentMachine.StepRead actual, bool quiet)
                {
                    lane.PassUses.Add(new Relation(decision.Witness, actual.Surface));
                    if (quiet || !actual.ConsumedEvent || actual.RequestedDelta is null) return;
                    if (decision.Kind == "shared") shared++;
                    if (decision.Kind == "provisional" && decision.Choice is not null && decision.Candidate is not null)
                    {
                        provisional++; lane.LastChoice = decision.Choice;
                        lane.PassChoices.Add(decision.Candidate);
                    }
                }
            }
            var finishGap = Gap(h.Authored, available); var finishEnd = End(w.Authored, available);
            var he = EvaluatePass(h, available ? Difference(startGap.AlternativeGap, (long)goal.Value, h.Discrepancy) : null,
                available ? Difference(finishGap.AlternativeGap, (long)goal.Value, h.Discrepancy) : null,
                S(startGap.Witness, finishGap.Witness));
            var we = EvaluatePass(w, available ? Difference(startEnd.AlongDifference, 0, w.Discrepancy) : null,
                available ? Difference(finishEnd.AlongDifference, 0, w.Discrepancy) : null,
                S(startEnd.Witness, finishEnd.Witness));
            CancelPending(h); CancelPending(w);
            passClock.Add(1);
            passes.Add(new(pass, (long)goal.Value, startGap.AlternativeGap, finishGap.AlternativeGap,
                startEnd.AlongDifference, finishEnd.AlongDifference, he.Text, we.Text,
                he.Choice, we.Choice, shared, provisional, trials, informative,
                h.Memory.RecentPasses.Length + w.Memory.RecentPasses.Length, h.Memory.DiscardedCount + w.Memory.DiscardedCount));
        }
        var final = frames[^1];
        setup.ExportPolicy?.Invoke(h.Memory, true); setup.ExportPolicy?.Invoke(w.Memory, false);
        var live = S(template.Surface, publication.Surface, h.Archive(), w.Archive(), Port(goal),
            S(oldContexts.ToArray()), schedule);
        var native = captureNative ? S(live, S(evidence.ToArray())) : null;
        var primary = final.Gap == final.Goal && final.HeightTension == 0;
        return new($"{scenario}-{mode}-{family}", scenario, mode, family, frames.ToArray(), passes.ToArray(),
            primary, final.LandmarkGap == final.Goal && final.HeightTension == 0,
            primary && final.EndpointDifference == 0 && picture.EndCoincident && final.WidthTension == 0,
            GojoNumericFormat.Encode(live).Length) { NativeSurface = native };

        void Keep(params IElement[] items) { if (captureNative) evidence.AddRange(items); }
        CoupledShapeTemplate.Realization Picture()
        { var read = publication.Observe(); Keep(read.Witness); return read.Picture ?? throw new InvalidOperationException("Declared shared-reach publication failed."); }
        void SetContexts(bool changed)
        {
            foreach (var lane in new[] { h, w })
            {
                var previous = changed ? lane.Memory.ArchiveSurface() : null;
                var context = new Relation(lane.Machine.ExtentPort,
                    S(publication.Surface, new CarrierLocation(goal, (long)goal.Value), ReferenceEquals(lane, h) ? frame.Motion.Down : frame.Motion.Right));
                lane.Context = context;
                lane.Memory = mode == "fixed"
                    ? PassPolicyMemory.CreateFixed(context, lane.Discrepancy, lane.Learner.Candidates.ToArray())
                    : PassPolicyMemory.Create(context, lane.Discrepancy, lane.Learner.Candidates.ToArray());
                if (!changed) setup.InitializePolicy?.Invoke(lane.Memory, ReferenceEquals(lane, h));
                if (previous is not null) oldContexts.Add(new Relation(previous, lane.Memory.ArchiveSurface()));
            }
        }
        void Calibrate(Lane lane, bool height)
        {
            var before = picture; var step = lane.Machine.Step(lane.Machine.Publication(4), compiled);
            picture = Picture();
            var read = lane.Learner.Observe(Sample(lane, step.Before.Value), Sample(lane, step.After.Value),
                Landmark(before, height), Landmark(picture, height), S(lane.Machine.ExtentPort), step.Surface);
            Keep(read.Witness, step.Surface);
        }
        GeometricGapAlternative.Observation Gap(Relation motion, bool available = true)
        {
            if (!gaps.TryGetValue(motion, out var claim))
                gaps.Add(motion, claim = GeometricGapAlternative.CreateCalibrated(frame, Port(goal), h.Machine, frame.Motion.Down, motion));
            return claim.Observe(Section(picture.Upper), Section(picture.Lower), SectionStart(picture.Upper), SectionStart(picture.Lower), available: available, compiled: compiled);
        }
        GeometricEndpointClaim.Observation End(Relation motion, bool available = true)
        {
            if (!ends.TryGetValue(motion, out var claim))
                ends.Add(motion, claim = GeometricEndpointClaim.Create(frame, w.Machine, frame.Motion.Right, motion));
            return claim.Observe(picture.Upper.EndPort, picture.Lower.EndPort, available: available, compiled: compiled);
        }
        CandidateResponseRead.Observation Observe(Lane lane, bool height, bool available)
        {
            var offers = lane.Learner.Candidates.Select(candidate =>
            {
                Relation packet; Relation witness;
                if (height) { var read = Gap(candidate, available); packet = read.Packet; witness = read.Witness; }
                else { var read = End(candidate, available); packet = read.Packet; witness = read.Witness; }
                var gate = lane.Gate.Evaluate(packet, compiled); Keep(witness, gate.Witness);
                return (IElement)new Relation(candidate, gate.Packet);
            }).ToArray();
            return lane.Response.Observe(lane.Learner, S(offers), compiled);
        }
        (Relation Packet, string Kind, Relation Witness, Relation? Choice, Relation? Candidate) Decide(Lane lane, CandidateResponseRead.Observation read)
        {
            if (read.Shared && (!requireUnique || lane.Learner.Read().Unique is not null))
                return (read.Packet, "shared", read.Witness, null, null);
            if (allowChoice)
            {
                var choice = lane.Memory.Choose(read.Proposal);
                if (choice.Selected && choice.Receipt is not null)
                {
                    var currentChoice = FiniteChoice.Select(read.Proposal, lane.Memory.Identity, choice.Candidate!,
                        new Relation(choice.Receipt, read.Witness));
                    var selected = lane.Response.Select(lane.Learner, read.Witness, currentChoice, compiled);
                    if (selected.Resolved)
                    {
                        return (selected.Packet, "provisional", S(read.Witness, choice.Witness, selected.Witness), choice.Receipt, selected.SelectedCandidate);
                    }
                }
                Keep(choice.Witness);
            }
            return (lane.Machine.Publication(0), "unresolved", read.Witness, null, null);
        }
        Relation Offer(Lane lane, int sign)
        {
            var snapshot = lane.Machine.Snapshot();
            var owner = (Carrier)lane.Machine.ExtentPort.Right;
            bool? feasible = !owner.IsAttached(lane.Boundary) ? null : sign > 0
                ? snapshot.Value < lane.Capacity && snapshot.Tension >= 0 : snapshot.Value > 0 && snapshot.Tension <= 0;
            var possible = lane.Learner.Read();
            var vectors = possible.Possible.Select(word => frame.Vector(word, out var v) ? (Curve.Vector?)v : null).ToArray();
            bool? distinction = vectors.Any(v => v is null) ? null : vectors.Distinct().Count() > 1;
            // These are explicit local predicted-offer reads, conditional on
            // the exposed bound and candidate words; actual movement is checked later.
            var bounds = S(lane.Machine.ExtentPort, lane.Machine.PendingPort, lane.Boundary,
                new CarrierLocation(owner, lane.Capacity), snapshot.Surface);
            return lane.Trial.Offer(sign, feasible, distinction, S(bounds, possible.Witness));
        }
        (string Text, int Choice) EvaluatePass(Lane lane, IElement? before, IElement? after, Relation claimEvidence)
        {
            if (lane.LastChoice is null)
            {
                if (lane.Memory.PendingChoice is { } unused)
                {
                    var unusedResult = lane.Memory.CompletePass(lane.Context, StructuralConstraint.Open(), StructuralConstraint.Open(),
                        new Relation(unused, StructuralConstraint.Open(claimEvidence, S(lane.PassUses.ToArray()))), unused, compiled: compiled);
                    Keep(unusedResult.Witness);
                }
                return ("No provisional policy used", -1);
            }
            var same = lane.PassChoices.Distinct(ReferenceEqualityComparer.Instance).Count() == 1;
            var result = lane.Memory.CompletePass(lane.Context,
                same && before is not null ? before : StructuralConstraint.Open(claimEvidence),
                same && after is not null ? after : StructuralConstraint.Open(claimEvidence),
                S(claimEvidence, S(lane.PassChoices.Cast<IElement>().ToArray()), S(lane.PassUses.ToArray()),
                    new Relation(schedule, S(new CarrierLocation(passClock, (long)passClock.Value), new CarrierLocation(beatClock, (long)beatClock.Value)))), lane.LastChoice, compiled: compiled);
            Keep(result.Witness);
            return (!result.Complete ? "Unresolved evaluation" : result.Achieved ? "Goal reached; retain option"
                : result.Improved ? "Improved; retain option" : result.Advanced ? "Stalled/worse; try next option" : "Retained", (int)result.PhaseAfter);
        }
        void CancelPending(Lane lane)
        {
            var pending = lane.Trial.Snapshot();
            if (pending.Outstanding && !pending.Dispatched && pending.Request is not null)
                Keep(lane.Trial.Cancel(pending.Request).Witness);
            lane.WaitBeats = 0;
        }
        Relation Section(CoupledShapeTemplate.RouteRead route) => (Relation)FiniteLaw.Entries(route.Sections)[1];
        Relation SectionStart(CoupledShapeTemplate.RouteRead route)
        { var read = frame.Read(Section(route)); return new(read.Start, FiniteLaw.Entries(read.ControlWords)[0]); }
        Relation Landmark(CoupledShapeTemplate.Realization p, bool height) => height ? SectionStart(p.Lower) : p.Lower.EndPort;
        void Record(int beat, int pass, int step, CoupledExtentMachine.StepRead? hs = null, CoupledExtentMachine.StepRead? ws = null,
            string ha = "initial", string wa = "initial", bool ht = false, bool wt = false, bool waiting = false,
            bool informative = false, bool available = true, string note = "Initial actual state; no candidate has been silently selected.")
        {
            var gap = Gap(h.Authored); var end = End(w.Authored);
            var hv = h.Machine.Snapshot(); var wv = w.Machine.Snapshot();
            frames.Add(new(beat, pass, step, hv.Value, wv.Value, hv.Tension, wv.Tension,
                gap.PrimaryGap, gap.AlternativeGap, (long)goal.Value, end.AlongDifference,
                h.Learner.Read().Possible.Length, w.Learner.Read().Possible.Length, ha, wa,
                hs?.RequestedDelta, ws?.RequestedDelta, ht, wt, waiting, informative,
                gap.UsedAlternative, available, picture.Upper.Points, picture.Lower.Points, note));
        }
    }

    private sealed class Lane
    {
        public CoupledExtentMachine Machine { get; }
        public Relation Authored { get; }
        public ObservedMotionCandidates Learner { get; }
        public CandidateResponseRead Response { get; }
        public PendingAdjustmentGate Gate { get; }
        public TrialQuietPort Quiet { get; }
        public LocalTrialMachine Trial = null!;
        public PassPolicyMemory Memory = null!;
        public Carrier Discrepancy { get; } = new();
        public IElement Context = null!;
        public long Capacity { get; }
        public Relation Boundary { get; }
        public int WaitBeats;
        public Relation? LastChoice;
        public List<Relation> PassChoices { get; } = [];
        public List<IElement> PassUses { get; } = [];
        public Lane(Curve.Frame frame, CoupledExtentMachine machine, Relation authored, bool equivalent, bool reverseOptions = false)
        {
            Machine = machine; Authored = authored;
            frame.Vector(authored, out var v);
            var first = equivalent ? S(FiniteLaw.Entries(frame.Word(1, 0)).Concat(FiniteLaw.Entries(frame.Word(-1, 0))).Concat(FiniteLaw.Entries(authored)).ToArray())
                : frame.Word(checked((int)-v.X), checked((int)-v.Y));
            Learner = reverseOptions ? ObservedMotionCandidates.Create(frame, machine.ExtentPort, authored, first)
                : ObservedMotionCandidates.Create(frame, machine.ExtentPort, first, authored);
            Response = CandidateResponseRead.Create(machine, Learner);
            Gate = PendingAdjustmentGate.Create(machine); Quiet = TrialQuietPort.Create(machine);
            Boundary = (Relation)FiniteLaw.Entries(machine.Surface)[3];
            Capacity = ((CarrierLocation)((Relation)Boundary.Left).Left).Ticks;
        }
        public void BeginPass() { LastChoice = null; PassChoices.Clear(); PassUses.Clear(); }
        public Relation Archive() => S(Machine.ArchiveSurface(), Learner.ArchiveSurface(), Response.Surface,
            Gate.Surface, Quiet.ArchiveSurface(), Trial.ArchiveSurface(), Memory.ArchiveSurface(), Authored);
    }
    private static IElement? Difference(long? value, long target, Carrier basis) => value.HasValue ? new CarrierLocation(basis, checked(value.Value - target)) : null;
    private static CarrierLocation Sample(Lane lane, long value) => new((Carrier)lane.Machine.ExtentPort.Right, value);
    private static Relation Port(Carrier carrier) => new(carrier.ValueSlot, carrier);
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);

    public static int RunExperiment(string[] args)
    {
        var output = args.FirstOrDefault() ?? Path.Combine("experiments", "QualitativeFrieze", "results", "partial-decisions");
        Directory.CreateDirectory(output);
        var runs = new List<Run>();
        foreach (var scenario in Scenarios) foreach (var mode in Modes) foreach (var family in Families)
            runs.Add(Execute(scenario, mode, family));
        var comparisons = new List<object>();
        foreach (var (scenario, mode) in new[] { ("equivalent", "shared"), ("opposed", "passes"), ("capacity", "adaptive"), ("delayed", "adaptive") })
        {
            var native = Execute(scenario, mode, compiled: false);
            var compact = Execute(scenario, mode, captureNative: true);
            var agree = JsonSerializer.Serialize(native.Frames) == JsonSerializer.Serialize(compact.Frames) &&
                JsonSerializer.Serialize(native.PassResults) == JsonSerializer.Serialize(compact.PassResults);
            if (!agree) throw new InvalidOperationException("Native and compiled partial-decision traces disagree.");
            var bytes = GojoNumericFormat.Encode(compact.NativeSurface!);
            if (GojoNumericFormat.Decode(bytes) is not [Relation]) throw new InvalidOperationException("Native witness archive failed to decode.");
            File.WriteAllBytes(Path.Combine(output, scenario + ".gojo"), bytes);
            comparisons.Add(new { scenario, mode, agree, archiveBytes = bytes.Length });
        }
        var json = JsonSerializer.Serialize(new { scope = "90 supplied conditions, six complete passes each. Partial actions, bounded probes and two-evaluation local policy memories.", runs, comparisons },
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        File.WriteAllText(Path.Combine(output, "study-data.json"), json);
        var view = Path.Combine("experiments", "QualitativeFrieze", "PartialDecisionView.html");
        if (File.Exists(view)) File.WriteAllText(Path.Combine(output, "index.html"), File.ReadAllText(view).Replace("__DATA__", json));
        Console.WriteLine($"Recorded {runs.Count} conditions; joint goals {runs.Count(r => r.JointSettled)}; {Path.GetFullPath(output)}");
        return 0;
    }
}
