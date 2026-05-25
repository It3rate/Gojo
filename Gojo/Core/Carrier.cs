namespace Gojo.Core;

/// <summary>
/// Physical Gojo value site. A carrier has stable identity and unit shape,
/// mutable value/tension state, and attached relations that settle the
/// connected local world when new tension enters.
/// </summary>
public sealed partial class Carrier : IElement
{
    private static ulong idCounter = 1;

    private Carrier? inverse;
    private readonly List<Relation> relations = [];
    private readonly TickRef valueTick = new(0);
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
    public UnitShape UnitShape { get; }

    /// <summary>Existing opposed face, when it has already been materialized.</summary>
    internal Carrier? ExistingInverse => inverse;

    /// <summary>Relations attached to this carrier's local settlement world.</summary>
    internal IReadOnlyList<Relation> AttachedRelations => relations;

    #endregion

    #region State and Slots

    public ulong Value => checked((ulong)valueTick.Ticks);

    public long Tension { get; private set; }

    public bool HasState => Value != 0 || Tension != 0;

    public CarrierLocation ValueSlot => new(this, valueTick);

    public CarrierLocation TensionSlot => new(this, Tension);

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

    /// <summary>Attaches relation structure to each carrier it references and settles.</summary>
    public Relation Attach(Relation relation)
    {
        foreach (var carrier in relation.Carriers().Where(carrier => !carrier.IsUnknown).Distinct())
        {
            if (!carrier.relations.Any(existing => existing.SameIdentityAs(relation)))
            {
                carrier.relations.Add(relation);
            }
        }

        SettleWorld();
        return relation;
    }

    /// <summary>Adds positive tick tension and settles the connected world.</summary>
    public void Add(ulong ticks)
    {
        Receive(checked((long)ticks));
        // TODO: Replace immediate quiet settlement with a time-step scheduler
        // so several perturbations can enter the same connected world before
        // relations lower tension. Sequential settlement should remain an
        // explicit higher-level operation when ordered changes matter.
        SettleWorld();
    }

    /// <summary>Adds negative tick tension and settles the connected world.</summary>
    public void Remove(ulong ticks)
    {
        Receive(checked(-checked((long)ticks)));
        // TODO: See Add. Public perturbations settle synchronously for the
        // current executable foothold, but the dynamic model wants batched
        // per-step settlement across independent structures.
        SettleWorld();
    }

    /// <summary>Feeds the delta required to make visible value approach ticks.</summary>
    public void Set(long ticks)
    {
        Receive(checked(ticks - checked((long)Value)));
        // TODO: See Add. This is slot-style delta input, not direct value
        // assignment; a future time-step scheduler should batch it with other
        // same-frame perturbations before settlement.
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

    /// <summary>Imports a visible location through a port and keeps residue as tension.</summary>
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
    internal void Receive(long ticks)
    {
        if (ticks == 0)
        {
            return;
        }

        Tension = checked(Tension + ticks);
        AbsorbTension();
    }

    /// <summary>Feeds the delta required to make visible value approach ticks without starting another settlement pass.</summary>
    internal bool ReceiveTo(long ticks)
    {
        var beforeValue = Value;
        var beforeTension = Tension;
        Receive(checked(ticks - checked((long)Value)));
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

    /// <summary>Applies attached relations until the connected carrier state quiets.</summary>
    private void SettleWorld()
    {
        if (settling)
        {
            return;
        }

        settling = true;
        try
        {
            var carriers = ConnectedCarriers();
            for (var pass = 0; pass < 256; pass++)
            {
                var before = Snapshot(carriers);
                var runtimeRouteRelations = ApplyRuntimeRouteSettlement(carriers);
                foreach (var relation in carriers.SelectMany(carrier => carrier.relations)
                    .GroupBy(relation => relation.Id)
                    .Select(group => group.First()))
                {
                    if (runtimeRouteRelations.Contains(relation.Id))
                    {
                        continue;
                    }

                    relation.Apply();
                }

                if (Snapshot(carriers).SequenceEqual(before))
                {
                    return;
                }

                carriers = ConnectedCarriers();
            }
        }
        finally
        {
            settling = false;
        }
    }

    /// <summary>Finds carriers reachable from this carrier through attached relations.</summary>
    private Carrier[] ConnectedCarriers()
    {
        var found = new HashSet<Carrier>();
        var pending = new Queue<Carrier>();
        pending.Enqueue(this);

        while (pending.Count != 0)
        {
            var carrier = pending.Dequeue();
            if (!found.Add(carrier))
            {
                continue;
            }

            foreach (var relation in carrier.relations)
            {
                foreach (var related in relation.Carriers().Where(carrier => !carrier.IsUnknown))
                {
                    pending.Enqueue(related);
                }
            }
        }

        return [.. found.OrderBy(carrier => carrier.Id)];
    }

    /// <summary>Captures value/tension state for fixed-point settlement checks.</summary>
    private static string[] Snapshot(Carrier[] carriers) => [.. carriers
        .Select(carrier => $"{carrier.Id}:{carrier.Value}:{carrier.Tension}")
        .Order()];

    /// <summary>Allocates the next non-wildcard carrier identity.</summary>
    private static ulong NextId() => idCounter++;

    /// <summary>Moves currently absorbable tension into value and leaves underflow.</summary>
    private void AbsorbTension()
    {
        if (Tension > 0)
        {
            SetValue(checked(Value + checked((ulong)Tension)));
            Tension = 0;
            return;
        }

        if (Tension == 0)
        {
            return;
        }

        var draw = checked((ulong)-Tension);
        if (Value >= draw)
        {
            SetValue(Value - draw);
            Tension = 0;
            return;
        }

        Tension = -checked((long)(draw - Value));
        SetValue(0);
    }

    private void SetValue(ulong value) => valueTick.Set(checked((long)value));

    #endregion
}
