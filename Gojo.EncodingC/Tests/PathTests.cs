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

    /// <summary>Two sites that neither precede the other can both lie between
    /// the ends. Traversing from the fork to the store in two steps leaves the
    /// route open: nothing chooses it.</summary>
    [Fact]
    public void OnABranchingPathBothRoutesFitAndNothingChooses()
    {
        Assert.Equal(Values(1, 2, 3), Fill(Paths.Within(Sites, Forked, closed: false), 0, 4));

        var site = Open(3, out var store);
        var direction = Supports.On(store, new Field(Paths.Direction, 0, 1));
        site.Restrict(0, Values(1));
        site.Restrict(2, Values(4));
        var steps = new Wiring(Paths.Step(Sites, Forked), site.Variable, site.Variable, direction.Variable);
        steps.Add(0, 1, 1);
        steps.Add(1, 2, 2);
        steps.Apply();
        steps.Apply();

        Assert.Equal(Values(2, 3), site.Values(1));
        Assert.Equal((Values(Paths.Forward), Values(Paths.Forward)), (direction.Values(1), direction.Values(2)));
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
        var along = Supports.On(store, new Field(Paths.Direction, 0, 1));
        var against = Supports.On(store, new Field(Paths.Direction, 0, 1));
        var reread = Supports.On(store, new Field(Paths.Direction, 0, 1));
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

        Assert.Equal((Values(Paths.Forward), Values(Paths.Back)), (along.Values(1), along.Values(2)));
        Assert.Equal((Values(Paths.Back), Values(Paths.Forward)), (against.Values(1), against.Values(2)));
        Assert.Equal([against.Values(1), against.Values(2)], new[] { reread.Values(1), reread.Values(2) });
        Assert.Equal([Values(1), Values(2), Values(1)], new[] { site.Values(0), site.Values(1), site.Values(2) });
        Assert.True(Read(Paths.Reversal, along).None);
        Assert.True(Read(Paths.Reversal, against).None);
        Assert.True(Read(Paths.TurnedBack, along).None);
        Assert.False(Read(Paths.TurnedBack, against).None);
    }

    /// <summary>Rows of a two-step direction read whose settled directions the law does not admit.</summary>
    private static Plane Read(RelationLaw law, Supports direction)
    {
        var turn = new Wiring(law, direction.Variable, direction.Variable);
        turn.Add(1, 2);
        return turn.Violations();
    }
}
