using System.Security.Cryptography;
using System.Text.Json;
using Gojo.Experiments.OrnamentPathCampaign;
using Gojo.Experiments.OrnamentTension;

namespace Gojo.Experiments.OrnamentTensionSeries;

internal sealed record Recipe(string Path,string Policy);
internal sealed record Tray(int Family,int Replicate,Recipe First,Recipe Second);
internal sealed record Setting(string Key,string Parent,int Slots,PathInput Input);
internal sealed record Member(SeriesMove Move,byte Publication,string Ink,double Broad,double Reference,double Repeat,PatternSignature Shape,int Actuations);
internal sealed record Collection(double Novelty,double Exact,double Broad,double Reference,double Repeat,double Grade,
    int Switches,int Pulses,int DeclaredEchoes,int ActualShapes,int EchoNear,int EchoFar,int MissedNear);
internal sealed record Sheet(Tray Tray,string Rule,Setting Setting,SeriesMode Mode);

internal static class SeriesDraw
{
    public const int Length=8;
    // Measurements are training/assay-only context. The actor gets the eight-bit
    // projection, and cannot consult the memoized drawings, measures, or archive.
    public sealed class Bank(Tray tray,Setting setting,bool heldout)
    {
        private readonly Dictionary<(int,int,int,int),(byte,string,double,double,double,PatternSignature,int)> cache=[];
        public Member Expose(int step,SeriesMove move)
        {
            var key=(step,move.Tool,move.Lane,move.Pulse);
            if(!cache.TryGetValue(key,out var sample))
            {
                var run=Execute(tray,setting,step,move);
                var read=TensionCritics.Measure(run.Trace,setting.Slots,EngineLibrary.Names[tray.Family],heldout);
                sample=(run.Publication,Ink(run.Trace),read.Broad,read.Reference,read.Values[5],read.Signature,run.Stats.Actuations);
                cache.Add(key,sample);
            }
            return new(move,sample.Item1,sample.Item2,sample.Item3,sample.Item4,sample.Item5,sample.Item6,sample.Item7);
        }
    }
    public static TensionResult Execute(Tray tray,Setting setting,int step,SeriesMove move)
    {
        Recipe selected=move.Tool==0 ? tray.First : tray.Second;
        var path=Convert.FromHexString(selected.Path);
        var input=setting.Input with {Phase=(setting.Input.Phase+step%4)&3,SeedBit=(setting.Input.SeedBit+(step%2))&1};
        var impulses=move.Pulse==0 ? null : new PressureImpulse[]{new(0,path[1]+1,move.Lane,move.Pulse)};
        return TensionMachine.Run(path,input,setting.Slots,Convert.FromHexString(selected.Policy),
            FeedbackMode.Live,false,impulses);
    }
    public static Member[] Members(Bank bank,byte[] rule,SeriesMode mode,int initial)
    {
        var owner=new SeriesOwner(rule,initial);
        var members=new Member[Length];
        for(int step=0;step<Length;step++)
        {
            var move=owner.Next(mode);
            var mark=bank.Expose(step,move);
            members[step]=mark;
            owner.Receive(mark.Publication);
        }
        return members;
    }

    public static Collection Measure(Member[] members)
    {
        double novelty=0;
        int unique=0,nearEcho=0,farEcho=0,missedNear=0;
        for(int i=1;i<members.Length;i++)
        {
            double closest=0;
            for(int j=0;j<i;j++)closest=Math.Max(closest,TensionCritics.Similarity(members[i].Shape,members[j].Shape));
            novelty+=1-closest;
            if(!members.Take(i).Any(p=>p.Ink==members[i].Ink))unique++;
        }
        // Echo in the following action describes the preceding physical mark.
        // The memory has two predecessors, so compare with that same window.
        for(int i=1;i<members.Length-1;i++)
        {
            bool near=members.Skip(Math.Max(0,i-2)).Take(Math.Min(2,i))
                .Max(m=>TensionCritics.Similarity(m.Shape,members[i].Shape))>=.95;
            if(members[i+1].Move.Echo){if(near)nearEcho++;else farEcho++;}
            else if(near)missedNear++;
        }
        novelty/=members.Length-1;
        double exact=(double)unique/(members.Length-1),quality=members.Average(m=>m.Broad),refScore=members.Average(m=>m.Reference);
        double grade=.40*quality+.20*refScore+.30*novelty+.10*exact;
        return new(novelty,exact,quality,refScore,members.Average(m=>m.Repeat),grade,
            Enumerable.Range(1,members.Length-1).Count(i=>members[i].Move.Tool!=members[i-1].Move.Tool),
            members.Count(m=>m.Move.Pulse!=0),members.Count(m=>m.Move.Echo),
            members.Select(m=>m.Ink).Distinct().Count(),nearEcho,farEcho,missedNear);
    }
    internal static string Ink(PathTrace trace)
    {
        var atoms=new HashSet<(int X,int Y,int Dx,int Dy)>();
        foreach(var e in trace.Edges)
        {
            var a=trace.Sites[e.From];var b=trace.Sites[e.To];
            if(a.X>b.X || a.X==b.X && a.Y>b.Y)(a,b)=(b,a);
            int dx=Math.Sign(b.X-a.X),dy=Math.Sign(b.Y-a.Y);
            int length=Math.Max(Math.Abs(b.X-a.X),Math.Abs(b.Y-a.Y));
            for(int k=0;k<length;k++)atoms.Add((a.X+k*dx,a.Y+k*dy,dx,dy));
        }
        return Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(
            atoms.Order().Select(a=>new[]{a.X,a.Y,a.Dx,a.Dy}))));
    }
}
