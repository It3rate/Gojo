using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class BourgoinOrnamentDesignLibraryTests
{
    private static readonly Lazy<IReadOnlyList<BourgoinOrnamentDesignLibrary.Design>> Designs =
        new(BourgoinOrnamentDesignLibrary.Create);

    [Fact]
    public void EverySourceHasThreeDifferentNativeEnrichmentsOfTheSameActualSkeleton()
    {
        var designs = Designs.Value; Assert.Equal(108, designs.Count);
        foreach (var group in designs.GroupBy(design => design.SourceId))
        {
            var variants = group.ToArray(); Assert.Equal(3, variants.Length);
            Assert.Equal(new[] { "field", "motif", "detail" }, variants.Select(design => design.Mode));
            var source = variants[0].Source;
            var original = Assert.IsType<Relation>(Assert.Single(E(source.Motifs)));
            var signatures = new List<string>();
            foreach (var design in variants)
            {
                Assert.Same(source, design.Source);
                var enrichment = design.Enrichment; Assert.True(enrichment.IsComplete, design.Id);
                Assert.Same(original, enrichment.SourceSurface); Assert.Equal(3, E(enrichment.Layers).Length);
                var sourcePaths = E(enrichment.Source.Paths);
                foreach (var path in sourcePaths)
                    Assert.Contains(E(enrichment.Result.Paths), item => ReferenceEquals(item, path));
                var introduced = E(enrichment.Result.Paths).Where(path => !sourcePaths.Any(actual => ReferenceEquals(path, actual))).Cast<Relation>().ToArray();
                Assert.NotEmpty(introduced);
                foreach (var layer in E(enrichment.Layers).Cast<Relation>())
                {
                    Assert.True(enrichment.TryReadLayer(layer, out var definition, out _, out _, out _, out _));
                    var focus = enrichment.Focus(definition.Left); Assert.True(focus.IsComplete);
                    Assert.Same(layer, Assert.Single(E(focus.Selected))); Assert.Equal(2, E(focus.Complement).Length);
                }
                signatures.Add(string.Join("|", introduced.Select(path => Signature(source.Frame, path)).Order(StringComparer.Ordinal)));
                AssertNoNumeric(enrichment.Surface);
            }
            Assert.Equal(3, signatures.Distinct(StringComparer.Ordinal).Count());
        }
    }

    [Theory]
    [InlineData("B01")]
    [InlineData("B24")]
    [InlineData("B32")]
    public void APanelASlidTriangularFieldAndAnOpenDiagonalFretExecuteFromTheirArchivedNativeEnrichment(string id)
    {
        foreach (var design in Designs.Value.Where(design => design.SourceId == id))
        {
            var source = design.Source;
            var adapter = new OrnamentEnrichmentProgram(source.Program, source.Bindings, source.SeamContract, design.Enrichment);
            var initial = (Relation)source.Window.Left;
            Assert.True(OrnamentFieldPacket.TryLoad(adapter.Save(OrnamentProgram.Window(initial.Left, initial.Right, 1, 1)),
                out var packet, out var pending)); Assert.Empty(E(pending));
            var actual = Assert.IsType<Relation>(Assert.Single(E(packet.Program.Motifs)));
            Assert.True(OrnamentEnrichment.TryReadEnrichment(packet.Program.Frame, actual, out var decoded));
            Assert.True(decoded.IsComplete); Assert.Equal(3, E(decoded.Layers).Length);
            var field = new OrnamentProgramField(packet.Program.Generate(packet.Bindings, packet.Window), packet.SeamContract);
            Assert.True(field.IsComplete); Assert.Empty(E(field.Blocked)); Assert.Empty(E(field.Unresolved));
            var chords = E(decoded.Result.Paths).Cast<Relation>().Sum(path => E((Relation)path.Right).Length);
            Assert.Equal(chords, E(field.Graph.Strokes).Length);
            AssertNoNumeric(packet.Source);
        }
    }

    private static string Signature(QualitativeMotionFrame frame, Relation path) =>
        string.Join("/", new[] { (Relation)path.Left }.Concat(E((Relation)path.Right).Cast<Relation>()).Select(word =>
            string.Join("", E(word).Cast<Relation>().Select(move =>
            {
                Assert.True(frame.Direction(move, out var axis, out var forward, out _));
                return ReferenceEquals(axis, frame.Horizontal) ? forward ? "R" : "L" : forward ? "U" : "D";
            }))));

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
