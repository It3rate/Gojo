using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class ContourPatternTests
{
    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void SharedPartsAndSuccessiveChordsEstablishRegularPolygonFamily(int parts)
    {
        var contour = RepetitionExamples.Contour(parts, true, true);
        var read = contour.Read();
        Assert.True(read.Circle);
        Assert.True(read.RegularPolygon);
        Assert.Equal(parts == 4, read.Square);
        Assert.Equal(parts, read.Parts);
    }

    [Fact]
    public void CircleNeedsNoPolygonOrCoordinates()
    {
        var contour = RepetitionExamples.Contour(1, true, false);
        Assert.True(contour.Read().Circle);
        Assert.False(contour.Read().JoinedChords);
        Assert.Equal(new[] { 1, 2, 32 }, contour.ArcRow(0));
        Assert.Equal(new byte[] { 0x42, 0 }, contour.ClosureBytes());
        Assert.NotEmpty(contour.WholeBytes());
    }

    [Fact]
    public void AlternatingSharedPartsGiveRectangleWithoutClaimingSquare()
    {
        var read = RepetitionExamples.Contour(4, false, true).Read();
        Assert.True(read.Rectangle);
        Assert.False(read.RegularPolygon);
        Assert.False(read.Square);
    }

    [Fact]
    public void FourIndependentPartsDoNotEstablishRectangle()
    {
        var contour = New();
        int[] sites = [1, 5, 6, 7, 2];
        for (var i = 0; i < 4; i++)
        {
            contour.Arc(sites[i], sites[i + 1], 32 + i);
            contour.Chord(sites[i], i == 3 ? 1 : sites[i + 1]);
        }
        contour.Close();
        Assert.True(contour.Read().Circle);
        Assert.True(contour.Read().JoinedChords);
        Assert.False(contour.Read().Rectangle);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 3)]
    [InlineData(0, 0)]
    public void RadiusReadsPreserveDegenerateUncertainAndMissingCases(byte radius, byte expected)
    {
        var contour = New(radius);
        contour.Arc(1, 2, 32);
        contour.Close();
        Assert.Equal((byte?)expected, contour.Read().NondegeneracySupport);
        Assert.Equal(1, contour.PartCount);
    }

    [Fact]
    public void OpenOrWrongSeamDoesNotEstablishCircle()
    {
        var open = New();
        open.Arc(1, 2, 32);
        Assert.Null(open.Read().NondegeneracySupport);
        open.Close(5);
        Assert.Null(open.Read().NondegeneracySupport);
    }

    [Fact]
    public void UnconnectedOrRepeatedIntermediateSitesDoNotEstablishWholeTraversal()
    {
        var unconnected = New();
        unconnected.Arc(1, 5, 32);
        unconnected.Arc(6, 2, 32);
        unconnected.Close();
        Assert.False(unconnected.Read().Circle);
        var repeated = New();
        repeated.Arc(1, 5, 32);
        repeated.Arc(5, 6, 32);
        repeated.Arc(6, 5, 32);
        repeated.Arc(5, 2, 32);
        repeated.Close();
        Assert.False(repeated.Read().Circle);
    }

    [Fact]
    public void PartSlotsCannotAlsoBeGeometricSites()
    {
        var contour = New();
        contour.Arc(1, 5, 32);
        var before = contour.ArcBytes();
        Assert.Throws<ArgumentException>(() => contour.Arc(5, 32, 33));
        Assert.Equal(before, contour.ArcBytes());
    }

    [Fact]
    public void SkippingAMarkerIsNotTheSelectedSuccessiveChordPolygon()
    {
        var contour = RepetitionExamples.Contour(5, true, false);
        contour.Chord(1, 6);
        contour.Chord(6, 8);
        contour.Chord(8, 5);
        contour.Chord(5, 7);
        contour.Chord(7, 1);
        Assert.True(contour.Read().Circle);
        Assert.False(contour.Read().JoinedChords);
        Assert.False(contour.Read().RegularPolygon);
    }

    [Fact]
    public void PentagonBytesRetainActualEndToStartBinding()
    {
        var contour = RepetitionExamples.Contour(5, true, true);
        Assert.Equal("41011606681CE08180880002", Convert.ToHexString(contour.ArcBytes()));
        Assert.Equal("4200", Convert.ToHexString(contour.ClosureBytes()));
        Assert.Equal("415118C671204800", Convert.ToHexString(contour.ChordBytes()));
    }

    private static ContourPattern New(byte radius = 2) => new(new Observation(radius), new Observation(2));
    private sealed class Observation(byte support) : IPrimitiveReadPort
    {
        public byte ReadSupport() => support;
    }
}
