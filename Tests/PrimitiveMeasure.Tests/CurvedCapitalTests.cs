namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class CurvedCapitalTests
{
    private static CurveInk Ink(string name) => CurvedCapitals.Ink(CurvedCapitals.Names.ToList().IndexOf(name));

    [Fact]
    public void Opposed_ends_and_two_perpendicular_attachments_leave_the_interior_open()
    {
        var rounded = Ink("D");
        var angular = Ink("D · corners");
        var radial = Ink("D · half of O");
        var weak = Ink("D · ends only");
        foreach (var ink in new[] { rounded, angular, radial, weak })
        {
            Assert.Equal(0, ink.CrosswiseStem());
            Assert.Equal(new PenRead(1,PenHeading.Right,PenHeading.Left,3,null), ink.Read(false));
            Assert.True(ink.Read(false).RequiresHeadingChange);
        }
        Assert.Equal(rounded.Read(false), angular.Read(false));
        Assert.Equal((2,0), ((int)rounded.Read().BendSupport, rounded.Read().JointTurns!.Value));
        Assert.Equal((1,2), ((int)angular.Read().BendSupport, angular.Read().JointTurns!.Value));
        Assert.Equal(rounded.Read(), radial.Read());
        Assert.Equal(weak.Read(false), weak.Read());
        Assert.Equal(new[] { "D","D · corners","D · half of O","P","R" }, CurvedCapitals.Compatible(weak.Read()));
        Assert.DoesNotContain("D · corners",CurvedCapitals.Compatible(rounded.Read()));
        Assert.Equal(rounded.Catalogue.EdgeBytes(), weak.Catalogue.EdgeBytes());
        Assert.Equal(new byte[]{0x50}, rounded.DriveBytes());
        Assert.Equal(new byte[]{0}, weak.DriveBytes());
    }

    [Fact]
    public void Same_open_ink_read_preserves_three_distinct_histories()
    {
        var direct = Ink("C");
        var cut = Ink("C · gap in O");
        var claim = Ink("C · closure sought");
        Assert.Equal(direct.Read(), cut.Read());
        Assert.Equal(direct.Read(), claim.Read());
        Assert.Equal(new[]{"C · gap in O"}, CurvedCapitals.Compatible(cut.Read(),cut.ReadOrigin()));
        Assert.Equal(new[]{"C · closure sought"}, CurvedCapitals.Compatible(claim.Read(),claim.ReadOrigin()));
        Assert.Contains("G", CurvedCapitals.Compatible(direct.Read(),direct.ReadOrigin()));
        Assert.Null(direct.SourceRead);
        Assert.Equal(0, cut.Read().Loops);
        Assert.True(cut.Views.IsClosed(2));
        Assert.False(cut.Views.IsClosed(1));
        Assert.Equal(new[]{2,1}, Assert.Single(cut.RungLinks()));
        Assert.Equal(2, cut.ArcLinks().Length);
        Assert.Equal(new byte[]{6},cut.Catalogue.EdgeBytes());
        Assert.Null(direct.ClosureGoalMet());
        Assert.False(claim.ClosureGoalMet());
        Assert.Equal(direct.Catalogue.EdgeBytes(),claim.Catalogue.EdgeBytes());
        Assert.Equal(1,Ink("Straight · closure sought").Read().BendSupport);
        Assert.False(Ink("Straight · closure sought").ClosureGoalMet());
    }

    [Fact]
    public void One_radial_whole_can_supply_ink_or_be_partitioned_without_chords()
    {
        var o = Ink("O");
        Assert.True(o.SourceRead!.Value.Circle);
        Assert.Equal(1,o.SourceRead.Value.Parts);
        Assert.Equal(1,o.InkCount);
        Assert.Empty(o.DriveLinks());
        Assert.Equal(1,o.Read().Loops);
        Assert.False(o.Read().RequiresHeadingChange);
        Assert.Equal(2,o.Read().BendSupport);
        var half = Ink("D · half of O");
        Assert.True(half.SourceRead!.Value.EqualParts);
        Assert.True(half.ClockwiseContour());
        Assert.False(Ink("C · gap in O").ClockwiseContour());
        Assert.Null(Ink("C").ClockwiseContour());
        Assert.False(Ink("C · gap in O").SourceRead!.Value.EqualParts);
        Assert.False(half.SourceRead.Value.JoinedChords);
        Assert.True(half.Views.IsClosed(0));
        Assert.False(half.Views.IsClosed(1));
    }

    [Theory]
    [InlineData("B",2)] [InlineData("C",0)] [InlineData("D",1)] [InlineData("G",0)]
    [InlineData("J",0)] [InlineData("O",1)] [InlineData("P",1)] [InlineData("Q",1)]
    [InlineData("R",1)] [InlineData("S",0)] [InlineData("U",0)]
    public void Each_new_letter_has_its_expected_incidence_and_turn_evidence(string name,int loops)
    {
        var ink = Ink(name);
        Assert.Equal(loops,ink.Read().Loops);
        Assert.Equal(2,ink.Read().BendSupport);
        Assert.Contains(name,CurvedCapitals.Compatible(ink.Read()));
        if (name == "S") Assert.Equal(new[]{3,1},ink.DriveLinks().Select(row=>row[1]));
    }
}
