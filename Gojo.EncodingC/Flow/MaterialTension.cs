namespace Gojo.EncodingC;

/// <summary>Builds local unary accounts and the wirings that settle them.
/// Each site holds value, incoming pressure and outstanding draw on separate
/// planes of one store. Once wired, the store and laws carry the behavior.
/// </summary>
public sealed class MaterialTension
{
    public MaterialTension(int sites, int valueCapacity, int pressureCapacity, int demandCapacity)
    {
        var value = Shape.Unary(valueCapacity);
        var pressure = Shape.Unary(pressureCapacity);
        var demand = Shape.Unary(demandCapacity);
        Value = new Field(value, [.. Enumerable.Range(0, value.Bits)]);
        Pressure = new Field(pressure, [.. Enumerable.Range(value.Bits, pressure.Bits)]);
        Demand = new Field(demand, [.. Enumerable.Range(value.Bits + pressure.Bits, demand.Bits)]);
        Sites = new Store(value.Bits + pressure.Bits + demand.Bits, sites);
    }

    public Store Sites { get; }
    public Field Value { get; }
    public Field Pressure { get; }
    public Field Demand { get; }

    /// <summary>Moves a unit into pressure. A full account leaves the offered
    /// unit at its source, including when the source is behind a port.</summary>
    public Wiring AcceptPressure(Binding source, int sourceCapacity) =>
        new(Flows.Move(sourceCapacity, Capacity(Pressure)), source, Bind(Pressure));

    /// <summary>Accepts a draw unit from an explicitly connected source of
    /// requests. A request is counted independently of supplied value.</summary>
    public Wiring AcceptDemand(Binding source, int sourceCapacity) =>
        new(Flows.Move(sourceCapacity, Capacity(Demand)), source, Bind(Demand));

    /// <summary>Moves pressure into value when there is room.</summary>
    public Wiring Absorb(Binding? gate = null) =>
        Move(Pressure, Value, Bind(Value), gate);

    /// <summary>Settles a local signed withdrawal by moving a draw unit into
    /// the room in value. Use <see cref="Deliver"/> to send actual stock onward.</summary>
    public Wiring Draw(Binding? gate = null) =>
        Move(Demand, Value, Room(Value), gate);

    /// <summary>Clears one unit on each face. Wire this only where the two
    /// accounts are allowed to settle each other in the same basis.</summary>
    public Wiring Cancel(Binding? gate = null) =>
        Move(Pressure, Demand, Room(Demand), gate);

    /// <summary>Fulfils one draw by transferring one unit of value to an
    /// outlet with room. Roles are value, demand and outlet. Stock plus
    /// delivered stock, and demand plus delivered stock, are preserved.</summary>
    public Wiring Deliver(Binding outlet, int outletCapacity) =>
        new(Flows.Fulfill(Capacity(Value), Capacity(Demand), outletCapacity), Bind(Value), Bind(Demand), outlet);

    private Wiring Move(Field from, Field to, Binding target, Binding? gate) => gate is null
        ? new Wiring(Flows.Move(Capacity(from), Capacity(to)), Bind(from), target)
        : new Wiring(Flows.GatedMove(Capacity(from), Capacity(to)), Bind(from), gate, target);

    private Binding Bind(Field field) => new(Sites, [.. field.Planes]);

    private Binding Room(Field field) => new(new Flipped(Sites, [.. field.Planes]), [.. field.Planes.Reverse()]);

    private static int Capacity(Field field) => field.Shape.Size - 1;
}
