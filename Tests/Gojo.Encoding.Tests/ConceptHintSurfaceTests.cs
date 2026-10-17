using Gojo.Core;
using Gojo.Encoding;

namespace Gojo.Encoding.Tests;

public sealed class ConceptHintSurfaceTests
{
    [Fact]
    public void TemporalAndSpatialOrderShareAxisWithoutBecomingSameConcept()
    {
        var before = ConceptHintSurface.Create(
            new Carrier(),
            ConceptHintSurface.AxisOrder,
            ConceptHintSurface.SideNegative,
            ConceptHintSurface.ContextTemporal);
        var inFrontOf = ConceptHintSurface.Create(
            new Carrier(),
            ConceptHintSurface.AxisOrder,
            ConceptHintSurface.SideNegative,
            ConceptHintSurface.ContextSpatial);

        Assert.True(ConceptHintSurface.SharesAxis(before, inFrontOf));
        Assert.True(ConceptHintSurface.TryPackQueryToken(before, out var beforeToken));
        Assert.True(ConceptHintSurface.TryPackQueryToken(inFrontOf, out var inFrontOfToken));
        Assert.NotEqual(beforeToken, inFrontOfToken);
    }

    [Fact]
    public void PriorSideIsStoredAsNegativeCarrierPosition()
    {
        var before = ConceptHintSurface.Create(
            new Carrier(),
            ConceptHintSurface.AxisOrder,
            ConceptHintSurface.SideNegative,
            ConceptHintSurface.ContextTemporal);

        Assert.True(ConceptHintSurface.TryRead(before, out var read));
        Assert.Equal(ConceptHintSurface.AxisOrder, read.Axis.Ticks);
        Assert.Equal(ConceptHintSurface.SideNegative, read.Side.Ticks);
        Assert.True(read.Side.Ticks < 0);
    }

    [Fact]
    public void OpposedSideNegatesOnlyTheSidePosition()
    {
        var before = ConceptHintSurface.Create(
            new Carrier(),
            ConceptHintSurface.AxisOrder,
            ConceptHintSurface.SideNegative,
            ConceptHintSurface.ContextTemporal);

        var after = ConceptHintSurface.OpposedSide(before);

        Assert.True(ConceptHintSurface.TryRead(before, out var beforeRead));
        Assert.True(ConceptHintSurface.TryRead(after, out var afterRead));
        Assert.Equal(beforeRead.Axis.Ticks, afterRead.Axis.Ticks);
        Assert.Equal(beforeRead.Context.Ticks, afterRead.Context.Ticks);
        Assert.Equal(-beforeRead.Side.Ticks, afterRead.Side.Ticks);
    }

    [Fact]
    public void HintBasisIsAPartitionedCarrierSpan()
    {
        var axisBasis = ConceptHintSurface.AxisBasis();
        var locations = axisBasis.CarrierLocations();

        Assert.Equal(2, locations.Length);
        Assert.True(locations[0].Carrier.SameIdentityAs(locations[1].Carrier));
        Assert.Equal(0, locations[0].Ticks);
        Assert.True(locations[1].Ticks > locations[0].Ticks);
    }

    [Fact]
    public void SharpnessHintCanOpenIntoPointedGojoStructure()
    {
        var pointedStructure = PointedStructure();
        var sharp = ConceptHintSurface.Create(
            new Carrier(),
            ConceptHintSurface.AxisEdge,
            ConceptHintSurface.SideReference,
            ConceptHintSurface.ContextPerceptual,
            ConceptHintSurface.StrengthStrong,
            pointedStructure);

        Assert.True(ConceptHintSurface.TryRead(sharp, out var read));
        Assert.Equal(ConceptHintSurface.AxisEdge, read.Axis.Ticks);
        Assert.Equal(ConceptHintSurface.StrengthStrong, read.Strength.Ticks);
        Assert.Same(pointedStructure, read.OpenedStructure);
    }

    [Fact]
    public void SignatureKeepsSeveralTinyHintsAsStructure()
    {
        var cutInLine = ConceptHintSurface.Signature(
            new Carrier(),
            ConceptHintSurface.Create(
                new Carrier(),
                ConceptHintSurface.AxisOrder,
                ConceptHintSurface.SideNegative,
                ConceptHintSurface.ContextProcedural),
            ConceptHintSurface.Create(
                new Carrier(),
                ConceptHintSurface.AxisBoundary,
                ConceptHintSurface.SidePositive,
                ConceptHintSurface.ContextSocial));

        Assert.True(ConceptHintSurface.TryReadSignature(cutInLine, out var hints));
        Assert.Equal(2, hints.Length);
        Assert.All(hints, hint => Assert.True(ConceptHintSurface.TryPackQueryToken(hint, out _)));
    }

    private static Relation PointedStructure()
    {
        var vertex = new Carrier();
        var leftRay = new Carrier();
        var rightRay = new Carrier();
        var vertexPoint = Relation.Point(vertex, 0);

        return Relation.OrderedGroup(
            new Carrier(),
            vertexPoint,
            new Relation(vertexPoint, Relation.Point(leftRay, 1)),
            new Relation(vertexPoint, Relation.Point(rightRay, -1)));
    }
}
