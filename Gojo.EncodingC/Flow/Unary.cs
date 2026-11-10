namespace Gojo.EncodingC;

/// <summary>Unary counts on bit planes. Bit k of a count means "more than k",
/// so adding one shifts a bit in and removing one shifts a bit out, at every
/// selected position at once. Change that arrives from outside a structure
/// enters this way, as units, never as a direct value (canonical Axiom 5).</summary>
public static class Unary
{
    /// <summary>Adds one unit at each given position. Positions already full
    /// are returned and left unchanged: the caller keeps them as overflow, so
    /// nothing is lost without trace.</summary>
    public static Plane Add(Structure structure, Field field, Plane positions)
    {
        var planes = Load(structure, field, positions);
        var overflow = positions.And(planes[^1]);
        var take = positions.AndNot(overflow);
        for (var k = planes.Length - 1; k >= 1; k--) planes[k] = planes[k].Or(planes[k - 1].And(take));
        planes[0] = planes[0].Or(take);
        Store(structure, field, planes, take);
        return overflow;
    }

    /// <summary>Removes one unit at each given position. Positions already empty
    /// are returned and left unchanged.</summary>
    public static Plane Remove(Structure structure, Field field, Plane positions)
    {
        var planes = Load(structure, field, positions);
        var underflow = positions.AndNot(planes[0]);
        var take = positions.AndNot(underflow);
        for (var k = 0; k < planes.Length - 1; k++) planes[k] = planes[k].And(planes[k + 1].Or(take.Not()));
        planes[^1] = planes[^1].AndNot(take);
        Store(structure, field, planes, take);
        return underflow;
    }

    /// <summary>Positions holding at least one unit.</summary>
    public static Plane NonZero(Structure structure, Field field) => Load(structure, field, structure.Live)[0];

    /// <summary>Positions at capacity.</summary>
    public static Plane Full(Structure structure, Field field) => Load(structure, field, structure.Live)[^1];

    private static Plane[] Load(Structure structure, Field field, Plane positions)
    {
        ArgumentNullException.ThrowIfNull(structure);
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(positions);
        if (!field.Shape.IsUnary) throw new ArgumentException("A unary field is needed.", nameof(field));
        return field.Planes.Select(plane => structure.Read(plane)).ToArray();
    }

    private static void Store(Structure structure, Field field, Plane[] planes, Plane mask)
    {
        for (var k = 0; k < planes.Length; k++) structure.Write(field.Planes[k], planes[k], mask);
    }
}
