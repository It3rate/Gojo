namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>The host binds each private inlet-to-outlet claim to a pair of public
/// roles. A occupies roles 0,1 and B occupies 2,3. Seam rows walk from the
/// first outlet to the second inlet and from the second outlet back to the
/// first inlet. These addresses select public roles in the host's own table.</summary>
public sealed class TwoPathLoop(IPrimitiveReadPort a, IPrimitiveReadPort b) : IPrimitiveReadPort
{
    private readonly EndpointRows seams = new();
    private static readonly BitRelation both = BitRelation.FromTuples(3,
        from first in Enumerable.Range(0, 2)
        from second in Enumerable.Range(0, 2)
        select new byte[] { (byte)first, (byte)second, (byte)(first & second) });

    public int SeamCount => seams.Count(2, 2);
    public void Join(int from, int to)
    {
        if (SeamCount == 2) throw new InvalidOperationException("Both seam positions are occupied.");
        seams.Append(2, from, to);
    }
    public byte[] SeamBytes() => seams.PackedBytes(2, 2);
    public byte ReadSupport()
    {
        if (SeamCount != 2) return 1;
        var across = seams.Read(new(2, 2, 0));
        var back = seams.Read(new(2, 2, 1));
        if (across[0] != 1 || across[1] != 2 || back[0] != 3 || back[1] != 0) return 1;
        return both.Predict([a.ReadSupport(), b.ReadSupport()]);
    }
}

public static class PathExchangeExperiment
{
    public static LocalPath MakePath(int steps, bool chain)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(steps, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(steps, 65534);
        // Reserve the last local site for an optional side attachment.
        if (chain)
            return new(steps + 2, 0, steps, 1,
                Enumerable.Range(0, steps + 1).Select(i => new[] { i }).ToArray());
        return new(steps + 2, 0, steps, 2,
            Enumerable.Range(0, steps).Select(i => new[] { i, i + 1 }).ToArray());
    }

    public static TwoPathLoop Join(LocalPath first, LocalPath second, bool returnSeam = true)
    {
        var loop = new TwoPathLoop(first.Passage, second.Passage);
        loop.Join(1, 2);
        if (returnSeam) loop.Join(3, 0);
        return loop;
    }

    public static void Run()
    {
        foreach (var steps in new[] { 4, 8192 })
        foreach (var chain in new[] { false, true })
        {
            var a = MakePath(steps, chain);
            var b = MakePath(steps, chain);
            var loop = Join(a, b);
            var cold = a.Advance(int.MaxValue) + b.Advance(int.MaxValue);
            var initial = loop.ReadSupport();
            var routeBytes = a.TopologyBytes().Length + b.TopologyBytes().Length;
            var edit = b.ReverseStep(steps / 2);
            var stale = loop.ReadSupport();
            var work = b.Advance(int.MaxValue);
            var complete = loop.ReadSupport();
            var manual = a.Walk().Steps + b.Walk().Steps;
            Console.WriteLine($"steps/owner={steps} arity={a.RowArity} topologyBytes={routeBytes} " +
                $"coldChecks={cold} editChecks={work} fullWalkChecks={manual} " +
                $"editRunRowsRead/Written={edit.RowsRead}/{edit.RowsWritten} cutBytesAfterEdit={a.RunBytes().Length + b.RunBytes().Length} seamBytes={loop.SeamBytes().Length} " +
                $"support={initial:X2}->{stale:X2}->{complete:X2}");
        }
    }
}
