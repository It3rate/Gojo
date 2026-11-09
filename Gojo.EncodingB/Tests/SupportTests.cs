using Xunit;

namespace Gojo.EncodingB.Tests;

public sealed class SupportTests
{
    private static ulong Word(string name) => Bits.Of(OdvWord.Parse(name).Code);

    [Fact]
    public void OpenIsNotZeroAndSupportsOnlyNarrowWithinAnEpoch()
    {
        var support = new SupportPlanes("word", OdvWord.Domain, Bits.Low(2));
        Assert.Equal(OdvWord.Domain.Full, support.Values(0));
        Assert.Equal(Bits.Low(2), support.Open);
        support.Supply(0, OdvWord.Parse("R+?").Code);
        Assert.Equal(1, support.SettledValue(0));
        Assert.Throws<InvalidOperationException>(() => support.Reopen(0));
        support.BeginEpoch();
        support.Reopen(0);
        Assert.Equal(OdvWord.Domain.Full, support.Values(0));
    }

    [Fact]
    public void PropagationPredictsASumInsideAWorkspace()
    {
        var tile = new Tile("sum", Odv.Grammar, 3, ["a", "b", "r"]);
        tile.SeedCode("word", 0, OdvWord.Parse("R+?").Code);
        tile.SeedCode("word", 1, OdvWord.Parse("R-?").Code);
        var ws = new TrialWorkspace("reader", tile, SelectionMachines.All(SelectionScope.Sites(tile)));
        Assert.True(ws.Observe("word", 0));
        Assert.True(ws.Observe("word", 1));
        var rows = new LawRows("a+b=r", Odv.Addition(), Odv.Grammar, ["word", "word", "word"], [[0, 1, 2]]);
        var result = ws.Propagate([rows]);
        Assert.True(result.FixedPoint);
        Assert.Empty(result.Contradictions);
        Assert.Equal(Word("R+0") | Word("R+?") | Word("R-0") | Word("R-?"), ws.Support("word").Values(2));
        Assert.Equal(0b011UL, ws.Sup);
        Assert.Equal("000", Bits.Format(tile.Read("V") & 0b100, 3));
    }

    [Fact]
    public void AnEmptiedSupportIsAContradictionOnTheLawThatEmptiedIt()
    {
        var tile = new Tile("sum", Odv.Grammar, 3);
        var ws = new TrialWorkspace("reader", tile, SelectionMachines.All(SelectionScope.Sites(tile)));
        var support = ws.Support("word");
        support.Restrict(0, Word("R+?"));
        support.Restrict(1, Word("L+?"));
        var rows = new LawRows("a+b=r", Odv.Addition(), Odv.Grammar, ["word", "word", "word"], [[0, 1, 2]]);
        var result = ws.Propagate([rows]);
        Assert.NotEmpty(result.Contradictions);
        Assert.All(result.Contradictions, c => Assert.Equal("a+b=r", c.Law));
        Assert.Equal(Bits.Low(3), support.Empty);
    }

    [Fact]
    public void NarrowingReachesTheSameFixedPointInAnyOrder()
    {
        TrialWorkspace Run(bool reversed)
        {
            var tile = new Tile("chain", Odv.Grammar, 5);
            var ws = new TrialWorkspace("w", tile, SelectionMachines.All(SelectionScope.Sites(tile)));
            ws.Support("word").Restrict(0, Word("R+?"));
            ws.Support("word").Restrict(1, Word("R+?") | Word("R+0"));
            ws.Support("word").Restrict(3, Word("R-?"));
            ws.Support("word").Restrict(4, Word("R-?") | Word("R-0"));
            var first = new LawRows("a+b=c", Odv.Addition(), Odv.Grammar, ["word", "word", "word"], [[0, 1, 2]]);
            var second = new LawRows("c+d=e", Odv.Addition(), Odv.Grammar, ["word", "word", "word"], [[2, 3, 4]]);
            ws.Propagate(reversed ? [second, first] : [first, second]);
            return ws;
        }
        var forward = Run(false);
        var backward = Run(true);
        for (var site = 0; site < 5; site++)
            Assert.Equal(forward.Support("word").Values(site), backward.Support("word").Values(site));
        Assert.Equal(Word("R+?"), forward.Support("word").Values(2));
    }

    [Fact]
    public void TheChainShowsWhatEachUncertaintyRungKeeps()
    {
        var p = Passage.Presence;
        var g = Passage.Gate;
        const ulong empty = 0b01, occupied = 0b10, either = 0b11, open = 0b10;

        // U1: one joint per passage, joined only through B's marginal.
        var first = JointSupport.FromMarginals([("A", p, occupied), ("C1", g, either), ("B", p, empty)])
            .ApplyTransition(Passage.Law, "A", "C1", "B");
        var (_, lost) = first.ToMarginals();
        Assert.False(lost.Lossless);
        var second = JointSupport.FromMarginals([("B", p, first.Marginal("B")), ("C2", g, open), ("T", p, empty)])
            .ApplyTransition(Passage.Law, "B", "C2", "T");
        var product = JointSupport.FromMarginals([("A", p, first.Marginal("A")), ("B", p, second.Marginal("B")), ("T", p, second.Marginal("T"))]);
        Assert.Equal(4, product.Count);
        Assert.True(product.Contains(0, 0, 0)); // the item lost
        Assert.True(product.Contains(1, 0, 1)); // the item duplicated

        // U2: the whole chain as one cluster.
        var cluster = JointSupport.FromMarginals([("A", p, occupied), ("C1", g, either), ("B", p, empty), ("C2", g, open), ("T", p, empty)])
            .ApplyTransition(Passage.Law, "A", "C1", "B")
            .ApplyTransition(Passage.Law, "B", "C2", "T")
            .Project("A", "B", "T");
        Assert.Equal(2, cluster.Count);
        Assert.True(cluster.Contains(1, 0, 0));
        Assert.True(cluster.Contains(0, 0, 1));

        // U3: U1 plus the conservation the passage signature implies.
        var start = new Dictionary<string, ulong> { ["A"] = occupied, ["B"] = empty, ["T"] = empty };
        var conservation = UncertaintyReads.Conservation(Passage.Law, [["A", "C1", "B"], ["B", "C2", "T"]], v => start[v]);
        Assert.NotNull(conservation);
        Assert.Equal(["A", "B", "T"], conservation.Variables);
        Assert.Equal(1, conservation.Total);
        var restored = product.Restrict(conservation.Relation, "A", "B", "T");
        Assert.Equal(cluster.States().Select(s => string.Join(",", s)).Order(), restored.States().Select(s => string.Join(",", s)).Order());
        Assert.Contains("derived from accounting signature", conservation.Relation.Provenance);
    }

    [Fact]
    public void ConservationIsNotInventedWhenTheStartIsOpen()
    {
        Assert.Null(UncertaintyReads.Conservation(Passage.Law, [["A", "C", "B"]], v => v == "A" ? 0b11UL : 0b01UL));
    }

    [Fact]
    public void SemiringReadsRankWithoutAConfidenceField()
    {
        var law = Odv.Addition();
        ulong[] supports = [Word("R+?"), Word("R-?"), OdvWord.Domain.Full];
        Assert.Equal(4, UncertaintyReads.CountCompletions(law, supports));
        // Tropical: prefer results that read undetectable.
        var least = UncertaintyReads.MinimumCost(law, supports, (role, value) => role == 2 && new OdvWord((byte)value).Detectable ? 1 : 0);
        Assert.NotNull(least);
        Assert.Equal(0, least.Value.Cost);
        Assert.False(new OdvWord((byte)least.Value.Tuple[2]).Detectable);
        Assert.Null(UncertaintyReads.MinimumCost(law, [Word("R+?"), Word("L+?"), OdvWord.Domain.Full], (_, _) => 0));
        var widest = UncertaintyReads.WidestPath(law, supports, (role, value) => role == 2 ? value : 10);
        Assert.Equal(3, widest!.Value.Capacity);
    }
}
