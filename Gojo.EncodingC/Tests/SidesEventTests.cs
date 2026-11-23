using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Goal, source and route, and how far a use got, as presence bits a
/// trail of side and step words settles against one landmark. At each moment:
/// the side word, the step word that arrived there, and seven bits: at the
/// landmark, a step toward it, a step away, crossed it, reached it so far,
/// abandoned it so far, under way to it. Every bit starts open; partial
/// evidence leaves bits open rather than guessing.
/// <para>The patterns are these bits read together. A goal ("to") is reached.
/// A source ("from") is at the landmark, then away. A route that passes it
/// ("past") is crossed; one that touches and returns ("to and back") is
/// reached, then away without crossing. Toward is under way. Expected is not
/// an observation: it is a supposition that the last step continues, held in
/// a workspace so that it never rules anything out in what is known.</para></summary>
public sealed class SidesEventTests
{
    private static readonly int After = Sides.After, Before = Sides.Before;
    private static readonly int AtFromAfter = Sides.AtFromAfter, AtFromBefore = Sides.AtFromBefore;
    private static readonly int With = Sides.With, Against = Sides.Against;

    private static ulong Values(params int[] values) => values.Aggregate(0UL, (mask, v) => mask | 1UL << v);

    private sealed record Trail(Engine Engine, Store Words, Supports Side, Supports Step, Field StepCode,
        Supports At, Supports Toward, Supports Away, Supports Crossed, Supports Reached, Supports Abandoned, Supports UnderWay);

    private static Supports Bits(int moments) => Supports.On(new Store(1, moments), Field.Bit(0));

    private static Trail Build(int moments)
    {
        var words = Odv.Create(moments);
        var stepCode = new Field(Sides.Word, words.AddPlane(), words.AddPlane());
        var side = Supports.On(words, Sides.Field);
        var step = Supports.On(words, stepCode);
        var (at, toward, away, crossed, reached, abandoned, underWay) =
            (Bits(moments), Bits(moments), Bits(moments), Bits(moments), Bits(moments), Bits(moments), Bits(moments));
        var engine = new Engine();
        Wiring Wire(RelationLaw law, params Binding[] roles)
        {
            var wiring = new Wiring(law, roles);
            engine.Add(wiring);
            return wiring;
        }

        var moves = Wire(Sides.Moves(), side.Variable, step.Variable, side.Variable);
        var continuous = Wire(Sides.Continuous, side.Variable, side.Variable);
        var isAt = Wire(Sides.At, side.Variable, at.Variable);
        var goesToward = Wire(Sides.Toward, side.Variable, step.Variable, toward.Variable);
        var goesAway = Wire(Sides.Away, side.Variable, step.Variable, away.Variable);
        var crosses = Wire(Sides.Crosses, side.Variable, side.Variable, crossed.Variable);
        var firstReached = Wire(Settling.Same(Shape.Bit), at.Variable, reached.Variable);
        var everReached = Wire(Settling.Ever, reached.Variable, at.Variable, reached.Variable);
        var abandons = Wire(Sides.Abandons, abandoned.Variable, reached.Variable, toward.Variable, away.Variable, abandoned.Variable);
        var underWayNow = Wire(Sides.UnderWay, toward.Variable, reached.Variable, abandoned.Variable, underWay.Variable);
        firstReached.Add(0, 0);
        foreach (var bits in new[] { toward, away, crossed, abandoned }) bits.Restrict(0, Values(0));
        for (var t = 0; t < moments; t++)
        {
            isAt.Add(t, t);
            underWayNow.Add(t, t, t, t);
            if (t == 0) continue;
            moves.Add(t - 1, t, t);
            continuous.Add(t - 1, t);
            goesToward.Add(t - 1, t, t);
            goesAway.Add(t - 1, t, t);
            crosses.Add(t - 1, t, t);
            everReached.Add(t - 1, t, t);
            abandons.Add(t - 1, t - 1, t - 1, t, t);
        }
        return new Trail(engine, words, side, step, stepCode, at, toward, away, crossed, reached, abandoned, underWay);
    }

    /// <summary>A trail seen: side words and the steps that arrived at each
    /// moment after the first. A null leaves that word open.</summary>
    private static Trail Seen(int?[] sides, int?[] steps)
    {
        var trail = Build(sides.Length);
        for (var t = 0; t < sides.Length; t++)
            if (sides[t] is int side) trail.Side.Restrict(t, Values(side));
        for (var t = 0; t < steps.Length; t++)
            if (steps[t] is int step) trail.Step.Restrict(t + 1, Values(step));
        Settle(trail.Engine);
        return trail;
    }

    private static void Settle(Engine engine)
    {
        for (var round = 0; round < 32; round++)
            if (engine.Step().Steps.All(step => step.Changed.None)) return;
        throw new InvalidOperationException("Narrowing did not come to rest.");
    }

    /// <summary>Each moment's bit, as 0, 1, or null while still open.</summary>
    private static int?[] Read(Supports bits) =>
        [.. Enumerable.Range(0, bits.Surface.Count).Select(t => bits.Values(t) switch { 1UL => 0, 2UL => 1, _ => (int?)null })];

    /// <summary>A goal: heading toward the store and reaching it. Under way
    /// while heading toward it, completed once at it.</summary>
    [Fact]
    public void AGoalIsReachedAfterHeadingTowardIt()
    {
        var trail = Seen([Before, Before, AtFromBefore], [With, With]);

        Assert.Equal([0, 1, 1], Read(trail.Toward));
        Assert.Equal([0, 0, 1], Read(trail.Reached));
        Assert.Equal([0, 1, 0], Read(trail.UnderWay));
        Assert.Equal([0, 0, 0], Read(trail.Abandoned));
    }

    /// <summary>Heading toward the store, then turning back before it: the
    /// goal is abandoned, and stays abandoned, never reached.</summary>
    [Fact]
    public void TurningBackShortOfTheGoalAbandonsIt()
    {
        var trail = Seen([Before, Before, Before, Before], [With, With, Against]);

        Assert.Equal([0, 1, 1, 0], Read(trail.Toward));
        Assert.Equal([0, 0, 0, 1], Read(trail.Away));
        Assert.Equal([0, 0, 0, 1], Read(trail.Abandoned));
        Assert.Equal([0, 0, 0, 0], Read(trail.Reached));
        Assert.Equal([0, 1, 1, 0], Read(trail.UnderWay));
    }

    /// <summary>A route past the landmark crosses it: reached at the mark, D
    /// changes there, and the next step goes away on the far side. A route to
    /// it and back reaches it and goes away without crossing. A source starts
    /// at it and goes away.</summary>
    [Fact]
    public void PastToAndBackAndFromAreDifferentPatternsOfTheSameBits()
    {
        var past = Seen([Before, AtFromBefore, AtFromAfter, After], [With, With, With]);
        var toAndBack = Seen([Before, AtFromBefore, Before], [With, Against]);
        var from = Seen([AtFromAfter, After], [With]);

        Assert.Equal([0, 0, 1, 0], Read(past.Crossed));
        Assert.Equal([0, 1, 1, 1], Read(past.Reached));
        Assert.Equal([0, 0, 0, 1], Read(past.Away));
        Assert.Equal([0, 0, 0], Read(toAndBack.Crossed));
        Assert.Equal([0, 1, 1], Read(toAndBack.Reached));
        Assert.Equal([0, 0, 1], Read(toAndBack.Away));
        Assert.Equal([1, 0], Read(from.At));
        Assert.Equal([0, 1], Read(from.Away));
    }

    /// <summary>Seen before the store, then after a step back still before it,
    /// with the moment between unseen: it may have reached the store and come
    /// back, or turned short of it. Whether the goal was reached or abandoned
    /// stays open, and nothing is guessed.</summary>
    [Fact]
    public void PartialEvidenceLeavesTheOutcomeOpen()
    {
        var trail = Seen([Before, null, Before], [With, Against]);

        Assert.Equal(Values(Before, AtFromBefore), trail.Side.Values(1));
        Assert.Equal([0, null, null], Read(trail.Reached));
        Assert.Equal([0, 0, null], Read(trail.Abandoned));
        Assert.Equal([0, 0, 1], Read(trail.Away));
    }

    /// <summary>Expected is a supposition, not an observation. In a workspace,
    /// one moment more is introduced and its step supposed to continue the last
    /// one. Under way toward the store, reaching it next is then possible;
    /// having turned back, it is not. The known trail is never narrowed by
    /// the supposition. The supposition is wired into the trail's own local
    /// structure: a workspace over a store is that store's structure's to bind.</summary>
    [Fact]
    public void ExpectedIsASuppositionThatTheLastStepContinues()
    {
        bool ReachableNext(int?[] sides, int?[] steps)
        {
            var trail = Seen(sides, steps);
            var last = sides.Length - 1;
            var known = trail.Side.Values(last);
            var ahead = new Workspace(trail.Words).SelectAll();
            var next = ahead.Introduce();
            var side = trail.Side.In(ahead);
            var step = trail.Step.In(ahead);
            var same = new Wiring(Settling.Same(Sides.Word), step.Variable, step.Variable);
            same.Add(last, next);
            var moves = new Wiring(Sides.Moves(), side.Variable, step.Variable, side.Variable);
            moves.Add(last, next, next);
            var continuous = new Wiring(Sides.Continuous, side.Variable, side.Variable);
            continuous.Add(last, next);
            trail.Engine.Add(same).Add(moves).Add(continuous);
            Settle(trail.Engine);
            Assert.Equal(known, trail.Side.Values(last));
            return (side.Values(next) & Values(AtFromBefore, AtFromAfter)) != 0;
        }

        Assert.True(ReachableNext([Before, Before], [With]));
        Assert.False(ReachableNext([Before, Before, Before], [With, Against]));
    }
}
