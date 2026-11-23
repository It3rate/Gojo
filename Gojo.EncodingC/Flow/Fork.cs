namespace Gojo.EncodingC;

/// <summary>A fork of a local structure: a place to try an arrangement
/// without changing the structure or the world it is in.
/// <para><b>State by reference.</b> Every store of the parent that holds state
/// is read through a workspace over it, which reads the parent live and copies
/// a position only when the fork writes it. Where the fork has not written, it
/// sees the parent as it is now.</para>
/// <para><b>Material held fixed.</b> A plane is material when a law moves
/// units on it and its accounting conserves them: a role it writes that is in
/// a preserved sum of positive amounts. By default a fork holds every material
/// plane as it was when the fork was made, so the parent's units moving on do
/// not show through beside the fork's own; information, everything else, is
/// still read live. The structure says which planes are material; nothing is
/// declared.</para>
/// <para><b>Selection.</b> A fork can attend to areas of its parent's stores.
/// Its wirings then run only where their positions are selected, it holds
/// material only there, and running brings in by use what it touches, holding
/// material at each position as it comes in. Its cost follows what it attends
/// to, not the size of the parent.</para>
/// <para><b>Topology copied, for now.</b> Each wiring is rebuilt over the
/// workspaces with the same law and a copy of its rows, so the fork can be
/// rewired, rows recruited or retired, without touching the parent's wiring.
/// Reading rows by reference too is a later step.</para>
/// <para><b>Belonging.</b> The fork is the parent's own supposition: it may
/// read the parent's stores through its workspaces and never binds them
/// directly, so nothing it does reaches the world until it commits. A wiring
/// that takes through a port cannot be forked, since taking would act on
/// another structure; reading through ports and sockets carries over.</para>
/// <para><b>Commit.</b> Writing back is all or nothing: refused, writing
/// nothing, if any position it would overwrite has moved in the parent since
/// the fork copied it, or if it would split changes one row made together.
/// Rewiring is not committed.</para></summary>
public sealed class Fork
{
    private readonly Dictionary<Store, Workspace> views;
    private readonly Dictionary<Wiring, Wiring> wirings;

    private Fork(Engine parent, Engine engine, Dictionary<Store, Workspace> views, Dictionary<Wiring, Wiring> wirings, IReadOnlyList<(Store, int)> held)
    {
        Parent = parent;
        Engine = engine;
        this.views = views;
        this.wirings = wirings;
        Held = held;
    }

    /// <summary>The parent's planes this fork holds fixed, by store and plane.</summary>
    public IReadOnlyList<(Store Store, int Plane)> Held { get; }

    public Engine Parent { get; }

    /// <summary>The fork's own structure, stepped on its own.</summary>
    public Engine Engine { get; }

    /// <summary>The workspace through which the fork reads a parent store.</summary>
    public Workspace Of(Store store) => views[store];

    /// <summary>The fork's copy of one of the parent's wirings.</summary>
    public Wiring Of(Wiring wiring) => wirings[wiring];

    public IReadOnlyCollection<Workspace> Workspaces => views.Values;

    internal static Fork Of(Engine parent, bool holdMaterial, IReadOnlyDictionary<Store, Plane>? select = null)
    {
        ArgumentNullException.ThrowIfNull(parent);
        var rows = parent.Wirings.Select(w => w.Rows).ToHashSet(ReferenceEqualityComparer.Instance);
        var views = new Dictionary<Store, Workspace>(ReferenceEqualityComparer.Instance);
        foreach (var store in parent.Stores)
            if (!rows.Contains(store))
                views[store] = select is not null && select.TryGetValue(store, out var area) ? new Workspace(store).Select(area) : new Workspace(store).SelectAll();
        var engine = Engine.ForkOf(parent);
        var copies = new Dictionary<Wiring, Wiring>(ReferenceEqualityComparer.Instance);
        foreach (var wiring in parent.Wirings)
        {
            if (!copies.TryGetValue(wiring, out var copy))
            {
                var bindings = wiring.Bindings.Select(binding => Rebind(binding, views)).ToArray();
                copy = new Wiring(wiring.Law, bindings, Copy(wiring.Rows),
                    [.. Enumerable.Range(0, bindings.Length).Select(r => wiring.ReferencePlanes(r).ToArray())],
                    wiring.Recorded?.Changed, wiring.Attention);
                copies[wiring] = copy;
            }
            engine.Add(copy);
        }
        var held = holdMaterial ? Material(parent).Where(m => views.ContainsKey(m.Store)).ToArray() : [];
        foreach (var (store, plane) in held) views[store].Fix(plane, select is not null && select.TryGetValue(store, out var area) ? area : null);
        return new Fork(parent, engine, views, copies, held);
    }

    /// <summary>Planes a structure's own laws move conserved units on: the
    /// planes of roles a transition writes and keeps in a preserved sum of
    /// positive amounts, where they are bound to one of its stores.</summary>
    internal static IEnumerable<(Store Store, int Plane)> Material(Engine engine) =>
        engine.Wirings.Distinct()
            .Where(w => w.Law is TransitionLaw)
            .SelectMany(w => Enumerable.Range(0, w.Bindings.Count)
                .Where(r => w.Writes(r) && ((TransitionLaw)w.Law).Accounting.Conserves(r))
                .SelectMany(r => (w.Bindings[r].Surface is Store store ? w.Bindings[r].Planes.Select(p => (store, p)) : [])))
            .Distinct();

    /// <summary>Writes the fork's state back into the parent, all or nothing.</summary>
    public bool Commit()
    {
        if (views.Values.Any(view => view.Stale().Any)) return false;
        var results = views.Values.Select(view => view.Commit()).ToArray();
        return results.All(result => result.Committed);
    }

    private static Binding Rebind(Binding binding, Dictionary<Store, Workspace> views)
    {
        Surface surface = binding.Surface switch
        {
            Store store => views[store],
            Flipped { Source: Store store } view => new Flipped(views[store], [.. view.Planes]),
            Port or Socket => binding.Surface,
            _ => throw new InvalidOperationException("A fork supposes about a structure's own stores, views of them, and what it reads through ports; whether positions are in use, workspaces and gaps cannot be forked yet."),
        };
        return binding.IsOpen ? Binding.Open(surface, [.. binding.Planes]) : new Binding(surface, [.. binding.Planes]);
    }

    /// <summary>A copy of a wiring's rows: the same planes, positions, liveness
    /// and bits, owned by the fork.</summary>
    private static Store Copy(Store rows)
    {
        var copy = new Store(0);
        for (var p = 0; p < rows.PlaneCount; p++) copy.AddPlane(rows.Fills(p));
        for (var i = 0; i < rows.Count; i++)
        {
            if (rows.IsSpare(i)) copy.AddSpare();
            else copy.Add();
        }
        for (var p = 0; p < rows.PlaneCount; p++)
            for (var i = 0; i < rows.Count; i++)
                if (!rows.IsRetired(i)) copy.Write(p, i, rows.Read(p, i));
        for (var i = 0; i < rows.Count; i++)
            if (rows.IsRetired(i)) copy.Retire(i);
        return copy;
    }
}
