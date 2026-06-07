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
    public void CuspJoinCarriesUnsettledTangentContinuityAsStructure()
    {
        var first = GraphicConceptShape.StraightSection(new Carrier());
        var second = GraphicConceptShape.CircularArcSection(new Carrier());

        var cusp = GraphicConceptShape.CuspJoin(new Carrier(), first, second);

        Assert.True(GraphicConceptShape.TryReadCompositeShape(
            cusp,
            out _,
            out _,
            out _,
            out var internalJoins,
            out _,
            out _));
        var join = Assert.Single(internalJoins);
        Assert.True(GraphicConceptShape.TryReadPortJoin(
            Assert.IsType<Relation>(join),
            out _,
            out _,
            out var joinEvidence,
            out _));
        Assert.True(GraphicConceptShape.TryReadCuspJoinEvidence(
            Assert.IsType<Relation>(joinEvidence),
            out var incomingTangent,
            out var outgoingTangent,
            out var orthogonal,
            out var continuityExpectation,
            out var residue));
        Assert.Contains(Walk(joinEvidence), element => ReferenceEquals(element, incomingTangent));
        Assert.Contains(Walk(joinEvidence), element => ReferenceEquals(element, outgoingTangent));
        Assert.Contains(Walk(joinEvidence), element => ReferenceEquals(element, orthogonal));
        Assert.Contains(Walk(joinEvidence), element => ReferenceEquals(element, continuityExpectation));
        Assert.Contains(Walk(joinEvidence), element => ReferenceEquals(element, residue));
    }

    [Fact]
    public void CuspJoinEvidenceIsDistinctFromTangentJoinEvidence()
    {
        var first = GraphicConceptShape.StraightSection(new Carrier());
        var second = GraphicConceptShape.CircularArcSection(new Carrier());

        var tangent = GraphicConceptShape.TangentJoin(new Carrier(), first, second);

        Assert.True(GraphicConceptShape.TryReadCompositeShape(
            tangent,
            out _,
            out _,
            out _,
            out var internalJoins,
            out _,
            out _));
        var join = Assert.Single(internalJoins);
        Assert.True(GraphicConceptShape.TryReadPortJoin(
            Assert.IsType<Relation>(join),
            out _,
            out _,
            out var joinEvidence,
            out _));
        Assert.False(GraphicConceptShape.TryReadCuspJoinEvidence(
            Assert.IsType<Relation>(joinEvidence),
            out _,
            out _,
            out _,
            out _,
            out _));
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
    public void DiscoveryCreatesCompositeSectionFromRawSharedPortChain()
    {
        var start = GraphicConceptShape.Port(new Carrier());
        var middle = GraphicConceptShape.Port(new Carrier());
        var end = GraphicConceptShape.Port(new Carrier());
        var first = GraphicConceptShape.Section(new Carrier(), start, GraphicConceptShape.TensionSlot(), middle);
        var second = GraphicConceptShape.Section(new Carrier(), middle, GraphicConceptShape.TensionSlot(), end);
        var completedShape = GraphicConceptShape.Frame(new Carrier(), first, second);

        Assert.True(GraphicConceptShape.TryDiscoverCompositeShape(completedShape, out var composite));

        Assert.True(GraphicConceptShape.TryReadCompositeShape(
            composite,
            out var exposedPorts,
            out var visibleSurface,
            out var lowerStructure,
            out var internalJoins,
            out var previousRung,
            out _));
        Assert.Equal(2, exposedPorts.Length);
        Assert.Same(start, exposedPorts[0]);
        Assert.Same(end, exposedPorts[1]);
        Assert.Single(internalJoins);
        Assert.Contains(Walk(lowerStructure), element => ReferenceEquals(element, completedShape));
        Assert.Contains(Walk(previousRung), element => element is Placeholder);
        Assert.True(GraphicConceptShape.TryReadSectionConcept(
            Assert.IsType<Relation>(visibleSurface),
            out var readStart,
            out _,
            out var readEnd,
            out _));
        Assert.Same(start, readStart);
        Assert.Same(end, readEnd);
    }

    [Fact]
    public void DiscoveryCreatesJunctionShapeFromRawThreeWaySharedPort()
    {
        var center = GraphicConceptShape.Port(new Carrier());
        var top = GraphicConceptShape.Port(new Carrier());
        var left = GraphicConceptShape.Port(new Carrier());
        var right = GraphicConceptShape.Port(new Carrier());
        var vertical = GraphicConceptShape.Section(new Carrier(), top, GraphicConceptShape.TensionSlot(), center);
        var leftArm = GraphicConceptShape.Section(new Carrier(), center, GraphicConceptShape.TensionSlot(), left);
        var rightArm = GraphicConceptShape.Section(new Carrier(), center, GraphicConceptShape.TensionSlot(), right);
        var completedShape = GraphicConceptShape.Frame(new Carrier(), vertical, leftArm, rightArm);

        Assert.True(GraphicConceptShape.TryDiscoverCompositeShape(completedShape, out var junction));

        Assert.True(GraphicConceptShape.TryReadCompositeShape(
            junction,
            out var exposedPorts,
            out _,
            out var lowerStructure,
            out var internalJoins,
            out _,
            out _));
        Assert.Equal(3, exposedPorts.Length);
        Assert.Contains(top, exposedPorts);
        Assert.Contains(left, exposedPorts);
        Assert.Contains(right, exposedPorts);
        Assert.Single(internalJoins);
        Assert.DoesNotContain(center, exposedPorts);
        Assert.Contains(Walk(lowerStructure), element => ReferenceEquals(element, vertical));
        Assert.Contains(Walk(lowerStructure), element => ReferenceEquals(element, leftArm));
        Assert.Contains(Walk(lowerStructure), element => ReferenceEquals(element, rightArm));
    }

    [Fact]
    public void DiscoveryUsesExplicitPortJoinWhenPortsAreNotSharedByReference()
    {
        var start = GraphicConceptShape.Port(new Carrier());
        var firstEnd = GraphicConceptShape.Port(new Carrier());
        var secondStart = GraphicConceptShape.Port(new Carrier());
        var end = GraphicConceptShape.Port(new Carrier());
        var first = GraphicConceptShape.Section(new Carrier(), start, GraphicConceptShape.TensionSlot(), firstEnd);
        var second = GraphicConceptShape.Section(new Carrier(), secondStart, GraphicConceptShape.TensionSlot(), end);
        var join = GraphicConceptShape.PortJoin(new Carrier(), firstEnd, secondStart);
        var completedShape = GraphicConceptShape.Frame(new Carrier(), first, second, join);

        Assert.True(GraphicConceptShape.TryDiscoverCompositeShape(completedShape, out var composite));

        Assert.True(GraphicConceptShape.TryReadCompositeShape(
            composite,
            out var exposedPorts,
            out _,
            out _,
            out var internalJoins,
            out _,
            out _));
        Assert.Equal(2, exposedPorts.Length);
        Assert.Same(start, exposedPorts[0]);
        Assert.Same(end, exposedPorts[1]);
        Assert.Single(internalJoins);
        Assert.Same(join, internalJoins[0]);
    }

    [Fact]
    public void CompositeSectionCanBeResurfacedAsHigherCurve()
    {
        var start = GraphicConceptShape.Port(new Carrier());
        var middle = GraphicConceptShape.Port(new Carrier());
        var end = GraphicConceptShape.Port(new Carrier());
        var firstBase = GraphicConceptShape.Section(new Carrier(), start, GraphicConceptShape.TensionSlot(), middle);
        var secondBase = GraphicConceptShape.Section(new Carrier(), middle, GraphicConceptShape.TensionSlot(), end);
        var first = GraphicConceptShape.Elaborate(new Carrier(), firstBase, GraphicConceptShape.StraightEvidenceSurface(new Carrier()));
        var second = GraphicConceptShape.Elaborate(new Carrier(), secondBase, GraphicConceptShape.StraightEvidenceSurface(new Carrier()));
        var joined = GraphicConceptShape.Join(new Carrier(), first, second);
        var curveEvidence = GraphicConceptShape.CurveEvidenceSurface(new Carrier());

        var refined = GraphicConceptShape.RefineCompositeSection(new Carrier(), joined, curveEvidence);

        Assert.True(GraphicConceptShape.TryReadCompositeShape(
            refined,
            out var exposedPorts,
            out var visibleSurface,
            out var lowerStructure,
            out var internalJoins,
            out var previousRung,
            out _));
        Assert.Equal(2, exposedPorts.Length);
        Assert.Same(start, exposedPorts[0]);
        Assert.Same(end, exposedPorts[1]);
        Assert.Single(internalJoins);
        Assert.Same(joined, previousRung);
        Assert.Contains(Walk(lowerStructure), element => ReferenceEquals(element, first));
        Assert.Contains(Walk(lowerStructure), element => ReferenceEquals(element, second));
        Assert.Contains(Walk(visibleSurface), element => ReferenceEquals(element, curveEvidence));
        Assert.True(GraphicConceptShape.TryReadSectionConcept(
            Assert.IsType<Relation>(visibleSurface),
            out var readStart,
            out _,
            out var readEnd,
            out _));
        Assert.Same(start, readStart);
        Assert.Same(end, readEnd);
    }

    [Fact]
    public void DiscoveredCompositeCanBeResurfacedAsHigherCurveWithoutProvenance()
    {
        var start = GraphicConceptShape.Port(new Carrier());
        var middle = GraphicConceptShape.Port(new Carrier());
        var end = GraphicConceptShape.Port(new Carrier());
        var first = GraphicConceptShape.Section(new Carrier(), start, GraphicConceptShape.TensionSlot(), middle);
        var second = GraphicConceptShape.Section(new Carrier(), middle, GraphicConceptShape.TensionSlot(), end);
        var completedShape = GraphicConceptShape.Frame(new Carrier(), first, second);
        Assert.True(GraphicConceptShape.TryDiscoverCompositeShape(completedShape, out var discovered));
        var curveEvidence = GraphicConceptShape.BezierEvidenceSurface(new Carrier());

        var refined = GraphicConceptShape.RefineCompositeSection(new Carrier(), discovered, curveEvidence);

        Assert.True(GraphicConceptShape.TryReadCompositeShape(
            refined,
            out var exposedPorts,
            out var visibleSurface,
            out var lowerStructure,
            out _,
            out var previousRung,
            out _));
        Assert.Equal(2, exposedPorts.Length);
        Assert.Same(start, exposedPorts[0]);
        Assert.Same(end, exposedPorts[1]);
        Assert.Same(discovered, previousRung);
        Assert.Contains(Walk(lowerStructure), element => ReferenceEquals(element, completedShape));
        Assert.Contains(Walk(visibleSurface), element => ReferenceEquals(element, curveEvidence));
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
    public void OffsetEvidenceCarriesSideDirectionDistanceAndClosure()
    {
        var section = GraphicConceptShape.CubicBezierSection(new Carrier());
        var distance = Relation.Segment(new Carrier(), 0, 12);
        var sidePair = GraphicOffsetSurface.SidePair(new Carrier());
        var direction = GraphicOffsetSurface.Direction(new Carrier());
        var closure = GraphicOffsetSurface.Closure(new Carrier());
        var evidence = GraphicOffsetSurface.Evidence(new Carrier(), distance, sidePair, direction, closure);

        var offset = GraphicConceptShape.OffsetSectionWithEvidence(new Carrier(), section, evidence);

        Assert.Contains(Walk(offset), element => ReferenceEquals(element, section));
        Assert.True(GraphicOffsetSurface.TryReadEvidence(
            evidence,
            out var normalBasis,
            out var readDistance,
            out var readSidePair,
            out var readDirection,
            out var readClosure,
            out var residue));
        Assert.Same(distance, readDistance);
        Assert.Same(sidePair, readSidePair);
        Assert.Same(direction, readDirection);
        Assert.Same(closure, readClosure);
        Assert.Contains(normalBasis, Walk(evidence));
        Assert.Contains(residue, Walk(evidence));
        Assert.True(GraphicOffsetSurface.TryReadSidePair(
            sidePair,
            out var interior,
            out var exterior,
            out _));
        Assert.NotSame(interior, exterior);
        Assert.True(GraphicOffsetSurface.TryReadDirection(
            direction,
            out var flow,
            out _));
        Assert.Contains(flow, Walk(direction));
    }

    [Fact]
    public void OffsetRequestKeepsSourceAndEvidenceTraversable()
    {
        var source = GraphicConceptShape.ClothoidSection(new Carrier());
        var distance = Relation.Segment(new Carrier(), 0, 7);
        var request = GraphicOffsetSurface.RequestWithDistance(new Carrier(), source, distance);

        Assert.True(GraphicOffsetSurface.TryReadRequest(
            request,
            out var readSource,
            out var evidence,
            out var residue));
        Assert.Same(source, readSource);
        Assert.Contains(source, Walk(request));
        Assert.Contains(evidence, Walk(request));
        Assert.Contains(residue, Walk(request));
        Assert.True(GraphicOffsetSurface.TryReadEvidence(
            evidence,
            out _,
            out var readDistance,
            out _,
            out _,
            out _,
            out _));
        Assert.Same(distance, readDistance);
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
