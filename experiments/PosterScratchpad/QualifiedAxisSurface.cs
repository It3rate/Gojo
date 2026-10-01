using Gojo.Core;
using Gojo.Encoding;
using Gojo.Equations;
using Gojo.Structures;
using static PosterScratchpad.PosterSurfaces;

namespace PosterScratchpad;

// A local experiment in unreduced, basis-bearing observations. No confidence
// float or polarity enum owns meaning: source value, span, opposed face,
// explicit calibration, unresolved cell and provenance are Gojo structure.
internal static class QualifiedAxisSurface
{
    internal static Relation Words(string text) => EncodedTextSurface.Create(new Carrier(), text);
    internal static CarrierLocation Value(Relation witness)
    {
        if (!ReadWitness.TryRead(witness, out _, out _, out var result, out _, out _) || result is not CarrierLocation value)
            throw new InvalidOperationException("Expected a scalar read witness.");
        return value;
    }
    internal static Relation Project(Relation surface, CarrierLocation input, Carrier output)
    {
        if (!surface.TryProject(input, output, out var prediction) ||
            !Equation.TryCreatePredictionWitness(new Carrier(), surface, input, prediction, out var witness))
            throw new InvalidOperationException("Expected an explicit frame projection.");
        return witness;
    }
    internal static Relation Calculate(Relation surface, IElement input, Carrier output) =>
        Equation.PredictWitness(new Carrier(), surface, input, output);
    internal static Relation Pair(string positive, string inverse, IElement footing)
    {
        var forward = new Carrier();
        return Group(Relation.Point(forward, 0), Relation.Point(forward.Inverse(), 0),
            footing, Words(positive), Words(inverse));
    }
    internal static Carrier Forward(Relation pair) => ((Relation)Parts(pair)[0]).LeftLocation.Carrier;
    internal static Carrier Opposed(Relation pair) => ((Relation)Parts(pair)[1]).LeftLocation.Carrier;

    internal static Relation Observe(IElement selected, Relation pair, Carrier face,
        long numerator, long denominator, string evidence, IElement provenance)
    {
        var forward = Forward(pair);
        if (!ReferenceEquals(face, forward) && !ReferenceEquals(face, Opposed(pair)))
            throw new InvalidOperationException("Observation must use a connected face of its axis.");
        // This fold is a declared contextual alignment. The kernel does not
        // automatically identify inverse-face participation with a negative value.
        var endpoint = ReferenceEquals(face, forward) ? 10000L : -10000L;
        var basis = Relation.Segment(face, 0, denominator);
        var route = Equation.Affine(basis, Relation.Segment(forward, 0, endpoint));
        var raw = new CarrierLocation(face, numerator);
        var central = Project(route, raw, forward);
        var grain = Project(route, new CarrierLocation(face, 1), forward);

        // A declared half-cell envelope at this source resolution. Doubling
        // the basis lets integer locations represent both half-cell boundaries.
        var count = new Carrier();
        var doubledDenominator = Calculate(Equation.Sum(count, Equation.WeightedPort(count, 0, 1, 0, 2)),
            new CarrierLocation(count, denominator), count);
        var lowTwice = Project(Equation.Affine(face, 0, 1, face, -1, 1), raw, face);
        var highTwice = Project(Equation.Affine(face, 0, 1, face, 1, 3), raw, face);
        var halfRoute = Equation.Affine(face, 0, Value(doubledDenominator).Ticks, forward, 0, endpoint);
        var firstEnd = Project(halfRoute, Value(lowTwice), forward);
        var secondEnd = Project(halfRoute, Value(highTwice), forward);
        var entries = new[] { ExtremaTournament.Entry(firstEnd, Value(firstEnd)), ExtremaTournament.Entry(secondEnd, Value(secondEnd)) };
        var minimum = ExtremaTournament.Minimum(new Carrier(), entries, out _);
        var maximum = ExtremaTournament.Maximum(new Carrier(), entries, out _);
        ExtremaTournament.TryReadWinner(minimum, out var minEntry);
        ExtremaTournament.TryReadWinner(maximum, out var maxEntry);
        ExtremaTournament.TryReadEntry(minEntry, out _, out var low);
        ExtremaTournament.TryReadEntry(maxEntry, out _, out var high);
        return ReadWitness.Create(new Carrier(), selected, Group(pair, basis, route), Value(central),
            Group(new Relation(new Relation(low, forward), new Relation(high, forward)), UnitShape.DescriptorSlot(0)),
            Group(new Relation(raw, face), basis, central, grain,
                Group(doubledDenominator, lowTwice, highTwice, firstEnd, secondEnd, minimum, maximum),
                Words(evidence), provenance));
    }
    internal static Relation Bounds(Relation observation)
    {
        ReadWitness.TryRead(observation, out _, out _, out _, out var residue, out _);
        return (Relation)Parts((Relation)residue)[0];
    }
    internal static CarrierLocation Low(Relation observation) => ((Relation)Bounds(observation).Left).LeftLocation;
    internal static CarrierLocation High(Relation observation) => ((Relation)Bounds(observation).Right).LeftLocation;
    internal static CarrierLocation NativeValue(Relation observation)
    {
        ReadWitness.TryRead(observation, out _, out _, out _, out _, out var provenance);
        return ((Relation)Parts((Relation)provenance)[0]).LeftLocation;
    }
    internal static long Denominator(Relation observation)
    {
        ReadWitness.TryRead(observation, out _, out _, out _, out _, out var provenance);
        return ((Relation)((Relation)Parts((Relation)provenance)[1]).Right).LeftLocation.Ticks;
    }
    internal static Relation ReadPair(Relation observation)
    {
        ReadWitness.TryRead(observation, out _, out var basis, out _, out _, out _);
        return (Relation)Parts((Relation)basis)[0];
    }
}
