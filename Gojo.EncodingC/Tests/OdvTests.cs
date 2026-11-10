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
        Assert.Equal("{R+0}", Words(Odv.Add(W("R+0"), W("R+0"), threshold: 1)));
        Assert.Equal("{R+0, R+?}", Words(Odv.Add(W("R+0"), W("R+0"), threshold: 2)));
        Assert.Equal("{R+?}", Words(Odv.Add(W("R+?"), W("R+?"))));
        Assert.Equal(0UL, Odv.Add(W("R+?"), W("L+?")));
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
        var support = new Supports(structure, Odv.WordField);
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
}
