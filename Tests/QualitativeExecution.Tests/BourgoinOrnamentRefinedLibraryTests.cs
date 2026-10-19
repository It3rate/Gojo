using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class BourgoinOrnamentRefinedLibraryTests
{
    private static readonly Lazy<IReadOnlyList<BourgoinOrnamentRefinedLibrary.RefinedDesign>> Designs =
        new(BourgoinOrnamentRefinedLibrary.Create);

    [Fact]
    public void EverySourceHasThreeDistinctNativeReadingsWithTheSameActualSkeleton()
    {
        var designs = Designs.Value; Assert.Equal(108, designs.Count);
        Assert.Equal(36, designs.GroupBy(item => item.Design.SourceId).Count());
        foreach (var group in designs.GroupBy(item => item.Design.SourceId))
        {
            var variants = group.ToArray(); Assert.Equal(3, variants.Length);
            Assert.Equal(new[] { "field", "motif", "detail" }, variants.Select(item => item.Design.Mode));
            var source = variants[0].Design.Source; var signatures = new List<string>();
            foreach (var variant in variants)
            {
                var design = variant.Design; var enrichment = design.Enrichment;
                Assert.Same(source, design.Source); Assert.True(enrichment.IsComplete, design.Id);
                Assert.Same(Assert.Single(E(source.Motifs)), enrichment.SourceSurface);
                var original = E(enrichment.Source.Paths);
                foreach (var path in original)
                    Assert.Contains(E(enrichment.Result.Paths), actual => ReferenceEquals(path, actual));
                var introduced = E(enrichment.Result.Paths).Where(path => !original.Any(actual => ReferenceEquals(path, actual))).Cast<Relation>().ToArray();
                Assert.NotEmpty(introduced);
                signatures.Add(string.Join("|", introduced.Select(path => Signature(source.Frame, path)).Order(StringComparer.Ordinal)));
                foreach (var layer in E(enrichment.Layers).Cast<Relation>())
                {
                    Assert.True(enrichment.TryReadLayer(layer, out var definition, out _, out _, out _, out _));
                    var focus = enrichment.Focus(definition.Left); Assert.True(focus.IsComplete);
                    Assert.Same(layer, Assert.Single(E(focus.Selected))); Assert.Equal(2, E(focus.Complement).Length);
                }
                var chords = E(enrichment.Result.Paths).Cast<Relation>().Sum(path => E((Relation)path.Right).Length);
                Assert.True(chords is >= 1 and <= 200, $"{design.Id}: {chords} chords");
            }
            Assert.True(signatures.Distinct(StringComparer.Ordinal).Count() == 3, group.Key + " modes must add distinct geometry");
        }
    }

    [Fact]
    public void CurvedPresentationSelectsActualIntroducedPathsAndPinsActualLeafRootsAndTips()
    {
        foreach (var variant in Designs.Value)
        {
            var design = variant.Design; var enrichment = design.Enrichment;
            var hint = Assert.IsType<Relation>(Assert.Single(E(variant.CurveHints)));
            Assert.True(OrnamentCurveSelection.TryRead(design.Source.Frame, enrichment.ResultSurface, hint, out var curves), design.Id);
            Assert.Same(enrichment.ResultSurface, curves.SourceSurface);
            foreach (var selected in E(curves.Selected).Cast<Relation>())
            {
                Assert.Contains(E(enrichment.Result.Paths), path => ReferenceEquals(selected, path));
                Assert.DoesNotContain(E(enrichment.Source.Paths), path => ReferenceEquals(selected, path));
                if (E((Relation)selected.Right).Length == 6)
                {
                    // The six-chord lens has actual root and pointed tip roles.
                    Assert.Contains(0, curves.FixedLandmarks(selected)); Assert.Contains(3, curves.FixedLandmarks(selected));
                }
            }
            foreach (var port in E(curves.FixedPorts))
                Assert.Contains(E(enrichment.Result.Ports), actual => ReferenceEquals(port, actual));
            Assert.Equal(E(enrichment.Result.Paths).Length, E(curves.Selected).Length + E(curves.Complement).Length);
            AssertNoNumeric(enrichment.Surface); AssertNoNumeric(variant.CurveHints);
        }
    }

    [Theory]
    [InlineData("B02")]
    [InlineData("B05")]
    [InlineData("B07")]
    [InlineData("B08")]
    [InlineData("B15")]
    [InlineData("B26")]
    [InlineData("B32")]
    public void ArchivedNativeVariantAndCurveSelectionsExecuteWithoutTheAuthoringLibrary(string id)
    {
        var variant = Designs.Value.Single(item => item.Design.SourceId == id && item.Design.Mode == "detail");
        var design = variant.Design; var source = design.Source;
        var adapter = new OrnamentEnrichmentProgram(source.Program, source.Bindings, source.SeamContract, design.Enrichment);
        var phase = (Relation)source.Window.Left;
        var json = adapter.Save(OrnamentProgram.Window(phase.Left, phase.Right, 1, 1), variant.CurveHints);
        Assert.True(OrnamentFieldPacket.TryLoad(json, out var packet, out var pending)); Assert.Empty(E(pending));
        var actual = Assert.IsType<Relation>(Assert.Single(E(packet.Program.Motifs)));
        Assert.True(OrnamentEnrichment.TryReadEnrichment(packet.Program.Frame, actual, out var imported));
        var hint = Assert.IsType<Relation>(Assert.Single(E(Assert.IsType<Relation>(packet.PresentationHints))));
        Assert.True(OrnamentCurveSelection.TryRead(packet.Program.Frame, actual, hint, out var curves));
        foreach (var path in E(curves.Selected))
            Assert.Contains(E(imported.Result.Paths), actualPath => ReferenceEquals(actualPath, path));
        foreach (var port in E(curves.FixedPorts))
            Assert.Contains(E(imported.Result.Ports), actualPort => ReferenceEquals(actualPort, port));
        var field = new OrnamentProgramField(packet.Program.Generate(packet.Bindings, packet.Window), packet.SeamContract);
        Assert.True(field.IsComplete); Assert.Empty(E(field.Blocked)); Assert.Empty(E(field.Unresolved));
        Assert.Equal(E(imported.Result.Paths).Cast<Relation>().Sum(path => E((Relation)path.Right).Length), E(field.Graph.Strokes).Length);
        AssertNoNumeric(packet.Source);
    }

    [Fact]
    public void BasketOrnamentsStayInsideTheirOwnQuarterRelatedParentStrip()
    {
        foreach (var variant in Designs.Value.Where(item => item.Design.SourceId == "B02"))
        {
            var design = variant.Design; var enrichment = design.Enrichment; var frame = design.Source.Frame;
            foreach (var layer in E(enrichment.Layers).Cast<Relation>())
            {
                Assert.True(enrichment.TryReadLayer(layer, out _, out _, out _, out _, out var attachments));
                foreach (var receipt in E(attachments).Cast<Relation>())
                {
                    var attachment = (Relation)receipt.Left;
                    var parent = (Relation)((Relation)((Relation)attachment.Left).Right).Left;
                    Assert.Contains(E(enrichment.Source.ClosedPaths), path => ReferenceEquals(parent, path));
                    var added = (Relation)((Relation)attachment.Right).Right;
                    var bounds = Points(frame, parent);
                    var cx = (bounds.Min(point => point.X) + bounds.Max(point => point.X)) / 2;
                    var cy = (bounds.Min(point => point.Y) + bounds.Max(point => point.Y)) / 2;
                    var vertical = bounds.Max(point => point.Y) - bounds.Min(point => point.Y) > bounds.Max(point => point.X) - bounds.Min(point => point.X);
                    var width = design.Mode == "field" ? 5 : 4;
                    foreach (var point in Points(frame, added))
                    {
                        Assert.InRange(point.X - cx, vertical ? -width : -12, vertical ? width : 12);
                        Assert.InRange(point.Y - cy, vertical ? -12 : -width, vertical ? 12 : width);
                    }
                }
            }
        }
    }

    [Fact]
    public void AllHexagonalReadingsStayInsideTheActualHexagon()
    {
        foreach (var variant in Designs.Value.Where(item => item.Design.SourceId == "B05"))
            foreach (var path in E(variant.Design.Enrichment.Result.Paths).Cast<Relation>())
                foreach (var point in Points(variant.Design.Source.Frame, path))
                {
                    Assert.InRange(point.X, -16, 16); Assert.InRange(point.Y, -16, 16);
                    Assert.InRange(point.X + point.Y, -16, 16);
                }
    }

    [Theory]
    [InlineData("B08")]
    [InlineData("B08.B")]
    public void RibbonLeavesHaveOneCommonRootAndOpposedInteriorsInsteadOfCrossedBases(string id)
    {
        var design = Designs.Value.Single(item => item.Design.SourceId == id && item.Design.Mode == "motif").Design;
        var enrichment = design.Enrichment; var original = E(enrichment.Source.Paths);
        var leaves = E(enrichment.Result.ClosedPaths).Where(path => !original.Any(actual => ReferenceEquals(path, actual)))
            .Cast<Relation>().Where(path => E((Relation)path.Right).Length == 6).ToArray();
        Assert.Equal(2, leaves.Length);
        var first = Points(design.Source.Frame, leaves[0]); var second = Points(design.Source.Frame, leaves[1]);
        Assert.Equal(first[0], second[0]); Assert.Equal((0, 40), first[0]);
        // In the native strip basis u=(x+2y)/4, one leaf occupies
        // u>=20 and its sibling u<=20. Equality is only their root.
        Assert.All(first.Skip(1).SkipLast(1), point => Assert.True(point.X + 2 * point.Y > 80));
        Assert.All(second.Skip(1).SkipLast(1), point => Assert.True(point.X + 2 * point.Y < 80));
    }

    [Fact]
    public void PairedCloudLeavesOccupyOppositeSidesOfTheirSharedActualRoot()
    {
        var design = Designs.Value.Single(item => item.Design.SourceId == "B30" && item.Design.Mode == "motif").Design;
        var original = E(design.Enrichment.Source.Paths);
        var leaves = E(design.Enrichment.Result.ClosedPaths).Cast<Relation>()
            .Where(path => !original.Any(actual => ReferenceEquals(actual, path)) && E((Relation)path.Right).Length == 6)
            .Select(path => Points(design.Source.Frame, path)).GroupBy(points => points[0]).ToArray();
        Assert.Equal(4, leaves.Length);
        foreach (var pair in leaves)
        {
            var siblings = pair.ToArray(); Assert.Equal(2, siblings.Length); var root = pair.Key;
            Assert.All(siblings[0].Skip(1).SkipLast(1), point => Assert.True(point.X < root.X));
            Assert.All(siblings[1].Skip(1).SkipLast(1), point => Assert.True(point.X > root.X));
        }
    }

    [Fact]
    public void LotusFansStayInsideTheirOwnActualShellCompartment()
    {
        foreach (var variant in Designs.Value.Where(item => item.Design.SourceId == "B28"))
        {
            var enrichment = variant.Design.Enrichment; var frame = variant.Design.Source.Frame;
            var shells = E(enrichment.Source.ClosedPaths).Cast<Relation>().Select(path => Points(frame, path)).ToArray();
            var original = E(enrichment.Source.Paths);
            foreach (var path in E(enrichment.Result.Paths).Cast<Relation>().Where(path => !original.Any(actual => ReferenceEquals(path, actual))))
                Assert.Contains(shells, shell => Points(frame, path).All(point => InConvex(shell, point)));
        }
        static bool InConvex((int X, int Y)[] polygon, (int X, int Y) point)
        {
            var signs = Enumerable.Range(1, polygon.Length - 1).Select(index =>
            {
                var a = polygon[index - 1]; var b = polygon[index];
                return Math.Sign((long)(b.X - a.X) * (point.Y - a.Y) - (long)(b.Y - a.Y) * (point.X - a.X));
            }).Where(sign => sign != 0).Distinct().ToArray();
            return signs.Length <= 1;
        }
    }

    private static string Signature(QualitativeMotionFrame frame, Relation path) =>
        string.Join("/", new[] { (Relation)path.Left }.Concat(E((Relation)path.Right).Cast<Relation>()).Select(word =>
            string.Join("", E(word).Cast<Relation>().Select(move =>
            {
                Assert.True(frame.Direction(move, out var axis, out var forward, out _));
                return ReferenceEquals(axis, frame.Horizontal) ? forward ? "R" : "L" : forward ? "U" : "D";
            }))));

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
