using Gojo.Core;
using Gojo.Encoding.Syntax;
using Gojo.Structures;

namespace Gojo.Encoding.Tests;

public sealed class GojoSyntaxQuotientCompositionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AuthoredCompositionRetainsExplicitReductionDomainAndPartialState(bool restore)
    {
        var roots = GojoSyntax.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
            "Fixtures", "Syntax", "quotient-composition.gojo"))).Roots.ToArray();
        if (restore) roots = GojoNumericFormat.Decode(GojoNumericFormat.Encode(roots));
        Assert.True(Assert.IsType<Relation>(roots[0]).TryReadOrderedGroup(out var participants));
        var mile = Assert.IsType<Carrier>(participants[0]);
        var speed = Assert.IsType<Carrier>(participants[2]);
        Assert.False(QuantityQuotient.TryObserve(Assert.IsType<Relation>(roots[1]), out _));
        Assert.True(QuantityQuotient.TryObserveReduced(Assert.IsType<Relation>(roots[1]), out var witness));
        Assert.True(ReadWitness.TryRead(witness, out _, out _, out var result, out var residue, out _));
        Assert.Equal(new CarrierLocation(mile, 3), result);
        Assert.Equal(new CarrierLocation(speed, 1), residue);
        Assert.False(QuantityQuotient.TryObserveReduced(Assert.IsType<Relation>(roots[2]), out _));
        Assert.True(UnitQuotient.TryNormalize(Assert.IsType<Relation>(roots[3]), out var normalized));
        Assert.True(ReadWitness.TryRead(normalized, out _, out _, out var expression, out _, out _));
        Assert.True(UnitQuotient.TryReadNumeratorUnit(Assert.IsType<Relation>(expression), out var unit));
        Assert.Equal(mile.UnitShape, unit);
        Assert.True(UnitQuotient.TryNormalize(Assert.IsType<Relation>(roots[4]), out var partial));
        Assert.True(ReadWitness.TryRead(partial, out _, out _, out var partialResult, out _, out _));
        Assert.False(UnitQuotient.TryReadUnit(Assert.IsType<Relation>(partialResult), out _));
    }
}
