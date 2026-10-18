using Gojo.Core;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A non-draining, possibly partial cover of bounded source chambers under an explicit
/// question. Groups may overlap; neither shared membership nor boundary contact
/// joins source sites, consumes participation, or makes groups interchangeable.
/// The supplied cellular embedding remains a premise of this observation.</summary>
public sealed class OrnamentView
{
    // definition = group identity -> Closed(actual bounded source faces)
    // Surface = (source.Surface -> Closed(definitions)) -> context
    // group = (Surface -> actual definition) ->
    //   ((faces -> (actual strokes -> actual sites)) ->
    //    (complement faces -> (internal seams -> (boundary seams -> cut seams))))
    // seam = actual source stroke -> Closed(actual incident face -> actual
    //                                       oriented boundary participation)
    // adjacency = (actual group read -> actual group read) -> Closed(seams)
    // overlap = (actual group read -> actual group read) -> Closed(shared faces)
    // read = Surface -> (groups -> (selected faces -> (complement faces ->
    //            (adjacency -> (overlaps -> (blocked -> unresolved))))))
    public ScaffoldQuestionScaffold Source { get; }
    public Relation Definitions { get; }
    public IElement Context { get; }
    public Relation Surface { get; }
    public Relation Groups { get; }
    public Relation SelectedFaces { get; }
    public Relation Complement { get; }
    public Relation Adjacency { get; }
    public Relation Overlaps { get; }
    public Relation Blocked { get; }
    public Relation Unresolved { get; }
    /// <summary>The supplied membership question has a complete read. An exact
    /// complement may remain; this does not claim the groups exhaust the source.</summary>
    public bool IsComplete => N.E(Blocked).Length == 0 && N.E(Unresolved).Length == 0;
    private readonly Relation read;

    public OrnamentView(ScaffoldQuestionScaffold source, Relation groups, IElement context)
    {
        Source = source; Definitions = groups; Context = context;
        Surface = new(new Relation(source.Surface, groups), context);
        var blocked = new List<IElement>(); var pending = new List<IElement>();
        var accepted = new List<IElement>();
        var allFaces = N.E(source.Faces);
        var sourceReady = source.IsComplete;
        if (!sourceReady) pending.Add(source.Evaluate());
        var scopeReady = N.Read(groups, out var definitions) && N.Distinct(definitions);
        if (!scopeReady) pending.Add(groups);
        var identities = definitions.OfType<Relation>().Select(definition => definition.Left).ToArray();
        if (!N.Distinct(identities)) { blocked.Add(groups); scopeReady = false; }
        var seams = sourceReady ? SourceSeams(source) : [];
        foreach (var item in definitions)
        {
            if (!sourceReady || !scopeReady) { pending.Add(new Relation(item, source.Surface)); continue; }
            if (item is not Relation { Right: Relation selection } definition)
            { blocked.Add(new Relation(item, source.Faces)); continue; }
            if (!N.Read(selection, out var faces))
            { pending.Add(new Relation(definition, selection)); continue; }
            if (faces.Length == 0 || !N.Distinct(faces) || faces.Any(face =>
                face is not Relation actual || !allFaces.Any(candidate => ReferenceEquals(candidate, face)) ||
                !source.TryReadFace(actual, out _, out _)))
            { blocked.Add(new Relation(definition, source.Faces)); continue; }
            var boundaryParts = faces.Cast<Relation>().SelectMany(face =>
            {
                source.TryReadFace(face, out var boundary, out _); return N.E(boundary).Cast<Relation>();
            }).ToArray();
            var strokes = Unique(boundaryParts.Select(part => part.Left));
            var sites = Unique(boundaryParts.SelectMany(part => new[] { ((Relation)part.Right).Left, ((Relation)part.Right).Right }));
            var inner = new List<IElement>(); var boundaryRows = new List<IElement>(); var cuts = new List<IElement>();
            foreach (var seam in seams)
            {
                var incident = N.E((Relation)seam.Right).Cast<Relation>().ToArray();
                var inside = incident.Count(row => Contains(faces, row.Left));
                if (inside == 2) inner.Add(seam);
                else if (inside == 1)
                {
                    boundaryRows.Add(seam);
                    if (incident.Any(row => Contains(allFaces, row.Left) && !Contains(faces, row.Left))) cuts.Add(seam);
                }
            }
            var footprint = new Relation(selection, new Relation(N.S(strokes), N.S(sites)));
            var complement = N.S(allFaces.Where(face => !Contains(faces, face)).ToArray());
            accepted.Add(new Relation(new Relation(Surface, definition), new Relation(footprint,
                new Relation(complement, new Relation(N.S(inner.ToArray()), new Relation(N.S(boundaryRows.ToArray()), N.S(cuts.ToArray())))))));
        }
        Groups = N.S(accepted.ToArray());
        var selected = Unique(accepted.Cast<Relation>().SelectMany(group =>
        {
            ReadParts(group, out var parts); return N.E(parts.Faces);
        }));
        SelectedFaces = N.S(selected); Complement = N.S(allFaces.Where(face => !Contains(selected, face)).ToArray());
        var adjacency = new List<IElement>(); var overlaps = new List<IElement>();
        for (var i = 0; i < accepted.Count; i++) for (var j = i + 1; j < accepted.Count; j++)
        {
            ReadParts((Relation)accepted[i], out var a); ReadParts((Relation)accepted[j], out var b);
            var sharedFaces = N.E(a.Faces).Where(face => Contains(N.E(b.Faces), face)).ToArray();
            var pair = new Relation(a.Read, b.Read);
            if (sharedFaces.Length != 0) { overlaps.Add(new Relation(pair, N.S(sharedFaces))); continue; }
            var sharedSeams = N.E(a.Boundary).Where(seam => Contains(N.E(b.Boundary), seam)).ToArray();
            if (sharedSeams.Length != 0) adjacency.Add(new Relation(pair, N.S(sharedSeams)));
        }
        Adjacency = N.S(adjacency.ToArray()); Overlaps = N.S(overlaps.ToArray());
        Blocked = N.S(blocked.ToArray()); Unresolved = N.S(pending.ToArray());
        read = new(Surface, new Relation(Groups, new Relation(SelectedFaces, new Relation(Complement,
            new Relation(Adjacency, new Relation(Overlaps, new Relation(Blocked, Unresolved)))))));
    }

    public static Relation Group(IElement identity, params Relation[] actualFaces) => new(identity, N.S(actualFaces));
    public Relation Evaluate() => read;

    public bool TryReadGroup(Relation actualRead, out OrnamentViewGroup parts)
    {
        parts = null!;
        return N.E(Groups).Any(actual => ReferenceEquals(actual, actualRead)) && ReadParts(actualRead, out parts);
    }

    public bool TryReadAdjacency(Relation actualRead, out OrnamentViewGroup left, out OrnamentViewGroup right, out Relation seams)
    {
        left = right = null!; seams = null!;
        if (!N.E(Adjacency).Any(actual => ReferenceEquals(actual, actualRead)) ||
            actualRead is not { Left: Relation { Left: Relation a, Right: Relation b }, Right: Relation rows } ||
            !TryReadGroup(a, out left) || !TryReadGroup(b, out right)) return false;
        seams = rows; return true;
    }

    internal static IElement[] Unique(IEnumerable<IElement> values) => values.Distinct(N.Participants.Instance).ToArray();
    internal static bool Contains(IEnumerable<IElement> values, IElement item) => values.Any(value => N.Same(value, item));

    private static bool ReadParts(Relation group, out OrnamentViewGroup parts)
    {
        parts = null!;
        if (group is not { Left: Relation { Right: Relation definition },
                Right: Relation { Left: Relation { Left: Relation faces, Right: Relation { Left: Relation strokes, Right: Relation sites } },
                    Right: Relation { Left: Relation complement, Right: Relation { Left: Relation inner,
                        Right: Relation { Left: Relation boundary, Right: Relation cut } } } } }) return false;
        parts = new(group, definition, definition.Left, faces, strokes, sites, inner, boundary, complement, cut); return true;
    }

    private static Relation[] SourceSeams(ScaffoldQuestionScaffold source)
    {
        var incident = N.E(source.Faces).Concat(N.E(source.Exterior)).Cast<Relation>().SelectMany(face =>
        {
            source.TryReadFace(face, out var boundary, out _);
            return N.E(boundary).Cast<Relation>().Select(part => new Relation(face, part));
        }).ToArray();
        return N.E(source.Graph.Strokes).Select(edge => new Relation(edge,
            N.S(incident.Where(row => ReferenceEquals(((Relation)row.Right).Left, edge)).Cast<IElement>().ToArray()))).ToArray();
    }
}

/// <summary>A decoded observation; all fields are read from the native group
/// witness. This object does not own group membership or adjacency. Object
/// identity avoids recursively hashing or comparing the captured source graph.</summary>
public sealed class OrnamentViewGroup(Relation read, Relation definition, IElement identity,
    Relation faces, Relation strokes, Relation sites, Relation internalSeams, Relation boundary, Relation complement, Relation cut)
{
    public Relation Read { get; } = read;
    public Relation Definition { get; } = definition;
    public IElement Identity { get; } = identity;
    public Relation Faces { get; } = faces;
    public Relation Strokes { get; } = strokes;
    public Relation Sites { get; } = sites;
    public Relation Internal { get; } = internalSeams;
    public Relation Boundary { get; } = boundary;
    public Relation Complement { get; } = complement;
    public Relation Cut { get; } = cut;
}
