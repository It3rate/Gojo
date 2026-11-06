using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Gojo.Experiments.OrnamentPathCampaign;

namespace Gojo.Experiments.OrnamentOpenMeander;

internal sealed record Scene(string Name,string Parent,PathInput Input);
internal sealed record Cohort(Scene[][] Train,Scene[] Validation,Scene[] Final,double[] Weights);
internal static class Design
{
    public static byte[] Reference()
    {
        var rows=new[]{(0,1),(2,0),(4,1),(2,1),(0,2),(6,2),(0,1)}
            .Select(e=>StrokeRow.Bind(e.Item1,e.Item2)).ToArray();
        return PathMachine.Encode(new PathOwner(8,85,0,rows));
    }
    public static byte[] Box()=>PathMachine.Encode(new PathOwner(8,85,0,new[]{(4,0),(2,0),(0,0),(6,0),(0,3)}.Select(e=>StrokeRow.Bind(e.Item1,e.Item2)).ToArray()));
    public static Cohort Enroll(string file)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(file));
        var root=doc.RootElement;
        var used=new HashSet<(int,int,int)>();
        var prior=root.GetProperty("training").EnumerateArray().SelectMany(a=>a.Deserialize<Scene[]>(Program.Json)!)
            .Concat(new[]{"validation","blind","reserve"}.SelectMany(s=>root.GetProperty(s).Deserialize<Scene[]>(Program.Json)!));
        foreach(var s in prior)used.Add(Key(s.Input.A,s.Input.B,s.Input.Pitch));
        var strata=new List<(int A,int B,int P)>[2];
        for(int family=0;family<2;family++)
        {
            var pool=from a in Enumerable.Range(2,8) from b in Enumerable.Range(2,7)
                let p=family==0?a+2*b:2*a+b+1 where a!=b && (family==0||p!=a+2*b)
                orderby Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"OpenMeander/1/{a}/{b}/{p}"))),a,b select (a,b,p);
            strata[family]=[];
            foreach(var x in pool)if(used.Add(Key(x.a,x.b,x.p)))
            {
                strata[family].Add(x);
                if(strata[family].Count==11)break;
            }
            if(strata[family].Count!=11)throw new InvalidDataException("Insufficient new independent bases.");
        }
        Scene[] Cases(string split,int start,int parents,int variants,int replicate=0)
        {
            var result=new List<Scene>();
            for(int i=0;i<parents;i++)
            {
                var (a,b,p)=strata[i%2][start+i/2];
                for(int v=0;v<variants;v++)
                {
                    string id=$"{split}-{i:D2}";
                    result.Add(new($"{id}-{v}",id,new(a,b,p,3+(v+i)%3,(replicate+i+v)%4,(replicate+i+v)%2)));
                }
            }
            return result.ToArray();
        }
        return new(Enumerable.Range(0,4).Select(r=>Cases("train",0,6,1,r)).ToArray(),Cases("validation",3,4,2),
            Cases("final",5,12,2),root.GetProperty("weights").GetProperty("turning-key").Deserialize<double[]>()!);
    }
    private static (int,int,int) Key(int a,int b,int p)
    {
        int gcd=Gcd(a,Gcd(b,p));
        return (a/gcd,b/gcd,p/gcd);
    }
    private static int Gcd(int x,int y){while(y!=0){(x,y)=(y,x%y);}return x;}
}
