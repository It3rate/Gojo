namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Each arm owns one activation. Its ink and lifted paths converge on
/// one completion site. A waiting arm cannot publish a witnessed absence.</summary>
internal sealed class OrnamentArm : IPrimitiveReadPort
{
    private readonly PulseCircuit circuit = new(3);
    private readonly int start, ink, lift, done;
    private readonly byte input;

    public OrnamentArm(IPrimitiveReadPort guard)
    {
        start = circuit.State(true); ink = circuit.State(); lift = circuit.State(); done = circuit.State();
        int gate = circuit.Guard();
        circuit.Bind(gate,guard);
        int yes = circuit.AddTable(PassageLaw.Controlled());
        int no = circuit.AddTable(PassageLaw.Controlled(false));
        int forward = circuit.AddTable(PassageLaw.Unconditional());
        circuit.Link(yes,start,gate,ink); circuit.Link(no,start,gate,lift);
        circuit.Link(forward,ink,gate,done); circuit.Link(forward,lift,gate,done);
        input = guard.ReadSupport();
        circuit.Pulse();
    }

    public byte ReadSupport() => circuit.Active == ink ? (byte)2 : circuit.Active == lift ? (byte)1 :
        input == 0 ? (byte)0 : (byte)3;
    public bool Finish() => circuit.Active != start && circuit.Pulse().After == done;
}

public readonly record struct OrnamentCell(int Position, int Phase, byte Pair, byte Arms, byte Prediction,
    byte Arrival, bool Finial, bool Finished);

public sealed class OrnamentPress
{
    // One four-bit field admits complete [left,right] pairs, low pair first.
    // The two timing rows come from the operationally retained ornament test.
    private static readonly Lazy<PulseEdit> Learned = new(() =>
    {
        var study = SocketSurvey.Start(TimingStands.Border,3,0,2);
        while (study.Step()) { }
        return study.Bench?.Retained ?? throw new InvalidOperationException("No retained plotter timing.");
    });
    public static PulseEdit Timing => Learned.Value;
    public static byte[] Strip(OrnamentDesign design,int selection)
    {
        static byte[] Clock(WasherProbe p) => Enumerable.Range(0,p.Beats)
            .Select(i=>(byte)(1 << (((p.Forward>>i)&1) + 2*((p.Back>>i)&1)))).ToArray();
        return selection switch
        {
            0 => design.HomeTape.ToArray(), 1 => Clock(Timing.Parent), 2 => Clock(Timing.Child),
            3 => [8,8], 4 => [2,4], 5 => [2,2], 6 => [4,4], 7 => [1,1],
            8 => [8,2,6,4], 9 => [8,10], 10 => [8,0],
            _ => throw new ArgumentOutOfRangeException(nameof(selection))
        };
    }

    public static OrnamentCell Fire(int position,int phase,byte pair,byte joinTruth)
    {
        var input = PresencePairPort.Bind(pair);
        var a = new OrnamentArm(input.Test(0b1010));
        var b = new OrnamentArm(input.Test(0b1100));
        var atEye = PresencePairPort.Bind(15,a,b).Test(joinTruth);
        byte arrived = atEye.ReadSupport();
        var tip = new OrnamentArm(atEye); // Its own marking tool and activation.
        byte arms = (byte)((a.ReadSupport()==2 ? 1 : 0) | (b.ReadSupport()==2 ? 2 : 0));
        bool marked = tip.ReadSupport()==2;
        bool complete = a.Finish() & b.Finish() & tip.Finish();
        return new(position,phase,pair,arms,input.Test(joinTruth).ReadSupport(),arrived,marked,complete);
    }

    public static OrnamentCell[] Print(OrnamentDesign design,IReadOnlyList<byte> tape,byte joinTruth)
    {
        var phase=new ReferenceCycle(tape.Count);
        var cells=new List<OrnamentCell>();
        for (int i=0;i<design.Count;i++)
        {
            var cell=Fire(i,phase.Position,tape[phase.Position],joinTruth);
            cells.Add(cell);
            if (!cell.Finished) break;
            if (!phase.Circuit.Pulse().Moved) throw new InvalidOperationException("Repeat phase did not advance.");
        }
        return cells.ToArray();
    }
}
