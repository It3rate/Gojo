namespace Gojo.EncodingC;

/// <summary>Unary counts on bit planes. Bit k of a count means "more than k",
/// so adding one shifts a bit in and removing one shifts a bit out, at every
/// selected position at once. Change that arrives from outside a local
/// structure enters this way, as units, never as a direct value (canonical
/// Axiom 5).
/// <para>Bits that are not a count, such as <c>010</c>, are noise this read
/// cannot interpret. They are reported and left as they are, never shifted
/// into a count that looks valid.</para></summary>
public static class Unary
{
    /// <summary>Adds one unit at each given position. Positions already full
    /// are returned as blocked and left unchanged: the caller keeps them as
    /// overflow, so nothing is lost without trace.</summary>
    public static UnaryStep Add(Store store, Field field, Plane positions)
    {
        var planes = Load(store, field, positions);
        var broken = positions.And(BrokenIn(planes));
        var blocked = positions.And(planes[^1]).AndNot(broken);
        var take = positions.AndNot(blocked).AndNot(broken);
        for (var k = planes.Length - 1; k >= 1; k--) planes[k] = planes[k].Or(planes[k - 1].And(take));
        planes[0] = planes[0].Or(take);
        Save(store, field, planes, take);
        return new UnaryStep(take, blocked, broken);
    }

    /// <summary>Removes one unit at each given position. Positions already empty
    /// are returned as blocked and left unchanged.</summary>
    public static UnaryStep Remove(Store store, Field field, Plane positions)
    {
        var planes = Load(store, field, positions);
        var broken = positions.And(BrokenIn(planes));
        var blocked = positions.AndNot(planes[0]).AndNot(broken);
        var take = positions.AndNot(blocked).AndNot(broken);
        for (var k = 0; k < planes.Length - 1; k++) planes[k] = planes[k].And(planes[k + 1].Or(take.Not()));
        planes[^1] = planes[^1].AndNot(take);
        Save(store, field, planes, take);
        return new UnaryStep(take, blocked, broken);
    }

    /// <summary>Positions holding at least one unit. Broken counts are not read
    /// as holding anything.</summary>
    public static Plane NonZero(Store store, Field field)
    {
        var planes = Load(store, field, store.Live);
        return planes[0].AndNot(BrokenIn(planes)).And(store.Live);
    }

    /// <summary>Positions at capacity.</summary>
    public static Plane Full(Store store, Field field)
    {
        var planes = Load(store, field, store.Live);
        return planes[^1].AndNot(BrokenIn(planes)).And(store.Live);
    }

    /// <summary>Live positions whose bits are not a count: a higher bit set
    /// while a lower one is clear.</summary>
    public static Plane Broken(Store store, Field field) =>
        BrokenIn(Load(store, field, store.Live)).And(store.Live);

    /// <summary>Live positions on any <see cref="Surface"/> (including a
    /// <see cref="Workspace"/>) whose bits are not a valid unary count.</summary>
    public static Plane Broken(Surface surface, Field field)
    {
        ArgumentNullException.ThrowIfNull(surface);
        ArgumentNullException.ThrowIfNull(field);
        if (!field.Shape.IsUnary) throw new ArgumentException("A unary field is needed.", nameof(field));
        if (surface is Store store) return Broken(store, field);
        var broken = new Plane(surface.Count);
        for (var i = 0; i < surface.Count; i++)
        {
            if (!surface.IsLive(i)) continue;
            for (var k = 0; k + 1 < field.Planes.Count; k++)
                if (surface.Read(field.Planes[k + 1], i) && !surface.Read(field.Planes[k], i))
                {
                    broken[i] = true;
                    break;
                }
        }
        return broken;
    }

    /// <summary>The room left in a bounded unary count: capacity minus value,
    /// as a zero-copy <see cref="Flipped"/> view bound with its planes in
    /// reverse order. Room is anti-fill from the capacity landmark, distinct
    /// from reorientation under a shared origin. Moving a unit into a field's
    /// room removes one from the field, conserving their difference.</summary>
    public static Binding Room(Surface surface, Field field)
    {
        ArgumentNullException.ThrowIfNull(surface);
        ArgumentNullException.ThrowIfNull(field);
        if (!field.Shape.IsUnary) throw new ArgumentException("A unary field is needed.", nameof(field));
        var planes = field.Planes.ToArray();
        var reversed = new int[planes.Length];
        for (var i = 0; i < planes.Length; i++) reversed[i] = planes[planes.Length - 1 - i];
        return new Binding(new Flipped(surface, planes), reversed);
    }

    /// <summary>Offers one unit at each given position: absorbs into
    /// <paramref name="value"/> where room exists, and holds any overflow as
    /// positive material tension in <paramref name="plus"/> (<c>T+</c>) rather
    /// than discarding it. Only units that exceed both value and tension
    /// capacity are returned in <see cref="TensionEntry.Blocked"/>.</summary>
    public static TensionEntry Offer(Store store, Field value, Field plus, Plane positions)
    {
        var first = Add(store, value, positions);
        if (first.Blocked.None) return new TensionEntry(first.Taken, new Plane(positions.Length), first.Blocked, first.Broken);
        var held = Add(store, plus, first.Blocked);
        return new TensionEntry(first.Taken, held.Taken, held.Blocked, first.Broken.Or(held.Broken));
    }

    /// <summary>Draws one unit at each given position: removes from
    /// <paramref name="value"/> where supply exists, and records any unmet draw
    /// as negative material tension in <paramref name="minus"/> (<c>T−</c>).
    /// Only draws that exceed tension capacity are returned in
    /// <see cref="TensionEntry.Blocked"/>.</summary>
    public static TensionEntry Draw(Store store, Field value, Field minus, Plane positions)
    {
        var first = Remove(store, value, positions);
        if (first.Blocked.None) return new TensionEntry(first.Taken, new Plane(positions.Length), first.Blocked, first.Broken);
        var held = Add(store, minus, first.Blocked);
        return new TensionEntry(first.Taken, held.Taken, held.Blocked, first.Broken.Or(held.Broken));
    }

    private static Plane BrokenIn(Plane[] planes)
    {
        var broken = new Plane(planes[0].Length);
        for (var k = 0; k + 1 < planes.Length; k++) broken = broken.Or(planes[k + 1].AndNot(planes[k]));
        return broken;
    }

    private static Plane[] Load(Store store, Field field, Plane positions)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(positions);
        if (!field.Shape.IsUnary) throw new ArgumentException("A unary field is needed.", nameof(field));
        return field.Planes.Select(plane => store.Read(plane)).ToArray();
    }

    private static void Save(Store store, Field field, Plane[] planes, Plane mask)
    {
        for (var k = 0; k < planes.Length; k++) store.Write(field.Planes[k], planes[k], mask);
    }
}

/// <summary>What one unary change did at the positions it was given:
/// <see cref="Taken"/> positions gained or lost a unit; <see cref="Blocked"/>
/// ones were at their limit (full when adding, empty when removing);
/// <see cref="Broken"/> ones held bits that are not a count. Blocked and broken
/// positions keep their bits.</summary>
public sealed record UnaryStep(Plane Taken, Plane Blocked, Plane Broken);

/// <summary>What one tension-preserving entry or draw did:
/// <see cref="Absorbed"/> positions changed value directly; <see cref="Held"/>
/// positions retained overflow or unmet draw in their tension field;
/// <see cref="Blocked"/> positions exceeded tension capacity as well;
/// <see cref="Broken"/> positions held bits that are not a count.</summary>
public sealed record TensionEntry(Plane Absorbed, Plane Held, Plane Blocked, Plane Broken);
