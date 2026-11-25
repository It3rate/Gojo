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
/// directly, so nothing it does reaches the world until it commits. Its
/// workspaces are one supposition, so a move between two of the parent's
/// stores runs in the fork. A wiring that takes through a port cannot be
/// forked as it is, since taking would act on another structure; with ports
/// isolated, the fork takes from a snapshot of what each port showed instead,
/// and never commits. Reading through ports and sockets carries over.</para>
/// <para><b>Commit.</b> Writing back is all or nothing: refused, writing
/// nothing, if any position it would overwrite has moved in the parent since
/// the fork copied it, or if it would split changes one row made together.
/// Rewiring is not committed.</para></summary>
public sealed class Fork
{
    private readonly Dictionary<Store, Workspace> views;
    private readonly Dictionary<Surface, Workspace> ports;
    private readonly Dictionary<Wiring, Wiring> wirings;

    private Fork(Engine parent, Engine engine, Dictionary<Store, Workspace> views, Dictionary<Surface, Workspace> ports, Dictionary<Wiring, Wiring> wirings, IReadOnlyList<(Store, int)> held)
    {
        Parent = parent;
        Engine = engine;
        this.views = views;
        this.ports = ports;
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

    /// <summary>The workspace standing in for a port or socket the parent takes
    /// through, when the fork isolates ports: a snapshot of what the port
    /// showed when the fork was made.</summary>
    public Workspace Of(Surface port) => ports[port];

    /// <summary>The fork's copy of one of the parent's wirings.</summary>
    public Wiring Of(Wiring wiring) => wirings[wiring];

    public IReadOnlyCollection<Workspace> Workspaces => views.Values;

    /// <summary>Whether the fork takes from snapshots of its parent's ports
    /// instead of the ports themselves. Such a fork never commits: what it took
    /// from a snapshot was never taken from the structure behind the port.</summary>
    public bool PortsIsolated => ports.Count > 0;

    internal static Fork Of(Engine parent, bool holdMaterial, IReadOnlyDictionary<Store, Plane>? select = null, bool isolatePorts = false)
    {
        ArgumentNullException.ThrowIfNull(parent);
        var group = new object();
        var distinct = parent.Wirings.Distinct().ToArray();
        var rows = new Dictionary<Store, Store>(ReferenceEqualityComparer.Instance);
        foreach (var wiring in distinct) rows[wiring.Rows] = Copy(wiring.Rows);
        var views = new Dictionary<Store, Workspace>(ReferenceEqualityComparer.Instance);
        foreach (var store in parent.Stores)
            if (!rows.ContainsKey(store))
                views[store] = select is not null && select.TryGetValue(store, out var area) ? new Workspace(store, group).Select(area) : new Workspace(store, group).SelectAll();
        var ports = new Dictionary<Surface, Workspace>(ReferenceEqualityComparer.Instance);
        if (isolatePorts)
            foreach (var wiring in distinct)
                for (var r = 0; r < wiring.Bindings.Count; r++)
                    if (wiring.Writes(r) && wiring.Bindings[r].Surface is Port or Socket && !ports.ContainsKey(wiring.Bindings[r].Surface))
                        ports[wiring.Bindings[r].Surface] = new Workspace(Snapshot(wiring.Bindings[r].Surface), group).SelectAll();
        var place = new Place(views, ports, rows);
        var engine = Engine.ForkOf(parent);
        var copies = new Dictionary<Wiring, Wiring>(ReferenceEqualityComparer.Instance);
        foreach (var wiring in parent.Wirings)
        {
            if (!copies.TryGetValue(wiring, out var copy))
            {
                var bindings = wiring.Bindings.Select(binding => Rebind(binding, place)).ToArray();
                copy = new Wiring(wiring.Law, bindings, rows[wiring.Rows],
                    [.. Enumerable.Range(0, bindings.Length).Select(r => wiring.ReferencePlanes(r).ToArray())],
                    wiring.Recorded?.Changed, wiring.Attention);
                copies[wiring] = copy;
            }
            engine.Add(copy);
        }
        var held = holdMaterial ? Material(parent).Where(m => views.ContainsKey(m.Store)).ToArray() : [];
        foreach (var (store, plane) in held) views[store].Fix(plane, select is not null && select.TryGetValue(store, out var area) ? area : null);
        return new Fork(parent, engine, views, ports, copies, held);
    }

    /// <summary>Planes a structure's own laws move conserved units on: the
    /// planes of roles a transition writes and keeps in a preserved sum of
    /// positive amounts, where they are bound to one of its stores.</summary>
    internal static IEnumerable<(Store Store, int Plane)> Material(Engine engine) =>
        engine.Wirings.Distinct()
            .Where(w => w.Law is TransitionLaw)
            .SelectMany(w => Enumerable.Range(0, w.Bindings.Count)
                .Where(r => w.Writes(r) && ((TransitionLaw)w.Law).Accounting.Conserves(r))
                .SelectMany(r => (w.Bindings[r].Surface.Underlying is Store store ? w.Bindings[r].Planes.Select(p => (store, p)) : [])))
            .Distinct();

    /// <summary>Writes the fork's state back into the parent, all or nothing.
    /// A fork that took from snapshots of ports never commits.</summary>
    public bool Commit()
    {
        if (PortsIsolated) return false;
        if (views.Values.Any(view => view.Stale().Any)) return false;
        var results = views.Values.Select(view => view.Commit()).ToArray();
        return results.All(result => result.Committed);
    }

    /// <summary>Where the fork's copies of the parent's surfaces are: its
    /// workspaces over the parent's stores, its snapshots of ports it takes
    /// through, and its own copies of the wirings' rows.</summary>
    private sealed record Place(Dictionary<Store, Workspace> Views, Dictionary<Surface, Workspace> Ports, Dictionary<Store, Store> Rows);

    private static Binding Rebind(Binding binding, Place place)
    {
        var surface = Rebind(binding.Surface, place);
        return binding.IsOpen ? Binding.Open(surface, [.. binding.Planes]) : new Binding(surface, [.. binding.Planes]);
    }

    /// <summary>The same surface seen from the fork: a store becomes its
    /// workspace; another wiring's rows, or whether they are in use, become the
    /// fork's copy of them, so a law that recruits or retires a row does it in
    /// the fork (from tension_neon); a port taken through becomes its snapshot
    /// when ports are isolated; and a flipped view is rebuilt over what its
    /// source becomes.</summary>
    private static Surface Rebind(Surface surface, Place place) => surface switch
    {
        Store store when place.Views.TryGetValue(store, out var view) => view,
        Store store when place.Rows.TryGetValue(store, out var rows) => rows,
        Life { Of: var store } when place.Rows.TryGetValue(store, out var rows) => rows.Life,
        Flipped view => new Flipped(Rebind(view.Source, place), [.. view.Planes]),
        Port or Socket when place.Ports.TryGetValue(surface, out var snapshot) => snapshot,
        Port or Socket => surface,
        _ => throw new InvalidOperationException("A fork supposes about a structure's own stores, its rows, views of them, and what it reads through ports; whether its stores' positions are in use, workspaces and workspace gaps cannot be forked yet."),
    };

    /// <summary>A store holding what a port or socket shows now, lane by lane,
    /// read through the port and nothing behind it (from tension_neon).</summary>
    private static Store Snapshot(Surface port)
    {
        var copy = new Store(0);
        for (var p = 0; p < port.PlaneCount; p++)
        {
            if (port.IsConstant(p)) copy.AddConstantPlane(port.Count > 0 && port.Read(p, 0));
            else copy.AddPlane(port.Filled(p));
        }
        if (port.Count > 0) copy.Add(port.Count);
        for (var p = 0; p < port.PlaneCount; p++)
            if (!copy.IsConstantPlane(p))
                for (var i = 0; i < port.Count; i++)
                    if (port.IsLive(i)) copy.Write(p, i, port.Read(p, i));
        for (var i = 0; i < port.Count; i++)
            if (!port.IsLive(i)) copy.Retire(i);
        return copy;
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
