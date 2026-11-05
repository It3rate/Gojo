namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>One positive-right, positive-direction array. Every physical slot
/// is a step, even when its V bit is zero. The enclosing array supplies O/D.
/// Visibility is a separate read surface, never a third stored value.</summary>
public sealed class ParticipationStrip
{
    private readonly PositionalBuckets values = new();
    public int Count => values.Count(0,0,1);
    public ParticipationStrip(params byte[] bits)
    {
        if (bits.Length is < 1 or > 32 || bits.Any(b=>b>1))
            throw new ArgumentException("Supply one to thirty-two participation bits.");
        foreach (byte bit in bits) values.Append(0,0,1,bit);
    }
    public byte At(int index) => values.TryRead(new(0,0,1,index),out var v) ? (byte)v :
        throw new ArgumentOutOfRangeException(nameof(index));
    public byte[] Bytes() => values.PackedBytes(0,0,1);
    public IPrimitiveReadPort Expose(int index, bool visible = true)
    {
        _ = At(index);
        return new Sample(this,index,visible);
    }
    private sealed class Sample(ParticipationStrip strip,int index,bool visible) : IPrimitiveReadPort
    {
        public byte ReadSupport() => visible ? (byte)(1 << strip.At(index)) : (byte)3;
    }
}

public readonly record struct GateVisit(int Slot, byte Support, bool Marked, bool Completed,
    int Before, int After);

/// <summary>Finite traversal with complementary mark/skip passages at every
/// slot. Both routes rejoin the next slot. The last routes reach a terminal
/// site, so array padding and host iteration limits cannot create more steps.</summary>
public sealed class GateWalk
{
    public PulseCircuit Circuit { get; }
    public int MarkTable { get; }
    public int SkipTable { get; }
    public int ForwardTable { get; }
    public int Done { get; }
    private readonly int[] visits, marks;
    private readonly IPrimitiveReadPort[] guards;
    public bool Complete => Circuit.Active == Done;
    public int Position => Array.IndexOf(visits,Circuit.Active ?? -1);

    public GateWalk(ParticipationStrip strip, int hiddenSlot = -1)
    {
        if (hiddenSlot < -1 || hiddenSlot >= strip.Count) throw new ArgumentOutOfRangeException(nameof(hiddenSlot));
        Circuit = new(OrderedBoundary.Bits(4*strip.Count+2));
        visits = Enumerable.Range(0,strip.Count).Select(i=>Circuit.State(i==0)).ToArray();
        marks = visits.Select(_=>Circuit.State()).ToArray();
        var skips = visits.Select(_=>Circuit.State()).ToArray();
        Done = Circuit.State();
        MarkTable = Circuit.AddTable(PassageLaw.Controlled());
        SkipTable = Circuit.AddTable(PassageLaw.Controlled(false));
        ForwardTable = Circuit.AddTable(PassageLaw.Unconditional());
        guards = Enumerable.Range(0,strip.Count).Select(i=>strip.Expose(i,i!=hiddenSlot)).ToArray();
        for (int i=0;i<strip.Count;i++)
        {
            int guard=Circuit.Guard(); Circuit.Bind(guard,guards[i]);
            int next=i+1<strip.Count?visits[i+1]:Done;
            Circuit.Link(MarkTable,visits[i],guard,marks[i]);
            Circuit.Link(SkipTable,visits[i],guard,skips[i]);
            Circuit.Link(ForwardTable,marks[i],guard,next);
            Circuit.Link(ForwardTable,skips[i],guard,next);
        }
    }

    public GateVisit Step()
    {
        if (Complete) throw new InvalidOperationException("The terminal site has no next step.");
        int slot=Position, before=Circuit.Active!.Value;
        var branch=Circuit.Pulse();
        bool marked=Circuit.Active==marks[slot];
        bool completed=branch.Moved && Circuit.Pulse().Moved;
        return new(slot,guards[slot].ReadSupport(),marked,completed,before,Circuit.Active!.Value);
    }

    public GateVisit[] Run()
    {
        var trace=new List<GateVisit>();
        while (!Complete)
        {
            var visit=Step(); trace.Add(visit);
            if (!visit.Completed) break;
        }
        return trace.ToArray();
    }
}
