using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class BourgoinOrnamentCompartmentLibraryTests
{
    private static readonly Lazy<IReadOnlyList<BourgoinOrnamentRefinedLibrary.RefinedDesign>> Designs = new(() =>
        new BourgoinPatternLibrary(8).Recipes.Where(source => BourgoinOrnamentCompartmentLibrary.Supports(source.Id))
            .SelectMany((source, index) => BourgoinOrnamentCompartmentLibrary.Create(source, index, ["field", "motif", "detail"])).ToArray());

    [Fact]
    public void RoomFillingVariantsPreserveTheirActualSourceAndHaveNativeCurveAndColourReads()
    {
        Assert.Equal(24, Designs.Value.Count);
        foreach (var variant in Designs.Value)
        {
            var design = variant.Design; var enriched = design.Enrichment;
            Assert.True(enriched.IsComplete, design.Id);
            Assert.Same(Assert.Single(E(design.Source.Motifs)), enriched.SourceSurface);
            foreach (var sourcePath in E(enriched.Source.Paths))
                Assert.Contains(E(enriched.Result.Paths), path => ReferenceEquals(sourcePath, path));
            var layers = E(enriched.Layers).Cast<Relation>().ToArray(); Assert.Equal(3, layers.Length);
            for (var layerIndex = 0; layerIndex < layers.Length; layerIndex++)
            {
                Assert.True(enriched.TryReadLayer(layers[layerIndex], out var definition, out var selected,
                    out var complement, out _, out _));
                if (layerIndex == 0)
                {
                    Assert.Same(design.FieldLayer, definition.Left);
                    Assert.Equal(E(enriched.Source.Paths).Length, E(selected).Length);
                    foreach (var sourcePath in E(enriched.Source.Paths))
                        Assert.Contains(E(selected), path => ReferenceEquals(sourcePath, path));
                    Assert.Empty(E(complement));
                }
                else
                {
                    Assert.Same(layerIndex == 1 ? design.MotifLayer : design.DetailLayer, definition.Left);
                    Assert.Empty(E(selected));
                    foreach (var sourcePath in E(enriched.Source.Paths))
                        Assert.Contains(E(complement), path => ReferenceEquals(sourcePath, path));
                }
            }
            var curves = E(variant.CurveHints).Cast<Relation>().Select(hint =>
                OrnamentCurveSelection.TryRead(design.Source.Frame, enriched.ResultSurface, hint, out var read) ? read : null)
                .Single(read => read is not null)!;
            var colours = E(variant.CurveHints).Cast<Relation>().Select(hint =>
                OrnamentColourSelection.TryRead(design.Source.Frame, enriched.ResultSurface, hint, out var read) ? read : null)
                .Single(read => read is not null)!;
            Assert.True(curves.IsComplete); Assert.True(colours.IsComplete);
            Assert.NotEmpty(E(curves.Selected));
            var chords = E(enriched.Result.Paths).Cast<Relation>().Sum(path => E((Relation)path.Right).Length);
            Assert.InRange(chords, 30, 550);
            AssertNoNumeric(enriched.Surface); AssertNoNumeric(variant.CurveHints);
        }
    }

    [Theory]
    [InlineData("B08")]
    [InlineData("B09")]
    [InlineData("B10")]
    [InlineData("B11")]
    [InlineData("B12")]
    [InlineData("B13")]
    [InlineData("B14")]
    public void ImportedRoomOrnamentExecutesAndHintsRetainTheirActualFinalPathIdentities(string id)
    {
        var variant = Designs.Value.Single(item => item.Design.SourceId == id && item.Design.Mode == "detail");
        var design = variant.Design; var source = design.Source;
        var program = new OrnamentEnrichmentProgram(source.Program, source.Bindings, source.SeamContract, design.Enrichment);
        var phase = (Relation)source.Window.Left;
        var saved = program.Save(OrnamentProgram.Window(phase.Left, phase.Right, 1, 1), variant.CurveHints);
        Assert.True(OrnamentFieldPacket.TryLoad(saved, out var packet, out var pending)); Assert.Empty(E(pending));
        var actual = Assert.IsType<Relation>(Assert.Single(E(packet.Program.Motifs)));
        Assert.True(OrnamentEnrichment.TryReadEnrichment(packet.Program.Frame, actual, out var imported));
        var hints = E(Assert.IsType<Relation>(packet.PresentationHints)).Cast<Relation>().ToArray();
        var colours = hints.Select(hint => OrnamentColourSelection.TryRead(packet.Program.Frame, actual, hint, out var read) ? read : null)
            .Single(read => read is not null)!;
        Assert.True(colours.IsComplete);
        var field = new OrnamentProgramField(packet.Program.Generate(packet.Bindings, packet.Window), packet.SeamContract);
        Assert.True(field.IsComplete); Assert.Empty(E(field.Blocked)); Assert.Empty(E(field.Unresolved));
        Assert.Equal(E(imported.Result.Paths).Cast<Relation>().Sum(path => E((Relation)path.Right).Length), E(field.Graph.Strokes).Length);
        AssertNoNumeric(packet.Source);
    }

    [Fact]
    public void LargeSteppedArmFlowersHaveEightActualPetalContoursAtEveryChamberAndNestedCentres()
    {
        var design = Designs.Value.Single(item => item.Design.SourceId == "B12" && item.Design.Mode == "detail").Design;
        var original = E(design.Enrichment.Source.Paths);
        var additions = E(design.Enrichment.Result.ClosedPaths).Cast<Relation>()
            .Where(path => !original.Any(actual => ReferenceEquals(path, actual))).ToArray();
        var petals = additions.Where(path => E((Relation)path.Right).Length == 6).ToArray();
        Assert.Equal(32, petals.Length);
        // Long diagonal petals are present as real installed words. Their
        // extent greatly exceeds the old tiny seed/edge decoration.
        foreach (var petal in petals)
        {
            var points = Points(design.Source.Frame, petal);
            Assert.True(Math.Max(points.Max(p => p.X) - points.Min(p => p.X), points.Max(p => p.Y) - points.Min(p => p.Y)) >= 17);
        }
        Assert.Equal(8, additions.Length - petals.Length);
    }

    [Theory]
    [InlineData("B10", 16)]
    [InlineData("B11", 32)]
    public void PointedRoomsHaveOneEightPetalFlowerWithFourDominantDiagonalPetals(string id, int expectedPetals)
    {
        var design = Designs.Value.Single(item => item.Design.SourceId == id && item.Design.Mode == "detail").Design;
        var sourcePaths = E(design.Enrichment.Source.Paths);
        var petals = E(design.Enrichment.Result.ClosedPaths).Cast<Relation>()
            .Where(path => !sourcePaths.Any(actual => ReferenceEquals(path, actual)) && E((Relation)path.Right).Length == 6).ToArray();
        Assert.Equal(expectedPetals, petals.Length);
        for (var room = 0; room < expectedPetals / 8; room++)
        {
            var axial = Points(design.Source.Frame, petals[room * 8]);
            var diagonal = Points(design.Source.Frame, petals[room * 8 + 1]);
            var axialReach = Math.Max(Math.Abs(axial[3].X - axial[0].X), Math.Abs(axial[3].Y - axial[0].Y));
            var diagonalReach = Math.Max(Math.Abs(diagonal[3].X - diagonal[0].X), Math.Abs(diagonal[3].Y - diagonal[0].Y));
            Assert.True(diagonalReach >= 2 * axialReach, $"{id}: dominant {diagonalReach}, secondary {axialReach}");
            if (id == "B11") Assert.True(diagonalReach >= 6);
        }
        var addedClosed = E(design.Enrichment.Result.ClosedPaths).Length - E(design.Enrichment.Source.ClosedPaths).Length;
        Assert.Equal(expectedPetals + expectedPetals / 8, addedClosed); // one centre per flower
    }

    [Theory]
    [InlineData("B10", 16, 2)]
    [InlineData("B11", 32, 4)]
    public void PointedRoomFlowersHaveStrictlySeparatePetalsAcrossTheirPeriodicNeighbours(string id, int expectedPetals, int expectedCentres)
    {
        var design = Designs.Value.Single(item => item.Design.SourceId == id && item.Design.Mode == "detail").Design;
        var sourcePaths = E(design.Enrichment.Source.Paths);
        var petals = E(design.Enrichment.Result.ClosedPaths).Cast<Relation>()
            .Where(path => !sourcePaths.Any(actual => ReferenceEquals(path, actual)) && E((Relation)path.Right).Length == 6)
            .Select(path => Points(design.Source.Frame, path)).ToArray();
        Assert.Equal(expectedPetals, petals.Length);
        var flowers = petals.Chunk(8).Select(group => new
        {
            Petals = group,
            Centre = (X: group.Average(path => path[0].X), Y: group.Average(path => path[0].Y))
        }).ToArray();
        Assert.Equal(expectedCentres, flowers.Select(flower => flower.Centre).Distinct().Count());
        // Each body's eight actual root sites form the same small radial
        // collar around one centre. The comparison permits overlap within a
        // flower, but never treats nearby different centres as one body.
        foreach (var flower in flowers)
            Assert.All(flower.Petals, path =>
            {
                Assert.InRange(Math.Abs(path[0].X - flower.Centre.X), 0, 2);
                Assert.InRange(Math.Abs(path[0].Y - flower.Centre.Y), 0, 2);
            });
        var column = Word(design.Source.Frame, design.Source.ColumnPitch);
        var row = Word(design.Source.Frame, design.Source.RowPitch);
        for (var first = 0; first < flowers.Length; first++)
            for (var second = 0; second < flowers.Length; second++)
                for (var u = -1; u <= 1; u++)
                    for (var v = -1; v <= 1; v++)
                    {
                        if (first == second && u == 0 && v == 0) continue;
                        var dx = column.X * u + row.X * v; var dy = column.Y * u + row.Y * v;
                        foreach (var a in flowers[first].Petals)
                            foreach (var actual in flowers[second].Petals)
                            {
                                var b = actual.Select(point => (X: point.X + dx, Y: point.Y + dy)).ToArray();
                                var witness = $"{id} bodies {flowers[first].Centre} and {flowers[second].Centre}, period ({u},{v})";
                                Assert.False(Intersects(a, b), witness + " have touching/crossing petal chords");
                                Assert.False(Inside(a, b[0]) || Inside(b, a[0]), witness + " have nested different-body petals");
                            }
                    }

        static bool Intersects((int X, int Y)[] a, (int X, int Y)[] b)
        {
            for (var ai = 1; ai < a.Length; ai++)
                for (var bi = 1; bi < b.Length; bi++)
                {
                    var p = a[ai - 1]; var q = a[ai]; var r = b[bi - 1]; var s = b[bi];
                    var pr = Cross(p, q, r); var ps = Cross(p, q, s);
                    var rp = Cross(r, s, p); var rq = Cross(r, s, q);
                    if (pr == 0 && Between(p, q, r) || ps == 0 && Between(p, q, s) ||
                        rp == 0 && Between(r, s, p) || rq == 0 && Between(r, s, q) ||
                        Math.Sign(pr) * Math.Sign(ps) < 0 && Math.Sign(rp) * Math.Sign(rq) < 0) return true;
                }
            return false;
        }
        static long Cross((int X, int Y) a, (int X, int Y) b, (int X, int Y) p) =>
            (long)(b.X - a.X) * (p.Y - a.Y) - (long)(b.Y - a.Y) * (p.X - a.X);
        static bool Between((int X, int Y) a, (int X, int Y) b, (int X, int Y) p) =>
            p.X >= Math.Min(a.X, b.X) && p.X <= Math.Max(a.X, b.X) &&
            p.Y >= Math.Min(a.Y, b.Y) && p.Y <= Math.Max(a.Y, b.Y);
        static bool Inside((int X, int Y)[] polygon, (int X, int Y) p)
        {
            var inside = false;
            for (var i = 1; i < polygon.Length; i++)
            {
                var a = polygon[i - 1]; var b = polygon[i];
                if ((a.Y > p.Y) != (b.Y > p.Y) &&
                    p.X < (b.X - a.X) * (double)(p.Y - a.Y) / (b.Y - a.Y) + a.X) inside = !inside;
            }
            return inside;
        }
    }

    [Theory]
    [InlineData("B13")]
    [InlineData("B14")]
    public void FourInterlockingSourceSiblingsAreFourDisjointActualColourFamilies(string id)
    {
        var variant = Designs.Value.Single(item => item.Design.SourceId == id && item.Design.Mode == "detail");
        var final = variant.Design.Enrichment.ResultSurface;
        var colour = E(variant.CurveHints).Cast<Relation>().Select(hint =>
            OrnamentColourSelection.TryRead(variant.Design.Source.Frame, final, hint, out var read) ? read : null)
            .Single(read => read is not null)!;
        var source = E(variant.Design.Enrichment.Source.ClosedPaths);
        Assert.Equal(4, source.Length);
        // The exact requested groups are part of the native selection. Read
        // through its shape rather than keeping palette indexes on DTOs.
        var groups = E(colour.Groups).Cast<Relation>().ToArray(); Assert.Equal(4, groups.Length);
        for (var index = 0; index < 4; index++) Assert.Contains(E(groups[index]), path => ReferenceEquals(path, source[index]));
        for (var first = 0; first < 4; first++)
            for (var second = first + 1; second < 4; second++)
                Assert.DoesNotContain(E(groups[first]), path => E(groups[second]).Any(other => ReferenceEquals(path, other)));
    }

    private static (int X, int Y)[] Points(QualitativeMotionFrame frame, Relation path)
    {
        var current = Word(frame, (Relation)path.Left); var points = new List<(int X, int Y)> { current };
        foreach (var word in E((Relation)path.Right).Cast<Relation>())
        { var step = Word(frame, word); current = (current.X + step.X, current.Y + step.Y); points.Add(current); }
        return points.ToArray();
    }

    private static (int X, int Y) Word(QualitativeMotionFrame frame, Relation word)
    {
        var x = 0; var y = 0;
        foreach (var move in E(word).Cast<Relation>())
        {
            Assert.True(frame.Direction(move, out var axis, out var forward, out _));
            if (ReferenceEquals(axis, frame.Horizontal)) x += forward ? 1 : -1; else y += forward ? 1 : -1;
        }
        return (x, y);
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
