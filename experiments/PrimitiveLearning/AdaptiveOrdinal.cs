using Gojo.Core;
using Gojo.Structures;

namespace PrimitiveLearning;

/// <summary>Construction and local reads of an exploratory piecewise-constant
/// ordinal hypothesis. Only acquired samples and requested comparison ports
/// locate a consequence boundary; no sample has a metric coordinate.</summary>
public sealed class AdaptiveOrdinal
{
    // state = G(policy, G(ordered landmarks), native decision rules, latest patch,
    //           G(update counter, comparison counter), G(recent receipts))
    // policy = G(source, presence frame, order frame, consequence frame,
    //            landmark budget, count budget, count-aging law, receipt budget)
    // landmark = G(opaque sample, G(outcome point -> support count carrier))
    private readonly Carrier source, presenceAxis, orderAxis, outcomeAxis, countAxis, updates, comparisons;
    private readonly int capacity;
    private readonly Relation policy;
    public Relation Root { get; private set; }
    public int LandmarkCount => Landmarks().Length;
    public int Updates => checked((int)updates.Value);
    public int Comparisons => checked((int)comparisons.Value);

    public AdaptiveOrdinal(Carrier source, Carrier presenceAxis, Carrier orderAxis, Carrier outcomeAxis, int capacity = 16)
    {
        if (capacity is < 1 or > 16) throw new ArgumentOutOfRangeException(nameof(capacity));
        this.source = source; this.presenceAxis = presenceAxis; this.orderAxis = orderAxis; this.outcomeAxis = outcomeAxis;
        this.capacity = capacity; countAxis = new Carrier(); updates = new Carrier(); comparisons = new Carrier();
        policy = OrdinalNative.G(source, Relation.Segment(presenceAxis, 0, 1), Relation.Segment(orderAxis, -1, 1),
            Relation.Segment(outcomeAxis, 0, 1), Relation.Point(new Carrier(), capacity), Relation.Point(countAxis, 255),
            new Relation(Relation.Segment(countAxis, 0, 2), Relation.Segment(countAxis, 0, 1)), Relation.Point(new Carrier(), 64));
        Root = OrdinalNative.G(policy, OrdinalNative.G(), OrdinalNative.G(), OrdinalNative.Open(policy, policy), OrdinalNative.G(updates, comparisons), OrdinalNative.G());
    }

    private AdaptiveOrdinal(Relation root)
    {
        if (!TryReadModel(root, out var p, out _, out _) || !p.TryReadOrderedGroup(out var f) ||
            f[0] is not Carrier s || !OrdinalNative.Frame(f[1], out var presence, out _, out _) ||
            !OrdinalNative.Frame(f[2], out var order, out _, out _) || !OrdinalNative.Frame(f[3], out var outcome, out _, out _) ||
            !OrdinalNative.Point(f[4], out var budget) || !OrdinalNative.Point(f[5], out var count) ||
            !root.TryReadOrderedGroup(out var state) || state[4] is not Relation counters ||
            !counters.TryReadOrderedGroup(out var cells) || cells.Length != 2 || cells[0] is not Carrier u || cells[1] is not Carrier q)
            throw new ArgumentException("The restored ordinal learner has no readable native contract.", nameof(root));
        source = s; presenceAxis = presence; orderAxis = order; outcomeAxis = outcome; capacity = (int)budget.Ticks;
        countAxis = count.Carrier; updates = u; comparisons = q; policy = p; Root = root;
    }
    public static AdaptiveOrdinal FromRoot(Relation root) => new(root);

    public Relation Predict(Relation sample, Func<Relation, Relation, Relation> compare)
    {
        var rows = Landmarks(); var rules = Rules(rows);
        var probes = new List<Relation>();
        if (!Present(sample)) return Finish(new Placeholder(), OrdinalNative.Open(sample, policy), probes);
        var position = Locate(sample, rows, compare, probes);
        if (!position.Resolved) return Finish(new Placeholder(), OrdinalNative.G(probes.ToArray()), probes);
        var bindings = probes.Select(probe =>
        {
            ReadWitness.TryRead(probe, out var selected, out _, out _, out _, out _);
            ((Relation)selected).TryReadOrderedGroup(out var pair);
            return SupportDecisionSurface.Binding(new Carrier(), pair[1], probe);
        }).ToArray();
        var selection = SupportDecisionSurface.Select(new Carrier(), rules, bindings);
        IElement result = new Placeholder();
        if (SupportDecisionSurface.TryReadSelection(selection, out _, out var response, out _, out _) &&
            response is Relation publication && OrdinalNative.Witness(publication, out _, out _, out var exposed, out _, out _)) result = exposed;
        return Finish(result, selection, probes);

        Relation Finish(IElement result, IElement selection, List<Relation> queries)
        {
            var prediction = ReadWitness.Create(new Carrier(), sample, OrdinalNative.G(policy, rules), result,
                result is CarrierLocation ? OrdinalNative.G() : new Relation(sample, rules),
                OrdinalNative.G(selection, OrdinalNative.G(queries.ToArray()), CountSnapshot(rows)));
            Rebuild(rows, rules, prediction); return prediction;
        }
    }

    /// <summary>Feedback retains the prediction made before admission. Frozen
    /// observations can expose a mismatch but do not change the learned model.</summary>
    public Relation Observe(Relation sample, Relation outcome, Func<Relation, Relation, Relation> compare, bool learn = true)
    {
        var receipts = Receipts();
        if (learn && receipts.Any(receipt => ReferenceEquals(receipt.Left, sample) && ReferenceEquals(receipt.Right, outcome)))
        {
            var refusal = OrdinalNative.Open(OrdinalNative.G(sample, outcome), OrdinalNative.G(policy, OrdinalNative.G(receipts)));
            Rebuild(Landmarks(), CurrentRules(), refusal); return refusal;
        }
        var beforeUpdates = updates.Value;
        var prediction = Predict(sample, compare); var rows = Landmarks().ToList();
        var changes = new List<IElement>(); var losses = new List<IElement>();
        CarrierLocation actual = default;
        var valid = Present(sample) && OrdinalNative.ReadPoint(outcome, outcomeAxis, 0, 1, out actual);
        IElement claim = new Relation(prediction, outcome);
        if (valid && TryReadPrediction(prediction, out var expected))
            claim = OperationClaim.Equality(new Carrier(), actual, expected, new Carrier(outcomeAxis.UnitShape));
        if (valid && learn)
        {
            var probes = new List<Relation>(); var location = Locate(sample, rows.ToArray(), compare, probes);
            changes.Add(OrdinalNative.G(probes.ToArray()));
            if (!location.Resolved) losses.Add(new Relation(sample, outcome));
            else
            {
                Relation? target = null; var insertion = location.Upper;
                if (location.Equal >= 0) target = rows[location.Equal];
                else if (rows.Count == 0) { target = NewLandmark(sample); rows.Add(target); }
                else
                {
                    var lower = location.Lower >= 0 ? Pure(rows[location.Lower]) : null;
                    var upper = location.Upper < rows.Count ? Pure(rows[location.Upper]) : null;
                    var label = (int)actual.Ticks;
                    if (location.Lower < 0 && upper == label) target = rows[location.Upper];
                    else if (location.Upper == rows.Count && lower == label) target = rows[location.Lower];
                    else if (lower == label && upper == label) target = rows[location.Lower];
                    else if (lower == label && upper is not null && upper != label &&
                        (location.Lower == 0 || Pure(rows[location.Lower - 1]) == label))
                    { target = Replace(rows, location.Lower, sample, changes); }
                    else if (upper == label && lower is not null && lower != label &&
                        (location.Upper == rows.Count - 1 || Pure(rows[location.Upper + 1]) == label))
                    { target = Replace(rows, location.Upper, sample, changes); }
                    else
                    {
                        var added = NewLandmark(sample); Increment(added, label, changes); rows.Insert(insertion, added);
                        Coarsen(rows, losses);
                        if (rows.Count > capacity)
                        {
                            rows.Remove(added); losses.Add(new Relation(OrdinalNative.G(sample, outcome), policy));
                        }
                        else { updates.Set(updates.Value + 1); target = null; changes.Add(new Relation(sample, added)); }
                    }
                }
                if (target is not null) { Increment(target, (int)actual.Ticks, changes); updates.Set(updates.Value + 1); }
            }
        }
        if (!valid) losses.Add(OrdinalNative.Open(OrdinalNative.G(sample, outcome), policy));
        var feedback = ReadWitness.Create(new Carrier(), sample, OrdinalNative.G(policy, outcome, Relation.Point(new Carrier(), learn ? 1 : 0)),
            valid ? actual : new Placeholder(), OrdinalNative.G(claim, OrdinalNative.G(losses.ToArray())),
            OrdinalNative.G(prediction, outcome, OrdinalNative.G(changes.ToArray())));
        if (updates.Value > beforeUpdates)
        {
            var next = receipts.Append(new Relation(sample, outcome)).ToArray();
            policy.TryReadOrderedGroup(out var contract); OrdinalNative.Point(contract[7], out var receiptBudget);
            if (next.Length > receiptBudget.Ticks)
            {
                // The eviction is explicit; old receipts beyond this finite
                // window are no longer an independent-event exclusion guard.
                var removed = next.Take(next.Length - (int)receiptBudget.Ticks).ToArray();
                losses.Add(new Relation(OrdinalNative.G(removed), contract[7]));
                next = next.TakeLast((int)receiptBudget.Ticks).ToArray();
                feedback = ReadWitness.Create(new Carrier(), sample, OrdinalNative.G(policy, outcome, Relation.Point(new Carrier(), learn ? 1 : 0)),
                    actual, OrdinalNative.G(claim, OrdinalNative.G(losses.ToArray())), OrdinalNative.G(prediction, outcome, OrdinalNative.G(changes.ToArray())));
            }
            receipts = next;
        }
        Rebuild(rows.ToArray(), learn && valid ? Rules(rows.ToArray()) : CurrentRules(), feedback, receipts);
        return feedback;
    }

    public static bool TryReadPrediction(Relation prediction, out CarrierLocation result) => OrdinalNative.Prediction(prediction, out result);
    public static bool TryReadAlternatives(Relation prediction, out CarrierLocation[] alternatives) => OrdinalNative.Alternatives(prediction, out alternatives);
    public static bool TryReadModel(Relation root, out Relation policy, out Relation landmarks, out Relation rules)
    {
        policy = landmarks = rules = null!;
        if (!root.TryReadOrderedGroup(out var f) || f.Length != 6 || f[0] is not Relation p ||
            !p.TryReadOrderedGroup(out var contract) || contract.Length != 8 || contract[0] is not Carrier ||
            !OrdinalNative.Frame(contract[1], out _, out var pl, out var ph) || pl != 0 || ph != 1 ||
            !OrdinalNative.Frame(contract[2], out _, out var ol, out var oh) || ol != -1 || oh != 1 ||
            !OrdinalNative.Frame(contract[3], out _, out var cl, out var ch) || cl != 0 || ch != 1 ||
            !OrdinalNative.Point(contract[4], out var cap) || cap.Ticks is < 1 or > 16 ||
            f[1] is not Relation l || !l.TryReadOrderedGroup(out var entries) || entries.Length > cap.Ticks ||
            entries.Any(entry => entry is not Relation row || !TryReadLandmark(row, out _, out _, out _)) ||
            f[2] is not Relation r || !r.TryReadOrderedGroup(out _) ||
            !OrdinalNative.Point(contract[7], out var receiptBudget) || receiptBudget.Ticks != 64 ||
            f[5] is not Relation receipts || !receipts.TryReadOrderedGroup(out var recent) || recent.Length > receiptBudget.Ticks ||
            recent.Any(receipt => receipt is not Relation { Left: Relation, Right: Relation })) return false;
        policy = p; landmarks = l; rules = r; return true;
    }
    public static bool TryReadLandmark(Relation landmark, out Relation sample, out Carrier zero, out Carrier one)
    {
        sample = null!; zero = one = null!;
        if (!landmark.TryReadOrderedGroup(out var f) || f.Length != 2 || f[0] is not Relation acquisition ||
            !SamplingSurface.TryReadSample(acquisition, out var presence, out _) || presence.Ticks != 1 ||
            f[1] is not Relation counts || !counts.TryReadOrderedGroup(out var c) || c.Length != 2 ||
            c[0] is not Relation a || !OrdinalNative.Point(a.Left, out var p0) || p0.Ticks != 0 || a.Right is not Carrier n0 ||
            c[1] is not Relation b || !OrdinalNative.Point(b.Left, out var p1) || p1.Ticks != 1 || b.Right is not Carrier n1 ||
            !p0.Carrier.SameIdentityAs(p1.Carrier) || n0.Value > 255 || n1.Value > 255) return false;
        sample = acquisition; zero = n0; one = n1; return true;
    }

    private Relation[] Landmarks() { TryReadModel(Root, out _, out var group, out _); group.TryReadOrderedGroup(out var rows); return rows.Cast<Relation>().ToArray(); }
    private Relation CurrentRules() { TryReadModel(Root, out _, out _, out var rules); return rules; }
    private bool Present(Relation sample) => SamplingSurface.TryReadSample(sample, out var p, out _) && p.Ticks == 1 &&
        p.Carrier.SameIdentityAs(presenceAxis) && OrdinalNative.Witness(sample, out var selected, out _, out _, out _, out _) && ReferenceEquals(source, selected);
    private Relation[] Receipts() { Root.TryReadOrderedGroup(out var fields); ((Relation)fields[5]).TryReadOrderedGroup(out var receipts); return receipts.Cast<Relation>().ToArray(); }
    private void Rebuild(Relation[] rows, Relation rules, Relation latest, Relation[]? receipts = null) =>
        Root = OrdinalNative.G(policy, OrdinalNative.G(rows), rules, latest, OrdinalNative.G(updates, comparisons), OrdinalNative.G(receipts ?? Receipts()));
    private Relation NewLandmark(Relation sample) => OrdinalNative.G(sample, OrdinalNative.G(
        new Relation(Relation.Point(outcomeAxis, 0), new Carrier(countAxis.UnitShape)),
        new Relation(Relation.Point(outcomeAxis, 1), new Carrier(countAxis.UnitShape))));
    private static int? Pure(Relation row)
    {
        TryReadLandmark(row, out _, out var zero, out var one);
        return zero.Value > 0 && one.Value == 0 ? 0 : one.Value > 0 && zero.Value == 0 ? 1 : null;
    }
    private static Relation Replace(List<Relation> rows, int index, Relation sample, List<IElement> changes)
    {
        var before = rows[index]; before.TryReadOrderedGroup(out var parts);
        var after = OrdinalNative.G(sample, parts[1]); rows[index] = after;
        changes.Add(new Relation(before, after)); return after;
    }
    private void Increment(Relation row, int label, List<IElement> changes)
    {
        TryReadLandmark(row, out _, out var zero, out var one);
        if (zero.Value + one.Value >= 255)
        {
            policy.TryReadOrderedGroup(out var p); var aging = (Relation)p[6];
            foreach (var cell in new[] { zero, one })
            {
                var before = new CarrierLocation(cell, (long)cell.Value);
                aging.TryProject(new CarrierLocation(countAxis, before.Ticks), countAxis, out var projected);
                var target = (CarrierLocation)projected.Left; cell.Set(target.Ticks);
                changes.Add(ReadWitness.Create(new Carrier(), before, aging, new CarrierLocation(cell, (long)cell.Value), projected.Right,
                    new Relation(before, projected)));
            }
        }
        var chosen = label == 0 ? zero : one; var old = new CarrierLocation(chosen, (long)chosen.Value);
        chosen.Set(chosen.Value + 1);
        changes.Add(ReadWitness.Create(new Carrier(), old, Relation.Point(countAxis, 1), new CarrierLocation(chosen, (long)chosen.Value),
            OrdinalNative.G(), new Relation(old, Relation.Point(outcomeAxis, label))));
    }
    private static void Coarsen(List<Relation> rows, List<IElement> losses)
    {
        var keep = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
        for (var start = 0; start < rows.Count;)
        {
            var label = Pure(rows[start]); var end = start;
            if (label is not null) while (end + 1 < rows.Count && Pure(rows[end + 1]) == label) end++;
            if (label is null || start > 0) keep.Add(rows[start]);
            if (label is null || end < rows.Count - 1 || keep.Count == 0) keep.Add(rows[end]);
            start = end + 1;
        }
        foreach (var row in rows.Where(row => !keep.Contains(row))) losses.Add(new Relation(row, OrdinalNative.G(keep.ToArray())));
        rows.RemoveAll(row => !keep.Contains(row));
    }
    private Relation Rules(Relation[] rows)
    {
        var rules = new List<Relation>();
        Relation Condition(Relation row, int sign) { TryReadLandmark(row, out var anchor, out _, out _); return SupportDecisionSurface.Condition(new Carrier(), anchor, Relation.Segment(orderAxis, sign, sign)); }
        Relation Response(params Relation[] witnesses)
        {
            var labels = witnesses.SelectMany(row => Labels(row)).Distinct().Order().ToArray();
            IElement result = labels.Length == 1 ? new CarrierLocation(outcomeAxis, labels[0]) :
                OrdinalNative.G(labels.Select(label => (IElement)new CarrierLocation(outcomeAxis, label)).ToArray());
            return ReadWitness.Create(new Carrier(), OrdinalNative.G(witnesses.Select(row => (IElement)Anchor(row)).ToArray()),
                Relation.Segment(outcomeAxis, 0, 1), result, OrdinalNative.G(), CountSnapshot(witnesses));
        }
        foreach (var row in rows) rules.Add(SupportDecisionSurface.Rule(new Carrier(), Response(row), Condition(row, 0)));
        for (var i = 1; i < rows.Length; i++) rules.Add(SupportDecisionSurface.Rule(new Carrier(), Response(rows[i - 1], rows[i]),
            Condition(rows[i - 1], 1), Condition(rows[i], -1)));
        // Extrapolation is an explicit learned piecewise-constant hypothesis,
        // admitted only after both consequences have been observed somewhere.
        if (rows.SelectMany(Labels).Distinct().Count() == 2)
        {
            rules.Add(SupportDecisionSurface.Rule(new Carrier(), Response(rows[0]), Condition(rows[0], -1)));
            rules.Add(SupportDecisionSurface.Rule(new Carrier(), Response(rows[^1]), Condition(rows[^1], 1)));
        }
        return SupportDecisionSurface.Create(new Carrier(), rules.ToArray());
    }
    private static Relation Anchor(Relation row) { TryReadLandmark(row, out var sample, out _, out _); return sample; }
    private static int[] Labels(Relation row)
    {
        TryReadLandmark(row, out _, out var zero, out var one);
        return (zero.Value > 0 ? new[] { 0 } : Array.Empty<int>()).Concat(one.Value > 0 ? new[] { 1 } : Array.Empty<int>()).ToArray();
    }
    private static Relation CountSnapshot(Relation[] rows) => OrdinalNative.G(rows.Select(row =>
    {
        TryReadLandmark(row, out var sample, out var zero, out var one);
        return (IElement)OrdinalNative.G(sample, new CarrierLocation(zero, (long)zero.Value), new CarrierLocation(one, (long)one.Value));
    }).ToArray());
    private (int Lower, int Upper, int Equal, bool Resolved) Locate(Relation sample, Relation[] rows,
        Func<Relation, Relation, Relation> compare, List<Relation> probes)
    {
        var low = 0; var high = rows.Length - 1;
        while (low <= high)
        {
            var middle = low + (high - low) / 2; var anchor = Anchor(rows[middle]);
            var witness = compare(sample, anchor); comparisons.Set(comparisons.Value + 1); probes.Add(witness);
            if (!SamplingSurface.TryReadComparison(witness, sample, anchor, orderAxis, out var sign)) return (-1, rows.Length, -1, false);
            if (sign == 0) return (middle, middle, middle, true);
            if (sign < 0) high = middle - 1; else low = middle + 1;
        }
        return (high, low, -1, true);
    }
}

internal static class OrdinalNative
{
    public static Relation G(params IElement[] fields) => Relation.OrderedGroup(new Carrier(), fields);
    public static Relation Open(IElement selected, IElement surface) => ReadWitness.Create(new Carrier(), selected, surface,
        new Placeholder(), new Relation(selected, surface), surface);
    public static bool Witness(Relation read, out IElement selected, out IElement surface, out IElement result, out IElement residue, out IElement provenance)
    {
        selected = surface = result = residue = provenance = null!;
        return read.TryReadOrderedGroup(out var f) && f.Length == 5 && ReadWitness.TryRead(read, out selected, out surface, out result, out residue, out provenance);
    }
    public static bool Point(IElement element, out CarrierLocation point)
    {
        if (element is CarrierLocation p && p.Carrier is not null && p.Tick is not null) { point = p; return true; }
        if (element is Relation { Left: CarrierLocation q, Right: Carrier c } && q.Carrier is not null && q.Tick is not null && q.Carrier.SameIdentityAs(c)) { point = q; return true; }
        point = default; return false;
    }
    public static bool Frame(IElement element, out Carrier axis, out long low, out long high)
    {
        if (element is Relation r && Point(r.Left, out var a) && Point(r.Right, out var b) && !a.Carrier.IsUnknown && a.Carrier.SameIdentityAs(b.Carrier))
        { axis = a.Carrier; low = a.Ticks; high = b.Ticks; return true; }
        axis = null!; low = high = 0; return false;
    }
    public static bool ReadPoint(Relation publication, Carrier axis, long minimum, long maximum, out CarrierLocation point)
    {
        point = default;
        if (!Witness(publication, out _, out _, out var result, out _, out _) || !Point(result, out var p) ||
            !p.Carrier.SameIdentityAs(axis) || p.Ticks < minimum || p.Ticks > maximum) return false;
        // A qualified supplier must establish its whole support, not merely a
        // nominal value. No reader consults that supplier's private ancestry.
        if (QualifiedReadSurface.TryReadPublished(publication, out _, out _, out _, out _, out _, out _, out _, out _) &&
            (!FiniteReadSurface.TryReadSupport(publication, out var lower, out var upper) || lower is null || upper is null ||
             lower.Value.Ticks != p.Ticks || upper.Value.Ticks != p.Ticks)) return false;
        point = p; return true;
    }
    public static bool Prediction(Relation read, out CarrierLocation point)
    { point = default; return Witness(read, out _, out _, out var result, out _, out _) && Point(result, out point); }
    public static bool Alternatives(Relation read, out CarrierLocation[] alternatives)
    {
        alternatives = [];
        if (!Witness(read, out _, out _, out var result, out var residue, out _)) return false;
        IElement support = result;
        if (residue is Relation remainder && remainder.TryReadOrderedGroup(out var retained) && retained.Length > 0 && retained[0] is Relation observed) support = observed;
        if (support is Relation group && group.TryReadOrderedGroup(out var fields) && fields.All(field => Point(field, out _)))
        { alternatives = fields.Select(field => { Point(field, out var p); return p; }).ToArray(); return alternatives.Length > 0; }
        if (Point(result, out var single)) { alternatives = [single]; return true; }
        return false;
    }
}
