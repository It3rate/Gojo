namespace Gojo.Experiments.OrnamentPathCampaign;

public static partial class PathJudge
{
    private readonly record struct BorderPort(int Y,int Slope,int Site);

    private static void MeasureSeams(PathTrace trace,double[] score)
    {
        if (trace.Cells<2) return;
        var left=new List<BorderPort>?[trace.Cells-1];
        var right=new List<BorderPort>?[trace.Cells-1];
        foreach (var e in trace.Edges)
        {
            var a=trace.Sites[e.From]; var b=trace.Sites[e.To];
            if (a.X==b.X) continue; // running along the boundary offers no transverse port
            int dx=Math.Sign(b.X-a.X), slope=Math.Sign(b.Y-a.Y)*dx;
            int lo=Math.Min(a.X,b.X), hi=Math.Max(a.X,b.X);
            int l=e.Cell*trace.Pitch, r=l+trace.Pitch;
            if (e.Cell>0 && lo<=l && hi>l) (right[e.Cell-1]??=[]).Add(Port(l));
            if (e.Cell<trace.Cells-1 && lo<r && hi>=r) (left[e.Cell]??=[]).Add(Port(r));

            BorderPort Port(int x)
                => new(a.Y+slope*(x-a.X),slope,x==a.X ? e.From : x==b.X ? e.To : -1);
        }
        int total=0, position=0, tangent=0, identity=0;
        for (int s=0;s<trace.Cells-1;s++)
        {
            var a=left[s]; var b=right[s];
            total+=(a?.Count??0)+(b?.Count??0);
            if (a==null || b==null) continue;
            position+=Matches(a,b,0); tangent+=Matches(a,b,1); identity+=Matches(a,b,2);
        }
        if (total>0)
        {
            score[25]=2.0*position/total; score[26]=2.0*tangent/total;
            score[27]=2.0*identity/total;
        }
    }

    private static int Matches(List<BorderPort> a,List<BorderPort> b,int detail)
    {
        Span<bool> used=b.Count<=96 ? stackalloc bool[b.Count] : new bool[b.Count];
        used.Clear();
        int matches=0;
        foreach (var p in a)
        for (int i=0;i<b.Count;i++)
        {
            var q=b[i];
            if (used[i] || p.Y!=q.Y || (detail==1 && p.Slope!=q.Slope) ||
                (detail==2 && (p.Site<0 || p.Site!=q.Site))) continue;
            used[i]=true; matches++; break;
        }
        return matches;
    }
}
