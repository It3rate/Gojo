using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class OrnamentDetailLibraryTests
{
    private static readonly Lazy<IReadOnlyList<OrnamentDetailLibrary.Study>> Studies = new(OrnamentDetailLibrary.Create);
    public static IEnumerable<object[]> StudyIds() => Enumerable.Range(1, 14).Select(index => new object[] { $"D{index:00}" });

    [Theory]
    [MemberData(nameof(StudyIds))]
    public void EveryDetailKeepsItsActualGuideAndThreeCompleteNativeLayerReads(string id)
    {
        var study = Studies.Value.Single(study => study.Id == id); var design = study.Design; var enrichment = design.Enrichment;
        Assert.True(enrichment.IsComplete);
        Assert.Same(Assert.Single(E(design.Source.Motifs)), enrichment.SourceSurface);
        foreach (var path in E(enrichment.Source.Paths))
            Assert.Contains(E(enrichment.Result.Paths), actual => ReferenceEquals(actual, path));
        Assert.Equal(3, E(enrichment.Layers).Length);
        foreach (var layer in E(enrichment.Layers).Cast<Relation>())
        {
            Assert.True(enrichment.TryReadLayer(layer, out var definition, out _, out _, out _, out _));
            var focus = enrichment.Focus(definition.Left); Assert.True(focus.IsComplete);
            Assert.Same(layer, Assert.Single(E(focus.Selected))); Assert.Equal(2, E(focus.Complement).Length);
        }
        var chords = E(enrichment.Result.Paths).Cast<Relation>().Sum(path => E((Relation)path.Right).Length);
        Assert.InRange(chords, 1, 200);
        AssertNoNumeric(enrichment.Surface);
    }

    [Theory]
    [InlineData("D01")]
    [InlineData("D04")]
    [InlineData("D09")]
    [InlineData("D10")]
    [InlineData("D11")]
    [InlineData("D13")]
    public void DetailExecutesFromItsImportedNativeProgramWithoutTheAuthoringFactory(string id)
    {
        var study = Studies.Value.Single(study => study.Id == id); var design = study.Design;
        var source = design.Source; var adapter = new OrnamentEnrichmentProgram(source.Program, source.Bindings, source.SeamContract, design.Enrichment);
        var initial = (Relation)source.Window.Left;
        Assert.True(OrnamentFieldPacket.TryLoad(adapter.Save(OrnamentProgram.Window(initial.Left, initial.Right, 1, 1)),
            out var packet, out var pending)); Assert.Empty(E(pending));
        var actual = Assert.IsType<Relation>(Assert.Single(E(packet.Program.Motifs)));
        Assert.True(OrnamentEnrichment.TryReadEnrichment(packet.Program.Frame, actual, out var imported));
        Assert.True(imported.IsComplete);
        var field = new OrnamentProgramField(packet.Program.Generate(packet.Bindings, packet.Window), packet.SeamContract);
        Assert.True(field.IsComplete); Assert.Empty(E(field.Blocked)); Assert.Empty(E(field.Unresolved));
        var chordCount = E(imported.Result.Paths).Cast<Relation>().Sum(path => E((Relation)path.Right).Length);
        Assert.Equal(chordCount, E(field.Graph.Strokes).Length);
        AssertNoNumeric(packet.Source);
    }

    [Theory]
    [InlineData("D11")]
    [InlineData("D12")]
    public void BasketDetailsStayInsideTheirOwnActualParentStripAndLeaveTheSquareGapsOpen(string id)
    {
        var study = Studies.Value.Single(study => study.Id == id); var enrichment = study.Design.Enrichment; var frame = study.Design.Source.Frame;
        foreach (var layer in E(enrichment.Layers).Cast<Relation>())
        {
            Assert.True(enrichment.TryReadLayer(layer, out _, out _, out _, out _, out var attachments));
            foreach (var receipt in E(attachments).Cast<Relation>())
            {
                var attachment = Assert.IsType<Relation>(receipt.Left);
                var parentPort = Assert.IsType<Relation>(attachment.Left);
                var parentSelector = Assert.IsType<Relation>(parentPort.Right);
                var parent = Assert.IsType<Relation>(parentSelector.Left);
                Assert.Contains(E(enrichment.Source.ClosedPaths), actual => ReferenceEquals(actual, parent));
                var added = Assert.IsType<Relation>(Assert.IsType<Relation>(attachment.Right).Right);
                var parentPoints = Points(frame, parent);
                var cx = (parentPoints.Min(point => point.X) + parentPoints.Max(point => point.X)) / 2;
                var cy = (parentPoints.Min(point => point.Y) + parentPoints.Max(point => point.Y)) / 2;
                var vertical = parentPoints.Max(point => point.Y) - parentPoints.Min(point => point.Y) >
                    parentPoints.Max(point => point.X) - parentPoints.Min(point => point.X);
                foreach (var point in Points(frame, added))
                {
                    Assert.InRange(point.X - cx, vertical ? -4 : -12, vertical ? 4 : 12);
                    Assert.InRange(point.Y - cy, vertical ? -12 : -4, vertical ? 12 : 4);
                }
            }
        }
    }

    [Theory]
    [InlineData("D13")]
    [InlineData("D14")]
    public void HexagonalDetailRemainsInsideTheActualSixSideBoundary(string id)
    {
        var study = Studies.Value.Single(study => study.Id == id); var enrichment = study.Design.Enrichment;
        foreach (var path in E(enrichment.Result.Paths).Cast<Relation>())
            foreach (var point in Points(study.Design.Source.Frame, path))
            {
                Assert.InRange(point.X, -16, 16); Assert.InRange(point.Y, -16, 16);
                Assert.InRange(point.X + point.Y, -16, 16);
            }
    }

    [Fact]
    public void QuatrefoilCompartmentsFollowActualDiagonalGuidesAndStayInsideItsLobes()
    {
        var study = Studies.Value.Single(study => study.Id == "D10"); var enrichment = study.Design.Enrichment;
        var frame = study.Design.Source.Frame;
        var sourcePaths = E(enrichment.Source.Paths);
        var diagonalGuides = sourcePaths.Cast<Relation>().Where(path =>
        {
            var points = Points(frame, path);
            return points[0] == (0, 0) && Math.Abs(points[^1].X) == 8 && Math.Abs(points[^1].Y) == 8;
        }).ToArray();
        Assert.Equal(4, diagonalGuides.Length);
        var additions = E(enrichment.Result.Paths).Where(path => !sourcePaths.Any(actual => ReferenceEquals(actual, path))).Cast<Relation>().ToArray();
        foreach (var added in additions)
            foreach (var point in Points(frame, added))
            {
                // The source's core square lies inside all four lobes; its
                // narrow axis waists are at8, not the former petal tip10.
                Assert.InRange(point.X, -8, 8); Assert.InRange(point.Y, -8, 8);
            }
        var compartments = additions.Where(path => E((Relation)path.Right).Length == 6).ToArray();
        Assert.Equal(4, compartments.Length);
        foreach (var path in compartments)
        {
            var points = Points(frame, path);
            var guide = Assert.Single(diagonalGuides, candidate => Points(frame, candidate)[1] == points[0]);
            Assert.Contains(Points(frame, guide)[^1], points);
        }
    }

    [Fact]
    public void CompassPetalsBeginAtTheirActualOuterNeckRolesAndEndAtTheirGuideTips()
    {
        var study = Studies.Value.Single(study => study.Id == "D03"); var enrichment = study.Design.Enrichment;
        var frame = study.Design.Source.Frame; var petals = 0;
        foreach (var layer in E(enrichment.Layers).Cast<Relation>())
        {
            Assert.True(enrichment.TryReadLayer(layer, out _, out _, out _, out _, out var attachments));
            foreach (var receipt in E(attachments).Cast<Relation>())
            {
                var attachment = (Relation)receipt.Left;
                var added = (Relation)((Relation)attachment.Right).Right;
                if (!E(enrichment.Result.ClosedPaths).Any(path => ReferenceEquals(path, added)) || E((Relation)added.Right).Length != 10) continue;
                var port = (Relation)attachment.Left; var actualGuide = (Relation)((Relation)port.Right).Left;
                var guide = Points(frame, actualGuide); var points = Points(frame, added);
                Assert.Equal(guide[1], points[0]); Assert.Contains(guide[^1], points);
                Assert.Equal(2, Math.Max(Math.Abs(points[0].X), Math.Abs(points[0].Y)));
                petals++;
            }
        }
        Assert.Equal(8, petals);
    }

    [Fact]
    public void RunningVineBendsAwayFromTheEmergingLeafShoulders()
    {
        var study = Studies.Value.Single(study => study.Id == "D02");
        var path = Assert.IsType<Relation>(E(study.Design.Enrichment.Source.Paths)[0]);
        Assert.Equal(new[] { (0, -12), (0, -8), (-1, -4), (0, 0), (1, 4), (0, 8), (0, 14) },
            Points(study.Design.Source.Frame, path));
    }

    [Fact]
    public void CornerCurlKeepsItsRootAndAvoidsTheParentAndRightLeafRegion()
    {
        var study = Studies.Value.Single(study => study.Id == "D07");
        var paths = E(study.Design.Enrichment.Source.Paths).Cast<Relation>().ToArray();
        var curl = Points(study.Design.Source.Frame, paths[^1]);
        Assert.Equal((1, -1), curl[0]); Assert.Equal((3, 1), curl[^1]);
        Assert.All(curl, point => Assert.InRange(point.X, 1, 5));
        var parent = Points(study.Design.Source.Frame, paths[0]);
        for (var i = 1; i < curl.Length; i++)
            for (var j = 1; j < parent.Length; j++)
            {
                var a = curl[i - 1]; var b = curl[i]; var c = parent[j - 1]; var d = parent[j];
                // No proper crossing remains. Coincidence at the exact
                // authored root is retained and is checked separately.
                Assert.False(Cross(a, b, c) * Cross(a, b, d) < 0 &&
                    Cross(c, d, a) * Cross(c, d, b) < 0);
            }
        Assert.Contains(curl[0], parent);
        static long Cross((int X, int Y) a, (int X, int Y) b, (int X, int Y) p) =>
            (long)(b.X - a.X) * (p.Y - a.Y) - (long)(b.Y - a.Y) * (p.X - a.X);
    }

    private static (int X, int Y)[] Points(QualitativeMotionFrame frame, Relation path)
    {
        var cursor = Word((Relation)path.Left); var result = new List<(int X, int Y)> { cursor };
        foreach (var chord in E((Relation)path.Right).Cast<Relation>())
        { var step = Word(chord); cursor = (cursor.X + step.X, cursor.Y + step.Y); result.Add(cursor); }
        return result.ToArray();
        (int X, int Y) Word(Relation word)
        {
            var x = 0; var y = 0;
            foreach (var move in E(word).Cast<Relation>())
            {
                Assert.True(frame.Direction(move, out var axis, out var forward, out _));
                if (ReferenceEquals(axis, frame.Horizontal)) x += forward ? 1 : -1; else y += forward ? 1 : -1;
            }
            return (x, y);
        }
    }

    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static void AssertNoNumeric(IElement source)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(source);
        while (pending.TryPop(out var element))
        {
            if (!seen.Add(element)) continue;
            Assert.False(element is Carrier or CarrierLocation);
            if (element is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }
}
