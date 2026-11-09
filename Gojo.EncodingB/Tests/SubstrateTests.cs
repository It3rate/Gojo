using Xunit;

namespace Gojo.EncodingB.Tests;

public sealed class SubstrateTests
{
    [Fact]
    public void EveryBitOfEveryPlaneDecodesFromItsAddress()
    {
        var tile = new Tile("words", Odv.Grammar, 4, ["a", "b", "c", "d"]);
        tile.SeedCode("word", 1, OdvWord.Parse("R-?").Code);
        var meanings = BitDecoder.DecodeAll(tile);
        Assert.Equal(Odv.Grammar.Planes.Count * Bits.Width, meanings.Count);
        Assert.All(meanings, m => Assert.False(string.IsNullOrWhiteSpace(m.Meaning)));
        Assert.Contains("which reads R-?", BitDecoder.Explain(tile, "D", 1).Meaning);
        Assert.Contains("negative", BitDecoder.Explain(tile, "D", 1).Meaning);
        Assert.Contains("declared padding", BitDecoder.Explain(tile, "V", 40).Meaning);
        Assert.Contains("supplied by the tile basis", BitDecoder.Explain(tile, "O", 0).Meaning);
        Assert.Empty(tile.Validate());
    }

    [Fact]
    public void ThermometerFieldsDecodeAsMagnitudes()
    {
        var tile = new Tile("gaps", TensionField.Grammar(4), 2);
        tile.SeedMagnitude("M", 0, 3);
        Assert.Contains("reads 3 of 4", BitDecoder.Explain(tile, "M2", 0).Meaning);
        Assert.Contains("greater than 2", BitDecoder.Explain(tile, "M2", 0).Meaning);
        tile.Seed("M3", Bits.Of(1));
        Assert.Null(tile.Magnitude("M", 1));
        Assert.Contains("not a prefix", BitDecoder.Explain(tile, "M3", 1).Meaning);
        Assert.Single(tile.Validate());
    }

    [Fact]
    public void LawBitsDecodeAsWholeTransitions()
    {
        var handoff = BitDecoder.Explain(Passage.Law, 3 + 8 * 6);
        Assert.True(handoff.Value);
        Assert.Contains("[S=occupied, C=open, T=empty] -> [S=empty, C=open, T=occupied]: admitted", handoff.Meaning);
        var merge = BitDecoder.Explain(Passage.Law, 5 + 8 * 7);
        Assert.False(merge.Value);
        Assert.Contains("not admitted", merge.Meaning);
    }

    [Fact]
    public void EpochCountsStructureAndVersionCountsEveryChange()
    {
        var tile = new Tile("p", Passage.Grammar, 3);
        var (epoch, version) = (tile.Epoch, tile.Version);
        tile.Supply("open", 1, true);
        Assert.Equal(epoch, tile.Epoch);
        Assert.Equal(version + 1, tile.Version);
        tile.AddRowSet("edges").Add(0, 1);
        Assert.True(tile.Epoch > epoch);
    }

    [Fact]
    public void SupplyingEvidenceIsNotMovingMaterial()
    {
        var tile = new Tile("p", Passage.Grammar, 3);
        Assert.Throws<InvalidOperationException>(() => tile.Supply("occupied", 0, true));
    }

    [Fact]
    public void RowsKeepDirectionAndReversalIsARead()
    {
        var tile = new Tile("house", Passage.Grammar, 3);
        var edges = tile.AddRowSet("edges");
        edges.Add(0, 2);
        Assert.Equal([2, 0], edges.Traverse(0, reversed: true));
        Assert.Equal([0, 2], edges.Row(0));
        edges.Insert(0, 1, 1);
        Assert.Equal([0, 1, 2], edges.Row(0));
    }

    [Fact]
    public void SnapshotsRestoreStateAndRows()
    {
        var tile = new Tile("p", Passage.Grammar, 3);
        tile.AddRowSet("edges").Add(0, 1);
        tile.Seed("occupied", "101");
        var snapshot = tile.Snapshot();
        tile.Seed("occupied", "010");
        tile.Rows("edges").Add(1, 2);
        tile.Restore(snapshot);
        Assert.Equal("101", Bits.Format(tile.Read("occupied"), 3));
        Assert.Equal(1, tile.Rows("edges").Count);
    }
}
