using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Where an operation sits among the others is calculated from what
/// its law already holds, never from a name (ops_claude/GojoOperations-2026-11.md,
/// §1.3). Two coordinates are mechanism, read from the table and its checked
/// accounting: whether what a written role gains is paid for by another role
/// (a move: the source is used up) or by nothing (a read: the inputs are kept),
/// and whether units trade one for one with no corner (along) or meet at a
/// rate or a corner (across). The rest are bindings, free to every law: which
/// face of a count a role fills, which roles are bound and which are read
/// back, whether a result is written over an input's own count, and whether it
/// is folded. So join, tear out and cancel are one move, and compare, threshold
/// and accumulate are heap's table read backward, folded or bound in place.</summary>
public sealed class OperationPrimitiveTests
{
    private const int Capacity = 3;

    private static readonly Plane Here = Plane.Of(1, 0);

    /// <summary>The one after-state of a functional law.</summary>
    private static int[] Next(TransitionLaw law, params int[] state) =>
        law.Tuple(Assert.Single(law.Afters(law.TupleIndex(state))));

    /// <summary>Per role: 0 only read, 1 written with nothing paying for it,
    /// 3 written and paid by another role.</summary>
    private static int[] Fate(TransitionLaw law) =>
        [.. Enumerable.Range(0, law.Roles.Count).Select(r => (law.Writes(r) ? 1 : 0) | (law.Accounting.Conserves(r) ? 2 : 0))];

    /// <summary>Every (loss, gain) the changing transitions trade between two roles.</summary>
    private static (int Loss, int Gain)[] Trades(TransitionLaw law, int from, int to) =>
        [.. Enumerable.Range(0, law.StateCount)
            .SelectMany(before => law.Afters(before).Select(after => (B: law.Tuple(before), A: law.Tuple(after))))
            .Where(p => p.B[from] != p.A[from])
            .Select(p => (p.B[from] - p.A[from], p.A[to] - p.B[to]))
            .Distinct()];

    /// <summary>The corner at each cell of the first two roles: what the
    /// result gains when both arrive that it gains from neither alone,
    /// f(a+1, b+1) − f(a+1, b) − f(a, b+1) + f(a, b).</summary>
    private static int[,] Corners(TransitionLaw law, int result, int capacityA, int capacityB)
    {
        int F(int a, int b)
        {
            var state = new int[law.Roles.Count];
            (state[0], state[1]) = (a, b);
            return Next(law, state)[result];
        }
        var corners = new int[capacityA, capacityB];
        for (var a = 0; a < capacityA; a++)
            for (var b = 0; b < capacityB; b++)
                corners[a, b] = F(a + 1, b + 1) - F(a + 1, b) - F(a, b + 1) + F(a, b);
        return corners;
    }

    /// <summary>A read of two inputs into one written result, the shape the
    /// counting laws share; master has no product, so the test builds one.</summary>
    private static TransitionLaw Reading(Shape a, Shape b, Shape result, Func<int, int, int> read) =>
        TransitionLaw.FromFunction([a, b, result], s => [s[0], s[1], read(s[0], s[1])],
            new Accounting(true, new Unchanged(0), new Unchanged(1)));

    /// <summary>A move writes its roles and every change is paid by another
    /// role, so the source is used up; even a delivery's receipt tally is paid,
    /// each receipt closing one unit of order. A read writes only its result
    /// and nothing pays for it, so the inputs are kept.</summary>
    [Fact]
    public void UsedUpOrKeptIsReadFromTheTableAndItsAccounting()
    {
        Assert.Equal([3, 3], Fate(Flows.Move(Capacity, Capacity)));
        Assert.Equal([3, 3], Fate(Flows.Convert(3, 2, 6, 6)));
        Assert.Equal([3, 3, 3, 3], Fate(Flows.Fulfill(Capacity, Capacity, Capacity, tallyCapacity: Capacity)));

        Assert.Equal([0, 0, 1], Fate(Counts.Sum(Capacity, Capacity)));
        Assert.Equal([0, 0, 1], Fate(Counts.Shortfall(Capacity, Capacity)));
        Assert.Equal([0, 0, 1], Fate(Counts.AtLeast(Capacity, Capacity)));
        Assert.Equal([0, 0, 0, 1], Fate(Counts.Tally(3)));
        Assert.Equal([0, 1], Fate(Counts.Accumulate(Capacity)));
        Assert.Equal([0, 0, 0, 0, 1, 1], Fate(Counts.Rank));
    }

    /// <summary>Join, tear out and cancel are one law, the one-unit move. They
    /// differ only in the face the target binding fills: a count, or its room,
    /// where filling the room empties the count. Tear out and cancel differ
    /// only in whose room: the value's or the debt's. One law, one account:
    /// value plus pressure minus debt never changes under any of them.</summary>
    [Fact]
    public void JoinTearOutAndCancelAreOneMoveBoundToDifferentFaces()
    {
        static Tension Site(int value, int pressure, int debt)
        {
            var store = new Store(Capacity, 1);
            var field = new Field(Shape.Unary(Capacity), 0, 1, 2);
            field.Write(store, 0, value);
            var site = new Tension(store, field, Capacity);
            for (var i = 0; i < pressure; i++) Assert.True(site.Receive(Here).Blocked.None);
            for (var i = 0; i < debt; i++) Assert.True(site.Demand(Here).Blocked.None);
            return site;
        }
        static (int Value, int Plus, int Minus) Read(Tension t) =>
            (t.Value.Read(t.Store, 0)!.Value, t.Plus.Read(t.Store, 0)!.Value, t.Minus.Read(t.Store, 0)!.Value);

        var site = Site(0, 0, 0);
        Wiring join = site.Absorb(Here), tearOut = site.Draw(Here), cancel = site.Cancel(Here);
        Assert.Same(join.Law, tearOut.Law);
        Assert.Same(join.Law, cancel.Law);
        Assert.True(join.Law.SameTable(Flows.Move(Capacity, Capacity)));

        static int[] Faces(Wiring wiring) => [.. wiring.Bindings.Select(b => b.Surface is Flipped ? 1 : 0)];
        Assert.Equal([0, 0], Faces(join));
        Assert.Equal([0, 1], Faces(tearOut));
        Assert.Equal([0, 1], Faces(cancel));
        Assert.Equal(site.Value.Planes.Reverse(), tearOut.Bindings[1].Planes);
        Assert.Equal(site.Minus.Planes.Reverse(), cancel.Bindings[1].Planes);

        // One step of each, from pressure, a held value with debt, and pressure meeting debt.
        (Func<Tension, Wiring> Act, Tension Site)[] runs =
            [(t => t.Absorb(Here), Site(0, 1, 0)), (t => t.Draw(Here), Site(1, 0, 1)), (t => t.Cancel(Here), Site(0, 1, 1))];
        foreach (var (act, at) in runs)
        {
            var before = Read(at);
            new Engine().Add(act(at)).Step();
            var after = Read(at);
            Assert.Equal(before.Value + before.Plus - before.Minus, after.Value + after.Plus - after.Minus);
        }
        Assert.Equal([(1, 0, 0), (0, 0, 0), (0, 0, 0)], runs.Select(r => Read(r.Site)));
    }

    /// <summary>A move along trades one unit for one. A move across trades at
    /// a rate, here three cases for two items, read from the table itself.</summary>
    [Fact]
    public void AMoveAlongTradesOneForOneAndAMoveAcrossAtARate()
    {
        Assert.Equal([(1, 1)], Trades(Flows.Move(Capacity, Capacity), 0, 1));
        Assert.Equal([(3, 2)], Trades(Flows.Convert(3, 2, 6, 6), 0, 1));
    }

    /// <summary>A read along has no corners, at any rate. A read across has a
    /// corner of one at every cell, and the product is the sum of its corners:
    /// each is a unit present only when both inputs are. At bits, AND is that
    /// one corner, and XOR's corner is −2, nothing once wrapped: XOR is the sum
    /// along, wrapped.</summary>
    [Fact]
    public void AReadAlongHasNoCornersAndAReadAcrossHasOneAtEveryCell()
    {
        Assert.All(Corners(Counts.Sum(Capacity, Capacity), 2, Capacity, Capacity).Cast<int>(), c => Assert.Equal(0, c));
        Assert.All(Corners(Counts.Sum(Capacity, Capacity, weight: 2), 2, Capacity, Capacity).Cast<int>(), c => Assert.Equal(0, c));

        var unary = Shape.Unary(Capacity);
        var product = Reading(unary, unary, Shape.Unary(Capacity * Capacity), (a, b) => a * b);
        var corners = Corners(product, 2, Capacity, Capacity);
        Assert.All(corners.Cast<int>(), c => Assert.Equal(1, c));
        Assert.Equal(Next(product, Capacity, Capacity, 0)[2], corners.Cast<int>().Sum());

        Assert.Equal(1, Corners(Reading(Shape.Bit, Shape.Bit, Shape.Bit, (a, b) => a & b), 2, 1, 1)[0, 0]);
        Assert.Equal(-2, Corners(Reading(Shape.Bit, Shape.Bit, Shape.Bit, (a, b) => a ^ b), 2, 1, 1)[0, 0]);
    }

    /// <summary>Compare is heap's own table read backward: bind one part and
    /// the total after, and the relation gives the other part. Where no part
    /// fits, the read is empty and the shortfall folds to zero; the fold is
    /// the only corner, on the diagonal where have meets want. The other face,
    /// the excess, is the same law bound the other way round, and between them
    /// the two faces carry the whole difference, at most one of them nonzero.</summary>
    [Fact]
    public void CompareIsHeapsTableReadBackwardAndFoldedAtZero()
    {
        var shortfall = Counts.Shortfall(Capacity, Capacity);
        var heap = Counts.Sum(Capacity, Capacity).AsRelation().AdmittedTuples();
        for (var have = 0; have <= Capacity; have++)
            for (var want = 0; want <= Capacity; want++)
            {
                // Roles before (a, b, total), then after: bind a and the total after, read b back.
                int[] back = [.. heap.Where(t => t[0] == have && t[5] == want).Select(t => t[1]).Distinct()];
                Assert.Equal(want >= have ? [want - have] : [], back);

                var gap = Next(shortfall, have, want, 0)[2];
                var excess = Next(shortfall, want, have, 0)[2];
                Assert.Equal(back.Length == 0 ? 0 : back[0], gap);
                Assert.Equal(want - have, gap - excess);
                Assert.True(gap == 0 || excess == 0);
            }

        var corners = Corners(shortfall, 2, Capacity, Capacity);
        for (var have = 0; have < Capacity; have++)
            for (var want = 0; want < Capacity; want++)
                Assert.Equal(have == want ? -1 : 0, corners[have, want]);
    }

    /// <summary>Accumulate is heap with its total written over the count it
    /// read, folded at capacity. A threshold is compare folded to a bit: at
    /// least when nothing is short.</summary>
    [Fact]
    public void AccumulateIsHeapBoundInPlaceAndAThresholdIsCompareFoldedToABit()
    {
        var heap = Counts.Sum(Capacity, 1);
        var accumulate = Counts.Accumulate(Capacity);
        for (var signal = 0; signal <= 1; signal++)
            for (var count = 0; count <= Capacity; count++)
                {
                    var total = Next(heap, count, signal, 0)[2];
                    Assert.Equal(total > Capacity ? Capacity : total, Next(accumulate, signal, count)[1]);
                }

        var shortfall = Counts.Shortfall(Capacity, Capacity);
        var atLeast = Counts.AtLeast(Capacity, Capacity);
        for (var count = 0; count <= Capacity; count++)
            for (var threshold = 0; threshold <= Capacity; threshold++)
                Assert.Equal(Next(shortfall, count, threshold, 0)[2] == 0 ? 1 : 0, Next(atLeast, count, threshold, 0)[2]);
    }
}
