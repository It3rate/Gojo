using Gojo.Core;
using Gojo.Structures;
using GojoCAD.Structures;

namespace GojoCAD.Tests;

public sealed class GraphicConceptShapeTests
{
    [Fact]
    public void SectionConceptIsConnectedUnparameterizedStructure()
    {
        var section = GraphicConceptShape.Section(new Carrier());

        var elements = ReadFrame(section);

        Assert.Equal(4, elements.Length);
        Assert.All(elements, element => Assert.Contains(element, Walk(section)));
        Assert.Contains(Walk(section), element => element is Placeholder);
    }

    [Fact]
    public void CurveFamiliesElaborateTheSameOpenSectionConcept()
    {
        var families = new[]
        {
            GraphicConceptShape.StraightSection(new Carrier()),
            GraphicConceptShape.CircularArcSection(new Carrier()),
            GraphicConceptShape.QuadraticBezierSection(new Carrier()),
            GraphicConceptShape.CubicBezierSection(new Carrier()),
            GraphicConceptShape.ClothoidSection(new Carrier()),
            GraphicConceptShape.InvoluteSection(new Carrier()),
            GraphicConceptShape.SplineSection(new Carrier())
        };

        Assert.All(families, family =>
        {
            var elements = ReadFrame(family);
            Assert.Equal(3, elements.Length);
            Assert.IsType<Relation>(elements[0]);
            Assert.IsType<Relation>(elements[1]);
            Assert.IsType<Relation>(elements[2]);
        });
    }

    [Fact]
    public void HigherRungsKeepLowerSectionPhysicallyPresent()
    {
        var lower = GraphicConceptShape.Section(new Carrier());
        var directed = GraphicConceptShape.WithDirection(new Carrier(), lower);
        var tangent = GraphicConceptShape.WithEndpointTangents(new Carrier(), directed);
        var orthogonal = GraphicConceptShape.WithOrthogonal(new Carrier(), tangent);

        Assert.Contains(Walk(directed), element => ReferenceEquals(element, lower));
        Assert.Contains(Walk(tangent), element => ReferenceEquals(element, lower));
        Assert.Contains(Walk(orthogonal), element => ReferenceEquals(element, directed));
        Assert.Contains(Walk(orthogonal), element => ReferenceEquals(element, tangent));
    }

    [Fact]
    public void SectionsCanJoinBranchAndMergeWithoutConcreteGeometry()
    {
        var line = GraphicConceptShape.StraightSection(new Carrier());
        var clothoid = GraphicConceptShape.ClothoidSection(new Carrier());
        var involute = GraphicConceptShape.InvoluteSection(new Carrier());
        var join = GraphicConceptShape.TangentJoin(new Carrier(), line, clothoid);
        var branch = GraphicConceptShape.Branch(new Carrier(), join, involute, GraphicConceptShape.CircularArcSection(new Carrier()));
        var merge = GraphicConceptShape.Merge(new Carrier(), [branch, GraphicConceptShape.BezierSection(new Carrier())], GraphicConceptShape.Section(new Carrier()));

        Assert.Contains(Walk(join), element => ReferenceEquals(element, line));
        Assert.Contains(Walk(join), element => ReferenceEquals(element, clothoid));
        Assert.Contains(Walk(branch), element => ReferenceEquals(element, join));
        Assert.Contains(Walk(branch), element => ReferenceEquals(element, involute));
        Assert.Contains(Walk(merge), element => ReferenceEquals(element, branch));
    }

    [Fact]
    public void JoiningTwoSectionsCreatesCurrentTwoPortComposite()
    {
        var firstStart = GraphicConceptShape.Port(new Carrier());
        var firstEnd = GraphicConceptShape.Port(new Carrier());
        var secondStart = GraphicConceptShape.Port(new Carrier());
        var secondEnd = GraphicConceptShape.Port(new Carrier());
        var first = GraphicConceptShape.Section(new Carrier(), firstStart, GraphicConceptShape.TensionSlot(), firstEnd);
        var second = GraphicConceptShape.Section(new Carrier(), secondStart, GraphicConceptShape.TensionSlot(), secondEnd);

        var composite = GraphicConceptShape.Join(new Carrier(), first, second);

        Assert.True(GraphicConceptShape.TryReadCompositeShape(
            composite,
            out var exposedPorts,
            out var visibleSurface,
            out var lowerStructure,
            out var internalJoins,
            out _,
            out _));
        Assert.Equal(2, exposedPorts.Length);
        Assert.Same(firstStart, exposedPorts[0]);
        Assert.Same(secondEnd, exposedPorts[1]);
        Assert.Single(internalJoins);
        Assert.Contains(Walk(lowerStructure), element => ReferenceEquals(element, first));
        Assert.Contains(Walk(lowerStructure), element => ReferenceEquals(element, second));
        Assert.True(GraphicConceptShape.TryReadSectionConcept(
            Assert.IsType<Relation>(visibleSurface),
            out var readStart,
            out _,
            out var readEnd,
            out _));
        Assert.Same(firstStart, readStart);
        Assert.Same(secondEnd, readEnd);
    }

    [Fact]
    public void AttachingAtCompositeInteriorJoinCreatesNewJunctionRung()
    {
        var first = GraphicConceptShape.Section(new Carrier());
        var second = GraphicConceptShape.Section(new Carrier());
        var third = GraphicConceptShape.Section(new Carrier());
        var firstComposite = GraphicConceptShape.Join(new Carrier(), first, second);
        Assert.True(GraphicConceptShape.TryReadCompositeShape(
            firstComposite,
            out var firstExposed,
            out _,
            out _,
            out var firstJoins,
            out _,
            out _));

        var junction = GraphicConceptShape.AttachSectionAtJoin(new Carrier(), firstComposite, firstJoins[0], third);

        Assert.True(GraphicConceptShape.TryReadCompositeShape(
            junction,
            out var exposedPorts,
            out _,
            out var lowerStructure,
            out var internalJoins,
            out var previousRung,
            out _));
        Assert.Equal(2, firstExposed.Length);
        Assert.Equal(3, exposedPorts.Length);
        Assert.Same(firstExposed[0], exposedPorts[0]);
        Assert.Same(firstExposed[1], exposedPorts[1]);
        Assert.Equal(2, internalJoins.Length);
        Assert.Same(firstComposite, previousRung);
        Assert.Contains(Walk(lowerStructure), element => ReferenceEquals(element, firstComposite));
        Assert.Contains(Walk(lowerStructure), element => ReferenceEquals(element, third));
        Assert.Contains(Walk(junction), element => ReferenceEquals(element, first));
        Assert.Contains(Walk(junction), element => ReferenceEquals(element, second));
        Assert.Contains(Walk(junction), element => ReferenceEquals(element, third));
    }

    [Fact]
    public void BranchAndMergeReadAsMultiPortCompositeShapes()
    {
        var trunk = GraphicConceptShape.Section(new Carrier());
        var left = GraphicConceptShape.Section(new Carrier());
        var right = GraphicConceptShape.Section(new Carrier());

        var branch = GraphicConceptShape.Branch(new Carrier(), trunk, left, right);
        var merge = GraphicConceptShape.Merge(new Carrier(), [left, right], trunk);

        Assert.True(GraphicConceptShape.TryReadCompositeShape(
            branch,
            out var branchPorts,
            out _,
            out _,
            out _,
            out _,
            out _));
        Assert.True(GraphicConceptShape.TryReadCompositeShape(
            merge,
            out var mergePorts,
            out _,
            out _,
            out _,
            out _,
            out _));
        Assert.Equal(3, branchPorts.Length);
        Assert.Equal(3, mergePorts.Length);
    }

    [Fact]
    public void BoundaryAreaAndThicknessAreLadderLikeElaborations()
    {
        var path = GraphicConceptShape.OpenPath(
            new Carrier(),
            GraphicConceptShape.StraightSection(new Carrier()),
            GraphicConceptShape.CircularArcSection(new Carrier()));
        var closed = GraphicConceptShape.ClosedBoundary(new Carrier(), path);
        var area = GraphicConceptShape.Area(new Carrier(), closed);
        var antiArea = GraphicConceptShape.AntiArea(new Carrier(), closed);
        var thickened = GraphicConceptShape.ThickenedSection(new Carrier(), path);

        Assert.Contains(Walk(closed), element => ReferenceEquals(element, path));
        Assert.Contains(Walk(area), element => ReferenceEquals(element, closed));
        Assert.Contains(Walk(antiArea), element => ReferenceEquals(element, closed));
        Assert.Contains(Walk(thickened), element => ReferenceEquals(element, path));
    }

    [Fact]
    public void ElaborationCanBeRecordedAsStructuralLadder()
    {
        var lower = GraphicConceptShape.Section(new Carrier());
        var higher = GraphicConceptShape.WithEndpointTangents(new Carrier(), lower);
        var ladder = GraphicConceptShape.ElaborationLadder(new Carrier(), lower, higher, higher);

        Assert.True(StructuralLadderSurface.TryRead(
            ladder,
            out var baseRung,
            out var rungFrame,
            out _,
            out _,
            out _));
        Assert.True(StructuralLadderSurface.TryReadRung(
            baseRung,
            out var baseSource,
            out _,
            out var baseResult,
            out _,
            out _,
            out _,
            out _,
            out _));
        Assert.True(StructuralLadderSurface.TryReadRungFrame(rungFrame, out var rungs));
        Assert.Single(rungs);
        Assert.Same(lower, baseSource);
        Assert.Same(lower, baseResult);
        Assert.Contains(Walk(ladder), element => ReferenceEquals(element, higher));
    }

    private static IElement[] ReadFrame(Relation frame)
    {
        var holder = Assert.IsType<Carrier>(frame.Right);
        Assert.True(frame.TryReadOrderedElementsOn(holder, out var elements));
        return elements;
    }

    private static IEnumerable<IElement> Walk(IElement root)
    {
        yield return root;

        if (root is not Relation relation)
        {
            yield break;
        }

        foreach (var element in Walk(relation.Left))
        {
            yield return element;
        }

        foreach (var element in Walk(relation.Right))
        {
            yield return element;
        }
    }
}
