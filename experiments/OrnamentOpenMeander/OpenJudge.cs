using Gojo.Experiments.OrnamentPathCampaign;

namespace Gojo.Experiments.OrnamentOpenMeander;

internal readonly record struct OpenRead(double Utility,double Open,double Through);
internal static class OpenJudge
{
    private readonly record struct Leg(int Dir,int From,int To,int Length);
    private sealed record Course(int Owner,int Cell,int Start,int End,double Strength);
    private static double Scale(double n,int pitch)=>n/pitch<.06?0:Math.Min(1,n/(.2*pitch));

    public static OpenRead Read(PathTrace t,double cleanliness)
    {
        if(t.Edges.Length==0)return new(0,0,0);
        var chains=Enumerable.Range(0,t.Owners).Select(_=>new List<Leg>(8)).ToArray();
        var cells=new int[t.Owners];
        var courses=new List<Course>();
        foreach(var edge in t.Edges)
        {
            var chain=chains[edge.Owner];
            if(chain.Count!=0 && (chain[^1].To!=edge.From||cells[edge.Owner]!=edge.Cell||
                ((edge.Direction-chain[^1].Dir)&7)==4))chain.Clear();
            cells[edge.Owner]=edge.Cell;
            if((edge.Direction&1)!=0){chain.Clear();continue;}
            var a=t.Sites[edge.From];var b=t.Sites[edge.To];
            int length=Math.Max(Math.Abs(a.X-b.X),Math.Abs(a.Y-b.Y));
            if(chain.Count!=0&&chain[^1].Dir==edge.Direction)chain[^1]=chain[^1] with{To=edge.To,Length=chain[^1].Length+length};
            else chain.Add(new(edge.Direction,edge.From,edge.To,length));
            for(int i=0;i+4<chain.Count;i++)
            {
                int east=0,west=0,north=0,south=0,minLeg=int.MaxValue,minGap=int.MaxValue;
                for(int j=i;j<chain.Count;j++)
                {
                    var leg=chain[j]; minLeg=Math.Min(minLeg,leg.Length);
                    switch(leg.Dir){case 0:east+=leg.Length;break;case 2:north+=leg.Length;break;case 4:west+=leg.Length;break;case 6:south+=leg.Length;break;}
                    var p=t.Sites[leg.From];var q=t.Sites[leg.To];
                    for(int k=i;k+1<j;k++)
                    {
                        var r=t.Sites[chain[k].From];var s=t.Sites[chain[k].To];
                        int gx=Math.Max(0,Math.Max(Math.Min(p.X,q.X),Math.Min(r.X,s.X))-Math.Min(Math.Max(p.X,q.X),Math.Max(r.X,s.X)));
                        int gy=Math.Max(0,Math.Max(Math.Min(p.Y,q.Y),Math.Min(r.Y,s.Y))-Math.Min(Math.Max(p.Y,q.Y),Math.Max(r.Y,s.Y)));
                        minGap=Math.Min(minGap,Math.Max(gx,gy));
                    }
                }
                int reach=Math.Min(minLeg,Math.Min(Math.Min(east,west),Math.Min(north,south)));
                double gap=Math.Clamp((minGap/(double)t.Pitch-.06)/.12,0,1);
                double strength=Scale(reach,t.Pitch)*Scale(Math.Abs(east-west),t.Pitch)*(.5+.5/(1+Math.Abs(north-south)/(double)t.Pitch))*gap;
                if(strength>0)courses.Add(new(edge.Owner,edge.Cell,chain[i].From,edge.To,strength));
            }
        }
        var best=new double[t.Cells];var open=new bool[t.Cells];var through=new bool[t.Cells];
        foreach(var path in courses)
        {
            // A successful facet joins two qualifying owner paths at an actual
            // shared endpoint on the observed boundary. A global rendered copy
            // or an identical XY coordinate cannot create this relation.
            bool joins=courses.Any(other=>other.Owner==path.Owner&&other.Strength>=.4&&
                ((other.Cell==path.Cell-1&&other.End==path.Start&&t.Sites[path.Start].X==path.Cell*t.Pitch)||
                 (other.Cell==path.Cell+1&&path.End==other.Start&&t.Sites[path.End].X==(path.Cell+1)*t.Pitch)));
            if(path.Strength>=.4){open[path.Cell]=true;through[path.Cell]|=joins;}
            best[path.Cell]=Math.Max(best[path.Cell],path.Strength*(joins?1:.45));
        }
        return new(best.Average()*(double.IsFinite(cleanliness)?cleanliness:0),open.Count(x=>x)/(double)t.Cells,through.Count(x=>x)/(double)t.Cells);
    }
}
