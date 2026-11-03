namespace Gojo.Experiments.PrimitiveMeasure;

// Headings are observed in the supplied upright letter frame, clockwise from right.
public enum PenHeading { Right, DownRight, Down, DownLeft, Left, UpLeft, Up, UpRight }
public readonly record struct PenOrigin(int? RadialParts, bool HasRung, bool? ClosureGoalMet);

internal readonly record struct PenSpan(int From, int To, PenHeading Enter, PenHeading Leave,
    int? SteerWord, int? Arc = null);

public readonly record struct PenRead(int Loops, PenHeading Enter, PenHeading Leave, byte BendSupport, int? JointTurns)
{
    public bool RequiresHeadingChange => Enter != Leave;
    public bool Fits(PenRead other) => Loops == other.Loops && Enter == other.Enter && Leave == other.Leave &&
        (BendSupport & other.BendSupport) != 0 && (JointTurns == null || other.JointTurns == null || JointTurns == other.JointTurns);
}

/// <summary>Ordered participating ink, local heading observations and optional steering
/// or contour bindings. A probe reads one continuous portion of the written figure.</summary>
public sealed class CurveInk
{
    private readonly EndpointRows headings = new(), drives = new(), arcs = new(), rung = new(), goal = new(), ray = new();
    public OrderedBoundary Catalogue { get; }
    public WalkWindows Views { get; }
    private readonly ContourPattern? contourSource;
    public ContourRead? SourceRead => contourSource?.Read();
    public byte[] SourceArcBytes() => contourSource?.ArcBytes() ?? [];
    public byte[] SourceRadialBytes() => contourSource?.RadialBytes() ?? [];
    public byte[] SourceWholeBytes() => contourSource?.WholeBytes() ?? [];
    public byte[] SourceClosureBytes() => contourSource?.ClosureBytes() ?? [];
    public byte[] SourceRayBytes() => ray.PackedBytes(1, 3);
    public bool? ClockwiseContour()
    {
        if (contourSource == null) return null;
        var first = ArcLinks().Single(row => row[1] == 0)[0];
        var offset = (Heading(first)[0] - ray.Read(new(1,3,0))[0] + 8) % 8;
        return offset switch { 2 => true, 6 => false, _ => throw new InvalidOperationException("A radial tangent must cross its ray.") };
    }
    public int WireBits { get; }
    public int InkCount => Views.Range(0)[1];
    public int[] Probe => Views.Range(1);
    public int[] Heading(int edge) => headings.Read(new(2, 3, edge));
    public byte[] HeadingBytes() => headings.PackedBytes(2, 3);
    public byte[] DriveBytes() => drives.PackedBytes(2, WireBits);
    public byte[] ArcBytes() => arcs.PackedBytes(2, WireBits);
    public byte[] RungBytes() => rung.PackedBytes(2, 2);
    public int[][] RungLinks() => Rows(rung, 2);
    public byte[] GoalBytes() => goal.PackedBytes(2, WireBits);
    public int[][] DriveLinks() => Rows(drives, WireBits);
    public int[][] ArcLinks() => Rows(arcs, WireBits);
    public int[][] GoalLinks() => Rows(goal, WireBits);
    private static int[][] Rows(EndpointRows rows, int bits) => Enumerable.Range(0, rows.Count(2, bits))
        .Select(i => rows.Read(new(2, bits, i))).ToArray();

    internal CurveInk(PenSpan[] spans, int inkCount, (int First, int End) probe,
        ContourPattern? contour = null, PenHeading? referenceRay = null, int? sourceFirst = null, int? closureGoal = null)
    {
        var edges = spans.Select(s => new[] { s.From, s.To }).ToArray();
        var sites = edges.SelectMany(pair => pair).Max() + 1;
        Catalogue = new OrderedBoundary(sites, edges, Enumerable.Range(0, spans.Length).Select(i => 2 * i).ToArray());
        if (probe.End > inkCount) throw new ArgumentException("The probe must follow visible ink.");
        var ranges = new List<(int, int)> { (0, inkCount), probe };
        if (sourceFirst.HasValue)
        {
            ranges.Add((sourceFirst.Value, spans.Length));
            rung.Append(2, 2, 1); // whole source window -> retained probe window
        }
        Views = new WalkWindows(Catalogue, ranges.ToArray());
        for (var i = probe.First + 1; i < probe.End; i++)
            if (spans[i - 1].To != spans[i].From) throw new ArgumentException("A pen probe cannot jump across a lift.");
        contourSource = contour;
        if ((contour != null) != referenceRay.HasValue) throw new ArgumentException("Bind the reference ray in the writing frame.");
        if (referenceRay is PenHeading direction) ray.Append(3, (int)direction);
        WireBits = OrderedBoundary.Bits(Math.Max(4, Math.Max(sites, spans.Length)));
        var bindings = new Dictionary<int, int>();
        void Bind(int contourSite, int inkSite)
        {
            if (contourSite == ContourPattern.End) contourSite = ContourPattern.Start;
            if (bindings.TryGetValue(contourSite, out var previous) ? previous != inkSite : bindings.ContainsValue(inkSite))
                throw new ArgumentException("Contour and ink sites disagree at a shared cut.");
            bindings[contourSite] = inkSite;
        }
        for (var i = 0; i < spans.Length; i++)
        {
            var span = spans[i];
            headings.Append(3, (int)span.Enter, (int)span.Leave);
            if (span.SteerWord is int word) drives.Append(WireBits, i, word);
            if (span.Arc is int arc)
            {
                if (contour == null || !contour.Read().Circle) throw new ArgumentException("Supply a participating closed radial source.");
                var row = contour.ArcRow(arc);
                Bind(row[0], span.From); Bind(row[1], span.To);
                arcs.Append(WireBits, i, arc);
            }
        }
        _ = ClockwiseContour();
        if (closureGoal is int target)
        {
            if ((uint)target >= sites) throw new ArgumentOutOfRangeException(nameof(closureGoal));
            goal.Append(WireBits, inkCount - 1, target);
        }
    }

    public bool? ClosureGoalMet()
    {
        var links = GoalLinks();
        return links.Length == 0 ? null : Catalogue.Edge(links[0][0])[1] == links[0][1];
    }
    public PenOrigin ReadOrigin() => new(contourSource?.PartCount, rung.Count(2,2) != 0, ClosureGoalMet());

    // The two ends of the probed return attach to opposite ends of one straight
    // stem. Quarter turns refer to the supplied eight-heading letter frame.
    public int? CrosswiseStem()
    {
        var route = Probe;
        int top = Catalogue.Edge(route[0])[0], foot = Catalogue.Edge(route[1] - 1)[1];
        var enter = Heading(route[0])[0];
        var leave = Heading(route[1] - 1)[1];
        if ((enter + 4) % 8 != leave) return null;
        foreach (var row in DriveLinks())
        {
            var stem = row[0];
            if (row[1] != 0 || stem >= InkCount || (stem >= route[0] && stem < route[1])) continue;
            var ends = Catalogue.Edge(stem);
            if (ends[0] == foot && ends[1] == top && (enter - Heading(stem)[0] + 8) % 4 == 2) return stem;
        }
        return null;
    }

    public PenRead Read(bool interior = true)
    {
        var ink = Catalogue.Read(InkCount);
        var loops = InkCount - ink.Balance.Count + ink.VisitedComponents;
        var probe = Probe;
        var entry = (PenHeading)Heading(probe[0])[0];
        var exit = (PenHeading)Heading(probe[1] - 1)[1];
        if (!interior) return new(loops, entry, exit, 3, null);
        byte bend = 1;
        var switches = 0;
        var steering = DriveLinks().ToDictionary(row => row[0], row => row[1]);
        var radial = ArcLinks().Select(row => row[0]).ToHashSet();
        for (var i = probe[0]; i < probe[1]; i++)
        {
            if (i > probe[0] && Heading(i - 1)[1] != Heading(i)[0]) switches++;
            if (radial.Contains(i) || (steering.TryGetValue(i, out var word) && (word & 1) != 0)) bend = 2;
            else if (bend == 1 && !steering.ContainsKey(i)) bend = 3;
        }
        return new(loops, entry, exit, bend, bend == 3 ? null : switches);
    }
}
