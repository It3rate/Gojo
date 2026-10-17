using Gojo.Core;
using Gojo.Encoding.Syntax;
using Gojo.Patterns;

namespace Gojo.Encoding.Tests;

public sealed class GojoSyntaxMathConformanceTests
{
    [Theory]
    [InlineData(3, 4)]
    [InlineData(-3, 4)]
    [InlineData(3, -4)]
    [InlineData(-3, -4)]
    [InlineData(0, 4)]
    public void AuthoredOperationsComputeSignedMathAndRetainSourceResidues(long a, long b)
    {
        var roots = Parse();
        var carriers = Group(roots[0]);
        var x = Assert.IsType<Carrier>(carriers[0]);
        var y = Assert.IsType<Carrier>(carriers[1]);
        var output = Assert.IsType<Carrier>(carriers[2]);
        var input = new Relation(new CarrierLocation(x, a), new CarrierLocation(y, b));
        for (var index = 1; index <= 2; index++)
        {
            Assert.True(Assert.IsType<Relation>(roots[index]).TryReadOperationFrom(input, output, out var read));
            Assert.Equal(index == 1 ? a + b : a * b, Assert.IsType<CarrierLocation>(read.Left).Ticks);
            Assert.Equal(new Relation(new CarrierLocation(x, 0), new CarrierLocation(y, 0)), read.Right);
        }
        Assert.Equal(0UL, output.Value);
        Assert.Equal(0, x.Tension);
    }

    [Fact]
    public void AuthoredRatiosAndScaledResidueAgreeWithExecutableReads()
    {
        var roots = Parse();
        var carriers = Group(roots[0]);
        var x = Assert.IsType<Carrier>(carriers[0]);
        var output = Assert.IsType<Carrier>(carriers[2]);
        var whole = Assert.IsType<Relation>(roots[3]).Project(new CarrierLocation(x, 10), output);
        Assert.Equal(new Relation(new CarrierLocation(output, 6), new CarrierLocation(x, 1)), whole);
        var fractional = Assert.IsType<Relation>(roots[4]).Project(new CarrierLocation(x, 1), output);
        Assert.True(fractional.TryReadProjection(out var result, out var residue));
        Assert.Equal(3, result.Ticks);
        Assert.Equal(roots[5], residue);
        Assert.True(ProjectionResidue.TryRead(residue, out var source, out var n, out var d));
        Assert.Same(x, source);
        Assert.Equal(1, n);
        Assert.Equal(10, d);
    }

    [Fact]
    public void NegativeLocationOpposedFlowAndReversalHaveDistinctReads()
    {
        var roots = Parse();
        var x = Assert.IsType<Carrier>(Group(roots[0])[0]);
        var views = Group(roots[6]);
        Assert.Equal(new CarrierLocation(x, -5), views[0]);
        Assert.Equal(new CarrierLocation(x.Inverse(), 5), views[1]);
        Assert.Equal(Relation.Segment(x, -2, -5), views[2]);
        Assert.Equal(Relation.Segment(x, 5, 2), views[3]);
        Assert.Equal(0, Assert.IsType<CarrierLocation>(views[1]).ReadFrom(x).ProjectedTicks);
        // The opposed-face product cycle is a contextual operation, not negation.
        var output = Assert.IsType<Carrier>(Group(roots[0])[2]);
        var y = Assert.IsType<Carrier>(Group(roots[0])[1]);
        var input = new Relation(new CarrierLocation(x.Inverse(), 2), new CarrierLocation(y.Inverse(), 3));
        Assert.True(Assert.IsType<Relation>(roots[2]).TryReadOperationFrom(input, output, out var read));
        Assert.Equal(new CarrierLocation(output, -6), read.Left);
    }

    [Fact]
    public void ProductDimensionsAndCrystallizedShapeRetainRepeatedUnits()
    {
        var roots = Parse();
        var unit = Assert.IsType<Carrier>(Group(roots[0])[0]).UnitShape;
        var units = Group(roots[7]).Cast<Carrier>().ToArray();
        Assert.Equal(2, units[0].UnitShape.ProductParticipationOf(unit));
        Assert.Equal(3, units[1].UnitShape.ProductParticipationOf(unit));
        Assert.False(units[0].UnitShape.CompatibleWith(units[1].UnitShape));
        Assert.True(units[0].UnitShape.CompatibleWith(units[2].UnitShape));
        Assert.All(units, carrier => Assert.Equal(0UL, carrier.Value));
    }

    [Fact]
    public void ResolutionRungsPreserveEnoughRemainderToReconstructTheFineValue()
    {
        var roots = Parse();
        var x = Assert.IsType<Carrier>(Group(roots[0])[0]);
        var ladder = Group(roots[8]);
        var tens = Assert.IsType<Carrier>(ladder[0]);
        var hundreds = Assert.IsType<Carrier>(ladder[1]);
        Assert.True(hundreds.UnitShape.TryReadResolution(out var unit, out var ratio));
        Assert.True(unit.CompatibleWith(x.UnitShape));
        Assert.Equal(100, ratio);
        Assert.True(Assert.IsType<Relation>(ladder[2]).Project(new CarrierLocation(x, 237), tens).TryGetCarrierLocations(out var t, out var r1));
        Assert.True(Assert.IsType<Relation>(ladder[3]).Project(t, hundreds).TryGetCarrierLocations(out var h, out var r2));
        Assert.Equal(2, h.Ticks);
        Assert.Equal(237, 100 * h.Ticks + 10 * r2.Ticks + r1.Ticks);
    }

    [Fact]
    public void APartialOperationRefusesACompleteReadUntilItsPortIsBound()
    {
        var roots = Parse();
        var carriers = Group(roots[0]).Cast<Carrier>().ToArray();
        var partial = Group(roots[9]);
        var operation = Assert.IsType<Relation>(partial[0]);
        var input = new Relation(new CarrierLocation(carriers[0], 3), new CarrierLocation(carriers[1], 4));
        Assert.False(operation.TryReadOperationFrom(input, carriers[2], out _));
        Assert.False(StructuralPattern.IsComplete(operation));
        var complete = StructuralPattern.Apply(operation, Assert.IsType<Relation>(partial[1]));
        Assert.True(Assert.IsType<Relation>(complete).TryReadOperationFrom(input, carriers[2], out var read));
        Assert.Equal(7, Assert.IsType<CarrierLocation>(read.Left).Ticks);
        Assert.False(StructuralPattern.IsComplete(operation));
    }

    [Fact]
    public void CarryRecipeIsInactiveUntilAttachedAndThenConservesItsWeightedValue()
    {
        var carry = Group(Parse()[10]);
        var ones = Assert.IsType<Carrier>(carry[0]);
        var whole = Assert.IsType<Carrier>(carry[1]);
        ones.Add(7);
        Assert.Equal(7UL, ones.Value);
        Assert.Equal(0UL, whole.Value);
        ones.Attach(Assert.IsType<Relation>(carry[2]));
        ones.Add(230);
        Assert.Equal(7UL, ones.Value);
        Assert.Equal(23UL, whole.Value);
        Assert.Equal(237UL, ones.Value + 10 * whole.Value);
        Assert.Equal(0, ones.Tension);
    }

    [Fact]
    public void OrthogonalShapesAndIncompatibleAdditiveUnitsDoNotInventScalarMeaning()
    {
        var roots = Parse();
        var shapes = Group(roots[11]).Cast<Carrier>().ToArray();
        Assert.False(shapes[0].UnitShape.CompatibleWith(shapes[1].UnitShape));
        Assert.False(shapes[1].UnitShape.CompatibleWith(shapes[2].UnitShape));
        Assert.All(shapes, carrier => Assert.Equal(0UL, carrier.Value));
        var mixed = Group(roots[12]);
        var x = Assert.IsType<Carrier>(Group(roots[0])[0]);
        var heat = Assert.IsType<Carrier>(mixed[0]);
        var input = new Relation(new CarrierLocation(x, 3), new CarrierLocation(heat, 4));
        Assert.False(Assert.IsType<Relation>(mixed[1]).TryProjectCrystallizedValueFrom(input, Assert.IsType<Carrier>(mixed[2]), out _));
    }

    [Fact]
    public void SharedSiteAcrossThreeComponentsDoesNotMergeCarrierIdentities()
    {
        var components = Group(Parse()[13]).Cast<CarrierLocation>().ToArray();
        Assert.True(components[0].SameTickRefAs(components[1]));
        Assert.False(components[0].Carrier.SameIdentityAs(components[1].Carrier));
        Assert.False(components[0].Equals(components[1]));
        Assert.Equal(-4, components[2].Ticks);
    }

    [Theory]
    [InlineData("resolution(unit_of(carrier()), 0)")]
    [InlineData("resolution(unit_of(carrier()), -2)")]
    [InlineData("resolution(resolution(unit_of(carrier()), 9223372036854775807), 2)")]
    public void InvalidResolutionProducesALocatedSyntaxError(string unit)
    {
        var error = Assert.Throws<GojoSyntaxException>(() => GojoSyntax.Parse($"gojo 1;\nroot carrier({unit});"));
        Assert.Equal(2, error.Line);
        Assert.True(error.Column > 1);
    }

    [Fact]
    public void MinimumSignedLiteralProjectsWithoutAHostOverflow()
    {
        var roots = GojoSyntax.Parse("""
            gojo 1;
            let $a = carrier();
            let $b = carrier();
            root point($a, 1) -> point($b, 1);
            root at($a, -9223372036854775808);
            root $b;
            """).Roots;
        var projection = Assert.IsType<Relation>(roots[0]).Project(Assert.IsType<CarrierLocation>(roots[1]), Assert.IsType<Carrier>(roots[2]));
        Assert.Equal(long.MinValue, Assert.IsType<CarrierLocation>(projection.Left).Ticks);
    }

    private static IReadOnlyList<IElement> Parse() => GojoSyntax.Parse(File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "Syntax", "math-conformance.gojo"))).Roots;

    private static IElement[] Group(IElement element)
    {
        var group = Assert.IsType<Relation>(element);
        Assert.True(group.TryReadOrderedElementsOn(Assert.IsType<Carrier>(group.Right), out var values));
        return values;
    }
}
