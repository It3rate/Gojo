using Xunit;

namespace Gojo.EncodingB.Tests;

public sealed class OdvTests
{
    private static string Read(ulong support) => OdvWord.Domain.Describe(support);

    private static string Set(params string[] names) =>
        Read(names.Aggregate(0UL, (m, n) => m | Bits.Of(OdvWord.Parse(n).Code)));

    [Fact]
    public void WordBitsAreOrientationDirectionValueHighToLow()
    {
        var word = new OdvWord(0b001);
        Assert.Equal("R+?", word.Name);
        Assert.False(word.PositiveLeft);
        Assert.False(word.Negative);
        Assert.True(word.Detectable);
        Assert.Equal("L-0", new OdvWord(0b110).Name);
    }

    [Fact]
    public void ReverseMovesTheThingAndReorientMovesTheReader()
    {
        var word = OdvWord.Parse("R+?");
        Assert.Equal(0b011, word.Reverse().Code);
        Assert.Equal(0b111, word.Reorient().Code);
        Assert.Equal(OdvWord.Parse("R+0").Reorient(), OdvWord.Parse("L-0"));
        Assert.Equal(word, word.Reorient().Reorient());
    }

    [Fact]
    public void AllEightWordsPackIntoThreeBytesWithNoHeader()
    {
        var words = Enumerable.Range(0, 8).Select(i => new OdvWord((byte)i)).ToArray();
        var packed = Odv.Pack(words);
        Assert.Equal([0x88, 0xC6, 0xFA], packed);
        for (var i = 0; i < 8; i++) Assert.Equal(words[i], Odv.At(packed, i));
    }

    [Theory]
    [InlineData("R")]
    [InlineData("L")]
    public void ExactZeroContractMatchesTheHistoricalTable(string o)
    {
        // The PrimitiveMeasure README table, row + column, for one orientation.
        var table = new Dictionary<(string, string), string[]>
        {
            [("+0", "+0")] = ["+0"], [("+0", "+?")] = ["+?"], [("+0", "-0")] = ["+0", "-0"], [("+0", "-?")] = ["-?"],
            [("+?", "+0")] = ["+?"], [("+?", "+?")] = ["+?"], [("+?", "-0")] = ["+?"], [("+?", "-?")] = ["+?", "-?", "+0", "-0"],
            [("-0", "+0")] = ["+0", "-0"], [("-0", "+?")] = ["+?"], [("-0", "-0")] = ["-0"], [("-0", "-?")] = ["-?"],
            [("-?", "+0")] = ["-?"], [("-?", "+?")] = ["+?", "-?", "+0", "-0"], [("-?", "-0")] = ["-?"], [("-?", "-?")] = ["-?"],
        };
        foreach (var ((a, b), expected) in table)
            Assert.Equal(Set(expected.Select(e => o + e).ToArray()), Read(Odv.Add(OdvWord.Parse(o + a), OdvWord.Parse(o + b))));
    }

    [Fact]
    public void AcrossOrientationsNothingIsAdmittedAndNoZeroIsManufactured()
    {
        Assert.Equal(0UL, Odv.Add(OdvWord.Parse("R+?"), OdvWord.Parse("L+?")));
        Assert.Null(OdvWord.Compare(OdvWord.Parse("R+?"), OdvWord.Parse("L+?")));
        Assert.Equal(1, OdvWord.Compare(OdvWord.Parse("R+?"), OdvWord.Parse("R-0")));
        Assert.Equal(0, OdvWord.Compare(OdvWord.Parse("R+0"), OdvWord.Parse("R-0")));
    }

    [Fact]
    public void EqualWordsDoNotCancel()
    {
        Assert.Equal(Set("R+?", "R-?", "R+0", "R-0"), Read(Odv.Subtract(OdvWord.Parse("R+?"), OdvWord.Parse("R+?"))));
    }

    [Fact]
    public void OneSiteUsedTwiceAdmitsOnlyDiagonalTuples()
    {
        var law = Odv.Addition();
        var either = Bits.Of(OdvWord.Parse("R+?").Code) | Bits.Of(OdvWord.Parse("R-?").Code);
        // Two independent sites that may each be +? or -?: any sum.
        Assert.Equal(Set("R+?", "R-?", "R+0", "R-0"), Read(law.Project(2, [either, either, OdvWord.Domain.Full])));
        // The same site read twice: X + X keeps X's word, so zero is excluded.
        Assert.Equal(Set("R+?", "R-?"), Read(law.Project(2, [either, either, OdvWord.Domain.Full], [7, 7, 9])));
        Assert.Equal(Set("R+?"), Read(Odv.AddSameReference(OdvWord.Parse("R+?"))));
    }

    [Fact]
    public void DetectionContractWidensExactlyTheUndetectableRows()
    {
        var widened = new Dictionary<(string, string), string[]>
        {
            [("+0", "+0")] = ["+0", "+?"],
            [("-0", "-0")] = ["-0", "-?"],
            [("+0", "-?")] = ["-0", "-?"],
            [("-?", "+0")] = ["-0", "-?"],
            [("-0", "+?")] = ["+0", "+?"],
            [("+?", "-0")] = ["+0", "+?"],
        };
        string[] words = ["+0", "+?", "-0", "-?"];
        foreach (var o in new[] { "R", "L" })
            foreach (var a in words)
                foreach (var b in words)
                {
                    var exact = Odv.Add(OdvWord.Parse(o + a), OdvWord.Parse(o + b), threshold: 1);
                    var detection = Odv.Add(OdvWord.Parse(o + a), OdvWord.Parse(o + b), threshold: 2);
                    if (widened.TryGetValue((a, b), out var expected))
                    {
                        Assert.Equal(Set(expected.Select(e => o + e).ToArray()), Read(detection));
                        Assert.Equal(exact, exact & detection);
                    }
                    else Assert.Equal(Read(exact), Read(detection));
                }
    }

    [Fact]
    public void AnyThresholdAboveOneIsTheSameDetectionTable()
    {
        Assert.Equal(Odv.Addition(2).ContentHash, Odv.Addition(5).ContentHash);
        Assert.NotEqual(Odv.Addition(1).ContentHash, Odv.Addition(2).ContentHash);
        Assert.Contains("exact-zero", Odv.Addition(1).Name);
    }

    [Fact]
    public void ReorientingATileFlipsTheBasisAndEveryDirectionBit()
    {
        var tile = new Tile("words", Odv.Grammar, 3);
        tile.SeedCode("word", 0, OdvWord.Parse("R+?").Code);
        tile.SeedCode("word", 1, OdvWord.Parse("R-0").Code);
        tile.SeedCode("word", 2, OdvWord.Parse("R+0").Code);
        var epoch = tile.Epoch;
        Odv.ReorientTile(tile);
        Assert.Equal(["L-?", "L+0", "L-0"], Enumerable.Range(0, 3).Select(s => OdvWord.Domain.NameOf(tile.Code("word", s))));
        Assert.True(tile.Basis("O"));
        Assert.Equal(epoch + 1, tile.Epoch);
        Odv.Reverse(tile, Bits.Of(0));
        Assert.Equal("L+?", OdvWord.Domain.NameOf(tile.Code("word", 0)));
    }
}
