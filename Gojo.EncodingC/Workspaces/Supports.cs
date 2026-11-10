namespace Gojo.EncodingC;

/// <summary>What a field may still be at each position: one plane per value,
/// bit i set while value v is possible at position i. Nothing extra marks
/// "unknown": every value possible is open, one is settled, none is a
/// contradiction. Supports are narrowed by relation laws and never widened by
/// them.</summary>
public sealed class Supports
{
    private readonly Plane[] values;

    public Supports(Structure structure, Field field)
    {
        ArgumentNullException.ThrowIfNull(structure);
        ArgumentNullException.ThrowIfNull(field);
        Structure = structure;
        Field = field;
        values = Enumerable.Range(0, field.Shape.Size).Select(_ => structure.Live).ToArray();
    }

    public Structure Structure { get; }

    public Field Field { get; }

    public Plane Of(int value) => values[value].Clone();

    /// <summary>The values still possible at a position, as a mask.</summary>
    public ulong Values(int position)
    {
        ulong mask = 0;
        for (var v = 0; v < values.Length; v++)
            if (values[v][position]) mask |= 1UL << v;
        return mask;
    }

    public Plane Settled => Positions(count => count == 1);

    public Plane Open => Positions(count => count > 1);

    public Plane Empty => Positions(count => count == 0);

    /// <summary>Keeps only the allowed values at a position. Returns whether anything was removed.</summary>
    public bool Restrict(int position, ulong allowed)
    {
        var changed = false;
        for (var v = 0; v < values.Length; v++)
            if (((allowed >> v) & 1) == 0 && values[v][position])
            {
                values[v][position] = false;
                changed = true;
            }
        return changed;
    }

    /// <summary>Narrows a position to the value its bits hold. Returns false,
    /// changing nothing, when the bits are not a code.</summary>
    public bool Observe(int position)
    {
        if (Field.Read(Structure, position) is not int value) return false;
        Restrict(position, 1UL << value);
        return true;
    }

    /// <summary>Narrows through a relation wiring, every row at once.
    /// <paramref name="roles"/> gives the supports each role reads; passing one
    /// supports object for two roles of a row that read the same position
    /// keeps them equal. Returns whether anything was removed.</summary>
    public static bool Narrow(Wiring relation, params Supports[] roles)
    {
        ArgumentNullException.ThrowIfNull(relation);
        ArgumentNullException.ThrowIfNull(roles);
        if (relation.Law is not RelationLaw law) throw new ArgumentException("Supports narrow through relation laws.", nameof(relation));
        if (roles.Length != law.Roles.Count) throw new ArgumentException($"Give supports for all {law.Roles.Count} roles.", nameof(roles));
        for (var r = 0; r < roles.Length; r++)
        {
            var binding = relation.Bindings[r];
            if (!ReferenceEquals(binding.Surface, roles[r].Structure) || !binding.Planes.SequenceEqual(roles[r].Field.Planes) || roles[r].Field.Shape != law.Roles[r])
                throw new ArgumentException($"Role {r} is not bound to the field these supports describe.", nameof(roles));
        }
        var lanes = relation.Runnable();
        var rows = lanes.Indexes().ToDictionary(row => row, relation.Row);
        var supports = new Plane[roles.Length][];
        for (var r = 0; r < roles.Length; r++)
        {
            supports[r] = new Plane[law.Roles[r].Size];
            for (var v = 0; v < supports[r].Length; v++)
            {
                supports[r][v] = new Plane(relation.Count);
                foreach (var (row, indexes) in rows)
                    if (roles[r].values[v][indexes[r]]) supports[r][v][row] = true;
            }
        }
        var same = new Plane?[roles.Length, roles.Length];
        for (var r = 0; r < roles.Length; r++)
            for (var j = r + 1; j < roles.Length; j++)
                if (ReferenceEquals(roles[r], roles[j]))
                    same[r, j] = Plane.Of(relation.Count, rows.Where(e => e.Value[r] == e.Value[j]).Select(e => e.Key));
        var next = law.Narrow(supports, lanes, same);
        var changed = false;
        foreach (var (row, indexes) in rows)
            for (var r = 0; r < roles.Length; r++)
            {
                ulong allowed = 0;
                for (var v = 0; v < next[r].Length; v++)
                    if (next[r][v][row]) allowed |= 1UL << v;
                changed |= roles[r].Restrict(indexes[r], allowed);
            }
        return changed;
    }

    /// <summary>Narrows through several relations until nothing changes.
    /// Returns the rounds taken; narrowing only removes, so this ends.</summary>
    public static int Propagate(IReadOnlyList<(Wiring Relation, Supports[] Roles)> relations)
    {
        ArgumentNullException.ThrowIfNull(relations);
        var rounds = 0;
        bool changed;
        do
        {
            changed = false;
            foreach (var (relation, roles) in relations) changed |= Narrow(relation, roles);
            rounds++;
        }
        while (changed);
        return rounds;
    }

    private Plane Positions(Func<int, bool> count)
    {
        var result = new Plane(Structure.Count);
        foreach (var position in Structure.Live.Indexes())
            if (count(System.Numerics.BitOperations.PopCount(Values(position)))) result[position] = true;
        return result;
    }
}
