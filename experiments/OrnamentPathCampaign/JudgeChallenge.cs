namespace Gojo.Experiments.OrnamentPathCampaign;

public sealed record ChallengeResult(string Example,string Criterion,string Expectation,
    double? A,double? B,bool Pass,string Limit);

/// <summary>Controlled drawings for the observer. None are training or evaluation examples.</summary>
public static class JudgeChallenge
{
    private sealed class Drawing(int pitch=4,int cells=2,int owners=1)
    {
        private readonly List<PathSite> sites=[];
        private readonly List<PathEdge> edges=[];
        private readonly List<PathEvent> events=[];
        public int At(int x,int y) { sites.Add(new(x,y)); return sites.Count-1; }
        public void Draw(int a,int b,int cell=0,int owner=0,string? kind=null)
        {
            var u=sites[a]; var v=sites[b];
            int dx=Math.Sign(v.X-u.X),dy=Math.Sign(v.Y-u.Y);
            int direction=(dx,dy) switch { (1,0)=>0,(1,1)=>1,(0,1)=>2,(-1,1)=>3,
                (-1,0)=>4,(-1,-1)=>5,(0,-1)=>6,(1,-1)=>7,_=>throw new InvalidOperationException() };
            int i=edges.Count;
            edges.Add(new(a,b,owner,i,cell,i,direction,0));
            events.Add(new(owner,i,cell,i,"draw",a,b,2));
            if (kind!=null) events.Add(new(owner,i,cell,i,kind,a,b,2));
        }
        public void Segment(int x,int y,int x2,int y2,int cell=0)
            => Draw(At(x,y),At(x2,y2),cell);
        public double[] Read()=>PathJudge.Measure(new(sites.ToArray(),edges.ToArray(),events.ToArray(),pitch,cells,owners));
    }

    public static ChallengeResult[] Run()
    {
        var output=new List<ChallengeResult>();
        void Compare(string name,string key,double[] a,double[] b,string expect,string limit)
        {
            int i=Array.FindIndex(PathJudge.Criteria,x=>x.Key==key);
            double lhs=a[i], rhs=b[i];
            bool pass=expect switch { "A > B"=>lhs>rhs+1e-9, "A = B"=>Math.Abs(lhs-rhs)<1e-9,
                "A present, B unavailable"=>double.IsFinite(lhs)&&double.IsNaN(rhs),_=>false };
            output.Add(new(name,key,expect,double.IsFinite(lhs)?lhs:null,double.IsFinite(rhs)?rhs:null,pass,limit));
        }
        var clean=new Drawing(); clean.Segment(0,0,3,0); clean.Segment(2,1,5,1);
        var drawTwice=new Drawing(); drawTwice.Segment(0,0,3,0); drawTwice.Segment(5,0,2,0);
        Compare("parallel strokes / partial reverse overlap","no-overdraw",clean.Read(),drawTwice.Read(),"A > B",
            "Positive-length coincidence charges the duplicated portion exactly once on these two strokes.");

        var cross=new Drawing(); cross.Segment(0,0,1,1); cross.Segment(0,1,1,0);
        Compare("parallel strokes / half-step diagonal crossing","no-crossings",clean.Read(),cross.Read(),"A > B",
            "A half-step crossing can be unattractive in one brief and essential in a weaving brief.");
        var fork=new Drawing(4,2,2); var apart=new Drawing(4,2,2);
        foreach (var d in new[]{fork,apart})
        {
            int a=d.At(0,0),hub=d.At(2,0),b=d.At(4,0),tip=d.At(2,2);
            d.Draw(a,hub); d.Draw(hub,b);
            d.Draw(d==fork ? hub : d.At(2,0),tip,owner:1,kind:"branch");
        }
        var f=fork.Read(); var k=apart.Read();
        Compare("joined fork / coincident loose tip","branch-use",f,k,"A > B","Identical ink in both drawings.");
        Compare("joined fork / coincident loose tip","owner-dialogue",f,k,"A > B","Shared coordinates need shared identity to exchange material here.");
        Compare("joined fork / coincident loose tip","no-false-joins",f,k,"A > B","An unbound endpoint on another line has no connected site.");
        Compare("joined fork / coincident loose tip","no-overdraw",f,k,"A = B","Geometric support cannot see site identity.");

        var whole=new Drawing(); var separate=new Drawing(); var bent=new Drawing(); var displaced=new Drawing();
        foreach (var d in new[]{whole,separate,bent,displaced})
        {
            int join=d.At(4,0);
            d.Draw(d.At(0,0),join);
            int incoming = d==separate ? d.At(4,0) : d==displaced ? d.At(4,2) : join;
            d.Draw(incoming,d.At(8,d==bent ? 4 : d==displaced ? 2 : 0),1);
        }
        var w=whole.Read();
        Compare("shared seam / coincident separate sites","boundary-match",w,separate.Read(),"A = B","Geometric seam alignment survives the identity change.");
        Compare("shared seam / coincident separate sites","shared-continuation",w,separate.Read(),"A > B","A shared endpoint is stronger evidence than coincident marks.");
        Compare("straight seam / turning seam","tangent-match",w,bent.Read(),"A > B","A clean angular turn remains a joined path.");
        Compare("straight seam / turning seam","shared-continuation",w,bent.Read(),"A = B","A turn at a shared site preserves material continuity.");
        Compare("same height / displaced continuation","boundary-match",w,displaced.Read(),"A > B","Repeated motives can succeed with no through-running seam.");

        var ring=new Drawing(); var open=new Drawing();
        foreach (var d in new[]{ring,open})
        {
            int a=d.At(0,0),b=d.At(2,0),c=d.At(2,2),e=d.At(0,2);
            d.Draw(a,b); d.Draw(b,c); d.Draw(c,e); d.Draw(e,d==ring ? a : d.At(0,0),kind:d==ring ? "merge" : null);
        }
        Compare("closed four-edge path / loose coincident end","loop-use",ring.Read(),open.Read(),"A > B","Close geometry by itself supplies no shared endpoint.");
        Compare("closed four-edge path / loose coincident end","merge-use",ring.Read(),open.Read(),"A > B","The bound case supplies an explicit merge witness.");

        var glide=new Drawing(); var distorted=new Drawing();
        foreach (var d in new[]{glide,distorted})
        for (int cell=0;cell<2;cell++)
        {
            d.Segment(4*cell,1,4*cell+2,1,cell);
            int y=d==distorted && cell==1 ? -2 : -1;
            d.Segment(4*cell+2,y,4*cell+4,y,cell);
        }
        var g=glide.Read();
        Compare("gliding pair / shifted final partner","half-glide",g,distorted.Read(),"A > B",
            "Glide uses a half pitch and a mirror. In the gliding drawing the unshifted parallel mirror score is zero.");
        Compare("repeating pair / shifted final partner","period-match",g,distorted.Read(),"A > B","Agreement uses the observed next period.");

        var even=new Drawing(8,2); var uneven=new Drawing(8,2);
        foreach (var d in new[]{even,uneven})
        foreach (int y in (d==even ? new[]{0,2,4} : new[]{0,1,4}))
            d.Segment(0,y,8,y);
        Compare("even lanes / unequal lanes","gap-evenness",even.Read(),uneven.Read(),"A > B","Both pictures have identical ink length.");
        Compare("even lanes / compressed lane","breathing-room",even.Read(),uneven.Read(),"A > B","Only transverse clearance between neighboring paths is read.");
        Compare("visible gap / blank drawing","breathing-room",even.Read(),new Drawing().Read(),"A present, B unavailable","An empty field supplies no observed pair of paths.");
        return output.ToArray();
    }
}
