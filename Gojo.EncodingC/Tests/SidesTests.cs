using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Positions as sides of landmarks: a stretch is a few words, not a
/// chain of sites, and the laws that relate them come from ODV arithmetic.</summary>
public sealed class SidesTests
{
    private static readonly int After = Sides.After, Before = Sides.Before;
    private static readonly int AtFromAfter = Sides.AtFromAfter, AtFromBefore = Sides.AtFromBefore;

    private static ulong Values(params int[] words) => words.Aggregate(0UL, (mask, w) => mask | 1UL << w);

    /// <summary>An ODV store on the positive-right orientation, with side
    /// supports: every position starts open, every word still possible.</summary>
    private static Supports Open(int positions, out Store store)
    {
        store = Odv.Create(positions);
        return Supports.On(store, Sides.Field);
    }

    private static Binding Sample(Store store) => new(store, Odv.V, Odv.D);

    private static void Settle(Engine engine)
    {
        for (var round = 0; round < 16; round++)
            if (engine.Step().Steps.All(step => step.Changed.None)) return;
        throw new InvalidOperationException("Narrowing did not come to rest.");
    }

    /// <summary>With the store after home, the words a position can have against
    /// the two are the five places: before home, at home, between, at the
    /// store, after it. Under the detection contract one more is possible: at
    /// both at once, when the two are close enough that something between them
    /// is not detectably away from either at this resolution.</summary>
    [Fact]
    public void TwoLandmarksGiveFivePlacesAndNoSites()
    {
        static HashSet<(int, int)> Admitted(RelationLaw agree) =>
            [.. from home in Enumerable.Range(0, 4)
                from store in Enumerable.Range(0, 4)
                where agree.BitAt(agree.TupleIndex([home, store, After]))
                select (home, store)];
        HashSet<(int, int)> places =
        [
            (Before, Before),
            (AtFromAfter, Before), (AtFromBefore, Before),
            (After, Before),
            (After, AtFromAfter), (After, AtFromBefore),
            (After, After),
        ];

        Assert.Equal(places, Admitted(Sides.Agree(Odv.ExactZero)));
        Assert.Equal([.. places, (AtFromAfter, AtFromBefore)], Admitted(Sides.Agree()));
    }

    /// <summary>"Beyond home" is a region: one word still possible, the rest
    /// ruled out, whatever the length beyond. Known to be before home, a
    /// position is known to be before the store too; known only to be after
    /// home, it may be anywhere against the store until that is said.</summary>
    [Fact]
    public void AFactAboutOneLandmarkNarrowsTheOther()
    {
        var side = Open(3, out var store);
        Sides.Field.Write(store, 2, After);
        var agree = new Wiring(Sides.Agree(), side.Variable, side.Variable, Sample(store));
        agree.Add(0, 1, 2);

        side.Restrict(0, Values(Before));
        agree.Apply();
        Assert.Equal(Values(Before), side.Values(1));

        side.Forget(0);
        side.Forget(1);
        side.Restrict(0, Values(After));
        agree.Apply();
        Assert.Equal(Values(After, Before, AtFromAfter, AtFromBefore), side.Values(1));

        side.Restrict(1, Values(Before));
        agree.Apply();
        Assert.Equal((Values(After), Values(Before)), (side.Values(0), side.Values(1)));
    }

    /// <summary>Evidence that cannot be a point between the two, "before home and
    /// after the store", as for a street that runs past both, is kept, not
    /// thrown away: its words are left with nothing possible and its row says
    /// so. The declared fact that the store is after home is a sample the
    /// relation never narrows, and an ordinary position beside it settles as
    /// usual.</summary>
    [Fact]
    public void UnusualEvidenceIsKeptAsInformationAndOrdinaryFactsStand()
    {
        var side = Open(5, out var store);
        Sides.Field.Write(store, 2, After);
        var agree = new Wiring(Sides.Agree(), side.Variable, side.Variable, Sample(store));
        agree.Add(0, 1, 2);
        agree.Add(3, 4, 2);
        side.Restrict(0, Values(Before));
        side.Restrict(1, Values(After));
        side.Restrict(3, Values(After));
        side.Restrict(4, Values(Before));

        var step = agree.Apply();

        Assert.True(step.Unadmitted[0]);
        Assert.Equal((0UL, 0UL), (side.Values(0), side.Values(1)));
        Assert.Equal((Values(After), Values(Before)), (side.Values(3), side.Values(4)));
        Assert.Equal(After, Sides.Field.Read(store, 2));
    }

    /// <summary>From one moment to the next a side changes only while at the
    /// landmark: before, at, at on the other side, after. Never straight across.</summary>
    [Fact]
    public void NothingJumpsAcrossAMark()
    {
        var continuous = Sides.Continuous;
        bool Allows(int from, int to) => continuous.BitAt(continuous.TupleIndex([from, to]));

        Assert.True(Allows(Before, AtFromBefore) && Allows(AtFromBefore, AtFromAfter) && Allows(AtFromAfter, After));
        Assert.False(Allows(Before, After));
        Assert.False(Allows(After, Before));
        Assert.False(Allows(Before, AtFromAfter));
    }

    /// <summary>The sentence on sides, against the store alone: one word per
    /// moment, one word per step. Starting before the store and stepping with
    /// the orientation twice, the mover may have reached the store but cannot
    /// have passed it. Then it turned back, and is before the store again or
    /// just leaving it. Arrival stays possible and unestablished until it is
    /// seen there.</summary>
    [Fact]
    public void MovingTowardTheStoreThenTurningBackOnSides()
    {
        var side = Open(7, out _);
        var engine = new Engine();
        var moves = new Wiring(Sides.Moves(), side.Variable, side.Variable, side.Variable);
        var continuous = new Wiring(Sides.Continuous, side.Variable, side.Variable);
        for (var t = 1; t <= 3; t++)
        {
            moves.Add(t - 1, 3 + t, t);
            continuous.Add(t - 1, t);
        }
        var turned = new Wiring(Sides.TurnedBack, side.Variable, side.Variable);
        turned.Add(5, 6);
        engine.Add(moves).Add(continuous).Add(turned);
        side.Restrict(0, Values(Before));
        side.Restrict(4, Values(Sides.With));
        side.Restrict(5, Values(Sides.With));
        side.Restrict(6, Values(Sides.Against));

        Settle(engine);

        Assert.Equal(Values(Before, AtFromBefore), side.Values(1));
        Assert.Equal(Values(Before, AtFromBefore, AtFromAfter), side.Values(2));
        Assert.Equal(Values(Before, AtFromBefore), side.Values(3));
        Assert.True(turned.Violations().None);

        side.Restrict(2, Values(AtFromBefore, AtFromAfter));
        Settle(engine);
        Assert.Equal(Values(AtFromBefore, AtFromAfter), side.Values(2));
        Assert.True(side.Empty.None);
    }

    /// <summary>A bridge between home and the store makes the read finer. Seen
    /// before the bridge, the mover is still between home and the store: what
    /// the coarse read knew stands, and the bridge only adds.</summary>
    [Fact]
    public void ALandmarkInsideTheStretchRefinesWithoutUndoingIt()
    {
        // 0 B vs home, 1 B vs store, 2 B vs bridge; 3 store vs home, 4 bridge vs home, 5 store vs bridge.
        var side = Open(6, out var store);
        foreach (var fact in new[] { 3, 4, 5 }) Sides.Field.Write(store, fact, After);
        var agree = new Wiring(Sides.Agree(), side.Variable, side.Variable, Sample(store));
        agree.Add(0, 1, 3);
        agree.Add(0, 2, 4);
        agree.Add(2, 1, 5);
        var engine = new Engine().Add(agree);
        side.Restrict(0, Values(After));
        side.Restrict(1, Values(Before));
        Settle(engine);
        var coarse = (side.Values(0), side.Values(1));

        side.Restrict(2, Values(Before));
        Settle(engine);

        Assert.Equal(coarse, (side.Values(0), side.Values(1)));
        Assert.Equal(Values(Before), side.Values(2));
        Assert.True(side.Empty.None);
    }
}
