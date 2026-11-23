using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>A path before numbers: sites, a declared order between neighbours,
/// segments as the sites an open position can take between landmarks, and
/// traversal as a use of that definition.</summary>
public sealed class PathTests
{
    private const int Sites = 5;
    private static readonly Shape Site = Shape.Binary(Sites);

    /// <summary>home 0 → 1 → 2 → 3 → store 4.</summary>
    private static readonly (int, int)[] Road = [(0, 1), (1, 2), (2, 3), (3, 4)];

    /// <summary>home 0 → fork 1 → either 2 or 3 → store 4. Sites 2 and 3 are
    /// both between the ends and neither is before the other.</summary>
    private static readonly (int, int)[] Forked = [(0, 1), (1, 2), (1, 3), (2, 4), (3, 4)];

    private static ulong Values(params int[] values) => values.Aggregate(0UL, (mask, v) => mask | 1UL << v);

    private static Supports Open(int positions, out Store store)
    {
        store = new Store(Site.Bits, positions);
        return Supports.On(store, new Field(Site, [.. Enumerable.Range(0, Site.Bits)]));
    }

    /// <summary>The sites a position role can take between two settled landmarks.</summary>
    private static ulong Fill(RelationLaw law, int a, int b)
    {
        var site = Open(3, out _);
        site.Restrict(0, Values(a));
        site.Restrict(2, Values(b));
        var row = new Wiring(law, site.Variable, site.Variable, site.Variable);
        row.Add(0, 1, 2);
        row.Apply();
        return site.Values(1);
    }

    /// <summary>A segment at rest is an open position and the sites that can
    /// fill it. Open ends leave the landmarks out as its boundary; closed ends
    /// admit them. The landmarks may be written in either order.</summary>
    [Fact]
    public void ASegmentIsTheSitesAnOpenPositionCanTakeBetweenItsLandmarks()
    {
        var open = Paths.Within(Sites, Road, closed: false);
        var closed = Paths.Within(Sites, Road, closed: true);

        Assert.Equal(Values(1, 2, 3), Fill(open, 0, 4));
        Assert.Equal(Values(0, 1, 2, 3, 4), Fill(closed, 0, 4));
        Assert.Equal(Fill(open, 0, 4), Fill(open, 4, 0));
        Assert.Equal(0UL, Fill(open, 1, 2));
        Assert.Equal(Values(1, 2), Fill(closed, 1, 2));
        Assert.Equal(Values(2), Fill(closed, 2, 2));
        Assert.Equal(0UL, Fill(open, 2, 2));
    }

    /// <summary>A directed segment starts at its lower landmark, however the
    /// landmarks are written. Defined on the opposite orientation, the other
    /// landmark is lower. A range is the same sites with no start, and
    /// re-expressing it on the opposite orientation keeps every site.
    /// Landmarks the path does not order have no start and enclose nothing.</summary>
    [Fact]
    public void ASegmentStartsAtItsLowerLandmarkAndARangeKeepsItsSitesOnEitherOrientation()
    {
        var start = Paths.Start(Sites, Road);
        var reoriented = Paths.Reorient(Road);

        Assert.Equal(Values(0), Fill(start, 0, 4));
        Assert.Equal(Values(0), Fill(start, 4, 0));
        Assert.Equal(Values(4), Fill(Paths.Start(Sites, reoriented), 0, 4));
        Assert.True(Paths.Within(Sites, reoriented, closed: false).SameTable(Paths.Within(Sites, Road, closed: false)));
        Assert.Equal(0UL, Fill(Paths.Start(Sites, Forked), 2, 3));
        Assert.Equal(0UL, Fill(Paths.Within(Sites, Forked, closed: true), 2, 3));
    }

    /// <summary>One open site narrowed by several bound facts, each a row. The
    /// site is position 0; landmarks are positions 1, 2, …</summary>
    private static ulong Narrow(int[] landmarks, params (RelationLaw Law, int[] Row)[] facts)
    {
        var site = Open(1 + landmarks.Length, out _);
        for (var i = 0; i < landmarks.Length; i++) site.Restrict(i + 1, Values(landmarks[i]));
        var rows = facts.Select(fact =>
        {
            var row = new Wiring(fact.Law, [.. Enumerable.Repeat(site.Variable, fact.Law.Roles.Count)]);
            row.Add(fact.Row);
            return row;
        }).ToArray();
        for (var pass = 0; pass < 2; pass++)
            foreach (var row in rows) row.Apply();
        return site.Values(0);
    }

    /// <summary>A bound on one side leaves the other open: a ray. A bound from
    /// above is the same law on the reoriented path. A lower and an upper bound
    /// are the segment between them.</summary>
    [Fact]
    public void OneSidedBoundsAreRaysAndTwoSidesMakeTheSegment()
    {
        var after = Paths.After(Sites, Road, closed: false);
        var before = Paths.After(Sites, Paths.Reorient(Road), closed: false);

        Assert.Equal(Values(2, 3, 4), Narrow([1], (after, [1, 0])));
        Assert.Equal(Values(1, 2, 3, 4), Narrow([1], (Paths.After(Sites, Road, closed: true), [1, 0])));
        Assert.Equal(Values(0, 1, 2), Narrow([3], (before, [1, 0])));
        Assert.Equal(Fill(Paths.Within(Sites, Road, closed: false), 0, 4), Narrow([0, 4], (after, [1, 0]), (before, [2, 0])));
    }

    /// <summary>"A &lt; B" and "C &lt; B" with A before C: both landmarks lie on one
    /// side, and the bounds cancel to a ray after C. Dropping A's row changes
    /// nothing, since it rules nothing out; dropping C's does.</summary>
    [Fact]
    public void TwoLowerBoundsCancelToTheGreaterAndTheOtherIsRedundant()
    {
        var after = Paths.After(Sites, Road, closed: false);

        var both = Narrow([0, 2], (after, [1, 0]), (after, [2, 0]));

        Assert.Equal(Values(3, 4), both);
        Assert.Equal(both, Narrow([0, 2], (after, [2, 0])));
        Assert.NotEqual(both, Narrow([0, 2], (after, [1, 0])));
    }

    /// <summary>A gap is what lies before both landmarks or after both: the
    /// complement of the closed segment between them. It reads the same with
    /// its landmarks written either way and on the reoriented path, so it
    /// works on a non-flowing range as on a directed segment. On a branching
    /// path, a site the landmarks do not order is in neither.</summary>
    [Fact]
    public void AGapIsWhatLiesBeyondBothLandmarks()
    {
        var gap = Paths.Outside(Sites, Road, closed: false);

        Assert.Equal(Values(0, 4), Fill(gap, 1, 3));
        Assert.Equal(Fill(gap, 1, 3), Fill(gap, 3, 1));
        Assert.Equal(Values(0, 1, 3, 4), Fill(Paths.Outside(Sites, Road, closed: true), 1, 3));
        Assert.Equal(Values(0, 1, 2, 3, 4) & ~Fill(Paths.Within(Sites, Road, closed: true), 1, 3), Fill(gap, 1, 3));
        Assert.True(Paths.Outside(Sites, Paths.Reorient(Road), closed: false).SameTable(gap));
        Assert.Equal(Values(0, 4), Fill(Paths.Outside(Sites, Forked, closed: false), 1, 3));
        Assert.Equal(Values(0, 1, 4), Fill(Paths.Outside(Sites, Forked, closed: false), 2, 3));
    }

    /// <summary>Two sites that neither precede the other can both lie between
    /// the ends. Traversing from the fork to the store in two steps leaves the
    /// route open: nothing chooses it.</summary>
    [Fact]
    public void OnABranchingPathBothRoutesFitAndNothingChooses()
    {
        Assert.Equal(Values(1, 2, 3), Fill(Paths.Within(Sites, Forked, closed: false), 0, 4));

        var site = Open(3, out var store);
        var direction = Supports.On(store, new Field(Sides.Word, 0, 1));
        site.Restrict(0, Values(1));
        site.Restrict(2, Values(4));
        var steps = new Wiring(Paths.Step(Sites, Forked), site.Variable, site.Variable, direction.Variable);
        steps.Add(0, 1, 1);
        steps.Add(1, 2, 2);
        steps.Apply();
        steps.Apply();

        Assert.Equal(Values(2, 3), site.Values(1));
        Assert.Equal((Values(Sides.With), Values(Sides.With)), (direction.Values(1), direction.Values(2)));
    }

    /// <summary>A use, out and back along the road. Read on the road's
    /// orientation its steps go forward then back; read on the reoriented
    /// road, by binding the same step law with its site roles swapped, they go
    /// back then forward. The sites never change. A change of direction is a
    /// turn on either reading; "turned back" holds only on the orientation it
    /// was said on.</summary>
    [Fact]
    public void AUseReadOnTheReorientedPathSwapsItsStepsAndKeepsItsTurn()
    {
        var site = Open(3, out var store);
        var along = Supports.On(store, new Field(Sides.Word, 0, 1));
        var against = Supports.On(store, new Field(Sides.Word, 0, 1));
        var reread = Supports.On(store, new Field(Sides.Word, 0, 1));
        foreach (var (moment, at) in new[] { (0, 1), (1, 2), (2, 1) }) site.Restrict(moment, Values(at));
        var step = Paths.Step(Sites, Road);
        var forward = new Wiring(step, site.Variable, site.Variable, along.Variable);
        var swapped = new Wiring(step, site.Variable, site.Variable, against.Variable);
        var reoriented = new Wiring(Paths.Step(Sites, Paths.Reorient(Road)), site.Variable, site.Variable, reread.Variable);
        for (var t = 1; t < 3; t++)
        {
            forward.Add(t - 1, t, t);
            swapped.Add(t, t - 1, t);
            reoriented.Add(t - 1, t, t);
        }
        foreach (var wiring in new[] { forward, swapped, reoriented }) wiring.Apply();

        Assert.Equal((Values(Sides.With), Values(Sides.Against)), (along.Values(1), along.Values(2)));
        Assert.Equal((Values(Sides.Against), Values(Sides.With)), (against.Values(1), against.Values(2)));
        Assert.Equal([against.Values(1), against.Values(2)], new[] { reread.Values(1), reread.Values(2) });
        Assert.Equal([Values(1), Values(2), Values(1)], new[] { site.Values(0), site.Values(1), site.Values(2) });
        Assert.True(Read(Sides.Reversal, along).None);
        Assert.True(Read(Sides.Reversal, against).None);
        Assert.True(Read(Sides.TurnedBack, along).None);
        Assert.False(Read(Sides.TurnedBack, against).None);
    }

    /// <summary>Rows of a two-step direction read whose settled directions the law does not admit.</summary>
    private static Plane Read(RelationLaw law, Supports direction)
    {
        var turn = new Wiring(law, direction.Variable, direction.Variable);
        turn.Add(1, 2);
        return turn.Violations();
    }
}
