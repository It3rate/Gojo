using System.Numerics;
using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Tests.Core;

public sealed class LocalCalibrationTests
{
    [Theory]
    [InlineData(10, 0, 5, 0, 1, 2, 1)]
    [InlineData(20, 0, 5, 0, 1, 4, 1)]
    [InlineData(10, 8, 9, 0, 1, 2, 1)]
    [InlineData(5, 10, 0, 0, 2, 1, 1)]
    [InlineData(5, 0, 10, 2, 0, 1, 1)]
    [InlineData(-5, 0, 10, 0, 1, -1, 2)]
    [InlineData(5, -10, 10, -3, 5, 3, 1)]
    [InlineData(2, 0, 3, 1, 2, 5, 3)]
    [InlineData(0, 1, 4, 2, -1, 3, 1)]
    [InlineData(long.MaxValue, 0, 2, 0, 2, long.MaxValue, 1)]
    public void ConditionalAffineReadsConformAcrossOffsetsReversalsAndExactFractions(
        long x, long a, long b, long c, long d, long numerator, long denominator)
    {
        var input = new Carrier(); var output = new Carrier();
        var port = G(input, new Placeholder()); var context = new Placeholder();
        var observation = G(port, new CarrierLocation(new Carrier(), 0), new CarrierLocation(input, x),
            new Placeholder(), context);
        var candidate = G(port, context, new Relation(Relation.Segment(input, a, b), Relation.Segment(output, c, d)),
            new CarrierLocation(output, long.MinValue), new Placeholder(), new Placeholder());
        Assert.True(LocalCalibration.TryPredict(G(observation, candidate), out var witness));
        Number(F(witness)[2], output, numerator, denominator);
        Assert.Equal(0UL, input.Value); Assert.Equal(0UL, output.Value);
        Assert.Equal(0, input.Tension); Assert.Equal(0, output.Tension);
    }

    [Fact]
    public void FractionalEndpointsComposeWithAnExplicitFeedbackBasis()
    {
        var input = new Carrier(); var output = new Carrier(); var feedbackBasis = new Carrier();
        var port = G(input); var subject = new Placeholder(); var context = new Placeholder();
        var observation = G(port, new CarrierLocation(new Carrier(), 0), new CarrierLocation(input, 1), subject, context);
        var half = new Relation(new CarrierLocation(output, 1), Relation.Point(output, 2));
        var third = new Relation(new CarrierLocation(output, 1), Relation.Point(output, 3));
        var map = new Relation(Relation.Segment(input, 0, 2), new Relation(third, half));
        var candidate = G(port, context, map, new CarrierLocation(output, 0), new Placeholder(), new Placeholder());
        Assert.True(LocalCalibration.TryPredict(G(observation, candidate), out var prediction));
        Number(F(prediction)[2], output, 5, 12);
        var feedback = G(G(feedbackBasis), new CarrierLocation(new Carrier(), 1),
            new CarrierLocation(feedbackBasis, 3), subject, context);
        var conversion = new Relation(Relation.Segment(feedbackBasis, 0, 6), Relation.Segment(output, 0, 1));
        Assert.True(LocalCalibration.TryCompare(G(prediction, feedback, conversion,
            new Relation(observation, feedback)), out var comparison));
        Number(F(comparison)[2], output, 1, 12);
        Number(F(F(comparison)[4])[0], output, 1, 2);
    }

    [Fact]
    public void DegenerateSourceAndUnrepresentableResultKeepRequestUnresolved()
    {
        var input = new Carrier(); var output = new Carrier(); var port = G(input);
        var context = new Placeholder();
        var observation = G(port, new CarrierLocation(new Carrier(), 0), new CarrierLocation(input, long.MaxValue),
            new Placeholder(), context);
        foreach (var map in new[]
        {
            new Relation(Relation.Segment(input, 0, 0), Relation.Segment(output, 0, 1)),
            new Relation(Relation.Segment(input, 0, 1), Relation.Segment(output, 0, 2))
        })
        {
            var request = G(observation, G(port, context, map, new CarrierLocation(output, 0),
                new Placeholder(), new Placeholder()));
            Assert.False(LocalCalibration.TryPredict(request, out var witness));
            Assert.IsType<Placeholder>(F(witness)[2]);
            Assert.Same(request, F(witness)[3]);
            Assert.Equal(0UL, output.Value);
        }
    }

    private static Relation G(params IElement[] fields) => Relation.OrderedGroup(new Carrier(), fields);
    private static IElement[] F(IElement e)
    {
        Assert.True(Assert.IsType<Relation>(e).TryReadOrderedGroup(out var fields)); return fields;
    }
    private static void Number(IElement e, Carrier basis, long numerator, long denominator)
    {
        Assert.True(ProjectionResidue.TryRead(e, out var actual, out var n, out var d));
        Assert.Same(basis, actual);
        Assert.Equal((BigInteger)numerator * d, (BigInteger)n * denominator);
    }
}
