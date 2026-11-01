namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>A selected homogeneous Euclidean planar radial-path contract. Each arc
/// maintains the same reach from the centre THROUGHOUT the arc. The ordered
/// parts cover one whole traversal, whose end is explicitly bound to its start.
/// These are declared constraints, not inferred field dynamics or coordinates.
/// Tables use fixed roles; no Circle/Pentagon tag participates in the read.</summary>
public sealed class ContourPattern
{
    // Local reference layout: centre 0, reference 1, unwrapped end 2,
    // radius read 3, whole traversal 4; intermediate sites and part slots follow.
    public const int Centre = 0, Start = 1, End = 2, Radius = 3, Whole = 4;
    private readonly EndpointRows arcs = new();
    private readonly EndpointRows radial = new();
    private readonly EndpointRows closure = new();
    private readonly EndpointRows whole = new();
    private readonly EndpointRows chords = new();
    private readonly IPrimitiveReadPort reach;
    private readonly IPrimitiveReadPort travel;
    public int ReferenceBits { get; }
    public int PartCount => arcs.Count(3, ReferenceBits);

    public ContourPattern(IPrimitiveReadPort reach, IPrimitiveReadPort travel, int referenceBits = 6)
    {
        ArgumentNullException.ThrowIfNull(reach);
        ArgumentNullException.ThrowIfNull(travel);
        if (referenceBits is < 3 or > 16) throw new ArgumentOutOfRangeException(nameof(referenceBits));
        this.reach = reach;
        this.travel = travel;
        ReferenceBits = referenceBits;
        radial.Append(referenceBits, Centre, Start, Radius);
        whole.Append(referenceBits, Start, End, Whole);
    }

    /// <summary>Part is a reference to an unknown positive share of this whole.
    /// Reusing it promises equal shares. Different part references leave their
    /// relative extent unresolved. All shares sum to the selected whole.</summary>
    public void Arc(int from, int to, int part)
    {
        if (from == to || !Site(from) || !Site(to) || part < 5 || part >= 1 << ReferenceBits)
            throw new ArgumentException("Use distinct contour sites and a local part slot.");
        // A role cannot simultaneously be a contour site and a part parameter.
        var existing = Enumerable.Range(0, PartCount).Select(ArcRow).ToArray();
        if (part == from || part == to || existing.Any(r =>
                r[2] == from || r[2] == to || r[0] == part || r[1] == part))
            throw new ArgumentException("Site and part slots must be distinct.");
        arcs.Append(ReferenceBits, from, to, part);
        radial.Append(ReferenceBits, Centre, to, Radius);
    }

    public void Close(int target = Start)
    {
        if (!Site(target)) throw new ArgumentOutOfRangeException(nameof(target));
        closure.Append(ReferenceBits, End, target);
    }

    public void Chord(int from, int to)
    {
        if (from == to || !Site(from) || !Site(to)) throw new ArgumentException("Use distinct contour sites.");
        chords.Append(ReferenceBits, from, to);
    }

    public ContourRead Read()
    {
        var rows = Enumerable.Range(0, PartCount).Select(ArcRow).ToArray();
        var ordered = rows.Length > 0 && rows[0][0] == Start && rows[^1][1] == End &&
            Enumerable.Range(1, rows.Length - 1).All(i => rows[i - 1][1] == rows[i][0]) &&
            rows.Select(r => r[0]).Distinct().Count() == rows.Length && rows.All(r => r[0] != End) &&
            rows.All(r => r[1] != Start);
        var closed = closure.Count(2, ReferenceBits) == 1 &&
            closure.Read(new(2, ReferenceBits, 0)).SequenceEqual(new[] { End, Start });
        byte? presence = null;
        if (ordered && closed)
        {
            var radius = reach.ReadSupport();
            var traversal = travel.ReadSupport();
            if ((radius & ~3) != 0 || (traversal & ~3) != 0)
                throw new InvalidOperationException("Use positive-context zero/nonzero observations.");
            presence = radius == 0 || traversal == 0 ? (byte)0 :
                (byte)(((radius & 1) != 0 || (traversal & 1) != 0 ? 1 : 0) |
                       ((radius & 2) != 0 && (traversal & 2) != 0 ? 2 : 0));
        }
        var joined = ordered && closed && rows.Length >= 3 && chords.Count(2, ReferenceBits) == rows.Length &&
            Enumerable.Range(0, rows.Length).All(i => chords.Read(new(2, ReferenceBits, i))
                .SequenceEqual(new[] { rows[i][0], rows[i][1] == End ? Start : rows[i][1] }));
        var equal = rows.Length > 0 && rows.Select(r => r[2]).Distinct().Count() == 1;
        var opposedPairs = rows.Length == 4 && rows[0][2] == rows[2][2] && rows[1][2] == rows[3][2];
        return new(presence, PartCount, equal, joined, joined && equal && presence == 2,
            joined && opposedPairs && presence == 2);
    }

    public int[] ArcRow(int index) => arcs.Read(new(3, ReferenceBits, index));
    public byte[] ArcBytes() => arcs.PackedBytes(3, ReferenceBits);
    public byte[] RadialBytes() => radial.PackedBytes(3, ReferenceBits);
    public byte[] WholeBytes() => whole.PackedBytes(3, ReferenceBits);
    public byte[] ClosureBytes() => closure.PackedBytes(2, ReferenceBits);
    public byte[] ChordBytes() => chords.PackedBytes(2, ReferenceBits);

    private bool Site(int site) => (site is Start or End || site >= 5) && site < 1 << ReferenceBits;
}

public readonly record struct ContourRead(byte? NondegeneracySupport, int Parts, bool EqualParts,
    bool JoinedChords, bool RegularPolygon, bool Rectangle)
{
    public bool Circle => NondegeneracySupport == 2;
    public bool Square => RegularPolygon && Parts == 4;
}
