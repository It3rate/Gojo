using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class MotifFieldCorrespondenceTests
{
    [Fact]
    public void OneReusableMotifRetainsMultipleActualRecognitionsAndACompetingFailedMap()
    {
        var motif = Chain(true, false, true); var field = Chain(true, false, true, false, true, false, true);
        var matcher = new MotifFieldCorrespondence(motif.Partition, S(motif.Cells));
        var roles = MotifFieldCorrespondence.RoleCorrespondence(motif.Pair, field.Pair);
        var maps = new[] { Map(motif, field, 0, 1, 2), Map(motif, field, 1, 2, 3), Map(motif, field, 4, 5, 6) };
        var read = matcher.Evaluate(field.Partition, roles, S(maps));
        Assert.True(matcher.TryReadResult(read, out var complement, out var outcomes, out var pending));
        Assert.Empty(E(pending)); Assert.Empty(E((Relation)((Relation)complement.Left).Left));
        var candidates = E(outcomes).Cast<Relation>().ToArray(); Assert.Equal(3, candidates.Length);
        Assert.True(matcher.TryReadPassed(field.Partition, candidates[0]));
        Assert.False(matcher.TryReadPassed(field.Partition, candidates[1]));
        Assert.True(matcher.TryReadPassed(field.Partition, candidates[2]));
        for (var i = 0; i < candidates.Length; i++)
        {
            Assert.True(matcher.TryReadCandidate(candidates[i], out var source, out var roleMap, out var map,
                out _, out var matched, out var unmatched, out _));
            Assert.Same(field.Partition.Surface, source); Assert.Same(roles, roleMap); Assert.Same(maps[i], map);
            Assert.NotEmpty(E(matched)); if (i == 1) Assert.NotEmpty(E(unmatched));
        }
        AssertNoNumbers(read); Assert.True(Contains(read, motif.Partition.Surface)); Assert.True(Contains(read, field.Partition.Surface));
    }

    [Fact]
    public void ExplicitChainReversalAndOppositeRoleMapRecognizeTheWhiteMotif()
    {
        var edges = new[] { (0, 1), (1, 0), (1, 2), (2, 1) };
        var motif = new Fixture([true, true, false], edges); var field = new Fixture([true, false, false], edges);
        var matcher = new MotifFieldCorrespondence(motif.Partition, S(motif.Cells));
        var reflection = Map(motif, field, 2, 1, 0);
        var opposite = MotifFieldCorrespondence.RoleCorrespondence(motif.Pair, field.Pair.Reverse());
        var candidate = Candidate(matcher, field, opposite, reflection);
        Assert.True(matcher.TryReadPassed(field.Partition, candidate));
        Assert.True(matcher.TryReadCandidate(candidate, out _, out _, out _, out _, out var matched, out _, out _));
        Assert.True(Contains(matched, motif.Labels[0])); Assert.True(Contains(matched, field.Labels[2]));
        Assert.Same(motif.First, motif.Labels[0].Right); Assert.Same(field.Second, field.Labels[2].Right);
        var sameRoles = MotifFieldCorrespondence.RoleCorrespondence(motif.Pair, field.Pair);
        Assert.False(matcher.TryReadPassed(field.Partition, Candidate(matcher, field, sameRoles, reflection)));
        AssertNoNumbers(candidate);
    }

    [Fact]
    public void PartialFocusCarriesBothUnselectedComplementsAndActualCutEdges()
    {
        var motif = Chain(true, false, true, false); var field = Chain(true, false, true, true, false);
        var focus = S(motif.Cells[1], motif.Cells[2]); var matcher = new MotifFieldCorrespondence(motif.Partition, focus);
        var roles = MotifFieldCorrespondence.RoleCorrespondence(motif.Pair, field.Pair);
        var map = S(new Relation(motif.Cells[1], field.Cells[1]), new Relation(motif.Cells[2], field.Cells[2]));
        var read = matcher.Evaluate(field.Partition, roles, S(map));
        Assert.True(matcher.TryReadResult(read, out var templateComplement, out var outcomes, out _));
        var candidate = (Relation)Assert.Single(E(outcomes)); Assert.True(matcher.TryReadPassed(field.Partition, candidate));
        Assert.True(matcher.TryReadCandidate(candidate, out _, out _, out _, out var fieldComplement, out _, out _, out _));
        Assert.Equal(new[] { motif.Cells[0], motif.Cells[3] }, E((Relation)((Relation)templateComplement.Left).Left));
        Assert.Equal(new[] { field.Cells[0], field.Cells[3], field.Cells[4] }, E((Relation)((Relation)fieldComplement.Left).Left));
        Assert.Equal(new IElement[] { motif.Edges[0], motif.Edges[2] }, E((Relation)((Relation)templateComplement.Right).Right));
        Assert.Equal(new IElement[] { field.Edges[0], field.Edges[2] }, E((Relation)((Relation)fieldComplement.Right).Right));
        Assert.Contains(E((Relation)((Relation)fieldComplement.Right).Left), edge => ReferenceEquals(edge, field.Edges[3]));
        Assert.Same(focus, matcher.Focus); Assert.True(Contains(read, focus));
    }

    [Fact]
    public void ExtraInternalTargetEdgeFailsEvenWhenEveryTemplateEdgeAndRoleMatch()
    {
        var motif = Chain(true, false);
        var field = new Fixture([true, false], [(0, 1), (1, 0)]);
        var matcher = new MotifFieldCorrespondence(motif.Partition, S(motif.Cells));
        var candidate = Candidate(matcher, field, MotifFieldCorrespondence.RoleCorrespondence(motif.Pair, field.Pair), Map(motif, field, 0, 1));
        Assert.False(matcher.TryReadPassed(field.Partition, candidate));
        Assert.True(matcher.TryReadCandidate(candidate, out _, out _, out _, out _, out var matched, out var unmatched, out var pending));
        Assert.NotEmpty(E(matched)); Assert.Empty(E(pending)); Assert.True(Contains(unmatched, field.Edges[1]));
    }

    [Fact]
    public void ReversedDirectedSupportDoesNotBecomeMatchingSupportThroughIncidenceAlone()
    {
        var motif = Chain(true, false); var field = new Fixture([true, false], [(1, 0)]);
        var matcher = new MotifFieldCorrespondence(motif.Partition, S(motif.Cells));
        var candidate = Candidate(matcher, field, MotifFieldCorrespondence.RoleCorrespondence(motif.Pair, field.Pair), Map(motif, field, 0, 1));
        Assert.False(matcher.TryReadPassed(field.Partition, candidate));
        Assert.True(matcher.TryReadCandidate(candidate, out _, out _, out _, out _, out _, out var unmatched, out var pending));
        Assert.Empty(E(pending)); Assert.True(Contains(unmatched, motif.Edges[0])); Assert.True(Contains(unmatched, field.Edges[0]));
    }

    [Fact]
    public void ManyToOneOrForeignMapsCannotStandInForAnActualOccurrence()
    {
        var motif = Chain(true, true); var field = Chain(true, true); var foreign = Chain(true, true);
        var matcher = new MotifFieldCorrespondence(motif.Partition, S(motif.Cells));
        var roles = MotifFieldCorrespondence.RoleCorrespondence(motif.Pair, field.Pair);
        foreach (var map in new[] { Map(motif, field, 0, 0), Map(motif, foreign, 0, 1) })
        {
            var candidate = Candidate(matcher, field, roles, map);
            Assert.False(matcher.TryReadPassed(field.Partition, candidate));
            Assert.True(matcher.TryReadCandidate(candidate, out _, out _, out _, out _, out _, out var unmatched, out _));
            Assert.NotEmpty(E(unmatched)); Assert.True(Contains(candidate, map));
        }
    }

    [Fact]
    public void MissingMapRowsAndOpenMapsRetainTheirUnfulfilledObligations()
    {
        var motif = Chain(true, false); var field = Chain(true, false);
        var matcher = new MotifFieldCorrespondence(motif.Partition, S(motif.Cells));
        var roles = MotifFieldCorrespondence.RoleCorrespondence(motif.Pair, field.Pair);
        foreach (var map in new[] { S(new Relation(motif.Cells[0], field.Cells[0])),
            StructuralConstraint.Open(new Relation(motif.Cells[0], field.Cells[0]), new Relation(motif.Cells[1], field.Cells[1])) })
        {
            var candidate = Candidate(matcher, field, roles, map);
            Assert.False(matcher.TryReadPassed(field.Partition, candidate));
            Assert.True(matcher.TryReadCandidate(candidate, out _, out _, out _, out _, out _, out _, out var pending));
            Assert.NotEmpty(E(pending)); Assert.True(Contains(pending, map));
        }
    }

    [Fact]
    public void MissingSelectedLabelsAndOpenTopologyCannotManufactureACompleteRecognition()
    {
        var motif = Chain(true, false); var field = Chain(true, false);
        var matcher = new MotifFieldCorrespondence(motif.Partition, S(motif.Cells));
        var roles = MotifFieldCorrespondence.RoleCorrespondence(motif.Pair, field.Pair); var map = Map(motif, field, 0, 1);
        foreach (var partial in new[] {
            new QualitativePartition(field.Partition.Cells, field.Partition.Adjacency, field.Pair, S(field.Labels[0])),
            new QualitativePartition(field.Partition.Cells, StructuralConstraint.Open(field.Edges), field.Pair, field.Partition.Labels) })
        {
            var read = matcher.Evaluate(partial, roles, S(map));
            Assert.True(matcher.TryReadResult(read, out _, out var candidates, out _)); var candidate = (Relation)Assert.Single(E(candidates));
            Assert.False(matcher.TryReadPassed(partial, candidate));
            Assert.True(matcher.TryReadCandidate(candidate, out _, out _, out _, out _, out _, out _, out var pending)); Assert.NotEmpty(E(pending));
        }
    }

    [Fact]
    public void UnknownLabelsOutsideTheSelectedOccurrenceDoNotForceAnUnrequestedFullFieldRead()
    {
        var motif = Chain(true, false, true); var field = Chain(false, true, false, true);
        var partialField = new QualitativePartition(field.Partition.Cells, field.Partition.Adjacency, field.Pair, S(field.Labels[1], field.Labels[2]));
        var matcher = new MotifFieldCorrespondence(motif.Partition, S(motif.Cells[0], motif.Cells[1]));
        var map = S(new Relation(motif.Cells[0], field.Cells[1]), new Relation(motif.Cells[1], field.Cells[2]));
        var read = matcher.Evaluate(partialField, MotifFieldCorrespondence.RoleCorrespondence(motif.Pair, field.Pair), S(map));
        Assert.True(matcher.TryReadResult(read, out _, out var candidates, out _)); var candidate = (Relation)Assert.Single(E(candidates));
        Assert.True(matcher.TryReadPassed(partialField, candidate));
        Assert.True(matcher.TryReadCandidate(candidate, out _, out _, out _, out var complement, out _, out _, out _));
        Assert.Equal(2, E((Relation)((Relation)complement.Left).Left).Length);
        Assert.Empty(E((Relation)((Relation)complement.Left).Right));
    }

    [Fact]
    public void CallerAuthoredEmptyFailuresAndInventedComplementsDoNotCreateCertificates()
    {
        var motif = Chain(true, false); var field = Chain(true, false, true);
        var matcher = new MotifFieldCorrespondence(motif.Partition, S(motif.Cells));
        var roles = MotifFieldCorrespondence.RoleCorrespondence(motif.Pair, field.Pair);
        var wrong = Candidate(matcher, field, roles, Map(motif, field, 1, 2));
        Assert.True(matcher.TryReadCandidate(wrong, out _, out _, out _, out var complement, out _, out _, out _));
        var erased = new Relation(wrong.Left, new Relation(complement, new Relation(S(), new Relation(S(), S()))));
        Assert.False(matcher.TryReadPassed(field.Partition, erased));
        var valid = Candidate(matcher, field, roles, Map(motif, field, 0, 1));
        var noComplement = new Relation(new Relation(S(), S()), new Relation(S(), S()));
        var forged = new Relation(valid.Left, new Relation(noComplement, ((Relation)valid.Right).Right));
        Assert.False(matcher.TryReadPassed(field.Partition, forged));
        var another = new MotifFieldCorrespondence(motif.Partition, S(motif.Cells));
        Assert.False(another.TryReadCandidate(valid, out _, out _, out _, out _, out _, out _, out _));
    }

    [Fact]
    public void AnOpenCandidateFamilyRetainsKnownCandidatesWithoutClaimingTheyAreTheOnlyOnes()
    {
        var motif = Chain(true, false); var field = Chain(true, false, true, false);
        var matcher = new MotifFieldCorrespondence(motif.Partition, S(motif.Cells));
        var map = Map(motif, field, 0, 1); var offered = StructuralConstraint.Open(map);
        var read = matcher.Evaluate(field.Partition, MotifFieldCorrespondence.RoleCorrespondence(motif.Pair, field.Pair), offered);
        Assert.True(matcher.TryReadResult(read, out _, out var candidates, out var pending));
        Assert.NotEmpty(E(pending)); Assert.True(Contains(pending, offered));
        Assert.True(matcher.TryReadPassed(field.Partition, (Relation)Assert.Single(E(candidates))));
        var emptyFocus = new MotifFieldCorrespondence(motif.Partition, S());
        var emptyRead = emptyFocus.Evaluate(field.Partition, MotifFieldCorrespondence.RoleCorrespondence(motif.Pair, field.Pair), S(S()));
        Assert.True(emptyFocus.TryReadResult(emptyRead, out _, out var emptyCandidates, out _));
        Assert.False(emptyFocus.TryReadPassed(field.Partition, (Relation)Assert.Single(E(emptyCandidates))));
    }

    [Fact]
    public void AnUnfinishedFocusDoesNotInventPositiveUnselectedComplementMembership()
    {
        var motif = Chain(true, false, true); var field = Chain(true, false, true);
        var matcher = new MotifFieldCorrespondence(motif.Partition, StructuralConstraint.Open(motif.Cells[0]));
        var map = S(new Relation(motif.Cells[0], field.Cells[0]));
        var read = matcher.Evaluate(field.Partition, MotifFieldCorrespondence.RoleCorrespondence(motif.Pair, field.Pair), S(map));
        Assert.True(matcher.TryReadResult(read, out var complement, out var candidates, out var pending));
        var cells = (Relation)((Relation)complement.Left).Left;
        Assert.True(StructuralConstraint.TryReadScope(cells, out var exposed, out var seam, out var tail));
        Assert.Empty(exposed); Assert.NotEqual(seam, tail); Assert.NotEmpty(E(pending));
        var candidate = (Relation)Assert.Single(E(candidates)); Assert.False(matcher.TryReadPassed(field.Partition, candidate));
        Assert.True(Contains(read, motif.Partition.Surface));
    }

    [Fact]
    public void PassingMapCannotCertifyAnOmittedOrReplacedMatchedReceipt()
    {
        var motif = Chain(true, false); var field = Chain(true, false, true);
        var matcher = new MotifFieldCorrespondence(motif.Partition, S(motif.Cells));
        var candidate = Candidate(matcher, field, MotifFieldCorrespondence.RoleCorrespondence(motif.Pair, field.Pair), Map(motif, field, 0, 1));
        Assert.True(matcher.TryReadCandidate(candidate, out _, out _, out _, out _, out var matched, out _, out _));
        Assert.True(matcher.TryReadPassed(field.Partition, candidate));
        Assert.False(matcher.TryReadPassed(field.Partition, WithMatched(candidate, S())));
        var replaced = E(matched); replaced[0] = replaced[1];
        Assert.False(matcher.TryReadPassed(field.Partition, WithMatched(candidate, S(replaced))));
    }

    [Fact]
    public void CertificateRequiresActualLabelMapAndTopologyRowsRatherThanLookalikeCopies()
    {
        var motif = Chain(true, false); var field = Chain(true, false);
        var matcher = new MotifFieldCorrespondence(motif.Partition, S(motif.Cells));
        var candidate = Candidate(matcher, field, MotifFieldCorrespondence.RoleCorrespondence(motif.Pair, field.Pair), Map(motif, field, 0, 1));
        Assert.True(matcher.TryReadCandidate(candidate, out _, out _, out _, out _, out var matched, out _, out _));
        var original = E(matched); var labelWitness = (Relation)original[0];
        var maps = (Relation)labelWitness.Left; var labels = (Relation)labelWitness.Right;
        var label = (Relation)labels.Left; var map = (Relation)maps.Left;
        foreach (var clone in new[] {
            new Relation(maps, new Relation(new Relation(label.Left, label.Right), labels.Right)),
            new Relation(new Relation(new Relation(map.Left, map.Right), maps.Right), labels) })
        {
            var replaced = original.ToArray(); replaced[0] = clone;
            Assert.False(matcher.TryReadPassed(field.Partition, WithMatched(candidate, S(replaced))));
        }
        var supportIndex = Array.FindIndex(original, row => row is Relation support && ReferenceEquals(support.Left, motif.Edges[0]));
        var supportWitness = (Relation)original[supportIndex]; var supportBody = (Relation)supportWitness.Right;
        var sourceEdge = (Relation)supportWitness.Left; var mappedEdge = (Relation)supportBody.Left;
        foreach (var clone in new[] {
            new Relation(new Relation(sourceEdge.Left, sourceEdge.Right), supportBody),
            new Relation(sourceEdge, new Relation(new Relation(mappedEdge.Right, mappedEdge.Left), supportBody.Right)) })
        {
            var replaced = original.ToArray(); replaced[supportIndex] = clone;
            Assert.False(matcher.TryReadPassed(field.Partition, WithMatched(candidate, S(replaced))));
        }
    }

    [Fact]
    public void FreshDerivedWrappersAndScopesCanRetainTheSameActualReceiptEvidence()
    {
        var motif = Chain(true, false); var field = Chain(true, false);
        var matcher = new MotifFieldCorrespondence(motif.Partition, S(motif.Cells));
        var candidate = Candidate(matcher, field, MotifFieldCorrespondence.RoleCorrespondence(motif.Pair, field.Pair), Map(motif, field, 0, 1));
        Assert.True(matcher.TryReadCandidate(candidate, out _, out _, out _, out _, out var matched, out _, out _));
        var edges = motif.Edges.Concat(field.Edges).ToArray();
        var rebuilt = E(matched).Cast<Relation>().Select(witness =>
        {
            var body = (Relation)witness.Right;
            if (edges.Any(edge => ReferenceEquals(edge, witness.Left)))
            {
                var mapped = (Relation)body.Left;
                return (IElement)new Relation(witness.Left,
                    new Relation(new Relation(mapped.Left, mapped.Right), S(E((Relation)body.Right))));
            }
            var maps = (Relation)witness.Left;
            return new Relation(new Relation(maps.Left, maps.Right), new Relation(body.Left, body.Right));
        }).ToArray();
        Assert.True(matcher.TryReadPassed(field.Partition, WithMatched(candidate, S(rebuilt))));
    }

    private sealed class Fixture
    {
        public IElement First { get; } = new Placeholder(); public IElement Second { get; } = new Placeholder();
        public Relation Pair { get; } public IElement[] Cells { get; } public Relation[] Labels { get; }
        public Relation[] Edges { get; } public QualitativePartition Partition { get; }
        public Fixture(bool[] first, (int From, int To)[] edges)
        {
            Pair = new Relation(First, Second); Cells = first.Select(_ => (IElement)new Placeholder()).ToArray();
            Labels = first.Select((value, i) => new Relation(Cells[i], value ? First : Second)).ToArray();
            Edges = edges.Select(edge => new Relation(Cells[edge.From], Cells[edge.To])).ToArray();
            Partition = new QualitativePartition(S(Cells), S(Edges), Pair, S(Labels));
        }
    }
    private static Fixture Chain(params bool[] first) => new(first, Enumerable.Range(0, first.Length - 1).Select(i => (i, i + 1)).ToArray());
    private static Relation Map(Fixture source, Fixture target, params int[] image) =>
        S(image.Select((to, from) => (IElement)new Relation(source.Cells[from], target.Cells[to])).ToArray());
    private static Relation Candidate(MotifFieldCorrespondence matcher, Fixture field, Relation roles, Relation map)
    {
        var read = matcher.Evaluate(field.Partition, roles, S(map));
        Assert.True(matcher.TryReadResult(read, out _, out var candidates, out _)); return (Relation)Assert.Single(E(candidates));
    }
    private static Relation WithMatched(Relation candidate, Relation matched)
    {
        var result = (Relation)candidate.Right; var evidence = (Relation)result.Right;
        return new Relation(candidate.Left, new Relation(result.Left, new Relation(matched, evidence.Right)));
    }
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Contains(IElement root, IElement target) => Traverse(root).Any(value => ReferenceEquals(value, target));
    private static void AssertNoNumbers(IElement root) => Assert.DoesNotContain(Traverse(root), value => value is Carrier or CarrierLocation);
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var value))
        { if (!seen.Add(value)) continue; yield return value; if (value is Relation relation) { stack.Push(relation.Right); stack.Push(relation.Left); } }
    }
}
