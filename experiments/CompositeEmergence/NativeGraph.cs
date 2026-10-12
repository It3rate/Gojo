using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;

namespace CompositeEmergence;

/// <summary>Narrow builders and readers for this experiment's connected native
/// surfaces. No helper owns a private semantic table.</summary>
public static class NativeGraph
{
    public const int MaximumLedgerCount = 64;
    public const int ReceiptLimit = 8;
    public static Relation G(params IElement[] elements) => Relation.OrderedGroup(new Carrier(), elements);
    public static IElement[] Parts(Relation relation, int? length = null)
    {
        if (relation.TryReadOrderedGroup(out var fields) && (length is null || fields.Length == length)) return fields;
        throw new ArgumentException("The supplied graph lacks the declared ordered surface.", nameof(relation));
    }
    public static bool TryPoint(IElement element, out CarrierLocation point)
    {
        if (element is CarrierLocation location && location.Carrier is not null && location.Tick is not null && !location.Carrier.IsUnknown)
        { point = location; return true; }
        if (element is Relation { Left: CarrierLocation located, Right: Carrier axis } && located.Carrier is not null &&
            located.Tick is not null && located.Carrier.SameIdentityAs(axis) && !axis.IsUnknown)
        { point = located; return true; }
        point = default; return false;
    }
    public static bool TryValue(Relation publication, Carrier axis, out CarrierLocation point)
    {
        IElement result = publication;
        if (ReadWitness.TryRead(publication, out _, out _, out var exposed, out _, out _))
        {
            result = exposed;
            if (QualifiedReadSurface.TryReadPublished(publication, out _, out _, out _, out _, out _, out _, out _, out _) &&
                (!FiniteReadSurface.TryReadSupport(publication, out var low, out var high) || low is null || high is null || low.Value.Ticks != high.Value.Ticks))
            { point = default; return false; }
        }
        else if (Equation.TryReadPrediction(publication, out var predicted, out _)) result = predicted;
        if (TryPoint(result, out var value) && value.Carrier.SameIdentityAs(axis))
        { point = new CarrierLocation(axis, value.Ticks); return true; }
        point = default; return false;
    }
    public static Relation Publication(Carrier axis, long ticks, IElement source, long timestamp = 0)
    {
        var time = new Carrier();
        return ReadWitness.Create(new Carrier(), source, Relation.Segment(axis, -16, 16), new CarrierLocation(axis, ticks), G(),
            G(Identity(source), Relation.Point(time, timestamp)));
    }
    public static Relation Open(IElement supplied, IElement attempted) => ReadWitness.Create(new Carrier(), supplied, attempted,
        new Placeholder(), new Relation(supplied, attempted), attempted);
    public static bool Pass(Relation claim) => OperationClaim.TryReadResolvedViolation(claim, out var violation) && violation.Ticks == 0;
    public static bool TryTimestamp(Relation publication, out long timestamp)
    {
        timestamp = 0;
        if (!ReadWitness.TryRead(publication, out _, out _, out _, out _, out var provenance) || provenance is not Relation group ||
            !group.TryReadOrderedGroup(out var fields) || fields.Length < 2 || !TryPoint(fields[1], out var time)) return false;
        timestamp = time.Ticks; return true;
    }
    /// <summary>Captures only the advertised coordinate, observation identity,
    /// read frame and time. It deliberately forgets producer-private detail.</summary>
    public static Relation CapturePublication(Relation publication, Carrier axis)
    {
        if (!TryValue(publication, axis, out var point)) return Open(Identity(publication), Relation.Segment(axis, -16, 16));
        TryTimestamp(publication, out var time);
        var timeAxis = new Carrier(); var frozen = new CarrierLocation(axis, point.Ticks);
        return ReadWitness.Create(new Carrier(), Identity(publication), Relation.Segment(axis, -16, 16), frozen, G(),
            G(Identity(publication), Relation.Point(timeAxis, time)));
    }
    public static Relation FreezeValue(Relation publication)
    {
        if (!ReadWitness.TryRead(publication, out _, out _, out var result, out _, out _) || !TryPoint(result, out var point))
            return Open(Identity(publication), new Placeholder());
        return CapturePublication(publication, point.Carrier);
    }
    public static IElement Identity(Relation publication) => Identity((IElement)publication);
    public static IElement Identity(IElement supplied)
    {
        if (supplied is not Relation publication || !ReadWitness.TryRead(publication, out var source, out _, out _, out _, out _)) return supplied;
        if (source is Relation group && group.TryReadOrderedGroup(out var fields) && fields.Length == 3 && fields[0] is Relation && fields[1] is Relation physical)
            return Identity(physical);
        return source;
    }
    public static Relation BindFeedback(Relation decision, Relation physical, Carrier axis)
    {
        var calibration = Equation.Affine(axis, 0, 1, axis, 0, 1);
        var correspondence = G(decision, physical, calibration);
        if (!TryValue(physical, axis, out var value)) return Open(correspondence, calibration);
        var projected = Equation.Predict(calibration, value, axis);
        return ReadWitness.Create(new Carrier(), correspondence, calibration, new CarrierLocation(axis, value.Ticks), projected.Right,
            G(decision, physical, projected));
    }
    public static bool IsBoundTo(Relation publication, Relation decision, Carrier axis)
    {
        if (!ReadWitness.TryRead(publication, out var selected, out var surface, out _, out _, out _) || selected is not Relation group ||
            !group.TryReadOrderedGroup(out var fields) || fields.Length != 3 || !ReferenceEquals(fields[0], decision) ||
            fields[1] is not Relation physical || fields[2] is not Relation calibration || !ReferenceEquals(surface, calibration) ||
            !TryValue(physical, axis, out var original) || !TryValue(publication, axis, out var actual) ||
            !Equation.TryPredict(calibration, original, axis, out var prediction) || !Equation.TryReadPrediction(prediction, out var expected, out _)) return false;
        return expected.Ticks == actual.Ticks && expected.Carrier.SameIdentityAs(axis);
    }
    public static bool ReceiptSeen(IEnumerable<Relation> receipts, Relation decision, Relation physical)
    {
        var identity = Identity(physical);
        foreach (var receipt in receipts)
            if (ReadWitness.TryRead(receipt, out _, out _, out _, out _, out var provenance) && provenance is Relation group &&
                group.TryReadOrderedGroup(out var fields) && fields.Length >= 2 &&
                (ReferenceEquals(fields[0], Identity(decision)) || ReferenceEquals(fields[1], identity))) return true;
        return false;
    }
    /// <summary>Native bounded signed-utility ledger. Count, positive utility,
    /// negative utility each have explicit stop boundaries.</summary>
    public static Relation Ledger(Carrier countAxis, Carrier utilityAxis)
    {
        var count = new Carrier(countAxis.UnitShape); var positive = new Carrier(utilityAxis.UnitShape); var negative = new Carrier(utilityAxis.UnitShape);
        foreach (var carrier in new[] { count, positive, negative })
            carrier.Attach(new Relation(Relation.Point(carrier, MaximumLedgerCount * 1_000_000L), Relation.Point(carrier, MaximumLedgerCount * 1_000_000L)));
        count.Attach(new Relation(Relation.Point(count, MaximumLedgerCount), Relation.Point(count, MaximumLedgerCount)));
        return G(count, positive, negative);
    }
    public static Carrier Count(Relation ledger) => (Carrier)Parts(ledger, 3)[0];
    public static Relation CaptureLedger(Relation ledger) => G(Parts(ledger, 3).Cast<Carrier>().Select(c => (IElement)new CarrierLocation(c, checked((long)c.Value))).ToArray());
    public static Relation Update(Relation ledger, long signedUtility)
    {
        if (signedUtility is < -1_000_000 or > 1_000_000) return Open(Relation.Point(new Carrier(), signedUtility), ledger);
        var fields = Parts(ledger, 3).Cast<Carrier>().ToArray(); var before = CaptureLedger(ledger);
        var losses = new List<IElement>();
        if (fields[0].Value >= MaximumLedgerCount)
            foreach (var carrier in fields)
            {
                var amount = checked((long)carrier.Value); var law = Equation.Affine(carrier, 0, 2, carrier, 0, 1);
                var read = Equation.Predict(law, new CarrierLocation(carrier, amount), carrier);
                Equation.TryReadPrediction(read, out var target, out var residue); carrier.Set(target.Ticks);
                losses.Add(ReadWitness.Create(new Carrier(), new CarrierLocation(carrier, amount), law, new CarrierLocation(carrier, target.Ticks), residue, G()));
            }
        fields[0].Add(1); (signedUtility >= 0 ? fields[1] : fields[2]).Add((ulong)Math.Abs(signedUtility));
        return ReadWitness.Create(new Carrier(), before, ledger, CaptureLedger(ledger), G(losses.ToArray()),
            G(before, Relation.Point(new Carrier(), signedUtility), CaptureLedger(ledger)));
    }
    public static Relation Score(Relation ledger, Carrier scoreAxis, long complexity = 0)
    {
        var fields = Parts(ledger, 3).Cast<Carrier>().ToArray(); var frozen = CaptureLedger(ledger);
        if (fields[0].Value == 0) return Open(frozen, Relation.Segment(fields[0], 1, MaximumLedgerCount));
        var sum = Equation.Sum(scoreAxis, Equation.WeightedPort(fields[1], 0, 1, 0, 256), Equation.WeightedPort(fields[2], 0, 1, 0, -256));
        var total = Equation.Predict(sum, new Relation(new CarrierLocation(fields[1], checked((long)fields[1].Value)), new CarrierLocation(fields[2], checked((long)fields[2].Value))), scoreAxis);
        if (!Equation.TryReadPrediction(total, out var value, out _)) return Open(frozen, sum);
        var mean = Equation.Affine(fields[0], 0, checked((long)fields[0].Value), scoreAxis, -complexity, value.Ticks - complexity);
        var result = Equation.Predict(mean, new CarrierLocation(fields[0], 1), scoreAxis);
        Equation.TryReadPrediction(result, out var score, out var residue);
        return ReadWitness.Create(new Carrier(), frozen, G(sum, mean), score, residue, G(frozen, total, result));
    }
}
