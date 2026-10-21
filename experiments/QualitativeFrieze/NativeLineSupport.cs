using Gojo.Core;
using Gojo.Patterns;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A non-draining support read over actual source incidence. Native
/// pure-axis passages may continue through a shared site when that axis has
/// exactly one incoming and one outgoing geometric link. Equal endpoint words
/// never create a shared site. Every source occurrence remains in the oriented
/// correspondence; this does not modify the source's physical topology.</summary>
public sealed class NativeLineSupport
{
    public QualitativeStrokeGraph Graph { get; }
    public Relation Bindings { get; }
    public Relation Correspondence { get; }
    public Relation Reductions { get; }
    public Relation Unresolved { get; }
    public Relation Surface { get; }

    private sealed record Use(Relation Stroke, IElement From, IElement To, Relation Word)
    { public Relation Surface => new(Stroke, new Relation(From, To)); }
    private sealed class Link(IElement from, IElement to, Relation axis)
    {
        public IElement From { get; } = from;
        public IElement To { get; } = to;
        public Relation Axis { get; } = axis;
        public List<Use> Uses { get; } = [];
        public Relation Word => Uses[0].Word;
    }

    public NativeLineSupport(QualitativeStrokeGraph source, Relation bindings)
    {
        var pending = new List<IElement>(); var reductions = new List<IElement>();
        var links = new List<Link>(); var passthrough = new List<Relation>();
        if (!N.Read(source.Sites, out var sourceSites)) pending.Add(source.Sites);
        if (!N.Read(source.Strokes, out var strokes)) pending.Add(source.Strokes);
        foreach (var item in strokes)
        {
            if (item is not Relation stroke || !QualitativeStrokeGraph.TryReadStroke(stroke,
                    out _, out var from, out var to, out var word) ||
                !sourceSites.Any(site => N.Same(site, from)) || !sourceSites.Any(site => N.Same(site, to)) || N.Same(from, to))
            { pending.Add(item); if (item is Relation malformed) passthrough.Add(malformed); continue; }
            var reduction = source.Frame.Reduce(N.S(), word);
            reductions.Add(new Relation(stroke, reduction));
            if (!source.Frame.TryReadResult(reduction, out var remaining, out _, out var unresolved) ||
                N.E(unresolved).Length != 0 || !N.Read(remaining, out var moves) || moves.Length == 0)
            { pending.Add(new Relation(stroke, reduction)); passthrough.Add(stroke); continue; }
            Relation? axis = null; bool? forward = null; var pure = true;
            foreach (var term in moves)
            {
                if (term is not Relation move || !source.Frame.Direction(move, out var actualAxis, out var actualForward, out _) ||
                    axis is not null && !ReferenceEquals(axis, actualAxis) || forward is not null && forward != actualForward)
                { pure = false; break; }
                axis = actualAxis; forward = actualForward;
            }
            if (!pure || axis is null || forward is null)
            { pending.Add(new Relation(stroke, reduction)); passthrough.Add(stroke); continue; }
            var a = forward.Value ? from : to; var b = forward.Value ? to : from;
            var oriented = forward.Value ? word : UnitArrangementRead.Inverse(source.Frame, word);
            var link = links.FirstOrDefault(candidate => ReferenceEquals(candidate.Axis, axis) &&
                N.Same(candidate.From, a) && N.Same(candidate.To, b));
            if (link is null) { link = new(a, b, axis); links.Add(link); }
            link.Uses.Add(new(stroke, a, b, oriented));
        }

        var output = new List<IElement>(); var correspondence = new List<IElement>();
        var visited = new HashSet<Link>();
        Link[] Incoming(Link link, IElement site) => links.Where(other => ReferenceEquals(other.Axis, link.Axis) && N.Same(other.To, site)).ToArray();
        Link[] Outgoing(Link link, IElement site) => links.Where(other => ReferenceEquals(other.Axis, link.Axis) && N.Same(other.From, site)).ToArray();
        bool Continues(Link link, IElement site) => Incoming(link, site).Length == 1 && Outgoing(link, site).Length == 1;
        foreach (var start in links.Where(link => !Continues(link, link.From))) Append(start);
        // An inconsistent pure-axis circuit has no justified open support.
        // Preserve its links rather than inventing a zero-ended coarse passage.
        foreach (var start in links.Where(link => !visited.Contains(link)).ToArray()) Append(start);
        foreach (var stroke in passthrough)
        {
            output.Add(stroke);
            var use = QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out _)
                ? (IElement)new Relation(stroke, new Relation(from, to)) : new Relation(stroke, StructuralConstraint.Open());
            correspondence.Add(new Relation(stroke, N.S(use)));
        }

        var selectedSites = new List<IElement>();
        foreach (var stroke in output.OfType<Relation>())
            if (QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out _))
                foreach (var site in new[] { from, to })
                    if (!selectedSites.Any(existing => N.Same(existing, site))) selectedSites.Add(site);
        Relation Scope(Relation original, IElement[] entries) => N.Read(original, out _) ? N.S(entries) : StructuralConstraint.Open(entries);
        Graph = new(source.Frame, Scope(source.Sites, selectedSites.ToArray()), Scope(source.Strokes, output.ToArray()), source.PeriodWord);
        if (!N.Read(bindings, out var rows)) pending.Add(bindings);
        Bindings = Scope(bindings, rows.Where(row => row is Relation binding && selectedSites.Any(site => N.Same(site, binding.Left))).ToArray());
        Correspondence = N.S(correspondence.ToArray()); Reductions = N.S(reductions.ToArray()); Unresolved = N.S(pending.ToArray());
        Surface = new(new Relation(source.Surface, bindings), new Relation(Reductions,
            new Relation(new Relation(Graph.Surface, Bindings), new Relation(Correspondence, Unresolved))));

        void Append(Link start)
        {
            if (visited.Contains(start)) return;
            var chain = new List<Link>(); var local = new HashSet<Link>(); var current = start; var cyclic = false;
            while (!visited.Contains(current))
            {
                if (!local.Add(current)) { cyclic = true; break; }
                chain.Add(current);
                if (!Continues(current, current.To)) break;
                current = Outgoing(current, current.To)[0];
            }
            if (cyclic || chain.Count > 0 && N.Same(chain[0].From, chain[^1].To))
            {
                pending.Add(new Relation(N.S(chain.SelectMany(link => link.Uses).Select(use => (IElement)use.Surface).ToArray()), source.Surface));
                foreach (var link in chain) Emit([link]);
            }
            else if (chain.Count > 0) Emit(chain);
        }
        void Emit(IReadOnlyList<Link> chain)
        {
            var word = N.S(chain.SelectMany(link => N.E(link.Word)).ToArray());
            var stroke = QualitativeStrokeGraph.Stroke(new Placeholder(), chain[0].From, chain[^1].To, word);
            output.Add(stroke);
            correspondence.Add(new Relation(stroke, N.S(chain.SelectMany(link => link.Uses).Select(use => (IElement)use.Surface).ToArray())));
            foreach (var link in chain) visited.Add(link);
        }
    }
}
