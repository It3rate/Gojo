using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;

namespace CompositeEmergence;

/// <summary>A local action market. It sees only exposed feature publications
/// and the public goal. The constructor supplies possible actions, never their
/// correct relation to a private apparatus.</summary>
public sealed class ServiceConsumer
{
    private readonly LocalChoiceMarket market;
    public IReadOnlyList<Carrier> FeatureAxes { get; }
    public Carrier FeatureAxis => FeatureAxes[0];
    public Carrier GoalAxis { get; }
    public Carrier ActionAxis => market.ChoiceAxis;
    public Carrier ResourceAxis => market.ResourceAxis;
    public Relation Root => market.Root;
    public Relation LearnedRoot => market.LearnedRoot;
    public Relation LastDecision => market.LastDecision;
    public Relation LastFeedback => market.LastFeedback;
    public int RowCount => market.RowCount;
    public long UpdateCount => market.UpdateCount;
    public long AgingEvents => market.AgingEvents;

    public ServiceConsumer(Carrier featureAxis, Carrier goalAxis, Carrier actionAxis, Carrier resourceAxis)
        : this([featureAxis], goalAxis, actionAxis, resourceAxis, false) { }

    /// <summary>The two-feature overload is the flat comparison: it receives
    /// the two primitive +/-1 signals and a goal, using the same market law.</summary>
    public ServiceConsumer(Carrier[] featureAxes, Carrier goalAxis, Carrier actionAxis, Carrier resourceAxis)
        : this(featureAxes, goalAxis, actionAxis, resourceAxis, true) { }

    private ServiceConsumer(Carrier[] featureAxes, Carrier goalAxis, Carrier actionAxis, Carrier resourceAxis, bool primitive)
    {
        if (featureAxes.Length == 0) throw new ArgumentException("At least one exposed feature is required.", nameof(featureAxes));
        FeatureAxes = featureAxes.ToArray(); GoalAxis = goalAxis;
        var ranges = featureAxes.Select(_ => primitive ? (-1L, 1L) : (-16L, 16L)).Append((-1L, 1L)).ToArray();
        market = new(featureAxes.Append(goalAxis).ToArray(), ranges, actionAxis, [-1L, 1L], resourceAxis);
    }

    private ServiceConsumer(Relation root)
    {
        market = LocalChoiceMarket.FromRoot(root);
        FeatureAxes = market.ContextAxes.Take(market.ContextAxes.Count - 1).ToArray();
        GoalAxis = market.ContextAxes[^1];
    }

    public static ServiceConsumer FromRoot(Relation root) => new(root);
    public Relation Act(Relation feature, Relation goal, bool learning = true) => Act([feature], goal, learning);
    public Relation Act(Relation[] features, Relation goal, bool learning = true) => market.Decide(features.Append(goal).ToArray(), learning);
    public Relation BindFeedback(Relation physicalResource) => market.BindFeedback(physicalResource);
    public Relation Observe(Relation boundResource, bool learning = true) => market.Observe(boundResource, learning);
    public void ResetOperational() => market.ResetOperational();
    public Relation ReadEvidence() => market.ReadEvidence();
    public IReadOnlyList<MarketEvidence> InspectEvidence() => market.InspectEvidence();
}

/// <summary>Inspection only: all values are read from the connected native
/// context/candidate ledger, not from a second learning table.</summary>
public sealed record MarketEvidence(long[] Context, long Choice, long Observations,
    long PositiveResource, long NegativeResource, long? Mean256);

/// <summary>
/// G(axes, legal frames, choice axis, choices, resource/score/count axes,
///   sparse rows, learning law, forgotten receipt count, update count,
///   latest aging witness, operational surface).
/// row = G(context points, G(candidate choice/ledger), learning phase,
///         selector, scoped choice).
/// Ledger values are changed only by physical resource feedback bound to the
/// actual chosen decision. Context rows are created locally on first training
/// observation; an unseen frozen context uses the declared first action and
/// retains its empty evidence, without adding a learned row.
/// </summary>
internal sealed class LocalChoiceMarket
{
    internal const int InitialProbes = 2, ProbePeriod = 16, MaximumRows = 128;
    private readonly Carrier scoreAxis, countAxis, forgotten, updates, ages;
    private readonly Relation[] frames;
    private readonly long[] choices;
    private readonly List<Relation> rows = [];
    private readonly List<Relation> receipts = [];
    private Relation latestAging;
    private Relation? learnedSnapshot;
    internal IReadOnlyList<Carrier> ContextAxes { get; }
    internal Carrier ChoiceAxis { get; }
    internal Carrier ResourceAxis { get; }
    internal Relation Root { get; private set; }
    internal Relation LastDecision { get; private set; }
    internal Relation LastFeedback { get; private set; }
    internal int RowCount => rows.Count;
    internal long UpdateCount => checked((long)updates.Value);
    internal long AgingEvents => checked((long)ages.Value);

    internal LocalChoiceMarket(Carrier[] axes, (long Lower, long Upper)[] ranges, Carrier choiceAxis, long[] choices, Carrier resourceAxis)
    {
        if (axes.Length != ranges.Length || choices.Length != 2) throw new ArgumentException("This local market needs aligned context frames and two actions.");
        ContextAxes = axes; ChoiceAxis = choiceAxis; ResourceAxis = resourceAxis; this.choices = choices;
        scoreAxis = new(); countAxis = new(); forgotten = new(countAxis.UnitShape); updates = new(countAxis.UnitShape); ages = new(countAxis.UnitShape);
        frames = ranges.Select((range, index) => Relation.Segment(axes[index], range.Lower, range.Upper)).ToArray();
        latestAging = NativeGraph.Open(NativeGraph.G(), NativeGraph.G());
        LastDecision = LastFeedback = NativeGraph.Open(NativeGraph.G(axes), NativeGraph.G(frames));
        Root = BuildRoot();
    }

    private LocalChoiceMarket(Relation root)
    {
        var fields = NativeGraph.Parts(root, 13);
        ContextAxes = NativeGraph.Parts((Relation)fields[0]).Cast<Carrier>().ToArray();
        frames = NativeGraph.Parts((Relation)fields[1]).Cast<Relation>().ToArray();
        ChoiceAxis = (Carrier)fields[2];
        choices = NativeGraph.Parts((Relation)fields[3], 2).Select(element => { NativeGraph.TryPoint(element, out var point); return point.Ticks; }).ToArray();
        ResourceAxis = (Carrier)fields[4]; scoreAxis = (Carrier)fields[5]; countAxis = (Carrier)fields[6];
        rows.AddRange(NativeGraph.Parts((Relation)fields[7]).Cast<Relation>());
        forgotten = (Carrier)fields[9]; updates = (Carrier)fields[10];
        var aging = NativeGraph.Parts((Relation)fields[11], 2); ages = (Carrier)aging[0]; latestAging = (Relation)aging[1];
        var operational = NativeGraph.Parts((Relation)fields[12], 3);
        LastDecision = (Relation)operational[0]; LastFeedback = (Relation)operational[1];
        receipts.AddRange(NativeGraph.Parts((Relation)operational[2]).Cast<Relation>()); Root = root;
    }

    internal static LocalChoiceMarket FromRoot(Relation root) => new(root);
    internal Relation BindFeedback(Relation physical) => NativeGraph.BindFeedback(LastDecision, physical, ResourceAxis);

    internal Relation Decide(Relation[] publications, bool learning)
    {
        var source = NativeGraph.G(publications);
        if (publications.Length != ContextAxes.Count) return PublishOpen(source);
        var values = new CarrierLocation[publications.Length]; var validation = new List<IElement>();
        for (var index = 0; index < publications.Length; index++)
        {
            if (!NativeGraph.TryValue(publications[index], ContextAxes[index], out values[index]) ||
                !NativeGraph.TryPoint(frames[index].Left, out var lower) || !NativeGraph.TryPoint(frames[index].Right, out var upper)) return PublishOpen(source);
            var range = OperationClaim.Between(new Carrier(), values[index], lower, upper, new Carrier()); validation.Add(range);
            if (!OperationClaim.TryReadBetween(range, out _, out _, out _, out _, out _, out var violation, out _) || violation.Ticks != 0)
                return PublishOpen(source);
        }
        // Retain the advertised coordinate and boundary event, never the
        // supplier's private explanation graph. A later controller cannot
        // discover another input by walking through this decision's history.
        source = NativeGraph.G(publications.Select((publication, index) =>
            NativeGraph.CapturePublication(publication, ContextAxes[index])).ToArray());
        Relation? row = null;
        foreach (var candidate in rows)
        {
            var contexts = NativeGraph.Parts((Relation)NativeGraph.Parts(candidate, 5)[0], values.Length);
            var claims = values.Select((value, index) => OperationClaim.Equality(new Carrier(), value, contexts[index], new Carrier())).ToArray();
            if (!claims.All(NativeGraph.Pass)) continue;
            row = candidate; validation.AddRange(claims); break;
        }
        if (row is null)
        {
            row = CreateRow(values);
            if (learning)
            {
                if (rows.Count >= MaximumRows) return PublishOpen(source);
                rows.Add(row); learnedSnapshot = null;
            }
        }
        var parts = NativeGraph.Parts(row, 5);
        var alternatives = NativeGraph.Parts((Relation)parts[1], 2).Cast<Relation>().ToArray();
        var scores = alternatives.Select(candidate => NativeGraph.Score(Ledger(candidate), scoreAxis)).ToArray();
        var comparisons = new List<IElement>(); var winner = 0;
        if (NativeGraph.TryValue(scores[1], scoreAxis, out var proposed))
        {
            if (!NativeGraph.TryValue(scores[0], scoreAxis, out var current)) winner = 1;
            else
            {
                var claim = OperationClaim.AtLeast(new Carrier(), proposed, new CarrierLocation(scoreAxis, checked(current.Ticks + 1)), new Carrier());
                comparisons.Add(claim); if (NativeGraph.Pass(claim)) winner = 1;
            }
        }
        var selected = winner;
        var phase = (Carrier)parts[2];
        if (learning)
        {
            var initial = false;
            for (var index = 0; index < alternatives.Length; index++)
            {
                var count = NativeGraph.Count(Ledger(alternatives[index]));
                var countRead = new CarrierLocation(countAxis, checked((long)count.Value));
                var probe = OperationClaim.AtMost(new Carrier(), countRead, new CarrierLocation(countAxis, InitialProbes - 1), new Carrier());
                comparisons.Add(probe);
                if (!NativeGraph.Pass(probe)) continue;
                selected = index; initial = true; break;
            }
            if (!initial && phase.Value == 0) selected = 1 - winner;
        }
        var selector = (Carrier)parts[3]; selector.Set(selected);
        var choice = (Relation)parts[4];
        var selectorRead = new Relation(new CarrierLocation(selector, selected), NativeGraph.G(comparisons.ToArray()));
        if (!ScopedChoice.TrySelect(choice, selectorRead, out var selectedElement, out _, out var binding) || selectedElement is not Relation selectedCandidate)
            return PublishOpen(source);
        var action = NativeGraph.Parts(selectedCandidate, 2)[0]; NativeGraph.TryPoint(action, out var actionPoint);
        var stamp = new Carrier();
        LastDecision = ReadWitness.Create(new Carrier(), source, choice, new CarrierLocation(ChoiceAxis, actionPoint.Ticks),
            NativeGraph.G(scores), NativeGraph.G(row, selectedCandidate, binding, NativeGraph.G(validation.ToArray()),
                NativeGraph.G(comparisons.ToArray()), Relation.Point(countAxis, learning ? 1 : 0), stamp,
                NativeGraph.G(values.Select(value => (IElement)new CarrierLocation(value.Carrier, value.Ticks)).ToArray())));
        Root = BuildRoot(); return LastDecision;
    }

    internal Relation Observe(Relation boundResource, bool learning)
    {
        if (!learning || !NativeGraph.TryValue(boundResource, ResourceAxis, out var resource) ||
            !NativeGraph.IsBoundTo(boundResource, LastDecision, ResourceAxis) || resource.Ticks is < -1_000_000 or > 1_000_000 ||
            !ReadWitness.TryRead(LastDecision, out _, out _, out _, out _, out var provenance) || provenance is not Relation group ||
            !group.TryReadOrderedGroup(out var fields) || fields.Length != 8 || fields[0] is not Relation row || fields[1] is not Relation candidate ||
            !NativeGraph.TryPoint(fields[5], out var training) || training.Ticks != 1 || fields[6] is not Carrier stamp ||
            !rows.Any(known => ReferenceEquals(known, row))) return RecordOpen(boundResource);
        var identity = NativeGraph.Identity(boundResource);
        foreach (var receipt in receipts)
        {
            var prior = NativeGraph.Parts(receipt, 7);
            if (ReferenceEquals(prior[0], stamp) || ReferenceEquals(prior[1], identity)) return RecordOpen(boundResource);
        }
        var ledger = Ledger(candidate); var before = FreezeLedger(ledger);
        var aging = NativeGraph.Count(ledger).Value >= NativeGraph.MaximumLedgerCount;
        var change = NativeGraph.Update(ledger, resource.Ticks);
        if (aging) { ages.Add(1); latestAging = change; }
        ((Carrier)NativeGraph.Parts(row, 5)[2]).Add(1); updates.Add(1); learnedSnapshot = null;
        NativeGraph.TryPoint(NativeGraph.Parts(candidate, 2)[0], out var chosen);
        var receiptRead = NativeGraph.G(stamp, identity,
            NativeGraph.G(NativeGraph.Parts((Relation)fields[7]).Select(FreezePoint).ToArray()),
            Relation.Point(ChoiceAxis, chosen.Ticks), Relation.Point(ResourceAxis, resource.Ticks), before, FreezeLedger(ledger));
        receipts.Add(receiptRead);
        if (receipts.Count > NativeGraph.ReceiptLimit) { receipts.RemoveAt(0); forgotten.Add(1); }
        LastFeedback = ReadWitness.Create(new Carrier(), NativeGraph.G(stamp, identity, Relation.Point(ResourceAxis, resource.Ticks)),
            NativeGraph.G(Relation.Point(ChoiceAxis, chosen.Ticks), change), FreezeLedger(ledger), NativeGraph.G(), receiptRead);
        Root = BuildRoot(); return LastFeedback;
    }

    internal Relation LearnedRoot => learnedSnapshot ??= BuildSnapshot();
    internal void ResetOperational()
    {
        LastDecision = LastFeedback = NativeGraph.Open(NativeGraph.G(ContextAxes.ToArray()), NativeGraph.G(frames));
        receipts.Clear(); Root = BuildRoot();
    }
    internal Relation ReadEvidence() => BuildSnapshot();
    internal IReadOnlyList<MarketEvidence> InspectEvidence()
    {
        var result = new List<MarketEvidence>();
        foreach (var row in rows)
        {
            var parts = NativeGraph.Parts(row, 5);
            var context = NativeGraph.Parts((Relation)parts[0]).Select(element => { NativeGraph.TryPoint(element, out var point); return point.Ticks; }).ToArray();
            foreach (var candidate in NativeGraph.Parts((Relation)parts[1], 2).Cast<Relation>())
            {
                NativeGraph.TryPoint(NativeGraph.Parts(candidate, 2)[0], out var action);
                var ledger = NativeGraph.Parts(Ledger(candidate), 3).Cast<Carrier>().ToArray();
                var score = NativeGraph.Score(Ledger(candidate), scoreAxis);
                long? mean = NativeGraph.TryValue(score, scoreAxis, out var value) ? value.Ticks : null;
                result.Add(new(context.ToArray(), action.Ticks, checked((long)ledger[0].Value), checked((long)ledger[1].Value), checked((long)ledger[2].Value), mean));
            }
        }
        return result;
    }

    private Relation CreateRow(CarrierLocation[] context)
    {
        var alternatives = choices.Select(action => NativeGraph.G(Relation.Point(ChoiceAxis, action), NativeGraph.Ledger(countAxis, ResourceAxis))).ToArray();
        var phase = new Carrier(countAxis.UnitShape); phase.Attach(new Relation(Relation.Point(phase, ProbePeriod), Relation.Point(phase, 0)));
        var selector = new Carrier();
        var choice = ScopedChoice.Create(selector.ValueSlot, Relation.Segment(selector, 0, alternatives.Length), ChoiceAxis,
            alternatives.Select((candidate, index) => ScopedChoice.Offer(Relation.Segment(selector, index, index + 1), candidate,
                Equation.Affine(ChoiceAxis, 0, 1, ChoiceAxis, 0, 1))).ToArray());
        return NativeGraph.G(NativeGraph.G(context.Select(value => (IElement)Relation.Point(value.Carrier, value.Ticks)).ToArray()),
            NativeGraph.G(alternatives), phase, selector, choice);
    }
    private Relation BuildSnapshot()
    {
        var snapshotAxes = ContextAxes.Select(axis => new Carrier(axis.UnitShape)).ToArray();
        var choiceSnapshotAxis = new Carrier(ChoiceAxis.UnitShape); var resourceSnapshotAxis = new Carrier(ResourceAxis.UnitShape); var snapshotCountAxis = new Carrier(countAxis.UnitShape);
        var snapshots = rows.Select(row =>
        {
            var parts = NativeGraph.Parts(row, 5);
            var contexts = NativeGraph.Parts((Relation)parts[0]).Select((element, index) => { NativeGraph.TryPoint(element, out var point); return (IElement)Relation.Point(snapshotAxes[index], point.Ticks); }).ToArray();
            var candidates = NativeGraph.Parts((Relation)parts[1], 2).Cast<Relation>().Select(candidate =>
            {
                NativeGraph.TryPoint(NativeGraph.Parts(candidate, 2)[0], out var action);
                var ledger = NativeGraph.Parts(Ledger(candidate), 3).Cast<Carrier>().ToArray();
                return (IElement)NativeGraph.G(Relation.Point(choiceSnapshotAxis, action.Ticks), NativeGraph.G(
                    Relation.Point(snapshotCountAxis, checked((long)ledger[0].Value)),
                    Relation.Point(resourceSnapshotAxis, checked((long)ledger[1].Value)), Relation.Point(resourceSnapshotAxis, checked((long)ledger[2].Value))));
            }).ToArray();
            return (IElement)NativeGraph.G(NativeGraph.G(contexts), NativeGraph.G(candidates),
                Relation.Point(snapshotCountAxis, checked((long)((Carrier)parts[2]).Value)));
        }).ToArray();
        return NativeGraph.G(NativeGraph.G(snapshotAxes), choiceSnapshotAxis, resourceSnapshotAxis, snapshotCountAxis,
            NativeGraph.G(snapshots), Law(snapshotCountAxis), Relation.Point(snapshotCountAxis, UpdateCount), Relation.Point(snapshotCountAxis, AgingEvents));
    }
    private Relation BuildRoot() => NativeGraph.G(NativeGraph.G(ContextAxes.ToArray()), NativeGraph.G(frames), ChoiceAxis,
        NativeGraph.G(choices.Select(action => (IElement)Relation.Point(ChoiceAxis, action)).ToArray()), ResourceAxis, scoreAxis, countAxis,
        NativeGraph.G(rows.ToArray()), Law(countAxis), forgotten, updates, NativeGraph.G(ages, latestAging),
        NativeGraph.G(LastDecision, LastFeedback, NativeGraph.G(receipts.ToArray())));
    private static Relation Law(Carrier axis) => NativeGraph.G(Relation.Point(axis, InitialProbes), Relation.Point(axis, ProbePeriod),
        Relation.Point(axis, MaximumRows), Relation.Point(axis, NativeGraph.MaximumLedgerCount), Relation.Point(axis, NativeGraph.ReceiptLimit),
        Relation.Point(axis, 256), Relation.Segment(axis, 0, 2));
    private static Relation Ledger(Relation candidate) => (Relation)NativeGraph.Parts(candidate, 2)[1];
    private Relation FreezeLedger(Relation ledger)
    {
        var sites = NativeGraph.Parts(ledger, 3).Cast<Carrier>().ToArray();
        return NativeGraph.G(sites.Select((site, index) => (IElement)Relation.Point(new Carrier((index == 0 ? countAxis : ResourceAxis).UnitShape), checked((long)site.Value))).ToArray());
    }
    private static IElement FreezePoint(IElement element)
    {
        if (!NativeGraph.TryPoint(element, out var point)) return new Placeholder();
        return Relation.Point(new Carrier(point.Carrier.UnitShape), point.Ticks);
    }
    private Relation PublishOpen(IElement source) { LastDecision = NativeGraph.Open(source, NativeGraph.G(frames)); Root = BuildRoot(); return LastDecision; }
    private Relation RecordOpen(IElement resource) { LastFeedback = NativeGraph.Open(resource, LastDecision); Root = BuildRoot(); return LastFeedback; }
}
