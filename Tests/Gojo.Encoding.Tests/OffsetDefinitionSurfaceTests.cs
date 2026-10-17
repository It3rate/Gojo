using System.Text.Json.Nodes;
using Gojo.Core;
using Gojo.Encoding;

namespace Gojo.Encoding.Tests;

public sealed class OffsetDefinitionSurfaceTests
{
    [Fact]
    public void OffsetVariantLedgerBuildsSegmentCircleAndResidueDefinitions()
    {
        var ledger = OffsetDefinitionSurface.CreateOffsetVariantLedger(new Carrier());

        Assert.True(ConceptDefinitionSurface.TryReadLedger(ledger, out var definitions));
        var rows = definitions
            .Select(ConceptDefinitionInspection.DefinitionJson)
            .ToDictionary(row => row.Name, StringComparer.Ordinal);

        AssertRow(
            rows["segment-left-offset"],
            "Off(segment-source,segment-orthogonal-outlet,segment-left-facing,left-distance,segment-offset-resolution)",
            "segment-source",
            "segment-orthogonal-outlet",
            "segment-left-facing",
            "left-distance",
            "segment-offset-resolution");
        AssertRow(
            rows["segment-right-offset"],
            "Off(segment-source,segment-orthogonal-outlet,segment-right-facing,right-distance,segment-offset-resolution)",
            "segment-source",
            "segment-orthogonal-outlet",
            "segment-right-facing",
            "right-distance",
            "segment-offset-resolution");
        AssertRow(
            rows["circle-exterior-offset"],
            "Off(circumference-boundary,circle-normal-outlet,circle-exterior-facing,outward-distance,circle-offset-resolution)",
            "circumference-boundary",
            "circle-normal-outlet",
            "circle-exterior-facing",
            "outward-distance",
            "circle-offset-resolution");
        AssertRow(
            rows["circle-interior-offset"],
            "Off(circumference-boundary,circle-normal-outlet,circle-interior-facing,inward-distance,circle-offset-resolution)",
            "circumference-boundary",
            "circle-normal-outlet",
            "circle-interior-facing",
            "inward-distance",
            "circle-offset-resolution");
        AssertRow(
            rows["closed-loop-offset"],
            "OffClosed(closed-segment-loop,segment-orthogonal-outlet,segment-left-facing,left-distance,segment-offset-resolution)",
            "closed-segment-loop",
            "segment-orthogonal-outlet",
            "segment-left-facing",
            "left-distance",
            "segment-offset-resolution");

        Assert.Equal("cap and join policy not present", rows["open-segment-offset"].ResidueOrTension);
        Assert.Equal(
            "offset read remains unresolved until source and outlet are locally compatible",
            rows["offset-mismatch-residue"].ResidueOrTension);
    }

    [Fact]
    public void OffsetReadDiscoversLineCircleOpenClosedResidueAndCrystallizedReuse()
    {
        var ledger = OffsetDefinitionSurface.CreateOffsetVariantLedger(new Carrier());

        Assert.True(OffsetReadSurface.TryReadOffsets(ledger, out var read));

        Assert.Contains(read.Offsets, offset =>
            offset.Definition.Name == "segment-left-offset" &&
            offset.SourceShape == "segment" &&
            offset.Facing?.Side == "left" &&
            offset.Distance == "+d");
        Assert.Contains(read.Offsets, offset =>
            offset.Definition.Name == "segment-right-offset" &&
            offset.SourceShape == "segment" &&
            offset.Facing?.Side == "right" &&
            offset.Distance == "-d");
        Assert.Contains(read.Offsets, offset =>
            offset.Definition.Name == "circle-exterior-offset" &&
            offset.SourceShape == "circumference" &&
            offset.Facing?.Side == "exterior" &&
            offset.Distance == "+d");
        Assert.Contains(read.Offsets, offset =>
            offset.Definition.Name == "circle-interior-offset" &&
            offset.SourceShape == "circumference" &&
            offset.Facing?.Side == "interior" &&
            offset.Distance == "-d");
        Assert.Contains(read.Offsets, offset => offset.Definition.Name == "open-segment-offset" && offset.Kind == "open");
        Assert.Contains(read.Offsets, offset => offset.Definition.Name == "closed-loop-offset" && offset.Kind == "closed");
        Assert.Contains(read.Offsets, offset => offset.Definition.Name == "offset-mismatch" && offset.Kind == "unresolved");

        Assert.Contains(read.Boundaries, boundary =>
            boundary.Definition.Name == "circle-exterior-offset-boundary" &&
            boundary.Role == "self-terminal" &&
            boundary.Offset?.Definition.Name == "circle-exterior-offset" &&
            boundary.Orientation?.Direction == "cw");
        Assert.Contains(read.Boundaries, boundary =>
            boundary.Definition.Name == "circle-interior-offset-boundary" &&
            boundary.Role == "self-terminal" &&
            boundary.Offset?.Definition.Name == "circle-interior-offset" &&
            boundary.Orientation?.Direction == "ccw");
        Assert.Contains(read.Bands, band =>
            band.Definition.Name == "circle-exterior-ring-band" &&
            band.First.Role == "self-source" &&
            band.Second.Offset?.Definition.Name == "circle-exterior-offset");
        Assert.Contains(read.Residues, residue =>
            residue.Offset.Definition.Name == "offset-mismatch" &&
            residue.ResidueOrTension == "offset read remains unresolved until source and outlet are locally compatible");
        Assert.Contains(read.CrystallizedOffsets, crystallized =>
            crystallized.Offset.Definition.Name == "circle-exterior-offset" &&
            crystallized.Context?.Name == "circumference-boundary");
    }

    [Fact]
    public void OffsetReadUsesStructureInsteadOfSpecificDefinitionNames()
    {
        var ledger = RenamedOffsetLedger();

        Assert.True(OffsetReadSurface.TryReadOffsets(ledger, out var read));
        Assert.Contains(read.Offsets, offset =>
            offset.SourceShape == "segment" &&
            offset.Facing?.Side == "left" &&
            offset.Distance == "+d");
        Assert.Contains(read.Boundaries, boundary =>
            boundary.Role == "terminal" &&
            boundary.Offset?.Facing?.Side == "left");
        Assert.Contains(read.Residues, residue => residue.Offset.Kind == "unresolved");
        Assert.DoesNotContain(read.Offsets.Select(offset => offset.Definition.Name), name => name is "segment-left-offset" or "offset-mismatch");
    }

    [Fact]
    public void OffsetVariantLedgerMatchesInspectableJsonFixture()
    {
        var generated = OffsetVariantFixtureEntry();
        var rewritten = GojoStructureEncodingJson.SerializeFixtureEntry(generated);
        var fixture = File.ReadAllText(OffsetVariantFixturePath());
        var reread = GojoStructureEncodingJson.DeserializeFixtureEntry(fixture);
        var rewrittenAgain = GojoStructureEncodingJson.SerializeFixtureEntry(reread);

        AssertJsonEquivalent(fixture, rewritten);
        AssertJsonEquivalent(rewritten, rewrittenAgain);
    }

    private static GojoEncodingJsonFixtureEntry OffsetVariantFixtureEntry()
    {
        var ledger = OffsetDefinitionSurface.CreateOffsetVariantLedger(new Carrier());
        Assert.True(ConceptDefinitionSurface.TryReadLedger(ledger, out var definitions));

        return new GojoEncodingJsonFixtureEntry
        {
            Represents = "A tiny offset sample covering segment-side offsets, circumference offsets, interior and exterior facing, clockwise and counterclockwise orientation evidence, open and closed offsets, mismatch residue, and crystallized offset reuse.",
            EntryId = "offset-variants",
            Expression = "Off(source,outlet,face,d,Res) + Bd1/BdSelf + Band + Residue",
            EncodingNote = "This is a primitive definition sample, not a geometry solver. Offset is represented as a local read from source support through outlet, facing, distance, and resolution evidence.",
            Gojo = new GojoEncodingJsonFixtureGojo
            {
                Known =
                [
                    "circumference-boundary",
                    "circle-offset",
                    "closed-offset",
                    "crystallized-offset-read",
                    "offset-residue",
                    "segment-offset"
                ],
                Paths =
                [
                    "segment + left facing + +d --> left offset boundary",
                    "segment + right facing + -d --> right offset boundary",
                    "circumference + exterior facing + clockwise evidence --> exterior offset boundary",
                    "circumference + interior facing + counterclockwise evidence --> interior offset boundary",
                    "closed source loop + offset read --> self boundary",
                    "incompatible source and outlet evidence --> unresolved offset residue"
                ],
                Tension =
                [
                    "cap and join policy not present",
                    "open offset has no closure boundary yet",
                    "source and normal evidence do not share local support",
                    "offset read remains unresolved until source and outlet are locally compatible"
                ],
                Definitions = [.. definitions.Select(ConceptDefinitionInspection.DefinitionJson)]
            }
        };
    }

    private static Relation RenamedOffsetLedger()
    {
        var definitions = new List<Relation>();

        Relation Add(
            string name,
            IElement body,
            IReadOnlyList<IElement>? dependencies = null,
            IElement? residueOrTension = null)
        {
            var definition = ConceptDefinitionSurface.Create(
                new Carrier(),
                name,
                body,
                dependencies,
                residueOrTension: residueOrTension);
            definitions.Add(definition);
            return definition;
        }

        var source = Add("rail", Relation.Segment(new Carrier(), 0, 1));
        var boundary = Add("rail start", Address("Bd0", Atom("rail")), [source]);
        var outlet = Add("normal read", Relation.OrthogonalOutlet(Relation.Segment(new Carrier(), 0, 1)), [source]);
        var face = Add("left hand side", Address("Face", Atom("rail"), Atom("left")), [source]);
        var distance = Add("amount", Atom("+d"));
        var resolution = Add("coarse normal", Address("Res", Atom("normal-distance")));
        var offset = Add("left shifted rail", Address("Off", Atom("rail"), Atom("normal read"), Atom("left hand side"), Atom("amount"), Atom("coarse normal")), [source, outlet, face, distance, resolution]);
        Add("outer rail", Address("Bd1", Atom("left shifted rail")), [offset]);

        var mismatch = Add(
            "bad shift",
            Address("Off?", Atom("rail"), Atom("unshared normal"), Atom("amount")),
            [source, distance],
            Text("source and normal evidence do not share local support"));
        Add(
            "bad shift residue",
            Address("Residue", Atom("bad shift")),
            [mismatch],
            Text("offset read remains unresolved until source and outlet are locally compatible"));

        _ = boundary;
        return ConceptDefinitionSurface.Ledger(new Carrier(), definitions);
    }

    private static void AssertRow(
        GojoEncodingJsonFixtureDefinition row,
        string body,
        params string[] dependencies)
    {
        Assert.Equal(body, row.Body);
        Assert.Equal(dependencies, row.DependsOn ?? []);
    }

    private static string OffsetVariantFixturePath() =>
        Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "EncoderJson",
            "concept-definitions",
            "offset-variants.json");

    private static Relation Address(string lexical, params IElement[] parts) =>
        ConceptAddressSurface.Compound(new Carrier(), lexical, parts);

    private static Relation Atom(string name) =>
        ConceptAddressSurface.Atom(new Carrier(), name);

    private static Relation Text(string text) =>
        EncodedTextSurface.Create(new Carrier(), text);

    private static void AssertJsonEquivalent(string expected, string actual)
    {
        var expectedNode = JsonNode.Parse(expected);
        var actualNode = JsonNode.Parse(actual);
        Assert.True(JsonNode.DeepEquals(expectedNode, actualNode), actual);
    }
}
