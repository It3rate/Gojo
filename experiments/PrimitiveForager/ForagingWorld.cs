using System.Runtime.CompilerServices;
using Gojo.ConceptSurface;
using Gojo.Core;
using Gojo.Structures;

namespace PrimitiveForager;

/// <summary>External physics and opaque instrument services. The world owns
/// position, light, charge, heat and outcomes; it never writes brain state.</summary>
public sealed class ForagingWorld
{
    public const int Cells = 31;
    public const int Maximum = 100;
    private readonly int seed;
    private readonly bool mirrored;
    private readonly int privateEncoding;
    private readonly List<Phase> phases = [];
    private readonly ConditionalWeakTable<Relation, Reading> readings = new();
    private double queryRemainder;
    private bool cycleArmed;

    public int X { get; private set; } = 15;
    public int Charge { get; private set; } = 60;
    public int Heat { get; private set; }
    public int Cycles { get; private set; }
    public int StarvationTicks { get; private set; }
    public int OverheatTicks { get; private set; }
    public int Movements { get; private set; }
    public long ChargeSum { get; private set; }
    public int ComparisonQueries { get; private set; }
    public int SpatialQueries { get; private set; }
    public int FineBodyReads { get; private set; }
    public int Acquisitions { get; private set; }
    public double TotalQueryCost { get; private set; }
    public double QueryDebtRemainder => queryRemainder;

    public ForagingWorld(int seed, bool mirrored = false, int privateEncoding = 0)
    {
        if (privateEncoding is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(privateEncoding));
        this.seed = seed; this.mirrored = mirrored; this.privateEncoding = privateEncoding;
    }

    public int PeakX(int tick) => At(tick).Peak;
    public bool IsCloud(int tick)
    {
        var phase = At(tick);
        var local = tick - phase.Start;
        return local >= phase.CloudStart && local < phase.CloudStart + 6;
    }
    public bool IsDark(int tick)
    {
        var phase = At(tick);
        var local = tick - phase.Start;
        return local is >= 92 and < 100;
    }
    public bool IsUnavailable(int tick) => tick % 211 is >= 51 and <= 53;

    /// <summary>Known absence occurs only while the source is actually dark.
    /// Instrument downtime leaves presence open instead of manufacturing zero.
    /// The returned event carries no magnitude coordinate.</summary>
    public Relation Acquire(Carrier source, Carrier presenceAxis, int tick, int offset = 0)
    {
        Acquisitions++;
        if (IsUnavailable(tick))
        {
            var frame = Relation.Segment(presenceAxis, 0, 1);
            return ReadWitness.Create(new Carrier(), source, frame,
                Group(new Placeholder(), new Placeholder()), new Relation(source, frame), source);
        }
        var present = !IsDark(tick);
        var sample = SamplingSurface.Sample(new Carrier(), source, presenceAxis, present);
        readings.Add(sample, new Reading(source, presenceAxis, present, Encode(LightAt(X + offset, tick))));
        return sample;
    }

    /// <summary>Requested candidate-versus-anchor order only. Weak event keys
    /// preserve private readings while native references retain an acquisition;
    /// forgotten events do not leave a permanent world-side value dictionary.</summary>
    public Relation Compare(Relation current, Relation anchor, Carrier orderAxis)
    {
        ComparisonQueries++;
        if (Comparable(current, anchor, out var a, out var b) && !orderAxis.IsUnknown)
            return SamplingSurface.Comparison(new Carrier(), current, anchor, orderAxis, a.Value.CompareTo(b.Value));
        return OpenComparison(current, anchor, orderAxis);
    }

    /// <summary>The electrical eye order can be mirrored. Its witness retains
    /// the actual ordered acquisition pair; the separate exposed Orientation
    /// calibration maps the sensor sign to body-relative right-minus-left.</summary>
    public Relation Spatial(Carrier source, Carrier presenceAxis, Carrier orderAxis, int tick)
    {
        SpatialQueries++;
        var left = Acquire(source, presenceAxis, tick, -1);
        var right = Acquire(source, presenceAxis, tick, 1);
        var candidate = mirrored ? left : right;
        var anchor = mirrored ? right : left;
        if (Comparable(candidate, anchor, out var a, out var b) && !orderAxis.IsUnknown)
            return SamplingSurface.Comparison(new Carrier(), candidate, anchor, orderAxis, a.Value.CompareTo(b.Value));
        return OpenComparison(candidate, anchor, orderAxis);
    }

    public Relation Orientation(Carrier sensorAxis, Carrier bodyAxis) =>
        new(Relation.Segment(sensorAxis, -1, 1), Relation.Segment(bodyAxis, mirrored ? 1 : -1, mirrored ? -1 : 1));

    public Relation Energy(Relation partition) =>
        CoarseReadSurface.Measure(new Carrier(), partition, Charge, new Placeholder());
    public Relation HeatRead(Relation partition) =>
        CoarseReadSurface.Measure(new Carrier(), partition, Heat, new Placeholder());
    public Relation FineEnergy(Relation capacity) { FineBodyReads++; return Fine(capacity, Charge); }
    public Relation FineHeat(Relation capacity) { FineBodyReads++; return Fine(capacity, Heat); }

    /// <summary>Observations precede this action's fees. Fractional fees are
    /// accumulated in the external resource ledger; completed integer units
    /// debit charge and the unpaid subunit remainder persists.</summary>
    public void Advance(int tick, int action, double queryCost)
    {
        if (action is < -1 or > 1) throw new ArgumentOutOfRangeException(nameof(action));
        if (!double.IsFinite(queryCost) || queryCost < 0) throw new ArgumentOutOfRangeException(nameof(queryCost));
        var before = X;
        var beforeCharge = Charge;
        X = Math.Clamp(X + action, 0, Cells - 1);
        if (X != before) Movements++;
        var distance = Math.Abs(X - PeakX(tick));
        var near = distance <= 2 && !IsDark(tick);
        var income = IsDark(tick) ? -1 : distance switch { 0 => 9, 1 => 6, 2 => 3, _ => -1 };
        if (IsCloud(tick) && income > 0) income /= 3;
        Heat = Math.Clamp(Heat + (near ? IsCloud(tick) ? 2 : 5 : -4), 0, Maximum);
        TotalQueryCost += queryCost;
        queryRemainder += queryCost;
        var wholeCost = Math.Floor(queryRemainder + 1e-12);
        queryRemainder = Math.Max(0, queryRemainder - wholeCost);
        // The physical resource is bounded; a fee larger than all available
        // charge has the same external outcome as exhaustion. No brain value
        // is assigned and no hidden finer native number is published.
        var balance = Charge + income - (X != before ? 1 : 0) - (Heat >= 90 ? 3 : 0) - wholeCost;
        Charge = balance <= 0 ? 0 : balance >= Maximum ? Maximum : (int)balance;
        if (Charge <= 5) StarvationTicks++;
        if (Heat >= 90) OverheatTicks++;
        ChargeSum += Charge;
        // A feeding bout must newly cross the target. Remaining above80 in
        // the dark does not re-arm and count another cycle every tick.
        if (beforeCharge < 80 && Charge >= 80 && near) cycleArmed = true;
        if (cycleArmed && Heat <= 20 && distance >= 3 && Charge >= 20) { Cycles++; cycleArmed = false; }
    }

    /// <summary>Observer-only reference for a hidden plot. Never use it as a
    /// controller publication; Acquire/Compare/Spatial are the light services.</summary>
    public int DisplayLight(int tick, int offset = 0) => Encode(LightAt(X + offset, tick));

    public static string[] WorldRules =>
    [
        "31 cells; start at 15, charge 60, heat 0. Seeded patch locations 4..26 change after 120..180 ticks.",
        "Private light is 20 + max(0, 100−4×distance). Each phase has a 6-tick cloud at its seeded local 34..44 start, reducing the directional field to 3/4.",
        "The source is dark during local ticks 92..99. This publishes known absence; photosynthesis stops and heat cools. Light-instrument downtime at ticks modulo 211 in 51..53 leaves presence unavailable.",
        "Visible integer charge receives +9/+6/+3 at patch distances 0/1/2, or −1 elsewhere. Clouds divide positive intake by 3. Actual movement costs 1 charge.",
        "Heat rises 5 within 2 cells of an active patch, or 2 during clouds; otherwise it cools 4. Heat 90 or greater costs 3 additional charge.",
        "The runner supplies query fees. Their fractional remainder persists; completed charge units are debited after observations with the chosen action.",
        "A cycle begins only by newly crossing from charge below 80 to at least 80 near active food, then completes at heat ≤20 at least 3 cells from the patch with charge ≥20. Staying above 80 does not re-arm a cycle. Cycles, starvation (charge ≤5), and overheating (heat ≥90) are external outcomes only.",
        "Opaque light services expose presence/order only. Physiological services publish into the receiver-provided partition/capacity, without a wider numeric rescue or a write to controller memory."
    ];
    public static string[] PrivateEncodingDescriptions =>
    [
        "0: private raw field x (nonnegative)",
        "1: 7x + 1000, a strictly increasing private scale/offset",
        "2: x² + 17, strictly increasing over the chosen nonnegative private field"
    ];

    private int LightAt(int x, int tick)
    {
        if (IsDark(tick)) return 0;
        var directional = Math.Max(0, 100 - 4 * Math.Abs(x - PeakX(tick)));
        if (IsCloud(tick)) directional = directional * 3 / 4;
        return 20 + directional;
    }
    private int Encode(int value) => privateEncoding switch
    {
        0 => value,
        1 => checked(7 * value + 1000),
        _ => checked(value * value + 17)
    };
    private bool Comparable(Relation current, Relation anchor, out Reading a, out Reading b)
    {
        if (readings.TryGetValue(current, out a!) && readings.TryGetValue(anchor, out b!) && a.Present && b.Present &&
            ReferenceEquals(a.Source, b.Source) && a.PresenceAxis.SameIdentityAs(b.PresenceAxis) &&
            SamplingSurface.TryReadSample(current, out var currentPresence, out _) && currentPresence.Ticks == 1 &&
            SamplingSurface.TryReadSample(anchor, out var anchorPresence, out _) && anchorPresence.Ticks == 1 &&
            currentPresence.Carrier.SameIdentityAs(a.PresenceAxis) && anchorPresence.Carrier.SameIdentityAs(b.PresenceAxis)) return true;
        a = b = null!; return false;
    }
    private static Relation OpenComparison(Relation current, Relation anchor, Carrier orderAxis)
    {
        var pair = Group(current, anchor);
        var frame = new Relation(pair, Relation.Segment(orderAxis, -1, 1));
        return ReadWitness.Create(new Carrier(), pair, frame, new Placeholder(), new Relation(pair, frame), pair);
    }
    private static Relation Fine(Relation capacity, int value)
    {
        var axis = BoundedWholeSurface.TryRead(capacity, out var whole, out _, out _, out _) && whole is Carrier carrier ? carrier : Carrier.Unknown;
        return FiniteReadSurface.Publish(new Carrier(), capacity, new CarrierLocation(axis, value), new Placeholder());
    }
    private Phase At(int tick)
    {
        if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
        while (phases.Count == 0 || tick >= phases[^1].End)
        {
            var index = phases.Count;
            var start = index == 0 ? 0 : phases[^1].End;
            var mix = Mix(index);
            var peak = 4 + (int)(mix % 23);
            if (index != 0 && Math.Abs(peak - phases[^1].Peak) < 5) peak = peak < 15 ? Math.Min(26, peak + 10) : Math.Max(4, peak - 10);
            phases.Add(new(start, checked(start + 120 + (int)(mix / 23 % 61)), peak, 34 + (int)(mix / 1403 % 11)));
        }
        var low = 0; var high = phases.Count - 1;
        while (low < high)
        {
            var middle = low + (high - low) / 2;
            if (tick >= phases[middle].End) low = middle + 1; else high = middle;
        }
        return phases[low];
    }
    private uint Mix(int phase)
    {
        var value = unchecked((uint)seed * 747796405u + (uint)phase * 2891336453u + 277803737u);
        value = unchecked((value ^ (value >> 16)) * 2246822519u);
        return value ^ (value >> 13);
    }
    private static Relation Group(params IElement[] elements) => Relation.OrderedGroup(new Carrier(), elements);
    private sealed record Reading(Carrier Source, Carrier PresenceAxis, bool Present, int Value);
    private sealed record Phase(int Start, int End, int Peak, int CloudStart);
}
