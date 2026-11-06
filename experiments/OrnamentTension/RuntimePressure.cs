namespace Gojo.Experiments.OrnamentTension;

public enum FeedbackMode { Live, Quiet, Fixed, CutUp, CutDown, Misrouted }
public sealed record PressureEvent(int Beat,int Cell,string Kind,int Source,int Target,int Lane,int Delta,
    int Accepted,int Blocked,int Before,int After);
public sealed record RouteSummary(int Source,int Target,int Lane,int Offered,int Accepted,int Blocked,
    int Relief,int Misses,int Strength);
public sealed record FeedbackStats(int Offered,int Accepted,int Blocked,int Remainder,int Actuations,
    int SlotBlocks,int Relief,int Misses,int LearnedChanges,int PenDebt,int MachineDebt,int FriezeDebt,
    RouteSummary[] Routes);

// Three signed local bases. Source measurements inject deltas into their own accounts.
// Only a routed unit leaves its owner. Failed deliveries remain on the source.
internal sealed class PressureNetwork
{
    internal const int Reach=0, Turn=1, Seam=2;
    private const int Lanes=3, Capacity=12;
    internal readonly int Machine, Frieze;
    internal readonly List<PressureEvent>? Timeline;
    private readonly FeedbackMode mode;
    private readonly byte[] policy;
    private readonly int[,] stock, sensor, incomingBeat;
    private readonly List<Token>[,] receipts;
    private readonly Link[][] neighbors;
    private readonly List<Link> links=[];
    private readonly List<Packet> pending=[];
    private int beat,cell,changed,actuations,slotBlocks;

    private sealed class Link(int source,int target,int lane,int strength)
    {
        internal readonly int Source=source,Target=target,Lane=lane;
        internal int Strength=strength, Offered,Accepted,Blocked,Relief,Misses;
        internal bool Waiting;
        internal int Need, Ticket, ActedBeat=-1;
    }
    private sealed record Receipt(Link Link,int Ticket,Receipt? Prior);
    private readonly record struct Token(int Sign,Receipt Receipt);
    private readonly record struct Packet(Link Link,int Delta,Receipt? Prior);

    internal PressureNetwork(int pens,byte[] policy,FeedbackMode mode,bool witness)
    {
        Machine=pens; Frieze=pens+1; this.policy=policy; this.mode=mode;
        Timeline=witness ? [] : null;
        stock=new int[pens+2,Lanes]; sensor=new int[pens+2,Lanes];
        incomingBeat=new int[pens+2,Lanes]; receipts=new List<Token>[pens+2,Lanes];
        for(int i=0;i<pens+2;i++)for(int l=0;l<Lanes;l++){incomingBeat[i,l]=-2;receipts[i,l]=[];}
        for(int pen=0;pen<pens;pen++) Join(pen,Machine);
        Join(Machine,Frieze);
        neighbors=Enumerable.Range(0,pens+2).Select(n=>links.Where(r=>r.Source==n).ToArray()).ToArray();
        void Join(int a,int b)
        {
            for(int lane=0;lane<Lanes;lane++)
            {
                links.Add(new(a,b,lane,1+((policy[4]>>(lane+3))&1)));
                links.Add(new(b,a,lane,1+((policy[4]>>((a+lane)&7))&1)));
            }
        }
    }

    internal void Event(string kind,int from,int to,int lane,int delta,int accepted,int blocked,int before,int after)
        =>Timeline?.Add(new(beat,cell,kind,from,to,lane,delta,accepted,blocked,before,after));
    internal int Balance(int n,int lane)=>stock[n,lane];
    internal void Inject(int node,int lane,int delta)
    {
        if((uint)node>Frieze || (uint)lane>=Lanes || Math.Abs((long)delta)>12)
            throw new ArgumentOutOfRangeException(nameof(node));
        int before=stock[node,lane];
        Deposit(node,lane,delta,null);
        Event("ingress",node,node,lane,delta,delta,0,before,stock[node,lane]);
    }
    internal void BlockedSlot(int n)
    {
        slotBlocks++;
        Event("slot-block",n,n,Turn,0,0,0,stock[n,Turn],stock[n,Turn]);
    }

    internal void Sense(int node,int lane,int measured)
    {
        measured=Math.Clamp(measured,-3,3);
        // The same source, lane and perspective compare the outstanding demand
        // after an acknowledged act with the demand at that delivery.
        foreach(var r in neighbors[node])
        {
            if(r.Lane!=lane || !r.Waiting || r.ActedBeat<0 || r.ActedBeat>=beat) continue;
            bool relief=Math.Abs(measured)<Math.Abs(r.Need);
            if(relief)r.Relief++; else r.Misses++;
            int old=r.Strength;
            if(mode!=FeedbackMode.Fixed)
                r.Strength=Math.Clamp(old+(relief ? 1 : -1),0,4);
            if(r.Strength!=old)changed++;
            Event(relief ? "relief" : "miss",node,r.Target,lane,measured-r.Need,0,0,old,r.Strength);
            r.Waiting=false;
        }
        int delta=measured-sensor[node,lane];
        sensor[node,lane]=measured;
        // A continuing demand can reissue one unit once its earlier local
        // charge has been used. A constantly sampled zero issues nothing.
        if(delta==0 && stock[node,lane]==0 && measured!=0 && !pending.Any(x=>x.Link.Source==node && x.Link.Lane==lane))
            delta=Math.Sign(measured);
        if(delta==0)return;
        int before=stock[node,lane];
        Deposit(node,lane,delta,null);
        Event("sense",node,node,lane,delta,delta,0,before,stock[node,lane]);
    }

    internal void Act(int node,int lane,int used)
    {
        if(used==0 || Math.Sign(stock[node,lane])!=used)return;
        int before=stock[node,lane];
        var receipt=Take(node,lane,used);
        for(var p=receipt;p!=null;p=p.Prior)
            if(p.Link.Ticket==p.Ticket && p.Link.Waiting)p.Link.ActedBeat=beat;
        actuations++;
        Event("act",node,node,lane,used,used,0,before,stock[node,lane]);
    }

    internal void Begin(int tick,int bay)
    {
        beat=tick;cell=bay;
        foreach(var parcel in pending)
        {
            var r=parcel.Link;
            int on=mode==FeedbackMode.Misrouted ? (r.Lane+1)%Lanes : r.Lane;
            int before=stock[r.Target,on];
            bool cut=(mode==FeedbackMode.CutUp && r.Source<r.Target) ||
                     (mode==FeedbackMode.CutDown && r.Source>r.Target);
            bool accepted=!cut && incomingBeat[r.Target,on]!=beat && Math.Abs(before+parcel.Delta)<=Capacity;
            if(accepted)
            {
                r.Accepted++; incomingBeat[r.Target,on]=beat;
                var receipt=parcel.Prior;
                if(!r.Waiting && Math.Sign(sensor[r.Source,r.Lane])==parcel.Delta)
                {
                    r.Need=sensor[r.Source,r.Lane];r.Ticket++;r.ActedBeat=-1;r.Waiting=true;
                    receipt=new Receipt(r,r.Ticket,receipt);
                }
                Deposit(r.Target,on,parcel.Delta,receipt);
            }
            else {Deposit(r.Source,r.Lane,parcel.Delta,parcel.Prior); r.Blocked++;}
            Event("delivery",r.Source,r.Target,on,parcel.Delta,accepted ? parcel.Delta : 0,
                accepted ? 0 : parcel.Delta,before,stock[r.Target,on]);
        }
        pending.Clear();
        // Each owner may put at most one unit on one outgoing wire per tick.
        // Rotating ties preserves opportunities for every owner and sign lane.
        for(int n=0;n<=Frieze;n++)
        {
            Link? pick=null;
            int merit=int.MinValue;
            var ns=neighbors[n];
            int offset=(beat+policy[7])%ns.Length;
            for(int j=0;j<ns.Length;j++)
            {
                var r=ns[(j+offset)%ns.Length];
                int amount=stock[n,r.Lane];
                if(amount==0 || incomingBeat[n,r.Lane]==beat || (n<Machine && Math.Abs(amount)<2))continue;
                int preference=4*Math.Min(3,Math.Abs(amount))+r.Strength;
                // Most machine requests initially go to an available pen.
                if(n==Machine && r.Target<Machine)preference+=1;
                if(preference>merit){pick=r;merit=preference;}
            }
            if(pick==null)continue;
            int before=stock[n,pick.Lane],unit=Math.Sign(before);
            var prior=Take(n,pick.Lane,unit);
            pending.Add(new(pick,unit,prior));
            pick.Offered++;
            Event("offer",n,pick.Target,pick.Lane,unit,0,0,before,stock[n,pick.Lane]);
        }
    }

    private Receipt? Take(int node,int lane,int unit)
    {
        stock[node,lane]-=unit;
        var queue=receipts[node,lane];
        if(queue.Count==0)return null;
        var token=queue[0];
        if(token.Sign!=unit)throw new InvalidOperationException("Receipt polarity differs from its carrier.");
        queue.RemoveAt(0);
        return token.Receipt;
    }
    private static void Cancel(Receipt? receipt)
    {
        for(var p=receipt;p!=null;p=p.Prior)
            if(p.Link.Ticket==p.Ticket && p.Link.ActedBeat<0)p.Link.Waiting=false;
    }
    private void Deposit(int node,int lane,int delta,Receipt? receipt)
    {
        int old=stock[node,lane], sign=Math.Sign(delta);
        var queue=receipts[node,lane];
        if(old!=0 && Math.Sign(old)!=sign)
        {
            int canceled=Math.Min(Math.Abs(old),Math.Abs(delta));
            for(int i=0;i<canceled && queue.Count>0;i++) {Cancel(queue[0].Receipt);queue.RemoveAt(0);}
            // A transferred unit annihilated by opposite pressure did not
            // actuate a stroke. It cannot earn route strength.
            if(receipt!=null) {Cancel(receipt);receipt=null;}
        }
        stock[node,lane]+=delta;
        if(receipt!=null)queue.Add(new(sign,receipt));
    }

    internal FeedbackStats Summary()
    {
        int Debt(int n)=>Math.Abs(stock[n,0])+Math.Abs(stock[n,1])+Math.Abs(stock[n,2]);
        var route=links.Select(r=>new RouteSummary(r.Source,r.Target,r.Lane,r.Offered,r.Accepted,r.Blocked,
            r.Relief,r.Misses,r.Strength)).ToArray();
        return new(route.Sum(r=>r.Offered),route.Sum(r=>r.Accepted),route.Sum(r=>r.Blocked),pending.Count,
            actuations,slotBlocks,route.Sum(r=>r.Relief),route.Sum(r=>r.Misses),changed,
            Enumerable.Range(0,Machine).Sum(Debt),Debt(Machine),Debt(Frieze),route);
    }
}
