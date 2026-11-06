using Gojo.Experiments.OrnamentPathCampaign;

namespace Gojo.Experiments.OrnamentTension;

/// <summary>Fixed-size observations of one drawing at two resolutions. These are external reads.</summary>
public sealed record PatternSignature(double[] Axes,double[] Angles,double[] Valences,double[] Form)
{
    public double[] Layout {get;init;} = [];
}
public sealed record CriticRead(double[] Values,double Broad,double Reference,double WrongReference,
    PatternSignature Signature,IReadOnlyDictionary<string,double> Raw);

public sealed class JudgePortfolio
{
    public const int Budget=16;
    private readonly byte[] shares;
    public byte[] Shares => (byte[])shares.Clone();

    public JudgePortfolio(byte[] shares)
    {
        ArgumentNullException.ThrowIfNull(shares);
        if(shares.Length!=TensionCritics.Names.Length || shares.Count(x=>x>0) is <2 or >4 ||
           shares.Sum(x=>(int)x)!=Budget)
            throw new ArgumentException("Allocate sixteen shares to two, three, or four observers.");
        this.shares=(byte[])shares.Clone();
    }
    public double Score(CriticRead read)
    {
        double sum=0;
        for(int i=0;i<shares.Length;i++) sum+=shares[i]*read.Values[i];
        return sum/Budget;
    }
    public byte[] Encode()=>Shares;
    public static JudgePortfolio Decode(byte[] bytes)=>new(bytes);
    public static JudgePortfolio Sample(Random rng,int size=0)
    {
        if(size==0)size=rng.Next(2,5);
        if(size is <2 or >4)throw new ArgumentOutOfRangeException(nameof(size));
        var selected=rng.GetItemsWithoutReplacement(TensionCritics.Names.Length,size);
        var weights=new byte[TensionCritics.Names.Length];
        foreach(int i in selected) weights[i]=1;
        for(int n=size;n<Budget;n++)weights[selected[rng.Next(size)]]++;
        return new(weights);
    }
    public JudgePortfolio Mutate(Random rng)
    {
        var active=Enumerable.Range(0,shares.Length).Where(i=>shares[i]>0).ToArray();
        var vacant=Enumerable.Range(0,shares.Length).Except(active).ToArray();
        var weights=Shares;
        int source=active[rng.Next(active.Length)];
        if(rng.Next(4)==0)
        {
            int target=vacant[rng.Next(vacant.Length)];
            weights[target]=weights[source]; weights[source]=0;
        }
        else
        {
            var destinations=Enumerable.Range(0,weights.Length).Where(i=>i!=source &&
                active.Length-(weights[source]==1 ? 1 : 0)+(weights[i]==0 ? 1 : 0) is >=2 and <=4).ToArray();
            int target=destinations[rng.Next(destinations.Length)];
            weights[source]--; weights[target]++;
        }
        return new(weights);
    }
}

internal static class Sampling
{
    internal static int[] GetItemsWithoutReplacement(this Random rng,int count,int take)
    {
        int[] pool=Enumerable.Range(0,count).ToArray();
        for(int i=0;i<take;i++)
        {
            int next=rng.Next(i,count); (pool[i],pool[next])=(pool[next],pool[i]);
        }
        return pool[..take];
    }
}

public static partial class TensionCritics
{
    public static readonly string[] Names=["course","material","joints","spacing","motif","frieze","variety","reference"];
    public static IReadOnlyList<string> Catalogue=>Names;
    private readonly record struct Atom(int X,int Y,int Axis);
    private sealed record Drawing(PatternSignature Signature,double SlotReach,double MotifMirror,
        double ReflectedNeighbor,double CellVariation,double Wind,double SpiralTerminals,double LongCourse,
        double WholeSpan,double StrayEnds,double ReturnReach,double DirectionalRun);

    public static PatternSignature Describe(PathTrace trace,int slots)=>Observe(trace,slots).Signature;

    public static double Similarity(PatternSignature a,PatternSignature b)
    {
        // Turn and junction histograms carry most of this read. Rotating a square into
        // a diamond changes its projection, while the right-angle sequence survives.
        double difference=.16*TotalVariation(a.Axes,b.Axes)+.35*TotalVariation(a.Angles,b.Angles)+
            .22*TotalVariation(a.Valences,b.Valences)+.27*a.Form.Zip(b.Form,(u,v)=>Math.Abs(u-v)).Average();
        return Math.Clamp(1-difference,0,1);
    }
    private static double TotalVariation(double[] a,double[] b)=>.5*a.Zip(b,(u,v)=>Math.Abs(u-v)).Sum();
    private static double Known(double v,double otherwise=0)=>double.IsFinite(v)?v:otherwise;
    private static double Fit(double value,double target,double tolerance)=>Math.Clamp(1-Math.Abs(value-target)/tolerance,0,1);

    public static CriticRead Measure(PathTrace trace,int verticalSlots,string family,bool heldout=false,
        IReadOnlyList<PatternSignature>? archive=null)
    {
        Drawing o=Observe(trace,verticalSlots);
        double[] prior=PathJudge.Measure(trace);
        bool spiral=family=="inward-spiral";
        bool detached=spiral;
        bool hasInk=trace.Edges.Length>0;
        double repeat=Known(prior[4]);
        double overdraw=Known(prior[0]);
        double reach=Known(prior[9]);
        double seam=Known(prior[27]);
        double continuity=Known(prior[20]);
        double terminal=spiral ? o.SpiralTerminals : 0;
        double endClean=1-o.StrayEnds*(1-terminal);
        double topology=family=="branching-spray" ? Known(prior[21]) : endClean;
        double course=detached ? .60*o.LongCourse+.25*continuity+.15*topology :
            .42*o.WholeSpan+.26*o.LongCourse+.20*seam+.12*continuity;
        double material=.82*overdraw+.18*Known(prior[15]);
        double joints=.55*topology+.25*continuity+.20*Known(prior[3]);
        double spacing=.35*o.SlotReach+.35*Known(prior[13])+.15*Known(prior[11],.5)+.15*reach;
        double motif=.60*o.MotifMirror+.20*Known(prior[10])+.20*Known(prior[7]);
        double frieze=.38*repeat+.32*o.ReflectedNeighbor+.30*(detached ? Known(prior[17]) : seam);
        double novelty=archive is {Count:>0} ? 1-archive.Max(s=>Similarity(o.Signature,s)) : double.NaN;
        double wide=Math.Clamp((verticalSlots-2)/6.0,0,1);
        // A two-level zigzag may repeat exactly. A roomy register can afford
        // modulation without asking successive bays to turn into noise.
        double variationFit=Fit(o.CellVariation,.18*wide,.35+wide*.1);
        double variety=.35*variationFit+.30*Fit(o.Wind,.18+.52*wide,.85)+
            .35*(double.IsFinite(novelty)? Math.Min(1,novelty*3) : .5);
        var (reference,wrong)=HistoricalReferences.Read(o.Signature,family,heldout);
        double[] values=[course,material,joints,spacing,motif,frieze,variety,reference];
        // A support wholly confined to one straight line cannot learn a drawing
        // brief by collecting perfect absence/crowding/symmetry reads.
        double viability=hasInk && o.Signature.Axes.Count(x=>x>0)>=2 ? 1 : 0;
        for(int i=0;i<values.Length;i++) values[i]=Math.Clamp(values[i]*viability,0,1);
        double broad=viability*(.23*course+.17*material+.12*joints+.11*spacing+
            .08*motif+.10*frieze+.07*variety+.12*reference);
        var raw=new Dictionary<string,double> {
            ["connected-course"]=o.LongCourse,["whole-course-span"]=o.WholeSpan,["shared-seam"]=Known(prior[27]),
            ["no-overdraw"]=overdraw,["transverse-clear"]=Known(prior[1]),["slot-reach"]=o.SlotReach,
            ["turn-variety"]=o.Wind,["motif-mirror"]=o.MotifMirror,["alternating-mirror"]=o.ReflectedNeighbor,
            ["period-match"]=repeat,["cell-variation"]=o.CellVariation,["shape-novelty"]=Known(novelty),
            ["archive-opportunity"]=archive is {Count:>0}?1:0,["unwanted-terminal"]=1-endClean,
            ["winding-terminal"]=terminal,["return-depth"]=o.ReturnReach,["turn-coherence"]=o.DirectionalRun,
            ["reference-match"]=reference,["wrong-reference"]=wrong,["viable"]=viability
        };
        return new(values,broad,reference,wrong,o.Signature,raw);
    }

    private static Drawing Observe(PathTrace t,int slots)
    {
        if(slots<1 || t.Cells<1 || t.Pitch<1)throw new ArgumentOutOfRangeException(nameof(slots));
        var cells=Enumerable.Range(0,t.Cells).Select(_=>new HashSet<Atom>()).ToArray();
        var levels=Enumerable.Range(0,t.Cells).Select(_=>new HashSet<int>()).ToArray();
        var rays=new Dictionary<PathSite,int>();
        var bonds=new HashSet<(int,int)>();
        var degree=new int[t.Sites.Length];
        var axes=new double[4]; var angles=new double[5]; var valences=new double[4];
        var last=new PathEdge?[t.Owners];
        var runs=new double[t.Owners]; var best=new double[t.Owners]; var total=new double[t.Owners];
        var xmin=new int[t.Owners]; var xmax=new int[t.Owners]; var runSpan=new double[t.Owners];
        var runCells=new HashSet<int>[t.Owners];
        for(int o=0;o<runCells.Length;o++)runCells[o]=new();
        var positive=new int[t.Owners]; var negative=new int[t.Owners];
        var consecutiveWinding=new int[t.Owners];
        var windingStartLength=new int[t.Owners];
        int leftward=0, nonzeroTurns=0;
        var windingEnds=new HashSet<int>();
        var allAtoms=new HashSet<Atom>();
        foreach(var e in t.Edges)
        {
            var a=t.Sites[e.From];var b=t.Sites[e.To];
            int dx=Math.Sign(b.X-a.X),dy=Math.Sign(b.Y-a.Y),len=Math.Max(Math.Abs(b.X-a.X),Math.Abs(b.Y-a.Y));
            var link=e.From<e.To ? (e.From,e.To) : (e.To,e.From);
            if(bonds.Add(link)){degree[e.From]++;degree[e.To]++;}
            rays[a]=rays.GetValueOrDefault(a)|(1<<e.Direction);
            rays[b]=rays.GetValueOrDefault(b)|(1<<((e.Direction+4)%8));
            levels[e.Cell].Add(a.Y);levels[e.Cell].Add(b.Y);
            if(dx<0)leftward+=len;
            int axis=e.Direction%4;
            for(int s=0;s<len;s++)
            {
                int x=2*a.X+dx*(2*s+1),y=2*a.Y+dy*(2*s+1);
                cells[e.Cell].Add(new Atom(x-2*t.Pitch*e.Cell,y,axis));
                if(allAtoms.Add(new Atom(x,y,axis))) axes[axis]+=axis%2==0 ? 1 : Math.Sqrt(2);
            }
            int o=e.Owner;
            double length=len*(axis%2==0 ? 1 : Math.Sqrt(2));
            total[o]+=length;
            if(last[o] is { } p && p.To==e.From)
            {
                runs[o]+=length;
                int turn=(e.Direction-p.Direction+8)%8;
                if(turn!=0)nonzeroTurns++;
                if(turn is >0 and <4)positive[o]++;
                if(turn is >4 and <8)negative[o]++;
                int sign=turn is >0 and <4 ? 1 : turn is >4 and <8 ? -1 : 0;
                if(sign!=0)
                {
                    int angle=sign*Math.Min(turn,8-turn);
                    if(sign==Math.Sign(consecutiveWinding[o])) consecutiveWinding[o]+=angle;
                    else
                    {
                        consecutiveWinding[o]=angle;
                        var pa=t.Sites[p.From];var pb=t.Sites[p.To];
                        windingStartLength[o]=Math.Max(Math.Abs(pb.X-pa.X),Math.Abs(pb.Y-pa.Y));
                    }
                    if(Math.Abs(consecutiveWinding[o])>=6 && len<windingStartLength[o]) windingEnds.Add(e.To);
                }
                else if(turn==4)consecutiveWinding[o]=0;
            }
            else
            {
                runs[o]=length;xmin[o]=a.X;xmax[o]=a.X;
                consecutiveWinding[o]=0;runCells[o].Clear();
            }
            runCells[o].Add(e.Cell);
            xmin[o]=Math.Min(xmin[o],Math.Min(a.X,b.X));xmax[o]=Math.Max(xmax[o],Math.Max(a.X,b.X));
            double coverage=Math.Min(runCells[o].Count/(double)t.Cells,
                (xmax[o]-xmin[o])/(double)(t.Pitch*t.Cells));
            runSpan[o]=Math.Max(runSpan[o],coverage);
            best[o]=Math.Max(best[o],runs[o]); last[o]=e;
        }
        foreach(var (site,mask) in rays)
        {
            int k=System.Numerics.BitOperations.PopCount((uint)mask);
            valences[Math.Clamp(k-1,0,3)]++;
            for(int i=0;i<8;i++)if((mask&(1<<i))!=0)
                for(int j=i+1;j<8;j++)if((mask&(1<<j))!=0)
                    angles[Math.Min(j-i,8-j+i)]++;
        }
        int interior=0,qualifiedTips=0;
        for(int i=0;i<degree.Length;i++)if(degree[i]==1 && t.Sites[i].X!=0 && t.Sites[i].X!=t.Pitch*t.Cells)
        { interior++;if(windingEnds.Contains(i))qualifiedTips++; }
        // Credit inner terminals only when a physically connected owner wound
        // repeatedly in the same sense on its way into the degree-one endpoint.
        var localMirrors=new List<double>();var neighbors=new List<double>();var variations=new List<double>();
        for(int c=0;c<t.Cells;c++)
        {
            var ink=cells[c];
            if(ink.Count>0)
            {
                int reflectAt=ink.Min(a=>a.X)+ink.Max(a=>a.X);
                localMirrors.Add((double)ink.Count(a=>ink.Contains(new(reflectAt-a.X,a.Y,(4-a.Axis)%4)))/ink.Count);
            }
            if(c==0)continue;
            var first=cells[c-1]; int n=first.Count+ink.Count;
            if(n==0)continue;
            neighbors.Add(2.0*first.Count(a=>ink.Contains(new(2*t.Pitch-a.X,a.Y,(4-a.Axis)%4)))/n);
            variations.Add(1-2.0*first.Count(ink.Contains)/n);
        }
        double lengthSum=total.Sum();
        double wander=t.Edges.Length>1 ? Math.Min(1,(double)nonzeroTurns/(t.Edges.Length-1)) : 0;
        double verticalReach=levels.Average(set=>Math.Min(1,(double)set.Count/slots));
        double slender=allAtoms.Count==0 ? 0 : Math.Min(1,(double)(allAtoms.Max(a=>a.Y)-allAtoms.Min(a=>a.Y))/(2*t.Pitch));
        double branch=degree.Count(d=>d>=3)/(double)Math.Max(1,degree.Count(d=>d>0));
        var layout=new double[16];
        foreach(var ink in cells)
        {
            if(ink.Count==0)continue;
            int low=ink.Min(a=>a.Y),high=ink.Max(a=>a.Y);
            foreach(var atom in ink)
            {
                int quadrant=(atom.X<t.Pitch ? 0 : 1)+(2*atom.Y<low+high ? 0 : 2);
                layout[4*quadrant+atom.Axis]+=atom.Axis%2==0?1:Math.Sqrt(2);
            }
        }
        var sig=new PatternSignature(Unit(axes),Unit(angles),Unit(valences),[slender,Math.Min(1,branch*4),wander,Math.Min(1,allAtoms.Count/(double)(t.Pitch*t.Cells*4))]){Layout=Unit(layout)};
        return new(sig,verticalReach,localMirrors.Count==0?0:localMirrors.Average(),neighbors.Count==0?0:neighbors.Average(),
            variations.Count==0?0:variations.Average(),wander,qualifiedTips/(double)Math.Max(1,interior),
            lengthSum>0?best.Sum()/lengthSum:0,runSpan.Length==0?0:runSpan.Max(),
            Math.Min(1,interior/(double)Math.Max(1,2*t.Cells)),Math.Min(1,leftward/(double)Math.Max(1,t.Pitch*t.Cells)),
            Math.Abs(positive.Sum()-negative.Sum())/(double)Math.Max(1,positive.Sum()+negative.Sum()));
    }
    private static double[] Unit(double[] bins)
    {
        double sum=bins.Sum();
        return sum==0 ? bins : bins.Select(x=>x/sum).ToArray();
    }
}
