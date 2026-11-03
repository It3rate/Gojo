namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>One cut is anchored at the reference ray. The moving cut selects a
/// nonempty proper prefix. Its complement exists only in the retained O owner.</summary>
public sealed class GlyphCut
{
    public const int Landmarks = 8;
    private static readonly ContourPattern Circle = RepetitionExamples.Contour(Landmarks, true, false);
    private readonly ContourPattern? source;
    public OrderedBoundary Walk { get; }
    public WalkWindows Windows { get; }
    public int InkCount => Windows.Range(0)[1];
    public int Start => Walk.DirectedUse(0)[0];
    public int Tip => Walk.DirectedUse(InkCount - 1)[1];
    public byte[] RadialRows() => source?.ArcBytes() ?? [];
    public bool InkCloses => Windows.IsClosed(0);

    public GlyphCut(int cut, bool reverse, bool retainCircle)
    {
        if (cut is < 1 or >= Landmarks) throw new ArgumentOutOfRangeException(nameof(cut));
        source = retainCircle ? Circle : null;
        var count = retainCircle ? Landmarks : cut;
        // End is explicitly bound to Start in this closed source. The open
        // control has only its own span rows and no radial correspondence.
        var edges = Enumerable.Range(0, count).Select(i =>
        {
            if (source == null) return new[] { Site(i), Site(i + 1) };
            var arc = source.ArcRow(i);
            return new[] { arc[0], arc[1] == ContourPattern.End ? ContourPattern.Start : arc[1] };
        }).ToArray();
        IEnumerable<int> Uses(int first, int length)
        {
            var ranks = Enumerable.Range(first, length);
            return (reverse ? ranks.Reverse() : ranks).Select(i => 2*i + (reverse ? 1 : 0));
        }
        var uses = Uses(0, cut).Concat(Uses(cut, count-cut)).ToArray();
        Walk = new OrderedBoundary(edges.Max(e => Math.Max(e[0],e[1])) + 1, edges, uses);
        Windows = retainCircle ? new(Walk, (0,cut), (cut,count), (0,count)) : new(Walk, (0,cut));
    }

    private static int Site(int ordinal) => ordinal == 0 ? ContourPattern.Start : ordinal + 4;

    // Published answer to a paid question about this owner's complete local
    // construction inventory. No query leaves both outcomes possible.
    public byte InspectReturn() => Windows.Count > 1 && Windows.IsClosed(2) ? (byte)2 : (byte)1;

    public (int[] Uses, bool Arrived) AttemptInk() => Follow(0, Start, Start);
    public (int[] Uses, bool Arrived) AttemptReturn() =>
        Windows.Count > 1 ? Follow(1, Tip, Start) : ([], false);

    private (int[] Uses, bool Arrived) Follow(int window, int site, int target)
    {
        var visited = new List<int>();
        var range = Windows.Range(window);
        for (var i = range[0]; i < range[1]; i++)
        {
            var edge = Walk.DirectedUse(i);
            if (edge[0] != site) return (visited.ToArray(), false);
            visited.Add(Walk.SignedUse(i));
            site = edge[1];
        }
        return (visited.ToArray(), site == target);
    }

    public static byte Predict(bool askAboutReturn, bool inkCloses, byte? returnReply) =>
        askAboutReturn ? returnReply ?? (byte)3 : inkCloses ? (byte)2 : (byte)1;
}

/// <summary>Try a candidate's departure and arrival roles against an observed
/// return in the same supplied writing frame. Site numbers belong to each owner.</summary>
public sealed class ReturnTrial
{
    private readonly EndpointRows bindings = new();
    public int[] Binding(int index) => bindings.Read(new(2, Bits, index));
    public byte[] BindingBytes() => bindings.PackedBytes(2, Bits);
    public int Bits { get; }
    public bool Fits { get; }
    public ReturnTrial(CurveInk observed, CurveInk proposal)
    {
        var want = observed.Probe;
        var offer = proposal.Probe;
        Bits = Math.Max(observed.Catalogue.SiteBits, proposal.Catalogue.SiteBits);
        var top = observed.Catalogue.DirectedUse(want[0])[0];
        var foot = observed.Catalogue.DirectedUse(want[1]-1)[1];
        var from = proposal.Catalogue.DirectedUse(offer[0])[0];
        var to = proposal.Catalogue.DirectedUse(offer[1]-1)[1];
        bindings.Append(Bits, from, top);
        bindings.Append(Bits, to, foot);
        var first = Binding(0);
        var last = Binding(1);
        var required = observed.Read(false);
        var offered = proposal.Read();
        Fits = (first[0] == last[0]) == (first[1] == last[1]) &&
            required.Enter == offered.Enter && required.Leave == offered.Leave;
    }
}
