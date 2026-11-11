namespace Gojo.EncodingC;

/// <summary>Unary counts on bit planes. Bit k of a count means "more than k",
/// so adding one shifts a bit in and removing one shifts a bit out, at every
/// selected position at once. Change that arrives from outside a structure
/// enters this way, as units, never as a direct value (canonical Axiom 5).
/// <para>Bits that are not a count, such as <c>010</c>, are noise this read
/// cannot interpret. They are reported and left as they are, never shifted
/// into a count that looks valid.</para></summary>
public static class Unary
{
    /// <summary>Adds one unit at each given position. Positions already full
    /// are returned as blocked and left unchanged: the caller keeps them as
    /// overflow, so nothing is lost without trace.</summary>
    public static UnaryStep Add(Structure structure, Field field, Plane positions)
    {
        var planes = Load(structure, field, positions);
        var broken = positions.And(BrokenIn(planes));
        var blocked = positions.And(planes[^1]).AndNot(broken);
        var take = positions.AndNot(blocked).AndNot(broken);
        for (var k = planes.Length - 1; k >= 1; k--) planes[k] = planes[k].Or(planes[k - 1].And(take));
        planes[0] = planes[0].Or(take);
        Store(structure, field, planes, take);
        return new UnaryStep(take, blocked, broken);
    }

    /// <summary>Removes one unit at each given position. Positions already empty
    /// are returned as blocked and left unchanged.</summary>
    public static UnaryStep Remove(Structure structure, Field field, Plane positions)
    {
        var planes = Load(structure, field, positions);
        var broken = positions.And(BrokenIn(planes));
        var blocked = positions.AndNot(planes[0]).AndNot(broken);
        var take = positions.AndNot(blocked).AndNot(broken);
        for (var k = 0; k < planes.Length - 1; k++) planes[k] = planes[k].And(planes[k + 1].Or(take.Not()));
        planes[^1] = planes[^1].AndNot(take);
        Store(structure, field, planes, take);
        return new UnaryStep(take, blocked, broken);
    }

    /// <summary>Positions holding at least one unit. Broken counts are not read
    /// as holding anything.</summary>
    public static Plane NonZero(Structure structure, Field field)
    {
        var planes = Load(structure, field, structure.Live);
        return planes[0].AndNot(BrokenIn(planes)).And(structure.Live);
    }

    /// <summary>Positions at capacity.</summary>
    public static Plane Full(Structure structure, Field field)
    {
        var planes = Load(structure, field, structure.Live);
        return planes[^1].AndNot(BrokenIn(planes)).And(structure.Live);
    }

    /// <summary>Live positions whose bits are not a count: a higher bit set
    /// while a lower one is clear.</summary>
    public static Plane Broken(Structure structure, Field field) =>
        BrokenIn(Load(structure, field, structure.Live)).And(structure.Live);

    private static Plane BrokenIn(Plane[] planes)
    {
        var broken = new Plane(planes[0].Length);
        for (var k = 0; k + 1 < planes.Length; k++) broken = broken.Or(planes[k + 1].AndNot(planes[k]));
        return broken;
    }

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

/// <summary>What one unary change did at the positions it was given:
/// <see cref="Taken"/> positions gained or lost a unit; <see cref="Blocked"/>
/// ones were at their limit (full when adding, empty when removing);
/// <see cref="Broken"/> ones held bits that are not a count. Blocked and broken
/// positions keep their bits.</summary>
public sealed record UnaryStep(Plane Taken, Plane Blocked, Plane Broken);
