using System.Numerics;
using Gojo.Experiments.OrnamentPathCampaign;
using Gojo.Experiments.OrnamentTension;
using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.OrnamentSlotLadder;

internal enum Mode { Ladder, CapacityOnly, AlwaysOn, SpineOnly }
internal sealed record Draw(PathTrace Trace,int[] Offers,int[] GateBlocks,int[] PhysicalBlocks);
internal static class SlotBodies
{
    internal static readonly string[] Names=["key-with-branches","opposed-spirals","mirrored-crests"];
    private static StrokeRow S(int d,int extent=0,int source=0,int law=15,bool stop=false) =>
        StrokeRow.Bind(d,extent,source,law:law,stop:stop);
    private static readonly StrokeRow Wait=StrokeRow.Bind(0,pen:false);
    private static PathOwner Pen(StrokeRow[] rows,int frame=8,int clock=85,int control=0) =>
        new((byte)frame,(byte)clock,(byte)control,rows);

    // Header version 1. Three four-bit row references name unary capacity
    // publications 1,4,8; high nibbles are one-input Boolean truth masks.
    internal static byte[] Program(int i)
    {
        PathOwner[] pens=i switch{
            0=>[Pen([S(0,1),S(2,1),S(4,1),S(2,1),S(0,2),S(6,1),S(6,1),S(0,1)]),
                Pen([Wait,Wait,Wait,Wait,S(1,1,3,12),S(0,1),S(7,1,stop:true)]),
                Pen([Wait,S(7,1,3,12),S(6,1),S(5,1,stop:true)],control:1)],
            1=>[Pen([S(0,3)]),
                PathMachine.Decode(EngineLibrary.Create(1))[0] with{Frame=1},
                PathMachine.Decode(EngineLibrary.Create(1))[0] with{Frame=18}],
            2=>[Pen([S(0,3)]),
                Pen([S(2,1),S(0),S(6,1),S(7,1),S(1,1),Wait,Wait,Wait],8,128,43),
                Pen([S(2,1),S(0),S(6,1),S(7,1),S(1,1),Wait,Wait,Wait],24,128,43)],
            _=>throw new ArgumentOutOfRangeException(nameof(i))
        };
        return [1,3,0x20,0x23,0x27,..PathMachine.Encode(pens)];
    }

    private sealed class Surface(byte[] gates,PathInput input,int slots,Mode mode):IPathCoupling
    {
        // Gojo positive-frame supports 1 = absent, 2 = present. Law bit
        // ordering is input -> response, not a host motif classification.
        private static readonly BitRelation[] Laws=Enumerable.Range(0,4).Select(law=>
            BitRelation.FromTuples(2,Enumerable.Range(0,2).Select(a=>new[]{(byte)a,(byte)((law>>a)&1)}))).ToArray();
        internal readonly int[] Offers=new int[gates.Length], GateBlocks=new int[gates.Length], PhysicalBlocks=new int[gates.Length];
        private readonly int available=(1<<slots)-1;
        public void BeginBeat(int beat,int cell){}
        public PenResponse Offer(PenOffer o)
        {
            Offers[o.Owner]++;
            int address=gates[o.Owner]&15,law=gates[o.Owner]>>4;
            int published=mode switch{
                Mode.CapacityOnly or Mode.AlwaysOn=>1,
                Mode.SpineOnly=>address==0 ? 1 : 0,
                _=>(available>>address)&1};
            bool admits=Laws[law].Predict([(byte)(1<<published)])==2;
            if(!admits)GateBlocks[o.Owner]++;
            int dy=o.Direction switch{1 or 2 or 3=>1,5 or 6 or 7=>-1,_=>0};
            bool fits=mode==Mode.AlwaysOn || Math.Abs(o.Start.Y+dy*o.Length)<=slots*input.B;
            if(admits && !fits)PhysicalBlocks[o.Owner]++;
            return new(o.Direction,o.Extent,admits&&fits);
        }
        public void Drew(PathEdge edge,PathSite start,PathSite end){}
        public void EndCell(int cell,int beat,int owner,int cursor,int anchor,int nextAnchor,bool stopped){}
    }

    internal static Draw Run(byte[] program,PathInput input,int slots,Mode mode)
    {
        if(program.Length<7 || program[0]!=1 || program[1]!=3 || slots is <1 or >15)
            throw new InvalidDataException("Bad width-publication header or slot budget.");
        var native=program.AsSpan(5).ToArray();
        var gates=program.AsSpan(2,3).ToArray();
        if(gates.Any(x=>(x>>4)>3)||PathMachine.Decode(native).Length!=gates.Length)throw new InvalidDataException("Bad gate.");
        var surface=new Surface(gates,input,slots,mode);
        var trace=PathMachine.Run(native,input,PathMode.Live,surface);
        return new(trace,surface.Offers,surface.GateBlocks,surface.PhysicalBlocks);
    }
}
