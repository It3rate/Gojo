using System.Text.Json.Serialization;
using Gojo.Core;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Diagnostic construction reads, not discovered human concepts.
/// Native sharing is read from actual retained provenance. The separate
/// editorial estimates declare a coarser grammar and keep their uncertainty.</summary>
public static class OrnamentMotifComplexity
{
    public const int ReviewTrigger = 12;
    public const string Granularity = "One repeating unit's ornamental construction vocabulary: reusable shapes, internal articulation, symmetry/repetition and attachment rules. Repeated instances are not counted separately; the underlying lattice, palette and display fillets are outside this estimate.";

    public sealed record ReviewedEstimate(int Minimum, int Maximum, string[] ConceptualRecipe)
    {
        public bool MayExceedReviewTrigger => Maximum > ReviewTrigger;
        public bool EntireRangeExceedsReviewTrigger => Minimum > ReviewTrigger;
        public string Basis => "Authored construction review; not autonomous conceptual discovery or a measured cognitive load.";
    }

    public sealed record EncodingCounts(int SourcePaths, int AddedPaths, int SourceChords,
        int AddedChords, int RetainedPrototypeIdentities, int WitnessedPrototypePlacements,
        int UnattributedAddedPaths, int ActualSourceAnchors);

    public sealed record EncodingRead(EncodingCounts Counts,
        [property: JsonIgnore] Relation PrototypeGroups,
        [property: JsonIgnore] Relation Unattributed,
        [property: JsonIgnore] Relation Surface);

    public sealed record Report(string SourceId, string Mode, ReviewedEstimate? Estimate,
        EncodingCounts Encoding, [property: JsonIgnore] Relation Evidence)
    {
        public string EstimateGranularity => Granularity;
        public int SuggestedReviewTrigger => ReviewTrigger;
        public string Limit => "Prototype identities and path/chord counts describe the encoding. They are not the conceptual estimate, do not establish a minimal grammar, and do not measure visual crowding.";
    }

    public static Report Review(BourgoinOrnamentDesignLibrary.Design design) =>
        Review(design.SourceId, design.Mode, design.Enrichment);

    public static Report Review(string sourceId, string mode, OrnamentEnrichment enrichment)
    {
        var native = ReadEncoding(enrichment);
        return new(sourceId, mode, mode == "detail" ? FineEstimate(sourceId) : null,
            native.Counts, native.Surface);
    }

    /// <summary>Reads only admitted enrichment rows and their exact attachment /
    /// transform receipt positions. It does not mine arbitrary ancestry or infer
    /// sharing from equal coordinates. Unrecognized ancestry stays attributed
    /// only to its actual added path, not to a guessed prototype.</summary>
    public static EncodingRead ReadEncoding(OrnamentEnrichment enrichment)
    {
        var sourcePaths = N.E(enrichment.Source.Paths).Cast<Relation>().ToArray();
        var addedPaths = N.E(enrichment.Result.Paths).Cast<Relation>()
            .Where(path => !sourcePaths.Any(source => ReferenceEquals(source, path))).ToArray();
        var groups = new Dictionary<Relation, List<Relation>>(ReferenceEqualityComparer.Instance);
        var recognized = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
        var anchors = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
        foreach (var layer in N.E(enrichment.Layers).Cast<Relation>())
        {
            if (!enrichment.TryReadLayer(layer, out var definition, out _, out _, out var added, out _) ||
                definition.Right is not Relation { Right: Relation { Left: Relation additions, Right: Relation attachments } }) continue;
            foreach (var attachment in N.E(attachments).OfType<Relation>())
                if (attachment.Left is Relation anchor && N.E(enrichment.Source.Ports).Any(port => ReferenceEquals(port, anchor))) anchors.Add(anchor);
            foreach (var addition in N.E(additions).OfType<Relation>())
            {
                if (!TryPrototype(enrichment, addition, out var prototype, out var actualPaths)) continue;
                if (!actualPaths.All(path => N.E(added).Any(actual => ReferenceEquals(actual, path)))) continue;
                if (!groups.TryGetValue(prototype, out var instances)) groups.Add(prototype, instances = []);
                foreach (var path in actualPaths)
                    if (recognized.Add(path)) instances.Add(path);
            }
        }
        var selected = N.S(groups.Select(group => (IElement)new Relation(group.Key,
            N.S(group.Value.Cast<IElement>().ToArray()))).ToArray());
        var unattributed = N.S(addedPaths.Where(path => !recognized.Contains(path)).Cast<IElement>().ToArray());
        var counts = new EncodingCounts(sourcePaths.Length, addedPaths.Length, Chords(sourcePaths), Chords(addedPaths),
            groups.Count, recognized.Count, N.E(unattributed).Length, anchors.Count);
        // Selection evidence retains the actual source and actual prototype /
        // added-path identities; the diagnostic count can be recalculated.
        var surface = new Relation(enrichment.Surface, new Relation(selected, new Relation(unattributed,
            new Relation(N.S(anchors.Cast<IElement>().ToArray()), N.S(addedPaths.Cast<IElement>().ToArray())))));
        return new(counts, selected, unattributed, surface);
    }

    private static bool TryPrototype(OrnamentEnrichment enrichment, Relation addition,
        out Relation prototype, out Relation[] actualPaths)
    {
        prototype = null!; actualPaths = [];
        if (!Program(addition, enrichment, out var paths, out var evidence) ||
            evidence is not Relation { Left: Relation mapped,
                Right: Relation { Left: Relation { Left: Relation mappedPath, Right: Relation actualPath },
                    Right: Relation { Left: Relation actualAnchor } } } ||
            !paths.Contains(actualPath, ReferenceEqualityComparer.Instance) || paths.Length != 1 ||
            !N.E(enrichment.Source.Ports).Any(port => ReferenceEquals(port, actualAnchor)) ||
            !Program(mapped, enrichment, out var mappedPaths, out var mapEvidence) ||
            !mappedPaths.Contains(mappedPath, ReferenceEqualityComparer.Instance) ||
            mapEvidence is not Relation { Left: Relation candidate,
                Right: Relation { Right: Relation { Left: Relation { Left: Relation pathCorrespondences } } } } ||
            !Program(candidate, enrichment, out var prototypePaths, out _) ||
            !N.E(pathCorrespondences).OfType<Relation>().Any(row =>
                prototypePaths.Any(path => ReferenceEquals(path, row.Left)) && ReferenceEquals(row.Right, mappedPath))) return false;
        prototype = candidate; actualPaths = paths; return true;
    }

    private static bool Program(Relation surface, OrnamentEnrichment enrichment,
        out Relation[] paths, out IElement evidence)
    {
        paths = []; evidence = null!;
        if (!ReferenceEquals(surface.Left, enrichment.Source.Frame.Surface) ||
            surface.Right is not Relation { Left: Relation { Left: Relation pathScope, Right: Relation ports },
                Right: Relation { Left: Relation closed, Right: var retained } } || !N.Read(pathScope, out var entries) ||
            entries.Any(entry => entry is not Relation)) return false;
        var program = new UnitArrangementProgram(enrichment.Source.Frame, "complexity provenance read", pathScope, ports, closed, retained);
        if (!program.Complete()) return false;
        paths = entries.Cast<Relation>().ToArray(); evidence = retained; return true;
    }

    private static int Chords(IEnumerable<Relation> paths) => paths.Sum(path => N.E((Relation)path.Right).Length);

    // An explicit editorial vocabulary. These ranges are deliberately not
    // calculated from names, path counts, palette indexes, or SVG geometry.
    // They are hypotheses for future alternative native chunkings to challenge.
    public static ReviewedEstimate? FineEstimate(string sourceId) => sourceId switch
    {
        "B01" => Estimate(6, 9, "broad leaf contour", "axial vein", "repeated fork veins", "nested inner leaf", "paired brick-interior placement", "parent attachment"),
        "B02" => Estimate(6, 8, "strip-following stem", "leaf contour", "midrib and branch veins", "alternating rooted leaves", "terminal seed", "quarter-related strip placement"),
        "B03" or "B06" or "B06.B" => Estimate(6, 9, "eight-lobed rosette contour", "radial vein", "eightfold vein arrangement", "centre", "complementary cell-family placement", "parent attachment"),
        "B04" or "B25" or "B26" or "B29" => Estimate(6, 9, "shared petal", "petal vein", "sixfold arrangement", "centre", "interior-relative placement", "parent attachment"),
        "B20" => Estimate(5, 8, "interior leaf", "internal vein", "local symmetric correspondence", "cell-relative attachment", "repeat through the parent disposition"),
        "B05" => Estimate(5, 8, "petal contour", "midrib and forked veins", "sixfold arrangement", "central seed", "shared parent landmark"),
        "B07" => Estimate(5, 8, "broad eight-lobed rosette contour", "radial veins", "centre", "wide grid-interior placement", "parent attachment"),
        "B08" or "B08.B" => Estimate(8, 11, "long petal", "short petal", "radial alternation", "petal veins", "opposed horizontal leaves", "nested centre collars", "crossing flower", "diamond-room placement"),
        "B09" => Estimate(7, 10, "palmette outline", "central rib", "opposed side ribs", "nested fan articulation", "centre collar", "opposed return-cup maps", "source-relative attachment"),
        "B10" or "B11" => Estimate(7, 10, "long diagonal petal", "short axial petal", "simple petal vein", "fourfold interleaving", "single centre", "chamber-relative attachment", "alternation between chambers"),
        "B12" => Estimate(8, 11, "long diagonal petal", "short axial petal", "fourfold repetition", "interleaved petal family", "midrib", "branch veins", "nested centre", "chamber-relative rotation and attachment"),
        "B13" or "B14" => Estimate(4, 7, "simple leaf contour", "quiet axial vein", "opposed common-root pair", "bar-aligned placement", "quarter-related sibling correspondence"),
        "B15" => Estimate(5, 8, "elongated leaf contour", "midrib", "forked veins", "oblique correspondence", "cell-relative placement"),
        "B16" or "B24" => Estimate(5, 8, "contained petal", "petal vein", "threefold arrangement", "triangular-cell attachment", "centre"),
        "B17" or "B18" => Estimate(5, 8, "leaf contour", "midrib or internal articulation", "course following", "opposed/alternating local map", "parent attachment"),
        "B19" or "B22" => Estimate(6, 9, "elongated interior leaf", "axial vein", "repeated fork veins", "nested inner leaf", "cell-axis correspondence", "shared parent attachment"),
        "B21" => Estimate(5, 8, "elongated interior leaf", "midrib", "cell-axis correspondence", "shared parent attachment", "repetition through the disposition"),
        "B23" => Estimate(5, 8, "eight-lobed flower contour", "radial vein", "radial vein arrangement", "centre", "square-relative attachment"),
        "B27" => Estimate(8, 11, "ridge foliage", "leaf veins", "paired ridge correspondence", "inset diamond", "eight-lobed flower contour", "radial veins", "centre", "parent-course attachment"),
        "B28" => Estimate(6, 9, "five-lobed fan contour", "fan rib", "five-part rib arrangement", "centre", "common root", "shell-relative attachment"),
        "B30" => Estimate(5, 8, "eight-lobed flower contour", "radial veins", "centre", "cloud-valley placement", "repeat through the parent disposition"),
        "B31" => Estimate(8, 11, "central blossom contour", "radial veins", "centre", "wrist leaf", "leaf articulation", "quarter-related arm correspondence", "wrist attachment", "source marker retained beneath the blossom"),
        "B32" => Estimate(5, 7, "small diamond-based petal", "quarter-related fourfold rosette", "quiet midrib and centre", "actual hook-start attachment", "opposed crossing-family correspondence", "repeat through the staggered disposition"),
        "B33" => Estimate(6, 9, "eight-lobed blossom contour", "radial veins", "centre", "short connecting stem", "actual source-course attachment", "repeat through the staggered disposition"),
        "B34" => Estimate(5, 9, "central blossom contour", "radial veins", "centre", "crossing-relative placement", "unjoined loop histories retained"),
        _ => null
    };

    private static ReviewedEstimate Estimate(int minimum, int maximum, params string[] terms) => new(minimum, maximum, terms);
}
