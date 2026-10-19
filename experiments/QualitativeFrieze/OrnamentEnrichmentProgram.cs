using Gojo.Core;
using Gojo.Patterns;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Installs one enriched path declaration into an existing native
/// ornament recipe. Only declared motif alternatives, concrete bindings and
/// seam selectors are rebound. Native phase/layout structure is retained;
/// arbitrary ancestry is never recursively replaced.</summary>
public sealed class OrnamentEnrichmentProgram
{
    public OrnamentProgram Source { get; }
    public OrnamentEnrichment Enrichment { get; }
    public OrnamentProgram Program { get; }
    public Relation Bindings { get; }
    public Relation SeamContract { get; }
    public Relation Correspondence { get; }
    public Relation Surface { get; }

    public OrnamentEnrichmentProgram(OrnamentProgram source, Relation bindings, Relation seamContract, OrnamentEnrichment enrichment)
    {
        Source = source;
        if (!ReferenceEquals(source.Frame, enrichment.Source.Frame) || !enrichment.IsComplete)
            throw new ArgumentException("Installation requires a complete enrichment in the source program's actual frame.", nameof(enrichment));
        var sourceMotifs = Read(source.Motifs);
        var matches = sourceMotifs.OfType<Relation>().Where(motif => SameDeclaration(motif, enrichment.SourceSurface)).ToArray();
        if (matches is not [var oldMotif])
            throw new ArgumentException("Exactly one actual source motif must supply the enrichment's path declaration.", nameof(enrichment));
        // A caller may have used a parsed program wrapper. Restore the actual
        // source declaration identity before retaining the installed history.
        Enrichment = ReferenceEquals(oldMotif, enrichment.SourceSurface) ? enrichment :
            new OrnamentEnrichment(source.Frame, oldMotif, enrichment.Definitions);
        if (!Enrichment.IsComplete) throw new ArgumentException("The installed source must retain a complete enrichment.", nameof(enrichment));
        var newMotif = Enrichment.ResultSurface;
        var motifs = N.S(sourceMotifs.Select(item => ReferenceEquals(item, oldMotif) ? newMotif : item).ToArray());
        var parameterMaps = new List<IElement>();
        var domainMap = new Dictionary<IElement, Relation>(ReferenceEqualityComparer.Instance);
        var parameters = N.S(Read(source.Parameters).Select(item =>
        {
            if (item is not Relation { Right: Relation alternatives } row) throw new ArgumentException("Parameter domains must be native closed scopes.", nameof(source));
            var domain = Read(alternatives); if (!domain.Any(item => ReferenceEquals(item, oldMotif))) return item;
            if (!domainMap.TryGetValue(alternatives, out var mappedDomain))
            {
                mappedDomain = N.S(domain.Select(item => ReferenceEquals(item, oldMotif) ? newMotif : item).ToArray());
                domainMap.Add(alternatives, mappedDomain);
            }
            var replacement = new Relation(row.Left, mappedDomain);
            parameterMaps.Add(new Relation(row, replacement)); return replacement;
        }).ToArray());
        Program = new(source.Frame, motifs, source.Transforms, parameters, source.Layout);
        var bindingMaps = new List<IElement>();
        Bindings = N.S(Read(bindings).Select(item =>
        {
            if (item is not Relation row) throw new ArgumentException("Bindings must be native relations.", nameof(bindings));
            if (!ReferenceEquals(row.Right, oldMotif)) return item;
            var replacement = new Relation(row.Left, newMotif); bindingMaps.Add(new Relation(row, replacement)); return replacement;
        }).ToArray());
        if (source.Layout.Left is not Relation { Left: Relation u, Right: Relation v } ||
            seamContract is not { Left: Relation { Left: var actualSourceProgram, Right: Relation axes }, Right: Relation rules } ||
            !ReferenceEquals(actualSourceProgram, source.Surface) || !ReferenceEquals(axes.Left, u) || !ReferenceEquals(axes.Right, v))
            throw new ArgumentException("The seam law must expose this source program and its two actual layout cycles.", nameof(seamContract));
        var seamMaps = new List<IElement>(); var stepMaps = new List<IElement>();
        var stepMap = new Dictionary<IElement, Relation>(ReferenceEqualityComparer.Instance);
        var selectorMap = new Dictionary<IElement, Relation>(ReferenceEqualityComparer.Instance);
        var mappedRules = Read(rules).Select(item =>
        {
            if (item is not Relation { Left: Relation { Left: Relation from, Right: Relation to },
                    Right: Relation { Left: Relation offset, Right: Relation roles } } rule)
                throw new ArgumentException("A seam rule must expose motif/transform selectors, native offset and roles.", nameof(seamContract));
            var steps = Read(offset).Select(step =>
            {
                if (step is not Relation direction) throw new ArgumentException("A seam offset must select an actual program cycle.", nameof(seamContract));
                if (stepMap.TryGetValue(direction, out var retained)) return (IElement)retained;
                Relation replacement;
                if (ReferenceEquals(direction.Left, source.Surface) && (ReferenceEquals(direction.Right, u) || ReferenceEquals(direction.Right, v)))
                    replacement = new(Program.Surface, direction.Right);
                else if (ReferenceEquals(direction.Right, source.Surface) && (ReferenceEquals(direction.Left, u) || ReferenceEquals(direction.Left, v)))
                    replacement = new(direction.Left, Program.Surface);
                else throw new ArgumentException("A seam offset must retain the source program's actual cycle direction.", nameof(seamContract));
                stepMap.Add(direction, replacement); stepMaps.Add(new Relation(direction, replacement)); return (IElement)replacement;
            }).ToArray();
            var replacement = new Relation(new Relation(MapSelector(from), MapSelector(to)),
                new Relation(N.S(steps), roles));
            seamMaps.Add(new Relation(rule, replacement)); return (IElement)replacement;
        }).ToArray();
        SeamContract = OrnamentProgramField.Contract(Program, mappedRules.Cast<Relation>().ToArray());
        Correspondence = new(new Relation(source.Surface, Program.Surface),
            new Relation(new Relation(oldMotif, newMotif), new Relation(N.S(parameterMaps.ToArray()),
                new Relation(N.S(bindingMaps.ToArray()), new Relation(N.S(seamMaps.ToArray()), N.S(stepMaps.ToArray()))))));
        Surface = new(new Relation(new Relation(source.Surface, bindings), new Relation(seamContract, enrichment.Surface)),
            new Relation(new Relation(Program.Surface, Bindings), new Relation(SeamContract, new Relation(Enrichment.Surface, Correspondence))));
        Relation MapSelector(Relation actual)
        {
            if (!ReferenceEquals(actual.Left, oldMotif)) return actual;
            if (!selectorMap.TryGetValue(actual, out var mapped))
            { mapped = new(newMotif, actual.Right); selectorMap.Add(actual, mapped); }
            return mapped;
        }
    }

    public string Save(Relation window) => OrnamentFieldPacket.Save(Program, Bindings, window, SeamContract);

    private static IElement[] Read(Relation scope) => N.Read(scope, out var entries) ? entries :
        throw new ArgumentException("Installation requires a closed native declaration scope.", nameof(scope));
    private static bool SameDeclaration(Relation actual, Relation expected) => ReferenceEquals(actual, expected) ||
        ReferenceEquals(actual.Left, expected.Left) &&
        actual.Right is Relation { Left: Relation { Left: var paths, Right: var ports }, Right: Relation { Left: var closed, Right: var evidence } } &&
        expected.Right is Relation { Left: Relation { Left: var expectedPaths, Right: var expectedPorts }, Right: Relation { Left: var expectedClosed, Right: var expectedEvidence } } &&
        ReferenceEquals(paths, expectedPaths) && ReferenceEquals(ports, expectedPorts) && ReferenceEquals(closed, expectedClosed) && ReferenceEquals(evidence, expectedEvidence);
}
