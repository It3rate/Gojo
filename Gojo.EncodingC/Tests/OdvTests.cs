using Xunit;

namespace Gojo.EncodingC.Tests;

public sealed class OdvTests
{
    private static OdvWord W(string name) => OdvNotation.Parse(name);

    private static string Words(ulong support) => OdvNotation.Describe(support);

    [Fact]
    public void EightWordsPackIntoThreeBytesWithNoHeader()
    {
        var words = Enumerable.Range(0, 8).Select(c => new OdvWord((byte)c)).ToArray();

        var packed = Odv.Pack(words);

        Assert.Equal(new byte[] { 0x88, 0xC6, 0xFA }, packed);
        Assert.All(Enumerable.Range(0, 8), i => Assert.Equal(words[i], Odv.At(packed, i)));
    }

    [Fact]
    public void UnderTheDetectionContractUndetectableOperandsStillCount()
    {
        Assert.Equal("{R+0, R+?}", Words(Odv.Add(W("R+0"), W("R+0"))));
        Assert.Equal("{R+0}", Words(Odv.Add(W("R+0"), W("R+0"), Odv.ExactZero)));
        Assert.Equal("{R+?}", Words(Odv.Add(W("R+?"), W("R+?"))));
        Assert.Equal(0UL, Odv.Add(W("R+?"), W("L+?")));
    }

    /// <summary>Detection is one contract. A reference reader here enumerates
    /// hidden integers at each threshold, and every threshold from 2 up gives
    /// the detection table; every pair of thresholds in the same order gives the
    /// same correspondence. So each table is built once, and a huge threshold
    /// costs nothing and cannot overflow. The exact-zero table is a different
    /// contract that a reader has to ask for.</summary>
    [Fact]
    public void DetectionIsOneContractWhateverTheThreshold()
    {
        for (var threshold = 1; threshold <= 7; threshold++)
            Assert.True(Odv.Addition(threshold).SameTable(ReferenceAddition(threshold)));
        Assert.True(Odv.Addition(int.MaxValue).SameTable(Odv.Addition()));
        Assert.False(Odv.Addition(Odv.ExactZero).SameTable(Odv.Addition()));

        for (var from = 1; from <= 6; from++)
            for (var to = 1; to <= 6; to++)
                Assert.True(Odv.ThresholdCorrespondence(from, to).SameTable(ReferenceCorrespondence(from, to)));
        Assert.True(Odv.ThresholdCorrespondence(int.MaxValue, 9).SameTable(Odv.ThresholdCorrespondence(3, 2)));
    }

    /// <summary>The words a hidden integer reads as: detectable at the
    /// threshold, otherwise its sign, and both directions for zero.</summary>
    private static int[] WordsOf(int hidden, int threshold) =>
        hidden >= threshold ? [0b01] : hidden <= -threshold ? [0b11] : hidden > 0 ? [0b00] : hidden < 0 ? [0b10] : [0b00, 0b10];

    private static RelationLaw ReferenceAddition(int threshold)
    {
        var admitted = new HashSet<(int, int, int)>();
        var reach = 3 * threshold + 1;
        for (var o = 0; o <= 4; o += 4)
            for (var x = -reach; x <= reach; x++)
                for (var y = -reach; y <= reach; y++)
                    foreach (var a in WordsOf(x, threshold))
                        foreach (var b in WordsOf(y, threshold))
                        {
                            var results = x + y != 0 ? WordsOf(x + y, threshold)
                                : (a & 0b10) == (b & 0b10) ? [a & 0b10]
                                : [0b00, 0b10];
                            foreach (var r in results) admitted.Add((a | o, b | o, r | o));
                        }
        return RelationLaw.FromPredicate([Odv.Word, Odv.Word, Odv.Word], t => admitted.Contains((t[0], t[1], t[2])));
    }

    private static RelationLaw ReferenceCorrespondence(int from, int to)
    {
        var admitted = new HashSet<(int, int)>();
        var reach = 3 * Math.Max(from, to) + 1;
        for (var o = 0; o <= 4; o += 4)
            for (var x = -reach; x <= reach; x++)
                foreach (var a in WordsOf(x, from))
                    foreach (var b in x == 0 ? [a & 0b10] : WordsOf(x, to))
                        admitted.Add((a | o, b | o));
        return RelationLaw.FromPredicate([Odv.Word, Odv.Word], t => admitted.Contains((t[0], t[1])));
    }

    [Fact]
    public void OnePositionReadTwiceTakesOneValue()
    {
        var law = Odv.Addition();
        var either = (1UL << W("R+?").Code) | (1UL << W("R-?").Code);

        var apart = law.Project(2, [either, either, 0xFF]);
        var same = law.Project(2, [either, either, 0xFF], [0, 0, 1]);

        Assert.Equal("{R+0, R+?, R-0, R-?}", Words(apart));
        Assert.Equal("{R+?, R-?}", Words(same));
    }

    [Fact]
    public void ReorientingAStructureIsOneConstantBitAndOneXor()
    {
        var structure = Odv.Create(3);
        Odv.Write(structure, 0, W("R+?"));
        Odv.Write(structure, 1, W("R-0"));
        Odv.Write(structure, 2, W("R+0"));

        Odv.Reorient(structure);

        Assert.Equal(new[] { "L-?", "L+0", "L-0" }, Enumerable.Range(0, 3).Select(i => Odv.Read(structure, i).ToString()));
        Assert.Throws<ArgumentException>(() => Odv.Write(structure, 0, W("R+0")));

        // A reader facing the other way can read it back in its own terms, if it wants to.
        Assert.Equal("{R+?}", Words(Odv.ReadIn(positiveLeft: false).Project(1, [1UL << W("L-?").Code, 0xFF])));
    }

    [Fact]
    public void AdditionNarrowsAHundredRowsInOnePass()
    {
        const int rows = 100;
        var structure = Odv.Create(rows * 3);
        var sum = new Wiring(Odv.Addition(),
            new Binding(structure, Odv.V, Odv.D, Odv.O),
            new Binding(structure, Odv.V, Odv.D, Odv.O),
            new Binding(structure, Odv.V, Odv.D, Odv.O));
        var support = Supports.On(structure, Odv.WordField);
        for (var i = 0; i < rows; i++)
        {
            sum.Add(3 * i, 3 * i + 1, 3 * i + 2);
            Odv.Write(structure, 3 * i, i % 2 == 0 ? W("R+?") : W("R+0"));
            Odv.Write(structure, 3 * i + 1, i % 2 == 0 ? W("R+?") : W("R-0"));
            support.Observe(3 * i);
            support.Observe(3 * i + 1);
        }

        Supports.Narrow(sum, support, support, support);

        for (var i = 0; i < rows; i++)
            Assert.Equal(i % 2 == 0 ? "{R+?}" : "{R+0, R-0}", Words(support.Values(3 * i + 2)));
    }

    [Fact]
    public void AThresholdCalibrationNeverTurnsAnUndetectableWordIntoAnExactZero()
    {
        var detectionToExact = Odv.ThresholdCorrespondence(2, 1);

        Assert.Equal("{R+0, R+?}", Words(detectionToExact.Project(1, [1UL << W("R+0").Code, 0xFF])));
        Assert.Equal("{R+0}", Words(Odv.ThresholdCorrespondence(1, 1).Project(1, [1UL << W("R+0").Code, 0xFF])));
    }

    /// <summary>A write whose orientation does not match the structure's is
    /// refused before any bit changes, so V and D are not left half written.</summary>
    [Fact]
    public void AWordFacingTheOtherWayIsRefusedBeforeAnyBitChanges()
    {
        var words = Odv.Create(1);
        Odv.Write(words, 0, W("R+0"));

        Assert.Throws<ArgumentException>(() => Odv.Write(words, 0, W("L-?")));

        Assert.Equal("R+0", Odv.Read(words, 0).ToString());
    }
}
