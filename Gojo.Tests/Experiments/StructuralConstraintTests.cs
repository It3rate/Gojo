using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Tests.Experiments;

/// <summary>
/// Carrier-free probes of an explicit local constraint passage. State names
/// below are presentation labels; identity, ordered relations, scope closure,
/// selected variables and correspondence carry the computation.
/// </summary>
public sealed class StructuralConstraintTests
{
    [Fact]
    public void ScopeClosureRequiresTheActualSharedSeamRatherThanAnotherOpenSite()
    {
        var (first, second, third, _) = States();
        var closed = StructuralConstraint.Closed(first, second, third);
        var open = StructuralConstraint.Open(first, second, third);

        Assert.True(StructuralConstraint.TryReadScope(closed, out var members, out var seam, out var tail));
        Assert.Equal([first, second, third], members);
        Assert.Equal(seam, tail);
        Assert.True(StructuralConstraint.TryReadScope(open, out var offered, out var openSeam, out var openTail));
        Assert.Equal(members, offered);
        Assert.NotEqual(openSeam, openTail);

        // The terminal has the same primitive shape as the seam but does not
        // close through the seam's identity.
        var impostor = new Placeholder();
        var unfinished = new Relation(seam, new Relation(first, impostor));
        Assert.True(StructuralConstraint.TryReadScope(unfinished, out _, out var readSeam, out var readTail));
        Assert.Same(seam, readSeam);
        Assert.Same(impostor, readTail);
        Assert.NotEqual(readSeam, readTail);
        AssertCarrierFree(closed, open, unfinished);
    }

    [Fact]
    public void ClosedDomainAdmitsAKnownMemberAndBlocksAKnownNonmember()
    {
        var (first, second, third, outside) = States();
        var candidate = new Placeholder();
        var variables = StructuralConstraint.Closed(candidate);
        var domain = StructuralConstraint.Closed(first, second, third);
        var admitted = StructuralConstraint.Match(candidate, variables, domain, new Relation(candidate, second));
        var rejected = StructuralConstraint.Match(candidate, variables, domain, new Relation(candidate, outside));

        Assert.True(StructuralConstraint.TryReadUnique(admitted, out var member, out _));
        Assert.Same(second, member);
        var accepted = Assert.Single(Candidates(admitted));
        Assert.True(StructuralConstraint.TryReadCandidate(accepted, out var offered, out var instance, out var bindings));
        Assert.Same(second, offered);
        Assert.Same(second, instance);
        AssertBinding(bindings, candidate, second);
        Assert.Empty(Unresolved(admitted));

        Assert.Empty(Candidates(rejected));
        Assert.NotEmpty(Blocked(rejected));
        Assert.Empty(Unresolved(rejected));
        Assert.False(StructuralConstraint.ApplyUnique(rejected, candidate, out var unchanged));
        Assert.Same(candidate, unchanged);
        AssertCarrierFree(admitted, rejected);
    }

    [Fact]
    public void UnknownCandidatePreservesAllClosedDomainAlternativesWithoutChoosingTheFirst()
    {
        var (first, second, third, _) = States();
        var candidate = new Placeholder();
        var read = StructuralConstraint.Match(candidate, StructuralConstraint.Closed(candidate),
            StructuralConstraint.Closed(first, second, third));

        var alternatives = Candidates(read);
        Assert.Equal(3, alternatives.Length);
        Assert.Equal([first, second, third], alternatives.Select(Instance).ToArray());
        Assert.Empty(Unresolved(read));
        Assert.False(StructuralConstraint.TryReadUnique(read, out _, out _));
        Assert.False(StructuralConstraint.ApplyUnique(read, candidate, out var unchanged));
        Assert.Same(candidate, unchanged);
        AssertCarrierFree(read);
    }

    [Fact]
    public void OpenDomainRetainsUnknownMembershipAndAnUnenumeratedAlternative()
    {
        var (first, second, third, outside) = States();
        var candidate = new Placeholder();
        var variables = StructuralConstraint.Closed(candidate);
        var domain = StructuralConstraint.Open(first, second, third);
        var unknown = StructuralConstraint.Match(candidate, variables, domain);
        var absent = StructuralConstraint.Match(candidate, variables, domain, new Relation(candidate, outside));
        var known = StructuralConstraint.Match(candidate, variables, domain, new Relation(candidate, first));

        Assert.Equal(3, Candidates(unknown).Length);
        Assert.NotEmpty(Unresolved(unknown));
        Assert.Empty(Candidates(absent));
        Assert.NotEmpty(Unresolved(absent));
        Assert.False(StructuralConstraint.TryReadUnique(absent, out _, out _));
        Assert.Single(Candidates(known));
        // The known member has a witness even while the enumeration remains
        // open. The deliberately conservative unique reader retains that tail.
        Assert.Same(first, Instance(Assert.Single(Candidates(known))));
        Assert.NotEmpty(Unresolved(known));
        AssertCarrierFree(unknown, absent, known);
    }

    [Fact]
    public void NominalMembershipKeepsRelationIdentityDistinctFromLookalikeShape()
    {
        var (first, _, _, _) = States();
        var lookalike = new Relation(first.Left, first.Right);
        var candidate = new Placeholder();
        Assert.Equal(first, lookalike);
        Assert.False(first.SameIdentityAs(lookalike));

        var read = StructuralConstraint.Match(candidate, StructuralConstraint.Closed(candidate),
            StructuralConstraint.Closed(first), new Relation(candidate, lookalike));

        Assert.Empty(Candidates(read));
        Assert.NotEmpty(Blocked(read));
        Assert.Empty(Unresolved(read));
    }

    [Fact]
    public void ShapeMatchingRequiresExplicitSelectedSlotsRatherThanAllPlaceholderLeaves()
    {
        var (first, second, _, _) = States();
        var lookalike = new Relation(first.Left, first.Right);
        var slot = new Placeholder();
        var shape = new Relation(slot, slot);
        var shaped = StructuralConstraint.Match(shape, StructuralConstraint.Closed(slot),
            StructuralConstraint.Closed(first, second, lookalike));
        var nominal = StructuralConstraint.Match(first, StructuralConstraint.Closed(),
            StructuralConstraint.Closed(lookalike));

        Assert.Equal(2, Candidates(shaped).Length);
        Assert.Single(Blocked(shaped));
        Assert.Empty(Candidates(nominal));
        foreach (var candidate in Candidates(shaped))
        {
            Assert.True(StructuralConstraint.TryReadCandidate(candidate, out _, out _, out var bindings));
            AssertBinding(bindings, slot, first.Left);
        }
        AssertCarrierFree(shaped, nominal);
    }

    [Fact]
    public void NominalParticipantIdentityDoesNotCollapseOpposedPlaceholderFaces()
    {
        var participant = new Placeholder();
        var candidate = new Placeholder();
        var read = StructuralConstraint.Match(candidate, StructuralConstraint.Closed(candidate),
            StructuralConstraint.Closed(participant), new Relation(candidate, participant.Dual()));

        Assert.Empty(Candidates(read));
        Assert.NotEmpty(Blocked(read));
        Assert.Empty(Unresolved(read));
        AssertCarrierFree(read);
    }

    [Fact]
    public void RepeatedOffersRetainTheirSeparateCandidateOccurrences()
    {
        var (first, _, _, _) = States();
        var candidate = new Placeholder();
        var read = StructuralConstraint.Match(candidate, StructuralConstraint.Closed(candidate),
            StructuralConstraint.Closed(first, first));

        var occurrences = Candidates(read);
        Assert.Equal(2, occurrences.Length);
        Assert.NotSame(occurrences[0], occurrences[1]);
        Assert.All(occurrences, occurrence => Assert.Same(first, Instance(occurrence)));
        Assert.False(StructuralConstraint.TryReadUnique(read, out _, out _));
        AssertCarrierFree(read);
    }

    [Fact]
    public void AnUnselectedDualFaceRemainsRigidDuringMatchingAndApplication()
    {
        var (first, _, _, _) = States();
        var slot = new Placeholder();
        var dual = slot.Dual();
        var pattern = new Relation(slot, dual);
        var read = StructuralConstraint.Match(pattern, StructuralConstraint.Closed(slot),
            StructuralConstraint.Closed(new Relation(first, dual)));

        Assert.True(StructuralConstraint.ApplyUnique(read, pattern, out var applied));
        var result = Assert.IsType<Relation>(applied);
        Assert.Same(first, result.Left);
        Assert.Same(dual, result.Right);
        Assert.Same(slot, pattern.Left);
        AssertCarrierFree(read, result);
    }

    [Fact]
    public void ExplicitTransitionTemplateAdmitsTheAuthoredOrderedEdge()
    {
        var (first, second, third, _) = States();
        var from = new Placeholder();
        var next = new Placeholder();
        var pattern = new Relation(from, next);
        var firstEdge = new Relation(first, second);
        var support = StructuralConstraint.Closed(firstEdge, new Relation(second, third));
        var read = StructuralConstraint.Match(pattern, StructuralConstraint.Closed(from, next), support,
            new Relation(from, first), new Relation(next, second));

        Assert.True(StructuralConstraint.TryReadUnique(read, out var result, out var frame));
        var edge = Assert.IsType<Relation>(result);
        Assert.Same(first, edge.Left);
        Assert.Same(second, edge.Right);
        AssertBinding(frame, from, first);
        AssertBinding(frame, next, second);
        Assert.True(StructuralConstraint.TryReadCandidate(Assert.Single(Candidates(read)), out var offered, out _, out _));
        Assert.Same(firstEdge, offered);
        Assert.True(StructuralConstraint.ApplyUnique(read, next, out var applied));
        Assert.Same(second, applied);
        Assert.Same(from, pattern.Left);
        Assert.Same(next, pattern.Right);
        AssertCarrierFree(read);
    }

    [Fact]
    public void MissingAndReversedEdgesAreBlockedWithoutAnImplicitTransitiveTransition()
    {
        var (first, second, third, _) = States();
        var from = new Placeholder();
        var next = new Placeholder();
        var pattern = new Relation(from, next);
        var variables = StructuralConstraint.Closed(from, next);
        var support = StructuralConstraint.Closed(new Relation(first, second), new Relation(second, third));
        var missing = StructuralConstraint.Match(pattern, variables, support,
            new Relation(from, first), new Relation(next, third));
        var reversed = StructuralConstraint.Match(pattern, variables, support,
            new Relation(from, second), new Relation(next, first));

        foreach (var read in new[] { missing, reversed })
        {
            Assert.Empty(Candidates(read));
            Assert.NotEmpty(Blocked(read));
            Assert.Empty(Unresolved(read));
            Assert.False(StructuralConstraint.ApplyUnique(read, pattern, out var unchanged));
            Assert.Same(pattern, unchanged);
        }
        AssertCarrierFree(missing, reversed);
    }

    [Fact]
    public void ClosedOutgoingConstraintRefinesAnUnresolvedDestination()
    {
        var (first, second, third, _) = States();
        var from = new Placeholder();
        var next = new Placeholder();
        var read = StructuralConstraint.Match(new Relation(from, next), StructuralConstraint.Closed(from, next),
            StructuralConstraint.Closed(new Relation(first, second), new Relation(second, third)), new Relation(from, first));

        Assert.True(StructuralConstraint.ApplyUnique(read, next, out var destination));
        Assert.Same(second, destination);
        Assert.Empty(Unresolved(read));
        AssertCarrierFree(read);
    }

    [Fact]
    public void AnOpenOutgoingConstraintDoesNotTurnItsOneKnownDestinationIntoAUniqueAnswer()
    {
        var (first, second, _, _) = States();
        var from = new Placeholder();
        var next = new Placeholder();
        var read = StructuralConstraint.Match(new Relation(from, next), StructuralConstraint.Closed(from, next),
            StructuralConstraint.Open(new Relation(first, second)), new Relation(from, first));

        Assert.Single(Candidates(read));
        Assert.NotEmpty(Unresolved(read));
        Assert.False(StructuralConstraint.ApplyUnique(read, next, out var unchanged));
        Assert.Same(next, unchanged);
        AssertCarrierFree(read);
    }

    [Fact]
    public void AddingAndRemovingExposedAlternativesRefinesWithoutOverwritingEarlierReads()
    {
        var (first, second, third, _) = States();
        var from = new Placeholder();
        var next = new Placeholder();
        var pattern = new Relation(from, next);
        var variables = StructuralConstraint.Closed(from, next);
        var edgeToSecond = new Relation(first, second);
        var edgeToThird = new Relation(first, third);
        var initial = new Relation(from, first);
        var originalRequest = StructuralConstraint.Request(pattern, variables,
            StructuralConstraint.Closed(edgeToSecond), initial);
        var original = StructuralConstraint.Read(originalRequest);
        var expanded = StructuralConstraint.Match(pattern, variables,
            StructuralConstraint.Closed(edgeToSecond, edgeToThird), initial);
        var narrowed = StructuralConstraint.Match(pattern, variables,
            StructuralConstraint.Closed(edgeToThird), initial);

        Assert.True(StructuralConstraint.TryRead(original, out var retainedRequest, out _, out _, out _));
        Assert.Same(originalRequest, retainedRequest);
        Assert.True(StructuralConstraint.ApplyUnique(original, next, out var before));
        Assert.Same(second, before);
        Assert.Equal(2, Candidates(expanded).Length);
        Assert.False(StructuralConstraint.ApplyUnique(expanded, next, out var ambiguous));
        Assert.Same(next, ambiguous);
        Assert.True(StructuralConstraint.ApplyUnique(narrowed, next, out var after));
        Assert.Same(third, after);
        Assert.True(StructuralConstraint.ApplyUnique(original, next, out var retained));
        Assert.Same(second, retained);
        Assert.True(StructuralConstraint.ApplyUnique(StructuralConstraint.Read(originalRequest), next, out var reread));
        Assert.Same(second, reread);
        AssertCarrierFree(original, expanded, narrowed);
    }

    [Fact]
    public void ConflictingInitialBindingsRemainUnresolvedInsteadOfSelectingTheFirst()
    {
        var (first, second, _, _) = States();
        var slot = new Placeholder();
        var read = StructuralConstraint.Match(slot, StructuralConstraint.Closed(slot),
            StructuralConstraint.Closed(first, second), new Relation(slot, first), new Relation(slot, second));

        Assert.Empty(Candidates(read));
        Assert.NotEmpty(Unresolved(read));
        Assert.False(StructuralConstraint.ApplyUnique(read, slot, out var unchanged));
        Assert.Same(slot, unchanged);
        AssertCarrierFree(read);
    }

    [Fact]
    public void LocalCompositionChecksEveryProjectedAlternativeThroughTheSharedParticipant()
    {
        var (first, second, third, terminal) = States();
        var from = new Placeholder();
        var shared = new Placeholder();
        var end = new Placeholder();
        var firstRead = StructuralConstraint.Match(new Relation(from, shared), StructuralConstraint.Closed(from, shared),
            StructuralConstraint.Closed(new Relation(first, second), new Relation(first, third)), new Relation(from, first));
        var published = StructuralConstraint.Project(firstRead, shared);
        var secondPattern = new Relation(shared, end);
        var secondVariables = StructuralConstraint.Closed(shared, end);
        var secondSupport = StructuralConstraint.Closed(new Relation(second, terminal));

        // The harness connects each exposed candidate to the other passage;
        // it supplies no test for edge legality or preferred destination.
        var downstream = Candidates(published).Select(candidate =>
            StructuralConstraint.Match(secondPattern, secondVariables, secondSupport,
                new Relation(shared, Instance(candidate)))).ToArray();

        Assert.Equal(2, downstream.Length);
        var admitted = Assert.Single(downstream, read => Candidates(read).Length != 0);
        var rejected = Assert.Single(downstream, read => Candidates(read).Length == 0);
        Assert.True(StructuralConstraint.ApplyUnique(admitted, end, out var result));
        Assert.Same(terminal, result);
        Assert.NotEmpty(Blocked(rejected));
        Assert.Empty(Unresolved(rejected));
        Assert.Equal(2, Candidates(firstRead).Length);
        foreach (var candidate in Candidates(published))
        {
            Assert.True(StructuralConstraint.TryReadCandidate(candidate, out var provenance, out _, out _));
            Assert.Contains(Candidates(firstRead), original => ReferenceEquals(original, provenance));
        }
        AssertCarrierFree(firstRead, published, admitted, rejected);
    }

    [Fact]
    public void ComposedConstraintRetainsEveryBranchAndAnOpenUpstreamRemainder()
    {
        var (first, second, third, terminal) = States();
        var from = new Placeholder();
        var shared = new Placeholder();
        var end = new Placeholder();
        var pattern = new Relation(from, shared);
        var variables = StructuralConstraint.Closed(from, shared);
        var firstSupport = StructuralConstraint.Closed(new Relation(first, second), new Relation(first, third));
        var firstRead = StructuralConstraint.Match(pattern, variables, firstSupport, new Relation(from, first));
        var joined = StructuralConstraint.Join(firstRead, new Relation(shared, end),
            StructuralConstraint.Closed(shared, end), StructuralConstraint.Closed(new Relation(second, terminal)));

        Assert.Single(Candidates(joined));
        Assert.NotEmpty(Blocked(joined));
        Assert.Empty(Unresolved(joined));
        Assert.True(StructuralConstraint.ApplyUnique(joined, end, out var result));
        Assert.Same(terminal, result);
        Assert.True(StructuralConstraint.TryReadCandidate(Assert.Single(Candidates(joined)), out var route, out _, out _));
        var correspondence = Assert.IsType<Relation>(route);
        Assert.Contains(Candidates(firstRead), candidate => ReferenceEquals(candidate, correspondence.Left));

        Assert.True(StructuralConstraint.TryReadScope(firstSupport, out var edges, out _, out _));
        var openFirstRead = StructuralConstraint.Match(pattern, variables, StructuralConstraint.Open(edges), new Relation(from, first));
        var openJoined = StructuralConstraint.Join(openFirstRead, new Relation(shared, end),
            StructuralConstraint.Closed(shared, end), StructuralConstraint.Closed(new Relation(second, terminal)));
        Assert.Single(Candidates(openJoined));
        Assert.NotEmpty(Unresolved(openJoined));
        Assert.False(StructuralConstraint.ApplyUnique(openJoined, end, out var unchanged));
        Assert.Same(end, unchanged);
        AssertCarrierFree(firstRead, joined, openFirstRead, openJoined);
    }

    [Fact]
    public void AnOfferedRelationItselfParticipatesInAnotherConstraintWithoutAWrapper()
    {
        var (first, second, third, _) = States();
        var edge = new Relation(first, second);
        var role = new Placeholder();
        var response = new Placeholder();
        var firstRead = StructuralConstraint.Match(role, StructuralConstraint.Closed(role),
            StructuralConstraint.Closed(edge), new Relation(role, edge));
        Assert.True(StructuralConstraint.TryReadUnique(firstRead, out var participant, out _));
        Assert.Same(edge, participant);
        var higher = StructuralConstraint.Match(new Relation(role, response), StructuralConstraint.Closed(role, response),
            StructuralConstraint.Closed(new Relation(edge, third)), new Relation(role, participant));

        Assert.True(StructuralConstraint.ApplyUnique(higher, response, out var result));
        Assert.Same(third, result);
        var lookalike = new Relation(edge.Left, edge.Right);
        var unrelated = StructuralConstraint.Match(new Relation(role, response), StructuralConstraint.Closed(role, response),
            StructuralConstraint.Closed(new Relation(edge, third)), new Relation(role, lookalike));
        Assert.Empty(Candidates(unrelated));
        Assert.NotEmpty(Blocked(unrelated));
        AssertCarrierFree(firstRead, higher, unrelated);
    }

    private static (Relation First, Relation Second, Relation Third, Relation Outside) States()
    {
        var a = new Placeholder();
        var b = new Placeholder();
        return (new Relation(a, a), new Relation(a, b), new Relation(b, a), new Relation(b, b));
    }

    private static Relation[] Candidates(Relation read)
    {
        Assert.True(StructuralConstraint.TryRead(read, out _, out var candidates, out _, out _));
        return Entries(candidates).Select(Assert.IsType<Relation>).ToArray();
    }

    private static IElement[] Blocked(Relation read)
    {
        Assert.True(StructuralConstraint.TryRead(read, out _, out _, out var blocked, out _));
        return Entries(blocked);
    }

    private static IElement[] Unresolved(Relation read)
    {
        Assert.True(StructuralConstraint.TryRead(read, out _, out _, out _, out var unresolved));
        return Entries(unresolved);
    }

    private static IElement[] Entries(Relation scope)
    {
        Assert.True(StructuralConstraint.TryReadScope(scope, out var entries, out _, out _));
        return entries;
    }

    private static IElement Instance(Relation candidate)
    {
        Assert.True(StructuralConstraint.TryReadCandidate(candidate, out _, out var instance, out _));
        return instance;
    }

    private static void AssertBinding(Relation scope, Placeholder slot, IElement expected)
    {
        var binding = Assert.Single(Entries(scope).Select(Assert.IsType<Relation>),
            entry => Equals(entry.Left, slot));
        Assert.Same(expected, binding.Right);
    }

    private static void AssertCarrierFree(params Relation[] structures)
    {
        foreach (var structure in structures)
        {
            Assert.Empty(structure.Carriers());
            Assert.Empty(structure.CarrierLocations());
        }
    }
}
