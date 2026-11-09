namespace Gojo.EncodingB;

/// <summary>What a selection ranges over: a tile's sites, or the rows of one
/// of its row sets, restricted to <see cref="Members"/>.</summary>
public sealed record SelectionScope(Tile Tile, string? RowSet, ulong Members, string Description)
{
    public static SelectionScope Sites(Tile tile) => new(tile, null, tile.Allocated, $"all sites of {tile.Name}");

    public static SelectionScope Rows(Tile tile, string rowSet) =>
        new(tile, rowSet, tile.Rows(rowSet).All, $"all rows of {tile.Name}.{rowSet}");

    public bool OverRows => RowSet is not null;
}

/// <summary>How a selection reads one element.</summary>
public enum MembershipRead
{
    /// <summary>Selected: in this focus.</summary>
    Selected,

    /// <summary>In scope and decided against by a closed selection's machine.</summary>
    ExcludedWithinScope,

    /// <summary>No claim: an open selection, or outside the scope. Not absence.</summary>
    NoClaim,
}

/// <summary>A read over structure: which elements of a scope are in this focus.
/// It is not stored in the selected elements and is not a value. A selection is
/// Gojo structure only while it stays connected to its scope and to the machine
/// that produced it; a detached mask would be a bag of indexes.</summary>
public sealed class Selection
{
    internal Selection(string machine, SelectionScope scope, ulong members, bool closed, Selection? parent)
    {
        if ((members & ~scope.Members) != 0) throw new ArgumentException("A selection stays within its scope.", nameof(members));
        Machine = machine;
        Scope = scope;
        Members = members;
        Closed = closed;
        Parent = parent;
    }

    /// <summary>The machine that produced this selection, in words.</summary>
    public string Machine { get; }

    public SelectionScope Scope { get; }

    /// <summary>The selection plane: bit i set when element i is selected.</summary>
    public ulong Members { get; }

    /// <summary>Closed selections decided membership for every element of their
    /// scope, so they can support "not in this selection" within that scope.
    /// Open selections ("these") make no claim about anything else.</summary>
    public bool Closed { get; }

    /// <summary>The selection this one selected from, when it is a selection of a selection.</summary>
    public Selection? Parent { get; }

    public IReadOnlyList<int> InOrder => Bits.Indexes(Members).ToArray();

    public int Count => Bits.Count(Members);

    /// <summary>Scope for selecting from this selection: its members, in order.</summary>
    public SelectionScope AsScope() => Scope with { Members = Members, Description = $"members of ({Machine})" };

    /// <summary>The commit rule: a selection covers an accounting scope when no
    /// element of that scope is left out. <c>(scope &amp; ~Sel) == 0</c>.</summary>
    public bool Covers(ulong accountingScope) => (accountingScope & ~Members) == 0;

    /// <summary>Elements of an accounting scope this selection leaves out.</summary>
    public ulong Missing(ulong accountingScope) => accountingScope & ~Members;

    public MembershipRead Read(int index)
    {
        if (Bits.Get(Members, index)) return MembershipRead.Selected;
        return Closed && Bits.Get(Scope.Members, index) ? MembershipRead.ExcludedWithinScope : MembershipRead.NoClaim;
    }

    /// <summary>Steps between consecutive members around a ring of
    /// <paramref name="ringSize"/> equal partitions, starting at the first member.</summary>
    public int[] RingGaps(int ringSize)
    {
        var members = InOrder;
        if (members.Count == 0) return [];
        if (members[^1] >= ringSize) throw new ArgumentOutOfRangeException(nameof(ringSize));
        return members.Select((m, i) => i + 1 < members.Count ? members[i + 1] - m : ringSize - m + members[0]).ToArray();
    }

    public string Pattern(int width) => Bits.Format(Members, width);

    public override string ToString() =>
        $"{(Closed ? "closed" : "open")} selection {Pattern(Scope.OverRows ? Scope.Tile.Rows(Scope.RowSet!).Count : Scope.Tile.SiteCount)} by {Machine}";
}
