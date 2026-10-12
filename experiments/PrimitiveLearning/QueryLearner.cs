using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;

namespace PrimitiveLearning;

/// <summary>
/// A two-choice empirical query allocator. The two public contexts, alternatives,
/// counts, paid costs, benefit totals, probe phase, selected binding and feedback
/// receipts are native structure. The helper schedules these local reads; it
/// does not know which world context makes a query useful.
/// </summary>
public sealed class QueryLearner
{
    // G(context, benefit, cost, choice, score, count, declared cost calibration,
    //   G(context rows), last decision, last feedback, G(recent receipts)).
    // row = G(context point, G(G(choice point, ledger)), selector, probe phase).
    // ledger = G(count, positive benefit, negative benefit, paid cost).
    private readonly Carrier contextAxis, benefitAxis, costAxis, scoreAxis, countAxis, forgotten;
    private readonly Relation calibration;
    private readonly Relation[] rows;
    private readonly List<Relation> receipts = [];
    public Carrier ChoiceAxis { get; }
    public Carrier ContextAxis => contextAxis;
    public Carrier BenefitAxis => benefitAxis;
    public Carrier CostAxis => costAxis;
    public Relation Root { get; private set; }
    public Relation LastDecision { get; private set; }
    public Relation LastFeedback { get; private set; }
    public const int InitialProbesPerChoice = 4;
    public const int ProbePeriod = 32;

    public QueryLearner(Carrier contextAxis, Carrier benefitAxis, Carrier costAxis)
    {
        this.contextAxis = contextAxis; this.benefitAxis = benefitAxis; this.costAxis = costAxis;
        ChoiceAxis = new(); scoreAxis = new(); countAxis = new(); forgotten = new(countAxis.UnitShape);
        // This experiment explicitly values one paid-cost tick as one benefit
        // tick. It is a visible calibration, not inferred unit compatibility.
        calibration = Equation.Affine(costAxis, 0, 1, benefitAxis, 0, 1);
        rows = Enumerable.Range(0, 2).Select(context =>
        {
            var phase = new Carrier(countAxis.UnitShape);
            phase.Attach(new Relation(Relation.Point(phase, ProbePeriod), Relation.Point(phase, 0)));
            return LearningGraph.G(Relation.Point(contextAxis, context), LearningGraph.G(
                LearningGraph.G(Relation.Point(ChoiceAxis, 0), LearningGraph.Ledger(countAxis, benefitAxis, costAxis)),
                LearningGraph.G(Relation.Point(ChoiceAxis, 1), LearningGraph.Ledger(countAxis, benefitAxis, costAxis))),
                new Carrier(ChoiceAxis.UnitShape), phase);
        }).ToArray();
        LastDecision = LastFeedback = LearningGraph.Open(Relation.Point(contextAxis, 0), calibration);
        Root = BuildRoot();
    }

    private QueryLearner(Relation root)
    {
        var fields = LearningGraph.Parts(root, 12);
        contextAxis = (Carrier)fields[0]; benefitAxis = (Carrier)fields[1]; costAxis = (Carrier)fields[2];
        ChoiceAxis = (Carrier)fields[3]; scoreAxis = (Carrier)fields[4]; countAxis = (Carrier)fields[5];
        calibration = (Relation)fields[6]; rows = LearningGraph.Parts((Relation)fields[7], 2).Cast<Relation>().ToArray();
        LastDecision = (Relation)fields[8]; LastFeedback = (Relation)fields[9];
        receipts.AddRange(LearningGraph.Parts((Relation)fields[10]).Cast<Relation>());
        forgotten = (Carrier)LearningGraph.Parts((Relation)fields[11], 4)[1];
        Root = root;
    }

    public static QueryLearner FromRoot(Relation root) => new(root);

    public Relation BindBenefit(Relation physicalBenefit) => LearningGraph.BindFeedback(LastDecision, physicalBenefit, benefitAxis);
    public Relation BindCost(Relation physicalCost) => LearningGraph.BindFeedback(LastDecision, physicalCost, costAxis);

    /// <summary>Returns an exposed 0/1 choice publication. Initial balanced
    /// probes and a sparse continuing probe are part of the declared experiment.</summary>
    public Relation Decide(Relation context, bool learn = true)
    {
        if (!LearningGraph.TryValue(context, contextAxis, out var value) || value.Ticks is < 0 or > 1)
            return PublishOpen(context);
        var row = rows[(int)value.Ticks]; var fields = LearningGraph.Parts(row, 4);
        var candidates = LearningGraph.Parts((Relation)fields[1], 2).Cast<Relation>().ToArray();
        var scoreReads = candidates.Select(candidate => LearningGraph.Score(Ledger(candidate), scoreAxis, 0)).ToArray();
        var comparisons = new List<IElement>();
        var winner = 0;
        if (LearningGraph.TryValue(scoreReads[1], scoreAxis, out var right) &&
            LearningGraph.TryValue(scoreReads[0], scoreAxis, out var left))
        {
            var claim = OperationClaim.AtLeast(new Carrier(), right, new CarrierLocation(scoreAxis, checked(left.Ticks + 1)), new Carrier());
            comparisons.Add(claim);
            if (LearningGraph.Pass(claim)) winner = 1;
        }
        else if (LearningGraph.TryValue(scoreReads[1], scoreAxis, out _)) winner = 1;
        var selected = winner;
        var phase = (Carrier)fields[3];
        if (learn)
        {
            for (var index = 0; index < candidates.Length; index++)
            {
                var count = LearningGraph.Count(Ledger(candidates[index]));
                var claim = OperationClaim.AtMost(new Carrier(), count.ValueSlot,
                    new CarrierLocation(count, InitialProbesPerChoice - 1), new Carrier());
                comparisons.Add(claim);
                if (LearningGraph.Pass(claim)) { selected = index; break; }
            }
            var fullyProbed = candidates.All(candidate => LearningGraph.Count(Ledger(candidate)).Value >= InitialProbesPerChoice);
            if (fullyProbed && phase.Value == 0) selected = 1 - winner;
        }
        var selector = (Carrier)fields[2]; selector.Set(selected);
        var choice = ScopedChoice.Create(selector.ValueSlot, Relation.Segment(selector, 0, 2), ChoiceAxis,
            candidates.Select((candidate, index) => ScopedChoice.Offer(Relation.Segment(selector, index, index + 1),
                candidate, Equation.Affine(ChoiceAxis, 0, 1, ChoiceAxis, 0, 1))).ToArray());
        var selectorRead = new Relation(new CarrierLocation(selector, selected), LearningGraph.G(comparisons.ToArray()));
        if (!ScopedChoice.TrySelect(choice, selectorRead, out var chosen, out _, out var binding) || chosen is not Relation selectedCandidate)
            return PublishOpen(context);
        var point = (Relation)LearningGraph.Parts(selectedCandidate, 2)[0];
        LastDecision = ReadWitness.Create(new Carrier(), context, choice, point.Left, LearningGraph.G(scoreReads),
            LearningGraph.G(row, selectedCandidate, binding, LearningGraph.G(scoreReads), LearningGraph.G(comparisons.ToArray()),
                Relation.Point(contextAxis, value.Ticks), Relation.Point(countAxis, learn ? 1 : 0)));
        Root = BuildRoot(); return LastDecision;
    }

    /// <summary>Connects the actual selected choice to independently supplied
    /// later benefit and paid cost. Unrelated/open/reused evidence stays open.</summary>
    public Relation Observe(Relation context, Relation outcomeBenefit, Relation paidCost, bool learn = true)
    {
        var supplied = LearningGraph.G(context, outcomeBenefit, paidCost);
        if (!learn || !LearningGraph.TryValue(context, contextAxis, out var contextValue) ||
            !LearningGraph.TryValue(outcomeBenefit, benefitAxis, out var benefit) ||
            !LearningGraph.TryValue(paidCost, costAxis, out var cost) || cost.Ticks < 0 ||
            !LearningGraph.IsBoundTo(outcomeBenefit, LastDecision, benefitAxis) || !LearningGraph.IsBoundTo(paidCost, LastDecision, costAxis) ||
            !ReadWitness.TryRead(LastDecision, out _, out _, out _, out _, out var provenance) ||
            provenance is not Relation group || !group.TryReadOrderedGroup(out var fields) || fields.Length != 7 ||
            fields[0] is not Relation row || fields[1] is not Relation candidate ||
            !LearningGraph.TryPoint(fields[5], out var decidedContext) || decidedContext.Ticks != contextValue.Ticks ||
            !LearningGraph.TryPoint(fields[6], out var training) || training.Ticks != 1 ||
            LearningGraph.ReceiptSeen(receipts, LastDecision, outcomeBenefit) ||
            !LearningGraph.CanUpdate(Ledger(candidate), benefit.Ticks, cost.Ticks))
            return RecordFeedback(LearningGraph.Open(supplied, LastDecision));
        var ledger = Ledger(candidate); var before = LearningGraph.CaptureLedger(ledger);
        LearningGraph.Update(ledger, benefit.Ticks, cost.Ticks);
        ((Carrier)LearningGraph.Parts(row, 4)[3]).Add(1);
        LastFeedback = ReadWitness.Create(new Carrier(), supplied, LastDecision, LearningGraph.CaptureLedger(ledger), LearningGraph.G(),
            LearningGraph.G(LastDecision, outcomeBenefit, paidCost, candidate, before, ledger));
        receipts.Add(LastFeedback); if (receipts.Count > LearningGraph.RecentReceiptLimit) { receipts.RemoveAt(0); forgotten.Add(1); }
        Root = BuildRoot(); return LastFeedback;
    }

    /// <summary>A diagnostic read of a native count, not a separate learner table.</summary>
    public long ObservationCount(int context, int choice) => checked((long)LearningGraph.Count(
        Ledger((Relation)LearningGraph.Parts((Relation)LearningGraph.Parts(rows[context], 4)[1], 2)[choice])).Value);

    /// <summary>Publishes the local candidate's frozen ledger and its native
    /// mean-benefit-minus-cost read. This does not choose or update a candidate.
    /// The score uses 256 ticks per declared resource tick; an unsampled
    /// candidate has an open score, while its zero observation count is known.</summary>
    public Relation ReadEvidence(int context, int choice)
    {
        if (context is < 0 or > 1 || choice is < 0 or > 1)
            return LearningGraph.Open(LearningGraph.G(Relation.Point(contextAxis, context), Relation.Point(ChoiceAxis, choice)), LearningGraph.G(rows));
        var rowFields = LearningGraph.Parts(rows[context], 4);
        var candidate = (Relation)LearningGraph.Parts((Relation)rowFields[1], 2)[choice];
        var ledger = Ledger(candidate); var score = LearningGraph.Score(ledger, scoreAxis, 0);
        ReadWitness.TryRead(score, out _, out _, out var result, out var residue, out _);
        return ReadWitness.Create(new Carrier(), candidate, score, result, residue,
            LearningGraph.G(Relation.Point(contextAxis, context), Relation.Point(ChoiceAxis, choice),
                LearningGraph.CaptureLedger(ledger), score,
                new CarrierLocation((Carrier)rowFields[3], checked((long)((Carrier)rowFields[3]).Value))));
    }

    /// <summary>Small display projection of a ReadEvidence witness. The
    /// connected native witness remains the authoritative diagnostic.</summary>
    public static bool TryInspectEvidence(Relation publication, out QueryEvidence evidence)
    {
        evidence = default!;
        if (!ReadWitness.TryRead(publication, out _, out _, out _, out _, out var provenance) ||
            provenance is not Relation group || !group.TryReadOrderedGroup(out var fields) || fields.Length != 5 ||
            !LearningGraph.TryPoint(fields[0], out var context) || !LearningGraph.TryPoint(fields[1], out var choice) ||
            fields[2] is not Relation frozenLedger || !frozenLedger.TryReadOrderedGroup(out var totals) || totals.Length != 4 ||
            fields[3] is not Relation score || !LearningGraph.TryPoint(fields[4], out var phase)) return false;
        var values = new CarrierLocation[4];
        for (var index = 0; index < totals.Length; index++) if (!LearningGraph.TryPoint(totals[index], out values[index])) return false;
        long? mean = null;
        if (ReadWitness.TryRead(score, out _, out _, out var scored, out _, out _) && LearningGraph.TryPoint(scored, out var point)) mean = point.Ticks;
        evidence = new(checked((int)context.Ticks), checked((int)choice.Ticks), values[0].Ticks,
            values[1].Ticks, values[2].Ticks, values[3].Ticks, mean, phase.Ticks);
        return true;
    }

    private Relation PublishOpen(Relation context) { LastDecision = LearningGraph.Open(context, LearningGraph.G(rows)); Root = BuildRoot(); return LastDecision; }
    private Relation RecordFeedback(Relation feedback) { LastFeedback = feedback; Root = BuildRoot(); return feedback; }
    private Relation BuildRoot() => LearningGraph.G(contextAxis, benefitAxis, costAxis, ChoiceAxis, scoreAxis, countAxis, calibration,
        LearningGraph.G(rows), LastDecision, LastFeedback, LearningGraph.G(receipts.ToArray()),
        LearningGraph.G(Relation.Point(countAxis, LearningGraph.RecentReceiptLimit), forgotten,
            Relation.Point(countAxis, InitialProbesPerChoice), Relation.Point(countAxis, ProbePeriod)));
    private static Relation Ledger(Relation candidate) => (Relation)LearningGraph.Parts(candidate, 2)[1];
}

/// <summary>Inspection-only coordinates from one frozen native evidence read.</summary>
public sealed record QueryEvidence(int Context, int Choice, long Observations,
    long PositiveBenefit, long NegativeBenefit, long PaidCost, long? NetMean256, long ProbePhase);

/// <summary>Narrow local builders/readers shared by the two experiment learners.
/// Every returned score/update retains the equations and participants that made it.</summary>
internal static class LearningGraph
{
    internal const int ScoreScale = 256, RecentReceiptLimit = 64, MaximumObservations = 8192;
    internal static Relation G(params IElement[] elements) => Relation.OrderedGroup(new Carrier(), elements);
    private static IElement InputTree(params IElement[] elements) => elements.Aggregate((left, right) => new Relation(left, right));
    internal static IElement[] Parts(Relation relation, int? length = null)
    {
        if (relation.TryReadOrderedGroup(out var fields) && (length is null || fields.Length == length)) return fields;
        throw new ArgumentException("The supplied root is not this learner's native ordered surface.", nameof(relation));
    }
    internal static bool TryPoint(IElement element, out CarrierLocation value)
    {
        if (element is CarrierLocation location && location.Carrier is not null && location.Tick is not null && !location.Carrier.IsUnknown) { value = location; return true; }
        if (element is Relation { Left: CarrierLocation point, Right: Carrier axis } && point.Carrier is not null && point.Tick is not null && point.Carrier.SameIdentityAs(axis) && !axis.IsUnknown)
        { value = point; return true; }
        value = default; return false;
    }
    internal static bool TryValue(Relation publication, Carrier axis, out CarrierLocation value)
    {
        IElement result = publication;
        if (publication.TryReadOrderedGroup(out var fields) && fields.Length == 5 &&
            ReadWitness.TryRead(publication, out _, out _, out var read, out _, out _))
        {
            result = read;
            if (QualifiedReadSurface.TryReadPublished(publication, out _, out _, out _, out _, out _, out _, out _, out _) &&
                (!FiniteReadSurface.TryReadSupport(publication, out var lower, out var upper) || lower is null || upper is null || lower.Value.Ticks != upper.Value.Ticks))
            { value = default; return false; }
        }
        else if (Equation.TryReadPrediction(publication, out var predicted, out _)) result = predicted;
        if (TryPoint(result, out var point) && point.Carrier.SameIdentityAs(axis)) { value = new(axis, point.Ticks); return true; }
        value = default; return false;
    }
    internal static Relation Publication(Carrier axis, long value, IElement source) => ReadWitness.Create(new Carrier(), source,
        Relation.Segment(axis, value, value), new CarrierLocation(axis, value), G(), source);
    internal static Relation Open(IElement supplied, IElement attempted) => ReadWitness.Create(new Carrier(), supplied, attempted,
        new Placeholder(), new Relation(supplied, attempted), attempted);
    internal static bool Pass(Relation claim) => OperationClaim.TryReadResolvedViolation(claim, out var violation) && violation.Ticks == 0;
    internal static Relation Ledger(Carrier count, Carrier benefit, Carrier cost)
    {
        var sites = new[] { new Carrier(count.UnitShape), new Carrier(benefit.UnitShape), new Carrier(benefit.UnitShape), new Carrier(cost.UnitShape) };
        for (var index = 0; index < sites.Length; index++)
        {
            var limit = index == 0 ? MaximumObservations : (long)MaximumObservations * 1_000_000;
            sites[index].Attach(new Relation(Relation.Point(sites[index], limit), Relation.Point(sites[index], limit)));
        }
        return G(sites);
    }
    internal static Carrier Count(Relation ledger) => (Carrier)Parts(ledger, 4)[0];
    internal static Relation CaptureLedger(Relation ledger) => G(Parts(ledger, 4).Cast<Carrier>()
        .Select(carrier => (IElement)new CarrierLocation(carrier, checked((long)carrier.Value))).ToArray());
    internal static bool CanUpdate(Relation ledger, long benefit, long cost)
    {
        if (benefit is < -1_000_000 or > 1_000_000 || cost is < 0 or > 1_000_000) return false;
        return Count(ledger).Value < MaximumObservations;
    }
    internal static void Update(Relation ledger, long benefit, long cost)
    {
        var fields = Parts(ledger, 4).Cast<Carrier>().ToArray();
        fields[0].Add(1); if (benefit >= 0) fields[1].Add((ulong)benefit); else fields[2].Add((ulong)-benefit); fields[3].Add((ulong)cost);
    }
    internal static Relation Score(Relation ledger, Carrier scoreAxis, long complexity)
    {
        var fields = Parts(ledger, 4).Cast<Carrier>().ToArray();
        if (fields[0].Value == 0) return Open(ledger, Relation.Segment(fields[0], 1, MaximumObservations));
        var sum = Equation.Sum(scoreAxis,
            Equation.WeightedPort(fields[1], 0, 1, 0, ScoreScale),
            Equation.WeightedPort(fields[2], 0, 1, 0, -ScoreScale),
            Equation.WeightedPort(fields[3], 0, 1, 0, -ScoreScale));
        var inputs = InputTree(fields.Skip(1).Select(carrier => (IElement)new CarrierLocation(carrier, checked((long)carrier.Value))).ToArray());
        var total = Equation.Predict(sum, inputs, scoreAxis);
        if (!Equation.TryReadPrediction(total, out var value, out _)) return Open(ledger, sum);
        var mean = Equation.Affine(fields[0], 0, checked((long)fields[0].Value), scoreAxis, 0, value.Ticks);
        var meanRead = Equation.Predict(mean, new CarrierLocation(fields[0], 1), scoreAxis);
        if (!Equation.TryReadPrediction(meanRead, out var average, out _)) return Open(ledger, mean);
        var penaltyAxis = new Carrier(); var penalty = Relation.Point(penaltyAxis, complexity);
        var difference = Equation.Sum(scoreAxis, Equation.UnitPort(scoreAxis), Equation.WeightedPort(penaltyAxis, 0, 1, 0, -1));
        var result = Equation.Predict(difference, new Relation(average, penalty.Left), scoreAxis);
        if (!Equation.TryReadPrediction(result, out var scored, out var remainder)) return Open(ledger, difference);
        return ReadWitness.Create(new Carrier(), ledger, G(sum, mean, difference), scored, remainder,
            G(CaptureLedger(ledger), total, meanRead, penalty, result));
    }
    internal static Relation BindFeedback(Relation decision, Relation physical, Carrier axis)
    {
        var calibration = Equation.Affine(axis, 0, 1, axis, 0, 1);
        var correspondence = G(decision, physical, calibration);
        if (!TryValue(physical, axis, out var value)) return Open(correspondence, calibration);
        var projected = Equation.Predict(calibration, value, axis);
        return ReadWitness.Create(new Carrier(), correspondence, calibration, value, projected.Right,
            G(decision, physical, projected));
    }
    internal static bool IsBoundTo(Relation publication, Relation decision, Carrier axis)
    {
        if (!ReadWitness.TryRead(publication, out var selected, out var surface, out _, out _, out _) || selected is not Relation group ||
            !group.TryReadOrderedGroup(out var fields) || fields.Length != 3 || !ReferenceEquals(fields[0], decision) ||
            fields[1] is not Relation physical || fields[2] is not Relation calibration || !ReferenceEquals(surface, calibration) ||
            !TryValue(physical, axis, out var original) || !TryValue(publication, axis, out var result) ||
            !Equation.TryPredict(calibration, original, axis, out var projected) || !Equation.TryReadPrediction(projected, out var expected, out _)) return false;
        return expected.Ticks == result.Ticks && expected.Carrier.SameIdentityAs(axis);
    }
    internal static IElement Identity(Relation publication)
    {
        if (!publication.TryReadOrderedGroup(out var fields) || fields.Length != 5 ||
            !ReadWitness.TryRead(publication, out var source, out _, out _, out _, out _) || source is Carrier) return publication;
        // A correspondence wrapping an old observation does not create a new
        // independent event, even when bound to a different decision.
        if (source is Relation group && group.TryReadOrderedGroup(out var parts) && parts.Length == 3 &&
            parts[0] is Relation && parts[1] is Relation physical && parts[2] is Relation) return Identity(physical);
        return source;
    }
    internal static bool ReceiptSeen(IEnumerable<Relation> receipts, Relation decision, Relation outcome)
    {
        var identity = Identity(outcome);
        foreach (var receipt in receipts)
            if (ReadWitness.TryRead(receipt, out _, out _, out _, out _, out var provenance) && provenance is Relation group &&
                group.TryReadOrderedGroup(out var fields) && fields.Length >= 2 &&
                (ReferenceEquals(fields[0], decision) || fields[1] is Relation earlier && ReferenceEquals(Identity(earlier), identity))) return true;
        return false;
    }
}
