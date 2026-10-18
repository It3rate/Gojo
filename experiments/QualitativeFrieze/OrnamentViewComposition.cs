using Gojo.Core;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Observational grouping of actual groups in a preceding view. The
/// result retains both the child grouping and its source participation; union
/// does not erase repeated membership of one chamber through several children.
/// Different parents may share children and different composed views may group
/// the same children differently. This is not a physical merge or ownership tree.</summary>
public sealed class OrnamentViewComposition
{
    // definition = parent identity -> Closed(actual lower group READS)
    // Surface = (lower.Surface -> Closed(definitions)) -> context
    // expanded = parent identity -> Closed(actual source face union)
    // correspondence = (actual definition -> actual expanded definition) ->
    //   (Closed(actual child read -> actual child face scope) ->
    //    Closed(actual source face -> Closed(actual child read -> that face)))
    // shared-membership = actual definition -> Closed(source-face rows whose
    //                                             child occurrence scope has >1)
    // View's context connects this request and its native correspondence.
    // read = Surface -> (View.Evaluate() -> (correspondence ->
    //                            (shared membership -> (blocked -> unresolved))))
    public OrnamentView Lower { get; }
    public Relation Definitions { get; }
    public IElement Context { get; }
    public Relation Surface { get; }
    public OrnamentView View { get; }
    public Relation Correspondence { get; }
    public Relation SharedMembership { get; }
    public Relation Blocked { get; }
    public Relation Unresolved { get; }
    public bool IsComplete => View.IsComplete && N.E(Blocked).Length == 0 && N.E(Unresolved).Length == 0;
    private readonly Relation read;

    public OrnamentViewComposition(OrnamentView lower, Relation parentDefinitions, IElement context)
    {
        Lower = lower; Definitions = parentDefinitions; Context = context;
        Surface = new(new Relation(lower.Surface, parentDefinitions), context);
        var blocked = new List<IElement>(); var pending = new List<IElement>();
        var expanded = new List<IElement>(); var correspondence = new List<IElement>(); var shared = new List<IElement>();
        var ready = lower.IsComplete;
        if (!ready) pending.Add(lower.Evaluate());
        var closed = N.Read(parentDefinitions, out var definitions) && N.Distinct(definitions);
        if (!closed) pending.Add(parentDefinitions);
        var identities = definitions.OfType<Relation>().Select(definition => definition.Left).ToArray();
        if (!N.Distinct(identities)) { blocked.Add(parentDefinitions); closed = false; }
        foreach (var item in definitions)
        {
            if (!ready || !closed) { pending.Add(new Relation(item, lower.Surface)); continue; }
            if (item is not Relation { Right: Relation children } definition)
            { blocked.Add(new Relation(item, lower.Groups)); continue; }
            if (!N.Read(children, out var entries)) { pending.Add(new Relation(definition, children)); continue; }
            var childParts = new List<OrnamentViewGroup>();
            if (entries.Length == 0 || !N.Distinct(entries))
            { blocked.Add(new Relation(definition, lower.Groups)); continue; }
            var legal = true;
            foreach (var child in entries)
            {
                if (child is Relation childRead && lower.TryReadGroup(childRead, out var part)) childParts.Add(part);
                else { blocked.Add(new Relation(new Relation(definition, child), lower.Groups)); legal = false; }
            }
            // An exact parent is not published from only the valid prefix of
            // its child membership. Other valid parent proposals remain useful.
            if (!legal) continue;
            var faces = OrnamentView.Unique(childParts.SelectMany(child => N.E(child.Faces)));
            var expandedDefinition = new Relation(definition.Left, N.S(faces)); expanded.Add(expandedDefinition);
            var childMaps = N.S(childParts.Select(child => (IElement)new Relation(child.Read, child.Faces)).ToArray());
            var faceRows = faces.Select(face => new Relation(face, N.S(childParts.Where(child =>
                OrnamentView.Contains(N.E(child.Faces), face)).Select(child => (IElement)new Relation(child.Read, face)).ToArray()))).ToArray();
            var sharedFaces = faceRows.Where(row => N.E((Relation)row.Right).Length > 1).Cast<IElement>().ToArray();
            correspondence.Add(new Relation(new Relation(definition, expandedDefinition), new Relation(childMaps, N.S(faceRows))));
            if (sharedFaces.Length != 0) shared.Add(new Relation(definition, N.S(sharedFaces)));
        }
        Correspondence = N.S(correspondence.ToArray()); SharedMembership = N.S(shared.ToArray());
        Blocked = N.S(blocked.ToArray()); Unresolved = N.S(pending.ToArray());
        View = new(lower.Source, N.S(expanded.ToArray()), new Relation(Surface, Correspondence));
        read = new(Surface, new Relation(View.Evaluate(), new Relation(Correspondence,
            new Relation(SharedMembership, new Relation(Blocked, Unresolved)))));
    }

    public Relation Evaluate() => read;
}
