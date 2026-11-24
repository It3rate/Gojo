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
    public Wiring Absorb(Plane positions, Binding? gate = null) => Moves(Plus.On(Store), Value.On(Store), positions, gate);

    /// <summary>Pays debt from the value, one unit a step, while there is value:
    /// a move from debt into the value's room.</summary>
    public Wiring Draw(Plane positions, Binding? gate = null) => Moves(Minus.On(Store), Value.Room(Store), positions, gate);

    /// <summary>Cancels a unit of pressure against a unit of debt, one a step: a
    /// move from pressure into the room of debt. Wire it only where the two are
    /// the same quantity.</summary>
    public Wiring Cancel(Plane positions, Binding? gate = null) => Moves(Plus.On(Store), Minus.Room(Store), positions, gate);

    /// <summary>The one-unit move at each position, or, with a
    /// <paramref name="gate"/>, the move that runs only while the gate's bit
    /// at that position is 1: a route choice (see <see cref="Supports.Possible"/>).</summary>
    internal static Wiring Moves(Binding from, Binding to, Plane positions, Binding? gate = null)
    {
        ArgumentNullException.ThrowIfNull(positions);
        var (source, target) = (from.Planes.Count, to.Planes.Count);
        var wiring = gate is null
            ? new Wiring(Flows.Move(source, target), from, to)
            : new Wiring(Flows.GatedMove(source, target), from, gate, to);
        foreach (var position in positions.Indexes())
            if (gate is null) wiring.Add(position, position);
            else wiring.Add(position, position, position);
        return wiring;
    }
}
