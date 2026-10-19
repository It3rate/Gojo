using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class LatticeGeometryChangeReadTests
{
    [Fact]
    public void NewWrappersAndStraightSubdivisionDoNotInventChangedSupportOrBends()
    {
        var author = new LatticeLadderAuthor(); var word = author.Word(4, 0);
        var source = author.Unit("long line", LatticeLadderAuthor.Path(S(), word));
        var current = QualitativeLatticeLadder.Begin(source).Refine("four actual chords", S(new Relation(word,
            S(author.Word(1, 0), author.Word(1, 0), author.Word(1, 0), author.Word(1, 0))))).Current.Program;
        var read = LatticeGeometryChangeRead.Read(source, current);
        Assert.True(read.Summary.IsComparable); Assert.False(read.Summary.HasSupportChange);
        Assert.Equal(4, read.Summary.SourceFormalSupport); Assert.Equal(4, read.Summary.RetainedFormalSupport);
        Assert.Equal(0, read.Summary.SymmetricDifferenceRatio); Assert.Equal(0, read.Summary.AddedAuthoredBends);
        Assert.Empty(E(read.RemovedSpans)); Assert.Empty(E(read.IntroducedSpans));
        Assert.Equal(4, E(read.RetainedCorrespondence).Length); AssertNative(read.Surface);
    }

    [Fact]
    public void RealDetourExposesRemovedMiddleNewSupportAndFourNativeBends()
    {
        var author = new LatticeLadderAuthor(); var word = author.Word(4, 0);
        var path = LatticeLadderAuthor.Path(S(), word); var source = author.Unit("straight", path);
        var row = LatticeGeometrySurgery.NotchRow(author.Frame, word, author.Word(1, 0),
            author.Word(0, 1), author.Word(2, 0), author.Word(1, 0));
        var current = QualitativeLatticeLadder.Begin(source).Refine("detour", S(row)).Current.Program;
        var read = LatticeGeometryChangeRead.Read(source, current);
        Assert.True(read.Summary.HasSupportChange);
        Assert.Equal(4, read.Summary.SourceFormalSupport); Assert.Equal(6, read.Summary.ResultFormalSupport);
        Assert.Equal(2, read.Summary.RetainedFormalSupport); Assert.Equal(2, read.Summary.RemovedFormalSupport);
        Assert.Equal(4, read.Summary.AddedFormalSupport); Assert.Equal(.75, read.Summary.SymmetricDifferenceRatio);
        Assert.Equal(4, read.Summary.AddedAuthoredBends); Assert.Equal(0, read.Summary.RemovedAuthoredBends);
        Assert.Contains(path, Reachable(read.RemovedSpans)); Assert.Contains(word, Reachable(read.RemovedSpans));
        Assert.Contains(source.Surface, Reachable(read.Surface)); Assert.Contains(current.Surface, Reachable(read.Surface));
        AssertNative(read.Surface);
    }

    [Fact]
    public void CuttingASpanReportsLossWithoutInventingAddedGeometry()
    {
        var author = new LatticeLadderAuthor(); var word = author.Word(6, 0);
        var path = LatticeLadderAuthor.Path(S(), word); var source = author.Unit("uncut", path);
        var current = LatticeGeometrySurgery.CutChordAndPublish(QualitativeLatticeLadder.Begin(source), "cut middle",
            path, word, author.Word(2, 0), author.Word(2, 0), author.Word(2, 0)).Current.Program;
        var read = LatticeGeometryChangeRead.Read(source, current);
        Assert.Equal(2, read.Summary.RemovedFormalSupport); Assert.Equal(0, read.Summary.AddedFormalSupport);
        Assert.Equal(4, read.Summary.RetainedFormalSupport); Assert.Equal(1d / 3d, read.Summary.SymmetricDifferenceRatio);
        Assert.Equal(0, read.Summary.AddedAuthoredBends); Assert.Single(E(read.RemovedSpans));
        Assert.Empty(E(read.IntroducedSpans)); AssertNative(read.Surface);
    }

    [Fact]
    public void TwoRunsFusedAtADeclaredNewLandmarkReportMovedSupport()
    {
        var author = new LatticeLadderAuthor();
        var first = LatticeLadderAuthor.Path(S(), author.Word(4, 0));
        var second = LatticeLadderAuthor.Path(author.Word(0, 2), author.Word(4, 0));
        var source = author.Unit("two runs", first, second);
        var current = LatticeGeometrySurgery.FuseParallelRuns(QualitativeLatticeLadder.Begin(source), "fusion",
            first, second, author.Word(0, 1)).Current.Program;
        var read = LatticeGeometryChangeRead.Read(source, current);
        Assert.Equal(8, read.Summary.RemovedFormalSupport); Assert.Equal(4, read.Summary.AddedFormalSupport);
        Assert.Equal(0, read.Summary.RetainedFormalSupport); Assert.Equal(1, read.Summary.SymmetricDifferenceRatio);
        Assert.Equal(2, E(read.RemovedSpans).Length); Assert.Single(E(read.IntroducedSpans));
        Assert.Empty(E(read.RetainedCorrespondence)); AssertNative(read.Surface);
    }

    [Fact]
    public void ReversingAnActualCornerDoesNotInventAGeometricBend()
    {
        var author = new LatticeLadderAuthor();
        var path = LatticeLadderAuthor.Path(S(), author.Word(2, 0), author.Word(0, 2));
        var source = author.Unit("forward", path);
        var reversed = author.ReversePath(path, out _); var current = author.Unit("reversed", reversed);
        var read = LatticeGeometryChangeRead.Read(source, current);
        Assert.False(read.Summary.HasSupportChange); Assert.Equal(0, read.Summary.AddedAuthoredBends);
        Assert.Equal(0, read.Summary.RemovedAuthoredBends); Assert.Equal(4, read.Summary.RetainedFormalSupport);
        AssertNative(read.Surface);
    }

    [Fact]
    public void LineToBoxRegroupingChangesAuthoredCornersWhileSupportRemainsTheSame()
    {
        var author = new LatticeLadderAuthor();
        var right = author.Word(2, 0); var up = author.Word(0, 2); var left = author.Word(-2, 0); var down = author.Word(0, -2);
        var sides = new[] { LatticeLadderAuthor.Path(S(), right), LatticeLadderAuthor.Path(right, up),
            LatticeLadderAuthor.Path(author.Word(2, 2), left), LatticeLadderAuthor.Path(up, down) };
        var source = author.Unit("separate lines", sides);
        var box = LatticeLadderAuthor.Path(S(), right, up, left, down);
        var current = new UnitArrangementProgram(author.Frame, "closed box", S(box), S(), S(box));
        var read = LatticeGeometryChangeRead.Read(source, current);
        Assert.False(read.Summary.HasSupportChange); Assert.Equal(0, read.Summary.SymmetricDifferenceRatio);
        Assert.Equal(4, read.Summary.AddedAuthoredBends);
        Assert.Empty(E(read.RemovedSpans)); Assert.Empty(E(read.IntroducedSpans)); AssertNative(read.Surface);
    }

    [Fact]
    public void UnrelatedFramesOrReachIdentitiesRemainOpenInsteadOfClaimingCoincidence()
    {
        var first = new LatticeLadderAuthor(); var source = first.Unit("first", LatticeLadderAuthor.Path(S(), first.Word(2, 0)));
        var otherFrame = new LatticeLadderAuthor(); var foreign = otherFrame.Unit("foreign", LatticeLadderAuthor.Path(S(), otherFrame.Word(2, 0)));
        var read = LatticeGeometryChangeRead.Read(source, foreign);
        Assert.False(read.Summary.IsComparable); Assert.False(read.Summary.HasSupportChange); Assert.NotEmpty(E(read.Open));
        var otherReach = new LatticeLadderAuthor(first.Frame, new Placeholder());
        var uncalibrated = otherReach.Unit("unshared reach", LatticeLadderAuthor.Path(S(), otherReach.Word(2, 0)));
        var reachRead = LatticeGeometryChangeRead.Read(source, uncalibrated);
        Assert.False(reachRead.Summary.IsComparable); Assert.NotEmpty(E(reachRead.Open));
        Assert.Contains(source.Surface, Reachable(reachRead.Surface)); Assert.Contains(uncalibrated.Surface, Reachable(reachRead.Surface));
        AssertNative(reachRead.Surface);
    }

    private static HashSet<IElement> Reachable(IElement source)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(source);
        while (pending.TryPop(out var item))
            if (seen.Add(item) && item is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        return seen;
    }
    private static void AssertNative(IElement root) => Assert.All(Reachable(root), item => Assert.True(item is Relation or Placeholder));
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
