using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>
/// Occupancy and ordinal clearance on a declared finite topology. Cells and
/// footprint witnesses have identity; there are no coordinates, distances,
/// implicit outside cells or claims of independent observations.
/// </summary>
public sealed class QualitativeCoverage
{
    // Surface = (Closed(cells) -> Closed(cell -> neighbor)) ->
    //           Closed(cell -> Closed(actual footprint witnesses))
    // read = Surface -> ((empty -> (single -> multiple)) ->
    //                    (layers -> (centers -> unresolved)))
    // Occupancy scopes retain the actual coverage rows, not fresh samples.
    // layer = ((Surface -> layer-identity) -> predecessor) -> Closed(cells)
    // The first predecessor is Closed(); later predecessors are actual layers.
    // center = (Surface -> layer) -> Closed(connected plateau cells)
    // Outgoing adjacency propagates shells from occupied cells. A plateau uses
    // weak incidence connectivity within one shell; its outgoing neighbors may
    // not belong to a higher shell. Neither read adds reverse adjacency edges.
    public Relation Cells { get; }
    public Relation Adjacency { get; }
    public Relation Coverage { get; }
    public Relation Surface { get; }

    public QualitativeCoverage(Relation cells, Relation adjacency, Relation coverage)
    {
        Cells = cells; Adjacency = adjacency; Coverage = coverage;
        Surface = new Relation(new Relation(cells, adjacency), coverage);
    }

    public Relation Evaluate()
    {
        var empty = new List<IElement>(); var single = new List<IElement>(); var multiple = new List<IElement>();
        var layers = new List<IElement>(); var centers = new List<IElement>(); var pending = new List<IElement>();
        if (!Domain(Cells, out var cells))
        { pending.Add(new Relation(Cells, Surface)); return Result(); }
        var cellIndex = cells.Select((cell, index) => (cell, index)).ToDictionary(pair => pair.cell, pair => pair.index, Participants.Instance);
        var successors = Enumerable.Range(0, cells.Length).Select(_ => new List<int>()).ToArray();
        var incidence = Enumerable.Range(0, cells.Length).Select(_ => new List<int>()).ToArray();
        var topologyClosed = Closed(Adjacency, out var edgeEntries);
        if (!topologyClosed) pending.Add(new Relation(Adjacency, Surface));
        foreach (var entry in edgeEntries)
        {
            if (entry is not Relation edge)
            { topologyClosed = false; pending.Add(new Relation(entry, Cells)); continue; }
            if (!cellIndex.TryGetValue(edge.Left, out var from) || !cellIndex.TryGetValue(edge.Right, out var to))
            { topologyClosed = false; pending.Add(new Relation(entry, Cells)); continue; }
            successors[from].Add(to); incidence[from].Add(to); incidence[to].Add(from);
        }

        var coverageClosed = Closed(Coverage, out var rowEntries);
        if (!coverageClosed) pending.Add(new Relation(Coverage, Surface));
        var rowsByCell = Enumerable.Range(0, cells.Length).Select(_ => new List<Relation>()).ToArray();
        foreach (var row in rowEntries)
        {
            if (row is not Relation offered || !cellIndex.TryGetValue(offered.Left, out var at))
            { pending.Add(new Relation(row, Cells)); continue; }
            rowsByCell[at].Add(offered);
        }
        var occupied = new List<int>(); var unknown = new List<int>(); var knownEmpty = new HashSet<int>();
        for (var i = 0; i < cells.Length; i++)
        {
            var rows = rowsByCell[i].ToArray();
            if (!coverageClosed || rows.Length != 1 || rows[0].Right is not Relation witnesses ||
                !Closed(witnesses, out var hits))
            {
                unknown.Add(i);
                pending.Add(new Relation(cells[i], new Relation(Coverage, S(rows))));
                continue;
            }
            // A repeated route to the same witness is not fresh evidence.
            var distinct = Distinct(hits);
            if (distinct.Length == 0) { empty.Add(rows[0]); knownEmpty.Add(i); }
            else if (distinct.Length == 1) { single.Add(rows[0]); occupied.Add(i); }
            else { multiple.Add(rows[0]); occupied.Add(i); }
        }

        var knownDistance = Shells(cells.Length, occupied, successors);
        var unknownDistance = Shells(cells.Length, unknown, successors);
        var occupiedSet = occupied.ToHashSet();
        var exact = new bool[cells.Length];
        for (var i = 0; i < cells.Length; i++)
        {
            if (occupiedSet.Contains(i)) exact[i] = true;
            else if (knownEmpty.Contains(i) && topologyClosed && knownDistance[i] >= 0 &&
                (unknownDistance[i] < 0 || unknownDistance[i] >= knownDistance[i])) exact[i] = true;
            else if (knownEmpty.Contains(i))
                pending.Add(new Relation(cells[i], new Relation(Adjacency, Coverage)));
        }
        // Host indexes traverse the graph. Only predecessor structure and cell
        // membership are published as the ordinal layer; no number is stored.
        var memberLayer = new Relation?[cells.Length];
        Relation predecessor = S();
        for (var shell = 0; shell < cells.Length; shell++)
        {
            var members = Enumerable.Range(0, cells.Length).Where(i => exact[i] && knownDistance[i] == shell).ToArray();
            if (members.Length == 0) break;
            var layer = new Relation(new Relation(new Relation(Surface, new Placeholder()), predecessor),
                S(members.Select(i => cells[i]).ToArray()));
            layers.Add(layer); predecessor = layer;
            foreach (var i in members) memberLayer[i] = layer;
        }

        var visited = new HashSet<int>();
        foreach (var seed in Enumerable.Range(0, cells.Length).Where(i => knownEmpty.Contains(i) && memberLayer[i] is not null))
        {
            if (!visited.Add(seed)) continue;
            var component = new List<int>(); var queue = new Queue<int>(); queue.Enqueue(seed);
            while (queue.TryDequeue(out var cell))
            {
                component.Add(cell);
                foreach (var next in incidence[cell])
                {
                    if (!knownEmpty.Contains(next) ||
                        !ReferenceEquals(memberLayer[next], memberLayer[seed]) || !visited.Add(next)) continue;
                    queue.Enqueue(next);
                }
            }
            var outgoing = component.SelectMany(i => successors[i]).Distinct().ToArray();
            if (outgoing.Any(i => memberLayer[i] is not null && knownDistance[i] > knownDistance[seed])) continue;
            var plateau = S(component.Select(i => cells[i]).ToArray());
            if (!topologyClosed || outgoing.Any(i => memberLayer[i] is null))
            { pending.Add(new Relation(plateau, Adjacency)); continue; }
            centers.Add(new Relation(new Relation(Surface, memberLayer[seed]!), plateau));
        }
        return Result();

        Relation Result() => new(Surface,
            new Relation(new Relation(S(empty.ToArray()), new Relation(S(single.ToArray()), S(multiple.ToArray()))),
                new Relation(S(layers.ToArray()), new Relation(S(centers.ToArray()), S(pending.ToArray())))));
    }

    public bool TryReadResult(Relation read, out Relation empty, out Relation single, out Relation multiple,
        out Relation layers, out Relation centers, out Relation unresolved)
    {
        empty = single = multiple = layers = centers = unresolved = null!;
        if (!ReferenceEquals(read.Left, Surface) || read.Right is not Relation { Left: Relation occupancy, Right: Relation geometry } ||
            occupancy.Left is not Relation none || occupancy.Right is not Relation { Left: Relation one, Right: Relation many } ||
            geometry.Left is not Relation shellScope || geometry.Right is not Relation { Left: Relation centerScope, Right: Relation pending } ||
            !Closed(none, out _) || !Closed(one, out _) || !Closed(many, out _) || !Closed(shellScope, out var shellEntries) ||
            !Closed(centerScope, out var centerEntries) || !Closed(pending, out _) ||
            shellEntries.Any(entry => entry is not Relation layer || !TryReadLayer(layer, out _, out _)) ||
            centerEntries.Any(entry => entry is not Relation center || !TryReadCenter(center, out var layer, out _) ||
                !shellEntries.Any(candidate => ReferenceEquals(candidate, layer)))) return false;
        var classified = new HashSet<IElement>(Participants.Instance);
        var sourceClosed = Closed(Coverage, out var sourceRows);
        var sourceIdentities = sourceRows.ToHashSet(ReferenceEqualityComparer.Instance);
        var rowCounts = sourceRows.OfType<Relation>().GroupBy(row => row.Left, Participants.Instance)
            .ToDictionary(group => group.Key, group => group.Count(), Participants.Instance);
        var validDomain = Domain(Cells, out var sourceCells);
        var cellDomain = sourceCells.ToHashSet(Participants.Instance);
        if (!Rows(none, count => count == 0) || !Rows(one, count => count == 1) || !Rows(many, count => count > 1)) return false;
        empty = none; single = one; multiple = many; layers = shellScope; centers = centerScope; unresolved = pending;
        return true;

        bool Rows(Relation scope, Func<int, bool> accepts)
        {
            foreach (var entry in E(scope))
            {
                if (entry is not Relation row || !sourceClosed || !sourceIdentities.Contains(row) ||
                    !rowCounts.TryGetValue(row.Left, out var count) || count != 1 ||
                    !classified.Add(row.Left) || !validDomain || !cellDomain.Contains(row.Left) ||
                    row.Right is not Relation witnesses || !Closed(witnesses, out var hits) || !accepts(Distinct(hits).Length)) return false;
            }
            return true;
        }
    }

    public bool TryReadLayer(Relation layer, out IElement predecessor, out Relation cells)
    {
        predecessor = null!; cells = null!;
        if (layer.Left is not Relation { Left: Relation context, Right: Relation prior } ||
            !ReferenceEquals(context.Left, Surface) || layer.Right is not Relation members ||
            !Domain(members, out var entries) || !Domain(Cells, out var domain)) return false;
        var admitted = domain.ToHashSet(Participants.Instance);
        if (entries.Any(entry => !admitted.Contains(entry))) return false;
        if (!(Closed(prior, out var root) && root.Length == 0) &&
            !(prior.Left is Relation { Left: Relation previous } && ReferenceEquals(previous.Left, Surface) && prior.Right is Relation)) return false;
        predecessor = prior; cells = members;
        return true;
    }

    public bool TryReadCenter(Relation center, out Relation layer, out Relation cells)
    {
        layer = cells = null!;
        if (center.Left is not Relation { Right: Relation selected } context || !ReferenceEquals(context.Left, Surface) ||
            !TryReadLayer(selected, out _, out var layerMembers) || center.Right is not Relation members ||
            !Domain(members, out var entries) || entries.Length == 0 ||
            entries.Any(entry => Index(E(layerMembers), entry) < 0)) return false;
        layer = selected; cells = members;
        return true;
    }

    private static int[] Shells(int count, List<int> sources, List<int>[] successors)
    {
        var distance = Enumerable.Repeat(-1, count).ToArray(); var queue = new Queue<int>();
        foreach (var source in sources) { distance[source] = 0; queue.Enqueue(source); }
        while (queue.TryDequeue(out var cell))
            foreach (var next in successors[cell])
                if (distance[next] < 0)
                { distance[next] = checked(distance[cell] + 1); queue.Enqueue(next); }
        return distance;
    }

    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) =>
        StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    private static bool Closed(Relation scope, out IElement[] entries) =>
        StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail) && seam.Equals(tail);
    private static bool Domain(Relation scope, out IElement[] entries) =>
        Closed(scope, out entries) && Distinct(entries).Length == entries.Length;
    private static IElement[] Distinct(IElement[] entries)
    {
        return entries.Distinct(Participants.Instance).ToArray();
    }
    private static int Index(IElement[] entries, IElement sought) => Array.FindIndex(entries, entry => Same(entry, sought));
    private static bool Same(IElement left, IElement right) => ReferenceEquals(left, right) ||
        left is Placeholder a && right is Placeholder b && a.Equals(b) ||
        left is CarrierLocation x && right is CarrierLocation y && x.Carrier.SameIdentityAs(y.Carrier) && x.SameTickRefAs(y);

    private sealed class Participants : IEqualityComparer<IElement>
    {
        public static readonly Participants Instance = new();
        public bool Equals(IElement? a, IElement? b) => a is not null && b is not null && Same(a, b);
        public int GetHashCode(IElement element) => element switch
        {
            Placeholder slot => slot.GetHashCode(),
            CarrierLocation site => HashCode.Combine(site.Carrier.Id, System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(site.Tick)),
            _ => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(element)
        };
    }
}
