namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>A closed family route with one explicitly controlled passage.
/// The gate changes permission to move, never the stored endpoint identities.
/// Its observation is supplied through a public read port. The one activation
/// is held by FamilyWalk and is conserved when the control is absent or unresolved.</summary>
public sealed class ClosurePassage
{
    private sealed class GateRead : IPrimitiveReadPort
    {
        public byte Support = 2;
        public byte ReadSupport() => Support;
    }

    private readonly GateRead gate = new();
    public FamilyTable Table { get; }
    public FamilyEntry Entry { get; }
    public FamilyUseTable Uses { get; }
    public FamilyWalk Walk { get; }
    public int ControlledUse { get; }
    public byte ControlSupport => gate.Support;
    public bool Closed => Uses.IsClosed(Table, Entry, 0);

    public ClosurePassage(int steps = 4, int? controlledEdge = null)
    {
        if (steps is < 2 or > 16) throw new ArgumentOutOfRangeException(nameof(steps));
        var edge = controlledEdge ?? steps - 1;
        if (edge < 0 || edge >= steps) throw new ArgumentOutOfRangeException(nameof(controlledEdge));
        var links = Enumerable.Range(0, steps).Select(i => new FamilyLink(i, (i + 1) % steps, 0)).ToArray();
        Table = new(new([new(0, 0)], links));
        Entry = Table.Append(1, 1);
        Uses = new(Table.Template, Enumerable.Range(0, steps).Select(i => i << 1).ToArray());
        ControlledUse = edge << 1;
        Walk = new(Table, Entry, Uses, laws: new Dictionary<int, PassageLaw>
        {
            [ControlledUse] = PassageLaw.Controlled()
        }, control: gate);
    }

    /// <summary>Public evidence about a supplied control. 01 admits 0, 10
    /// admits 1, 11 admits either, 00 supplies no observation. This is not a
    /// material transfer and does not import a hidden value into the circuit.</summary>
    public void ObserveControl(byte support)
    {
        if (support > 3) throw new ArgumentOutOfRangeException(nameof(support));
        gate.Support = support;
    }

    public PulseStep Pulse() => Walk.Pulse();
    public byte[] SourceBytes() => FamilyUseCodec.Encode(Table, Entry, Uses);
}
