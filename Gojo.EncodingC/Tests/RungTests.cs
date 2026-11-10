using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Rungs: live views with a layer that holds only what changed,
/// selection coarse to fine and by use, and the ways a rung is left.</summary>
public sealed class RungTests
{
    /// <summary>A passage chain over six positions: plane 0 holds a unit, plane 1
    /// a gate. Rows (0, 1, 2) and (2, 3, 4). A unit at 0; gates 1 and 3 open.</summary>
    private static Structure Chain()
    {
        var chain = new Structure(2, 6);
        chain.Write(0, 0, true);
        chain.Write(1, 1, true);
        chain.Write(1, 3, true);
        return chain;
    }

    /// <summary>Passage rows (u, u + 1, u + 2) for each u given, on any surface.</summary>
    private static Wiring Passages(Surface surface, params int[] starts)
    {
        var rows = new Wiring(Flows.Passage, new Binding(surface, 0), new Binding(surface, 1), new Binding(surface, 0));
        foreach (var u in starts) rows.Add(u, u + 1, u + 2);
        return rows;
    }

    private static Wiring One(TransitionLaw law, Surface surface)
    {
        var wiring = new Wiring(law, new Binding(surface, Enumerable.Range(0, law.Roles[0].Bits).ToArray()));
        wiring.Add(0);
        return wiring;
    }

    [Fact]
    public void ARungReadsItsSourceLiveAndCopiesOnlyWhatItWrites()
    {
        var chain = Chain();
        var rung = new Rung(chain).SelectAll();
        Assert.True(rung.Held.None);

        chain.Write(0, 5, true);
        Assert.True(rung.Read(0, 5));

        rung.Apply(Passages(rung, 0, 2));

        Assert.True(rung.Read(0, 4));
        Assert.True(chain.Read(0, 0));
        Assert.False(chain.Read(0, 4));
        Assert.Equal(Plane.Of(6, 0, 2, 4), rung.Held);
        Assert.Equal(Plane.Of(6, 0, 4), rung.Changes());

        var commit = rung.Commit();

        Assert.True(commit.Committed);
        Assert.Equal(Plane.Of(6, 0, 4), commit.Written);
        Assert.Equal((false, true), (chain.Read(0, 0), chain.Read(0, 4)));
        Assert.True(rung.Held.None);
    }

    /// <summary>A commit checks everything it would overwrite before writing
    /// anything. A position the source changed or retired since it was copied
    /// is stale, and a stale commit writes nothing. What has not moved can go
    /// back on its own; what has can be reverted and read live again.</summary>
    [Fact]
    public void ACommitWritesNothingWhenAnythingItWouldOverwriteHasMoved()
    {
        var source = new Structure(1, 3);
        var rung = new Rung(source).SelectAll();
        rung.Write(0, 0, true);
        rung.Write(0, 1, true);
        rung.Write(0, 2, true);
        source.Write(0, 1, true);
        source.Retire(2);

        var refused = rung.Commit();

        Assert.False(refused.Committed);
        Assert.Equal(Plane.Of(3, 1, 2), refused.Stale);
        Assert.False(source.Read(0, 0));

        Assert.True(rung.Commit(Plane.Of(3, 0)).Committed);
        Assert.True(source.Read(0, 0));

        source.Write(0, 1, false);
        rung.Revert(Plane.Of(3, 1));
        Assert.False(rung.Read(0, 1));
        Assert.Equal(Plane.Of(3, 2), rung.Held);
    }

    /// <summary>A passage changes two positions at once. Committing one of them
    /// would make or lose a unit, so it is refused and the partner is named.
    /// A change made on its own can go back on its own.</summary>
    [Fact]
    public void ChangesMadeTogetherCommitTogether()
    {
        var chain = Chain();
        var rung = new Rung(chain).SelectAll();
        rung.Apply(Passages(rung, 0));
        rung.Write(1, 5, true);

        var half = rung.Commit(Plane.Of(6, 2));

        Assert.False(half.Committed);
        Assert.Equal(Plane.Of(6, 0), half.Split);
        Assert.False(chain.Read(0, 2));

        Assert.True(rung.Commit(Plane.Of(6, 5)).Committed);
        Assert.True(chain.Read(1, 5));
        Assert.Equal(Plane.Of(6, 0, 2), rung.Held);

        Assert.True(rung.Commit(Plane.Of(6, 0, 2)).Committed);
        Assert.Equal((false, true), (chain.Read(0, 0), chain.Read(0, 2)));
    }

    /// <summary>Three layers select ten positions: all of it, but not the second
    /// half, except one detail. Nobody lists positions. "All" follows the source
    /// as it grows.</summary>
    [Fact]
    public void SelectionIsCoarseThenFine()
    {
        var source = new Structure(1, 10);
        var rung = new Rung(source).SelectAll().Ignore(Plane.Parse("0000011111")).Select(Plane.Of(10, 7));

        Assert.Equal(Plane.Parse("1111100100"), rung.Selected);
        Assert.Equal(Plane.Parse("0000011011"), rung.Ignored);

        source.Add(2);

        Assert.Equal(Plane.Parse("111110010011"), rung.Selected);
    }

    /// <summary>A line of passages; only position 0 is asked for, and use may
    /// reach two steps. The first row brings in its gate and target, the next
    /// one more, and the unit runs to position 4. The row after that would
    /// reach too far and is reported instead. Asking for position 4 lets
    /// attention move on. An ignored gate stops use at it.</summary>
    [Fact]
    public void UsingAPositionBringsItInUpToTheReach()
    {
        var line = new Structure(2, 13);
        line.Write(0, 0, true);
        for (var gate = 1; gate < 13; gate += 2) line.Write(1, gate, true);
        var rung = new Rung(line, reach: 2).Select(Plane.Of(13, 0));
        var rows = Passages(rung, 0, 2, 4, 6, 8, 10);

        var first = rung.Apply(rows);

        Assert.Equal(Plane.Of(13, 1, 2, 3, 4), first.Brought);
        Assert.Equal(Plane.Of(6, 2), first.Beyond);
        Assert.True(rung.Read(0, 4));
        Assert.Equal((1, 0), (rung.ReachLeft(2), rung.ReachLeft(4)));
        Assert.True(line.Read(0, 0));

        rung.Select(Plane.Of(13, 4));
        var second = rung.Apply(rows);

        Assert.Equal(Plane.Of(6, 4), second.Beyond);
        Assert.True(rung.Read(0, 8));

        var blinkered = new Rung(line, reach: 3).Select(Plane.Of(13, 0)).Ignore(Plane.Of(13, 3));
        var stopped = blinkered.Apply(Passages(blinkered, 0, 2, 4));

        Assert.Equal(Plane.Of(3, 1), stopped.Beyond);
        Assert.True(blinkered.Read(0, 2));
    }

    /// <summary>Two rungs over one chain suppose different things: one leaves
    /// the second gate open, the other shuts it. Compared, they differ where
    /// the suppositions led apart. A rung over a rung sees its parent's
    /// changes live, and commits into the parent, not the chain.</summary>
    [Fact]
    public void SiblingRungsAreTwoSuppositionsFromOneStart()
    {
        var chain = Chain();
        var open = new Rung(chain).SelectAll();
        var shut = new Rung(chain).SelectAll();
        shut.Write(1, 3, false);

        open.Apply(Passages(open, 0, 2));
        shut.Apply(Passages(shut, 0, 2));

        Assert.True(open.Read(0, 4));
        Assert.True(shut.Read(0, 2));
        Assert.Equal(Plane.Of(6, 2, 3, 4), open.Differs(shut));

        var child = new Rung(open).SelectAll();
        Assert.True(child.Read(0, 4));
        child.Write(0, 5, true);

        Assert.True(child.Commit().Committed);
        Assert.True(open.Read(0, 5));
        Assert.False(chain.Read(0, 5));
    }

    /// <summary>Every word of an ODV structure shares its orientation bit, so a
    /// rung reads it by reference and cannot turn it, by a write or by a law.
    /// When the structure turns, it turns as a whole and the rung sees it.</summary>
    [Fact]
    public void ARungReadsConstantsByReferenceAndCannotTurnThem()
    {
        var words = Odv.Create(2);
        Odv.Write(words, 0, OdvNotation.Parse("R+?"));
        var rung = new Rung(words).SelectAll();

        Assert.Throws<InvalidOperationException>(() => rung.Write(Odv.O, 0, true));
        Assert.Throws<ArgumentException>(() => new Wiring(Flows.Toggle, new Binding(rung, Odv.O)));

        Odv.Reorient(words);

        Assert.True(rung.Read(Odv.O, 1));
        Assert.Equal("L-?", Odv.Read(words, 0).ToString());
        Assert.True(rung.Read(Odv.D, 0));
    }

    /// <summary>Two rungs predict a counter's next value: one stretches it by
    /// two, the other adds one. Then the counter doubles. The rung whose law
    /// matched reads the same as the source; the other misses at the counter.
    /// Neither is committed: a prediction is compared, not written back.</summary>
    [Fact]
    public void APredictionIsARungComparedWithWhatTheSourceThenDid()
    {
        var counter = new Structure(8, 1);
        var count = new Field(Shape.Unary(8), Enumerable.Range(0, 8).ToArray());
        count.Write(counter, 0, 2);
        var stretches = TransitionLaw.FromFunction([Shape.Unary(8)], s => [Math.Min(8, 2 * s[0])], new Accounting(true));
        var adds = TransitionLaw.FromFunction([Shape.Unary(8)], s => [Math.Min(8, s[0] + 1)], new Accounting(true));
        var byTwo = new Rung(counter).SelectAll();
        var byOne = new Rung(counter).SelectAll();
        byTwo.Apply(One(stretches, byTwo));
        byOne.Apply(One(adds, byOne));

        One(stretches, counter).Apply();

        Assert.Equal(4, count.Read(counter, 0));
        Assert.True(byTwo.Changes().None);
        Assert.Equal(Plane.Of(1, 0), byOne.Changes());
        Assert.Equal(Plane.Of(1, 0), byTwo.Stale());
    }

    /// <summary>An introduced position has no source, is always selected and is
    /// never read as evidence about the source. A commit adopts it. Source
    /// positions added after an introduction come into view on Follow.</summary>
    [Fact]
    public void AnIntroducedPositionIsACandidateUntilACommitAdoptsIt()
    {
        var source = new Structure(1, 2);
        var rung = new Rung(source).SelectAll();
        var candidate = rung.Introduce();

        Assert.Equal(2, candidate);
        Assert.Null(rung.SourceOf(candidate));
        Assert.True(rung.Selected[candidate]);

        rung.Write(0, candidate, true);
        source.Add();
        var commit = rung.Commit();

        var adopted = Assert.Single(commit.Adopted);
        Assert.Equal(3, adopted);
        Assert.True(source.Read(0, 3));
        Assert.Equal(3, rung.SourceOf(candidate));
        Assert.True(rung.Read(0, candidate));

        Assert.Equal(1, rung.Follow());
        Assert.Equal(2, rung.SourceOf(rung.Count - 1));
    }

    [Fact]
    public void SupportsNarrowToAFixedPointAndKeepContradictions()
    {
        var equal = RelationLaw.FromPredicate([Shape.Bit, Shape.Bit], t => t[0] == t[1]);
        (Structure, Wiring, Supports) Line()
        {
            var line = new Structure(1, 4);
            var rows = new Wiring(equal, new Binding(line, 0), new Binding(line, 0));
            rows.Add(0, 1);
            rows.Add(1, 2);
            rows.Add(2, 3);
            return (line, rows, new Supports(line, Field.Bit(0)));
        }

        var (line, rows, support) = Line();
        line.Write(0, 0, true);
        support.Observe(0);
        Assert.Equal(3, support.Open.Count);

        Supports.Propagate(new[] { (rows, new[] { support, support }) });

        Assert.Equal(Plane.Ones(4), support.Settled);
        Assert.Equal(Plane.Ones(4), support.Of(1));

        var (other, otherRows, contradiction) = Line();
        other.Write(0, 0, true);
        contradiction.Observe(0);
        contradiction.Observe(3);

        Supports.Propagate(new[] { (otherRows, new[] { contradiction, contradiction }) });

        Assert.True(contradiction.Empty.Any);
    }

    [Fact]
    public void SelectionsComeFromStructureNotFromNames()
    {
        var line = new Structure(1, 4);
        var equal = RelationLaw.FromPredicate([Shape.Bit, Shape.Bit], t => t[0] == t[1]);
        var rows = new Wiring(equal, new Binding(line, 0), new Binding(line, 0));
        rows.Add(0, 1);
        rows.Add(1, 2);
        rows.Add(2, 3);

        Assert.Equal("101010", Selections.Every(6, 2).ToString());
        Assert.Equal(Plane.Of(4, 0, 1, 2), Selections.Within(Plane.Of(4, 0), 2, rows, line));
    }
}
