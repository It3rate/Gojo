using Gojo.Experiments.OrnamentPathCampaign;

namespace Gojo.Experiments.OrnamentPathRefinement;

internal sealed record Calibration(string Name,double A,double B,bool Pass,string Meaning);
internal static class Challenges
{
    private static PathTrace Key(bool scramble=false,int pitch=8,bool overdub=false)
    {
        PathSite[] sites=[new(0,0),new(4,0),new(4,2),new(2,2),new(2,4),new(6,4),new(6,0)];
        int[] order=scramble?[0,2,1,4,3,5]:[0,1,2,3,4,5];
        int[] dirs=[0,2,4,2,0,6];
        var edges=order.Select((k,i)=>new PathEdge(k,k+1,0,i,0,i,dirs[k],0)).ToList();
        if(overdub)edges.Add(new(1,0,1,8,0,0,4,0));
        return new(sites,edges.ToArray(),[],pitch,1,2);
    }
    private static PathTrace Spray(bool loose=false,bool lacksThrough=false)
    {
        PathSite[] sites=[new(0,0),new(4,0),new(8,0),new(4,3),new(6,5),new(4,0)];
        PathEdge[] edges=[new(0,1,0,0,0,0,0,0), new(1,2,0,1,0,1,0,0),
            new(loose?5:1,3,1,1,0,1,2,0),new(3,4,1,2,0,2,1,0)];
        if(lacksThrough)edges[1]=edges[1] with {Owner=2};
        PathEvent[] events=[new(1,1,0,1,"branch",loose?5:1,3,2)];
        return new(sites,edges,events,8,1,3);
    }
    public static Calibration[] Run()
    {
        double S(PathTrace t,string brief)=>StyleJudge.Read(t,brief,PathJudge.Measure(t)[0]).Utility;
        double key=S(Key(),"returning-key"),spray=S(Spray(),"ramified-spray");
        var items=new List<Calibration>();
        void Add(string name,double a,double b,string meaning)=>items.Add(new(name,a,b,a>b+1e-9,meaning));
        Add("return order",key,S(Key(scramble:true),"returning-key"),"Identical segments and ink; only deposition order changes.");
        Add("tiny motion",key,S(Key(pitch:100),"returning-key"),"The same corner reaches are too small relative to this pitch.");
        Add("duplicate discharge",key,S(Key(overdub:true),"returning-key"),"The same successful curve plus another owner's reverse overdraw.");
        Add("bound lateral arm",spray,S(Spray(loose:true),"ramified-spray"),"Identical visible fork; one program starts its arm from a different site.");
        Add("through-going owner",spray,S(Spray(lacksThrough:true),"ramified-spray"),"Identical ink and identity at the joint; the through-stem loses its same-owner history.");
        Add("straight rails",key,S(Spray(),"returning-key"),"Three arms cannot supply the winding course.");
        return items.ToArray();
    }
}
