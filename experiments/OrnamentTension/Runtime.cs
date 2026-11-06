using Gojo.Experiments.OrnamentPathCampaign;
using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.OrnamentTension;

public sealed record TensionResult(PathTrace Trace,FeedbackStats Stats,PressureEvent[] Timeline)
{
    public byte Publication { get; init; }
}
public readonly record struct PressureImpulse(int Beat,int Node,int Lane,int Delta);

public static class TensionMachine
{
    private static readonly byte[] Masks=[255,7,7,15,255,7,15,255];
    public static byte[] DefaultPolicy()=>[255,2,4,1,50,0,10,85];
    public static byte[] RandomPolicy(Random rng)=>Masks.Select(m=>(byte)(rng.Next(256)&m)).ToArray();
    public static byte[] MutatePolicy(byte[] source,Random rng)
    {
        Validate(source);
        var copy=(byte[])source.Clone();
        int field=rng.Next(copy.Length);
        int bits=System.Numerics.BitOperations.Log2(Masks[field])+1;
        copy[field]^=(byte)(1<<rng.Next(bits));
        return copy;
    }
    private static void Validate(byte[] p)
    {
        ArgumentNullException.ThrowIfNull(p);
        if(p.Length!=Masks.Length || p.Where((v,i)=>(v&~Masks[i])!=0).Any())
            throw new ArgumentException("Invalid pressure policy bytes.");
    }
    public static TensionResult Run(byte[] path,PathInput input,int slots,byte[] policy,FeedbackMode mode=FeedbackMode.Live,
        bool witness=false,PressureImpulse[]? impulses=null)
    {
        Validate(policy);
        if(slots is <1 or >16 || !Enum.IsDefined(mode))throw new ArgumentOutOfRangeException(nameof(slots));
        if(path.Length<2 || path[1] is <1 or >3)throw new FormatException("Invalid owner count.");
        if(impulses?.Any(i=>i.Beat<0 || i.Node<0 || i.Node>path[1]+1 || (uint)i.Lane>=3 || Math.Abs((long)i.Delta)>12)==true)
            throw new ArgumentOutOfRangeException(nameof(impulses));
        var network=new PressureNetwork(path[1],policy,mode,witness);
        var pens=new PenCoupling(input,path[1],slots,policy,network,mode,impulses);
        var drawing=PathMachine.Run(path,input,PathMode.Live,pens);
        return new(drawing,network.Summary(),network.Timeline?.ToArray()??[]){Publication=pens.Publication};
    }
}

internal sealed class PenCoupling : IPathCoupling
{
    private readonly record struct Segment(PathSite Start,PathSite End);
    private sealed class Pen
    {
        internal int LastDirection=-1,TurnSum,Length,Draws,Upward,Axes;
        internal ulong Signature=17,Reflected=17;
        internal int PendingLane=-1,PendingSign;
        internal int[] Directions=new int[8];
    }
    private readonly PathInput input;
    private readonly int slots,height;
    private readonly int[] extents;
    private readonly FeedbackMode mode;
    private readonly PressureNetwork net;
    private readonly byte[] policy;
    private readonly PressureImpulse[]? impulses;
    private readonly List<Segment> ink=[];
    private readonly Pen[] pens;
    private int visibleInk;
    private int missingSeams,closedLoops,upward,axes;
    internal byte Publication { get; private set; }
    private ulong signature,reflected,previous,previousReflected;
    private bool hasPrevious;
    private static readonly int[] Dx=[1,1,0,-1,-1,-1,0,1], Dy=[0,1,1,1,0,-1,-1,-1];
    private static readonly bool[,,] Laws=Compile();

    internal PenCoupling(PathInput input,int count,int slots,byte[] policy,PressureNetwork network,FeedbackMode mode,
        PressureImpulse[]? impulses)
    {
        this.input=input;this.slots=slots;height=checked(slots*input.B);
        this.policy=policy;this.mode=mode;this.impulses=impulses;net=network;
        extents=[input.A,input.B,input.A+input.B,input.Pitch];
        pens=Enumerable.Range(0,count).Select(_=>new Pen()).ToArray();
    }
    private static bool[,,] Compile()
    {
        var table=new bool[16,2,2];
        for(int mask=0;mask<16;mask++)
        {
            var law=BitRelation.FromTuples(3,from a in Enumerable.Range(0,2) from b in Enumerable.Range(0,2)
                select new byte[]{(byte)a,(byte)b,(byte)((mask>>(a+2*b))&1)});
            for(int a=0;a<2;a++)for(int b=0;b<2;b++)
                table[mask,a,b]=law.Predict([(byte)(1<<a),(byte)(1<<b)])==2;
        }
        return table;
    }
    public void BeginBeat(int beat,int cell)
    {
        visibleInk=ink.Count;
        net.Begin(beat,cell);
        if(impulses!=null) foreach(var i in impulses)
            if(i.Beat==beat) net.Inject(i.Node,i.Lane,i.Delta);
    }
    private static PathSite End(PathSite p,int d,int length)=>new(p.X+Dx[d]*length,p.Y+Dy[d]*length);
    private bool TouchesInk(PathSite start,PathSite end)
    {
        long dx=end.X-start.X,dy=end.Y-start.Y;
        for(int j=0;j<visibleInk;j++)
        {
            var old=ink[j];
            if(dx*(old.Start.Y-start.Y)!=dy*(old.Start.X-start.X) ||
               dx*(old.End.Y-start.Y)!=dy*(old.End.X-start.X))continue;
            int a=dx==0 ? start.Y : start.X,b=dx==0 ? end.Y : end.X;
            int c=dx==0 ? old.Start.Y : old.Start.X,d=dx==0 ? old.End.Y : old.End.X;
            if(Math.Max(Math.Min(a,b),Math.Min(c,d))<Math.Min(Math.Max(a,b),Math.Max(c,d)))return true;
        }
        return false;
    }
    private bool Legal(PathSite site)=>Math.Abs((long)site.Y)<=height;
    private static int Turn(int a,int b)
    {
        int delta=(b-a+8)&7;
        return delta>4 ? delta-8 : delta;
    }
    public PenResponse Offer(PenOffer o)
    {
        var p=pens[o.Owner];
        p.PendingLane=-1;
        var baseEnd=End(o.Start,o.Direction,o.Length);
        bool collision=(policy[0]&1)!=0 && TouchesInk(o.Start,baseEnd);
        int hand=(policy[1]&1)==0 ? 1 : -1;
        int turn=collision ? hand*3 : 0;
        if(!Legal(baseEnd))turn=-Math.Sign(baseEnd.Y)*2;
        if(turn==0 && (policy[0]&4)!=0 && Math.Abs(p.TurnSum)>2+(policy[3]&3))
            turn=-Math.Sign(p.TurnSum);
        net.Sense(o.Owner,PressureNetwork.Turn,turn);
        int clock=(policy[7]>>((o.Beat+input.Phase)&7))&1;
        if(mode!=FeedbackMode.Quiet)
        {
            // Start the local priority scan at a position selected by the policy.
            int offset=(policy[5]>>2)+(o.Beat&1);
            for(int j=0;j<3;j++)
            {
                int lane=(j+offset)%3, demand=net.Balance(o.Owner,lane), sign=Math.Sign(demand);
                if((policy[0]&(32<<lane))==0 || Math.Abs(demand)<1+(policy[5]&3) ||
                    !Laws[policy[6],1,clock])continue;
                PenResponse? change=lane switch
                {
                    PressureNetwork.Turn=>TryTurn(o,sign,collision),
                    PressureNetwork.Reach=>TryReach(o,sign),
                    _=>TryJoin(o,sign)
                };
                if(change.HasValue)
                {
                    p.PendingLane=lane;p.PendingSign=sign;
                    return change.Value;
                }
            }
        }
        if(!Legal(baseEnd))net.BlockedSlot(o.Owner);
        return new(o.Direction,o.Extent,Legal(baseEnd));
    }
    private bool Allows(PenOffer o,PathSite candidate)
        => Legal(candidate) && (o.TargetPoint==null || candidate==o.TargetPoint);

    private PenResponse? TryTurn(PenOffer o,int sign,bool collision)
    {
        int step=1+(policy[1]>>1);
        int best=-1,bestEvidence=int.MinValue;
        for(int j=0;j<4;j++)
        {
            int rotation=j switch {0=>step,1=>-step,2=>1, _=>-1};
            int direction=(o.Direction+sign*rotation+8)&7;
            var candidate=End(o.Start,direction,o.Length);
            if(!Allows(o,candidate) || TouchesInk(o.Start,candidate))continue;
            // On a saturated vertical boundary the response must actually turn inward.
            if(!collision && !Legal(End(o.Start,o.Direction,o.Length)) &&
                Math.Abs(candidate.Y)>=Math.Abs(o.Start.Y))continue;
            int evidence=pens[o.Owner].Directions[direction];
            if(evidence>bestEvidence){best=direction;bestEvidence=evidence;}
        }
        return best<0 ? null : new(best,o.Extent,true);
    }
    private PenResponse? TryReach(PenOffer o,int sign)
    {
        int found=-1;
        for(int i=0;i<extents.Length;i++)
        {
            int difference=extents[i]-o.Length;
            if(Math.Sign(difference)!=sign || found>=0 && Math.Abs(difference)>=Math.Abs(extents[found]-o.Length))continue;
            var candidate=End(o.Start,o.Direction,extents[i]);
            if(Allows(o,candidate) && !TouchesInk(o.Start,candidate))found=i;
        }
        return found<0 ? null : new(o.Direction,found,true);
    }
    private PenResponse? TryJoin(PenOffer o,int sign)
    {
        // Only an explicit receiving port at the end of the phrase can admit
        // a repair. An authored stop can be excluded by its contract.
        if(!o.Last && !o.Stop || o.Stop && (policy[2]&4)!=0)return null;
        var site=sign>0 ? o.NextAnchorPoint : o.AnchorPoint;
        int target=sign>0 ? o.NextAnchor : o.Anchor;
        if(o.Target>=0 && o.Target!=target || !Allows(o,site) || TouchesInk(o.Start,site))return null;
        for(int d=0;d<8;d++)for(int i=0;i<4;i++)
            if(End(o.Start,d,extents[i])==site && (o.Direction!=d || o.Extent!=i ||
                (o.Target!=target && !(o.Target<0 && o.AutoNext && target==o.NextAnchor))))
                return new(d,i,true,target);
        return null;
    }
    public void Drew(PathEdge e,PathSite start,PathSite end)
    {
        var p=pens[e.Owner];
        int evidence=p.Directions[e.Direction];
        int gain=TouchesInk(start,end) ? -1 : 1;
        p.Directions[e.Direction]=Math.Clamp(evidence+gain,-3,3);
        if(evidence!=p.Directions[e.Direction])
            net.Event("direction",e.Owner,e.Owner,e.Direction,gain,0,0,evidence,p.Directions[e.Direction]);
        ink.Add(new(start,end));
        if(p.PendingLane>=0)net.Act(e.Owner,p.PendingLane,p.PendingSign);
        if(p.LastDirection>=0)p.TurnSum+=Turn(p.LastDirection,e.Direction);
        p.LastDirection=e.Direction;p.Draws++;p.Length+=extents[e.Extent];p.Upward+=end.Y-start.Y;
        p.Axes|=(e.Direction&3) switch {0=>1,1=>4,2=>2,_=>8};
        p.Signature=unchecked(p.Signature*31+(uint)(1+e.Direction+8*e.Extent));
        p.Reflected=unchecked(p.Reflected*31+(uint)(1+((8-e.Direction)&7)+8*e.Extent));
    }
    public void EndCell(int cell,int beat,int owner,int cursor,int anchor,int nextAnchor,bool stopped)
    {
        var p=pens[owner];
        if((policy[0]&2)!=0)
        {
            int target=input.Pitch/2+slots*input.B/2+((policy[3]>>2)-1)*input.B;
            int deficit=target-p.Length;
            net.Sense(owner,PressureNetwork.Reach,Math.Clamp(deficit/Math.Max(1,input.B),-3,3));
        }
        // Each pen publishes just its completion, seam, stop and turn word.
        if(cursor!=nextAnchor && p.Draws>0 && !(stopped && (policy[2]&4)!=0))missingSeams++;
        if(cursor==anchor && p.Draws>0)closedLoops++;
        upward+=Math.Sign(p.Upward);
        axes|=p.Axes;
        signature=unchecked(signature*65599+p.Signature);
        reflected=unchecked(reflected*65599+p.Reflected);
        p.LastDirection=-1;p.TurnSum=p.Length=p.Draws=p.Upward=p.Axes=0;p.Signature=p.Reflected=17;
        if(owner!=pens.Length-1)return;
        if((policy[0]&8)!=0)
        {
            int targetClosures=(policy[2]&1)!=0 ? 1 : 0;
            int need=targetClosures>closedLoops ? -2 : missingSeams>0 ? 2 : 0;
            net.Sense(net.Machine,PressureNetwork.Seam,need);
        }
        if(hasPrevious && (policy[0]&16)!=0)
        {
            bool mirrorWanted=(policy[2]&2)!=0;
            bool matches=signature==(mirrorWanted ? previousReflected : previous);
            // On a tall simple repeat the upper reader can ask for a change;
            // on a narrow/mirrored brief a failed echo asks for restoration.
            int side=upward==0 ? (cell%2==0 ? 1 : -1) : -Math.Sign(upward);
            int demand=!mirrorWanted && slots>=5 && matches ? 2*side : !matches ? side : 0;
            net.Sense(net.Frieze,PressureNetwork.Turn,demand);
            net.Sense(net.Frieze,PressureNetwork.Seam,missingSeams==0 ? 0 : 1);
        }
        Publication=(byte)(axes|(hasPrevious && signature==previous ? 16 : 0)|
            (hasPrevious && signature==previousReflected ? 32 : 0)|(missingSeams>0 ? 64 : 0)|(closedLoops>0 ? 128 : 0));
        hasPrevious=true;previous=signature;previousReflected=reflected;
        missingSeams=closedLoops=upward=axes=0;signature=reflected=0;
    }
}
