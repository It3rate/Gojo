namespace Gojo.EncodingB;

/// <summary>Neighbourhood over sites, used to grow selections.</summary>
public abstract class Adjacency
{
    /// <summary>The frontier plus its neighbours.</summary>
    public abstract ulong Expand(ulong frontier);

    public abstract string Describe();

    /// <summary>Implicit chain topology: a neighbour is a shift. No rows stored.</summary>
    public static Adjacency Chain(int count) => new ChainAdjacency(count);

    /// <summary>Implicit ring topology: shifts that wrap within <paramref name="count"/> sites.</summary>
    public static Adjacency Ring(int count) => new RingAdjacency(count);

    /// <summary>Explicit rows: any row that touches the frontier contributes all its sites.</summary>
    public static Adjacency Rows(RowSet rows) => new RowAdjacency(rows);

    private sealed class ChainAdjacency(int count) : Adjacency
    {
        public override ulong Expand(ulong f) => (f | (f << 1) | (f >> 1)) & Bits.Low(count);

        public override string Describe() => $"chain of {count}";
    }

    private sealed class RingAdjacency(int count) : Adjacency
    {
        public override ulong Expand(ulong f)
        {
            var all = Bits.Low(count);
            var left = ((f << 1) | (f >> (count - 1))) & all;
            var right = ((f >> 1) | (f << (count - 1))) & all;
            return (f | left | right) & all;
        }

        public override string Describe() => $"ring of {count}";
    }

    private sealed class RowAdjacency(RowSet rows) : Adjacency
    {
        public override ulong Expand(ulong f)
        {
            var next = f;
            for (var row = 0; row < rows.Count; row++)
                if ((rows.SitesOf(row) & f) != 0) next |= rows.SitesOf(row);
            return next;
        }

        public override string Describe() => $"rows {rows.Name}";
    }
}

/// <summary>Small machines that produce selections. Each result records its
/// machine and scope, so it stays structure rather than a list of indexes.</summary>
public static class SelectionMachines
{
    /// <summary>An explicitly empty selection.</summary>
    public static Selection None(SelectionScope scope) => new("none", scope, 0, closed: false, parent: null);

    /// <summary>Everything in the scope; closed, so it can support absence within it.</summary>
    public static Selection All(SelectionScope scope) => new("all", scope, scope.Members, closed: true, parent: null);

    /// <summary>Open picks: "these". No claim about anything else in scope.</summary>
    public static Selection These(SelectionScope scope, ulong members, string reason) =>
        new($"these: {reason}", scope, members, closed: false, parent: null);

    /// <summary>Every second member of the scope in order, starting at <paramref name="phase"/>.
    /// For a contiguous scope from site 0 this is <c>0x5555555555555555 &amp; scope</c>.</summary>
    public static Selection EverySecond(SelectionScope scope, int phase = 0, Selection? parent = null)
    {
        if (phase is not (0 or 1)) throw new ArgumentOutOfRangeException(nameof(phase));
        var members = Bits.Indexes(scope.Members).Where((_, i) => i % 2 == phase).Aggregate(0UL, (m, s) => m | Bits.Of(s));
        return new($"every second element, phase {phase}", scope, members, closed: true, parent);
    }

    public static Selection EverySecond(Selection parent, int phase = 0) => EverySecond(parent.AsScope(), phase, parent);

    /// <summary>On a ring scope with an even number of members, omit member
    /// <paramref name="first"/> and the one opposite it.</summary>
    public static Selection OmitOppositePair(SelectionScope scope, int first)
    {
        var members = Bits.Indexes(scope.Members).ToArray();
        if (members.Length % 2 != 0) throw new ArgumentException("An opposite pair needs an even ring.", nameof(scope));
        if (first < 0 || first >= members.Length / 2) throw new ArgumentOutOfRangeException(nameof(first));
        var omitted = Bits.Of(members[first]) | Bits.Of(members[first + members.Length / 2]);
        return new($"omit opposite pair {first} and {first + members.Length / 2}", scope, scope.Members & ~omitted, closed: true, parent: null);
    }

    /// <summary>Everything within <paramref name="steps"/> connections of the seeds:
    /// <c>F = F | N(F)</c>, repeated, kept inside the scope.</summary>
    public static Selection Within(SelectionScope scope, ulong seeds, int steps, Adjacency adjacency)
    {
        ArgumentNullException.ThrowIfNull(adjacency);
        if (steps < 0) throw new ArgumentOutOfRangeException(nameof(steps));
        var frontier = seeds & scope.Members;
        for (var i = 0; i < steps; i++) frontier = adjacency.Expand(frontier) & scope.Members;
        return new($"within {steps} of seeds {{{string.Join(",", Bits.Indexes(seeds & scope.Members))}}} over {adjacency.Describe()}",
            scope, frontier, closed: true, parent: null);
    }

    /// <summary>Grows from seeds to the least fixed point. Growth only adds, so the
    /// order of expansion does not matter. Returns the number of growth steps taken.</summary>
    public static (Selection Selection, int Steps) Grow(SelectionScope scope, ulong seeds, Adjacency adjacency)
    {
        ArgumentNullException.ThrowIfNull(adjacency);
        var frontier = seeds & scope.Members;
        var steps = 0;
        while (true)
        {
            var next = adjacency.Expand(frontier) & scope.Members;
            if (next == frontier) break;
            frontier = next;
            steps++;
        }
        return (new($"grown from seeds to a fixed point over {adjacency.Describe()}", scope, frontier, closed: true, parent: null), steps);
    }

    /// <summary>Everything in the scope where a predicate plane is set, such as an
    /// area read in a chosen frame.</summary>
    public static Selection Where(SelectionScope scope, ulong predicate, string description) =>
        new($"where {description}", scope, predicate & scope.Members, closed: true, parent: null);

    /// <summary>The sites touched by the selected rows of a row selection.</summary>
    public static Selection SitesOf(Selection rowSelection)
    {
        ArgumentNullException.ThrowIfNull(rowSelection);
        if (!rowSelection.Scope.OverRows) throw new ArgumentException("Select rows first.", nameof(rowSelection));
        var rows = rowSelection.Scope.Tile.Rows(rowSelection.Scope.RowSet!);
        var sites = rowSelection.InOrder.Aggregate(0UL, (m, row) => m | rows.SitesOf(row));
        return new($"sites of ({rowSelection.Machine})", SelectionScope.Sites(rowSelection.Scope.Tile), sites,
            closed: true, parent: rowSelection);
    }
}
