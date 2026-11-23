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
/// (thin boundary, C7). Nothing is kept that no selected row uses: positions
/// only come in through rows, and <see cref="Planes"/> holds only the planes
/// selected rows read (coherence, C5). Seeds stay, as referents, even when
/// nothing binds them (C3).</para>
/// <para><b>Use.</b> <see cref="Stores"/> is a fork's selection;
/// <see cref="Attend"/> writes the selected rows into each wiring's attention
/// plane, so the structure runs only there.</para></summary>
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
    /// grown <paramref name="reach"/> rows beyond the units the seeds belong to.</summary>
    public static Closure Of(Engine engine, IReadOnlyDictionary<Surface, Plane> seeds, int reach = 0)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(seeds);
        ArgumentOutOfRangeException.ThrowIfNegative(reach);
        var areas = new Dictionary<Surface, Plane>(ReferenceEqualityComparer.Instance);
        var planes = new Dictionary<Surface, Plane>(ReferenceEqualityComparer.Instance);
        Plane Area(Dictionary<Surface, Plane> map, Surface surface, int length) =>
            map.TryGetValue(surface, out var area) ? area : map[surface] = new Plane(length);
        bool Selected(Surface surface, int index) => areas.TryGetValue(surface, out var area) && index < area.Length && area[index];

        foreach (var (surface, seed) in seeds) areas[Key(surface)] = Area(areas, Key(surface), surface.Count).Or(seed);
        var wirings = engine.Wirings.Distinct().ToArray();
        for (var round = 0; round <= reach; round++)
        {
            var picked = wirings.Select(w => w.Live.Indexes().Where(row =>
                w.Row(row).Select((index, role) => (index, surface: w.Bindings[role].Surface))
                    .Any(x => !x.surface.ThroughPort && Selected(Key(x.surface), x.index))).ToArray()).ToArray();
            for (var i = 0; i < wirings.Length; i++)
            {
                var rows = Area(areas, wirings[i].Rows, wirings[i].Count);
                foreach (var row in picked[i]) rows[row] = true;
            }
            foreach (var w in wirings)
                foreach (var row in Area(areas, w.Rows, w.Count).Indexes())
                    for (var role = 0; role < w.Bindings.Count; role++)
                    {
                        var surface = Key(w.Bindings[role].Surface);
                        Area(areas, surface, surface.Count)[w.Row(row)[role]] = true;
                        var read = Area(planes, surface, surface.PlaneCount);
                        foreach (var plane in w.Bindings[role].Planes) read[plane] = true;
                        var refs = Area(planes, w.Rows, w.Rows.PlaneCount);
                        foreach (var plane in w.ReferencePlanes(role)) refs[plane] = true;
                    }
        }
        return new Closure(engine, areas, planes);
    }

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
