namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>A normal-depth budget in one shared, unmeasured step unit.
/// A pass reserves one slot, then retains it when its last corner completes.
/// Available + pending + retained is invariant. Observation moves nothing.</summary>
public sealed class StripDepth
{
    private readonly PortStructure owner = new();
    private readonly int[][] slots;
    private readonly PassageConnections passages;
    private readonly ConnectionRow[] reserve, retain;
    public int Count { get; }
    public int Bits { get; }
    public IPrimitiveReadPort HasRoom { get; }
    public int Pending => Array.FindIndex(slots[1],p=>owner.ReadPort(p)==1);
    public int Frontier => Array.FindIndex(slots[0],p=>owner.ReadPort(p)==1);
    public int ActiveDepth => Pending>=0?Pending:Frontier;

    public StripDepth(int count)
    {
        if(count is < 2 or > 32)throw new ArgumentOutOfRangeException(nameof(count));
        Count=count;Bits=OrderedBoundary.Bits(3*count+1);
        slots=Enumerable.Range(0,3).Select(bank=>Enumerable.Range(0,count).Select(_=>
            owner.Publish(owner.Create(0,0,1,bank==0?1UL:0UL))).ToArray()).ToArray();
        int guard=owner.Publish(owner.Create(0,0,1,1));
        passages=new(owner,PassageLaw.Controlled());
        reserve=new ConnectionRow[count-1];retain=new ConnectionRow[count-1];
        var adjacent=new List<IPrimitiveReadPort>();
        for(int i=0;i<count-1;i++)
        {
            reserve[i]=passages.Connect(Bits,slots[0][i],guard,slots[1][i]);
            retain[i]=passages.Connect(Bits,slots[1][i],guard,slots[2][i]);
            adjacent.Add(PresencePairPort.Bind(15,owner.Connect(slots[0][i]),owner.Connect(slots[0][i+1])).Test(8));
        }
        // Two consecutive available slots witness a removable gap and a
        // strictly positive inner depth. There is no numerical target average.
        HasRoom=PresencePorts.Any(adjacent.ToArray());
    }
    public void Reserve()
    {
        if(Pending>=0 || HasRoom.ReadSupport()!=2)throw new InvalidOperationException("No complete positive inner strip is available.");
        Move(reserve[Frontier]);
    }
    public void Retain()
    {
        if(Pending<0)throw new InvalidOperationException("No strip is pending.");
        Move(retain[Pending]);
    }
    private void Move(ConnectionRow row)
    {
        if(!passages.TryStep(row,passages.Observe(row),out var after) || after!=6)
            throw new InvalidOperationException("The declared whole-step passage failed.");
    }
    public byte[] BankBytes(int bank)
    {
        var bytes=new byte[(Count+7)/8];
        for(int i=0;i<Count;i++)bytes[i/8]|=(byte)(owner.ReadPort(slots[bank][i])<<(i%8));
        return bytes;
    }
    public int[] AuditCounts()=>slots.Select(row=>row.Count(p=>owner.ReadPort(p)==1)).ToArray();
    public byte[] PassageBytes()=>passages.PackedRows(Bits);
    public byte[] LawBytes()=>passages.LawBytes();
}
