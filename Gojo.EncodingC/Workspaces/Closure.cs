namespace Gojo.EncodingC;

/// <summary>A selection built from a seed rather than listed: the smallest
/// set of whole units around it (docs/GojoStructureClosure-2026-11.md,
/// section 4).
/// <para><b>One kind of answer.</b> A selection is a plane of positions per
/// surface. A wiring's rows are positions of its own <see cref="Wiring.Rows"/>
/// store, so selected rows are a plane there too, and a port's lanes are
/// positions of the port. A seed is the same thing: positions of a store are
/// referents, positions of a rows store are whole rows.</para>
/// <para><b>Steps.</b> A position snaps up to the rows that bind it, since
/// nothing runs on part of a row (saturation, C1 and C2). Every selected row
/// brings in every position it binds (completeness, C4). Each unit of
/// <c>reach</c> repeats that once more from what is now selected (growth).
/// Growth stops at a port or socket: the lane a selected row binds is kept as
/// the head of the structure behind it, and nothing is followed through it
/// (thin boundary, C7). A position whose open value is settled is a finished
/// unit: rows reach it, and it reaches no further rows (phase, C8).
/// <para><b>Resolution.</b> Both are sealed boundaries, and <c>open</c> says
/// how many of them growth may cross on the way to any position. Crossing a
/// port selects the owner's position and runs on in the owner's structure;
/// crossing a settled position rereads what settled it.</para> Nothing is kept that no selected row uses: positions
/// only come in through rows, and <see cref="Planes"/> holds only the planes
/// selected rows read (coherence, C5). Seeds stay, as referents, even when
/// nothing binds them (C3).</para>
/// <para><b>Use.</b> <see cref="Stores"/> is a fork's selection;
/// <see cref="Attend"/> writes the selected rows into each wiring's attention
/// plane of this structure only, so it runs only there; another structure's
/// attention is its own.</para></summary>
public sealed class Closure
{
    private readonly Dictionary<Surface, Plane> areas;
    private readonly Dictionary<Surface, Plane> planes;

    private Closure(Engine engine, Dictionary<Surface, Plane> areas, Dictionary<Surface, Plane> planes)
    {
        Engine = engine;
        this.areas = areas;
        this.planes = planes;
    }

    public Engine Engine { get; }

    /// <summary>Selected positions by surface: stores, rows stores and the
    /// lanes of ports and sockets, which are heads.</summary>
    public IReadOnlyDictionary<Surface, Plane> Areas => areas;

    /// <summary>The selected positions of the engine's stores, as a fork
    /// selects them.</summary>
    public IReadOnlyDictionary<Store, Plane> Stores =>
        areas.Where(a => a.Key is Store).ToDictionary<KeyValuePair<Surface, Plane>, Store, Plane>(a => (Store)a.Key, a => a.Value, ReferenceEqualityComparer.Instance);

    /// <summary>Selected positions of a surface; none if it was not reached.</summary>
    public Plane Of(Surface surface) => areas.TryGetValue(Key(surface), out var area) ? area.Clone() : new Plane(surface.Count);

    /// <summary>Selected rows of a wiring.</summary>
    public Plane Rows(Wiring wiring) => Of(wiring.Rows);

    /// <summary>The planes of a surface that selected rows read; the rest are
    /// not needed by the selection.</summary>
    public Plane PlanesOf(Surface surface) => planes.TryGetValue(Key(surface), out var read) ? read.Clone() : new Plane(surface.PlaneCount);

    /// <summary>The closure of <paramref name="seeds"/> in <paramref name="engine"/>,
    /// grown <paramref name="reach"/> rows beyond the units the seeds belong to,
    /// opening at most <paramref name="open"/> sealed boundaries on the way to
    /// any position.</summary>
    public static Closure Of(Engine engine, IReadOnlyDictionary<Surface, Plane> seeds, int reach = 0, int open = 0)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(seeds);
        ArgumentOutOfRangeException.ThrowIfNegative(reach);
        ArgumentOutOfRangeException.ThrowIfNegative(open);
        var areas = new Dictionary<Surface, Plane>(ReferenceEqualityComparer.Instance);
        var planes = new Dictionary<Surface, Plane>(ReferenceEqualityComparer.Instance);
        var crossed = new Dictionary<(Surface, int), int>();
        var engines = new List<Engine> { engine };
        Plane Area(Dictionary<Surface, Plane> map, Surface surface, int length) =>
            map.TryGetValue(surface, out var area) ? area : map[surface] = new Plane(length);
        void Mark(Surface surface, int index, int boundaries)
        {
            Area(areas, surface, surface.Count)[index] = true;
            if (!crossed.TryGetValue((surface, index), out var before) || boundaries < before) crossed[(surface, index)] = boundaries;
        }
        void Read(Surface surface, IEnumerable<int> read)
        {
            var area = Area(planes, surface, surface.PlaneCount);
            foreach (var plane in read) area[plane] = true;
        }

        foreach (var (surface, seed) in seeds)
            foreach (var index in seed.Indexes()) Mark(Key(surface), index, 0);
        for (var round = 0; round <= reach; round++)
        {
            var wirings = engines.SelectMany(e => e.Wirings).Distinct().ToArray();
            var picked = wirings.SelectMany(w => w.Live.Indexes()
                .Where(row => !crossed.ContainsKey((w.Rows, row)))
                .Select(row => (w, row, boundaries: w.Row(row).Select((index, role) =>
                        w.Bindings[role].Surface.ThroughPort || !crossed.TryGetValue((Key(w.Bindings[role].Surface), index), out var c)
                            ? int.MaxValue
                            : c + (Sealed(w.Bindings[role], index) ? 1 : 0)).Min()))
                .Where(x => x.boundaries <= open)).ToArray();
            foreach (var (w, row, boundaries) in picked) Mark(w.Rows, row, boundaries);
            foreach (var w in wirings)
                foreach (var row in Area(areas, w.Rows, w.Count).Indexes().ToArray())
                {
                    var boundaries = crossed[(w.Rows, row)];
                    var indexes = w.Row(row);
                    for (var role = 0; role < indexes.Length; role++)
                    {
                        var binding = w.Bindings[role];
                        var surface = Key(binding.Surface);
                        Mark(surface, indexes[role], boundaries);
                        Read(surface, binding.Planes);
                        Read(w.Rows, w.ReferencePlanes(role));
                        if (boundaries >= open || Behind(surface) is not { } port || !port.IsLive(indexes[role])) continue;
                        Mark(port.Owner, port.Lane(indexes[role]), boundaries + 1);
                        Read(port.Owner, binding.Planes.Select(p => port.OwnerPlanes[p]));
                        if (port.Owner.Local is { } owner && !engines.Contains(owner)) engines.Add(owner);
                    }
                }
        }
        return new Closure(engine, areas, planes);
    }

    /// <summary>The closure that follows a referent across structures, out and
    /// back (formal concept analysis's closure): from the
    /// <paramref name="seeds"/> to the structures in <paramref name="view"/>
    /// that reference all of them, and back to the positions those structures
    /// share. Positions are taken by identity, so a lane read through a port
    /// or a socket is its owner's position, and positions private to one
    /// structure drop out unless enough others reach them too.
    /// <para><paramref name="missing"/> is how many of the using structures may
    /// lack a position for it to stay. At 0 the result is the strict common
    /// core, and computing it again from that core gives it back. At one less
    /// than the number of users it is everything any of them references.
    /// In between are the selections that are partly shared.</para>
    /// <para>The rows selected are those of the using structures that bind a
    /// shared position, so a fork or attention can run them. The first
    /// structure in view is the one that asks.</para></summary>
    public static Closure Common(IReadOnlyList<Engine> view, IReadOnlyDictionary<Surface, Plane> seeds, int missing = 0)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(seeds);
        ArgumentOutOfRangeException.ThrowIfNegative(missing);
        if (view.Count == 0) throw new ArgumentException("Nothing is in view.", nameof(view));
        var seed = seeds.SelectMany(s => s.Value.Indexes().Select(i => Identity(s.Key, i))).ToHashSet();
        IEnumerable<(Wiring W, int Row, (Surface, int)[] Binds)> Rows(Engine e) =>
            e.Wirings.Distinct().SelectMany(w => w.Live.Indexes().Select(row => (w, row, w.Row(row).Select((i, r) => Identity(w.Bindings[r].Surface, i)).ToArray())));
        var users = view.Where(e => seed.IsSubsetOf(Rows(e).SelectMany(x => x.Binds))).ToArray();
        var references = users.Select(e => Rows(e).SelectMany(x => x.Binds).ToHashSet()).ToArray();
        var shared = references.SelectMany(r => r).Distinct()
            .Where(p => references.Count(r => r.Contains(p)) >= references.Length - missing)
            .Union(seed).ToHashSet();

        var areas = new Dictionary<Surface, Plane>(ReferenceEqualityComparer.Instance);
        var planes = new Dictionary<Surface, Plane>(ReferenceEqualityComparer.Instance);
        Plane Area(Dictionary<Surface, Plane> map, Surface surface, int length) =>
            map.TryGetValue(surface, out var area) ? area : map[surface] = new Plane(length);
        foreach (var (surface, index) in shared) Area(areas, surface, surface.Count)[index] = true;
        foreach (var (w, row, binds) in users.SelectMany(Rows).Where(x => x.Binds.Any(shared.Contains)))
        {
            Area(areas, w.Rows, w.Count)[row] = true;
            for (var role = 0; role < binds.Length; role++)
            {
                if (!shared.Contains(binds[role])) continue;
                var read = Area(planes, binds[role].Item1, binds[role].Item1.PlaneCount);
                foreach (var plane in w.Bindings[role].Planes) read[Behind(w.Bindings[role].Surface) is { } port ? port.OwnerPlanes[plane] : plane] = true;
            }
        }
        return new Closure(view[0], areas, planes);
    }

    /// <summary>Where a position is: through a port or a plugged socket, the
    /// owner's position; through a view, its source's.</summary>
    private static (Surface, int) Identity(Surface surface, int index) =>
        Behind(Key(surface)) is { } port && port.IsLive(index) ? (port.Owner, port.Lane(index)) : (Key(surface), index);

    /// <summary>A role bound open whose value is settled at this position: a
    /// finished unit, read through its edge and not grown through (phase, C8).</summary>
    private static bool Sealed(Binding binding, int index) =>
        binding.IsOpen && binding.Planes.Count(plane => binding.Surface.Read(plane, index)) == 1;

    /// <summary>The port a surface reaches another structure through, if any.</summary>
    private static Port? Behind(Surface surface) => surface switch
    {
        Port port => port,
        Socket socket => socket.Plugged,
        _ => null,
    };

    /// <summary>Runs the structure only on the selected rows from now on:
    /// writes them into each wiring's attention plane, and clears the rest.</summary>
    public Closure Attend()
    {
        foreach (var w in Engine.Wirings.Distinct())
        {
            var plane = w.Attend();
            var rows = Rows(w);
            foreach (var row in w.Live.Indexes()) w.Rows.Write(plane, row, row < rows.Length && rows[row]);
        }
        return this;
    }

    /// <summary>A view of a surface is the surface: a flipped view shows the
    /// same positions.</summary>
    private static Surface Key(Surface surface) => surface is Flipped flipped ? flipped.Source : surface;
}
