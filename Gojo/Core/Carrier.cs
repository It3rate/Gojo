namespace Gojo.Core;

/// <summary>
/// Physical Gojo value site. A carrier has stable identity and unit shape,
/// mutable value/tension state, and attached relations that settle the
/// connected local world when new tension enters.
/// </summary>
public sealed partial class Carrier : IElement
{
    private static ulong idCounter;

    private Carrier? inverse;
    private readonly List<Relation> relations = [];
    private readonly IReadOnlyList<Relation> attachmentView;
    private TickRef valueTick = new(0);
    private TickRef tensionTick = new(0);
    private bool settling;

    #region Construction and Identity

    /// <summary>Creates a forward carrier with a fresh linear unit atom.</summary>
    public Carrier()
        : this(isInverse: false, unitShape: null)
    {
    }

    /// <summary>Creates a forward carrier sharing an existing stable unit shape.</summary>
    public Carrier(UnitShape unitShape)
        : this(isInverse: false, unitShape)
    {
    }

    /// <summary>Creates a carrier face with explicit flow orientation.</summary>
    public Carrier(bool isInverse)
        : this(isInverse, unitShape: null)
    {
    }

    private Carrier(bool isInverse, UnitShape? unitShape)
        : this(NextId(), isInverse, unitShape)
    {
    }

    private Carrier(ulong id, bool isInverse, UnitShape? unitShape)
    {
        Id = id;
        IsInverse = isInverse;
        attachmentView = relations.AsReadOnly();
        UnitShape = unitShape ?? UnitShape.Atom(this);
    }

    /// <summary>Wildcard carrier used by discovery-oriented frames and ports.</summary>
    public static Carrier Unknown { get; } = new(0, isInverse: false, unitShape: null);

    /// <summary>Alias for the wildcard carrier when describing unbound structure.</summary>
    public static Carrier Unbound => Unknown;

    public ulong Id { get; }

    public bool IsInverse { get; }

    public bool IsUnknown => Id == 0;

    /// <summary>Stable tick meaning for this carrier; value remains mutable.</summary>
    public UnitShape UnitShape { get; private set; }

    /// <summary>Existing opposed face, when it has already been materialized.</summary>
    internal Carrier? ExistingInverse => inverse;

    /// <summary>
    /// Read-only live view of this carrier's attachment incidences. Membership
    /// is physical relation identity, not equality of relation shape. Reading
    /// this view neither settles nor changes the connected world.
    /// </summary>
    public IReadOnlyList<Relation> AttachedRelations => attachmentView;

    // Used only while reconstructing an unpublished archive graph. Restoring
    // settled state must not replay input or run settlement a second time.
    internal void RestoreArchiveState(UnitShape unit, TickRef tick, TickRef tension,
        Carrier? opposed, IEnumerable<Relation> attachments)
    {
        if (IsUnknown) return;
        UnitShape = unit;
        valueTick = tick;
        tensionTick = tension;
        inverse = opposed;
        relations.Clear();
        relations.AddRange(attachments);
    }

    #endregion

    #region State and Slots

    public ulong Value => checked((ulong)valueTick.Ticks);

    public long Tension
    {
        get => tensionTick.Ticks;
        private set => tensionTick.Set(value);
    }

    public bool HasState => Value != 0 || Tension != 0;

    public CarrierLocation ValueSlot => new(this, valueTick);

    /// <summary>Captures current pressure as a historical coordinate observation.</summary>
    public CarrierLocation TensionSlot => new(this, Tension);

    /// <summary>Exposes the shared pressure coordinate for connected live reads.</summary>
    public CarrierLocation LiveTensionSlot => new(this, tensionTick);

    public bool TryReadCircularOrigin(out CarrierLocation origin) =>
        UnitShape.TryReadCircularSource(out origin);

    public bool TryReadLinearOrigin(out CarrierLocation origin) =>
        UnitShape.TryReadLinearSource(out origin);

    #endregion

    #region Duality and Reads

    /// <summary>Returns this carrier's stable opposed-flow face.</summary>
    public Carrier Dual() => Inverse();

    /// <summary>Gets or creates the opposite carrier face sharing this unit shape.</summary>
    public Carrier Inverse()
    {
        if (inverse is not null)
        {
            return inverse;
        }

        inverse = new Carrier(!IsInverse, UnitShape.OpposedFaceOf(UnitShape))
        {
            inverse = this
        };
        return inverse;
    }

    /// <summary>Returns this carrier's structural dual as an element.</summary>
    IElement IElement.Dual() => Dual();

    /// <summary>Compares physical carrier identity, not unit compatibility.</summary>
    public bool SameIdentityAs(Carrier other) => ReferenceEquals(this, other);

    /// <summary>Reads a location carrier as aligned or invisible from this carrier.</summary>
    public long ReadSignOf(Carrier locationCarrier)
    {
        return SameIdentityAs(locationCarrier) ? 1 : 0;
    }

    #endregion

    #region Structure and Public Input

    /// <summary>Adds positive tick tension and settles the connected world.</summary>
    public void Add(ulong ticks)
    {
        ReceiveCore((Int128)ticks);
        // Use ReceiveTogether when several arrivals belong to one input frame.
        SettleWorld();
    }

    /// <summary>Adds negative tick tension and settles the connected world.</summary>
    public void Remove(ulong ticks)
    {
        ReceiveCore(-(Int128)ticks);
        // This entry point intentionally represents a separate arrival.
        SettleWorld();
    }

    /// <summary>Feeds the delta required to make visible value approach ticks.</summary>
    public void Set(long ticks)
    {
        ReceiveCore((Int128)ticks - Value);
        // This is a visible-value delta request, not direct assignment.
        SettleWorld();
    }

    /// <summary>Feeds an unsigned target value through slot-style delta input.</summary>
    public void Set(ulong ticks) => Set(checked((long)ticks));

    /// <summary>Imports another carrier's visible value through a port relation.</summary>
    public bool TrySetFrom(Relation port, Carrier source) =>
        TrySetFrom(port, source.ValueSlot);

    /// <summary>Attaches a live read from an input structure into a concrete output port.</summary>
    public Relation AttachLiveOutput(IElement input, Relation outputPort) =>
        Attach(Relation.LiveOutput(input, outputPort));

    /// <summary>Attaches ordered additive participants to this holder carrier.</summary>
    public Relation AttachAdditive(params IElement[] participants) =>
        Attach(Relation.AdditiveGroup(this, participants));

    /// <summary>
    /// Performs one explicit import through a port and deposits its residue as
    /// tension. Each call is another import; repeating it is not an idempotent
    /// observation. Use Project for a read or AttachLiveOutput for a live level.
    /// </summary>
    public bool TrySetFrom(Relation port, CarrierLocation input)
    {
        if (!port.TryProject(input, this, out var projected) ||
            !projected.TryGetCarrierLocations(out var target, out var residue) ||
            !target.Carrier.SameIdentityAs(this))
        {
            return false;
        }

        Set(target.Ticks);
        residue.Carrier.HoldResidueTension(residue.Ticks);
        return true;
    }

    #endregion

    #region Settlement State Mutation

    /// <summary>Receives signed tension and absorbs what fits in this carrier value.</summary>
    internal void Receive(long ticks) => ReceiveCore(ticks);

    private void ReceiveCore(Int128 ticks)
    {
        if (ticks == 0) return;
        var (value, tension) = ReadReceivedState(ticks);
        SetValue(value);
        Tension = tension;
    }

    // Compute introduced tension and its local absorption before changing
    // either field. Intermediate deltas may be wide; stored state may not.
    private (ulong Value, long Tension) ReadReceivedState(Int128 ticks)
    {
        var total = (Int128)Value + Tension + ticks;
        return total >= 0 ? (checked((ulong)checked((long)total)), 0) : (0, checked((long)total));
    }

    /// <summary>
    /// Drives total locally held participation toward ticks without starting a
    /// settlement pass. Existing unresolved pressure contributes to that total.
    /// Downstream consumption can still make a live level replenish;
    /// this is not a finite arrival or a record of previously emitted output.
    /// </summary>
    internal bool ReceiveTo(long ticks)
    {
        var beforeValue = Value;
        var beforeTension = Tension;
        ReceiveCore((Int128)ticks - Value - Tension);
        return Value != beforeValue || Tension != beforeTension;
    }

    /// <summary>Sets visible value and leaves the supplied residue as tension.</summary>
    internal bool LeaveTension(ulong value, long tension)
    {
        if (Value == value && tension == 0)
        {
            return false;
        }

        SetValue(value);
        AddResidualTension(tension);
        return true;
    }

    /// <summary>Replaces unresolved tension with a remainder from routed flow.</summary>
    internal bool KeepTensionRemainder(long ticks)
    {
        if (Tension == ticks)
        {
            return false;
        }

        Tension = ticks;
        return true;
    }

    /// <summary>Cancels shared visible value between opposed carrier faces.</summary>
    internal bool BalanceOpposedValueWith(Carrier other)
    {
        var shared = Math.Min(Value, other.Value);
        if (shared == 0)
        {
            return false;
        }

        SetValue(Value - shared);
        other.SetValue(other.Value - shared);
        return true;
    }

    /// <summary>Adds unresolved signed tension without immediately absorbing it.</summary>
    internal void AddResidualTension(long ticks)
    {
        if (ticks != 0)
        {
            Tension = checked(Tension + ticks);
        }
    }

    /// <summary>Holds residue from a projection and settles the connected world.</summary>
    internal void HoldResidueTension(long ticks)
    {
        AddResidualTension(ticks);
        SettleWorld();
    }

    #endregion

    #region Diagnostics and Settlement Loop

    public override string ToString()
    {
        var orientation = IsInverse ? "-" : "+";
        return HasState
            ? $"{Id}{orientation}[{Value}|{Tension}]"
            : $"{Id}{orientation}";
    }

    /// <summary>Legacy synchronous entry point; explicit Settle returns the execution witness.</summary>
    private void SettleWorld() => RunSettlement([this], this, this, 256, observe: false);

    /// <summary>Allocates the next non-wildcard carrier identity.</summary>
    private static ulong NextId() => Interlocked.Increment(ref idCounter);

    private void SetValue(ulong value) => valueTick.Set(checked((long)value));

    #endregion
}
