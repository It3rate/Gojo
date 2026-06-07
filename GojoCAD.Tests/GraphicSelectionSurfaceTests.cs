using Gojo.Core;
using Gojo.Structures;
using GojoCAD.Structures;

namespace GojoCAD.Tests;

public sealed class GraphicSelectionSurfaceTests
{
    [Fact]
    public void SelectionCarriesSourceDomainConceptMatchesAndResidue()
    {
        var source = GraphicConceptShape.CuspJoin(
            new Carrier(),
            GraphicConceptShape.StraightSection(new Carrier()),
            GraphicConceptShape.CircularArcSection(new Carrier()));
        var candidateEvidence = ReadJoinEvidence(source);
        var searchDomain = GraphicSelectionSurface.Frame(new Carrier(), candidateEvidence);
        var conceptPattern = GraphicConceptShape.CuspJoinEvidenceSurface(new Carrier());
        var bindings = FocusSurface.BindingFrame(
            new Carrier(),
            FocusSurface.Bind(conceptPattern, candidateEvidence));
        var match = GraphicSelectionSurface.Match(
            new Carrier(),
            candidateEvidence,
            conceptPattern,
            bindings,
            candidateEvidence,
            GraphicConceptShape.TensionSlot());
        var matchFrame = GraphicSelectionSurface.MatchFrame(new Carrier(), match);
        var residue = GraphicConceptShape.TensionSlot();

        var selection = GraphicSelectionSurface.Select(
            new Carrier(),
            source,
            searchDomain,
            conceptPattern,
            matchFrame,
            residue);

        Assert.True(GraphicSelectionSurface.TryReadSelection(
            selection,
            out var readSource,
            out var readDomain,
            out var readPattern,
            out var readMatches,
            out var readResidue));
        Assert.Same(source, readSource);
        Assert.Same(searchDomain, readDomain);
        Assert.Same(conceptPattern, readPattern);
        Assert.Same(matchFrame, readMatches);
        Assert.Same(residue, readResidue);
        Assert.Contains(source, Walk(selection));
        Assert.Contains(candidateEvidence, Walk(selection));

        Assert.True(GraphicSelectionSurface.TryReadMatchFrame(
            Assert.IsType<Relation>(readMatches),
            out var matches));
        var readMatch = Assert.Single(matches);
        Assert.Same(match, readMatch);
    }

    [Fact]
    public void PatternMatchUsesRelationBindingsAndKeepsUnboundConceptResidue()
    {
        var conceptPattern = GraphicConceptShape.CuspJoinEvidenceSurface(new Carrier());
        Assert.True(GraphicConceptShape.TryReadCuspJoinEvidence(
            conceptPattern,
            out var patternIncoming,
            out var patternOutgoing,
            out var patternOrthogonal,
            out _,
            out _));
        var source = GraphicConceptShape.CuspJoin(
            new Carrier(),
            GraphicConceptShape.StraightSection(new Carrier()),
            GraphicConceptShape.CircularArcSection(new Carrier()));
        var candidateEvidence = ReadJoinEvidence(source);
        Assert.True(GraphicConceptShape.TryReadCuspJoinEvidence(
            candidateEvidence,
            out var candidateIncoming,
            out var candidateOutgoing,
            out var candidateOrthogonal,
            out _,
            out _));
        var bindings = FocusSurface.BindingFrame(
            new Carrier(),
            FocusSurface.Bind(patternIncoming, candidateIncoming),
            FocusSurface.Bind(patternOutgoing, candidateOutgoing),
            FocusSurface.Bind(patternOrthogonal, candidateOrthogonal));

        Assert.True(GraphicSelectionSurface.TryCreatePatternMatch(
            new Carrier(),
            candidateEvidence,
            conceptPattern,
            bindings,
            out var match));

        Assert.True(GraphicSelectionSurface.TryReadMatch(
            match,
            out var readCandidate,
            out var readPattern,
            out var readBindings,
            out var readEvidence,
            out var readResidue));
        Assert.Same(candidateEvidence, readCandidate);
        Assert.Same(conceptPattern, readPattern);
        Assert.Same(bindings, readBindings);
        Assert.True(FocusSurface.TryReadBindingFrame(readBindings, out var bindingClauses));
        Assert.Equal(3, bindingClauses.Length);

        Assert.True(GraphicSelectionSurface.TryReadPatternEvidence(
            Assert.IsType<Relation>(readEvidence),
            out var evidencePattern,
            out var appliedPattern,
            out var evidenceCandidate,
            out var evidenceResidue));
        Assert.Same(conceptPattern, evidencePattern);
        Assert.Same(candidateEvidence, evidenceCandidate);
        Assert.Same(readResidue, evidenceResidue);
        Assert.Contains(candidateIncoming, Walk(appliedPattern));
        Assert.Contains(candidateOutgoing, Walk(appliedPattern));
        Assert.Contains(candidateOrthogonal, Walk(appliedPattern));
        Assert.True(FocusSurface.TryReadFrame(readResidue, out var unresolvedSlots));
        Assert.NotEmpty(unresolvedSlots);
    }

    [Fact]
    public void SelectionMatchCanFeedGenericTransformApplication()
    {
        var source = GraphicConceptShape.CuspJoin(
            new Carrier(),
            GraphicConceptShape.StraightSection(new Carrier()),
            GraphicConceptShape.CircularArcSection(new Carrier()));
        var candidateEvidence = ReadJoinEvidence(source);
        var conceptPattern = GraphicConceptShape.CuspJoinEvidenceSurface(new Carrier());
        var bindings = FocusSurface.BindingFrame(
            new Carrier(),
            FocusSurface.Bind(conceptPattern, candidateEvidence));
        var match = GraphicSelectionSurface.Match(
            new Carrier(),
            candidateEvidence,
            conceptPattern,
            bindings,
            candidateEvidence,
            GraphicConceptShape.TensionSlot());
        var matches = GraphicSelectionSurface.MatchFrame(new Carrier(), match);
        var searchDomain = GraphicSelectionSurface.Frame(new Carrier(), candidateEvidence);
        var selection = GraphicSelectionSurface.Select(
            new Carrier(),
            source,
            searchDomain,
            conceptPattern,
            matches,
            GraphicConceptShape.TensionSlot());
        var roundedEvidence = GraphicConceptShape.CircularArcEvidenceSurface(new Carrier());
        var result = GraphicConceptShape.RefineCompositeSection(new Carrier(), source, roundedEvidence);
        var focus = FocusSurface.Create(
            new Carrier(),
            source,
            conceptPattern,
            matches,
            GraphicConceptShape.TensionSlot(),
            roundedEvidence,
            bindings,
            GraphicConceptShape.TensionSlot(),
            selection);
        var selectedPort = new Placeholder();
        var roundedPort = new Placeholder();
        var transform = TransformSurface.Create(
            new Carrier(),
            TransformSurface.Ports(new Carrier(), selectedPort),
            TransformSurface.Frame(new Carrier(), conceptPattern, selectedPort, roundedPort),
            TransformSurface.Ports(new Carrier(), roundedPort),
            GraphicConceptShape.TensionSlot(),
            selection);
        var applicationBindings = FocusSurface.BindingFrame(
            new Carrier(),
            FocusSurface.Bind(selectedPort, matches),
            FocusSurface.Bind(roundedPort, roundedEvidence));
        var residue = GraphicConceptShape.TensionSlot();

        Assert.True(TransformSurface.TryCreateApplication(
            new Carrier(),
            transform,
            focus,
            applicationBindings,
            result,
            residue,
            selection,
            out var application));
        Assert.True(TransformSurface.TryReadApplication(
            application,
            out var readTransform,
            out var readFocus,
            out var readBindings,
            out var readResult,
            out var patch,
            out _,
            out var readResidue,
            out var readProvenance));
        Assert.Same(transform, readTransform);
        Assert.Same(focus, readFocus);
        Assert.Same(applicationBindings, readBindings);
        Assert.Same(result, readResult);
        Assert.Same(residue, readResidue);
        Assert.Same(selection, readProvenance);

        Assert.True(TransformSurface.TryReadPatch(
            patch,
            out var patchSource,
            out _,
            out _,
            out var patchParticipation,
            out _,
            out var patchReplacement,
            out _,
            out var patchResult,
            out _,
            out _));
        Assert.Same(source, patchSource);
        Assert.Same(matches, patchParticipation);
        Assert.Same(roundedEvidence, patchReplacement);
        Assert.Same(result, patchResult);
    }

    [Fact]
    public void StructuralSearchEmitsMatchForConceptShapeInLocalDomain()
    {
        var tangentSource = GraphicConceptShape.TangentJoin(
            new Carrier(),
            GraphicConceptShape.StraightSection(new Carrier()),
            GraphicConceptShape.CircularArcSection(new Carrier()));
        var cuspSource = GraphicConceptShape.CuspJoin(
            new Carrier(),
            GraphicConceptShape.StraightSection(new Carrier()),
            GraphicConceptShape.CircularArcSection(new Carrier()));
        var tangentEvidence = ReadJoinEvidence(tangentSource);
        var cuspEvidence = ReadJoinEvidence(cuspSource);
        var source = GraphicSelectionSurface.Frame(new Carrier(), tangentSource, cuspSource);
        var domain = GraphicSelectionSurface.Frame(new Carrier(), tangentEvidence, cuspEvidence);
        var conceptPattern = GraphicConceptShape.CuspJoinEvidenceSurface(new Carrier());

        var selection = GraphicSelectionSurface.Search(new Carrier(), source, domain, conceptPattern);

        Assert.True(GraphicSelectionSurface.TryReadSelection(
            selection,
            out var readSource,
            out var readDomain,
            out var readPattern,
            out var readMatches,
            out _));
        Assert.Same(source, readSource);
        Assert.Same(domain, readDomain);
        Assert.Same(conceptPattern, readPattern);
        Assert.True(GraphicSelectionSurface.TryReadMatchFrame(
            Assert.IsType<Relation>(readMatches),
            out var matches));
        var match = Assert.Single(matches);
        Assert.True(GraphicSelectionSurface.TryReadMatch(
            Assert.IsType<Relation>(match),
            out var candidate,
            out _,
            out var bindings,
            out var evidence,
            out _));
        Assert.Same(cuspEvidence, candidate);
        Assert.True(FocusSurface.TryReadBindingFrame(bindings, out var bindingClauses));
        Assert.Contains(bindingClauses, binding => ReferenceEquals(binding.Left, conceptPattern) && ReferenceEquals(binding.Right, cuspEvidence));
        Assert.True(GraphicSelectionSurface.TryReadPatternEvidence(
            Assert.IsType<Relation>(evidence),
            out _,
            out var appliedPattern,
            out var evidenceCandidate,
            out _));
        Assert.Same(cuspEvidence, appliedPattern);
        Assert.Same(cuspEvidence, evidenceCandidate);
    }

    [Fact]
    public void StructuralSearchCanFindNestedConceptEvidenceInsideComposite()
    {
        var cuspSource = GraphicConceptShape.CuspJoin(
            new Carrier(),
            GraphicConceptShape.StraightSection(new Carrier()),
            GraphicConceptShape.CircularArcSection(new Carrier()));
        var cuspEvidence = ReadJoinEvidence(cuspSource);
        var conceptPattern = GraphicConceptShape.CuspJoinEvidenceSurface(new Carrier());

        var selection = GraphicSelectionSurface.Search(new Carrier(), cuspSource, cuspSource, conceptPattern);

        Assert.True(GraphicSelectionSurface.TryReadSelection(
            selection,
            out _,
            out _,
            out _,
            out var readMatches,
            out _));
        Assert.True(GraphicSelectionSurface.TryReadMatchFrame(
            Assert.IsType<Relation>(readMatches),
            out var matches));
        Assert.Contains(matches, match =>
            match is Relation relation &&
            GraphicSelectionSurface.TryReadMatch(
                relation,
                out var candidate,
                out _,
                out _,
                out _,
                out _) &&
            ReferenceEquals(candidate, cuspEvidence));
    }

    [Fact]
    public void StructuralSearchLeavesInspectableResidueWhenConceptDoesNotSettle()
    {
        var tangentSource = GraphicConceptShape.TangentJoin(
            new Carrier(),
            GraphicConceptShape.StraightSection(new Carrier()),
            GraphicConceptShape.CircularArcSection(new Carrier()));
        var tangentEvidence = ReadJoinEvidence(tangentSource);
        var domain = GraphicSelectionSurface.Frame(new Carrier(), tangentEvidence);
        var conceptPattern = GraphicConceptShape.CuspJoinEvidenceSurface(new Carrier());

        var selection = GraphicSelectionSurface.Search(new Carrier(), tangentSource, domain, conceptPattern);

        Assert.True(GraphicSelectionSurface.TryReadSelection(
            selection,
            out _,
            out _,
            out _,
            out var readMatches,
            out var residue));
        Assert.True(GraphicSelectionSurface.TryReadMatchFrame(
            Assert.IsType<Relation>(readMatches),
            out var matches));
        Assert.Empty(matches);
        Assert.Contains(domain, Walk(residue));
        Assert.Contains(conceptPattern, Walk(residue));
    }

    private static Relation ReadJoinEvidence(Relation composite)
    {
        Assert.True(GraphicConceptShape.TryReadCompositeShape(
            composite,
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
        return Assert.IsType<Relation>(joinEvidence);
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
