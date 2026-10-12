using CompositeEmergence;
using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;

namespace UtilityEcology;

/// <summary>
/// An authored local gate, not a learned policy. Its error evidence starts open.
/// Native claims compare public pre-update predictions to independently supplied
/// physical consequences. An unsupported prediction remains unaccounted pressure.
/// Rules grant bounded attention to investigate, train or test an opportunity;
/// they never manufacture support for the service's answer.
/// </summary>
public sealed class LearningGate
{
    public const int ReceiptCapacity = 8;
    public const int MinimumEvidence = 4;
    public const long LowMeanError256 = 64;
    public const long MaximumPacket = 1_000_000;

    public Carrier ResourceAxis { get; }
    public Carrier ErrorAxis { get; }
    public Carrier MeanErrorAxis { get; }
    public Carrier TrafficAxis { get; }
    public Carrier DemandAxis { get; }
    public Carrier PermissionAxis { get; }
    public Carrier TimeAxis { get; }
    public Carrier Attention { get; }
    /// <summary>Cumulative paid abstract inspection/learning work. It is a
    /// resource sink, not available capital and not measured CPU time.</summary>
    public Carrier Spent { get; }
    public long InitialCapital { get { NativeGraph.TryPoint(NativeGraph.Parts(laws, 9)[8], out var value); return value.Ticks; } }
    public Relation Root => BuildRoot();
    public Relation LastDecision { get; private set; }
    public Relation LastFeedback { get; private set; }
    public Relation LastCredit { get; private set; }
    public ulong InspectionCount => inspections.Value;
    public ulong GrantCount => grants.Value;
    public ulong UnknownObservationCount => unknownObservations.Value;
    public ulong EvidenceCount => NativeGraph.Count(errorLedger).Value;
    public LearningProgress? Progress { get; }
    public bool ProgressGated => eligibilityPolicy is not null;
    public long EpisodeRemaining => episodeBudget is null ? 0 : checked((long)episodeBudget.Value);
    public long PositiveEpisodes => positiveEpisodes is null ? 0 : checked((long)positiveEpisodes.Value);
    public long ChangeEpisodes => changeEpisodes is null ? 0 : checked((long)changeEpisodes.Value);
    public long CoverageEpisodes => coverageEpisodes is null ? 0 : checked((long)coverageEpisodes.Value);
    public long RetestEpisodes => retestEpisodes is null ? 0 : checked((long)retestEpisodes.Value);
    public long SuppressedRequests => suppressedRequests is null ? 0 : checked((long)suppressedRequests.Value);
    public Relation? LastEligibility { get; private set; }
    public bool RequestedLearning => TryDesired(out var selection) && NativeGraph.TryValue(selection, PermissionAxis, out var wanted) && wanted.Ticks == 1;
    public bool ErrorRequestedLearning
    {
        get
        {
            if (!TryDesired(out var selection)) return false;
            if (ProgressGated && ReadWitness.TryRead(selection, out var source, out _, out _, out _, out _) && source is Relation original) selection = original;
            return NativeGraph.TryValue(selection, PermissionAxis, out var wanted) && wanted.Ticks == 1;
        }
    }

    private readonly Carrier countAxis, settledPhase, opportunityPhase, clock;
    private readonly Carrier errorRole, meanRole, countRole, settledRole, opportunityRole, trafficRole, demandRole, attentionRole;
    private readonly Carrier inspections, grants, unknownObservations, forgottenFeedback, forgottenCredit;
    private readonly Relation capacity, errorLedger, policy, admissionPolicy, laws, costs;
    private readonly List<Relation> feedbackReceipts = [], creditReceipts = [];
    private Relation latestError, lastCharge;
    private readonly Carrier? episodeBudget, consumedVersion, retestPhase, positiveEpisodes, changeEpisodes, coverageEpisodes, retestEpisodes, suppressedRequests;
    private readonly Carrier[] progressRoles = [];
    private readonly Relation? eligibilityPolicy, episodePolicy, progressLaws, conjunctionPolicy;
    private Relation? lastEpisode;

    // G(axes, attention, capacity, error ledger, latest error, settled cycle,
    //   opportunity cycle, clock, roles, error/opportunity policy,
    //   paid admission policy, finite law, latest decision, latest feedback,
    //   latest credit, G(recent feedback,recent credit), counters, latest charge,
    //   cost equations). All operational aliases transport as one connected root.
    private LearningGate(Carrier resourceAxis, long initialAttention, long maximumAttention, long inspectCost,
        long learnCost, int settledPeriod, int opportunityPeriod, bool opportunity, bool always,
        Carrier? progressSource, bool progressGate, bool useProgress, bool changeInterest, int retestPeriod)
    {
        if (initialAttention < 0 || maximumAttention <= 0 || initialAttention > maximumAttention ||
            inspectCost <= 0 || learnCost <= 0 || settledPeriod <= 0 || opportunityPeriod <= 0 ||
            inspectCost > maximumAttention || learnCost > maximumAttention)
            throw new ArgumentOutOfRangeException(nameof(initialAttention), "The authored capacity, cycles and attention costs must be positive and fit the endowment.");
        ResourceAxis = resourceAxis; ErrorAxis = new(resourceAxis.UnitShape); MeanErrorAxis = new();
        TrafficAxis = new(); DemandAxis = new(resourceAxis.UnitShape); PermissionAxis = new(); TimeAxis = new(); countAxis = new();
        Attention = new(resourceAxis.UnitShape); Spent = new(resourceAxis.UnitShape);
        capacity = new(Relation.Point(Attention, maximumAttention), Relation.Point(Attention, maximumAttention));
        Attention.Attach(capacity); Attention.Add(checked((ulong)initialAttention));
        errorLedger = NativeGraph.Ledger(countAxis, ErrorAxis);
        settledPhase = Cycle(settledPeriod); opportunityPhase = Cycle(opportunityPeriod); clock = new(TimeAxis.UnitShape);
        errorRole = new(); meanRole = new(); countRole = new(); settledRole = new(); opportunityRole = new();
        trafficRole = new(); demandRole = new(); attentionRole = new();
        inspections = new(); grants = new(); unknownObservations = new(); forgottenFeedback = new(); forgottenCredit = new();
        var rules = new List<Relation>();
        if (always) rules.Add(Rule(1));
        else
        {
            rules.Add(Rule(1, Condition(errorRole, ErrorAxis, 1, MaximumPacket)));
            rules.Add(Rule(1, Condition(meanRole, MeanErrorAxis, LowMeanError256 + 1, MaximumPacket * 256)));
            if (opportunity)
                rules.Add(Rule(1, Condition(opportunityRole, opportunityPhase, 0, 0),
                    Condition(trafficRole, TrafficAxis, 1, MaximumPacket), Condition(demandRole, DemandAxis, 1, MaximumPacket)));
            rules.Add(Rule(1, Condition(errorRole, ErrorAxis, 0, 0),
                Condition(meanRole, MeanErrorAxis, 0, LowMeanError256), Condition(countRole, countAxis, MinimumEvidence, NativeGraph.MaximumLedgerCount),
                Condition(settledRole, settledPhase, 0, 0)));
            rules.Add(Rule(0, Condition(errorRole, ErrorAxis, 0, 0),
                Condition(meanRole, MeanErrorAxis, 0, LowMeanError256), Condition(countRole, countAxis, MinimumEvidence, NativeGraph.MaximumLedgerCount)));
            // Open or insufficient support explicitly asks for a paid test.
            // Earlier failed support is retained by Select; it is not sensor zero.
            rules.Add(Rule(1));
        }
        policy = SupportDecisionSurface.Create(new Carrier(), rules.ToArray());
        admissionPolicy = SupportDecisionSurface.Create(new Carrier(),
            Rule(1, Condition(attentionRole, Attention, learnCost, maximumAttention)), Rule(0));
        laws = NativeGraph.G(Relation.Point(countAxis, MinimumEvidence), Relation.Point(MeanErrorAxis, LowMeanError256),
            Relation.Point(TimeAxis, settledPeriod), Relation.Point(TimeAxis, opportunityPeriod),
            Relation.Point(ResourceAxis, inspectCost), Relation.Point(ResourceAxis, learnCost),
            Relation.Point(countAxis, ReceiptCapacity), Relation.Segment(ResourceAxis, -MaximumPacket / 2, MaximumPacket / 2),
            Relation.Point(ResourceAxis, initialAttention));
        costs = NativeGraph.G(Equation.Affine(TimeAxis, 0, 1, Attention, 0, inspectCost),
            Equation.Affine(PermissionAxis, 0, 1, Attention, 0, learnCost));
        latestError = NativeGraph.Open(new Placeholder(), Relation.Segment(ErrorAxis, 0, MaximumPacket));
        LastDecision = LastFeedback = LastCredit = lastCharge = NativeGraph.Open(new Placeholder(), capacity);
        if (progressSource is not null)
        {
            if (retestPeriod < 1) throw new ArgumentOutOfRangeException(nameof(retestPeriod));
            Progress = new LearningProgress(ResourceAxis, ErrorAxis, progressSource);
            episodeBudget = new(); episodeBudget.Attach(new Relation(Relation.Point(episodeBudget, 4), Relation.Point(episodeBudget, 4)));
            consumedVersion = new(Progress.VersionSite.UnitShape); retestPhase = Cycle(retestPeriod);
            positiveEpisodes = new(); changeEpisodes = new(); coverageEpisodes = new(); retestEpisodes = new(); suppressedRequests = new();
            progressRoles = Enumerable.Range(0, 8).Select(_ => new Carrier()).ToArray();
            progressLaws = NativeGraph.G(Relation.Point(grants, 16), Relation.Point(episodeBudget, 4),
                Relation.Point(Progress.ProgressAxis, 128), Relation.Point(Progress.CoverageAxis, 128), Relation.Point(retestPhase, retestPeriod));
            lastEpisode = LastEligibility = NativeGraph.Open(progressSource, progressLaws);
            if (progressGate)
            {
                var r = progressRoles;
                Relation Episode(Carrier counter, params Relation[] additional) => SupportDecisionSurface.Rule(new Carrier(),
                    NativeGraph.G(new CarrierLocation(episodeBudget, 4), new CarrierLocation(counter, 1)),
                    new[] { Condition(r[0], episodeBudget, 0, 0), Condition(r[1], grants, 16, MaximumPacket) }.Concat(additional).ToArray());
                var episodeRules = new List<Relation>();
                if (useProgress)
                {
                    episodeRules.Add(Episode(positiveEpisodes, Condition(r[2], Progress.VersionSite, 1, MaximumPacket),
                        Condition(r[3], Progress.ProgressAxis, 128, 512), Condition(r[4], Progress.CoverageAxis, 0, 256)));
                    if (changeInterest)
                        episodeRules.Add(Episode(changeEpisodes, Condition(r[2], Progress.VersionSite, 1, MaximumPacket),
                            Condition(r[3], Progress.ProgressAxis, -512, -128), Condition(r[4], Progress.CoverageAxis, 0, 256)));
                    episodeRules.Add(Episode(coverageEpisodes, Condition(r[2], Progress.VersionSite, 1, MaximumPacket),
                        Condition(r[4], Progress.CoverageAxis, 128, 256)));
                }
                episodeRules.Add(Episode(retestEpisodes, Condition(r[5], retestPhase, 0, 0)));
                episodePolicy = SupportDecisionSurface.Create(new Carrier(), episodeRules.ToArray());
                eligibilityPolicy = SupportDecisionSurface.Create(new Carrier(),
                    Rule(1, Condition(r[1], grants, 0, 15)), Rule(1, Condition(r[0], episodeBudget, 1, 4)), Rule(0));
                conjunctionPolicy = SupportDecisionSurface.Create(new Carrier(),
                    Rule(1, Condition(r[6], PermissionAxis, 1, 1), Condition(r[7], PermissionAxis, 1, 1)), Rule(0));
            }
        }
    }

    private LearningGate(Relation root)
    {
        var fields = NativeGraph.Parts(root);
        if (fields.Length is not (19 or 20)) throw new ArgumentException("The gate lacks its native contract.", nameof(root));
        var axes = NativeGraph.Parts((Relation)fields[0], 8).Cast<Carrier>().ToArray();
        ResourceAxis = axes[0]; ErrorAxis = axes[1]; MeanErrorAxis = axes[2]; TrafficAxis = axes[3];
        DemandAxis = axes[4]; PermissionAxis = axes[5]; TimeAxis = axes[6]; countAxis = axes[7];
        Attention = (Carrier)fields[1]; capacity = (Relation)fields[2]; errorLedger = (Relation)fields[3]; latestError = (Relation)fields[4];
        settledPhase = (Carrier)fields[5]; opportunityPhase = (Carrier)fields[6]; clock = (Carrier)fields[7];
        var roles = NativeGraph.Parts((Relation)fields[8], 8).Cast<Carrier>().ToArray();
        errorRole = roles[0]; meanRole = roles[1]; countRole = roles[2]; settledRole = roles[3]; opportunityRole = roles[4];
        trafficRole = roles[5]; demandRole = roles[6]; attentionRole = roles[7];
        policy = (Relation)fields[9]; admissionPolicy = (Relation)fields[10]; laws = (Relation)fields[11];
        LastDecision = (Relation)fields[12]; LastFeedback = (Relation)fields[13]; LastCredit = (Relation)fields[14];
        var receipts = NativeGraph.Parts((Relation)fields[15], 2);
        feedbackReceipts.AddRange(NativeGraph.Parts((Relation)receipts[0]).Cast<Relation>());
        creditReceipts.AddRange(NativeGraph.Parts((Relation)receipts[1]).Cast<Relation>());
        var counters = NativeGraph.Parts((Relation)fields[16], 6).Cast<Carrier>().ToArray();
        inspections = counters[0]; grants = counters[1]; unknownObservations = counters[2]; forgottenFeedback = counters[3]; forgottenCredit = counters[4];
        Spent = counters[5];
        lastCharge = (Relation)fields[17]; costs = (Relation)fields[18];
        if (fields.Length == 20)
        {
            var extension = NativeGraph.Parts((Relation)fields[19], 12);
            Progress = LearningProgress.FromRoot((Relation)extension[0]);
            eligibilityPolicy = extension[1] as Relation; episodePolicy = extension[2] as Relation;
            progressRoles = NativeGraph.Parts((Relation)extension[3], 8).Cast<Carrier>().ToArray();
            episodeBudget = (Carrier)extension[4]; consumedVersion = (Carrier)extension[5]; retestPhase = (Carrier)extension[6];
            var progressCounters = NativeGraph.Parts((Relation)extension[7], 5).Cast<Carrier>().ToArray();
            positiveEpisodes = progressCounters[0]; changeEpisodes = progressCounters[1]; coverageEpisodes = progressCounters[2]; retestEpisodes = progressCounters[3]; suppressedRequests = progressCounters[4];
            lastEpisode = (Relation)extension[8]; LastEligibility = (Relation)extension[9]; progressLaws = (Relation)extension[10];
            conjunctionPolicy = extension[11] as Relation;
        }
    }

    public static LearningGate Create(Carrier resourceAxis, long initialAttention, long capacity = 4096,
        long inspectCost = 1, long learnCost = 1, int settledPeriod = 16, int opportunityPeriod = 8,
        bool opportunity = true, bool always = false, Carrier? progressSource = null,
        bool progressGate = false, bool useProgress = true, bool changeInterest = false, int retestPeriod = 16) =>
        new(resourceAxis, initialAttention, capacity, inspectCost, learnCost, settledPeriod, opportunityPeriod, opportunity, always,
            progressSource, progressGate, useProgress, changeInterest, retestPeriod);
    public static LearningGate FromRoot(Relation root) => new(root);

    /// <summary>A pre-update claim: neither unknown prediction nor unknown
    /// consequence enters the known-error ledger as a successful zero.</summary>
    public Relation ObservePrediction(Relation prediction, Relation independentPhysicalResource)
    {
        var identity = NativeGraph.Identity(independentPhysicalResource);
        if (Seen(feedbackReceipts, identity)) return LastFeedback = NativeGraph.Open(identity, errorLedger);
        var capturedPrediction = NativeGraph.CapturePublication(prediction, ResourceAxis);
        var capturedPhysical = NativeGraph.CapturePublication(independentPhysicalResource, ResourceAxis);
        var source = NativeGraph.G(capturedPrediction, capturedPhysical);
        Relation update;
        if (!NativeGraph.TryValue(capturedPrediction, ResourceAxis, out var predicted) ||
            !NativeGraph.TryValue(capturedPhysical, ResourceAxis, out var actual) ||
            predicted.Ticks is < -MaximumPacket / 2 or > MaximumPacket / 2 || actual.Ticks is < -MaximumPacket / 2 or > MaximumPacket / 2)
        {
            latestError = NativeGraph.Open(source, Relation.Segment(ErrorAxis, 0, MaximumPacket));
            unknownObservations.Add(1); update = latestError;
        }
        else
        {
            var claim = OperationClaim.Equality(new Carrier(), predicted, actual, ErrorAxis);
            if (!OperationClaim.TryReadResolvedViolation(claim, out var magnitude))
            { latestError = NativeGraph.Open(source, claim); unknownObservations.Add(1); update = latestError; }
            else
            {
                latestError = ReadWitness.Create(new Carrier(), source, claim, new CarrierLocation(ErrorAxis, magnitude.Ticks),
                    NativeGraph.G(), claim);
                update = NativeGraph.Update(errorLedger, magnitude.Ticks);
            }
        }
        LastFeedback = ReadWitness.Create(new Carrier(), identity, errorLedger, latestError,
            NativeGraph.G(update), NativeGraph.G(source, update, ReadError()));
        Progress?.Observe(latestError, independentPhysicalResource);
        Retain(feedbackReceipts, ReadWitness.Create(new Carrier(), identity, errorLedger, new Placeholder(), NativeGraph.G(), NativeGraph.G()), forgottenFeedback);
        return LastFeedback;
    }

    /// <summary>The mean remains open until enough actual comparisons exist.
    /// It is a read of retained native evidence, not an uncertainty field.</summary>
    public Relation ReadError()
    {
        if (EvidenceCount < MinimumEvidence) return NativeGraph.Open(NativeGraph.CaptureLedger(errorLedger),
            Relation.Segment(countAxis, MinimumEvidence, NativeGraph.MaximumLedgerCount));
        return NativeGraph.Score(errorLedger, MeanErrorAxis);
    }

    /// <summary>Charges one inspection and, only if granted, one learning unit.
    /// Traffic and funded demand are advertised coarse counts, not hidden raw
    /// features. They motivate an experiment without refining the service.</summary>
    public Relation Decide(Relation trafficPublication, Relation demandPublication)
    {
        var start = checked((long)clock.Value); var before = CaptureBudget();
        var inspectLaw = (Relation)NativeGraph.Parts(costs, 2)[0];
        var inspectPrediction = Equation.Predict(inspectLaw, new CarrierLocation(TimeAxis, 1), Attention);
        Equation.TryReadPrediction(inspectPrediction, out var inspect, out var inspectResidue);
        var affordable = OperationClaim.AtLeast(new Carrier(), new CarrierLocation(Attention, checked((long)Attention.Value)),
            new CarrierLocation(Attention, inspect.Ticks), new Carrier());
        var inspectionAllowed = NativeGraph.Pass(affordable) && Attention.Tension >= 0;
        var paidWork = new List<IElement>();
        Relation selection; Relation admission; var permission = 0L;
        if (inspectionAllowed)
        {
            var inspectionTransfer = new Relation(Relation.Point(Attention, inspect.Ticks), Relation.Point(Spent, inspect.Ticks));
            var inspectionSettlement = Carrier.ReceiveTogether(NativeGraph.G(new CarrierLocation(Attention, -inspect.Ticks), new CarrierLocation(Spent, inspect.Ticks),
                new CarrierLocation(inspections, 1)));
            paidWork.Add(NativeGraph.G(inspectionTransfer, inspectionSettlement));
            selection = SupportDecisionSurface.Select(new Carrier(), policy,
                Binding(errorRole, latestError), Binding(meanRole, ReadError()),
                Binding(countRole, P(countAxis, checked((long)EvidenceCount))),
                Binding(settledRole, P(settledPhase, checked((long)settledPhase.Value))),
                Binding(opportunityRole, P(opportunityPhase, checked((long)opportunityPhase.Value))),
                Binding(trafficRole, NativeGraph.CapturePublication(trafficPublication, TrafficAxis)),
                Binding(demandRole, NativeGraph.CapturePublication(demandPublication, DemandAxis)));
            if (ProgressGated)
            {
                var original = selection;
                var eligibility = ReadProgressEligibility();
                var wanted = NativeGraph.TryValue(original, PermissionAxis, out var requested) && requested.Ticks == 1;
                var allowed = NativeGraph.TryValue(eligibility, PermissionAxis, out var eligible) && eligible.Ticks == 1;
                if (wanted && !allowed) suppressedRequests!.Add(1);
                var combined = SupportDecisionSurface.Select(new Carrier(), conjunctionPolicy!,
                    Binding(progressRoles[6], original), Binding(progressRoles[7], eligibility));
                IElement value = NativeGraph.TryValue(combined, PermissionAxis, out var request) ? request : new Placeholder();
                selection = ReadWitness.Create(new Carrier(), original, conjunctionPolicy!, value,
                    NativeGraph.G(original, eligibility), NativeGraph.G(combined, eligibility, lastEpisode!));
            }
            admission = SupportDecisionSurface.Select(new Carrier(), admissionPolicy,
                Binding(attentionRole, P(Attention, checked((long)Attention.Value))));
            if (NativeGraph.TryValue(selection, PermissionAxis, out var desired) && desired.Ticks == 1 &&
                NativeGraph.TryValue(admission, PermissionAxis, out var funded) && funded.Ticks == 1 && Attention.Tension >= 0)
            {
                var learnLaw = (Relation)NativeGraph.Parts(costs, 2)[1];
                var learnPrediction = Equation.Predict(learnLaw, new CarrierLocation(PermissionAxis, 1), Attention);
                Equation.TryReadPrediction(learnPrediction, out var cost, out _);
                var learningTransfer = new Relation(Relation.Point(Attention, cost.Ticks), Relation.Point(Spent, cost.Ticks));
                var packets = new List<IElement> { new CarrierLocation(Attention, -cost.Ticks), new CarrierLocation(Spent, cost.Ticks), new CarrierLocation(grants, 1) };
                if (ProgressGated && episodeBudget!.Value > 0) packets.Add(new CarrierLocation(episodeBudget, -1));
                var learningSettlement = Carrier.ReceiveTogether(NativeGraph.G(packets.ToArray()));
                paidWork.Add(Progress is null ? NativeGraph.G(learnLaw, learnPrediction, learningTransfer, learningSettlement) :
                    NativeGraph.G(learnLaw, learnPrediction, learningTransfer, NativeGraph.G(packets.ToArray()), learningSettlement));
                permission = 1;
                admission = ReadWitness.Create(new Carrier(), admission, learnLaw, new CarrierLocation(PermissionAxis, 1),
                    NativeGraph.G(), NativeGraph.G(learnPrediction, admission));
            }
            retestPhase?.Add(1);
        }
        else { selection = NativeGraph.Open(latestError, policy); admission = NativeGraph.Open(before, affordable); }
        clock.Add(1); settledPhase.Add(1); opportunityPhase.Add(1);
        lastCharge = TimeStepSequence.Step(new Carrier(), clock, start, checked((long)clock.Value), before,
            NativeGraph.G(inspectLaw, inspectPrediction, affordable, selection, admission, NativeGraph.G(paidWork.ToArray())), CaptureBudget(),
            NativeGraph.G(inspectResidue, Attention.TensionSlot));
        LastDecision = ReadWitness.Create(new Carrier(), NativeGraph.G(latestError, ReadError()), policy,
            new CarrierLocation(PermissionAxis, permission), NativeGraph.G(selection, admission), lastCharge);
        return LastDecision;
    }

    /// <summary>Physical budget packets settle existing debt first. Duplicate
    /// exclusion covers only the declared bounded receipt window.</summary>
    public Relation CreditAttention(Relation independentPhysicalResource)
    {
        var identity = NativeGraph.Identity(independentPhysicalResource);
        if (Seen(creditReceipts, identity) || !NativeGraph.TryValue(independentPhysicalResource, ResourceAxis, out var resource) ||
            resource.Ticks is < -MaximumPacket or > MaximumPacket)
            return LastCredit = NativeGraph.Open(identity, capacity);
        var before = CaptureBudget(); var law = Equation.Affine(ResourceAxis, 0, 1, Attention, 0, 1);
        var prediction = Equation.Predict(law, resource, Attention);
        Equation.TryReadPrediction(prediction, out var packet, out var residue);
        var settlement = Carrier.ReceiveTogether(NativeGraph.G(new CarrierLocation(Attention, packet.Ticks)));
        LastCredit = ReadWitness.Create(new Carrier(), identity, law, CaptureBudget(), NativeGraph.G(residue, Attention.TensionSlot),
            NativeGraph.G(before, NativeGraph.CapturePublication(independentPhysicalResource, ResourceAxis), prediction, settlement));
        Retain(creditReceipts, LastCredit, forgottenCredit); return LastCredit;
    }

    public bool TryPermission(Relation publication, out bool permitted)
    {
        permitted = false;
        if (!NativeGraph.TryValue(publication, PermissionAxis, out var value) || value.Ticks is < 0 or > 1) return false;
        permitted = value.Ticks == 1; return true;
    }

    private Relation BuildRoot()
    {
        var fields = new List<IElement> { NativeGraph.G(ResourceAxis, ErrorAxis, MeanErrorAxis, TrafficAxis, DemandAxis, PermissionAxis, TimeAxis, countAxis),
        Attention, capacity, errorLedger, latestError, settledPhase, opportunityPhase, clock,
        NativeGraph.G(errorRole, meanRole, countRole, settledRole, opportunityRole, trafficRole, demandRole, attentionRole),
        policy, admissionPolicy, laws, LastDecision, LastFeedback, LastCredit,
        NativeGraph.G(NativeGraph.G(feedbackReceipts.Cast<IElement>().ToArray()), NativeGraph.G(creditReceipts.Cast<IElement>().ToArray())),
        NativeGraph.G(inspections, grants, unknownObservations, forgottenFeedback, forgottenCredit, Spent), lastCharge, costs };
        if (Progress is not null)
            fields.Add(NativeGraph.G(Progress.Root, eligibilityPolicy ?? (IElement)new Placeholder(), episodePolicy ?? (IElement)new Placeholder(),
                NativeGraph.G(progressRoles), episodeBudget!, consumedVersion!, retestPhase!,
                NativeGraph.G(positiveEpisodes!, changeEpisodes!, coverageEpisodes!, retestEpisodes!, suppressedRequests!), lastEpisode!, LastEligibility!, progressLaws!,
                conjunctionPolicy ?? (IElement)new Placeholder()));
        return NativeGraph.G(fields.ToArray());
    }

    private Relation ReadProgressEligibility()
    {
        var r = progressRoles; var budget = episodeBudget!; var progress = Progress!;
        var versionSite = progress.VersionSite;
        var difference = Equation.Sum(versionSite, Equation.WeightedPort(versionSite, 0, 1, 0, 1),
            Equation.WeightedPort(consumedVersion!, 0, 1, 0, -1));
        var versionInputs = new Relation(new CarrierLocation(versionSite, checked((long)versionSite.Value)),
            new CarrierLocation(consumedVersion!, checked((long)consumedVersion!.Value)));
        var newVersion = Equation.PredictWitness(new Carrier(), difference, versionInputs, versionSite);
        var proposal = SupportDecisionSurface.Select(new Carrier(), episodePolicy!,
            Binding(r[0], P(budget, checked((long)budget.Value))), Binding(r[1], P(grants, checked((long)grants.Value))),
            Binding(r[2], newVersion), Binding(r[3], progress.ReadProgress()), Binding(r[4], progress.ReadCoverageChange()),
            Binding(r[5], P(retestPhase!, checked((long)retestPhase!.Value))));
        if (SupportDecisionSurface.TryReadSelection(proposal, out _, out var response, out _, out _) && response is Relation packet &&
            packet.TryReadOrderedGroup(out var parts) && parts.Length == 2 && parts[0] is CarrierLocation amount && amount.Carrier.SameIdentityAs(budget) &&
            amount.Ticks == 4 && parts[1] is CarrierLocation counter && counter.Ticks == 1 &&
            new[] { positiveEpisodes, changeEpisodes, coverageEpisodes, retestEpisodes }.Any(c => c!.SameIdentityAs(counter.Carrier)))
        {
            var before = new CarrierLocation(budget, checked((long)budget.Value));
            var priorVersion = new CarrierLocation(consumedVersion, checked((long)consumedVersion.Value));
            var settlement = Carrier.ReceiveTogether(NativeGraph.G(new CarrierLocation(budget, amount.Ticks), counter));
            consumedVersion.Set(progress.Version);
            lastEpisode = ReadWitness.Create(new Carrier(), proposal, progressLaws!, new CarrierLocation(budget, checked((long)budget.Value)),
                NativeGraph.G(before, priorVersion), NativeGraph.G(proposal, settlement, new CarrierLocation(consumedVersion, checked((long)consumedVersion.Value))));
        }
        return LastEligibility = SupportDecisionSurface.Select(new Carrier(), eligibilityPolicy!,
            Binding(r[0], P(budget, checked((long)budget.Value))), Binding(r[1], P(grants, checked((long)grants.Value))));
    }
    private bool TryDesired(out Relation selection)
    {
        selection = null!;
        return ReadWitness.TryRead(LastDecision, out _, out _, out _, out var residue, out _) && residue is Relation group &&
            group.TryReadOrderedGroup(out var parts) && parts.Length == 2 && parts[0] is Relation chosen && (selection = chosen) is not null;
    }
    private Relation Rule(long permission, params Relation[] conditions) => SupportDecisionSurface.Rule(new Carrier(), new CarrierLocation(PermissionAxis, permission), conditions);
    private static Relation Condition(Carrier role, Carrier axis, long lower, long upper) =>
        SupportDecisionSurface.Condition(new Carrier(), role, Relation.Segment(axis, lower, upper));
    private static Relation Binding(Carrier role, Relation publication) => SupportDecisionSurface.Binding(new Carrier(), role, publication);
    private static Relation P(Carrier axis, long value) => NativeGraph.Publication(axis, value, new Carrier());
    private Relation CaptureBudget()
    {
        var fields = new List<IElement> { new CarrierLocation(Attention, checked((long)Attention.Value)), Attention.TensionSlot,
        new CarrierLocation(Spent, checked((long)Spent.Value)),
        new CarrierLocation(clock, checked((long)clock.Value)), new CarrierLocation(settledPhase, checked((long)settledPhase.Value)),
        new CarrierLocation(opportunityPhase, checked((long)opportunityPhase.Value)) };
        if (Progress is not null) fields.Add(NativeGraph.G(new CarrierLocation(episodeBudget!, checked((long)episodeBudget!.Value)),
            new CarrierLocation(consumedVersion!, checked((long)consumedVersion!.Value)), new CarrierLocation(retestPhase!, checked((long)retestPhase!.Value)),
            new CarrierLocation(grants, checked((long)grants.Value))));
        return NativeGraph.G(fields.ToArray());
    }
    private static Carrier Cycle(int period) { var cycle = new Carrier(); cycle.Attach(new Relation(Relation.Point(cycle, period), Relation.Point(cycle, 0))); return cycle; }
    private static bool Seen(IEnumerable<Relation> receipts, IElement identity) => receipts.Any(receipt =>
        ReadWitness.TryRead(receipt, out var selected, out _, out _, out _, out _) && ReferenceEquals(selected, identity));
    private static void Retain(List<Relation> receipts, Relation receipt, Carrier forgotten)
    { receipts.Add(receipt); if (receipts.Count > ReceiptCapacity) { receipts.RemoveAt(0); forgotten.Add(1); } }
}
