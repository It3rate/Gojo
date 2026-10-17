using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Tests.Experiments;

/// <summary>False-read controls for the shared local structural passage.</summary>
public sealed class StructuralConstraintReadSafetyTests
{
    [Fact]
    public void ProjectionBindsOnlyTheSelectedPlaceholderFace()
    {
        var slot = new Placeholder();
        var opposed = slot.Dual();
        var participant = new Placeholder();
        var read = StructuralConstraint.Match(slot, StructuralConstraint.Closed(slot),
            StructuralConstraint.Closed(participant));
        var target = new Relation(slot, opposed);

        var projected = StructuralConstraint.Project(read, target);

        Assert.True(StructuralConstraint.TryReadUnique(projected, out var result, out _));
        var relation = Assert.IsType<Relation>(result);
        Assert.Same(participant, relation.Left);
        Assert.Same(opposed, relation.Right);
        Assert.Same(slot, target.Left);
        Assert.Same(opposed, target.Right);
        Assert.Empty(Entries(projected, unresolved: true));
    }

    [Fact]
    public void SiteIdentityUsesTheSharedTickReferenceRatherThanCoordinateEquality()
    {
        var carrier = new Carrier();
        var sharedTick = new TickRef(4);
        IElement selectedSite = new CarrierLocation(carrier, sharedTick);
        IElement offeredSameSite = new CarrierLocation(carrier, sharedTick);
        IElement equalCoordinate = new CarrierLocation(carrier, 4L);
        IElement opposedCarrierSite = new CarrierLocation(carrier.Dual(), sharedTick);
        var slot = new Placeholder();
        var read = StructuralConstraint.Match(slot, StructuralConstraint.Closed(slot),
            StructuralConstraint.Closed(offeredSameSite, equalCoordinate, opposedCarrierSite),
            new Relation(slot, selectedSite));

        Assert.Equal(selectedSite, equalCoordinate);
        Assert.NotSame(selectedSite, offeredSameSite);
        Assert.True(StructuralConstraint.TryReadUnique(read, out var result, out _));
        var location = Assert.IsType<CarrierLocation>(result);
        Assert.True(location.SameTickRefAs(Assert.IsType<CarrierLocation>(selectedSite)));
        Assert.Same(carrier, location.Carrier);
        var candidate = Assert.IsType<Relation>(Assert.Single(Entries(read)));
        Assert.True(StructuralConstraint.TryReadCandidate(candidate, out var offered, out _, out _));
        Assert.Same(offeredSameSite, offered);
        Assert.Equal(2, Entries(read, blocked: true).Length);
        Assert.Empty(Entries(read, unresolved: true));
    }

    [Fact]
    public void ASelectedButUnboundOutputStaysOpenAtTheApplicationPassage()
    {
        var input = new Placeholder();
        var output = new Placeholder();
        var participant = new Placeholder();
        var read = StructuralConstraint.Match(input, StructuralConstraint.Closed(input, output),
            StructuralConstraint.Closed(participant));
        var target = new Relation(input, output);

        Assert.True(StructuralConstraint.TryReadUnique(read, out var knownInput, out _));
        Assert.Same(participant, knownInput);
        var projected = StructuralConstraint.Project(read, target);
        Assert.Empty(Entries(projected));
        var open = Assert.IsType<Relation>(Assert.Single(Entries(projected, unresolved: true)));
        Assert.Same(target, open.Right);
        Assert.Contains(Entries(read), candidate => ReferenceEquals(candidate, open.Left));
        Assert.False(StructuralConstraint.ApplyUnique(read, target, out var unchanged));
        Assert.Same(target, unchanged);
        Assert.Same(input, target.Left);
        Assert.Same(output, target.Right);
    }

    [Fact]
    public void ARepeatedRelationPatternRequiresTheSameOfferedRelationParticipant()
    {
        var left = new Placeholder();
        var right = new Placeholder();
        var sharedPattern = new Relation(left, right);
        var pattern = new Relation(sharedPattern, sharedPattern);
        var first = new Placeholder();
        var second = new Placeholder();
        var sharedParticipant = new Relation(first, second);
        var corresponding = new Relation(sharedParticipant, sharedParticipant);
        var copiedParticipant = new Relation(first, second);
        var lookalikeTopology = new Relation(sharedParticipant, copiedParticipant);

        var read = StructuralConstraint.Match(pattern, StructuralConstraint.Closed(left, right),
            StructuralConstraint.Closed(corresponding, lookalikeTopology));

        Assert.Equal(sharedParticipant, copiedParticipant);
        Assert.False(sharedParticipant.SameIdentityAs(copiedParticipant));
        Assert.True(StructuralConstraint.TryReadUnique(read, out var result, out _));
        var instance = Assert.IsType<Relation>(result);
        Assert.Same(instance.Left, instance.Right);
        var candidate = Assert.IsType<Relation>(Assert.Single(Entries(read)));
        Assert.True(StructuralConstraint.TryReadCandidate(candidate, out var offered, out _, out _));
        Assert.Same(corresponding, offered);
        Assert.Single(Entries(read, blocked: true));
        Assert.Empty(Entries(read, unresolved: true));
    }

    [Fact]
    public void InvalidVariableFramesAndSelfContainingBindingsRemainUnresolved()
    {
        var slot = new Placeholder();
        var atom = new Placeholder();
        var support = StructuralConstraint.Closed(atom);
        var malformed = new[]
        {
            StructuralConstraint.Match(slot, StructuralConstraint.Open(slot), support),
            StructuralConstraint.Match(slot, StructuralConstraint.Closed(slot, slot), support),
            StructuralConstraint.Match(slot, StructuralConstraint.Closed(slot), support,
                new Relation(slot, new Relation(atom, slot))),
            StructuralConstraint.Match(slot, StructuralConstraint.Closed(slot), support,
                new Relation(new Placeholder(), atom))
        };

        foreach (var read in malformed)
        {
            Assert.Empty(Entries(read));
            Assert.Empty(Entries(read, blocked: true));
            Assert.NotEmpty(Entries(read, unresolved: true));
            Assert.False(StructuralConstraint.ApplyUnique(read, slot, out var unchanged));
            Assert.Same(slot, unchanged);
        }
    }

    [Fact]
    public void AnInvalidScopeDoesNotDiscoverAnAnswerInsideItsNestedEntries()
    {
        var slot = new Placeholder();
        var atom = new Placeholder();
        var nested = StructuralConstraint.Closed(atom);
        var malformedScope = new Relation(nested, StructuralConstraint.Closed(atom));

        Assert.False(StructuralConstraint.TryReadScope(malformedScope, out _, out _, out _));
        var read = StructuralConstraint.Match(slot, StructuralConstraint.Closed(slot), malformedScope);

        Assert.Empty(Entries(read));
        Assert.Empty(Entries(read, blocked: true));
        Assert.NotEmpty(Entries(read, unresolved: true));
        Assert.False(StructuralConstraint.TryReadUnique(read, out _, out _));
    }

    [Fact]
    public void AUniqueWitnessRejectsInternallyMalformedCandidateFrames()
    {
        var slot = new Placeholder();
        var first = new Placeholder();
        var second = new Placeholder();
        var foreign = new Placeholder();
        var variables = StructuralConstraint.Closed(slot);
        var request = StructuralConstraint.Request(slot, variables, StructuralConstraint.Closed(first, second));

        Relation ClaimedRead(IElement instance, params Relation[] bindings)
        {
            var candidate = new Relation(first,
                new Relation(instance, StructuralConstraint.Closed(bindings)));
            return new Relation(new Relation(request, variables),
                new Relation(StructuralConstraint.Closed(candidate),
                    new Relation(StructuralConstraint.Closed(), StructuralConstraint.Closed())));
        }

        var malformed = new[]
        {
            ClaimedRead(first, new Relation(foreign, first)),
            ClaimedRead(first, new Relation(slot, first), new Relation(slot, second)),
            ClaimedRead(first, new Relation(slot, new Relation(first, slot))),
            ClaimedRead(slot),
            ClaimedRead(slot, new Relation(slot, first))
        };

        foreach (var read in malformed)
        {
            // An ordered witness layout is not enough to make an inconsistent
            // selected binding frame usable at its guarded passage.
            Assert.True(StructuralConstraint.TryRead(read, out _, out _, out _, out _));
            Assert.False(StructuralConstraint.TryReadUnique(read, out _, out _));
            Assert.False(StructuralConstraint.ApplyUnique(read, slot, out var unchanged));
            Assert.Same(slot, unchanged);
        }
    }

    private static IElement[] Entries(Relation read, bool blocked = false, bool unresolved = false)
    {
        Assert.True(StructuralConstraint.TryRead(read, out _, out var candidates, out var rejected, out var open));
        var scope = unresolved ? open : blocked ? rejected : candidates;
        Assert.True(StructuralConstraint.TryReadScope(scope, out var entries, out var seam, out var tail));
        Assert.Equal(seam, tail);
        return entries;
    }
}
