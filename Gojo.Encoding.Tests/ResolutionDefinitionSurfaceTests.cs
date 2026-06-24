using System.Text.Json.Nodes;
using Gojo.Core;
using Gojo.Encoding;

namespace Gojo.Encoding.Tests;

public sealed class ResolutionDefinitionSurfaceTests
{
    [Fact]
    public void ResolutionFacetLedgerBuildsAllResolutionFacetDefinitions()
    {
        var ledger = ResolutionDefinitionSurface.CreateFacetLedger(new Carrier());

        Assert.True(ConceptDefinitionSurface.TryReadLedger(ledger, out var definitions));
        var rows = definitions
            .Select(ConceptDefinitionInspection.DefinitionJson)
            .ToDictionary(row => row.Name, StringComparer.Ordinal);

        Assert.Contains("partition-resolution", rows.Keys);
        Assert.Contains("unit-resolution", rows.Keys);
        Assert.Contains("observer-resolution", rows.Keys);
        Assert.Contains("omitted-input-slot", rows.Keys);
        Assert.Contains("approximation-resolution", rows.Keys);
        Assert.Contains("ambiguous-resolution", rows.Keys);
        Assert.Contains("ladder-resolution", rows.Keys);
        Assert.Contains("runtime-resolution", rows.Keys);

        AssertRow(rows["partition-resolution"], "Part(span,partition-count)", "span", "partition-count");
        AssertRow(rows["unit-resolution"], "U(unit-comparison)", "unit-comparison");
        AssertRow(rows["observer-resolution"], "Read(span,observer-basis,coarse-rung)", "span", "observer-basis", "coarse-rung");
        AssertRow(rows["omitted-input-slot"], "Slot(input,?)");
        AssertRow(rows["approximation-resolution"], "Approx(approximate-comparison,coarse-rung)", "approximate-comparison", "coarse-rung");
        AssertRow(rows["ambiguous-resolution"], "Read(span,ambiguous-basis)", "span", "ambiguous-basis");
        AssertRow(rows["ladder-resolution"], "Refine(rough-rung,fine-rung-read,rung-correspondence)", "rough-rung", "fine-rung-read", "rung-correspondence");
        AssertRow(rows["runtime-resolution"], "Run(span,runtime-input,runtime-step,coarse-rung)", "span", "runtime-input", "runtime-step", "coarse-rung");

        Assert.Equal("input role is intentionally open, not invalid", rows["omitted-input-slot"].ResidueOrTension);
        Assert.Equal("near enough at this rung while sharper difference remains residue", rows["approximation-resolution"].ResidueOrTension);
        Assert.Equal("read surface is unresolved rather than absent", rows["ambiguous-resolution"].ResidueOrTension);
        Assert.Equal("runtime residue belongs to this run, not permanently to the source", rows["runtime-resolution"].ResidueOrTension);

        var nativeRows = definitions.ToDictionary(NameOf, StringComparer.Ordinal);
        AssertNativeBody(nativeRows["partition-resolution"], body => ResolutionFacetStructureSurface.TryReadPartition(body, out _));
        AssertNativeBody(nativeRows["unit-resolution"], body => ResolutionFacetStructureSurface.TryReadUnitResolution(body, out _));
        AssertNativeBody(nativeRows["observer-resolution"], body => ResolutionFacetStructureSurface.TryReadObserverResolution(body, out _));
        AssertNativeBody(nativeRows["omitted-input-slot"], body => ResolutionFacetStructureSurface.TryReadSlot(body, out _));
        AssertNativeBody(nativeRows["approximation-resolution"], body => ResolutionFacetStructureSurface.TryReadApproximation(body, out _));
        AssertNativeBody(nativeRows["ambiguous-resolution"], body => ResolutionFacetStructureSurface.TryReadAmbiguousResolution(body, out _));
        AssertNativeBody(nativeRows["ladder-resolution"], body => ResolutionFacetStructureSurface.TryReadLadderResolution(body, out _));
        AssertNativeBody(nativeRows["runtime-resolution"], body => ResolutionFacetStructureSurface.TryReadRuntimeResolution(body, out _));
    }

    [Fact]
    public void ResolutionFacetReadDiscoversEveryFacetFromLocalDefinitions()
    {
        var ledger = ResolutionDefinitionSurface.CreateFacetLedger(new Carrier());

        Assert.True(ResolutionReadSurface.TryReadResolutionFacets(ledger, out var read));

        AssertFacet(read, "partition", "Part(span,partition-count)");
        AssertFacet(read, "unit", "U(unit-comparison)");
        AssertFacet(read, "observer", "Read(span,observer-basis,coarse-rung)");
        AssertFacet(read, "omission", "Slot(input,?)");
        AssertFacet(read, "approximation", "Approx(approximate-comparison,coarse-rung)");
        AssertFacet(read, "ambiguity", "Read(span,ambiguous-basis)");
        AssertFacet(read, "ladder", "Refine(rough-rung,fine-rung-read,rung-correspondence)");
        AssertFacet(read, "runtime", "Run(span,runtime-input,runtime-step,coarse-rung)");

        Assert.Contains(read.Of("approximation"), facet =>
            facet.ResidueOrTension == "near enough at this rung while sharper difference remains residue");
        Assert.Contains(read.Of("ambiguity"), facet =>
            facet.ResidueOrTension == "read surface is unresolved rather than absent");
    }

    [Fact]
    public void ResolutionFacetReadUsesStructureInsteadOfSpecificDefinitionNames()
    {
        var ledger = RenamedResolutionLedger();

        Assert.True(ResolutionReadSurface.TryReadResolutionFacets(ledger, out var read));

        Assert.True(read.Has("partition"));
        Assert.True(read.Has("unit"));
        Assert.True(read.Has("observer"));
        Assert.True(read.Has("omission"));
        Assert.True(read.Has("approximation"));
        Assert.True(read.Has("ambiguity"));
        Assert.True(read.Has("ladder"));
        Assert.True(read.Has("runtime"));
        Assert.DoesNotContain(read.Facets.Select(facet => facet.Definition.Name), name =>
            name is "partition-resolution" or "unit-resolution" or "observer-resolution" or "runtime-resolution");
    }

    [Fact]
    public void ResolutionFacetLedgerMatchesInspectableJsonFixture()
    {
        var generated = ResolutionFacetFixtureEntry();
        var rewritten = GojoStructureEncodingJson.SerializeFixtureEntry(generated);
        var fixture = File.ReadAllText(ResolutionFacetFixturePath());
        var reread = GojoStructureEncodingJson.DeserializeFixtureEntry(fixture);
        var rewrittenAgain = GojoStructureEncodingJson.SerializeFixtureEntry(reread);

        AssertJsonEquivalent(fixture, rewritten);
        AssertJsonEquivalent(rewritten, rewrittenAgain);
    }

    private static GojoEncodingJsonFixtureEntry ResolutionFacetFixtureEntry()
    {
        var ledger = ResolutionDefinitionSurface.CreateFacetLedger(new Carrier());
        Assert.True(ConceptDefinitionSurface.TryReadLedger(ledger, out var definitions));

        return new GojoEncodingJsonFixtureEntry
        {
            Represents = "A tiny resolution sample that keeps partition, unit, observer, omission, approximation, ambiguity, ladder, and runtime resolution separate while showing their shared read/compare/residue footing.",
            EntryId = "resolution-facets",
            Expression = "Part | U(Comp) | Read(...,Res) | Slot(?) | Approx | Read(...,B?) | Refine | Run(...,Res)",
            EncodingNote = "This is a primitive definition sample, not a glossary page. It prevents coarse, omitted, approximate, ambiguous, and runtime-local resolution from collapsing into generic unknown.",
            Gojo = new GojoEncodingJsonFixtureGojo
            {
                Known =
                [
                    "ambiguity",
                    "approximation",
                    "ladder-resolution",
                    "observer-resolution",
                    "omission",
                    "partition-resolution",
                    "runtime-resolution",
                    "unit-resolution"
                ],
                Paths =
                [
                    "span + partition count --> partition-resolution",
                    "span + unit span --> unit-resolution",
                    "span + observer basis + coarse rung --> observer-resolution",
                    "input slot left open --> omission-resolution",
                    "span + aspirational target + coarse rung --> approximation-resolution",
                    "span + unresolved basis --> ambiguous-resolution",
                    "rough rung + fine rung + correspondence --> ladder-resolution",
                    "span + input + time + coarse rung --> runtime-resolution"
                ],
                Tension =
                [
                    "input role is intentionally open, not invalid",
                    "near enough at this rung while sharper difference remains residue",
                    "read surface is unresolved rather than absent",
                    "runtime residue belongs to this run, not permanently to the source"
                ],
                Definitions = [.. definitions.Select(ConceptDefinitionInspection.DefinitionJson)]
            }
        };
    }

    private static Relation RenamedResolutionLedger()
    {
        var definitions = new List<Relation>();
        var resolutionCarrier = new Carrier();
        var runtimeClock = new Carrier();

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

        var baseSpanBody = Relation.Segment(new Carrier(), 0, 12);
        var baseSpan = Add("measured thing", baseSpanBody);
        var count = Add("five-ish", Atom("5"));
        Add(
            "divided read",
            ResolutionFacetStructureSurface.Partition(
                new Carrier(),
                baseSpan,
                count,
                ResolutionFacetStructureSurface.PartitionSites(baseSpanBody, 4)),
            [baseSpan, count]);

        var calibrator = Add("little stick", Relation.Segment(new Carrier(), 0, 1));
        var comparison = Add(
            "against stick",
            ResolutionFacetStructureSurface.Comparison(new Carrier(), baseSpan, calibrator),
            [baseSpan, calibrator]);
        Add(
            "measuring handle",
            ResolutionFacetStructureSurface.UnitResolution(new Carrier(), comparison),
            [comparison]);

        var basis = Add(
            "view frame",
            ResolutionFacetStructureSurface.ObserverBasis(new Carrier(), baseSpan),
            [baseSpan]);
        var rough = Add(
            "rough view",
            ResolutionFacetStructureSurface.ResolutionRung(new Carrier(), resolutionCarrier, "rough", 1));
        Add(
            "view result",
            ResolutionFacetStructureSurface.ObserverResolution(new Carrier(), baseSpan, basis, rough),
            [baseSpan, basis, rough]);

        Add(
            "empty socket",
            ResolutionFacetStructureSurface.Slot(new Carrier(), Atom("input")));

        var unknownTarget = Add(
            "maybe target",
            ResolutionFacetStructureSurface.Target(new Carrier()));
        var looseComparison = Add(
            "close enough comparison",
            ResolutionFacetStructureSurface.Comparison(new Carrier(), baseSpan, unknownTarget, rough),
            [baseSpan, unknownTarget, rough]);
        Add(
            "about result",
            ResolutionFacetStructureSurface.Approximation(new Carrier(), looseComparison, rough),
            [looseComparison, rough]);

        var unresolvedBasis = Add(
            "several possible frames",
            ResolutionFacetStructureSurface.AmbiguousBasis(new Carrier(), baseSpan),
            [baseSpan]);
        Add(
            "unsettled view",
            ResolutionFacetStructureSurface.ObserverResolution(
                new Carrier(),
                baseSpan,
                unresolvedBasis,
                Relation.Point(Carrier.Unknown, 0)),
            [baseSpan, unresolvedBasis]);

        var fine = Add(
            "sharp view",
            ResolutionFacetStructureSurface.ResolutionRung(new Carrier(), resolutionCarrier, "fine", 10));
        var lowRung = Add(
            "low rung",
            ResolutionFacetStructureSurface.Rung(new Carrier(), baseSpan, rough),
            [baseSpan, rough]);
        var highRung = Add(
            "high rung",
            ResolutionFacetStructureSurface.Rung(new Carrier(), baseSpan, fine),
            [baseSpan, fine]);
        var mapping = Add(
            "rung map",
            ResolutionFacetStructureSurface.Correspondence(new Carrier(), lowRung, highRung),
            [lowRung, highRung]);
        Add(
            "sharpened read",
            ResolutionFacetStructureSurface.LadderResolution(new Carrier(), lowRung, highRung, mapping),
            [lowRung, highRung, mapping]);

        var input = Add(
            "changing input",
            ResolutionFacetStructureSurface.RuntimeInput(new Carrier()));
        var time = Add(
            "time now",
            ResolutionFacetStructureSurface.RuntimeStep(new Carrier(), runtimeClock, "0", 0));
        Add(
            "live read",
            ResolutionFacetStructureSurface.RuntimeResolution(new Carrier(), runtimeClock, baseSpan, input, time, rough),
            [baseSpan, input, time, rough]);

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

    private static void AssertNativeBody(Relation definition, Func<IElement, bool> read)
    {
        Assert.True(ConceptDefinitionSurface.TryRead(definition, out var definitionRead));
        Assert.False(ConceptAddressSurface.TryReadCompound(definitionRead.Body, out _, out _, out _, out _));
        Assert.True(read(definitionRead.Body));
    }

    private static string NameOf(Relation definition)
    {
        Assert.True(ConceptDefinitionSurface.TryRead(definition, out var read));
        return ConceptDefinitionInspection.ElementDebug(read.Name);
    }

    private static void AssertFacet(
        ResolutionReadSurface.ResolutionFacetRead read,
        string facet,
        string body)
    {
        Assert.Contains(read.Of(facet), item => item.Body == body);
    }

    private static string ResolutionFacetFixturePath() =>
        Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "EncoderJson",
            "concept-definitions",
            "resolution-facets.json");

    private static Relation Atom(string name) =>
        ConceptAddressSurface.Atom(new Carrier(), name);

    private static void AssertJsonEquivalent(string expected, string actual)
    {
        var expectedNode = JsonNode.Parse(expected);
        var actualNode = JsonNode.Parse(actual);
        Assert.True(JsonNode.DeepEquals(expectedNode, actualNode), actual);
    }
}
