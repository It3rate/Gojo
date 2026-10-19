using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class OrnamentMotifComplexityTests
{
    [Fact]
    public void RepeatedPlacementsRetainOneActualPrototypeInsteadOfBecomingFourConcepts()
    {
        var f = new Fixture(); var prototype = f.Prototype();
        var read = OrnamentMotifComplexity.ReadEncoding(f.Enrich(
            f.Place(prototype, 2), f.Place(prototype, 4), f.Place(prototype, 6), f.Place(prototype, 8)));
        Assert.Equal(4, read.Counts.AddedPaths); Assert.Equal(8, read.Counts.AddedChords);
        Assert.Equal(1, read.Counts.RetainedPrototypeIdentities);
        Assert.Equal(4, read.Counts.WitnessedPrototypePlacements);
        Assert.Equal(0, read.Counts.UnattributedAddedPaths);
        var group = Assert.IsType<Relation>(Assert.Single(E(read.PrototypeGroups)));
        Assert.Same(prototype.Surface, group.Left); Assert.Equal(4, E((Relation)group.Right).Length);
        Assert.Empty(E(read.Unattributed));
    }

    [Fact]
    public void EqualButIndependentlyAuthoredPrototypesKeepTheirDistinctIdentities()
    {
        var f = new Fixture(); var first = f.Prototype(); var second = f.Prototype();
        var read = OrnamentMotifComplexity.ReadEncoding(f.Enrich(f.Place(first, 2), f.Place(second, 4)));
        Assert.Equal(2, read.Counts.RetainedPrototypeIdentities);
        Assert.Collection(E(read.PrototypeGroups).Cast<Relation>(),
            group => Assert.Same(first.Surface, group.Left), group => Assert.Same(second.Surface, group.Left));
    }

    [Fact]
    public void MissingOrUnrelatedAncestryRemainsUnattributedInsteadOfInventingAPrototype()
    {
        var f = new Fixture(); var placed = f.Place(f.Prototype(), 2);
        var plain = new UnitArrangementProgram(f.Frame, "no declared ancestry", placed.Paths,
            placed.Ports, placed.ClosedPaths);
        var unrelated = new UnitArrangementProgram(f.Frame, "unrelated ancestry", placed.Paths,
            placed.Ports, placed.ClosedPaths, new Relation(new Placeholder(), placed.Evidence));
        foreach (var candidate in new[] { plain, unrelated })
        {
            var read = OrnamentMotifComplexity.ReadEncoding(f.Enrich(candidate));
            Assert.Equal(0, read.Counts.RetainedPrototypeIdentities);
            Assert.Equal(1, read.Counts.UnattributedAddedPaths);
            Assert.Same(Assert.Single(E(candidate.Paths)), Assert.Single(E(read.Unattributed)));
        }
    }

    [Fact]
    public void AllThirtySixFineReviewsAreRangesAtDeclaredGranularityAndTwelveIsAReviewTrigger()
    {
        var ids = Enumerable.Range(1, 34).Select(number => $"B{number:00}").Concat(["B06.B", "B08.B"]).ToArray();
        foreach (var id in ids)
        {
            var estimate = Assert.IsType<OrnamentMotifComplexity.ReviewedEstimate>(OrnamentMotifComplexity.FineEstimate(id));
            Assert.InRange(estimate.Minimum, 1, estimate.Maximum);
            Assert.NotEmpty(estimate.ConceptualRecipe);
            Assert.Contains("not autonomous", estimate.Basis);
        }
        foreach (var id in new[] { "B13", "B14" })
        {
            var estimate = OrnamentMotifComplexity.FineEstimate(id)!;
            Assert.True(estimate.MayExceedReviewTrigger); Assert.False(estimate.EntireRangeExceedsReviewTrigger);
        }
        Assert.Null(OrnamentMotifComplexity.FineEstimate("an unknown ornament"));
        Assert.Equal(12, OrnamentMotifComplexity.ReviewTrigger);
    }

    [Fact]
    public void UnreviewedModesAndUnknownSourcesDoNotAcquireInventedConceptualCounts()
    {
        var f = new Fixture(); var enriched = f.Enrich(f.Place(f.Prototype(), 2));
        var known = OrnamentMotifComplexity.Review("B12", "detail", enriched);
        Assert.NotNull(known.Estimate);
        Assert.Null(OrnamentMotifComplexity.Review("B12", "field", enriched).Estimate);
        Assert.Null(OrnamentMotifComplexity.Review("unknown", "detail", enriched).Estimate);
        Assert.Same(enriched.Surface, known.Evidence.Left);
    }

    [Fact]
    public void NativeRosetteSharingSurvivesPacketImportWithoutExecutingTheWholeField()
    {
        var source = new BourgoinPatternLibrary(8).Recipes.Single(source => source.Id == "B05");
        var design = BourgoinOrnamentRefinedLibrary.Create(source, 0, ["detail"]).Single().Design;
        var before = OrnamentMotifComplexity.ReadEncoding(design.Enrichment);
        var installed = new OrnamentEnrichmentProgram(source.Program, source.Bindings, source.SeamContract, design.Enrichment);
        var phase = (Relation)source.Window.Left;
        var saved = installed.Save(OrnamentProgram.Window(phase.Left, phase.Right, 1, 1));
        Assert.True(OrnamentFieldPacket.TryLoad(saved, out var packet, out var unresolved)); Assert.Empty(E(unresolved));
        var actual = Assert.IsType<Relation>(Assert.Single(E(packet.Program.Motifs)));
        Assert.True(OrnamentEnrichment.TryReadEnrichment(packet.Program.Frame, actual, out var imported));
        var after = OrnamentMotifComplexity.ReadEncoding(imported);
        Assert.Equal(before.Counts, after.Counts);
        Assert.Equal(0, after.Counts.UnattributedAddedPaths);
        Assert.True(after.Counts.RetainedPrototypeIdentities < after.Counts.AddedPaths);
        Assert.NotSame(before.Surface, after.Surface);
    }

    private sealed class Fixture
    {
        public QualitativeMotionFrame Frame { get; } = new();
        private readonly IElement reach = new Placeholder();
        private readonly UnitArrangementProgram source;
        private readonly Relation anchor;
        public Fixture()
        {
            var chord = Word(1, 0); var path = new Relation(S(), S(chord));
            anchor = new(new Placeholder(), new Relation(path, chord));
            source = new(Frame, "actual source", S(path), S(anchor));
        }
        public UnitArrangementProgram Prototype() => new(Frame, "name does not identify a concept",
            S(new Relation(S(), S(Word(1, 1), Word(1, -1)))), S());
        public UnitArrangementProgram Place(UnitArrangementProgram prototype, int x)
        {
            Relation Row(IElement direction) => new(QualitativeMotionFrame.Move(direction, reach),
                S(QualitativeMotionFrame.Move(direction, reach)));
            var map = new QualitativePatternTransform(Frame, Frame,
                S(Row(Frame.Right), Row(Frame.Left), Row(Frame.Up), Row(Frame.Down)));
            var mapped = prototype.Transform("actual native transform", map);
            var mappedPath = (Relation)Assert.Single(E(mapped.Paths)); var offset = Word(x, 0);
            Assert.True(OrnamentEnrichment.TryAnchorWord(source, anchor, out var anchorWord, out var anchorRead));
            var offsetRead = Frame.Reduce(offset, (Relation)mappedPath.Left);
            Assert.True(Frame.TryReadResult(offsetRead, out var relative, out _, out _));
            var placement = Frame.Reduce(anchorWord, relative);
            Assert.True(Frame.TryReadResult(placement, out var start, out _, out _));
            var actual = new Relation(start, mappedPath.Right);
            var evidence = new Relation(mapped.Surface, new Relation(new Relation(mappedPath, actual),
                new Relation(anchor, new Relation(anchorRead, new Relation(offsetRead, placement)))));
            return new(Frame, "actual native attachment", S(actual), S(), evidence: evidence);
        }
        public OrnamentEnrichment Enrich(params UnitArrangementProgram[] additions) => new(source,
            S(OrnamentEnrichment.Layer(new Placeholder(), S(), S(additions.Select(program => (IElement)program.Surface).ToArray()),
                S(additions.Select(program =>
                {
                    var path = (Relation)Assert.Single(E(program.Paths));
                    return (IElement)OrnamentEnrichment.Attach(anchor, (Relation)path.Left, path);
                }).ToArray()))));
        private Relation Word(int x, int y) => S(Enumerable.Repeat<IElement>(QualitativeMotionFrame.Move(x < 0 ? Frame.Left : Frame.Right, reach), Math.Abs(x))
            .Concat(Enumerable.Repeat<IElement>(QualitativeMotionFrame.Move(y < 0 ? Frame.Down : Frame.Up, reach), Math.Abs(y))).ToArray());
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
