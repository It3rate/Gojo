using Gojo.Core;
using Gojo.Encoding;

namespace Gojo.Encoding.Tests;

public sealed class ConceptSeedSurfaceTests
{
    [Fact]
    public void SeedCarriesTextAndCompactHintSignature()
    {
        var before = ConceptSeedSurface.Create(
            new Carrier(),
            "before",
            [
                ConceptHintSurface.Create(
                    new Carrier(),
                    ConceptHintSurface.AxisOrder,
                    ConceptHintSurface.SideNegative,
                    ConceptHintSurface.ContextTemporal)
            ]);

        Assert.True(ConceptSeedSurface.TryRead(before, out var read));
        Assert.Equal("before", read.Name);
        Assert.Single(read.Hints);
        Assert.True(ConceptHintSurface.TryRead(read.Hints[0], out var hintRead));
        Assert.Equal(ConceptHintSurface.AxisOrder, hintRead.Axis.Ticks);
        Assert.Equal(ConceptHintSurface.SideNegative, hintRead.Side.Ticks);
        Assert.Equal(ConceptHintSurface.ContextTemporal, hintRead.Context.Ticks);
    }

    [Fact]
    public void SeedQuerySignatureIsDerivedFromHints()
    {
        var before = ConceptSeedSurface.Create(
            new Carrier(),
            "before",
            [
                ConceptHintSurface.Create(
                    new Carrier(),
                    ConceptHintSurface.AxisOrder,
                    ConceptHintSurface.SideNegative,
                    ConceptHintSurface.ContextTemporal),
                ConceptHintSurface.Create(
                    new Carrier(),
                    ConceptHintSurface.AxisSourceTarget,
                    ConceptHintSurface.SideNegative,
                    ConceptHintSurface.ContextTemporal)
            ]);

        Assert.True(ConceptSeedSurface.TryPackQuerySignature(before, out var tokens));
        Assert.Equal(2, tokens.Length);
        Assert.NotEqual(tokens[0], tokens[1]);
    }

    [Fact]
    public void DifferentSeedsCanSharePrimitiveHintAxisWithoutSharingLocalSense()
    {
        var before = ConceptSeedSurface.Create(
            new Carrier(),
            "before",
            [
                ConceptHintSurface.Create(
                    new Carrier(),
                    ConceptHintSurface.AxisOrder,
                    ConceptHintSurface.SideNegative,
                    ConceptHintSurface.ContextTemporal)
            ]);
        var inFrontOf = ConceptSeedSurface.Create(
            new Carrier(),
            "in-front-of",
            [
                ConceptHintSurface.Create(
                    new Carrier(),
                    ConceptHintSurface.AxisOrder,
                    ConceptHintSurface.SideNegative,
                    ConceptHintSurface.ContextSpatial)
            ]);

        Assert.True(ConceptSeedSurface.SharesAnyHintAxis(before, inFrontOf));
        Assert.True(ConceptSeedSurface.TryRead(before, out var beforeRead));
        Assert.True(ConceptSeedSurface.TryRead(inFrontOf, out var inFrontOfRead));
        Assert.NotEqual(beforeRead.Name, inFrontOfRead.Name);
    }

    [Fact]
    public void SeedCanOpenIntoGojoStructureThatRhymeWithHint()
    {
        var pointedStructure = PointedStructure();
        var sharp = ConceptSeedSurface.Create(
            new Carrier(),
            "sharp",
            [
                ConceptHintSurface.Create(
                    new Carrier(),
                    ConceptHintSurface.AxisEdge,
                    ConceptHintSurface.SideReference,
                    ConceptHintSurface.ContextPerceptual,
                    ConceptHintSurface.StrengthStrong,
                    pointedStructure)
            ],
            pointedStructure);

        Assert.True(ConceptSeedSurface.TryRead(sharp, out var read));
        Assert.Equal("sharp", read.Name);
        Assert.Same(pointedStructure, read.OpenedStructure);
        Assert.True(ConceptHintSurface.TryRead(read.Hints[0], out var hintRead));
        Assert.Same(pointedStructure, hintRead.OpenedStructure);
    }

    [Fact]
    public void SeedResidueRemainsVisibleForUnsettledConcept()
    {
        var residue = ConceptAddressSurface.Atom(new Carrier(), "weak-metaphor-residue");
        var cuttingRemark = ConceptSeedSurface.Create(
            new Carrier(),
            "cutting-remark",
            [
                ConceptHintSurface.Create(
                    new Carrier(),
                    ConceptHintSurface.AxisEdge,
                    ConceptHintSurface.SidePositive,
                    ConceptHintSurface.ContextSocial,
                    ConceptHintSurface.StrengthWeak)
            ],
            residueOrTension: residue);

        Assert.True(ConceptSeedSurface.TryRead(cuttingRemark, out var read));
        Assert.Same(residue, read.ResidueOrTension);
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
