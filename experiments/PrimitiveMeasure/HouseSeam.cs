namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Two boundary walks use the same house-local catalogue. A signed
/// use chooses a traversal. Cut rows and optional receiver bindings supply
/// additional reads over the original catalogue.</summary>
public sealed class HouseSeam
{
    // Published sites in this fixture: A=0, B=1, peak=2, C=3, D=4, M=5.
    private readonly OrderedBoundary catalogue;
    private readonly EndpointRows cuts = new(), cutBindings = new();
    public WalkWindows Faces { get; }
    private readonly PositionalBuckets samples = new();
    public SeamPort Roof { get; }
    public SeamPort Room { get; }

    public HouseSeam(bool shareEdge)
    {
        List<int[]> edges = [[0, 1], [1, 2], [2, 0], [0, 3], [3, 4], [4, 1]];
        if (!shareEdge) edges.Add([0, 1]);
        var roomEdge = shareEdge ? 0 : edges.Count - 1;
        catalogue = new(6, edges.ToArray(), [0, 2, 4, (roomEdge << 1) | 1, 6, 8, 10]);
        // Half-open ranges of use positions within the catalogue.
        Faces = new(catalogue, (0, 3), (3, 7));
        for (var e = 0; e < catalogue.EdgeCount; e++) samples.Append(0, 2, 2, 1);
        Roof = new(this, FaceRange(0)[0]);
        Room = new(this, FaceRange(1)[0]);
    }

    public sealed class SeamPort : IPrimitiveReadPort
    {
        private readonly HouseSeam owner;
        private readonly int position;
        internal SeamPort(HouseSeam owner, int position) { this.owner = owner; this.position = position; }
        public int SignedUse => owner.catalogue.SignedUse(position);
        public int Edge => SignedUse >> 1;
        public int[] Ends() => owner.catalogue.DirectedUse(position);
        // A missing rung leaves the two observed ends available at coarse resolution.
        public int[] Landmarks()
        {
            var ends = Ends();
            return owner.CutFor(Edge) is int site ? [ends[0], site, ends[1]] : ends;
        }
        public byte Flow(bool flipMeter = false)
        {
            if (!owner.samples.TryRead(new(0, 2, 2, Edge), out var raw))
                throw new InvalidOperationException("The seam sample must be live.");
            var word = PositionalBuckets.CoarseRead(new(0, 2, 2, Edge), raw);
            var opposed = ((SignedUse & 1) != 0) ^ flipMeter;
            return opposed ? ThreeBitMeasure.Reorient(word) : word;
        }
        public byte ReadSupport() => Gojo.Experiments.PrimitiveMeasure.ReadSupport.Of(Flow());
    }

    public void SetRoofSample(bool opposed, bool present = true)
    {
        var raw = PositionalBuckets.Encode(0, 2, 2, false, opposed, present ? 1UL : 0UL);
        if (!samples.TryWrite(new(0, 2, 2, Roof.Edge), raw))
            throw new InvalidOperationException("The roof sample must be live.");
    }

    // One optional cut per edge in this experiment. The cut is an asserted
    // interior landmark with no calibrated distance from either endpoint.
    public void AddRoofCut()
    {
        if (CutCount == 0) cuts.Append(3, Roof.Edge, 5);
    }
    public bool BindRoomToRoofCut()
    {
        if (CutCount == 0 || Room.Edge == Roof.Edge || BindingCount != 0) return false;
        cutBindings.Append(3, Room.Edge, 0); // receiving edge, cut row position
        return true;
    }
    private int? CutFor(int edge)
    {
        for (var i = 0; i < CutCount; i++)
        {
            var cut = cuts.Read(new(2, 3, i));
            if (cut[0] == edge) return cut[1];
        }
        for (var i = 0; i < BindingCount; i++)
        {
            var binding = cutBindings.Read(new(2, 3, i));
            if (binding[0] == edge) return cuts.Read(new(2, 3, binding[1]))[1];
        }
        return null;
    }

    // Incidence by edge reference, one signed occurrence per use of each face.
    // Cancellation here is a boundary read. Both face walks remain intact.
    public int[] EdgeBalance()
    {
        var counts = new int[catalogue.EdgeCount];
        for (var face = 0; face < Faces.Count; face++)
        {
            var range = FaceRange(face);
            for (var i = range[0]; i < range[1]; i++)
            {
                var use = catalogue.SignedUse(i);
                counts[use >> 1] += (use & 1) == 0 ? 1 : -1;
            }
        }
        return counts;
    }

    public int EdgeCount => catalogue.EdgeCount;
    public int CutCount => cuts.Count(2, 3);
    public int BindingCount => cutBindings.Count(2, 3);
    public int[] FaceRange(int face) => Faces.Range(face);
    public int[] EdgeEnds(int edge) => catalogue.Edge(edge);
    public int SignedUse(int position) => catalogue.SignedUse(position);
    public byte[] EdgeBytes() => catalogue.EdgeBytes();
    public byte[] UseBytes() => catalogue.UseBytes();
    public byte[] CutBytes() => cuts.PackedBytes(2, 3);
    public byte[] BindingBytes() => cutBindings.PackedBytes(2, 3);
    public byte[] SampleBytes() => samples.PackedBytes(0, 2, 2);
}
