using Gojo.Core;
using Gojo.Structures;
using static Minds.TwoSpace.Graph;

namespace Minds.TwoSpace;

/// <summary>
/// Observes published-value reads through Gojo affine composition. The average,
/// mismatch and movement are connected calculations, not host arithmetic.
/// The selected observer does not send feedback to the two spaces.
/// </summary>
public sealed class PairObserver
{
    private readonly Carrier basis;
    public PairObserver(Carrier comparisonBasis) => basis = comparisonBasis;

    public Relation Observe(Space a, Space b, Relation? previous = null)
    {
        var ap = a.Predict(); var bp = b.Predict();
        var av = Known(F(ap)[2], ap);
        // Read the actual authored B-to-A calibration, whatever its selected scale.
        var bToA = R(F(F(a.Inbox)[0])[4]);
        var bv = Combine((Known(F(bp)[2], bp), bToA));
        var mean = Combine((av, Map(2, 1)), (bv, Map(2, 1)));
        var gap = Combine((av, Map(1, -1)), (bv, Map(1, 1)));
        var prior = previous is null ? null : F(previous);
        var da = Difference(prior is null ? av : R(prior[0]), av);
        var db = Difference(prior is null ? bv : R(prior[1]), bv);
        var movement = Combine((Magnitude(da), Map(1, 1)), (Magnitude(db), Map(1, 1)));
        var meanChange = Difference(prior is null ? mean : R(prior[2]), mean);
        var ar = a.ReferenceComparison(); var br = b.ReferenceComparison();
        var aReferenceGap = Known(F(ar)[2], ar);
        var bReferenceGap = Combine((Known(F(br)[2], br), bToA));
        return G(av, bv, mean, gap, da, db, movement, meanChange, aReferenceGap, bReferenceGap);
    }

    private Relation Map(long inputExtent, long outputExtent) =>
        new(Relation.Segment(basis, 0, inputExtent), Relation.Segment(basis, 0, outputExtent));

    private static Relation Known(IElement scalar, IElement evidence)
    {
        var expression = BoundedQuantity.Affine(new Carrier(), scalar, G(), evidence);
        Require(BoundedQuantity.TryObserve(expression, out var witness), "observer scalar");
        return witness;
    }

    private Relation Combine(params (Relation Witness, Relation Map)[] terms)
    {
        var ports = G(terms.Select(term => (IElement)new Relation(F(term.Witness)[1], term.Map)).ToArray());
        Require(BoundedQuantity.TryCombine(new Relation(ports, Relation.Point(basis, 1)), out var witness),
            "observer composition");
        return witness;
    }

    private Relation Difference(Relation before, Relation after) =>
        Combine((before, Map(1, -1)), (after, Map(1, 1)));

    private Relation Magnitude(Relation value)
    {
        Require(BoundedQuantity.TryObserveConformance(G(value, Relation.Segment(basis, 0, 0), G()),
            out var magnitude), "observer distance from zero");
        // Conformance exposes a bound witness; a known result can be used as an affine source.
        return Known(Scalar(magnitude), magnitude);
    }

    public static IElement Scalar(Relation witness)
    {
        Require(BoundedQuantity.TryReadBounds(witness, out _, out var lower, out var upper, out _),
            "observer bounds");
        Require(ProjectionResidue.TryRead(lower, out _, out var ln, out var ld) &&
            ProjectionResidue.TryRead(upper, out _, out var un, out var ud) && ln == un && ld == ud,
            "an exact observer result (a range must not be collapsed)");
        return lower;
    }
}
