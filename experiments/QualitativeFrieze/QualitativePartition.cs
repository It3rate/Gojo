using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>
/// Two equally occupied roles on explicit finite topology. Neither role is a
/// default background. Region and exchange reads use cell identity and ordered
/// adjacency; no coordinate, area, color code or geometric transform is stored.
/// </summary>
public sealed class QualitativePartition
{
    // Surface = (cells -> adjacency) -> (labels -> ordered-role-pair)
    // label = actual-cell -> actual-role
    // read = Surface -> (((first-role -> first-labels) -> (second-role -> second-labels)) ->
    //                    (regions -> (interfaces -> unresolved)))
    // region = (Surface -> role) -> (member-cells -> boundary-interfaces)
    // interface = actual-directed-edge -> (actual-from-label -> actual-to-label)
    // role-exchange = Closed(first-role -> second-role, second-role -> first-role)
    // exchange-read = ((Surface -> cell-map) -> role-exchange) ->
    //                 (matched -> (unmatched -> unresolved))
    // color witness = actual-map-row -> (actual-from-label -> actual-to-label)
    // adjacency witness = actual-edge -> (mapped-edge -> Closed(actual matching edges))
    // Connected regions use weak incidence of the actual directed edges. This
    // does not add reverse edges: exchange must preserve the ordered edges.
    public Relation Cells { get; }
    public Relation Adjacency { get; }
    public Relation RolePair { get; }
    public Relation Labels { get; }
    public Relation RoleExchange { get; }
    public Relation Surface { get; }

    public QualitativePartition(Relation cells, Relation adjacency, Relation rolePair, Relation labels)
    {
        Cells = cells; Adjacency = adjacency; RolePair = rolePair; Labels = labels;
        Surface = new Relation(new Relation(cells, adjacency), new Relation(labels, rolePair));
        RoleExchange = S(new Relation(rolePair.Left, rolePair.Right), new Relation(rolePair.Right, rolePair.Left));
    }

    public Relation Evaluate()
    {
        var pending = new List<IElement>(); var basis = ReadBasis(pending);
        var first = new List<IElement>(); var second = new List<IElement>();
        var regions = new List<IElement>(); var interfaces = new List<IElement>();
        foreach (var label in basis.Labels.Where(label => label is not null).Cast<Relation>())
            (Same(label.Right, RolePair.Left) ? first : second).Add(label);
        foreach (var (edge, from, to) in basis.Edges)
            if (basis.Labels[from] is Relation a && basis.Labels[to] is Relation b && !Same(a.Right, b.Right))
                interfaces.Add(new Relation(edge, new Relation(a, b)));
        var visited = new HashSet<int>();
        for (var seed = 0; seed < basis.Cells.Length; seed++)
        {
            if (basis.Labels[seed] is not Relation label || !visited.Add(seed)) continue;
            var component = new List<int>(); var queue = new Queue<int>(); queue.Enqueue(seed);
            var complete = basis.TopologyClosed && basis.CellsClosed;
            while (queue.TryDequeue(out var cell))
            {
                component.Add(cell);
                foreach (var next in basis.Incidence[cell])
                {
                    if (basis.Labels[next] is not Relation neighbor) { complete = false; continue; }
                    if (Same(label.Right, neighbor.Right) && visited.Add(next)) queue.Enqueue(next);
                }
            }
            var memberIds = component.Select(i => basis.Cells[i]).ToArray();
            var memberSet = memberIds.ToHashSet(Participants.Instance);
            var boundary = interfaces.Cast<Relation>().Where(item => item.Left is Relation edge &&
                (memberSet.Contains(edge.Left) || memberSet.Contains(edge.Right))).Cast<IElement>().ToArray();
            regions.Add(new Relation(new Relation(Surface, label.Right),
                new Relation(Scope(complete, memberIds), Scope(complete, boundary))));
        }
        var labelsComplete = basis.CellsClosed && basis.LabelsClosed && basis.Labels.All(label => label is not null);
        var interfacesComplete = basis.TopologyClosed && basis.Edges.All(edge =>
            basis.Labels[edge.From] is not null && basis.Labels[edge.To] is not null);
        return new Relation(Surface, new Relation(
            new Relation(new Relation(RolePair.Left, Scope(labelsComplete, first.ToArray())),
                new Relation(RolePair.Right, Scope(labelsComplete, second.ToArray()))),
            new Relation(Scope(labelsComplete && basis.TopologyClosed, regions.ToArray()),
                new Relation(Scope(interfacesComplete, interfaces.ToArray()), S(pending.ToArray())))));
    }

    /// <summary>Checks an explicitly supplied cell permutation. Equal role
    /// counts alone never establish exchange. Native mismatches and incomplete
    /// obligations are retained separately from locally matching evidence.</summary>
    public Relation EvaluateExchange(Relation cellMap)
    {
        var matched = new List<IElement>(); var unmatched = new List<IElement>(); var pending = new List<IElement>();
        var basis = ReadBasis(pending);
        var mapClosed = ReadScope(cellMap, out var rows);
        if (!mapClosed) pending.Add(new Relation(cellMap, Surface));
        var rowsByCell = Enumerable.Range(0, basis.Cells.Length).Select(_ => new List<Relation>()).ToArray();
        foreach (var value in rows)
        {
            if (value is not Relation row || !basis.Index.TryGetValue(row.Left, out var from) ||
                !basis.Index.ContainsKey(row.Right))
            { unmatched.Add(new Relation(value, Cells)); continue; }
            rowsByCell[from].Add(row);
        }
        var image = Enumerable.Repeat(-1, basis.Cells.Length).ToArray();
        var actualMap = new Relation?[basis.Cells.Length];
        var inverse = Enumerable.Range(0, basis.Cells.Length).Select(_ => new List<int>()).ToArray();
        for (var from = 0; from < basis.Cells.Length; from++)
        {
            var offers = rowsByCell[from];
            if (offers.Count == 0) { pending.Add(new Relation(basis.Cells[from], cellMap)); continue; }
            if (offers.Count != 1)
            { unmatched.Add(new Relation(basis.Cells[from], S(offers.ToArray()))); continue; }
            var row = offers[0]; var to = basis.Index[row.Right];
            image[from] = to; actualMap[from] = row; inverse[to].Add(from);
            if (basis.Labels[from] is not Relation a || basis.Labels[to] is not Relation b)
            { pending.Add(new Relation(row, Labels)); continue; }
            var witness = new Relation(row, new Relation(a, b));
            (Same(a.Right, b.Right) ? unmatched : matched).Add(witness);
        }
        for (var to = 0; to < inverse.Length; to++)
        {
            if (inverse[to].Count == 0) pending.Add(new Relation(cellMap, basis.Cells[to]));
            else if (inverse[to].Count != 1)
                unmatched.Add(new Relation(S(inverse[to].Select(i => (IElement)actualMap[i]!).ToArray()), basis.Cells[to]));
        }
        var edgeLookup = basis.Edges.GroupBy(edge => (edge.From, edge.To))
            .ToDictionary(group => group.Key, group => group.Select(edge => (IElement)edge.Edge).ToArray());
        foreach (var (edge, from, to) in basis.Edges)
        {
            if (image[from] < 0 || image[to] < 0)
            { pending.Add(new Relation(edge, cellMap)); continue; }
            var target = new Relation(basis.Cells[image[from]], basis.Cells[image[to]]);
            var found = edgeLookup.TryGetValue((image[from], image[to]), out var offered) ? offered : [];
            var witness = new Relation(edge, new Relation(target, S(found)));
            if (found.Length != 0) matched.Add(witness);
            else if (basis.TopologyClosed) unmatched.Add(witness);
            else pending.Add(witness);
        }
        // The reverse obligation is explicit as well. It prevents treating a
        // partial one-way preservation read as an isomorphism certificate.
        foreach (var (edge, from, to) in basis.Edges)
        {
            if (inverse[from].Count != 1 || inverse[to].Count != 1)
            { pending.Add(new Relation(cellMap, edge)); continue; }
            var a = inverse[from][0]; var b = inverse[to][0];
            var source = new Relation(basis.Cells[a], basis.Cells[b]);
            var found = edgeLookup.TryGetValue((a, b), out var offered) ? offered : [];
            var witness = new Relation(new Relation(cellMap, edge), new Relation(source, S(found)));
            if (found.Length != 0) matched.Add(witness);
            else if (basis.TopologyClosed) unmatched.Add(witness);
            else pending.Add(witness);
        }
        return new Relation(new Relation(new Relation(Surface, cellMap), RoleExchange),
            new Relation(S(matched.ToArray()), new Relation(S(unmatched.ToArray()), S(pending.ToArray()))));
    }

    public bool TryReadResult(Relation read, out Relation first, out Relation second, out Relation regions,
        out Relation interfaces, out Relation unresolved)
    {
        first = second = regions = interfaces = unresolved = null!;
        if (!ReferenceEquals(read.Left, Surface) || read.Right is not Relation { Left: Relation groups, Right: Relation result } ||
            groups.Left is not Relation a || groups.Right is not Relation b || !Same(a.Left, RolePair.Left) || !Same(b.Left, RolePair.Right) ||
            a.Right is not Relation aRows || b.Right is not Relation bRows ||
            result.Left is not Relation components || result.Right is not Relation { Left: Relation boundaries, Right: Relation pending } ||
            !Scope(aRows) || !Scope(bRows) || !Scope(components) || !Scope(boundaries) || !ReadScope(pending, out _)) return false;
        var scratch = new List<IElement>(); var basis = ReadBasis(scratch);
        var labelsComplete = basis.CellsClosed && basis.LabelsClosed && basis.Labels.All(label => label is not null);
        var interfacesComplete = basis.TopologyClosed && basis.Edges.All(edge =>
            basis.Labels[edge.From] is not null && basis.Labels[edge.To] is not null);
        if (ReadScope(aRows, out _) != labelsComplete || ReadScope(bRows, out _) != labelsComplete ||
            ReadScope(components, out _) != (labelsComplete && basis.TopologyClosed) ||
            ReadScope(boundaries, out _) != interfacesComplete ||
            (E(pending).Length == 0) != (scratch.Count == 0)) return false;
        var regionCells = new HashSet<int>(); var interfaceEdges = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
        foreach (var entry in E(components))
        {
            if (entry is not Relation region || !ReadRegion(region, basis, out _, out var members, out _)) return false;
            foreach (var cell in E(members)) if (!regionCells.Add(basis.Index[cell])) return false;
        }
        for (var i = 0; i < basis.Labels.Length; i++) if (basis.Labels[i] is not null && !regionCells.Contains(i)) return false;
        foreach (var entry in E(boundaries))
        {
            if (entry is not Relation boundary || !ReadInterface(boundary, basis, out var edge, out _, out _)) return false;
            interfaceEdges.Add(edge);
        }
        foreach (var (edge, from, to) in basis.Edges)
            if (basis.Labels[from] is Relation fromLabel && basis.Labels[to] is Relation toLabel &&
                !Same(fromLabel.Right, toLabel.Right) && !interfaceEdges.Contains(edge)) return false;
        var expectedA = basis.Labels.Where(label => label is not null && Same(label.Right, RolePair.Left)).Cast<IElement>().ToArray();
        var expectedB = basis.Labels.Where(label => label is not null && Same(label.Right, RolePair.Right)).Cast<IElement>().ToArray();
        if (!SameRows(E(aRows), expectedA) || !SameRows(E(bRows), expectedB)) return false;
        first = aRows; second = bRows; regions = components; interfaces = boundaries; unresolved = pending;
        return true;
    }

    public bool TryReadRegion(Relation region, out IElement role, out Relation cells, out Relation boundary)
        => ReadRegion(region, ReadBasis([]), out role, out cells, out boundary);

    private bool ReadRegion(Relation region, Basis basis, out IElement role, out Relation cells, out Relation boundary)
    {
        role = null!; cells = boundary = null!;
        if (region.Left is not Relation owner || !ReferenceEquals(owner.Left, Surface) ||
            !IsRole(owner.Right) || region.Right is not Relation { Left: Relation members, Right: Relation edges } ||
            !Scope(members) || !Scope(edges)) return false;
        var selected = new HashSet<int>();
        foreach (var member in E(members))
            if (!basis.Index.TryGetValue(member, out var at) || !selected.Add(at) ||
                basis.Labels[at] is not Relation label || !Same(label.Right, owner.Right)) return false;
        if (selected.Count == 0) return false;
        var reached = new HashSet<int>(); var queue = new Queue<int>(); queue.Enqueue(selected.First());
        while (queue.TryDequeue(out var cell))
        {
            if (!reached.Add(cell)) continue;
            foreach (var next in basis.Incidence[cell]) if (selected.Contains(next)) queue.Enqueue(next);
        }
        if (reached.Count != selected.Count) return false;
        var complete = basis.TopologyClosed && basis.CellsClosed;
        foreach (var cell in selected)
            foreach (var next in basis.Incidence[cell])
            {
                if (basis.Labels[next] is not Relation neighbor) complete = false;
                else if (Same(neighbor.Right, owner.Right) && !selected.Contains(next)) return false;
            }
        if (ReadScope(members, out _) && !complete || ReadScope(edges, out _) && !complete) return false;
        var exposed = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
        foreach (var entry in E(edges))
            if (entry is not Relation edge || !ReadInterface(edge, basis, out var actual, out _, out _) ||
                !selected.Contains(basis.Index[actual.Left]) && !selected.Contains(basis.Index[actual.Right])) return false;
            else exposed.Add(actual);
        foreach (var (edge, from, to) in basis.Edges)
            if ((selected.Contains(from) || selected.Contains(to)) && basis.Labels[from] is Relation a &&
                basis.Labels[to] is Relation b && !Same(a.Right, b.Right) && !exposed.Contains(edge)) return false;
        role = owner.Right; cells = members; boundary = edges;
        return true;
    }

    public bool TryReadInterface(Relation witness, out Relation edge, out Relation fromLabel, out Relation toLabel)
        => ReadInterface(witness, ReadBasis([]), out edge, out fromLabel, out toLabel);

    private bool ReadInterface(Relation witness, Basis basis, out Relation edge, out Relation fromLabel, out Relation toLabel)
    {
        edge = fromLabel = toLabel = null!;
        if (witness.Left is not Relation actual || !basis.EdgeSet.Contains(actual) ||
            witness.Right is not Relation { Left: Relation a, Right: Relation b } ||
            !basis.Index.TryGetValue(actual.Left, out var from) || !basis.Index.TryGetValue(actual.Right, out var to) ||
            !ReferenceEquals(basis.Labels[from], a) || !ReferenceEquals(basis.Labels[to], b) || Same(a.Right, b.Right)) return false;
        edge = actual; fromLabel = a; toLabel = b; return true;
    }

    public bool TryReadExchange(Relation read, out Relation cellMap, out Relation matched,
        out Relation unmatched, out Relation unresolved)
    {
        cellMap = matched = unmatched = unresolved = null!;
        if (read.Left is not Relation { Left: Relation context } exchange || !ReferenceEquals(context.Left, Surface) ||
            !ReferenceEquals(exchange.Right, RoleExchange) || context.Right is not Relation map ||
            read.Right is not Relation { Left: Relation yes, Right: Relation { Left: Relation no, Right: Relation pending } } ||
            !ReadScope(yes, out _) || !ReadScope(no, out _) || !ReadScope(pending, out _)) return false;
        cellMap = map; matched = yes; unmatched = no; unresolved = pending; return true;
    }

    /// <summary>Prospective certification checks the actual native map and
    /// source again; caller-authored empty failure scopes cannot create proof.</summary>
    public bool TryReadExchangePassed(Relation read)
    {
        if (!TryReadExchange(read, out var map, out _, out var unmatched, out var pending) || E(unmatched).Length != 0 || E(pending).Length != 0) return false;
        return TryReadExchange(EvaluateExchange(map), out _, out _, out var freshNo, out var freshPending) &&
            E(freshNo).Length == 0 && E(freshPending).Length == 0;
    }

    private Basis ReadBasis(List<IElement> pending)
    {
        var cellsClosed = ReadScope(Cells, out var rawCells);
        var cells = rawCells.Distinct(Participants.Instance).ToArray();
        if (!cellsClosed || cells.Length != rawCells.Length)
        { cellsClosed = false; pending.Add(new Relation(Cells, Surface)); }
        var index = cells.Select((cell, i) => (cell, i)).ToDictionary(pair => pair.cell, pair => pair.i, Participants.Instance);
        var topologyClosed = ReadScope(Adjacency, out var edgeEntries);
        if (!topologyClosed) pending.Add(new Relation(Adjacency, Surface));
        var edges = new List<(Relation Edge, int From, int To)>();
        foreach (var value in edgeEntries)
        {
            if (value is not Relation edge || !index.TryGetValue(edge.Left, out var from) || !index.TryGetValue(edge.Right, out var to))
            { topologyClosed = false; pending.Add(new Relation(value, Cells)); continue; }
            edges.Add((edge, from, to));
        }
        var rolesValid = !Same(RolePair.Left, RolePair.Right);
        if (!rolesValid) pending.Add(new Relation(RolePair, Surface));
        var labelsClosed = ReadScope(Labels, out var rowEntries);
        if (!labelsClosed) pending.Add(new Relation(Labels, Surface));
        var offered = Enumerable.Range(0, cells.Length).Select(_ => new List<Relation>()).ToArray();
        foreach (var value in rowEntries)
        {
            if (value is not Relation row || !index.TryGetValue(row.Left, out var at))
            { pending.Add(new Relation(value, Cells)); continue; }
            offered[at].Add(row);
        }
        var labels = new Relation?[cells.Length];
        for (var i = 0; i < cells.Length; i++)
        {
            if (rolesValid && labelsClosed && offered[i].Count == 1 && IsRole(offered[i][0].Right)) labels[i] = offered[i][0];
            else pending.Add(new Relation(cells[i], new Relation(Labels, S(offered[i].ToArray()))));
        }
        var incidence = Enumerable.Range(0, cells.Length).Select(_ => new List<int>()).ToArray();
        foreach (var (_, from, to) in edges) { incidence[from].Add(to); incidence[to].Add(from); }
        return new(cells, index, labels, edges, incidence, new HashSet<Relation>(edges.Select(edge => edge.Edge), ReferenceEqualityComparer.Instance),
            cellsClosed, topologyClosed, labelsClosed);
    }

    private bool IsRole(IElement role) => !Same(RolePair.Left, RolePair.Right) && (Same(role, RolePair.Left) || Same(role, RolePair.Right));
    private sealed record Basis(IElement[] Cells, Dictionary<IElement, int> Index, Relation?[] Labels,
        List<(Relation Edge, int From, int To)> Edges, List<int>[] Incidence, HashSet<Relation> EdgeSet,
        bool CellsClosed, bool TopologyClosed, bool LabelsClosed);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static Relation Scope(bool closed, params IElement[] entries) => closed ? S(entries) : StructuralConstraint.Open(entries);
    private static bool Scope(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out _, out _);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    private static bool ReadScope(Relation scope, out IElement[] entries) =>
        StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail) && seam.Equals(tail);
    private static bool SameRows(IElement[] actual, IElement[] expected) => actual.Length == expected.Length &&
        actual.Zip(expected).All(pair => ReferenceEquals(pair.First, pair.Second));
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) ||
        a is Placeholder x && b is Placeholder y && x.Equals(y) ||
        a is CarrierLocation p && b is CarrierLocation q && p.Carrier.SameIdentityAs(q.Carrier) && p.SameTickRefAs(q);
    private sealed class Participants : IEqualityComparer<IElement>
    {
        public static readonly Participants Instance = new();
        public bool Equals(IElement? x, IElement? y) => x is not null && y is not null && Same(x, y);
        public int GetHashCode(IElement element) => element switch
        {
            Placeholder slot => slot.GetHashCode(),
            CarrierLocation site => HashCode.Combine(site.Carrier.Id, System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(site.Tick)),
            _ => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(element)
        };
    }
}
