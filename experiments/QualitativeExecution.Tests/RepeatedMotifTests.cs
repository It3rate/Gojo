using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class RepeatedMotifTests
{
    [Fact]
    public void UnmeasuredAbaStripReusesTemplatesAndLandmarkButHasFreshOccurrencesAndLocalSites()
    {
        var (landmarks, a, b, _, _, _) = Fixtures();
        var first = a.Generate(); var second = b.Generate(); var third = a.Generate();
        var strip = Joined(StructuralConstraint.Closed(first, second, third));
        Settled(RepeatedMotif.Read(strip, StructuralConstraint.Closed(a.Surface, b.Surface, a.Surface)));
        var identities = new List<IElement>();
        var interiors = new List<IElement>();
        foreach (var (motif, occurrence) in new[] { (a, first), (b, second), (a, third) })
        {
            Assert.True(StructuralConstraint.TryReadUnique(motif.Recognize(StructuralConstraint.Closed(occurrence)), out _, out _));
            Assert.True(RepeatedMotif.TryReadOccurrence(occurrence, out var identity, out var template,
                out var instance, out _, out _));
            Assert.Same(motif.Surface, template); identities.Add(identity);
            Assert.True(StructuralConstraint.TryReadUnique(landmarks.ReadRoles((Relation)instance), out var roles, out _));
            var middle = (Relation)((Relation)((Relation)roles).Left).Right;
            Assert.Same(landmarks.Interior, middle.Left); interiors.Add(middle.Right);
            Assert.True(Contains(occurrence, landmarks.Frame));
        }
        Assert.Equal(3, identities.Distinct().Count());
        Assert.Equal(3, interiors.Distinct().Count());
        NoNumbers(strip);
    }

    [Fact]
    public void SuccessorAndExplicitSeamDoNotInventSharedSiteIdentity()
    {
        var (_, a, _, _, _, _) = Fixtures();
        var first = a.Generate(); var second = a.Generate();
        Assert.True(RepeatedMotif.TryReadOccurrence(first, out _, out _, out _, out _, out var exit));
        Assert.True(RepeatedMotif.TryReadOccurrence(second, out _, out _, out _, out var entry, out _));
        Assert.NotSame(exit, entry);
        var strip = Joined(StructuralConstraint.Closed(first, second));
        var edge = Assert.IsType<Relation>(Entries((Relation)((Relation)strip.Right).Left)[0]);
        var seam = Assert.IsType<Relation>(Entries((Relation)((Relation)strip.Right).Right)[0]);
        Assert.Same(first, edge.Left); Assert.Same(second, edge.Right);
        Assert.Same(edge, seam.Left);
        Assert.Same(exit, ((Relation)seam.Right).Left); Assert.Same(entry, ((Relation)seam.Right).Right);
        Settled(RepeatedMotif.Read(strip));
    }

    [Fact]
    public void SharedPortCanBeAuthoredButStillNeedsTheDeclaredJoin()
    {
        var (_, a, _, entrySlot, _, _) = Fixtures();
        var first = a.Generate();
        Assert.True(RepeatedMotif.TryReadOccurrence(first, out _, out _, out _, out _, out var exit));
        var second = a.Generate(bindings: [new Relation(entrySlot, exit)]);
        Assert.True(RepeatedMotif.TryReadOccurrence(second, out _, out _, out _, out var entry, out _));
        Assert.Same(exit, entry);
        var disconnected = RepeatedMotif.Sequence(StructuralConstraint.Closed(first, second));
        Rejected(RepeatedMotif.Read(disconnected));
        Settled(RepeatedMotif.Read(Joined((Relation)disconnected.Left)));
    }

    [Fact]
    public void ClosedMissingSeamIsAContradictionWhileOpenJoinSupportIsUnresolved()
    {
        var (_, a, _, _, _, _) = Fixtures();
        var strip = RepeatedMotif.Sequence(StructuralConstraint.Closed(a.Generate(), a.Generate()));
        Rejected(RepeatedMotif.Read(strip));
        var open = new Relation(strip.Left, new Relation(((Relation)strip.Right).Left, StructuralConstraint.Open()));
        Pending(RepeatedMotif.Read(open));
        Assert.True(Contains(RepeatedMotif.Read(open), open));
    }

    [Fact]
    public void OpenPrefixDoesNotBecomeACompleteFiniteStrip()
    {
        var (_, a, _, _, _, _) = Fixtures();
        var first = a.Generate(); var second = a.Generate();
        Pending(RepeatedMotif.Read(Joined(StructuralConstraint.Open(first, second))));
        Settled(RepeatedMotif.Read(Joined(StructuralConstraint.Closed(first, second))));
    }

    [Fact]
    public void TemplateOrderIsAReadOfActualOrderedOccurrences()
    {
        var (_, a, b, _, _, _) = Fixtures();
        var strip = Joined(StructuralConstraint.Closed(a.Generate(), b.Generate(), a.Generate()));
        Settled(RepeatedMotif.Read(strip, StructuralConstraint.Closed(a.Surface, b.Surface, a.Surface)));
        Rejected(RepeatedMotif.Read(strip, StructuralConstraint.Closed(b.Surface, a.Surface, a.Surface)));
        Rejected(RepeatedMotif.Read(strip, StructuralConstraint.Closed(a.Surface, b.Surface)));
        Assert.False(StructuralConstraint.TryReadUnique(a.Recognize(StructuralConstraint.Closed(b.Generate())), out _, out _));
    }

    [Fact]
    public void ReusingAnOccurrenceOrItsIdentityIsNotAnotherOccurrenceOfTheTemplate()
    {
        var (_, a, _, _, _, _) = Fixtures();
        var first = a.Generate();
        Rejected(RepeatedMotif.Read(Joined(StructuralConstraint.Closed(first, first))));
        Assert.True(RepeatedMotif.TryReadOccurrence(first, out var identity, out _, out _, out _, out _));
        var replay = a.Generate(identity);
        Assert.NotSame(first, replay);
        Rejected(RepeatedMotif.Read(Joined(StructuralConstraint.Closed(first, replay))));
    }

    [Fact]
    public void CopyingAnOccurrenceCannotImpersonateTheParticipantConsumedByItsSuccessor()
    {
        var (_, a, _, _, _, _) = Fixtures();
        var first = a.Generate(); var second = a.Generate();
        var original = Joined(StructuralConstraint.Closed(first, second));
        var copy = new Relation(second.Left, second.Right);
        Assert.True(RepeatedMotif.TryReadOccurrence(copy, out _, out _, out _, out _, out _));
        var wrong = new Relation(StructuralConstraint.Closed(first, copy), original.Right);
        Rejected(RepeatedMotif.Read(wrong));
        Assert.True(Contains(RepeatedMotif.Read(wrong), second));
    }

    [Fact]
    public void EqualLookingSuccessorDoesNotSupplyTheActualSeamCorrespondence()
    {
        var (_, a, _, _, _, _) = Fixtures();
        var strip = RepeatedMotif.Sequence(StructuralConstraint.Closed(a.Generate(), a.Generate()));
        var successors = (Relation)((Relation)strip.Right).Left;
        var edge = (Relation)Entries(successors)[0];
        var independent = new Relation(edge.Left, edge.Right);
        var wrong = new Relation(strip.Left, new Relation(successors, StructuralConstraint.Closed(RepeatedMotif.Join(independent))));
        Rejected(RepeatedMotif.Read(wrong));
    }

    [Fact]
    public void LookalikeInstanceCannotClaimTheOriginalGenerationWitness()
    {
        var (_, a, _, _, _, _) = Fixtures();
        var occurrence = a.Generate();
        var body = (Relation)occurrence.Right;
        var instance = (Relation)body.Left;
        var copied = new Relation(instance.Left, instance.Right);
        var falseClaim = new Relation(occurrence.Left, new Relation(copied, body.Right));
        Assert.False(RepeatedMotif.TryReadOccurrence(falseClaim, out _, out _, out _, out _, out _));
        Assert.False(StructuralConstraint.TryReadUnique(a.Recognize(StructuralConstraint.Closed(falseClaim)), out _, out _));
        Rejected(RepeatedMotif.Read(Joined(StructuralConstraint.Closed(falseClaim))));
    }

    [Fact]
    public void ExplicitRoleSeparationRejectsCollapsedLocalTopology()
    {
        var (landmarks, a, _, entrySlot, _, exitSlot) = Fixtures();
        var shared = new Placeholder();
        var collapsed = a.Generate(bindings: [new Relation(entrySlot, shared), new Relation(exitSlot, shared)]);
        var instance = (Relation)((Relation)collapsed.Right).Left;
        Assert.True(StructuralConstraint.TryReadUnique(StructuralConstraint.Match(landmarks.ETemplate,
            landmarks.Variables, StructuralConstraint.Closed(instance), landmarks.LandmarkBinding), out _, out _));
        Assert.False(RepeatedMotif.TryReadOccurrence(collapsed, out _, out _, out _, out _, out _));
        Rejected(RepeatedMotif.Read(RepeatedMotif.Sequence(StructuralConstraint.Closed(collapsed))));
        Assert.True(Contains(a.Recognize(StructuralConstraint.Closed(collapsed)), shared));
    }

    [Fact]
    public void ConflictingStableLandmarkChoiceRemainsUnresolvedAndInspectable()
    {
        var (landmarks, a, _, _, _, _) = Fixtures();
        var conflict = new Relation(landmarks.LandmarkBinding.Left, new Placeholder());
        var refused = a.Generate(bindings: [conflict]);
        Assert.False(RepeatedMotif.TryReadOccurrence(refused, out _, out _, out _, out _, out _));
        var read = RepeatedMotif.Read(RepeatedMotif.Sequence(StructuralConstraint.Closed(refused)));
        Pending(read);
        Assert.True(Contains(read, conflict));
        Assert.Same(landmarks.Interior, landmarks.LandmarkBinding.Right);
    }

    [Fact]
    public void ReverseTraversalNeedsNewSuccessorsAndCorrespondingPortJoins()
    {
        var (_, a, b, _, _, _) = Fixtures();
        var first = a.Generate(); var second = b.Generate(); var third = b.Generate();
        var forward = Joined(StructuralConstraint.Closed(first, second, third));
        var backwards = RepeatedMotif.Sequence(StructuralConstraint.Closed(third, second, first));
        var oldSeams = ((Relation)forward.Right).Right;
        var falseReverse = new Relation(backwards.Left, new Relation(((Relation)backwards.Right).Left, oldSeams));
        Rejected(RepeatedMotif.Read(falseReverse));
        Settled(RepeatedMotif.Read(Joined((Relation)backwards.Left), StructuralConstraint.Closed(b.Surface, b.Surface, a.Surface)));
        // This reverses occurrence traversal. It does not mutate or mirror
        // the ordered local motif, its frame, or any primitive dual face.
        Assert.True(RepeatedMotif.TryReadOccurrence(first, out _, out var originalTemplate, out _, out _, out _));
        Assert.Same(a.Surface, originalTemplate);
    }

    [Fact]
    public void DuplicateSuccessorProofsRemainAmbiguousRatherThanSelectingOneRoute()
    {
        var (_, a, _, _, _, _) = Fixtures();
        var first = a.Generate(); var second = a.Generate();
        var edge = new Relation(first, second); var copy = new Relation(first, second);
        var strip = new Relation(StructuralConstraint.Closed(first, second), new Relation(
            StructuralConstraint.Closed(edge, copy), StructuralConstraint.Closed(RepeatedMotif.Join(edge), RepeatedMotif.Join(copy))));
        Rejected(RepeatedMotif.Read(strip));
    }

    [Fact]
    public void IdentitySeparationIsAnExplicitLocalPromiseRatherThanAUniversalBanOnLoops()
    {
        var site = new Placeholder();
        var loop = new RepeatedMotif(new Relation(site, site), StructuralConstraint.Closed(site),
            site, site, distinctRoles: StructuralConstraint.Closed());
        var occurrence = loop.Generate();
        Assert.True(RepeatedMotif.TryReadOccurrence(occurrence, out _, out _, out _, out var entry, out var exit));
        Assert.Same(entry, exit);
        Settled(RepeatedMotif.Read(RepeatedMotif.Sequence(StructuralConstraint.Closed(occurrence))));
        NoNumbers(occurrence);
    }

    [Fact]
    public void EdgeAndSeamIntoAnOpenOccurrenceTailRemainPendingUntilTheTailIsExposed()
    {
        var (_, a, _, _, _, _) = Fixtures();
        var first = a.Generate(); var second = a.Generate(); var third = a.Generate();
        var knownEdge = new Relation(first, second); var tailEdge = new Relation(second, third);
        var seams = StructuralConstraint.Closed(RepeatedMotif.Join(knownEdge), RepeatedMotif.Join(tailEdge));
        var partial = new Relation(StructuralConstraint.Open(first, second),
            new Relation(StructuralConstraint.Open(knownEdge, tailEdge), seams));
        var read = RepeatedMotif.Read(partial);
        Pending(read);
        Assert.True(RepeatedMotif.TryReadResult(read, out _, out var blocked, out _));
        Assert.Empty(Entries(blocked));
        Assert.True(Contains(read, tailEdge));
        var complete = new Relation(StructuralConstraint.Closed(first, second, third),
            new Relation(StructuralConstraint.Closed(knownEdge, tailEdge), seams));
        Settled(RepeatedMotif.Read(complete));
    }

    [Fact]
    public void SeamCanPrecedeItsSuccessorPublicationThroughAnOpenScope()
    {
        var (_, a, _, _, _, _) = Fixtures();
        var first = a.Generate(); var second = a.Generate(); var edge = new Relation(first, second);
        var seams = StructuralConstraint.Closed(RepeatedMotif.Join(edge));
        var partial = new Relation(StructuralConstraint.Closed(first, second),
            new Relation(StructuralConstraint.Open(), seams));
        var read = RepeatedMotif.Read(partial);
        Pending(read);
        Assert.True(RepeatedMotif.TryReadResult(read, out _, out var blocked, out _));
        Assert.Empty(Entries(blocked));
        var complete = new Relation(partial.Left, new Relation(StructuralConstraint.Closed(edge), seams));
        Settled(RepeatedMotif.Read(complete));
    }

    [Fact]
    public void OpenTailDoesNotExcuseAnEdgeThatContradictsKnownAdjacency()
    {
        var (_, a, _, _, _, _) = Fixtures();
        var first = a.Generate(); var second = a.Generate(); var third = a.Generate();
        var wrong = new Relation(first, third);
        var partial = new Relation(StructuralConstraint.Open(first, second, third),
            new Relation(StructuralConstraint.Open(new Relation(first, second), new Relation(second, third), wrong),
                StructuralConstraint.Open(RepeatedMotif.Join(wrong))));
        var read = RepeatedMotif.Read(partial);
        Assert.True(RepeatedMotif.TryReadResult(read, out _, out var blocked, out var unresolved));
        Assert.NotEmpty(Entries(blocked)); Assert.NotEmpty(Entries(unresolved));
        Assert.True(Contains(blocked, wrong));
    }

    [Fact]
    public void ClosedSuccessorScopeCannotAcquireAnOrphanSeamEvenWhenOccurrencesHaveAnOpenTail()
    {
        var (_, a, _, _, _, _) = Fixtures();
        var first = a.Generate(); var second = a.Generate(); var third = a.Generate();
        var known = new Relation(first, second); var absent = new Relation(second, third);
        var orphan = RepeatedMotif.Join(absent);
        var partial = new Relation(StructuralConstraint.Open(first, second), new Relation(
            StructuralConstraint.Closed(known), StructuralConstraint.Closed(RepeatedMotif.Join(known), orphan)));
        var read = RepeatedMotif.Read(partial);
        Assert.True(RepeatedMotif.TryReadResult(read, out _, out var blocked, out var unresolved));
        Assert.NotEmpty(Entries(blocked)); Assert.NotEmpty(Entries(unresolved));
        Assert.True(Contains(blocked, orphan));
    }

    [Fact]
    public void OpenSuccessorScopeDoesNotLetACopiedRouteImpersonateThePublishedRoute()
    {
        var (_, a, _, _, _, _) = Fixtures();
        var first = a.Generate(); var second = a.Generate();
        var actual = new Relation(first, second); var copied = new Relation(first, second);
        var impostor = RepeatedMotif.Join(copied);
        var partial = new Relation(StructuralConstraint.Closed(first, second), new Relation(
            StructuralConstraint.Open(actual), StructuralConstraint.Closed(impostor)));
        var read = RepeatedMotif.Read(partial);
        Assert.True(RepeatedMotif.TryReadResult(read, out _, out var blocked, out var unresolved));
        Assert.NotEmpty(Entries(blocked)); Assert.NotEmpty(Entries(unresolved));
        Assert.True(Contains(blocked, impostor));
    }

    [Fact]
    public void OpenTailCannotMakeARepeatedOccurrenceIdentityFresh()
    {
        var (_, a, _, _, _, _) = Fixtures();
        var first = a.Generate(); var second = a.Generate();
        Assert.True(RepeatedMotif.TryReadOccurrence(first, out var identity, out _, out _, out _, out _));
        var replay = a.Generate(identity); var forbidden = new Relation(second, replay);
        var partial = new Relation(StructuralConstraint.Open(first, second), new Relation(
            StructuralConstraint.Open(new Relation(first, second), forbidden), StructuralConstraint.Open()));
        var read = RepeatedMotif.Read(partial);
        Assert.True(RepeatedMotif.TryReadResult(read, out _, out var blocked, out var unresolved));
        Assert.NotEmpty(Entries(blocked)); Assert.NotEmpty(Entries(unresolved));
        Assert.True(Contains(blocked, forbidden));
    }

    [Fact]
    public void DuplicateRoutesIntoAnUnknownTailAlreadyContradictUniqueSuccession()
    {
        var (_, a, _, _, _, _) = Fixtures();
        var first = a.Generate(); var second = a.Generate();
        var edge = new Relation(first, second); var copy = new Relation(first, second);
        var partial = new Relation(StructuralConstraint.Open(first), new Relation(
            StructuralConstraint.Open(edge, copy), StructuralConstraint.Open()));
        var read = RepeatedMotif.Read(partial);
        Assert.True(RepeatedMotif.TryReadResult(read, out _, out var blocked, out var unresolved));
        Assert.NotEmpty(Entries(blocked)); Assert.NotEmpty(Entries(unresolved));
        Assert.True(Contains(blocked, edge));
    }

    [Fact]
    public void OrdinaryFiniteLawWitnessDoesNotImpersonateAStripRead()
    {
        var a = new Placeholder(); var b = new Placeholder(); var result = new Placeholder();
        var law = FiniteLaw.Create(StructuralConstraint.Closed(a), StructuralConstraint.Closed(b),
            StructuralConstraint.Closed(result), FiniteLaw.Row(a, b, StructuralConstraint.Closed(result)));
        var read = FiniteLaw.Evaluate(law, StructuralConstraint.Closed(a), StructuralConstraint.Closed(b));
        Assert.True(FiniteLaw.TryReadResult(read, out _, out _, out _));
        Assert.False(RepeatedMotif.TryReadResult(read, out _, out _, out _));
    }

    [Fact]
    public void MalformedStripRequestRemainsInspectableWithoutPassingTheExactResultReader()
    {
        var malformed = new Relation(new Placeholder(), new Placeholder());
        var read = RepeatedMotif.Read(malformed);
        Assert.False(RepeatedMotif.TryReadResult(read, out _, out _, out _));
        Assert.True(Contains(read, malformed));
        var pending = Assert.IsType<Relation>(Assert.IsType<Relation>(Assert.IsType<Relation>(read.Right).Right).Right);
        Assert.NotEmpty(Entries(pending));
    }

    private static (StructuralLandmarkFixture Landmarks, RepeatedMotif A, RepeatedMotif B,
        Placeholder Entry, Placeholder Interior, Placeholder Exit) Fixtures()
    {
        var landmarks = new StructuralLandmarkFixture();
        var placement = (Relation)((Relation)landmarks.ETemplate.Right).Left;
        var maps = (Relation)((Relation)placement.Right).Left;
        var entry = (Placeholder)((Relation)maps.Left).Right;
        var rest = (Relation)maps.Right;
        var interior = (Placeholder)((Relation)rest.Left).Right;
        var exit = (Placeholder)((Relation)rest.Right).Right;
        var stable = StructuralConstraint.Closed(landmarks.LandmarkBinding);
        var distinct = StructuralConstraint.Closed(entry, interior, exit);
        var a = new RepeatedMotif(landmarks.ETemplate, landmarks.Variables, entry, exit, stable, distinct);
        var b = new RepeatedMotif(landmarks.HTemplate, landmarks.Variables, entry, exit, stable, distinct);
        return (landmarks, a, b, entry, interior, exit);
    }

    private static Relation Joined(Relation occurrences)
    {
        var strip = RepeatedMotif.Sequence(occurrences);
        var successors = (Relation)((Relation)strip.Right).Left;
        var joins = Entries(successors).Cast<Relation>().Select(RepeatedMotif.Join).Cast<IElement>().ToArray();
        return new(occurrences, new Relation(successors, StructuralConstraint.Closed(joins)));
    }

    private static void Settled(Relation read)
    {
        Assert.True(RepeatedMotif.TryReadResult(read, out var passed, out var blocked, out var unresolved));
        Assert.NotEmpty(Entries(passed)); Assert.Empty(Entries(blocked)); Assert.Empty(Entries(unresolved));
    }

    private static void Rejected(Relation read)
    {
        Assert.True(RepeatedMotif.TryReadResult(read, out _, out var blocked, out var unresolved));
        Assert.NotEmpty(Entries(blocked)); Assert.Empty(Entries(unresolved));
    }

    private static void Pending(Relation read)
    {
        Assert.True(RepeatedMotif.TryReadResult(read, out _, out _, out var unresolved));
        Assert.NotEmpty(Entries(unresolved));
    }

    private static IElement[] Entries(Relation scope)
    {
        Assert.True(StructuralConstraint.TryReadScope(scope, out var entries, out _, out _)); return entries;
    }

    private static bool Contains(IElement root, IElement target) => ReferenceEquals(root, target) ||
        root is Relation relation && (Contains(relation.Left, target) || Contains(relation.Right, target));

    private static void NoNumbers(IElement root)
    {
        Assert.IsNotType<Carrier>(root); Assert.IsNotType<CarrierLocation>(root);
        if (root is not Relation relation) return;
        NoNumbers(relation.Left); NoNumbers(relation.Right);
    }
}
