using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class AdaptiveWallpaperTests
{
    [Theory]
    [InlineData("cushion")]
    [InlineData("key")]
    [InlineData("rosette")]
    public void AuthoredMotifsExposeActualPortOccurrencesAndJustifiedUnmeasuredClosure(string family)
    {
        var grammar = new AdaptiveMotifGrammar(new QualitativeMotionFrame(), new Placeholder(), family);
        foreach (var template in new[] { grammar.Plus, grammar.Minus })
        {
            var paths = E(template.Paths);
            Assert.NotEmpty(paths);
            foreach (var role in new[] { grammar.East, grammar.North, grammar.West, grammar.South })
            {
                var port = Assert.Single(E(template.Ports).Cast<Relation>(), row => ReferenceEquals(row.Left, role));
                var selection = Assert.IsType<Relation>(port.Right);
                var path = Assert.IsType<Relation>(selection.Left);
                Assert.Contains(paths, actual => ReferenceEquals(actual, path));
                Assert.Contains(E(Assert.IsType<Relation>(path.Right)), chord => ReferenceEquals(chord, selection.Right));
            }
            foreach (var path in paths)
            {
                var check = Assert.Single(E(grammar.ClosureChecks).Cast<Relation>(), row => ReferenceEquals(row.Left, path));
                Assert.True(grammar.Frame.TryReadResult(Assert.IsType<Relation>(check.Right), out var rest, out _, out var unresolved));
                Assert.Empty(E(rest)); Assert.Empty(E(unresolved));
            }
        }
        Assert.DoesNotContain(Traverse(grammar.Surface), item => item is Carrier or CarrierLocation);
    }

    [Fact]
    public void NeighbourObservationRepairsAMotifWithoutLettingItsPressureCrossInTheSameRound()
    {
        var world = Pair(observeTarget: true); var packet = new Placeholder();
        var original = world.Right.Current;
        Assert.False(world.Machine.TryReadAccepted(world.Machine.Negotiate(world.Link), out _));
        world.Machine.Seed(world.Link, packet);
        world.Machine.Round();
        Assert.Same(world.Grammar.Minus, world.Right.Current.Template); Assert.NotSame(original, world.Right.Current);
        Assert.Same(world.Link.QueuePort, Owner(world.Machine, packet));
        Assert.Empty(E(world.Machine.Deliveries));
        Assert.True(world.Machine.TryReadAccepted(Assert.IsType<Relation>(Assert.Single(E(world.Machine.CurrentSeams))), out var accepted));
        Assert.Same(world.Link, accepted);
        world.Machine.Round();
        Assert.Same(world.Link.TargetPort, Owner(world.Machine, packet));
        Assert.Single(E(world.Machine.Deliveries)); Assert.Single(E(world.Right.Replacements));
        Assert.Empty(E(world.Machine.CurrentUnknown));
        AssertAccounted(world.Machine, packet);
        Assert.DoesNotContain(Traverse(new Relation(world.Machine.Surface,
            new Relation(world.Machine.Timeline, world.Machine.Transport.Account()))), item => item is Carrier or CarrierLocation);
    }

    [Fact]
    public void ABlockedQueueKeepsItsActualPacketAcrossMotifReplacementAndReopensWithoutReimport()
    {
        var world = Pair(); var packet = new Placeholder();
        world.Machine.Seed(world.Link, packet); world.Machine.Round();
        var blocked = world.Machine.Negotiate(world.Link); var waiting = world.Machine.Transport.State;
        Assert.False(world.Machine.Deliver(world.Link, blocked));
        Assert.Same(waiting, world.Machine.Transport.State); Assert.NotEmpty(E(world.Machine.Blocked));
        Assert.Same(world.Link.QueuePort, Owner(world.Machine, packet));
        Replace(world.Right, world.Grammar.Minus);
        Assert.Same(waiting, world.Machine.Transport.State);
        var reopened = world.Machine.Negotiate(world.Link);
        Assert.True(world.Machine.TryReadAccepted(reopened, out var link)); Assert.Same(world.Link, link);
        Assert.False(world.Machine.Deliver(world.Link, blocked));
        Assert.Same(waiting, world.Machine.Transport.State);
        Assert.True(world.Machine.Deliver(world.Link, reopened));
        Assert.Same(world.Link.TargetPort, Owner(world.Machine, packet));
        var delivered = world.Machine.Transport.State;
        Assert.False(world.Machine.Deliver(world.Link, reopened)); Assert.Same(delivered, world.Machine.Transport.State);
        Replace(world.Right, world.Grammar.Plus);
        Assert.False(world.Machine.TryReadAccepted(reopened, out _));
        Assert.Same(delivered, world.Machine.Transport.State);
        Assert.Same(world.Link.TargetPort, Owner(world.Machine, packet));
        Assert.Single(E(world.Machine.Deliveries)); AssertAccounted(world.Machine, packet);
    }

    [Fact]
    public void FormerlyAcceptedSeamsAndCopiedAgreementsCannotAuthorizeCurrentDelivery()
    {
        var world = Pair(matched: true); var packet = new Placeholder();
        var accepted = world.Machine.Negotiate(world.Link);
        Assert.True(world.Machine.TryReadAccepted(accepted, out _));
        var copy = new Relation(accepted.Left, accepted.Right);
        Assert.False(world.Machine.TryReadAccepted(copy, out _));
        world.Machine.Seed(world.Link, packet); world.Machine.Round();
        var queued = world.Machine.Transport.State;
        Assert.False(world.Machine.Deliver(world.Link, copy)); Assert.Same(queued, world.Machine.Transport.State);
        Replace(world.Right, world.Grammar.Plus);
        Assert.False(world.Machine.TryReadAccepted(accepted, out _));
        Assert.False(world.Machine.Deliver(world.Link, accepted)); Assert.Same(queued, world.Machine.Transport.State);
        Replace(world.Right, world.Grammar.Minus);
        // Equal geometry after another revision does not revive an old head's permission.
        Assert.False(world.Machine.TryReadAccepted(accepted, out _));
        var current = world.Machine.Negotiate(world.Link);
        Assert.True(world.Machine.Deliver(world.Link, current)); AssertAccounted(world.Machine, packet);
    }

    [Fact]
    public void ActiveReplacementPreservesHistoricalReceiptsPortsAndInkWithoutLeavingThemInTheActiveField()
    {
        var world = Pair(); var old = world.Right.Current;
        var oldStrokes = E(old.Graph.Strokes); var oldReceipts = E(old.Receipts);
        Assert.True(old.TryReadPort(world.Grammar.West, out var oldPort, out var oldBinding));
        var replacement = Replace(world.Right, world.Grammar.Minus);
        var current = world.Right.Current;
        Assert.Same(old.Surface, Assert.IsType<Relation>(Assert.IsType<Relation>(replacement.Right).Right).Left);
        Assert.NotSame(old.Surface, current.Surface); Assert.NotSame(old.After, current.After);
        Assert.Same(old.After, current.Before);
        Assert.Equal(2, E(world.Right.Revisions).Length);
        Assert.True(old.TryReadPort(world.Grammar.West, out var retainedPort, out var retainedBinding));
        Assert.Same(oldPort, retainedPort); Assert.Same(oldBinding, retainedBinding);
        Assert.True(current.TryReadPort(world.Grammar.West, out var newPort, out _)); Assert.NotSame(oldPort, newPort);
        Assert.True(old.Join.TryReadPassed(old.Join.Evaluate()));
        Assert.Equal(oldStrokes, E(old.Graph.Strokes)); Assert.Equal(oldReceipts, E(old.Receipts));
        foreach (var receipt in oldReceipts.Cast<Relation>()) Assert.True(world.Right.Drawing.TryReadAccepted(receipt, out _, out _));
        foreach (var stroke in oldStrokes) Assert.Contains(E(world.Right.Drawing.Strokes), retained => ReferenceEquals(retained, stroke));
        var active = world.Machine.Field();
        foreach (var stroke in oldStrokes) Assert.DoesNotContain(E(active.Source.Strokes), present => ReferenceEquals(present, stroke));
        foreach (var stroke in E(current.Graph.Strokes)) Assert.Contains(E(active.Source.Strokes), present => ReferenceEquals(present, stroke));
        Assert.Empty(E(world.Machine.Transport.ImportedPackets));
    }

    [Fact]
    public void CopiedForeignAndReplayedRevisionRequestsCannotReplaceTheActiveHead()
    {
        var world = Pair(); var old = world.Right.Current;
        var proposal = world.Right.Propose(Question(world.Grammar.Minus.Surface));
        var copy = new Relation(proposal.Left, proposal.Right);
        world.Right.Commit(copy); Assert.Same(old, world.Right.Current);
        var foreign = world.Left.Propose(Question(world.Grammar.Minus.Surface));
        world.Right.Commit(foreign); Assert.Same(old, world.Right.Current);
        Assert.Same(proposal, Assert.Single(E(world.Right.Pending)));
        world.Right.Commit(proposal); var after = world.Right.Current;
        Assert.Same(world.Grammar.Minus, after.Template); Assert.Single(E(world.Right.Replacements));
        world.Right.Commit(proposal); Assert.Same(after, world.Right.Current); Assert.Single(E(world.Right.Replacements));
        Assert.NotEmpty(E(world.Right.Unresolved));
    }

    [Fact]
    public void ARevisionProposalCannotOverwriteAnInterveningLiveDrawingHeadOrReleaseItsPacket()
    {
        var world = Pair(); var packet = new Placeholder();
        world.Machine.Seed(world.Link, packet); world.Machine.Round();
        var motif = world.Right.Current; var material = world.Machine.Transport.State;
        var proposal = world.Right.Propose(Question(world.Grammar.Minus.Surface));
        var extra = world.Right.Drawing.Apply(new Placeholder(),
            S(world.Right.Drawing.Draw(Word(world.Grammar, world.Grammar.Frame.Up, 1))), new Placeholder());
        Assert.True(world.Right.Drawing.TryReadAccepted(extra, out var live, out _));
        Assert.Same(live, world.Right.Drawing.State);
        world.Right.Commit(proposal);
        Assert.Same(motif, world.Right.Current); Assert.Same(live, world.Right.Drawing.State);
        Assert.Same(material, world.Machine.Transport.State); Assert.Same(world.Link.QueuePort, Owner(world.Machine, packet));
        Assert.Same(proposal, Assert.Single(E(world.Right.Pending)));
        Assert.Empty(E(world.Right.Replacements)); Assert.NotEmpty(E(world.Right.Unresolved));
        var timeline = world.Machine.Timeline; world.Machine.Round(); Assert.Same(timeline, world.Machine.Timeline);
        AssertAccounted(world.Machine, packet);
    }

    [Fact]
    public void AllLocalProposalsUseTheSameBeforeRoundHeadsRegardlessOfSchedule()
    {
        var forward = Pair(reciprocal: true); var reversed = Pair(reciprocal: true);
        var a = new Placeholder(); var b = new Placeholder();
        forward.Machine.Seed(forward.Link, a); reversed.Machine.Seed(reversed.Link, b);
        for (var round = 0; round < 3; round++)
        {
            forward.Machine.Round(); reversed.Machine.Round(S(reversed.Right.Cell, reversed.Left.Cell));
            var firstExpected = round % 2 == 0 ? forward.Grammar.Minus : forward.Grammar.Plus;
            var secondExpected = round % 2 == 0 ? reversed.Grammar.Minus : reversed.Grammar.Plus;
            Assert.Same(firstExpected, forward.Left.Current.Template); Assert.Same(firstExpected, forward.Right.Current.Template);
            Assert.Same(secondExpected, reversed.Left.Current.Template); Assert.Same(secondExpected, reversed.Right.Current.Template);
            Assert.Equal(E(forward.Left.Replacements).Length, E(reversed.Left.Replacements).Length);
            Assert.Equal(E(forward.Right.Replacements).Length, E(reversed.Right.Replacements).Length);
            Assert.Same(forward.Link.QueuePort, Owner(forward.Machine, a)); Assert.Same(reversed.Link.QueuePort, Owner(reversed.Machine, b));
            AssertAccounted(forward.Machine, a); AssertAccounted(reversed.Machine, b);
        }
        Assert.Empty(E(forward.Machine.Deliveries)); Assert.Empty(E(reversed.Machine.Deliveries));
    }

    [Fact]
    public void FrozenLocalPolicyKeepsAnUnmatchedNeighbourConnectionAndItsPressureVisible()
    {
        var world = Pair(observeTarget: true, frozenTarget: true); var packet = new Placeholder();
        var original = world.Right.Current;
        world.Machine.Seed(world.Link, packet); world.Machine.Round(); world.Machine.Round();
        Assert.Same(original, world.Right.Current); Assert.Empty(E(world.Right.Replacements));
        Assert.False(world.Machine.TryReadAccepted(Assert.IsType<Relation>(Assert.Single(E(world.Machine.CurrentSeams))), out _));
        Assert.Same(world.Link.QueuePort, Owner(world.Machine, packet));
        Assert.NotEmpty(E(world.Machine.Blocked)); Assert.Empty(E(world.Machine.Deliveries));
        Assert.Empty(E(world.Machine.CurrentUnknown)); AssertAccounted(world.Machine, packet);
    }

    [Fact]
    public void ContradictoryNeighboursRemainAnUnansweredQuestionRatherThanFirstBindingWins()
    {
        var grammar = new AdaptiveMotifGrammar(new QualitativeMotionFrame(), new Placeholder(), "cushion");
        var left = new AdaptiveMotifNode("left", grammar, Word(grammar, grammar.Frame.Left, 8), grammar.Plus);
        var middle = new AdaptiveMotifNode("middle", grammar, S(), grammar.Plus);
        var right = new AdaptiveMotifNode("right", grammar, Word(grammar, grammar.Frame.Right, 8), grammar.Minus);
        var incoming = new AdaptiveMotifLink(left, middle, grammar.East, grammar.West);
        var outgoing = new AdaptiveMotifLink(middle, right, grammar.East, grammar.West);
        var machine = new AdaptiveWallpaperMachine("contradictory neighbours", grammar, new[] { left, middle, right },
            new[] { incoming, outgoing }, S(new Relation(middle.Cell, incoming.Surface), new Relation(middle.Cell, outgoing.Surface)));
        Assert.True(machine.IsValid); var original = middle.Current; var packet = new Placeholder(); machine.Seed(incoming, packet);
        machine.Round(); machine.Round();
        Assert.Same(original, middle.Current); Assert.Empty(E(middle.Replacements)); Assert.Empty(E(middle.Pending));
        var unknown = Assert.IsType<Relation>(Assert.Single(E(machine.CurrentUnknown))); Assert.Same(middle.Cell, unknown.Left);
        var question = Assert.IsType<Relation>(unknown.Right);
        Assert.False(QualitativeQuestion.TryReadAnswer(question, out _));
        Assert.True(QualitativeQuestion.TryRead(question, out var alternatives, out var requests, out _));
        Assert.Equal(2, E(alternatives).Length); Assert.NotEmpty(E(requests));
        Assert.Contains(E(alternatives), option => ReferenceEquals(option, grammar.Plus.Surface));
        Assert.Contains(E(alternatives), option => ReferenceEquals(option, grammar.Minus.Surface));
        Assert.Same(incoming.QueuePort, Owner(machine, packet)); Assert.NotEmpty(E(machine.Blocked)); AssertAccounted(machine, packet);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DuplicateNodeOrLinkDefinitionsRemainVisibleInvalidWorlds(bool duplicateNode)
    {
        var original = Pair();
        var nodes = duplicateNode ? new[] { original.Left, original.Left, original.Right } : new[] { original.Left, original.Right };
        var links = duplicateNode ? new[] { original.Link } : new[] { original.Link, original.Link };
        var invalid = new AdaptiveWallpaperMachine("duplicate", original.Grammar, nodes, links,
            S(new Relation(original.Right.Cell, original.Link.Surface)));
        Assert.False(invalid.IsValid); Assert.NotEmpty(E(invalid.DefinitionProblems));
        invalid.Seed(original.Link, new Placeholder()); invalid.Round();
        Assert.Empty(E(invalid.Transport.ImportedPackets)); Assert.Empty(E(invalid.Timeline));
    }

    [Fact]
    public void AssessmentRetainsEveryRequiredSeamAndItsKnownBlockedWitnessAsAViolation()
    {
        var world = Pair(); var packet = new Placeholder();
        world.Machine.Seed(world.Link, packet); world.Machine.Round(); world.Machine.Round();
        var assessment = new AdaptiveWallpaperAssessment(world.Machine);
        Assert.Equal(new PatternAestheticProfile.Summary(0, 2, 1, 0, true, false), assessment.Summary);
        Assert.Same(world.Link.Surface, Assert.Single(E(assessment.RequiredSeams)));
        var seam = Assert.IsType<Relation>(Assert.Single(E(assessment.BlockedSeams)));
        Assert.Same(Assert.Single(E(assessment.CurrentSeams)), seam);
        var check = Assert.IsType<Relation>(Assert.Single(E(assessment.SeamChecks)));
        Assert.Same(world.Link.Surface, check.Left); Assert.Same(seam, check.Right);
        Assert.Empty(E(assessment.AcceptedSeams)); Assert.Empty(E(assessment.UnresolvedSeams));
        Assert.True(assessment.Profile.TryReadResult(assessment.Read, out _, out _, out var violations, out var unknown));
        Assert.Same(assessment.Features[1].Criterion, Assert.IsType<Relation>(Assert.Single(E(violations))).Left);
        Assert.Empty(E(unknown)); Assert.Same(world.Link.QueuePort, Owner(world.Machine, packet));
        AssertAccounted(world.Machine, packet);
        Assert.DoesNotContain(Traverse(assessment.Surface), item => item is Carrier or CarrierLocation);
        Replace(world.Right, world.Grammar.Minus); world.Machine.Negotiate(world.Link);
        var repaired = new AdaptiveWallpaperAssessment(world.Machine);
        Assert.Equal(new PatternAestheticProfile.Summary(0, 3, 0, 0, true, true), repaired.Summary);
        Assert.NotEmpty(E(world.Machine.Blocked));
        Assert.Equal(new PatternAestheticProfile.Summary(0, 2, 1, 0, true, false), assessment.Profile.Describe(assessment.Read));
    }

    [Fact]
    public void AssessmentDoesNotTreatNoJunctionObligationsAsAProvenConnectedField()
    {
        var world = Pair();
        var unconnected = new AdaptiveWallpaperMachine("no declared obligations", world.Grammar,
            new[] { world.Left, world.Right }, Array.Empty<AdaptiveMotifLink>(), S());
        var assessment = new AdaptiveWallpaperAssessment(unconnected);
        Assert.Equal(new PatternAestheticProfile.Summary(0, 2, 0, 1, false, false), assessment.Summary);
        Assert.Empty(E(assessment.RequiredSeams)); Assert.Empty(E(assessment.SeamChecks));
        Assert.Empty(E(assessment.AcceptedSeams)); Assert.Empty(E(assessment.BlockedSeams));
        Assert.True(assessment.Profile.TryReadResult(assessment.Read, out _, out _, out var violations, out var unknown));
        Assert.Empty(E(violations));
        Assert.Same(assessment.Features[1].Criterion, Assert.IsType<Relation>(Assert.Single(E(unknown))).Left);
        var connected = Pair(matched: true); var proven = new AdaptiveWallpaperAssessment(connected.Machine);
        Assert.Equal(new PatternAestheticProfile.Summary(0, 3, 0, 0, true, true), proven.Summary);
        Assert.Single(E(proven.RequiredSeams)); Assert.Single(E(proven.AcceptedSeams));
    }

    [Fact]
    public void AssessmentCapturesItsActualHeadsAndKeepsStaleJunctionObligationsUnknownUntilReread()
    {
        var world = Pair(matched: true); var before = new AdaptiveWallpaperAssessment(world.Machine);
        var oldHeads = before.Snapshot; var oldSeams = before.CurrentSeams; var oldSummary = before.Summary;
        Assert.True(oldSummary.Acceptable);
        Replace(world.Right, world.Grammar.Plus);
        var stale = new AdaptiveWallpaperAssessment(world.Machine);
        Assert.Equal(new PatternAestheticProfile.Summary(0, 2, 0, 1, false, false), stale.Summary);
        Assert.Single(E(stale.RequiredSeams)); Assert.Single(E(stale.UnresolvedSeams)); Assert.Empty(E(stale.AcceptedSeams));
        var missing = Assert.IsType<Relation>(Assert.Single(E(stale.UnresolvedSeams)));
        Assert.Same(world.Link.Surface, missing.Left);
        Assert.Same(oldHeads, before.Snapshot); Assert.Same(oldSeams, before.CurrentSeams);
        Assert.Equal(oldSummary, before.Profile.Describe(before.Read));
        world.Machine.Negotiate(world.Link);
        var current = new AdaptiveWallpaperAssessment(world.Machine);
        Assert.Equal(new PatternAestheticProfile.Summary(0, 2, 1, 0, true, false), current.Summary);
        Assert.Single(E(current.BlockedSeams)); Assert.Empty(E(current.UnresolvedSeams));
        Assert.Equal(oldSummary, before.Profile.Describe(before.Read));
        Assert.NotSame(Assert.IsType<Relation>(E(before.Snapshot)[1]).Right,
            Assert.IsType<Relation>(E(current.Snapshot)[1]).Right);
    }

    [Fact]
    public void AssessmentKeepsAPendingRevisionDistinctFromMaterialConservationAndCompletedJunctions()
    {
        var world = Pair(matched: true); var packet = new Placeholder();
        world.Machine.Seed(world.Link, packet); world.Machine.Round();
        world.Right.Propose(Question(world.Grammar.Plus.Surface));
        var assessment = new AdaptiveWallpaperAssessment(world.Machine);
        Assert.Equal(new PatternAestheticProfile.Summary(0, 2, 1, 0, true, false), assessment.Summary);
        Assert.Single(E(assessment.AcceptedSeams)); Assert.Empty(E(assessment.BlockedSeams));
        Assert.True(assessment.Profile.TryReadResult(assessment.Read, out _, out _, out var violations, out _));
        Assert.Same(assessment.Features[0].Criterion, Assert.IsType<Relation>(Assert.Single(E(violations))).Left);
        Assert.Same(world.Machine.Transport.State, assessment.TransportState);
        Assert.Same(world.Link.QueuePort, Owner(world.Machine, packet)); AssertAccounted(world.Machine, packet);
    }

    private sealed record PairWorld(AdaptiveMotifGrammar Grammar, AdaptiveMotifNode Left,
        AdaptiveMotifNode Right, AdaptiveMotifLink Link, AdaptiveWallpaperMachine Machine);

    private static PairWorld Pair(bool observeTarget = false, bool frozenTarget = false, bool matched = false, bool reciprocal = false)
    {
        var grammar = new AdaptiveMotifGrammar(new QualitativeMotionFrame(), new Placeholder(), "cushion");
        var left = new AdaptiveMotifNode("left", grammar, S(), grammar.Plus);
        var right = new AdaptiveMotifNode("right", grammar, Word(grammar, grammar.Frame.Right, 8), matched ? grammar.Minus : grammar.Plus, frozenTarget);
        var link = new AdaptiveMotifLink(left, right, grammar.East, grammar.West);
        var observations = new List<IElement>();
        if (observeTarget || reciprocal) observations.Add(new Relation(right.Cell, link.Surface));
        if (reciprocal) observations.Add(new Relation(left.Cell, link.Surface));
        var machine = new AdaptiveWallpaperMachine("pair", grammar, new[] { left, right }, new[] { link }, S(observations.ToArray()));
        Assert.True(machine.IsValid); return new(grammar, left, right, link, machine);
    }

    private static Relation Replace(AdaptiveMotifNode node, AdaptiveMotifGrammar.Template template)
    {
        var proposal = node.Propose(Question(template.Surface));
        Assert.Same(proposal, Assert.Single(E(node.Pending)));
        var witness = node.Commit(proposal);
        Assert.Same(template, node.Current.Template); Assert.Empty(E(node.Pending)); return witness;
    }

    private static Relation Question(IElement selected)
    {
        var input = new Placeholder(); var law = FiniteLaw.Create(S(input), S(input), S(selected), FiniteLaw.Row(input, input, S(selected)));
        return QualitativeQuestion.Ask(FiniteLaw.Evaluate(law, S(input), S(input)), S(new Relation(selected, selected)), new Placeholder());
    }

    private static Relation Word(AdaptiveMotifGrammar grammar, IElement direction, int repetitions) =>
        S(Enumerable.Range(0, repetitions).Select(_ => (IElement)QualitativeMotionFrame.Move(direction, grammar.Reach)).ToArray());

    private static IElement Owner(AdaptiveWallpaperMachine machine, IElement packet)
    {
        Assert.True(machine.Transport.TryReadState(machine.Transport.State, out _, out var placements, out _, out _));
        return Assert.IsType<Relation>(Assert.Single(E(placements).Cast<Relation>(), row => ReferenceEquals(row.Left, packet)).Right).Left;
    }

    private static void AssertAccounted(AdaptiveWallpaperMachine machine, params IElement[] packets)
    {
        Assert.True(machine.Transport.TryReadState(machine.Transport.State, out _, out var placements, out var imported, out _));
        Assert.Equal(packets.Length, E(placements).Length); Assert.Equal(packets.Length, E(imported).Length);
        foreach (var packet in packets)
        {
            Assert.Single(E(imported), actual => ReferenceEquals(actual, packet));
            Assert.Single(E(placements).Cast<Relation>(), row => ReferenceEquals(row.Left, packet));
        }
        var account = machine.Transport.Account(); var result = Assert.IsType<Relation>(account.Right);
        Assert.Same(placements, result.Left); Assert.Empty(E(Assert.IsType<Relation>(result.Right)));
    }

    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var current))
        {
            if (!seen.Add(current)) continue;
            yield return current;
            if (current is Relation row) { pending.Push(row.Right); pending.Push(row.Left); }
        }
    }
}
