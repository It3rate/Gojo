using Gojo.Core;
using Gojo.Encoding.Syntax;
using Gojo.Patterns;
using Gojo.Structures;

namespace Gojo.Encoding.Tests;

public sealed class GojoSyntaxUnitQuotientTests
{
    [Fact]
    public void AuthoredSpeedAndElapsedTimeCancelOnlyInAnExplicitRead()
    {
        var roots = Parse();
        var mile = Assert.IsType<Carrier>(Elements(Assert.IsType<Relation>(roots[8]))[0]);
        Assert.False(UnitQuotient.TryReadNumeratorUnit(Assert.IsType<Relation>(roots[0]), out _));
        var expression = Assert.IsType<Relation>(roots[1]);
        Assert.True(UnitQuotient.TryRead(expression, out var n, out var d, out var standard));
        Assert.Equal(2, Elements(n).Length);
        Assert.Single(Elements(d));
        var (result, pairs) = Cancel(expression);
        Assert.Single(pairs);
        Assert.True(UnitQuotient.TryReadNumeratorUnit(result, out var unit));
        Assert.Same(mile.UnitShape, unit);
        Assert.True(UnitQuotient.TryRead(result, out _, out _, out var retained));
        Assert.Same(standard, retained);
        Assert.Equal(2, Elements(n).Length);
        Assert.Single(Elements(d));
    }

    [Fact]
    public void CommutativeReadDoesNotRewriteTheTwoAuthoredOrders()
    {
        var roots = Parse();
        var first = Assert.IsType<Relation>(roots[1]);
        var second = Assert.IsType<Relation>(roots[2]);
        Assert.NotEqual(first, second);
        var (a, _) = Cancel(first);
        var (b, _) = Cancel(second);
        Assert.True(UnitQuotient.TryReadNumeratorUnit(a, out var ua));
        Assert.True(UnitQuotient.TryReadNumeratorUnit(b, out var ub));
        Assert.True(ua.Equals(ub));
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void ConsumptionStandardRetainsItsHundredKilometreCalibration()
    {
        var roots = Parse();
        var litre = Assert.IsType<Carrier>(Elements(Assert.IsType<Relation>(roots[8]))[2]);
        var (unconverted, pairs) = Cancel(Assert.IsType<Relation>(roots[3]));
        Assert.Empty(pairs);
        Assert.False(UnitQuotient.TryReadNumeratorUnit(unconverted, out _));
        var (converted, convertedPairs) = Cancel(Assert.IsType<Relation>(roots[4]));
        Assert.Single(convertedPairs);
        Assert.True(UnitQuotient.TryReadNumeratorUnit(converted, out var unit));
        Assert.Same(litre.UnitShape, unit);
        Assert.True(UnitQuotient.TryRead(converted, out _, out _, out var standard));
        var reference = Assert.IsType<Relation>(standard);
        Assert.Equal(100, Assert.IsType<CarrierLocation>(Assert.IsType<Relation>(reference.Right).Left).Ticks);
    }

    [Fact]
    public void ReciprocalAndOpenDenominatorAreValidAuthoredStructure()
    {
        var roots = Parse();
        var (reciprocal, _) = Cancel(Assert.IsType<Relation>(roots[5]));
        Assert.False(UnitQuotient.IsDimensionless(reciprocal));
        var partial = Assert.IsType<Relation>(roots[6]);
        var (open, pairs) = Cancel(partial);
        Assert.Empty(pairs);
        Assert.False(UnitQuotient.TryReadNumeratorUnit(open, out _));
        Assert.False(StructuralPattern.IsComplete(open));
        var bound = Assert.IsType<Relation>(StructuralPattern.Apply(partial, Assert.IsType<Relation>(roots[7])));
        var (complete, completePairs) = Cancel(bound);
        Assert.Single(completePairs);
        Assert.True(UnitQuotient.TryReadNumeratorUnit(complete, out _));
        Assert.False(StructuralPattern.IsComplete(partial));
    }

    private static (Relation Result, IElement[] Pairs) Cancel(Relation expression)
    {
        Assert.True(UnitQuotient.TryCancel(expression, out var witness));
        Assert.True(ReadWitness.TryRead(witness, out _, out var pairs, out var result, out _, out _));
        return (Assert.IsType<Relation>(result), Elements(Assert.IsType<Relation>(pairs)));
    }

    private static IReadOnlyList<IElement> Parse() => GojoSyntax.Parse(File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "Syntax", "unit-quotients.gojo"))).Roots;

    private static IElement[] Elements(Relation group)
    {
        Assert.True(group.TryReadOrderedElementsOn(Assert.IsType<Carrier>(group.Right), out var elements));
        return elements;
    }
}
