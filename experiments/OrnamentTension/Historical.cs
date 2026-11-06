using System.Globalization;
using Gojo.Experiments.OrnamentPathCampaign;

namespace Gojo.Experiments.OrnamentTension;

/// <summary>A catalogue focus and a published vector approximation, separate from its source object.</summary>
public sealed record HistoricalExample(string Family,bool Holdout,string Object,string Source,string Focus,
    int Pitch,params string[] Paths);

public static class HistoricalReferences
{
    private const string Met="https://www.metmuseum.org/art/collection/search/";
    private const string Meyer="https://digital.ub.uni-paderborn.de/download/pdf/7739355.pdf";
    public static readonly HistoricalExample[] Examples=[
        new("running-key",false,"Meyer 1895, pl.81 no.1",Meyer,"One printed outline normalized to its grid",6,
            "0,0 0,4 3,4 3,3 1,3 1,0 6,0"),
        new("running-key",true,"Met 34.11.2, fig.1 upper register",
            "https://resources.metmuseum.org/resources/metpublications/pdf/Ships_on_a_Wine_Dark_Sea_in_the_Age_of_Homer_The_Metropolitan_Museum_Journal_v_35_2000.pdf",
            "Angular repeat above another narrow register; catalogue-guided interpretation",10,
            "0,0 0,5 5,5 5,2 2,2 2,3 4,3","5,0 10,0","0,-1 10,-1"),
        new("inward-spiral",false,"Met 11.186.12",Met+"248513","Spiral on the Knossos-type jug, rectilinear substitution",10,
            "0,1 0,8 8,8 8,0 2,0 2,6 6,6 6,2 4,2 4,4"),
        new("inward-spiral",true,"Met 07.232.16",Met+"247633","Light spiral on a straight-sided cup; open coiling focus",10,
            "1,1 1,8 9,8 9,0 3,0 3,6 7,6 7,2 5,2"),
        new("zigzag",false,"Met Manjak wrapper",Met+"687564","A zigzag strip with a parallel boundary",8,
            "0,0 4,4 8,0","0,-1 8,-1"),
        new("zigzag",true,"Inka tunic with zigzag pattern, ca.1460–1540",
            "https://www.metmuseum.org/exhibitions/listings/2011/the-andean-tunic-400-bce1800-ce/photo-gallery",
            "Slants in adjoining squares read as a segmented zigzag",8,"0,0 4,4 8,0","0,2 4,6 8,2"),
        new("opposed-chevron",false,"Met 90.5.429",Met+"443915","Opposed zigzags enclose a diamond-shaped figure",8,
            "0,0 4,4 8,0","0,0 4,-4 8,0","2,0 4,2 6,0 4,-2 2,0"),
        new("opposed-chevron",true,"Met 62.145, Double X quilt",Met+"13885","Diamond-set units and a narrow zigzag border",8,
            "0,0 4,4 8,0 4,-4 0,0","0,5 2,7 4,5 6,7 8,5"),
        new("crossing-ribbons",false,"Met 89.18.176",Met+"443381","Angular interlace support within a medallion; depth omitted",8,
            "0,0 4,4 8,0","0,4 4,0 8,4","0,2 4,2 8,2"),
        new("crossing-ribbons",true,"Met 12.55.6a,b",Met+"446214","Interlaced cross and saw-pattern registers selected from the fragment",8,
            "0,0 4,4 8,0","0,4 4,0 8,4","0,-2 2,0 4,-2 6,0 8,-2"),
        new("branching-spray",false,"French flower-vine border",Met+"722501","Undulating stem with smaller offshoots, angularized",8,
            "0,0 2,2 4,2 6,0 8,0","2,2 2,4 3,5","6,0 7,-1 7,-3"),
        new("branching-spray",true,"Met textile of repeating plants",Met+"453004","Leaves and stems grow from a base within a repeating row",8,
            "4,0 4,5","4,1 2,3 1,3","4,3 6,5 7,5"),
        new("alternating-pinline",false,"Met 12.55.5d",Met+"446213","Parallel bands accompanied by alternating merlon accents",8,
            "0,0 8,0","0,4 8,4","0,1 2,3 4,1","4,3 6,1 8,3"),
        new("alternating-pinline",true,"Met 29.136.4",Met+"448251","Narrow bounding strips and scrolls in the broad intervening register",8,
            "0,-2 8,-2","0,-1 8,-1","0,4 8,4","0,5 8,5","0,1 2,3 4,1 6,-1 8,1"),
        new("counterstep",false,"Meyer 1895, pl.82 no.1",Meyer,"Upper stepped boundary normalized to its grid",4,
            "0,0 0,2 3,2 3,0 4,0","0,-1 1,-1 1,1 2,1 2,-1 4,-1"),
        new("counterstep",true,"Met 29.22",
            "https://www.metmuseum.org/-/media/files/learn/for%20educators/publications/Islamic%20Teacher%20Resource/Unit3.pdf",
            "Reciprocal stepped bands in the museum's fig.13, outline approximation",8,
            "0,0 2,0 2,2 4,2 4,0 6,0 6,-2 8,-2","0,3 2,3 2,5 4,5 4,3 6,3 6,1 8,1")
    ];
    private static readonly string[] Wrong=["zigzag","branching-spray","running-key","counterstep",
        "inward-spiral","opposed-chevron","crossing-ribbons","opposed-chevron"];
    private static readonly PatternSignature[] Packets=Examples.Select(e=>TensionCritics.Describe(Draw(e),4)).ToArray();

    public static PathTrace Draw(HistoricalExample e)
    {
        var sites=new List<PathSite>();
        var edges=new List<PathEdge>();
        var lookup=new Dictionary<PathSite,int>();
        int At(PathSite p)
        {
            if(lookup.TryGetValue(p,out int i))return i;
            i=sites.Count; sites.Add(p); lookup.Add(p,i); return i;
        }
        for(int cell=0;cell<3;cell++)
        for(int path=0;path<e.Paths.Length;path++)
        {
            int? last=null;
            foreach(string word in e.Paths[path].Split(' ',StringSplitOptions.RemoveEmptyEntries))
            {
                var xy=word.Split(',');
                var p=new PathSite(int.Parse(xy[0],CultureInfo.InvariantCulture)+e.Pitch*cell,
                    int.Parse(xy[1],CultureInfo.InvariantCulture));
                int next=At(p);
                if(last is int previous)
                {
                    var a=sites[previous];
                    int dx=p.X-a.X,dy=p.Y-a.Y;
                    if((dx==0 && dy==0) || (dx!=0 && dy!=0 && Math.Abs(dx)!=Math.Abs(dy)))
                        throw new InvalidDataException($"Invalid historical sketch {e.Object}: {a}→{p}");
                    int d=(Math.Sign(dx),Math.Sign(dy)) switch
                    {
                        (1,0)=>0,(1,1)=>1,(0,1)=>2,(-1,1)=>3,(-1,0)=>4,(-1,-1)=>5,(0,-1)=>6,_=>7
                    };
                    edges.Add(new(previous,next,path,edges.Count,cell,edges.Count,d,0));
                }
                last=next;
            }
        }
        return new(sites.ToArray(),edges.ToArray(),[],e.Pitch,3,e.Paths.Length);
    }

    public static (double Match,double Wrong) Read(PatternSignature sig,string family,bool holdout)
    {
        int index=Array.FindIndex(Examples,e=>e.Family==family && e.Holdout==holdout);
        if(index<0)throw new ArgumentException($"No registered historic reference for '{family}'.",nameof(family));
        string contrast=Wrong[index/2];
        int wrong=Array.FindIndex(Examples,e=>e.Family==contrast && e.Holdout==holdout);
        return (Compare(sig,Packets[index]),Compare(sig,Packets[wrong]));
    }
    // Comparisons concern line geometry alone. Neither the chronological pen
    // sequence nor material ownership can be read from a museum still image.
    private static double Compare(PatternSignature a,PatternSignature b)
    {
        static double Distance(double[] u,double[] v)=>.5*u.Zip(v,(x,y)=>Math.Abs(x-y)).Sum();
        double distance=.28*Distance(a.Axes,b.Axes)+.20*Distance(a.Angles,b.Angles)+
            .15*Distance(a.Valences,b.Valences)+.25*Distance(a.Layout,b.Layout)+
            .06*Math.Abs(a.Form[0]-b.Form[0])+.06*Math.Abs(a.Form[3]-b.Form[3]);
        return Math.Clamp(1-distance,0,1);
    }
}
