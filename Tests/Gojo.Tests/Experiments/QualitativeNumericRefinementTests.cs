using Gojo.Core;
using Gojo.Patterns;
using Gojo.Structures;

namespace Gojo.Tests.Experiments;

public sealed class QualitativeNumericRefinementTests
{
    [Theory]
    [InlineData(12, 10)]
    [InlineData(10, 10)]
    [InlineData(8, 10)]
    public void MeasuredDifferenceNarrowsTheSameRelationalDirectionDomain(long incoming, long outgoing)
    {
        var f = new RefinementWorld();
        var publication = f.Difference(incoming, outgoing);
        var before = StructuralConstraint.Match(f.DirectionSlot, f.Variables, f.Directions);
        var evidence = f.Refine(before, publication);
        var after = Assert.IsType<Relation>(evidence.Right);

        Assert.Equal(3, Candidates(before).Length);
        Assert.True(StructuralConstraint.TryReadUnique(after, out var direction, out _));
        // The independent arithmetic oracle is an assertion, not the inference.
        var expected = incoming > outgoing ? f.Forward : incoming < outgoing ? f.Reverse : f.Stable;
        Assert.Same(expected, direction);
        Assert.Contains(Entries(f.Directions), entry => ReferenceEquals(entry, direction));
        Assert.True(ReadWitness.TryRead(publication, out _, out _, out _, out _, out var arithmetic));
        Assert.IsType<Relation>(arithmetic);
        Assert.Equal(3, Candidates(before).Length);
    }

    [Fact]
    public void CoarseAmbiguityKeepsAlternativesUntilAnExplicitFinerPublicationArrives()
    {
        var f = new RefinementWorld();
        var before = StructuralConstraint.Match(f.DirectionSlot, f.Variables, f.Directions);
        // A four-tick instrument publishes [8,11], not its private input 11.
        var coarse = f.Difference(11, 10, quantum: 4);
        Assert.True(FiniteReadSurface.TryReadSupport(coarse, out var low, out var high));
        Assert.Equal(-2, low!.Value.Ticks);
        Assert.Equal(1, high!.Value.Ticks);
        var unresolved = Assert.IsType<Relation>(f.Refine(before, coarse).Right);
        Assert.Equal(3, Candidates(unresolved).Length);
        Assert.False(StructuralConstraint.TryReadUnique(unresolved, out _, out _));

        var fine = f.Difference(11, 10);
        var refined = Assert.IsType<Relation>(f.Refine(before, fine).Right);
        Assert.True(StructuralConstraint.TryReadUnique(refined, out var direction, out _));
        Assert.Same(f.Forward, direction);
        Assert.Equal(3, Candidates(unresolved).Length);
        // This bridge deliberately keeps the whole qualitative domain on an
        // inconclusive sign read. It does not promise the tightest subset.
    }

    [Fact]
    public void MeasurementConflictingWithAQualitativePremiseRemainsABlockedIntersection()
    {
        var f = new RefinementWorld();
        var prior = StructuralConstraint.Match(f.DirectionSlot, f.Variables,
            StructuralConstraint.Closed(f.Forward));
        var evidence = f.Refine(prior, f.Difference(8, 10));
        var combined = Assert.IsType<Relation>(evidence.Right);

        Assert.Empty(Candidates(combined));
        Assert.True(StructuralConstraint.TryRead(combined, out _, out _, out var blocked, out var unresolved));
        Assert.NotEmpty(Entries(blocked));
        Assert.Empty(Entries(unresolved));
        Assert.False(StructuralConstraint.ApplyUnique(combined, f.DirectionSlot, out var unchanged));
        Assert.Same(f.DirectionSlot, unchanged);
        Assert.True(StructuralConstraint.TryReadUnique(prior, out var retained, out _));
        Assert.Same(f.Forward, retained);
    }

    private sealed class RefinementWorld
    {
        public RefinementWorld()
        {
            var a = new Placeholder();
            var b = new Placeholder();
            Forward = new Relation(a, b);
            Reverse = new Relation(b, a);
            Stable = new Relation(a, a);
            Directions = StructuralConstraint.Closed(Forward, Reverse, Stable);
            Variables = StructuralConstraint.Closed(DirectionSlot);
            Capacity = FiniteReadSurface.Capacity(new Carrier(), Axis, -100, 100);
            // This explicitly selected calibration connects numeric sign reads
            // to the SAME pre-existing direction relations. Numbers enter here.
            Policy = SupportDecisionSurface.Create(new Carrier(),
                Rule(1, 100, Forward), Rule(-100, -1, Reverse), Rule(0, 0, Stable),
                SupportDecisionSurface.Rule(new Carrier(), Directions));
        }

        public Placeholder DirectionSlot { get; } = new();
        public Placeholder DifferenceRole { get; } = new();
        public Carrier Axis { get; } = new();
        public Relation Forward { get; }
        public Relation Reverse { get; }
        public Relation Stable { get; }
        public Relation Directions { get; }
        public Relation Variables { get; }
        private Relation Capacity { get; }
        private Relation Policy { get; }

        private Relation Rule(long low, long high, Relation direction) =>
            SupportDecisionSurface.Rule(new Carrier(), StructuralConstraint.Closed(direction),
                SupportDecisionSurface.Condition(new Carrier(), DifferenceRole, Relation.Segment(Axis, low, high)));

        public Relation Difference(long incoming, long outgoing, long quantum = 1)
        {
            var instrument = FiniteReadSurface.Capacity(new Carrier(), Axis, -100, 100, quantum);
            var left = FiniteReadSurface.Publish(new Carrier(), instrument, new CarrierLocation(Axis, incoming), new Placeholder());
            var right = FiniteReadSurface.Publish(new Carrier(), Capacity, new CarrierLocation(Axis, outgoing), new Placeholder());
            return FiniteReadSurface.Subtract(new Carrier(), Capacity, left, right);
        }

        public Relation Refine(Relation prior, Relation difference)
        {
            var selection = SupportDecisionSurface.Select(new Carrier(), Policy,
                SupportDecisionSurface.Binding(new Carrier(), DifferenceRole, difference));
            Assert.True(SupportDecisionSurface.TryReadSelection(selection, out _, out var response, out _, out _));
            var support = Assert.IsType<Relation>(response);
            // The result is connected to both measurement/calibration evidence
            // and its structural refinement; neither becomes a hidden side table.
            var refinement = StructuralConstraint.Join(prior, DirectionSlot, Variables, support);
            return new Relation(selection, refinement);
        }
    }

    private static Relation[] Candidates(Relation read)
    {
        Assert.True(StructuralConstraint.TryRead(read, out _, out var candidates, out _, out _));
        return Entries(candidates).Cast<Relation>().ToArray();
    }

    private static IElement[] Entries(Relation scope)
    {
        Assert.True(StructuralConstraint.TryReadScope(scope, out var entries, out _, out _));
        return entries;
    }
}
