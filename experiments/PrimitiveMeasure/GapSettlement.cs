namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>A local collection of unary, positive gap budgets. Arrays store
/// prefixes of occupied one-bit slots in a shared step basis. Pair rows are
/// the only permitted response routes. Counts are diagnostic reads and never
/// select a transfer. External initialization supplies a chosen allocation;
/// subsequent material changes use the existing atomic passage law.</summary>
public sealed class GapSettlement
{
    private readonly PortStructure owner = new();
    private readonly EndpointRows pairs = new(), units = new();
    private readonly int[][] slots;
    private readonly bool[] visible;
    private readonly int guardBinding, guard, changedBinding, done;
    private readonly List<(IPrimitiveReadPort Forward,IPrimitiveReadPort Back)> demands = [];
    private readonly List<(EndpointRows Forward,EndpointRows Back)> demandRows = [];
    private readonly PassageConnections transfers;
    public int Count => slots.Length;
    public int Capacity { get; }
    public int ReferenceBits { get; }
    public int PairCount => pairs.Count(2,3);
    public ReferenceCycle Scan { get; }
    public PulseCircuit Stop { get; } = new(2);
    public int Visits { get; private set; }
    public int Moves { get; private set; }
    public bool Quiet => Stop.Active==done;
    public GapStep? Last { get; private set; }
    public const byte ExcessTruth = 2, AnyTruth = 14;

    public GapSettlement(int[] allocation,int[][]? connections = null,int hiddenGap = -1,int[]? stepUnits = null)
    {
        if(allocation.Length is < 3 or > 8 || allocation.Any(n=>n<1 || n>128))
            throw new ArgumentException("Use three to eight positive unary chains, each at most 128 steps.");
        if(hiddenGap < -1 || hiddenGap>=allocation.Length)throw new ArgumentOutOfRangeException(nameof(hiddenGap));
        stepUnits ??= new int[allocation.Length];
        if(stepUnits.Length!=allocation.Length || stepUnits.Any(u=>u<0 || u>7))throw new ArgumentException("Use local three-bit unit references.");
        Capacity=allocation.Max()+1;
        ReferenceBits=OrderedBoundary.Bits(allocation.Length*Capacity+2);
        slots=allocation.Select(n=>Enumerable.Range(0,Capacity).Select(i=>
            owner.Publish(owner.Create(0,0,1,i<n?1UL:0UL))).ToArray()).ToArray();
        visible=Enumerable.Range(0,Count).Select(i=>i!=hiddenGap).ToArray();
        guardBinding=owner.Create(0,0,1,0);guard=owner.Publish(guardBinding);
        changedBinding=owner.Create(0,0,1,0);int changed=owner.Publish(changedBinding);
        transfers=new(owner,PassageLaw.Controlled());
        for(int i=0;i<Count;i++)units.Append(3,i,stepUnits[i]);
        connections ??= (from a in Enumerable.Range(0,Count) from b in Enumerable.Range(a+1,Count-a-1) select new[]{a,b}).ToArray();
        if(connections.Length is < 2 or > 28 || connections.Any(p=>p.Length!=2 || p[0]==p[1] || p.Any(g=>g<0 || g>=Count)))
            throw new ArgumentException("Supply two to twenty-eight local comparison rows.");
        foreach(var pair in connections)
        {
            pairs.Append(3,pair);
            var first=Offer(pair[0],pair[1]);var second=Offer(pair[1],pair[0]);
            demands.Add((first.Port,second.Port));demandRows.Add((first.Rows,second.Rows));
        }
        Scan=new(PairCount);
        int running=Stop.State(true);done=Stop.State();int quiet=Stop.Guard();
        var noChange=PresencePairPort.Bind(15,owner.Connect(changed)).Test(5);
        Stop.Bind(quiet,PresencePorts.All(Scan.AtStart,noChange));
        Stop.Link(Stop.AddTable(PassageLaw.Controlled()),running,quiet,done);
    }

    private (IPrimitiveReadPort Port,EndpointRows Rows) Offer(int from,int to)
    {
        var rows=new EndpointRows();IPrimitiveReadPort? aggregate=null;
        for(int i=0;i<Capacity-1;i++)
        {
            rows.Append(ReferenceBits,slots[from][i+1],slots[to][i]);
            var excess=PresencePairPort.Bind(15,new SlotRead(this,from,i+1),new SlotRead(this,to,i)).Test(ExcessTruth);
            aggregate=aggregate is null?excess:PresencePairPort.Bind(15,aggregate,excess).Test(AnyTruth);
        }
        return (new CompatibleRead(this,from,to,aggregate!),rows);
    }
    private sealed class SlotRead(GapSettlement pool,int gap,int slot) : IPrimitiveReadPort
    {
        public byte ReadSupport() => pool.visible[gap] ? (byte)(1<<pool.owner.ReadPort(pool.slots[gap][slot])) : (byte)3;
    }
    private sealed class CompatibleRead(GapSettlement pool,int a,int b,IPrimitiveReadPort read) : IPrimitiveReadPort
    {
        public byte ReadSupport()=>pool.Unit(a)==pool.Unit(b)?read.ReadSupport():(byte)0;
    }
    public int Unit(int gap)=>units.Read(new(2,3,gap))[1];
    public int[] Pair(int row)=>pairs.Read(new(2,3,row));
    public byte[] PairBytes()=>pairs.PackedBytes(2,3);
    public byte[] UnitBytes()=>units.PackedBytes(2,3);
    public byte[] ValueBytes()=>owner.PackedBytes(0,0,1);
    public byte[] LawBytes()=>transfers.LawBytes();
    public byte[] TransferBytes()=>transfers.PackedRows(ReferenceBits);
    public byte[] DemandBytes(int row,bool reverse=false)=>(reverse?demandRows[row].Back:demandRows[row].Forward).PackedBytes(2,ReferenceBits);
    public byte[] GapBytes(int gap)
    {
        var bytes=new byte[(Capacity+7)/8];
        for(int i=0;i<Capacity;i++)bytes[i/8]|=(byte)(owner.ReadPort(slots[gap][i])<<(i%8));
        return bytes;
    }
    public int[] Counts()=>slots.Select(row=>row.Count(p=>owner.ReadPort(p)==1)).ToArray();
    public bool AllVisible=>visible.All(v=>v);
    public bool CommonUnit=>Enumerable.Range(0,Count).Select(Unit).Distinct().Count()==1;

    public GapStep Step()
    {
        if(Quiet)throw new InvalidOperationException("A full route scan has made no transfer.");
        int row=Scan.Position;var pair=Pair(row);var demand=demands[row];
        byte forward=demand.Forward.ReadSupport(),back=demand.Back.ReadSupport();
        int? from=null,to=null,source=null,target=null;byte before=0,after=0;
        bool moved=false;
        // These are reads of the connected truth tables, not a numerical
        // length subtraction, maximum search, or averaged target assignment.
        if(forward==2 && back==1){from=pair[0];to=pair[1];}
        if(back==2 && forward==1){from=pair[1];to=pair[0];}
        if(from is int donor && to is int receiver)
        {
            source=Array.FindLastIndex(slots[donor],p=>owner.ReadPort(p)==1);
            target=Array.FindIndex(slots[receiver],p=>owner.ReadPort(p)==0);
            if(source<1 || target<1)throw new InvalidOperationException("The positive prefix invariant was violated.");
            // A derived row specializes the existing gap-to-gap connection to
            // its exposed occupied/empty tips. Retain it as the transfer witness.
            var route=transfers.Connect(ReferenceBits,slots[donor][source.Value],guard,slots[receiver][target.Value]);
            owner.StoreObservation(guardBinding,false,false,1);
            before=transfers.Observe(route);
            moved=transfers.TryStep(route,before,out after) && after==6;
        }
        if(moved){Moves++;owner.StoreObservation(changedBinding,false,false,1);}
        Visits++;
        Last=new(row,pair[0],pair[1],forward,back,from,to,source,target,moved,before,after);
        if(!Scan.Circuit.Pulse().Moved)throw new InvalidOperationException("The comparison clock did not advance.");
        Stop.Pulse();
        if(Scan.AtStart.ReadSupport()==2)owner.StoreObservation(changedBinding,false,false,0);
        return Last;
    }

    public void Settle(int budget = 100000)
    {
        for(int i=0;i<budget && !Quiet;i++)Step();
    }
    // Observer-only audit. This is never consulted by Step or Settle.
    public GapAudit Audit()
    {
        var counts=Counts();bool readable=AllVisible&&CommonUnit;
        return new(Quiet,readable?counts.Max()-counts.Min():null,counts.Sum(),Moves,Visits,
            counts.Aggregate(0L,(sum,n)=>sum+(long)n*n));
    }
}

public sealed record GapStep(int Row,int First,int Second,byte Forward,byte Backward,int? Donor,int? Receiver,
    int? DonorSlot,int? ReceiverSlot,bool Moved,byte BeforeSupport,byte AfterState);
public readonly record struct GapAudit(bool Quiet,int? Spread,int Total,int Moves,int Visits,long SquareSum)
{
    public bool Even => Spread==0;
    public bool WithinOne => Spread<=1;
}
