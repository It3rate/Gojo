namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>A selected route in one single-threaded owner. Pair rows describe independent
/// steps. A unary table describes successive vertices, so consecutive steps share
/// the actual vertex field. The same cut-pair encoding selects runs in either table.</summary>
public sealed class LocalPath
{
    private EndpointRows topology = new();
    private EndpointRows runs = new();
    private readonly EndpointRows branches = new();
    private readonly PositionalBuckets values = new();
    private readonly int inlet, outlet;
    private int runCursor, offset, previous;
    private byte support = 3;
    public IPrimitiveReadPort Passage { get; }
    private sealed class Surface(LocalPath owner) : IPrimitiveReadPort
    {
        public byte ReadSupport() => owner.support;
    }

    public int SiteCount { get; }
    public int StepCount { get; }
    public int SiteBits { get; }
    public int CutBits { get; }
    public int RowArity { get; private set; }
    public int RunCount => runs.Count(2, CutBits);
    public int BranchCount => branches.Count(2, SiteBits);
    public long Inspections { get; private set; }
    public int? FailedStep { get; private set; }

    public LocalPath(int sites, int inlet, int outlet, int rowArity, params int[][] rows)
    {
        if (sites is < 1 or > 65536 || rowArity is < 1 or > 2)
            throw new ArgumentOutOfRangeException();
        SiteCount = sites;
        SiteBits = OrderedBoundary.Bits(sites);
        RowArity = rowArity;
        StepCount = rowArity == 1 ? rows.Length - 1 : rows.Length;
        if (StepCount is < 1 or > 65535) throw new ArgumentOutOfRangeException(nameof(rows));
        CutBits = OrderedBoundary.Bits(StepCount + 1);
        CheckSite(inlet);
        CheckSite(outlet);
        this.inlet = inlet;
        this.outlet = outlet;
        foreach (var row in rows)
        {
            if (row.Length != rowArity) throw new ArgumentException("Row differs from the selected layout.");
            foreach (var site in row) CheckSite(site);
            topology.Append(SiteBits, row);
        }
        for (var i = 0; i < sites; i++) values.Append(0, 2, 2, 1);
        runs.Append(CutBits, 0, StepCount);
        previous = inlet;
        Passage = new Surface(this);
    }

    private void CheckSite(int site) => ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)site, (uint)SiteCount);
    public int[] Run(int index) => runs.Read(new(2, CutBits, index));
    public int[] Branch(int index) => branches.Read(new(2, SiteBits, index));
    public byte[] TopologyBytes() => topology.PackedBytes(RowArity, SiteBits);
    public byte[] RunBytes() => runs.PackedBytes(2, CutBits);
    public byte[] BranchBytes() => branches.PackedBytes(2, SiteBits);
    public byte[] ValueBytes() => values.PackedBytes(0, 2, 2);

    // Local input apparatus publishes a changed two-bit observation in fixed O=0.
    public void ObserveSite(int site, byte word)
    {
        CheckSite(site);
        if (word > 3) throw new ArgumentOutOfRangeException(nameof(word));
        values.TryWrite(new(0, 2, 2, site), word);
    }

    public void Attach(int from, int to)
    {
        CheckSite(from);
        CheckSite(to);
        branches.Append(SiteBits, from, to);
    }

    private int Vertex(int index) => topology.Read(new(1, SiteBits, index))[0];

    private (int From, int To) Between(int startCut, int endCut)
    {
        if (RowArity == 1) return (Vertex(startCut), Vertex(endCut));
        var pair = topology.Read(new(2, SiteBits, Math.Min(startCut, endCut)));
        return startCut < endCut ? (pair[0], pair[1]) : (pair[1], pair[0]);
    }

    /// <summary>Each inspection checks one ordinary edge or the two ends of one
    /// guaranteed chain run. Progress survives beats until a route edit commits.</summary>
    public int Advance(int allowance)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(allowance);
        var used = 0;
        while (used < allowance && support == 3)
        {
            var run = Run(runCursor);
            var direction = Math.Sign(run[1] - run[0]);
            var start = run[0] + direction * offset;
            var end = RowArity == 1 ? run[1] : start + direction;
            var step = Between(start, end);
            used++;
            Inspections++;
            if (step.From != previous)
            {
                FailedStep = direction > 0 ? start : start - 1;
                support = 1;
                break;
            }
            previous = step.To;
            if (end != run[1]) { offset++; continue; }
            offset = 0;
            runCursor++;
            if (runCursor == RunCount)
            {
                support = previous == outlet ? (byte)2 : (byte)1;
                if (support == 1) FailedStep = direction > 0 ? end - 1 : end;
            }
        }
        return used;
    }

    private void Restart()
    {
        support = 3;
        runCursor = offset = 0;
        previous = inlet;
        FailedStep = null;
    }

    /// <summary>Reverse one underlying occurrence. Cut ranges keep the other edges
    /// addressable. A second reversal can coalesce adjacent runs again.</summary>
    public (int RowsRead, int RowsWritten) ReverseStep(int edge)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)edge, (uint)StepCount);
        var pieces = new List<(int Start, int End)>();
        void Add(int start, int end)
        {
            if (start == end) return;
            if (pieces.Count > 0 && pieces[^1].End == start &&
                Math.Sign(pieces[^1].End - pieces[^1].Start) == Math.Sign(end - start))
                pieces[^1] = (pieces[^1].Start, end);
            else pieces.Add((start, end));
        }
        var priorCount = RunCount;
        for (var i = 0; i < priorCount; i++)
        {
            var run = Run(i);
            if (edge < Math.Min(run[0], run[1]) || edge >= Math.Max(run[0], run[1]))
            {
                Add(run[0], run[1]);
                continue;
            }
            var before = run[0] < run[1] ? edge : edge + 1;
            var after = run[0] < run[1] ? edge + 1 : edge;
            Add(run[0], before);
            Add(after, before);
            Add(after, run[1]);
        }
        var replacement = new EndpointRows();
        foreach (var piece in pieces) replacement.Append(CutBits, piece.Start, piece.End);
        runs = replacement;
        Restart();
        return (priorCount, pieces.Count);
    }

    /// <summary>Fold ordinary authored pairs into vertex storage only after
    /// checking every interior join. The owner, sites, occurrence order, branches
    /// and selected cuts keep their correspondence. A failed attempt changes nothing.</summary>
    public bool TryPackChain(out int pairsInspected)
    {
        pairsInspected = 0;
        if (RowArity == 1) return true;
        var vertices = new EndpointRows();
        var last = -1;
        for (var i = 0; i < StepCount; i++)
        {
            var pair = topology.Read(new(2, SiteBits, i));
            pairsInspected++;
            if (i == 0) vertices.Append(SiteBits, pair[0]);
            else if (last != pair[0]) return false;
            vertices.Append(SiteBits, pair[1]);
            last = pair[1];
        }
        topology = vertices;
        RowArity = 1;
        // A completed claim survives this exact representation change. An
        // unfinished cursor measured progress in pair rows and must restart.
        if (support == 3) Restart();
        return true;
    }

    /// <summary>Owner-side inspection of selected occurrences for charts and the
    /// full-walk comparison. External consumers receive only Passage.</summary>
    public IEnumerable<(int Edge, int From, int To)> SelectedSteps()
    {
        for (var i = 0; i < RunCount; i++)
        {
            var run = Run(i);
            var direction = Math.Sign(run[1] - run[0]);
            for (var at = run[0]; at != run[1]; at += direction)
            {
                var step = Between(at, at + direction);
                yield return (Math.Min(at, at + direction), step.From, step.To);
            }
        }
    }

    public (bool Continuous, int Steps) Walk()
    {
        var arrival = inlet;
        var inspected = 0;
        foreach (var step in SelectedSteps())
        {
            inspected++;
            if (step.From != arrival) return (false, inspected);
            arrival = step.To;
        }
        return (arrival == outlet, inspected);
    }
}
