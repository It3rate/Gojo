namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Material interfaces explicitly exported by the two owners. A
/// PortConnection remains an observation. Positions follow local port bindings.</summary>
public sealed partial class PortStructure
{
    public sealed class Outlet
    {
        internal PortStructure Owner { get; }
        public int Position { get; }
        internal Outlet(PortStructure owner, int port) { Owner = owner; Position = port; }
    }

    public sealed class Intake
    {
        internal PortStructure Owner { get; }
        public int Gate { get; }
        public int Target { get; }
        internal Intake(PortStructure owner, int gate, int target) { Owner = owner; Gate = gate; Target = target; }
    }

    public Outlet ExportOutlet(int port)
    {
        RequirePassagePorts([port]);
        return new(this, port);
    }

    public Intake ExportIntake(int gatePort, int targetPort)
    {
        lock (gate)
        {
            if (!TryPassageAddresses([gatePort, targetPort], out _))
                throw new ArgumentException("An intake needs distinct compatible gate and material sites.");
            return new(this, gatePort, targetPort);
        }
    }

    internal static HandoffRead Settle(Outlet outlet, Intake intake, PassageLaw law)
    {
        var sender = outlet.Owner;
        var receiver = intake.Owner;
        if (ReferenceEquals(sender, receiver)) throw new ArgumentException("Use a local passage within one owner.");
        lock (sender.gate)
        {
            // Opposite transfers need no global owner order. A busy receiver
            // defers this attempt with neither owner changed.
            if (!Monitor.TryEnter(receiver.gate)) return default;
            try
            {
                if (!sender.TryPassageAddresses([outlet.Position], out var source) ||
                    !receiver.TryPassageAddresses([intake.Gate, intake.Target], out var targets) ||
                    source[0].OrientationGroup != targets[0].OrientationGroup ||
                    source[0].DirectionGroup != targets[0].DirectionGroup) return default;
                var before = (byte)(sender.ReadLive(source[0]) | receiver.ReadLive(targets[0]) << 1 | receiver.ReadLive(targets[1]) << 2);
                if (!ReadSupport.TrySingle(law.Predict((byte)(1 << before)), out var after)) return default;
                // Both targets were validated under these same locks. No
                // callbacks or allocations between the paired writes.
                sender.storage.TryWrite(source[0], (ulong)(after & 1));
                receiver.storage.TryWrite(targets[1], (ulong)((after >> 2) & 1));
                return new(before, after);
            }
            finally { Monitor.Exit(receiver.gate); }
        }
    }
}

/// <summary>Joint S,C,T state numbers. An unavailable read has no before/after
/// pair. This witness is produced after settlement and has no outgoing control.</summary>
public readonly record struct HandoffRead(byte? Before, byte? After)
{
    public bool Offered => Before is byte before && (before & 1) != 0;
    public bool Moved => Offered && After is byte after && (after & 1) == 0;
}

public readonly record struct SeamStep(int Outlet, int Intake, HandoffRead Transfer);

public sealed class PortSeams
{
    private readonly PortStructure.Outlet[] outlets;
    private readonly PortStructure.Intake[] intakes;
    private readonly PassageLaw law;
    private readonly EndpointRows wiring = new();
    private SeamStep[] last = [];
    private readonly int bits;
    public PortSeams(PortStructure.Outlet[] outlets, PortStructure.Intake[] intakes, PassageLaw law)
    {
        this.outlets = outlets.ToArray();
        this.intakes = intakes.ToArray();
        this.law = law;
        bits = OrderedBoundary.Bits(Math.Max(outlets.Length, intakes.Length));
    }
    public void Link(int outlet, int intake)
    {
        if (ReferenceEquals(outlets[outlet].Owner, intakes[intake].Owner)) throw new ArgumentException("Expected two owners.");
        wiring.Append(bits, outlet, intake);
    }
    public byte[] RowBytes() => wiring.PackedBytes(2, bits);
    public byte[] LawBytes() => law.PackedBytes();
    public int Count => wiring.Count(2, bits);

    /// <summary>One-way observation of a committed before/after pair. The bit
    /// at before + 8*after selects an event. A deferred operation supplies
    /// no observation. Sampling this port cannot execute the seam.</summary>
    public IPrimitiveReadPort ObserveTransition(int row, byte[] bitmap)
    {
        if ((uint)row >= (uint)Count || bitmap.Length != 8) throw new ArgumentException("Expected an existing row and a 64-bit event mask.");
        return new TransitionPort(this, row, bitmap.ToArray());
    }
    private sealed class TransitionPort(PortSeams seams, int row, byte[] bits) : IPrimitiveReadPort
    {
        public byte ReadSupport()
        {
            if (row >= seams.last.Length) return 0;
            var read = seams.last[row].Transfer;
            if (read.Before is not byte before || read.After is not byte after) return 0;
            return (bits[after] & (1 << before)) != 0 ? (byte)2 : (byte)1;
        }
    }
    public SeamStep[] Pulse()
    {
        var reads = new SeamStep[wiring.Count(2, bits)];
        for (var i = 0; i < reads.Length; i++)
        {
            var row = wiring.Read(new(2, bits, i));
            reads[i] = new(row[0], row[1], PortStructure.Settle(outlets[row[0]], intakes[row[1]], law));
        }
        last = reads;
        return reads.ToArray();
    }
}
