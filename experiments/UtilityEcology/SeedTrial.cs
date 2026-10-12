using CompositeEmergence;
using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using PrimitiveLearning;

namespace UtilityEcology;

/// <summary>A single finite paid experiment. Actual feedback and paired reads
/// remain connected; projected alternative receipts never enter an economy.
/// A frozen model may still record operational Predict witnesses.</summary>
public sealed class SeedTrial
{
    public AdaptiveOrdinal Candidate { get; }
    public Carrier Purse { get; }
    public Carrier ResourceAxis { get; }
    public Carrier ActionAxis { get; }
    public Carrier AdviceAxis { get; }
    public Carrier LabelAxis { get; }
    private readonly Carrier source, training, validation, supported, closed, admitted, trialId, seedFunded, comparisonEnabled;
    private readonly Relation limits, payoff, admissionPolicy, gainLedger, origin;
    private readonly Carrier validationRole, supportRole, gainRole, responseAxis, comparisonRole;
    private readonly List<Relation> records;
    private Relation lastRead, decision, pending;
    public long Id => checked((long)trialId.Value);
    public bool SeedFunded => seedFunded.Value == 1;
    public int TrainingCount => checked((int)training.Value);
    public int ValidationCount => checked((int)validation.Value);
    public int SupportedCount => checked((int)supported.Value);
    public bool Closed => closed.Value == 1;
    public bool Accepted => Closed && admitted.Value == 1;
    public bool Validating => !Closed && TrainingCount >= TrainingEvents;
    public int TrainingEvents => checked((int)Point(LimitFields[0]).Ticks);
    public int ValidationEvents => checked((int)Point(LimitFields[1]).Ticks);
    public long Reserved => Point(LimitFields[2]).Ticks;
    public long WorkSpent => Read(ReadWork());
    public long Gain => Read(ReadGain());
    public long NetGain => Read(ReadNetGain());
    public Relation LastDecision => decision;
    public IReadOnlyList<Relation> Records => records;
    public Relation Pending => pending;
    public long? CandidateValidationAdvice => PendingValue(3);
    public long? IncumbentValidationAdvice => PendingValue(2);
    public string Reason => !Closed ? "pending" : Accepted ? "accepted" : ValidationCount < ValidationEvents ? "incomplete-budget" : SupportedCount < Point(LimitFields[3]).Ticks ? "coverage" : "gain";
    private IElement[] LimitFields => NativeGraph.Parts(limits, 4);

    // trial = G(axes,candidate,purse,limits,counters,projected gain ledger,
    //           payoff correspondences,admission roles/policy,records,last read,decision).
    public Relation Root => NativeGraph.G(NativeGraph.G(source, ResourceAxis, ActionAxis, AdviceAxis, LabelAxis),
        Candidate.Root, Purse, limits, NativeGraph.G(training, validation, supported, closed, admitted, trialId, seedFunded, comparisonEnabled),
        gainLedger, payoff, NativeGraph.G(validationRole, supportRole, gainRole, responseAxis, comparisonRole), admissionPolicy,
        NativeGraph.G(records.Cast<IElement>().ToArray()), lastRead, decision, pending, origin);

    public SeedTrial(AdaptiveOrdinal candidate, Carrier source, Carrier purse, Carrier resourceAxis,
        Carrier actionAxis, Carrier adviceAxis, Carrier labelAxis, long id, bool initialSeed,
        long reserve = 320, int trainingEvents = 16, int validationEvents = 32, bool compareBeforeAdmission = true,
        IElement? priorModel = null, Relation? fundingWitness = null, Relation? constructionWitness = null)
    {
        if (reserve < 16 || trainingEvents is < 1 or > 32 || validationEvents is < 1 or > 32 || id < 1)
            throw new ArgumentOutOfRangeException(nameof(reserve));
        Candidate = candidate; this.source = source; Purse = purse; ResourceAxis = resourceAxis;
        ActionAxis = actionAxis; AdviceAxis = adviceAxis; LabelAxis = labelAxis;
        training = new(); validation = new(); supported = new(); closed = new(); admitted = new();
        trialId = new(); trialId.Add(checked((ulong)id)); seedFunded = new(); if (initialSeed) seedFunded.Add(1);
        comparisonEnabled = new(); if (compareBeforeAdmission) comparisonEnabled.Add(1);
        limits = NativeGraph.G(Relation.Point(training, trainingEvents), Relation.Point(validation, validationEvents),
            Relation.Point(resourceAxis, reserve), Relation.Point(supported, (3 * validationEvents + 3) / 4));
        gainLedger = NativeGraph.Ledger(validation, resourceAxis);
        // The two opposed actions have this explicitly supplied payout ratio.
        // +8 maps to -12; -12 maps to +8. Rare payouts share the same ratio.
        payoff = NativeGraph.G(Equation.Affine(resourceAxis, 0, 8, resourceAxis, 0, -12),
            Equation.Affine(resourceAxis, 0, -12, resourceAxis, 0, 8), Relation.Segment(actionAxis, -1, 1));
        validationRole = new(); supportRole = new(); gainRole = new(); responseAxis = new(); comparisonRole = new();
        admissionPolicy = SupportDecisionSurface.Create(new Carrier(),
            SupportDecisionSurface.Rule(new Carrier(), new CarrierLocation(responseAxis, 1),
                SupportDecisionSurface.Condition(new Carrier(), comparisonRole, Relation.Segment(comparisonEnabled, 0, 0)),
                SupportDecisionSurface.Condition(new Carrier(), validationRole, Relation.Segment(validation, validationEvents, validationEvents))),
            SupportDecisionSurface.Rule(new Carrier(), new CarrierLocation(responseAxis, 1),
                SupportDecisionSurface.Condition(new Carrier(), comparisonRole, Relation.Segment(comparisonEnabled, 1, 1)),
                SupportDecisionSurface.Condition(new Carrier(), validationRole, Relation.Segment(validation, validationEvents, validationEvents)),
                SupportDecisionSurface.Condition(new Carrier(), supportRole, Relation.Segment(supported, (3 * validationEvents + 3) / 4, validationEvents)),
                SupportDecisionSurface.Condition(new Carrier(), gainRole, Relation.Segment(resourceAxis, 1, 1_000_000))),
            SupportDecisionSurface.Rule(new Carrier(), new CarrierLocation(responseAxis, 0)));
        records = []; lastRead = decision = pending = NativeGraph.Open(source, limits);
        origin = NativeGraph.G(priorModel ?? new Placeholder(), fundingWitness ?? NativeGraph.Open(purse, limits),
            constructionWitness ?? NativeGraph.Open(purse, Relation.Point(resourceAxis, 16)));
    }

    private SeedTrial(Relation root)
    {
        var f = NativeGraph.Parts(root, 14); var axes = NativeGraph.Parts((Relation)f[0], 5).Cast<Carrier>().ToArray();
        source = axes[0]; ResourceAxis = axes[1]; ActionAxis = axes[2]; AdviceAxis = axes[3]; LabelAxis = axes[4];
        Candidate = AdaptiveOrdinal.FromRoot((Relation)f[1]); Purse = (Carrier)f[2]; limits = (Relation)f[3];
        var c = NativeGraph.Parts((Relation)f[4], 8).Cast<Carrier>().ToArray();
        training = c[0]; validation = c[1]; supported = c[2]; closed = c[3]; admitted = c[4]; trialId = c[5]; seedFunded = c[6];
        comparisonEnabled = c[7];
        gainLedger = (Relation)f[5]; payoff = (Relation)f[6];
        var roles = NativeGraph.Parts((Relation)f[7], 5).Cast<Carrier>().ToArray();
        validationRole = roles[0]; supportRole = roles[1]; gainRole = roles[2]; responseAxis = roles[3];
        comparisonRole = roles[4];
        admissionPolicy = (Relation)f[8]; records = NativeGraph.Parts((Relation)f[9]).Cast<Relation>().ToList();
        lastRead = (Relation)f[10]; decision = (Relation)f[11]; pending = (Relation)f[12];
        origin = (Relation)f[13];
    }
    public static SeedTrial FromRoot(Relation root) => new(root);

    /// <summary>Receives only a genuine same-sample, bound physical event. An
    /// unsupported event remains open and cannot advance training or validation.</summary>
    public Relation Observe(Relation sample, Relation command, Relation bound,
        Func<Carrier, Relation, Relation, Relation> comparePaid, ServiceEconomy economy)
    {
        if (Closed || !TryPhysical(sample, command, bound, out var direction) ||
            records.Any(r => ReferenceEquals(NativeGraph.Parts(r, 6)[0], sample) ||
                ReferenceEquals(NativeGraph.Identity((Relation)NativeGraph.Parts(r, 6)[2]), NativeGraph.Identity(bound))))
            return lastRead = NativeGraph.Open(NativeGraph.G(sample, command, bound), limits);
        Relation Compare(Relation a, Relation b) => comparePaid(Purse, a, b);
        if (!Validating)
        {
            if (!economy.BurnWork(Purse, 1, out var payment)) return Reject(NativeGraph.Open(Purse, limits));
            NativeGraph.TryValue(direction, AdviceAxis, out var actualDirection);
            var label = Equation.PredictWitness(new Carrier(), Equation.Affine(AdviceAxis, -1, 1, LabelAxis, 0, 1), actualDirection, LabelAxis);
            var update = Candidate.Observe(sample, label, Compare, true);
            training.Add(1);
            var row = NativeGraph.G(sample, command, bound, new Placeholder(), new Placeholder(), NativeGraph.G(payment, update, direction));
            records.Add(row);
            lastRead = ReadWitness.Create(new Carrier(), sample, limits, new CarrierLocation(training, TrainingCount),
                NativeGraph.G(), row);
            if (Purse.Value == 0) Reject(NativeGraph.Open(Purse, limits));
            return lastRead;
        }
        if (!pending.TryReadOrderedGroup(out var prepared) || prepared.Length != 6 ||
            !ReferenceEquals(prepared[0], sample) || !ReferenceEquals(prepared[1], command))
            return lastRead = NativeGraph.Open(NativeGraph.G(sample, command, bound), pending);
        var incumbent = (Relation)prepared[2]; var candidate = (Relation)prepared[3];
        var prediction = (Relation)prepared[4]; var reads = (Relation)prepared[5];
        var known = NativeGraph.TryValue(candidate, AdviceAxis, out var value) && value.Ticks is -1 or 1;
        var incumbentScore = ProjectedGross(incumbent, command, bound);
        // Missing candidate interpretation retains the incumbent decision,
        // including any difference in comparison-query availability. Reusing
        // this same read makes the projected incremental gain exactly zero.
        var candidateScore = known ? ProjectedGross(candidate, command, bound) : incumbentScore;
        var gain = Difference(candidateScore, incumbentScore);
        if (!NativeGraph.TryValue(gain, ResourceAxis, out var amount)) return Reject(gain);
        var updateGain = NativeGraph.Update(gainLedger, amount.Ticks);
        validation.Add(1); if (known) supported.Add(1);
        var record = NativeGraph.G(sample, command, bound, NativeGraph.CapturePublication(incumbent, AdviceAxis),
            NativeGraph.CapturePublication(candidate, AdviceAxis),
            NativeGraph.G(reads, prediction, incumbentScore, candidateScore, gain, updateGain));
        records.Add(record); lastRead = ReadWitness.Create(new Carrier(), sample, limits,
            new CarrierLocation(validation, ValidationCount), NativeGraph.G(), record);
        if (ValidationCount == ValidationEvents) Finish();
        else if (Purse.Value == 0) Reject(NativeGraph.Open(Purse, limits));
        return lastRead;
    }

    /// <summary>Must run before physical execution. It can inspect only the
    /// opaque sample and the already chosen incumbent command; no outcome is
    /// supplied. Its captured pair is later bound to that exact command.</summary>
    public Relation PrepareValidation(Relation sample, Relation command,
        Func<Relation, Func<Relation, Relation, Relation>, Relation> incumbentRead,
        Func<Carrier, Relation, Relation, Relation> comparePaid, ServiceEconomy economy)
    {
        if (!Validating || !Present(sample) || !NativeGraph.TryValue(command, ActionAxis, out var issued) || issued.Ticks is not (-1 or 1))
            return lastRead = NativeGraph.Open(NativeGraph.G(sample, command), limits);
        if (pending.TryReadOrderedGroup(out var prior) && prior.Length == 6 && ReferenceEquals(prior[0], sample) && ReferenceEquals(prior[1], command))
            return pending;
        if (!economy.BurnWork(Purse, 2, out var reads)) return Reject(NativeGraph.Open(Purse, limits));
        Relation Compare(Relation a, Relation b) => comparePaid(Purse, a, b);
        var incumbent = incumbentRead(sample, Compare); var prediction = Candidate.Predict(sample, Compare);
        pending = NativeGraph.G(sample, command, NativeGraph.CapturePublication(incumbent, AdviceAxis),
            NativeGraph.CapturePublication(CalibratePrediction(prediction), AdviceAxis), prediction, reads);
        return lastRead = ReadWitness.Create(new Carrier(), NativeGraph.G(sample, command), limits, pending, NativeGraph.G(), pending);
    }

    public Relation ReadWork() => Sum(ResourceAxis,
        (new CarrierLocation(ResourceAxis, Reserved), 1), (new CarrierLocation(Purse, checked((long)Purse.Value)), -1));
    public Relation ReadGain()
    {
        var fields = NativeGraph.Parts(gainLedger, 3).Cast<Carrier>().ToArray();
        return Sum(ResourceAxis, (new CarrierLocation(fields[1], checked((long)fields[1].Value)), 1),
            (new CarrierLocation(fields[2], checked((long)fields[2].Value)), -1));
    }
    public Relation ReadNetGain() => Difference(ReadGain(), ReadWork());

    public Relation Reject(IElement reason)
    {
        if (Closed) return decision;
        closed.Add(1);
        return decision = ReadWitness.Create(new Carrier(), Candidate.Root, admissionPolicy, new CarrierLocation(responseAxis, 0),
            NativeGraph.G(reason, ReadWork(), ReadGain()), NativeGraph.G(limits, NativeGraph.G(records.Cast<IElement>().ToArray())));
    }
    private void Finish()
    {
        var net = ReadNetGain();
        var selection = ReplayAdmission();
        var accept = NativeGraph.TryValue(selection, responseAxis, out var result) && result.Ticks == 1;
        closed.Add(1); if (accept) admitted.Add(1);
        decision = ReadWitness.Create(new Carrier(), Candidate.Root, admissionPolicy,
            new CarrierLocation(responseAxis, accept ? 1 : 0), NativeGraph.G(net, selection),
            NativeGraph.G(limits, payoff, ReadGain(), ReadWork(), NativeGraph.G(records.Cast<IElement>().ToArray())));
    }
    /// <summary>Replays the retained native admission rule without granting
    /// energy, training a model or changing any lifecycle counter.</summary>
    public Relation ReplayAdmission() => SupportDecisionSurface.Select(new Carrier(), admissionPolicy,
            SupportDecisionSurface.Binding(new Carrier(), validationRole, NativeGraph.Publication(validation, ValidationCount, validation)),
            SupportDecisionSurface.Binding(new Carrier(), supportRole, NativeGraph.Publication(supported, SupportedCount, supported)),
            SupportDecisionSurface.Binding(new Carrier(), gainRole, ReadNetGain()),
            SupportDecisionSurface.Binding(new Carrier(), comparisonRole, NativeGraph.Publication(comparisonEnabled, checked((long)comparisonEnabled.Value), comparisonEnabled)));
    public Relation CalibratePrediction(Relation prediction) => AdaptiveOrdinal.TryReadPrediction(prediction, out var label)
        ? Equation.PredictWitness(new Carrier(), Equation.Affine(LabelAxis, 0, 1, AdviceAxis, -1, 1), label, AdviceAxis)
        : NativeGraph.Open(prediction, Relation.Segment(AdviceAxis, -1, 1));

    /// <summary>Observation under the authored opposed-action payout premise.
    /// Unknown advice preserves the actual command, hence no fictitious gain.</summary>
    public Relation ProjectedGross(Relation advice, Relation actualCommand, Relation bound)
    {
        if (!NativeGraph.IsBoundTo(bound, actualCommand, ResourceAxis) ||
            !NativeGraph.TryValue(actualCommand, ActionAxis, out var command) || command.Ticks is not (-1 or 1) ||
            !NativeGraph.TryValue(bound, ResourceAxis, out var gross) || gross.Ticks is not (-24 or -12 or 8 or 16))
            return NativeGraph.Open(NativeGraph.G(advice, actualCommand, bound), payoff);
        var available = NativeGraph.TryValue(advice, AdviceAxis, out var expected) && expected.Ticks is -1 or 1;
        var chosen = available ? expected : new CarrierLocation(AdviceAxis, command.Ticks);
        var adviceRole = new Carrier(); var commandRole = new Carrier(); var consequenceRole = new Carrier(); var rules = new List<Relation>();
        var pair = NativeGraph.Parts(payoff, 3);
        var opposed = Equation.PredictWitness(new Carrier(), (Relation)pair[gross.Ticks > 0 ? 0 : 1], gross, ResourceAxis);
        foreach (var orientation in new[] { -1L, 1L })
        {
            rules.Add(SupportDecisionSurface.Rule(new Carrier(), gross,
                SupportDecisionSurface.Condition(new Carrier(), adviceRole, Relation.Segment(AdviceAxis, orientation, orientation)),
                SupportDecisionSurface.Condition(new Carrier(), commandRole, Relation.Segment(ActionAxis, orientation, orientation))));
            rules.Add(SupportDecisionSurface.Rule(new Carrier(), NativeGraph.TryValue(opposed, ResourceAxis, out var inverse) ? inverse : new Placeholder(),
                SupportDecisionSurface.Condition(new Carrier(), adviceRole, Relation.Segment(AdviceAxis, orientation, orientation)),
                SupportDecisionSurface.Condition(new Carrier(), commandRole, Relation.Segment(ActionAxis, -orientation, -orientation))));
        }
        var policy = SupportDecisionSurface.Create(new Carrier(), rules.ToArray());
        var select = SupportDecisionSurface.Select(new Carrier(), policy,
            SupportDecisionSurface.Binding(new Carrier(), adviceRole, NativeGraph.Publication(AdviceAxis, chosen.Ticks, advice)),
            SupportDecisionSurface.Binding(new Carrier(), commandRole, NativeGraph.CapturePublication(actualCommand, ActionAxis)),
            SupportDecisionSurface.Binding(new Carrier(), consequenceRole, NativeGraph.CapturePublication(bound, ResourceAxis)));
        IElement result = NativeGraph.TryValue(select, ResourceAxis, out var score) ? score : new Placeholder();
        return ReadWitness.Create(new Carrier(), NativeGraph.G(advice, actualCommand, bound), NativeGraph.G(payoff, policy), result,
            available ? NativeGraph.G() : NativeGraph.Open(advice, actualCommand), NativeGraph.G(select, opposed));
    }
    private bool TryPhysical(Relation sample, Relation command, Relation bound, out Relation direction)
    {
        direction = NativeGraph.Open(bound, sample);
        if (!Present(sample) ||
            !NativeGraph.IsBoundTo(bound, command, ResourceAxis) || !NativeGraph.TryValue(command, ActionAxis, out var action) || action.Ticks is not (-1 or 1) ||
            !NativeGraph.TryValue(bound, ResourceAxis, out var gross) || gross.Ticks is not (-24 or -12 or 8 or 16)) return false;
        var signAxis = new Carrier(); var sign = OutcomeRetrodiction.ResourceSign(bound, signAxis);
        if (!NativeGraph.TryValue(sign, signAxis, out var consequence)) return false;
        direction = Equation.PredictWitness(new Carrier(), Equation.Product(AdviceAxis, Equation.UnitPort(ActionAxis), Equation.UnitPort(signAxis)),
            new Relation(action, consequence), AdviceAxis); return NativeGraph.TryValue(direction, AdviceAxis, out _);
    }
    private bool Present(Relation sample)
    {
        if (!SamplingSurface.TryReadSample(sample, out var presence, out _) || presence.Ticks != 1 ||
            !ReadWitness.TryRead(sample, out var selected, out _, out _, out _, out _) || selected is not Carrier s || !s.SameIdentityAs(source) ||
            !AdaptiveOrdinal.TryReadModel(Candidate.Root, out var policy, out _, out _)) return false;
        var frame = (Relation)NativeGraph.Parts(policy, 8)[1];
        return NativeGraph.TryPoint(frame.Left, out var origin) && origin.Carrier.SameIdentityAs(presence.Carrier);
    }
    private Relation Difference(Relation left, Relation right)
    {
        if (!NativeGraph.TryValue(left, ResourceAxis, out var l) || !NativeGraph.TryValue(right, ResourceAxis, out var r))
            return NativeGraph.Open(NativeGraph.G(left, right), ResourceAxis);
        return Sum(ResourceAxis, (l, 1), (r, -1));
    }
    private long Read(Relation publication) => NativeGraph.TryValue(publication, ResourceAxis, out var value) ? value.Ticks : 0;
    private long? PendingValue(int field) => pending.TryReadOrderedGroup(out var parts) && parts.Length == 6 &&
        parts[field] is Relation publication && NativeGraph.TryValue(publication, AdviceAxis, out var value) ? value.Ticks : null;
    private static CarrierLocation Point(IElement element) => NativeGraph.TryPoint(element, out var point) ? point : throw new ArgumentException("The trial's native limit is not readable.");
    private static Relation Sum(Carrier target, params (CarrierLocation Value, long Weight)[] terms)
    {
        var ports = terms.Select(t => Equation.WeightedPort(t.Value.Carrier, 0, 1, 0, t.Weight)).ToArray();
        IElement input = terms[0].Value; foreach (var term in terms.Skip(1)) input = new Relation(input, term.Value);
        return Equation.PredictWitness(new Carrier(), Equation.Sum(target, ports), input, target);
    }
}
