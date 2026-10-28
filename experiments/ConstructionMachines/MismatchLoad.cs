using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using static Gojo.Experiments.ConstructionMachines.MachineExpressions;

namespace Gojo.Experiments.ConstructionMachines;

/// <summary>
/// An explicitly selected level-accounting policy. This is review load measured
/// from a discrepancy, not confidence or conserved physical energy. A fresh
/// sample changes the level once; reductions remain visible as released load.
/// </summary>
public sealed class MismatchLoad
{
    // G(source-to-load calibration, remaining, introduced, released, clock,
    // timeline, consumed source events, last accepted receipt).
    public Relation Calibration { get; }
    public Carrier Remaining { get; }
    public Carrier Introduced { get; }
    public Carrier Released { get; }
    public Relation Timeline { get; private set; }
    public IElement LastReceipt { get; private set; } = new Placeholder();
    public Relation Surface => G(Calibration, Remaining, Introduced, Released, clock, Timeline, G(consumed.ToArray()), LastReceipt);
    private readonly Carrier clock;
    private readonly IElement initial;
    private readonly List<IElement> consumed = [];
    private readonly List<Relation> steps = [];

    public MismatchLoad(Carrier source)
    {
        Remaining = new Carrier(source.UnitShape); Introduced = new Carrier(source.UnitShape); Released = new Carrier(source.UnitShape);
        Calibration = new Relation(Relation.Segment(source, 0, 1), Relation.Segment(Remaining, 0, 1));
        clock = new Carrier(); initial = Capture(); Timeline = TimeStepSequence.Create(new Carrier(), clock, initial);
    }
    private MismatchLoad(Relation surface)
    {
        var f = F(surface);
        if (f.Length != 8) throw new ArgumentException("An exact mismatch-load surface is required.");
        Calibration = (Relation)f[0]; Remaining = (Carrier)f[1]; Introduced = (Carrier)f[2]; Released = (Carrier)f[3]; clock = (Carrier)f[4];
        Timeline = (Relation)f[5]; consumed.AddRange(F(f[6])); LastReceipt = f[7];
        if (!TimeStepSequence.TryRead(Timeline, out _, out var start, out var retained)) throw new ArgumentException("The load timeline is unavailable.");
        initial = start; steps.AddRange(retained);
    }
    public static MismatchLoad Restore(Relation surface) => new(surface);

    public bool TrySample(IElement signedDiscrepancy, IElement sourceEvent, IElement evidence, out Relation receipt)
    {
        if (consumed.Any(e => ReferenceEquals(e, sourceEvent)) ||
            !ProjectionResidue.TryRead(signedDiscrepancy, out var axis, out _, out _) ||
            !TimeStepSequence.TryReadSpan((Relation)Calibration.Left, out var a, out _) || !axis.SameIdentityAs(a.Carrier))
        {
            receipt = ReadWitness.Create(new Carrier(), signedDiscrepancy, Calibration, new Placeholder(), G(sourceEvent, signedDiscrepancy), evidence);
            return false;
        }
        var magnitude = Abs(Fraction.Read(signedDiscrepancy));
        var level = magnitude.Truncate(1); var before = Capture(); var delta = checked(level - (long)Remaining.Value);
        var arrivals = delta >= 0
            ? G(new CarrierLocation(Remaining, delta), new CarrierLocation(Introduced, delta))
            : G(new CarrierLocation(Remaining, delta), new CarrierLocation(Released, -delta));
        var settled = Carrier.ReceiveTogether(arrivals);
        var after = Capture();
        var introducedMap = new Relation(Relation.Segment(Introduced, 0, 1), Relation.Segment(Remaining, 0, 1));
        var releasedMap = new Relation(Relation.Segment(Released, 0, 1), Relation.Segment(Remaining, 0, 1));
        introducedMap.TryProject(new CarrierLocation(Introduced, Introduced.Value), Remaining, out var incoming);
        releasedMap.TryProject(new CarrierLocation(Released, Released.Value), Remaining, out var outgoing);
        var total = Equation.PredictWitness(new Carrier(), Equation.Sum(Remaining, Equation.UnitPort(Remaining), Equation.UnitPort(Remaining)),
            new Relation(new CarrierLocation(Remaining, Remaining.Value), outgoing.Left), Remaining);
        var identity = OperationClaim.Equality(new Carrier(), incoming.Left, F(total)[2], new Carrier(Remaining.UnitShape));
        receipt = ReadWitness.Create(new Carrier(), G(sourceEvent, signedDiscrepancy), Calibration,
            G(before, after, new CarrierLocation(Remaining, delta), identity),
            (magnitude - new Fraction(level)).Encode(axis), G(evidence, settled, arrivals, introducedMap, releasedMap, incoming, outgoing, total));
        consumed.Add(sourceEvent); LastReceipt = receipt;
        steps.Add(TimeStepSequence.Step(new Carrier(), clock, (long)clock.Value, checked((long)clock.Value + 1), before, receipt, after, F(receipt)[3]));
        clock.Add(1); Timeline = TimeStepSequence.Create(new Carrier(), clock, initial, steps.ToArray());
        return true;
    }
    private Relation Capture() => G(new CarrierLocation(Remaining, checked((long)Remaining.Value)),
        new CarrierLocation(Introduced, checked((long)Introduced.Value)), new CarrierLocation(Released, checked((long)Released.Value)));
    internal static Fraction Abs(Fraction value) => new(System.Numerics.BigInteger.Abs(value.N), value.D);
}
