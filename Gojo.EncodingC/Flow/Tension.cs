namespace Gojo.EncodingC;

/// <summary>Material tension at the positions of a store: change that has
/// arrived and not been absorbed, and draws that have not been paid
/// (docs/GojoTension-2026-11.md, §3.1 and §4).
/// <para><b>Two faces, kept apart.</b> Beside a unary value, <see cref="Plus"/>
/// holds units that arrived with no place yet (pressure) and
/// <see cref="Minus"/> holds draws not yet paid (debt). Each is a unary count:
/// more tension is more bits set. They are never netted by default. A unit of
/// each came from different participation, and cancelling them is a law,
/// wired only where the structure says they are the same quantity.</para>
/// <para><b>Change enters as tension</b> (canonical Axiom 5). What arrives from
/// outside is <see cref="Receive"/>d into <see cref="Plus"/> or
/// <see cref="Demand"/>ed into <see cref="Minus"/>, never written into the
/// value. The structure's own laws then settle it.</para>
/// <para><b>One law settles all of it.</b> Absorbing, drawing and cancelling are
/// each the existing one-unit move (<see cref="Flows.Move"/>), into the value
/// or into the room of a count (<see cref="Field.Room"/>). Each conserves by
/// the move's own accounting: value plus pressure, value minus debt, pressure
/// minus debt. A full value leaves pressure waiting, and an empty one leaves
/// debt unpaid. Both are results, read where laws can read them.</para>
/// <para><b>Route choice.</b> Where absorbing and drawing, or any two moves,
/// would take from one count in one step taken together, they contend. Each
/// move can take a gate: it runs only while its gate bit is 1. Gates from an
/// open choice (<see cref="Supports.Possible"/>) leave every possible route
/// open, so contention is exactly an open choice, and narrowing settles it.
/// Gates from a settled code, such as a turn bit and its flipped view, open
/// one route at a time.</para>
/// <para>This class only lays the planes out and wires the moves. Everything
/// it makes is ordinary planes and wirings, saved and forked like any
/// other.</para></summary>
public sealed class Tension
{
    /// <summary>Adds pressure and debt counts of <paramref name="capacity"/>
    /// units beside a unary <paramref name="value"/> in a store.</summary>
    public Tension(Store store, Field value, int capacity)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(value);
        if (!value.Shape.IsUnary) throw new ArgumentException("Material is a unary count.", nameof(value));
        if (value.Planes.Any(p => (uint)p >= (uint)store.PlaneCount || store.IsConstantPlane(p)))
            throw new ArgumentException("The value's planes are planes of this store that differ by position.", nameof(value));
        if (capacity is < 1 or > 63) throw new ArgumentOutOfRangeException(nameof(capacity));
        Store = store;
        Value = value;
        Plus = new Field(Shape.Unary(capacity), [.. Enumerable.Range(0, capacity).Select(_ => store.AddPlane())]);
        Minus = new Field(Shape.Unary(capacity), [.. Enumerable.Range(0, capacity).Select(_ => store.AddPlane())]);
    }

    public Store Store { get; }

    /// <summary>The material held: what has been absorbed.</summary>
    public Field Value { get; }

    /// <summary>Pressure: units that arrived and have not been absorbed.</summary>
    public Field Plus { get; }

    /// <summary>Debt: draws that have not been paid.</summary>
    public Field Minus { get; }

    /// <summary>One unit arrives at each given position, as pressure. Where
    /// pressure is at capacity the unit is returned as blocked.</summary>
    public UnaryStep Receive(Plane positions) => Unary.Add(Store, Plus, positions);

    /// <summary>One unit is drawn at each given position, as debt. Where debt is
    /// at capacity the draw is returned as blocked.</summary>
    public UnaryStep Demand(Plane positions) => Unary.Add(Store, Minus, positions);

    /// <summary>Moves pressure into the value, one unit a step, while there is room.</summary>
    public Wiring Absorb(Plane positions, params Binding[] gates) => Route(Plus.On(Store), Value.On(Store), positions, gates);

    /// <summary>Pays debt from the value, one unit a step, while there is value:
    /// a move from debt into the value's room.</summary>
    public Wiring Draw(Plane positions, params Binding[] gates) => Route(Minus.On(Store), Value.Room(Store), positions, gates);

    /// <summary>Cancels a unit of pressure against a unit of debt, one a step: a
    /// move from pressure into the room of debt. Wire it only where the two are
    /// the same quantity.</summary>
    public Wiring Cancel(Plane positions, params Binding[] gates) => Route(Plus.On(Store), Minus.Room(Store), positions, gates);

    /// <summary>Pays debt by sending a unit of value on to an outlet, such as
    /// a customer's pickup counter, one a step, while there is value, debt and
    /// room at the outlet (<see cref="Flows.Fulfill"/>). Unlike
    /// <see cref="Draw"/>, the unit is not gone: it is at the outlet, where its
    /// receiver can take it through a port.</summary>
    public Wiring Deliver(Binding outlet, Plane positions, params Binding[] gates) =>
        Wire(Flows.Fulfill(Value.Planes.Count, Minus.Planes.Count, outlet.Planes.Count), [Value.On(Store), Minus.On(Store), outlet], positions, gates);

    /// <summary>The one-unit move from one count to another at each position,
    /// waiting on any <paramref name="gates"/>: a route. Its source can be a
    /// port, so units arrive from another structure as pressure or as debt.</summary>
    public static Wiring Route(Binding from, Binding to, Plane positions, params Binding[] gates) =>
        Route(Flows.Move(from.Planes.Count, to.Planes.Count), from, to, positions, gates);

    /// <summary>A two-role move law, such as a conversion that takes one case
    /// and gives several items, from one count to another at each position.</summary>
    public static Wiring Route(TransitionLaw move, Binding from, Binding to, Plane positions, params Binding[] gates) =>
        Wire(move, [from, to], positions, gates);

    /// <summary>Any law, its roles bound at the same position of each surface,
    /// one row per position, running only while every gate bit at that
    /// position is 1 (see <see cref="Flows.When"/>).</summary>
    public static Wiring Wire(TransitionLaw law, Binding[] roles, Plane positions, params Binding[] gates)
    {
        ArgumentNullException.ThrowIfNull(law);
        ArgumentNullException.ThrowIfNull(roles);
        ArgumentNullException.ThrowIfNull(positions);
        ArgumentNullException.ThrowIfNull(gates);
        var wiring = gates.Length == 0 ? new Wiring(law, roles) : new Wiring(Flows.When(law, gates.Length), [.. roles, .. gates]);
        foreach (var position in positions.Indexes()) wiring.Add([.. Enumerable.Repeat(position, roles.Length + gates.Length)]);
        return wiring;
    }
}
