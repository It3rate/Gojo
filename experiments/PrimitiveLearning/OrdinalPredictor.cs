using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;

namespace PrimitiveLearning;

/// <summary>Small native empirical transition rows. Context ports retain their
/// declared axes; counts forecast an ordinal event, not a brightness amount or
/// a guaranteed sensor bound.</summary>
public sealed class OrdinalPredictor
{
    // state = G(policy, G(context -> consequence counts), context memory,
    //           latest patch, G(update counter, mismatch counter))
    // policy = G(output frame, G(ordered context ports), count budget,
    //            minimum support, winning margin, aging law, row budget)
    private readonly Carrier orderAxis, countAxis, updates, mismatches;
    private readonly Carrier[] contextAxes;
    private readonly Relation[] ports;
    private readonly Relation policy;
    public Relation Root { get; private set; }
    public int Updates => checked((int)updates.Value);
    public int Mismatches => checked((int)mismatches.Value);
    public int RowCount => Rows().Length;

    public OrdinalPredictor(Carrier orderAxis, int memoryDepth = 2) : this(orderAxis, RepeatedAxes(orderAxis, memoryDepth)) { }
    public OrdinalPredictor(Carrier orderAxis, params Carrier[] contextAxes)
    {
        if (contextAxes.Length > 4 || orderAxis.IsUnknown || contextAxes.Any(axis => axis.IsUnknown))
            throw new ArgumentException("Use at most four declared concrete context ports.", nameof(contextAxes));
        this.orderAxis = orderAxis; this.contextAxes = contextAxes.ToArray();
        countAxis = new Carrier(); updates = new Carrier(); mismatches = new Carrier(); var position = new Carrier();
        ports = contextAxes.Select((axis, index) => new Relation(Relation.Point(position, index), Relation.Segment(axis, -16, 16))).ToArray();
        policy = OrdinalNative.G(Relation.Segment(orderAxis, -1, 1), OrdinalNative.G(ports), Relation.Point(countAxis, 255),
            Relation.Point(countAxis, 4), Relation.Point(countAxis, 2),
            new Relation(Relation.Segment(countAxis, 0, 2), Relation.Segment(countAxis, 0, 1)), Relation.Point(new Carrier(), 128));
        Root = OrdinalNative.G(policy, OrdinalNative.G(), OrdinalNative.G(), OrdinalNative.Open(policy, policy), OrdinalNative.G(updates, mismatches));
    }
    private OrdinalPredictor(Relation root)
    {
        if (!TryReadModel(root, out var p, out _, out _) || !p.TryReadOrderedGroup(out var f) ||
            !OrdinalNative.Frame(f[0], out var axis, out _, out _) || f[1] is not Relation group ||
            !group.TryReadOrderedGroup(out var entries) || !OrdinalNative.Point(f[2], out var count) ||
            !root.TryReadOrderedGroup(out var state) || state[4] is not Relation counters ||
            !counters.TryReadOrderedGroup(out var cells) || cells.Length != 2 || cells[0] is not Carrier u || cells[1] is not Carrier m)
            throw new ArgumentException("The restored predictor has no readable native contract.", nameof(root));
        orderAxis = axis; countAxis = count.Carrier; ports = entries.Cast<Relation>().ToArray();
        contextAxes = ports.Select(port => { OrdinalNative.Frame(port.Right, out var a, out _, out _); return a; }).ToArray();
        updates = u; mismatches = m; policy = p; Root = root;
    }
    public static OrdinalPredictor FromRoot(Relation root) => new(root);
    public void ResetContext() => Rebuild(Rows(), OrdinalNative.G(), OrdinalNative.Open(policy, policy));

    public Relation Predict(params Relation[] contextPublications)
    {
        var rows = Rows(); var supplied = contextPublications.Length == 0 ? Memory() : contextPublications;
        var captured = Capture(supplied, out var key);
        if (captured is null) return Finish(new Placeholder(), OrdinalNative.G(), OrdinalNative.Open(OrdinalNative.G(supplied), policy), OrdinalNative.G());
        var row = rows.FirstOrDefault(candidate => SameKey(Key(candidate), key!));
        if (row is null) return Finish(new Placeholder(), OrdinalNative.G(), OrdinalNative.Open(OrdinalNative.G(captured), policy), OrdinalNative.G());
        TryReadRow(row, out _, out var counts); var snapshot = Snapshot(row); var totalRole = Counts(row);
        var bindings = ports.Select((port, index) => SupportDecisionSurface.Binding(new Carrier(), port, captured[index])).ToList();
        bindings.Add(SupportDecisionSurface.Binding(new Carrier(), totalRole, Sum(counts.Select(cell => ((int)cell.Value, 1)).ToArray())));
        var rules = new List<Relation>(); var alternativePoints = new List<IElement>();
        for (var candidate = 0; candidate < counts.Length; candidate++)
        {
            if (counts[candidate].Value > 0) alternativePoints.Add(new CarrierLocation(orderAxis, candidate - 1));
            var conditions = new List<Relation>();
            for (var index = 0; index < contextAxes.Length; index++)
            {
                OrdinalNative.Point(KeyFields(key!)[index], out var location);
                conditions.Add(SupportDecisionSurface.Condition(new Carrier(), ports[index], Relation.Segment(contextAxes[index], location.Ticks, location.Ticks)));
            }
            conditions.Add(SupportDecisionSurface.Condition(new Carrier(), totalRole, Relation.Segment(countAxis, 4, 255)));
            for (var other = 0; other < counts.Length; other++)
            {
                if (other == candidate) continue;
                var role = new Relation(counts[candidate], counts[other]);
                bindings.Add(SupportDecisionSurface.Binding(new Carrier(), role, Sum(((int)counts[candidate].Value, 1), ((int)counts[other].Value, -1))));
                conditions.Add(SupportDecisionSurface.Condition(new Carrier(), role, Relation.Segment(countAxis, 2, 255)));
            }
            rules.Add(SupportDecisionSurface.Rule(new Carrier(), new CarrierLocation(orderAxis, candidate - 1), conditions.ToArray()));
        }
        var ruleSurface = SupportDecisionSurface.Create(new Carrier(), rules.ToArray());
        var selection = SupportDecisionSurface.Select(new Carrier(), ruleSurface, bindings.ToArray());
        IElement result = new Placeholder();
        if (SupportDecisionSurface.TryReadSelection(selection, out _, out var response, out _, out _) && response is CarrierLocation forecast) result = forecast;
        return Finish(result, OrdinalNative.G(alternativePoints.ToArray()), selection, OrdinalNative.G(snapshot, ruleSurface));

        Relation Finish(IElement result, Relation alternatives, IElement selection, IElement counts)
        {
            var prediction = ReadWitness.Create(new Carrier(), OrdinalNative.G(captured ?? supplied), OrdinalNative.G(policy, counts), result,
                OrdinalNative.G(alternatives, result is Placeholder ? new Relation(selection, policy) : OrdinalNative.G()),
                OrdinalNative.G(selection, key ?? (IElement)new Placeholder(), counts));
            Rebuild(rows, captured is null ? CurrentMemory() : OrdinalNative.G(captured), prediction); return prediction;
        }
    }

    public Relation Observe(Relation prediction, Relation actual, bool learn = true)
    {
        var rows = Rows().ToList(); var changes = new List<IElement>(); var losses = new List<IElement>();
        var validActual = OrdinalNative.ReadPoint(actual, orderAxis, -1, 1, out var point);
        var owned = OrdinalNative.Witness(prediction, out var selected, out var readSurface, out _, out _, out _) &&
            readSurface is Relation surface && surface.TryReadOrderedGroup(out var premise) && premise.Length == 2 && ReferenceEquals(premise[0], policy);
        Relation[]? captured = null; Relation? key = null;
        if (owned && selected is Relation inputs && inputs.TryReadOrderedGroup(out var publications) && publications.All(item => item is Relation))
            captured = Capture(publications.Cast<Relation>().ToArray(), out key);
        IElement claim = new Relation(prediction, actual);
        if (validActual && TryReadPrediction(prediction, out var expected))
        {
            claim = OperationClaim.Equality(new Carrier(), point, expected, new Carrier(orderAxis.UnitShape));
            if (OperationClaim.TryReadResolvedViolation((Relation)claim, out var violation) && violation.Ticks > 0)
                mismatches.Set(mismatches.Value + 1);
        }
        if (validActual && captured is not null && learn)
        {
            var row = rows.FirstOrDefault(candidate => SameKey(Key(candidate), key!));
            if (row is null && rows.Count >= 128)
            {
                losses.Add(new Relation(key!, policy));
                return Finish();
            }
            if (row is null) { row = NewRow(key!); rows.Add(row); }
            TryReadRow(row, out _, out var cells);
            var total = Sum(cells.Select(cell => ((int)cell.Value, 1)).ToArray());
            if (OrdinalNative.Prediction(total, out var amount) && amount.Ticks >= 255)
            {
                policy.TryReadOrderedGroup(out var contract); var aging = (Relation)contract[5];
                foreach (var cell in cells)
                {
                    var before = new CarrierLocation(cell, (long)cell.Value);
                    aging.TryProject(new CarrierLocation(countAxis, before.Ticks), countAxis, out var projection);
                    var target = (CarrierLocation)projection.Left; cell.Set(target.Ticks);
                    losses.Add(ReadWitness.Create(new Carrier(), before, aging, new CarrierLocation(cell, (long)cell.Value), projection.Right,
                        new Relation(total, projection)));
                }
            }
            var chosen = cells[(int)point.Ticks + 1]; var old = new CarrierLocation(chosen, (long)chosen.Value);
            chosen.Set(chosen.Value + 1); updates.Set(updates.Value + 1);
            changes.Add(ReadWitness.Create(new Carrier(), actual, new Relation(Relation.Point(orderAxis, point.Ticks), chosen),
                new CarrierLocation(chosen, (long)chosen.Value), OrdinalNative.G(), new Relation(old, actual)));
        }
        if (!validActual || captured is null) losses.Add(OrdinalNative.Open(OrdinalNative.G(prediction, actual), policy));
        return Finish();

        Relation Finish()
        {
            var memory = CurrentMemory();
            if (validActual && contextAxes.All(axis => axis.SameIdentityAs(orderAxis)))
            {
                var history = Memory().ToList(); history.Add(Focus(actual, point));
                memory = OrdinalNative.G(history.TakeLast(contextAxes.Length).Cast<IElement>().ToArray());
            }
            var feedback = ReadWitness.Create(new Carrier(), prediction, OrdinalNative.G(policy, actual, Relation.Point(new Carrier(), learn ? 1 : 0)),
                validActual ? point : new Placeholder(), OrdinalNative.G(claim, OrdinalNative.G(losses.ToArray())), OrdinalNative.G(actual, OrdinalNative.G(changes.ToArray())));
            Rebuild(rows.ToArray(), memory, feedback); return feedback;
        }
    }

    public static bool TryReadPrediction(Relation prediction, out CarrierLocation result) => OrdinalNative.Prediction(prediction, out result);
    public static bool TryReadAlternatives(Relation prediction, out CarrierLocation[] alternatives) => OrdinalNative.Alternatives(prediction, out alternatives);
    public static bool TryReadModel(Relation root, out Relation policy, out Relation rows, out Relation context)
    {
        policy = rows = context = null!;
        if (!root.TryReadOrderedGroup(out var f) || f.Length != 5 || f[0] is not Relation p ||
            !p.TryReadOrderedGroup(out var contract) || contract.Length != 7 ||
            !OrdinalNative.Frame(contract[0], out _, out var low, out var high) || low != -1 || high != 1 ||
            contract[1] is not Relation ports || !ports.TryReadOrderedGroup(out var portFields) || portFields.Length > 4 ||
            portFields.Any(field => field is not Relation port || !OrdinalNative.Frame(port.Right, out _, out var l, out var h) || l != -16 || h != 16) ||
            !OrdinalNative.Point(contract[2], out var cap) || cap.Ticks != 255 || !OrdinalNative.Point(contract[3], out var minimum) || minimum.Ticks != 4 ||
            !OrdinalNative.Point(contract[4], out var margin) || margin.Ticks != 2 ||
            !OrdinalNative.Point(contract[6], out var rowBudget) || rowBudget.Ticks != 128 ||
            f[1] is not Relation r || !r.TryReadOrderedGroup(out var entries) || entries.Length > rowBudget.Ticks || entries.Any(entry => entry is not Relation row || !TryReadRow(row, out _, out _)) ||
            f[2] is not Relation c || !c.TryReadOrderedGroup(out _)) return false;
        policy = p; rows = r; context = c; return true;
    }
    public static bool TryReadRow(Relation row, out Relation key, out Carrier[] counts)
    {
        key = null!; counts = [];
        if (!row.TryReadOrderedGroup(out var f) || f.Length != 2 || f[0] is not Relation k ||
            !k.TryReadOrderedGroup(out var values) || values.Any(value => !OrdinalNative.Point(value, out _)) ||
            f[1] is not Relation c || !c.TryReadOrderedGroup(out var cells) || cells.Length != 3) return false;
        var found = new List<Carrier>(); Carrier? axis = null;
        for (var i = 0; i < cells.Length; i++)
        {
            if (cells[i] is not Relation entry || !OrdinalNative.Point(entry.Left, out var outcome) || outcome.Ticks != i - 1 ||
                axis is not null && !axis.SameIdentityAs(outcome.Carrier) || entry.Right is not Carrier carrier || carrier.Value > 255) return false;
            axis = outcome.Carrier; found.Add(carrier);
        }
        key = k; counts = found.ToArray(); return true;
    }
    private Relation[] Rows() { TryReadModel(Root, out _, out var group, out _); group.TryReadOrderedGroup(out var entries); return entries.Cast<Relation>().ToArray(); }
    private Relation CurrentMemory() { TryReadModel(Root, out _, out _, out var memory); return memory; }
    private Relation[] Memory() { CurrentMemory().TryReadOrderedGroup(out var entries); return entries.Cast<Relation>().ToArray(); }
    private void Rebuild(Relation[] rows, Relation memory, Relation latest) =>
        Root = OrdinalNative.G(policy, OrdinalNative.G(rows), memory, latest, OrdinalNative.G(updates, mismatches));
    private Relation NewRow(Relation key) => OrdinalNative.G(key, OrdinalNative.G(Enumerable.Range(-1, 3).Select(value =>
        (IElement)new Relation(Relation.Point(orderAxis, value), new Carrier(countAxis.UnitShape))).ToArray()));
    private Relation[]? Capture(Relation[] publications, out Relation? key)
    {
        key = null; if (publications.Length != contextAxes.Length) return null;
        var captured = new List<Relation>(); var values = new List<IElement>();
        for (var i = 0; i < publications.Length; i++)
        {
            if (!OrdinalNative.ReadPoint(publications[i], contextAxes[i], -16, 16, out var point)) return null;
            captured.Add(Focus(publications[i], point)); values.Add(Relation.Point(point.Carrier, point.Ticks));
        }
        key = OrdinalNative.G(values.ToArray()); return captured.ToArray();
    }
    private static Relation Focus(Relation publication, CarrierLocation point)
    {
        // Reuse this exact captured grammar instead of wrapping the previous
        // focus again on every prediction. Its provenance keeps the supplied
        // event connected, while the frozen coordinate remains independent.
        if (OrdinalNative.Witness(publication, out var selected, out var surface, out var result, out _, out var provenance) &&
            selected is CarrierLocation prior && result is CarrierLocation current &&
            ReferenceEquals(prior.Tick, current.Tick) && OrdinalNative.Frame(surface, out var axis, out var low, out var high) &&
            axis.SameIdentityAs(point.Carrier) && low == -16 && high == 16 && provenance is Relation correspondence &&
            correspondence.TryReadOrderedGroup(out var parts) && parts.Length == 2 && parts[0] is Relation &&
            parts[1] is CarrierLocation captured && ReferenceEquals(captured.Tick, current.Tick)) return publication;
        var frozen = new CarrierLocation(point.Carrier, point.Ticks);
        return ReadWitness.Create(new Carrier(), frozen, Relation.Segment(point.Carrier, -16, 16), frozen, OrdinalNative.G(),
            OrdinalNative.G(publication, frozen));
    }
    private static Relation Key(Relation row) { TryReadRow(row, out var key, out _); return key; }
    private static Relation Counts(Relation row) { row.TryReadOrderedGroup(out var f); return (Relation)f[1]; }
    private static IElement[] KeyFields(Relation key) { key.TryReadOrderedGroup(out var f); return f; }
    private static bool SameKey(Relation a, Relation b)
    {
        var left = KeyFields(a); var right = KeyFields(b); if (left.Length != right.Length) return false;
        return left.Zip(right).All(pair => OrdinalNative.Point(pair.First, out var x) && OrdinalNative.Point(pair.Second, out var y) &&
            x.Carrier.SameIdentityAs(y.Carrier) && x.Ticks == y.Ticks);
    }
    private static Relation Snapshot(Relation row)
    {
        TryReadRow(row, out var key, out var cells);
        return OrdinalNative.G(key, OrdinalNative.G(cells.Select(cell => (IElement)new CarrierLocation(cell, (long)cell.Value)).ToArray()));
    }
    private Relation Sum(params (int Value, int Coefficient)[] operands)
    {
        var ports = operands.Select(operand => Equation.WeightedPort(countAxis, 0, 1, 0, operand.Coefficient)).ToArray();
        IElement source = new CarrierLocation(countAxis, operands[0].Value);
        foreach (var operand in operands.Skip(1)) source = new Relation(source, new CarrierLocation(countAxis, operand.Value));
        return Equation.PredictWitness(new Carrier(), Equation.Sum(countAxis, ports), source, countAxis);
    }
    private static Carrier[] RepeatedAxes(Carrier axis, int depth)
    {
        if (depth is < 0 or > 4) throw new ArgumentOutOfRangeException(nameof(depth));
        return Enumerable.Repeat(axis, depth).ToArray();
    }
}
