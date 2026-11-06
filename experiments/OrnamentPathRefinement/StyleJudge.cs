using Gojo.Experiments.OrnamentPathCampaign;

namespace Gojo.Experiments.OrnamentPathRefinement;

internal readonly record struct HistoryRead(double Utility,double Witness);
internal static class StyleJudge
{
    private readonly record struct Leg(int Direction,int Length,int From,int To);
    private static int Length(PathTrace t,PathEdge e) =>
        Math.Max(Math.Abs(t.Sites[e.To].X-t.Sites[e.From].X),Math.Abs(t.Sites[e.To].Y-t.Sites[e.From].Y));
    private static double Scale(double length,int pitch) =>
        length/pitch < .06 ? 0 : Math.Min(1,length/(.20*pitch));
    private static int Angle(int a,int b) { int d=Math.Abs(a-b)%8; return Math.Min(d,8-d); }

    public static HistoryRead Read(PathTrace t,string brief,double clean)
    {
        var strengths=new double[t.Cells];
        if (brief=="returning-key") Meanders(t,strengths);
        else if (brief=="ramified-spray") Sprays(t,strengths);
        else throw new ArgumentException("Unknown construction brief.");
        return new(strengths.Average()*(double.IsFinite(clean)?clean:0),
            strengths.Count(v=>v>=.40)/(double)t.Cells);
    }

    private static void Meanders(PathTrace t,double[] strengths)
    {
        var chains=new List<Leg>[t.Owners];
        var lastCell=new int[t.Owners];
        for (int o=0;o<chains.Length;o++) chains[o]=new(8);
        foreach (var edge in t.Edges)
        {
            var legs=chains[edge.Owner];
            if (legs.Count>0 && (lastCell[edge.Owner]!=edge.Cell || legs[^1].To!=edge.From ||
                Angle(legs[^1].Direction,edge.Direction)==4)) legs.Clear();
            lastCell[edge.Owner]=edge.Cell;
            if ((edge.Direction&1)!=0) { legs.Clear(); continue; }
            if (legs.Count>0 && legs[^1].Direction==edge.Direction)
                legs[^1]=legs[^1] with {Length=legs[^1].Length+Length(t,edge),To=edge.To};
            else legs.Add(new(edge.Direction,Length(t,edge),edge.From,edge.To));
            // Every candidate interval ends at the currently drawn stroke.
            // Earlier good subcourses survive an unrelated later instruction.
            for(int i=0;i+4<legs.Count;i++)
            {
                int east=0,west=0,north=0,south=0,smallest=int.MaxValue;
                for(int j=i;j<legs.Count;j++)
                {
                    var leg=legs[j]; smallest=Math.Min(smallest,leg.Length);
                    switch(leg.Direction) {case 0:east+=leg.Length;break;case 4:west+=leg.Length;break;
                        case 2:north+=leg.Length;break;case 6:south+=leg.Length;break;}
                }
                int advance=Math.Abs(east-west);
                int reach=Math.Min(smallest,Math.Min(Math.Min(east,west),Math.Min(north,south)));
                double register=1/(1+Math.Abs(north-south)/(double)t.Pitch);
                double strength=Scale(reach,t.Pitch)*Scale(advance,t.Pitch)*(.5+.5*register);
                strengths[edge.Cell]=Math.Max(strengths[edge.Cell],strength);
            }
        }
    }

    private static void Sprays(PathTrace t,double[] strengths)
    {
        // True source bindings are witnessed by the interpreter. A visual X
        // and an independently created copy of a site cannot enter this query.
        var branches=t.Events.Where(e=>e.Kind=="branch").ToArray();
        if(branches.Length==0) return;
        foreach(var arm in t.Edges)
        {
            if (!branches.Any(e=>e.Beat==arm.Beat&&e.Owner==arm.Owner&&e.From==arm.From&&e.To==arm.To)) continue;
            foreach(var incoming in t.Edges)
            {
                if(incoming.To!=arm.From || incoming.Beat>arm.Beat) continue;
                foreach(var through in t.Edges)
                {
                    if(through.From!=arm.From || through.Owner!=incoming.Owner ||
                        through.Beat<=incoming.Beat || through.To==arm.To || through.To==incoming.From ||
                        Angle(through.Direction,incoming.Direction)>1 ||
                        Angle(arm.Direction,incoming.Direction) is 0 or 4 ||
                        Angle(arm.Direction,through.Direction) is 0 or 4) continue;
                    int stem=Math.Min(Length(t,incoming),Length(t,through));
                    double lateral=Length(t,arm)*Math.Sin(Angle(arm.Direction,incoming.Direction)*Math.PI/4);
                    bool extension=t.Edges.Any(e=>e.Owner==arm.Owner&&e.Beat>arm.Beat&&e.From==arm.To&&
                        e.To!=arm.From&&Angle(e.Direction,arm.Direction)<4);
                    double s=Scale(stem,t.Pitch)*Scale(lateral,t.Pitch)*(extension?1:.75);
                    strengths[arm.Cell]=Math.Max(strengths[arm.Cell],s);
                }
            }
        }
    }
}
