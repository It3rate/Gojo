using System.Text.Json;
using Gojo.Blocks;
using Gojo.Core;
using Gojo.Encoding.Syntax;

namespace Gojo.Encoding.Tests;

public sealed class NumericMilestoneInventoryTests
{
    [Fact]
    public void StructuralFixtureReadsDoNotGainMeaningFromCaptionKeywordsOrFixtureIds()
    {
        var source = new GojoStructureEncodingFixtureProjection.Source("plain", "", "", null,
            Relation.Point(new Carrier(), 0));
        var labelled = source with { EntryId = "resolution-tension-settled-read",
            Expression = "Circle Volume repeat settled read", EncodingNote = "constraint feedback timeline shared-reference" };
        var original = GojoStructureEncodingFixtureProjection.EntryFromStructure(source);
        var renamed = GojoStructureEncodingFixtureProjection.EntryFromStructure(labelled);
        Assert.Equal(JsonSerializer.Serialize(original.Gojo), JsonSerializer.Serialize(renamed.Gojo));
        Assert.Empty(renamed.Gojo.Definitions ?? []);
    }

    public static IEnumerable<object[]> SyntaxFixtures() => Directory.GetFiles(
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "Syntax"), "*.gojo")
        .Order().Select(path => new object[] { Path.GetFileName(path) });

    [Theory]
    [MemberData(nameof(SyntaxFixtures))]
    public void AuthoredSyntaxPreservesItsEntireNumericGraph(string fixture)
    {
        var text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Syntax", fixture));
        AssertRoundTrip(GojoSyntax.Parse(text).Roots.ToArray());
    }

    [Fact]
    public void EveryExportedBlocksSpecimenPreservesItsEntireNumericGraph()
    {
        var specimens = BlocksGlossaryExport.Structures();
        Assert.True(specimens.Count > 100);
        foreach (var specimen in specimens)
        {
            try { AssertRoundTrip(specimen.Structure); }
            catch (Exception error) { throw new InvalidOperationException($"Blocks specimen: {specimen.Id}", error); }
        }
    }

    private static void AssertRoundTrip(params IElement[] roots)
    {
        var original = GojoEncodedSnapshot.CaptureGraph(roots);
        var restored = GojoNumericFormat.Decode(GojoNumericFormat.Encode(original));
        var recaptured = GojoEncodedSnapshot.CaptureGraph(restored);
        // Table discovery follows the same ordered graph. Only process-global
        // IDs change; all references, repeats, descriptors and ownership remain.
        Assert.Equal(WithoutRuntimeIds(original), WithoutRuntimeIds(recaptured));
        Assert.Equal(WithoutRuntimeIds(recaptured), WithoutRuntimeIds(
            GojoEncodedSnapshot.CaptureGraph(GojoNumericFormat.Decode(GojoNumericFormat.Encode(restored)))));
    }

    private static string WithoutRuntimeIds(GojoEncodedSnapshot graph) => JsonSerializer.Serialize(graph with
    {
        Carriers = [.. graph.Carriers.Select(c => c with { Id = c.IsUnknown ? 0UL : (ulong)c.Index + 1 })],
        TickRefs = [.. graph.TickRefs.Select(t => t with { Id = (ulong)t.Index + 1 })],
        Relations = [.. graph.Relations.Select(r => r with { Id = (ulong)r.Index + 1 })],
        UnitShapes = [.. graph.UnitShapes.Select(u => u with { Id = (ulong)u.Index + 1 })]
    });
}
