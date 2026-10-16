using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class FriezeWordFeaturesTests
{
    [Fact]
    public void EqualVocabularyCanHaveDifferentOrderedContinuationStructure()
    {
        var frame = new QualitativeMotionFrame(); var h = new Placeholder(); var v = new Placeholder();
        var r = M(frame.Right, h); var u = M(frame.Up, v); var d = M(frame.Down, v);
        var a = S(r, u); var b = S(r, d);
        var alternating = FriezeFixture.Sequence(frame, S(a, b, a, b));
        var blocked = FriezeFixture.Sequence(frame, S(a, a, b, b));
        var first = new FriezeWordFeatures(alternating, alternating.Run(4), Budget());
        var second = new FriezeWordFeatures(blocked, blocked.Run(4), Budget());
        Assert.Equal(new FriezeWordFeatures.Summary(4, 2, 4, 0, 0), first.Describe());
        Assert.Equal(new FriezeWordFeatures.Summary(4, 2, 2, 2, 0), second.Describe());
        Assert.DoesNotContain(Traverse(first.Evaluate()), value => value is Carrier or CarrierLocation);
        // Prediction ambiguity belongs to the selected current-word context;
        // both actual phase recurrences remain fully determined.
        Assert.True(first.TryReadResult(first.Evaluate(), out _, out _, out _, out var pending)); Assert.Empty(E(pending));
        Assert.True(second.TryReadResult(second.Evaluate(), out _, out _, out _, out pending)); Assert.Empty(E(pending));
    }

    [Fact]
    public void ReuseKeepsDistinctSampleApplicationsAndActualMoveCorrespondences()
    {
        var frame = new QualitativeMotionFrame(); var h = new Placeholder();
        var firstMove = M(frame.Right, h); var secondMove = M(frame.Right, h);
        var fixture = FriezeFixture.Sequence(frame, S(S(firstMove), S(secondMove)));
        var feature = new FriezeWordFeatures(fixture, fixture.Run(2), Budget());
        Assert.Equal(1, feature.Describe().DistinctWords);
        var group = Assert.IsType<Relation>(Assert.Single(E(feature.Groups)));
        Assert.True(feature.TryReadGroup(group, out _, out var scope));
        var members = E(scope).Cast<Relation>().ToArray(); Assert.Equal(2, members.Length);
        var words = members.Select(member => Assert.IsType<Relation>(member.Left)).ToArray();
        Assert.NotSame(words[0].Left, words[1].Left); Assert.NotSame(words[0].Right, words[1].Right);
        Assert.Contains(members, member => E(Assert.IsType<Relation>(member.Right)).Any(value =>
            value is Relation pair && ReferenceEquals(pair.Left, firstMove) && ReferenceEquals(pair.Right, secondMove)));
    }

    [Fact]
    public void EqualLookingIndependentReachesAreNotTheSameEmissionWord()
    {
        var frame = new QualitativeMotionFrame();
        var fixture = FriezeFixture.Sequence(frame, S(S(M(frame.Right, new Placeholder())), S(M(frame.Right, new Placeholder()))));
        var feature = new FriezeWordFeatures(fixture, fixture.Run(2), Budget());
        Assert.Equal(2, feature.Describe().DistinctWords);
    }

    [Fact]
    public void AnUnfinishedPeriodCannotAcquireAnInventedCyclicTransition()
    {
        var fixture = FriezeFixture.CombinedDriveZigzag();
        var partial = new FriezeWordFeatures(fixture, fixture.Run(1), Budget());
        Assert.NotEmpty(E(partial.Unresolved)); Assert.Empty(E(partial.Transitions));
        var complete = new FriezeWordFeatures(fixture, fixture.Run(2), Budget());
        Assert.Empty(E(complete.Unresolved)); Assert.Equal(2, E(complete.Transitions).Length);
    }

    [Fact]
    public void UnknownNetDisplacementDoesNotEraseKnownOrderedEmissions()
    {
        var frame = new QualitativeMotionFrame();
        var fixture = FriezeFixture.Sequence(frame, S(S(M(frame.Right, new Placeholder())), S(M(frame.Left, new Placeholder()))));
        var feature = new FriezeWordFeatures(fixture, fixture.Run(2), Budget());
        Assert.Equal(2, feature.Describe().Samples); Assert.Equal(2, feature.Describe().ChangedWordTransitions);
        Assert.Empty(E(feature.Unresolved)); Assert.NotEmpty(E(feature.LegalityUnresolved));
        Assert.True(Contains(feature.Evaluate(), feature.RuleRead)); Assert.True(Contains(feature.Evaluate(), feature.DriveRead));
    }

    [Fact]
    public void KnownLegalityViolationsDoNotEraseReadableWordTransitions()
    {
        var frame = new QualitativeMotionFrame(); var reach = new Placeholder();
        var fixture = FriezeFixture.Sequence(frame, S(S(M(frame.Right, reach)), S(M(frame.Left, reach))));
        var feature = new FriezeWordFeatures(fixture, fixture.Run(2), Budget());
        Assert.Equal(2, feature.Describe().Samples); Assert.Equal(2, feature.Describe().DistinctWords);
        Assert.Equal(2, feature.Describe().ChangedWordTransitions);
        Assert.Empty(E(feature.Unresolved)); Assert.Empty(E(feature.LegalityUnresolved)); Assert.NotEmpty(E(feature.Violations));
    }

    [Fact]
    public void OmittingOrBorrowingEvidenceDoesNotReadAsThisFeatureResult()
    {
        var fixture = FriezeFixture.CombinedDriveZigzag();
        var a = new FriezeWordFeatures(fixture, fixture.Run(2), Budget());
        var b = new FriezeWordFeatures(fixture, fixture.Run(2), Budget());
        Assert.False(a.TryReadResult(b.Evaluate(), out _, out _, out _, out _));
        var forged = new Relation(a.Surface, new Relation(new Relation(a.DriveRead, a.RuleRead),
            new Relation(new Relation(S(), a.Transitions), new Relation(a.Continuations, a.Unresolved))));
        Assert.False(a.TryReadResult(forged, out _, out _, out _, out _));
    }

    private static Relation Budget() => S(Enumerable.Range(0, 64).Select(_ => (IElement)new Placeholder()).ToArray());
    private static Relation M(IElement direction, IElement reach) => QualitativeMotionFrame.Move(direction, reach);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Contains(IElement source, IElement expected) => Traverse(source).Any(element => ReferenceEquals(element, expected));
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var value))
        { if (!seen.Add(value)) continue; yield return value; if (value is Relation relation) { stack.Push(relation.Left); stack.Push(relation.Right); } }
    }
}
