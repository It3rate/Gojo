using System.Numerics;
using Gojo.Experiments.OrnamentPathCampaign;

namespace Gojo.Experiments.OrnamentTension;

public sealed record JointLocation(int TwiceX,int TwiceY,int ArmMask,int[] MaterialSites,
    int MaterialFour,int IndependentTurns,int ThroughAxes,int InteriorPairs);

public sealed record JointRead(int InteriorCrossings,int CollinearOverlaps,int SharedFour,int OpposedVs,
    int StraightCrossings,int FourIntoOne,int BranchSites,int MergeSites,int IntentionalStops,int LooseTips,
    IReadOnlyDictionary<string,int> TurnPairs,IReadOnlyDictionary<string,int> ForkPairs,JointLocation[] Locations);

public sealed record JointChallenge(string Key,PathTrace Trace,JointRead Read);

/// <summary>Reads deposited lines in two perspectives: material sites and projected contacts.
/// Splitting one original edge for this observation never creates a material site.</summary>
public static class JointCensus
{
    private readonly record struct Place(int X,int Y);
    private struct Incidence
    {
        public int Incoming, Outgoing;
    }
    private sealed class Projected
    {
        public int ArmMask, ThroughAxes, InteriorPairs;
        public readonly HashSet<int> Sites=[];
        public readonly Dictionary<int,int> BentPairs=[];
    }

    private static int Count(int mask) => BitOperations.PopCount((uint)mask);
    private static long Cross(long ax,long ay,long bx,long by) => ax*by-ay*bx;
    private static void Add(Dictionary<string,int> counts,string word) =>
        counts[word]=counts.GetValueOrDefault(word)+1;

    public static JointRead Read(PathTrace trace,bool details=false)
    {
        var sites=new Incidence[trace.Sites.Length];
        var positions=new Dictionary<Place,Projected>();
        Projected At(Place p)
        {
            if(!positions.TryGetValue(p,out var junction)) positions[p]=junction=new();
            return junction;
        }
        Projected SiteAt(int s)
        {
            var site=trace.Sites[s];
            var point=At(new(2*site.X,2*site.Y));
            point.Sites.Add(s);
            return point;
        }
        var turns=new Dictionary<string,int>();
        var previous=new PathEdge?[trace.Owners];
        var previousTurn=new int?[trace.Owners];
        int collinear=0;
        for(int i=0;i<trace.Edges.Length;i++)
        {
            var edge=trace.Edges[i];
            int direction=edge.Direction,back=(direction+4)&7;
            sites[edge.From].Outgoing|=1<<direction;
            sites[edge.To].Incoming|=1<<back;
            var start=SiteAt(edge.From);
            start.ArmMask|=1<<direction;
            SiteAt(edge.To).ArmMask|=1<<back;
            if(previous[edge.Owner] is {} last && last.To==edge.From)
            {
                int arrivedFrom=(last.Direction+4)&7;
                if(last.Direction==direction) start.ThroughAxes|=1<<(direction&3);
                else if(arrivedFrom!=direction)
                {
                    // A bent continuous route owns one site; separate turns at
                    // coincident sites remain distinguishable in this map.
                    start.BentPairs[edge.From]=start.BentPairs.GetValueOrDefault(edge.From) |
                        (1<<arrivedFrom) | (1<<direction);
                }
                int turn=(direction-last.Direction+8)&7;
                if(previousTurn[edge.Owner] is int first)
                    Add(turns,$"{first}.{turn}");
                previousTurn[edge.Owner]=turn;
            }
            else previousTurn[edge.Owner]=null;
            previous[edge.Owner]=edge;
            var a=trace.Sites[edge.From]; var b=trace.Sites[edge.To];
            for(int j=0;j<i;j++)
            {
                var other=trace.Edges[j];
                var c=trace.Sites[other.From]; var d=trace.Sites[other.To];
                if(Math.Max(a.X,b.X)<Math.Min(c.X,d.X) || Math.Max(c.X,d.X)<Math.Min(a.X,b.X)
                    || Math.Max(a.Y,b.Y)<Math.Min(c.Y,d.Y) || Math.Max(c.Y,d.Y)<Math.Min(a.Y,b.Y))continue;
                long ax=b.X-a.X,ay=b.Y-a.Y,bx=d.X-c.X,by=d.Y-c.Y;
                long den=Cross(ax,ay,bx,by);
                long u=Cross(c.X-a.X,c.Y-a.Y,ax,ay);
                if(den==0)
                {
                    if(u!=0)continue;
                    bool x= ax!=0;
                    int lo=Math.Max(x ? Math.Min(a.X,b.X) : Math.Min(a.Y,b.Y),x ? Math.Min(c.X,d.X) : Math.Min(c.Y,d.Y));
                    int hi=Math.Min(x ? Math.Max(a.X,b.X) : Math.Max(a.Y,b.Y),x ? Math.Max(c.X,d.X) : Math.Max(c.Y,d.Y));
                    if(hi>lo)collinear++;
                    continue;
                }
                long t=Cross(c.X-a.X,c.Y-a.Y,bx,by);
                if(den<0){den=-den;t=-t;u=-u;}
                if(t<0 || t>den || u<0 || u>den)continue;
                bool inner=t>0 && t<den,otherInner=u>0 && u<den;
                if(!inner && !otherInner)continue;
                // Native slopes are horizontal, vertical, or 45 degrees.
                var location=At(new(checked((int)(2*a.X+2*ax*t/den)),checked((int)(2*a.Y+2*ay*t/den))));
                if(inner)
                {
                    location.ArmMask|=(1<<direction)|(1<<back);
                    location.ThroughAxes|=1<<(direction&3);
                }
                if(otherInner)
                {
                    int dir=other.Direction;
                    location.ArmMask|=(1<<dir)|(1<<((dir+4)&7));
                    location.ThroughAxes|=1<<(dir&3);
                }
                if(inner && otherInner)location.InteriorPairs++;
            }
        }

        var forks=new Dictionary<string,int>();
        int branch=0,merge=0,four=0,into=0,tips=0;
        var stopped=trace.Events.Where(e=>e.Kind=="stop" && e.From>=0 && e.From==e.To)
            .Select(e=>e.From).ToHashSet();
        var starts=trace.Events.Where(e=>e.Kind=="start").Select(e=>e.To).ToHashSet();
        int intentional=0;
        for(int i=0;i<sites.Length;i++)
        {
            var s=sites[i]; int arms=s.Incoming|s.Outgoing,degree=Count(arms);
            if(degree==1 && s.Outgoing==0)
            {
                if(stopped.Contains(i))intentional++;
                else if(!starts.Contains(i) && trace.Sites[i].X!=trace.Pitch*trace.Cells)tips++;
            }
            if(degree<3)continue;
            int incoming=Count(s.Incoming),outgoing=Count(s.Outgoing);
            if(incoming>0 && outgoing>=2)branch++;
            if(incoming>=2 && (outgoing>0 || incoming>=3))merge++;
            if(degree==4)four++;
            if(incoming>=4 && outgoing==0)into++;
            // Orient relative to each incoming ray; this avoids a privileged
            // world axis and preserves multiple perspectives on one site.
            for(int slot=0;slot<8;slot++)
                if((s.Incoming & (1<<slot))!=0)
                {
                    int Rotate(int bits)=>((bits>>slot)|(bits<<(8-slot)))&255;
                    Add(forks,$"{Rotate(s.Incoming):X2}>{Rotate(s.Outgoing):X2}");
                }
        }
        int cross=0,straight=0,vs=0;
        var witnesses=new List<JointLocation>();
        foreach(var (place,location) in positions)
        {
            if(location.InteriorPairs>0)cross++;
            if(Count(location.ArmMask)<3)continue;
            int shared=location.Sites.Count(id=>Count(sites[id].Incoming|sites[id].Outgoing)==4);
            if(Count(location.ArmMask)==4 && shared==0)
            {
                if(Count(location.ThroughAxes)>=2)straight++;
                // Require two disjoint two-ray turns at different material sites,
                // with each ray opposed by one of the other turn's rays.
                bool opposed=location.BentPairs.Values.Any(pair=>Count(pair)==2 &&
                    location.BentPairs.Values.Any(other=>(((pair<<4)|(pair>>4))&255)==other && (pair&other)==0));
                if(opposed)vs++;
            }
            if(details) witnesses.Add(new(place.X,place.Y,location.ArmMask,
                location.Sites.Order().ToArray(),shared,location.BentPairs.Count,location.ThroughAxes,location.InteriorPairs));
        }
        return new(cross,collinear,four,vs,straight,into,branch,merge,intentional,tips,turns,forks,witnesses.ToArray());
    }

    public static JointChallenge[] Challenges()
    {
        var input=new PathInput(1,1,2,1);
        StrokeRow Step(int d,int extent=0,int target=0) => StrokeRow.Bind(d,extent,target:target);
        StrokeRow wait=StrokeRow.Bind(0,pen:false);
        PathTrace Run(StrokeRow[] upper,StrokeRow[] lower) =>
            PathMachine.Run(PathMachine.Encode(new PathOwner(1,85,0,upper),new PathOwner(2,85,0,lower)),input);
        var turns=Run([Step(7),Step(1)],[Step(1),Step(7)]);
        var cross=Run([Step(7,2)],[Step(1,2)]);
        var shared=Run([Step(7),wait,Step(1)],[wait,Step(1,target:2),Step(7)]);
        // A topology fixture: same four rays, all four arrivals use one site.
        var merge=new PathTrace([new(0,1),new(0,-1),new(2,-1),new(2,1),new(1,0)],
            [new(0,4,0,0,0,0,7,0),new(1,4,1,0,0,0,1,0),
             new(2,4,2,0,0,0,3,0),new(3,4,3,0,0,0,5,0)],[],2,1,4);
        var overlap=PathMachine.Run(PathMachine.Encode(new PathOwner(0,85,0,[Step(0,2)]),
            new PathOwner(0,85,0,[Step(0)])),input);
        return new[]{("opposed-vs",turns),("straight-cross",cross),("shared-four",shared),
            ("four-arrivals",merge),("collinear-overlap",overlap)}
            .Select(p=>new JointChallenge(p.Item1,p.Item2,Read(p.Item2,true))).ToArray();
    }
}
