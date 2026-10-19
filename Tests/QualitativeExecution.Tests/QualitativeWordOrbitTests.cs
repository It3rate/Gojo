using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QualitativeWordOrbitTests
{
    [Fact]
    public void FourActualTransitionsReturnAxisExchangeSeedsAndRetainTheirReadChain()
    {
        var f = new Fixture(); var right = f.Word(f.Frame.Right); var up = f.Word(f.Frame.Up);
        var cycle = Cycle(4); var orbit = new QualitativeWordOrbit(f.QuarterTurn(), S(right, up), cycle);
        Assert.True(orbit.IsComplete); Assert.Equal(8, E(orbit.Steps).Length); Assert.Equal(2, E(orbit.Returns).Length);
        Assert.Empty(E(orbit.Mismatches)); Assert.Empty(E(orbit.Blocked)); Assert.Empty(E(orbit.Unresolved));
        foreach (var seed in new[] { right, up })
        {
            var actualSteps = E(orbit.Steps).Cast<Relation>().Where(row => ReferenceEquals(((Relation)row.Left).Left, seed)).ToArray();
            var previous = seed;
            foreach (var step in actualSteps)
            {
                Assert.True(orbit.TryReadStep(step, out var source, out var successor, out var before, out var after, out var read));
                Assert.Same(seed, source); Assert.Same(previous, before);
                Assert.Contains(E((Relation)cycle.Right), actual => ReferenceEquals(actual, successor));
                Assert.Same(read, ((Relation)step.Right).Right); previous = after;
            }
            var result = E(orbit.Returns).Cast<Relation>().Single(row => ReferenceEquals(((Relation)row.Left).Left, seed));
            Assert.True(orbit.TryReadReturn(result, out var sourceSeed, out var terminal, out var difference));
            Assert.Same(seed, sourceSeed); Assert.Same(previous, terminal);
            Assert.True(f.Frame.TryReadResult(difference, out var remaining, out _, out _)); Assert.Empty(E(remaining));
            Assert.False(orbit.TryReadReturn(new Relation(result.Left, result.Right), out _, out _, out _));
        }
    }

    [Fact]
    public void SixPhaseLatticeSubstitutionReturnsWithoutACalibratedAngle()
    {
        var f = new Fixture(); var map = f.SixPhase();
        var orbit = new QualitativeWordOrbit(map, S(f.Word(f.Frame.Right), f.Word(f.Frame.Up)), Cycle(6));
        Assert.True(orbit.IsComplete); Assert.Equal(12, E(orbit.Steps).Length); Assert.Equal(2, E(orbit.Returns).Length);
        foreach (var read in E(orbit.Returns).Cast<Relation>())
            Assert.True(orbit.TryReadReturn(read, out _, out _, out _));
        var half = new QualitativeWordOrbit(map, S(f.Word(f.Frame.Right), f.Word(f.Frame.Up)), Cycle(3));
        Assert.False(half.IsComplete); Assert.Empty(E(half.Returns)); Assert.Equal(2, E(half.Mismatches).Length);
        Assert.Empty(E(half.Blocked)); Assert.Empty(E(half.Unresolved));
    }

    [Fact]
    public void ValidMoveSubstitutionCanRefuteOneReturnWhileSupportingAnother()
    {
        var f = new Fixture(); var map = f.Map(f.Word(f.Frame.Up), f.Word(f.Frame.Down),
            f.Word(f.Frame.Up), f.Word(f.Frame.Down));
        var right = f.Word(f.Frame.Right); var up = f.Word(f.Frame.Up);
        var orbit = new QualitativeWordOrbit(map, S(right, up), Cycle(4));
        Assert.False(orbit.IsComplete); Assert.Single(E(orbit.Returns)); Assert.Single(E(orbit.Mismatches));
        Assert.Empty(E(orbit.Blocked)); Assert.Empty(E(orbit.Unresolved));
        Assert.Same(right, ((Relation)((Relation)Assert.Single(E(orbit.Mismatches))).Left).Left);
        Assert.Same(up, ((Relation)((Relation)Assert.Single(E(orbit.Returns))).Left).Left);
    }

    [Fact]
    public void MissingSubstitutionRemainsOpenAndDoesNotAcquireAnIdentityFallback()
    {
        var f = new Fixture(); var map = new QualitativePatternTransform(f.Frame, f.Frame,
            S(new Relation(QualitativeMotionFrame.Move(f.Frame.Right, f.Reach), f.Word(f.Frame.Up))));
        var orbit = new QualitativeWordOrbit(map, S(f.Word(f.Frame.Right)), Cycle(4));
        Assert.False(orbit.IsComplete); Assert.Empty(E(orbit.Returns)); Assert.Empty(E(orbit.Mismatches));
        Assert.Equal(2, E(orbit.Steps).Length); Assert.Empty(E(orbit.Blocked)); Assert.NotEmpty(E(orbit.Unresolved));
        Assert.False(orbit.TryReadStep((Relation)E(orbit.Steps)[1], out _, out _, out _, out _, out _));
    }

    [Fact]
    public void AnIllegalImageIsBlockedRatherThanAnUncertainOrReturningMove()
    {
        var f = new Fixture(); var map = f.Map(S(new Placeholder()), f.Word(f.Frame.Left),
            f.Word(f.Frame.Up), f.Word(f.Frame.Down));
        var orbit = new QualitativeWordOrbit(map, S(f.Word(f.Frame.Right)), Cycle(4));
        Assert.False(orbit.IsComplete); Assert.Single(E(orbit.Steps)); Assert.NotEmpty(E(orbit.Blocked));
        Assert.Empty(E(orbit.Returns)); Assert.Empty(E(orbit.Mismatches)); Assert.Empty(E(orbit.Unresolved));
    }

    [Fact]
    public void OpenCycleAndIncorrectSuccessorLinksDoNotCertifyAPrefixAsAWholeOrbit()
    {
        var f = new Fixture(); var actual = Cycle(4); var phases = E((Relation)actual.Left);
        var open = new Relation(StructuralConstraint.Open(phases), actual.Right);
        var wrong = new Relation(actual.Left, S(phases.Select(phase => (IElement)new Relation(phase, phase)).ToArray()));
        var pending = new QualitativeWordOrbit(f.QuarterTurn(), S(f.Word(f.Frame.Right)), open);
        var blocked = new QualitativeWordOrbit(f.QuarterTurn(), S(f.Word(f.Frame.Right)), wrong);
        Assert.Empty(E(pending.Steps)); Assert.Empty(E(pending.Returns)); Assert.NotEmpty(E(pending.Unresolved));
        Assert.Empty(E(blocked.Steps)); Assert.Empty(E(blocked.Returns)); Assert.NotEmpty(E(blocked.Blocked));
    }

    [Fact]
    public void RepeatedEmptyForeignAndOpenSeedClaimsCannotSupplyMissingReturnEvidence()
    {
        var f = new Fixture(); var seed = f.Word(f.Frame.Right); var map = f.QuarterTurn(); var cycle = Cycle(4);
        var repeated = new QualitativeWordOrbit(map, S(seed, seed), cycle);
        var empty = new QualitativeWordOrbit(map, S(), cycle);
        var foreign = new QualitativeWordOrbit(map, S(S(QualitativeMotionFrame.Move(new QualitativeMotionFrame().Right, f.Reach))), cycle);
        var open = new QualitativeWordOrbit(map, S(StructuralConstraint.Open(E(seed))), cycle);
        foreach (var orbit in new[] { repeated, empty, foreign })
        { Assert.False(orbit.IsComplete); Assert.Empty(E(orbit.Returns)); Assert.NotEmpty(E(orbit.Blocked)); }
        Assert.False(open.IsComplete); Assert.Empty(E(open.Returns)); Assert.NotEmpty(E(open.Unresolved));
    }

    [Fact]
    public void EqualHeadingWithAnUnmappedReachDoesNotReuseAnotherSeedsReturn()
    {
        var f = new Fixture(); var otherReach = new Placeholder();
        var word = S(QualitativeMotionFrame.Move(f.Frame.Right, otherReach));
        var orbit = new QualitativeWordOrbit(f.QuarterTurn(), S(word), Cycle(4));
        Assert.False(orbit.IsComplete); Assert.Empty(E(orbit.Returns)); Assert.NotEmpty(E(orbit.Unresolved));
    }

    [Fact]
    public void ATransformIntoAnotherFrameNeedsAnExplicitCompositionBeforeItCanRepeat()
    {
        var f = new Fixture(); var target = new QualitativeMotionFrame();
        var map = new QualitativePatternTransform(f.Frame, target, S(new Relation(
            QualitativeMotionFrame.Move(f.Frame.Right, f.Reach), S(QualitativeMotionFrame.Move(target.Right, f.Reach)))));
        var orbit = new QualitativeWordOrbit(map, S(f.Word(f.Frame.Right)), Cycle(4));
        Assert.False(orbit.IsComplete); Assert.Empty(E(orbit.Steps)); Assert.NotEmpty(E(orbit.Blocked));
    }

    private sealed class Fixture
    {
        public QualitativeMotionFrame Frame { get; } = new();
        public IElement Reach { get; } = new Placeholder();
        public Relation Word(params IElement[] directions) => S(directions.Select(direction =>
            (IElement)QualitativeMotionFrame.Move(direction, Reach)).ToArray());
        public QualitativePatternTransform QuarterTurn() => Map(Word(Frame.Up), Word(Frame.Down), Word(Frame.Left), Word(Frame.Right));
        public QualitativePatternTransform SixPhase() => Map(Word(Frame.Up), Word(Frame.Down),
            Word(Frame.Left, Frame.Up), Word(Frame.Right, Frame.Down));
        public QualitativePatternTransform Map(Relation right, Relation left, Relation up, Relation down) => new(Frame, Frame, S(
            new Relation(QualitativeMotionFrame.Move(Frame.Right, Reach), right), new Relation(QualitativeMotionFrame.Move(Frame.Left, Reach), left),
            new Relation(QualitativeMotionFrame.Move(Frame.Up, Reach), up), new Relation(QualitativeMotionFrame.Move(Frame.Down, Reach), down)));
    }

    private static Relation Cycle(int count)
    {
        var phases = Enumerable.Range(0, count).Select(_ => (IElement)new Placeholder()).ToArray();
        return new(S(phases), S(phases.Select((phase, index) => (IElement)new Relation(phase, phases[(index + 1) % phases.Length])).ToArray()));
    }
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
