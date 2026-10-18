using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class OrnamentWordLandmarksTests
{
    [Fact]
    public void SharingCountsRepeatedOccurrencesWithoutCopyingOrDrainingInputParticipation()
    {
        var frame = new QualitativeMotionFrame(); var reach = new Placeholder();
        var move = QualitativeMotionFrame.Move(frame.Right, reach);
        var first = Bind(S(move, move)); var second = Bind(S(move, move));
        var inputs = S(first, second); var slots = Slots(2);
        var read = OrnamentWordLandmarks.Share(frame, inputs, slots);
        Assert.True(OrnamentWordLandmarks.TryReadComplete(frame, read, out var word));
        Assert.Equal(2, E(word).Length); Assert.Equal(2, E((Relation)first.Right).Length); Assert.Equal(2, E((Relation)second.Right).Length);
        Parts(read, out _, out var groups, out var remainder, out _); Assert.Empty(E(remainder));
        Assert.Equal(2, E(groups).Length);
        foreach (var group in E(groups).Cast<Relation>())
        {
            var grouping = Assert.IsType<Relation>(group.Left); var actual = Assert.IsType<Relation>(grouping.Left);
            Assert.Same(slots, grouping.Right); Assert.Equal(2, E(actual).Length); Assert.All(E(actual), item => Assert.Same(move, item));
        }
        AssertNoNumbers(read);
    }

    [Fact]
    public void OpposedParticipationCancelsBeforeSelectedEqualSharingAndRemainsInSummationEvidence()
    {
        var frame = new QualitativeMotionFrame(); var reach = new Placeholder();
        Relation M(IElement direction) => QualitativeMotionFrame.Move(direction, reach);
        var right = M(frame.Right); var left = M(frame.Left); var up = M(frame.Up);
        var bindings = S(Bind(S(right, right, right, up)), Bind(S(left, up)));
        var read = OrnamentWordLandmarks.Share(frame, bindings, Slots(2));
        Assert.True(OrnamentWordLandmarks.TryReadComplete(frame, read, out var word)); Assert.Equal(2, E(word).Length);
        Assert.Single(E(word).Cast<Relation>().Where(term => ReferenceEquals(term.Left, frame.Right)));
        Assert.Single(E(word).Cast<Relation>().Where(term => ReferenceEquals(term.Left, frame.Up)));
        Parts(read, out _, out _, out _, out var evidence);
        Assert.Contains(left, Reach(Assert.Single(E(evidence)))); Assert.Contains(bindings, Reach(read));
        AssertNoNumbers(read);
    }

    [Fact]
    public void IndivisibleOccurrenceRemainsExplicitAndThePartialWordCannotClaimCompleteLandmark()
    {
        var frame = new QualitativeMotionFrame(); var reach = new Placeholder(); var right = QualitativeMotionFrame.Move(frame.Right, reach);
        var read = OrnamentWordLandmarks.Share(frame, S(Bind(S(right, right, right)), Bind(S())), Slots(2));
        Assert.False(OrnamentWordLandmarks.TryReadComplete(frame, read, out _));
        Parts(read, out var result, out var groups, out var remainder, out _);
        Assert.Single(E(result)); Assert.Single(E(groups)); Assert.Same(right, Assert.Single(E(remainder)));
        Assert.True(StructuralConstraint.TryReadScope(result, out _, out var seam, out var tail)); Assert.False(seam.Equals(tail));
    }

    [Fact]
    public void DifferentUncalibratedReachIdentitiesCannotBeCombinedToPayOneShareGroup()
    {
        var frame = new QualitativeMotionFrame();
        var first = QualitativeMotionFrame.Move(frame.Right, new Placeholder());
        var second = QualitativeMotionFrame.Move(frame.Right, new Placeholder());
        var read = OrnamentWordLandmarks.Share(frame, S(Bind(S(first)), Bind(S(second))), Slots(2));
        Assert.False(OrnamentWordLandmarks.TryReadComplete(frame, read, out _));
        Parts(read, out var result, out var groups, out var remainder, out _);
        Assert.Empty(E(result)); Assert.Empty(E(groups)); Assert.Equal(2, E(remainder).Length);
        Assert.Same(first, E(remainder)[0]); Assert.Same(second, E(remainder)[1]);
    }

    [Fact]
    public void OpenInputsOpenSharingAndDuplicateSlotIdentityCannotEstablishCompleteRead()
    {
        var frame = new QualitativeMotionFrame(); var reach = new Placeholder(); var right = QualitativeMotionFrame.Move(frame.Right, reach);
        var a = Bind(S(right, right)); var b = Bind(S(right, right)); var slot = new Placeholder();
        var cases = new[]
        {
            (StructuralConstraint.Open(a, b), Slots(2)),
            (S(Bind(StructuralConstraint.Open(right, right)), b), Slots(2)),
            (S(a, b), StructuralConstraint.Open(new Placeholder(), new Placeholder())),
            (S(a, b), S(slot, slot.Dual().Dual())),
            (S(a, b), S()),
            (S(a, b), Slots(3))
        };
        foreach (var (bindings, slots) in cases)
            Assert.False(OrnamentWordLandmarks.TryReadComplete(frame, OrnamentWordLandmarks.Share(frame, bindings, slots), out _));
        var complete = OrnamentWordLandmarks.Share(frame, S(a, b), Slots(2));
        Assert.False(OrnamentWordLandmarks.TryReadComplete(new QualitativeMotionFrame(), complete, out _));
    }

    [Fact]
    public void ASharingWitnessCannotSpendOneInputOccurrenceGroupTwice()
    {
        var frame = new QualitativeMotionFrame(); var reach = new Placeholder();
        Relation R() => QualitativeMotionFrame.Move(frame.Right, reach);
        var read = OrnamentWordLandmarks.Share(frame, S(Bind(S(R(), R())), Bind(S(R(), R()))), Slots(2));
        Assert.True(OrnamentWordLandmarks.TryReadComplete(frame, read, out _));
        Parts(read, out var result, out var groups, out var remainder, out var evidence);
        var actual = E(groups).Cast<Relation>().ToArray();
        var forgedGroups = S(actual[0], new Relation(actual[0].Left, actual[1].Right));
        var forged = new Relation(read.Left, new Relation(result, new Relation(forgedGroups, new Relation(remainder, evidence))));
        Assert.False(OrnamentWordLandmarks.TryReadComplete(frame, forged, out _));
    }

    [Fact]
    public void FacePublicationIsOptionalObservationAndAnOpenSupplierCannotHideItsCompetingTail()
    {
        var source = OrnamentSourcePatch.TriangleGrid(1, 1); var before = source.Scaffold.Evaluate();
        var supplier = new OrnamentWordLandmarks(source.Scaffold, source.Bindings);
        Assert.Equal(2, E(supplier.Publications).Length); Assert.Empty(E(supplier.Unresolved));
        foreach (var face in E(source.Scaffold.Faces))
        {
            Assert.True(supplier.TryRead(face, out var binding, out var read));
            Assert.True(OrnamentWordLandmarks.TryReadComplete(source.Frame, read, out var word)); Assert.Same(word, binding.Right);
            Assert.DoesNotContain(E(source.Scaffold.Graph.Sites), site => ReferenceEquals(site, binding.Left));
        }
        var open = new OrnamentWordLandmarks(source.Scaffold, StructuralConstraint.Open(E(source.Bindings)));
        Assert.Empty(E(open.Publications)); Assert.NotEmpty(E(open.Unresolved));
        foreach (var face in E(source.Scaffold.Faces)) Assert.False(open.TryRead(face, out _, out _));
        Assert.Same(before, source.Scaffold.Evaluate()); Assert.Equal(5, E(source.Scaffold.Graph.Strokes).Length);
        AssertNoNumbers(supplier.Surface);
    }

    private static Relation Bind(Relation word) => new(new Placeholder(), word);
    private static Relation Slots(int count) => S(Enumerable.Range(0, count).Select(_ => (IElement)new Placeholder()).ToArray());
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static void Parts(Relation read, out Relation word, out Relation groups, out Relation remainder, out Relation evidence)
    {
        var body = Assert.IsType<Relation>(read.Right); word = Assert.IsType<Relation>(body.Left);
        var tail = Assert.IsType<Relation>(body.Right); groups = Assert.IsType<Relation>(tail.Left);
        var last = Assert.IsType<Relation>(tail.Right); remainder = Assert.IsType<Relation>(last.Left); evidence = Assert.IsType<Relation>(last.Right);
    }
    private static HashSet<IElement> Reach(IElement source)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var work = new Stack<IElement>(); work.Push(source);
        while (work.TryPop(out var item))
        {
            if (!seen.Add(item)) continue;
            if (item is Relation relation) { work.Push(relation.Left); work.Push(relation.Right); }
        }
        return seen;
    }
    private static void AssertNoNumbers(IElement source) => Assert.DoesNotContain(Reach(source), item => item is Carrier or CarrierLocation);
}
