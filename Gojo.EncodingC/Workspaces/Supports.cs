namespace Gojo.EncodingC;

/// <summary>A value that is not settled yet, held in the planes like any other
/// value. A field's bits are a code: one value, a constant. Its supports are
/// one more plane per value, set where that value has been <em>ruled out</em>.
/// <para><b>Open is the start.</b> A fresh position has nothing ruled out, so
/// every value is possible: everything that arrives is uncertain until
/// something accounts for it. Each bit set is one thing learned. All but one
/// ruled out is settled; all ruled out is a contradiction, kept visible. A
/// position the structure adds later starts open with no extra step.</para>
/// <para><b>In the planes.</b> These are ordinary planes of the structure. A
/// saved machine keeps them, a law can read them, and a rung copies them on
/// write. Narrowing in a rung is then a supposition: it can be compared,
/// dropped when it reaches a contradiction, or committed.</para>
/// <para><b>Narrowing and forgetting.</b> Relation laws only rule values out.
/// <see cref="Forget"/> opens a position again: the next moment has not
/// happened yet, and a place that may be changed is open by choice. A goal is
/// a value settled in advance. Narrowing between what is and what is wanted
/// then settles the open places in between (see
/// <see cref="TransitionLaw.AsRelation"/>).</para>
/// <para>"Every value possible" is uncertainty inside a declared, finite set of
/// values. It says nothing about values outside that set. One support tells
/// apart at most 64 values; a field with more is read through a coarser field
/// first, as a ladder does, rather than by widening the primitive.</para></summary>
public sealed class Supports
{
    private readonly int[] planes;

    /// <summary>Supports for <paramref name="field"/> on a surface, held in
    /// <paramref name="planes"/>: plane v is set where value v is ruled out.</summary>
    public Supports(Surface surface, Field field, params int[] planes)
    {
        ArgumentNullException.ThrowIfNull(surface);
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(planes);
        CheckSize(field);
        if (surface.ThroughPort || surface.ReadOnly)
            throw new ArgumentException("What is still possible is kept in a structure's or a rung's own planes, not behind a port.", nameof(surface));
        if (planes.Length != field.Shape.Size)
            throw new ArgumentException($"A field of {field.Shape.Size} values needs {field.Shape.Size} planes, one per value.", nameof(planes));
        if (planes.Distinct().Count() != planes.Length || planes.Intersect(field.Planes).Any())
            throw new ArgumentException("Each value has its own plane, apart from the field's bits.", nameof(planes));
        foreach (var plane in planes)
        {
            if ((uint)plane >= (uint)surface.PlaneCount) throw new ArgumentOutOfRangeException(nameof(planes));
            if (surface.IsConstant(plane)) throw new ArgumentException("A constant plane is one bit for every position; a support differs by position.", nameof(planes));
        }
        Surface = surface;
        Field = field;
        this.planes = planes.ToArray();
    }

    public Surface Surface { get; }

    /// <summary>The field these supports describe: its shape, and where a
    /// sample of it sits.</summary>
    public Field Field { get; }

    /// <summary>The planes, one per value, set where the value is ruled out.</summary>
    public IReadOnlyList<int> Planes => Array.AsReadOnly(planes);

    /// <summary>Adds one plane per value to a structure and returns the
    /// supports held there. Every position starts open.</summary>
    public static Supports On(Structure structure, Field field)
    {
        ArgumentNullException.ThrowIfNull(structure);
        ArgumentNullException.ThrowIfNull(field);
        CheckSize(field);
        var added = Enumerable.Range(0, field.Shape.Size).Select(_ => structure.AddPlane()).ToArray();
        return new Supports(structure, field, added);
    }

    /// <summary>The same supports seen through another surface that shows the
    /// same planes, such as a rung over this structure. What is narrowed or
    /// forgotten there is supposed, not known.</summary>
    public Supports In(Surface surface) => new(surface, Field, planes);

    /// <summary>Positions where a value is still possible.</summary>
    public Plane Of(int value)
    {
        if ((uint)value >= (uint)planes.Length) throw new ArgumentOutOfRangeException(nameof(value));
        var possible = new Plane(Surface.Count);
        for (var i = 0; i < possible.Length; i++)
            if (Surface.IsLive(i) && !Surface.Read(planes[value], i)) possible[i] = true;
        return possible;
    }

    /// <summary>The values still possible at a position, as a mask.</summary>
    public ulong Values(int position)
    {
        ulong mask = 0;
        for (var v = 0; v < planes.Length; v++)
            if (!Surface.Read(planes[v], position)) mask |= 1UL << v;
        return mask;
    }

    /// <summary>Positions with one value left.</summary>
    public Plane Settled => Positions(count => count == 1);

    /// <summary>Positions with several values left.</summary>
    public Plane Open => Positions(count => count > 1);

    /// <summary>Positions with no value left: a contradiction.</summary>
    public Plane Empty => Positions(count => count == 0);

    /// <summary>Keeps only the allowed values at a position. Returns whether
    /// anything was ruled out.</summary>
    public bool Restrict(int position, ulong allowed)
    {
        var changed = false;
        for (var v = 0; v < planes.Length; v++)
            if (((allowed >> v) & 1) == 0 && !Surface.Read(planes[v], position))
            {
                Surface.Set(planes[v], position, true);
                changed = true;
            }
        return changed;
    }

    /// <summary>Narrows a position to the value its field's bits hold. Returns
    /// false, changing nothing, when the bits are not a code. An observation
    /// that had been ruled out leaves the position empty: the read and what
    /// was believed disagree, and neither is silently preferred.</summary>
    public bool Observe(int position)
    {
        if (Field.Read(Surface, position) is not int value) return false;
        Restrict(position, 1UL << value);
        return true;
    }

    /// <summary>Opens a position again: nothing ruled out. Returns whether
    /// anything had been.</summary>
    public bool Forget(int position)
    {
        var changed = false;
        for (var v = 0; v < planes.Length; v++)
            if (Surface.Read(planes[v], position))
            {
                Surface.Set(planes[v], position, false);
                changed = true;
            }
        return changed;
    }

    /// <summary>Writes a settled position's one remaining value into the
    /// field's bits, turning what was worked out into what is. Returns false,
    /// changing nothing, when the position is open or empty.</summary>
    public bool Enact(int position)
    {
        var values = Values(position);
        if (System.Numerics.BitOperations.PopCount(values) != 1) return false;
        Field.Write(Surface, position, System.Numerics.BitOperations.TrailingZeroCount(values));
        return true;
    }

    /// <summary>Narrows through a relation wiring, every row at once.
    /// <paramref name="roles"/> gives the supports each role reads; two roles
    /// of a row that read one position of the same supports keep equal
    /// values. Returns whether anything was ruled out.</summary>
    public static bool Narrow(Wiring relation, params Supports[] roles)
    {
        ArgumentNullException.ThrowIfNull(relation);
        ArgumentNullException.ThrowIfNull(roles);
        if (relation.Law is not RelationLaw law) throw new ArgumentException("Supports narrow through relation laws.", nameof(relation));
        if (roles.Length != law.Roles.Count) throw new ArgumentException($"Give supports for all {law.Roles.Count} roles.", nameof(roles));
        for (var r = 0; r < roles.Length; r++)
        {
            var binding = relation.Bindings[r];
            if (!ReferenceEquals(binding.Surface, roles[r].Surface) || !binding.Planes.SequenceEqual(roles[r].Field.Planes) || roles[r].Field.Shape != law.Roles[r])
                throw new ArgumentException($"Role {r} is not bound to the field these supports describe.", nameof(roles));
        }
        var lanes = relation.Runnable();
        var rows = lanes.Indexes().ToDictionary(row => row, relation.Row);
        var supports = new Plane[roles.Length][];
        for (var r = 0; r < roles.Length; r++)
        {
            supports[r] = Enumerable.Range(0, law.Roles[r].Size).Select(_ => new Plane(relation.Count)).ToArray();
            foreach (var (row, indexes) in rows)
            {
                var possible = roles[r].Values(indexes[r]);
                for (var v = 0; v < supports[r].Length; v++)
                    if (((possible >> v) & 1) != 0) supports[r][v][row] = true;
            }
        }
        var same = new Plane?[roles.Length, roles.Length];
        for (var r = 0; r < roles.Length; r++)
            for (var j = r + 1; j < roles.Length; j++)
                if (roles[r].SameAs(roles[j]))
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
    /// Returns the rounds taken; narrowing only rules out, so this ends.</summary>
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

    /// <summary>The same planes on the same surface: one variable, however
    /// many times it was described.</summary>
    private bool SameAs(Supports other) => ReferenceEquals(other.Surface, Surface) && other.planes.AsSpan().SequenceEqual(planes);

    private Plane Positions(Func<int, bool> count)
    {
        var result = new Plane(Surface.Count);
        for (var position = 0; position < result.Length; position++)
            if (Surface.IsLive(position) && count(System.Numerics.BitOperations.PopCount(Values(position)))) result[position] = true;
        return result;
    }

    private static void CheckSize(Field field)
    {
        if (field.Shape.Size > 64)
            throw new ArgumentException($"A support tells apart at most 64 values; this field has {field.Shape.Size}. Read it through a coarser field.", nameof(field));
    }
}
