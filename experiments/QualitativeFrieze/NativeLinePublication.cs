using Gojo.Core;
using Gojo.Patterns;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A deliberately smaller publication of current native geometry.
/// Current incidence, ordered words, reach aliases and interpolation offers
/// survive; execution causes and private endpoint bindings do not. Maps and
/// the original source belong to this external audit object, never to the
/// Published source or a supplier port built from it.</summary>
public sealed class NativeLinePublication
{
    public NativeLatticeSources.SourceCase Original { get; }
    public NativeLatticeSources.SourceCase Published { get; }
    /// <summary>Closed(original site -> fresh public site).</summary>
    public Relation SiteMap { get; }
    /// <summary>Closed(original stroke row -> public stroke row).</summary>
    public Relation StrokeMap { get; }
    /// <summary>Closed(original reach participant -> opaque public reach).</summary>
    public Relation ReachMap { get; }
    public Relation Unresolved { get; }
    /// <summary>Private audit surface. Do not include this in the public port.</summary>
    public Relation Surface { get; }

    private readonly Dictionary<IElement, IElement> sites = new(N.Participants.Instance);
    private readonly Dictionary<IElement, IElement> reaches = new(N.Participants.Instance);
    private readonly Dictionary<Relation, Relation> strokes = new(ReferenceEqualityComparer.Instance);

    public NativeLinePublication(NativeLatticeSources.SourceCase original)
    {
        Original = original;
        var pending = new List<IElement>();
        var opaque = new Dictionary<IElement, IElement>(N.Participants.Instance);
        var wordCopies = new Dictionary<Relation, Relation>(ReferenceEqualityComparer.Instance);
        var moveCopies = new Dictionary<IElement, IElement>(N.Participants.Instance);
        var sourceSites = Entries(original.Graph.Sites);
        var publicSites = sourceSites.Select(MapSite).ToArray();
        var publicStrokes = new List<IElement>();
        foreach (var item in Entries(original.Graph.Strokes))
        {
            if (item is not Relation actual)
            {
                var unknown = new Relation(Opaque(item), StructuralConstraint.Open());
                publicStrokes.Add(unknown); pending.Add(new Relation(item, unknown)); continue;
            }
            if (!strokes.TryGetValue(actual, out var published))
            {
                if (QualitativeStrokeGraph.TryReadStroke(actual, out var cause, out var from, out var to, out var word) &&
                    sourceSites.Any(site => N.Same(site, from)) && sourceSites.Any(site => N.Same(site, to)) && !N.Same(from, to))
                    published = QualitativeStrokeGraph.Stroke(Opaque(cause), MapSite(from), MapSite(to), CopyWord(word));
                else
                {
                    published = new Relation(Opaque(actual), StructuralConstraint.Open());
                    pending.Add(new Relation(actual, published));
                }
                strokes.Add(actual, published);
            }
            publicStrokes.Add(published);
        }
        var publicBindings = new List<IElement>();
        foreach (var item in Entries(original.Bindings))
        {
            if (item is Relation binding && sites.TryGetValue(binding.Left, out var site))
            {
                var word = binding.Right is Relation actualWord ? CopyWord(actualWord) : StructuralConstraint.Open();
                var published = new Relation(site, word); publicBindings.Add(published);
                if (binding.Right is not Relation) pending.Add(new Relation(binding, published));
            }
            else
            {
                // Keep an unreadable row visible without exporting any of its
                // old participants or pretending that it binds a known site.
                var unknown = new Relation(Opaque(item), StructuralConstraint.Open());
                publicBindings.Add(unknown); pending.Add(new Relation(item, unknown));
            }
        }
        var publicStraight = new List<IElement>();
        foreach (var item in Entries(original.StraightPassages))
            if (item is Relation actual && strokes.TryGetValue(actual, out var published)) publicStraight.Add(published);
            else
            {
                var unknown = Opaque(item); publicStraight.Add(unknown); pending.Add(new Relation(item, unknown));
            }
        var graph = new QualitativeStrokeGraph(original.Graph.Frame,
            Scope(original.Graph.Sites, publicSites), Scope(original.Graph.Strokes, publicStrokes.ToArray()), CopyWord(original.Graph.PeriodWord));
        Published = original with { Graph = graph, Bindings = Scope(original.Bindings, publicBindings.ToArray()),
            StraightPassages = Scope(original.StraightPassages, publicStraight.ToArray()), Provenance = N.S() };
        SiteMap = N.S(sites.Select(row => (IElement)new Relation(row.Key, row.Value)).ToArray());
        StrokeMap = N.S(strokes.Select(row => (IElement)new Relation(row.Key, row.Value)).ToArray());
        ReachMap = N.S(reaches.Select(row => (IElement)new Relation(row.Key, row.Value)).ToArray());
        Unresolved = N.S(pending.ToArray());
        Surface = new(new Relation(original.Graph.Surface, new Relation(original.Bindings, original.StraightPassages)),
            new Relation(new Relation(graph.Surface, new Relation(Published.Bindings, Published.StraightPassages)),
                new Relation(SiteMap, new Relation(StrokeMap, new Relation(ReachMap, Unresolved)))));

        IElement Opaque(IElement originalParticipant)
        {
            if (!opaque.TryGetValue(originalParticipant, out var identity))
            {
                // Preserve placeholder dual faces without exposing their old
                // slot IDs. Non-placeholder extents are deliberately opaque.
                if (originalParticipant is Placeholder placeholder && opaque.TryGetValue(placeholder.Dual(), out var inverse)) identity = inverse.Dual();
                else { var fresh = new Placeholder(); identity = originalParticipant is Placeholder { IsInverse: true } ? fresh.Dual() : fresh; }
                opaque.Add(originalParticipant, identity);
            }
            return identity;
        }
        IElement MapSite(IElement originalSite)
        {
            if (!sites.TryGetValue(originalSite, out var site)) { site = Opaque(originalSite); sites.Add(originalSite, site); }
            return site;
        }
        Relation CopyWord(Relation originalWord)
        {
            if (wordCopies.TryGetValue(originalWord, out var existing)) return existing;
            if (!StructuralConstraint.TryReadScope(originalWord, out var terms, out var head, out var tail))
            {
                var unread = StructuralConstraint.Open(); wordCopies.Add(originalWord, unread);
                pending.Add(new Relation(originalWord, unread)); return unread;
            }
            var copies = new List<IElement>();
            foreach (var term in terms)
            {
                if (!moveCopies.TryGetValue(term, out var copy))
                {
                    if (term is Relation move && original.Graph.Frame.Direction(move, out _, out _, out var reach))
                    {
                        if (!reaches.TryGetValue(reach, out var publicReach)) { publicReach = Opaque(reach); reaches.Add(reach, publicReach); }
                        copy = new Relation(move.Left, publicReach);
                    }
                    else
                    {
                        copy = new Relation(Opaque(term), new Placeholder()); pending.Add(new Relation(term, copy));
                    }
                    moveCopies.Add(term, copy);
                }
                copies.Add(copy);
            }
            var result = head.Equals(tail) ? N.S(copies.ToArray()) : StructuralConstraint.Open(copies.ToArray());
            wordCopies.Add(originalWord, result);
            if (!head.Equals(tail)) pending.Add(new Relation(originalWord, result));
            return result;
        }
        Relation Scope(Relation originalScope, IElement[] rows)
        {
            if (StructuralConstraint.TryReadScope(originalScope, out _, out var head, out var tail) && head.Equals(tail)) return N.S(rows);
            var result = StructuralConstraint.Open(rows); pending.Add(new Relation(originalScope, result)); return result;
        }
    }

    public bool TryMapSite(IElement original, out IElement published) => sites.TryGetValue(original, out published!);
    public bool TryMapStroke(Relation original, out Relation published) => strokes.TryGetValue(original, out published!);
    public bool TryMapReach(IElement original, out IElement published) => reaches.TryGetValue(original, out published!);

    private static IElement[] Entries(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
}
