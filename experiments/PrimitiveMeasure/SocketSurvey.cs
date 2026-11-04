using System.Numerics;

namespace Gojo.Experiments.PrimitiveMeasure;

// Apparatus settings only. The timing bench sees clock words, resource limits
// and sensor supports. It receives neither a mode number nor the unknown ticket.
public enum SocketMode
{
    Both, Earlier, Later, Locked, Protected, Balanced, Inspection, GoalSettled,
    DeskLimit, BenchLimit, MaterialLimit, MissingReference, AmbiguousReference,
    ChangedRepeat, MissingUnknown, PairQuestion
}

public static class SocketSurvey
{
    public static SocketStudy Start(TimingStands stands,int candidates,int ticket,int sensors,
        SocketMode mode = SocketMode.Both,bool early = false)
    {
        int first = BitOperations.TrailingZeroCount((uint)candidates);
        bool unknownRead = false;
        var seen = new HashSet<byte>();
        byte Unknown(WasherProbe p)
        {
            var read = stands.Run(ticket,p);
            bool skip = mode == SocketMode.MissingUnknown && !unknownRead;
            unknownRead = true;
            return skip ? (byte)0 : read.Support;
        }
        TimingRead Reference(int k,WasherProbe p)
        {
            var read = stands.Run(k,p);
            if (k != first) return read;
            byte support = mode switch
            {
                SocketMode.MissingReference => 0, SocketMode.AmbiguousReference => 3,
                SocketMode.ChangedRepeat when !seen.Add(p.Word) => (byte)(3-read.Support),
                _ => read.Support
            };
            return read with { Support = support };
        }
        return new(stands,candidates,sensors,Unknown,mode switch
        {
            SocketMode.Earlier=>TimingBench.Earlier, SocketMode.Later=>TimingBench.Later,
            SocketMode.Locked=>0, _=>TimingBench.Both
        },early,deskBeats:mode==SocketMode.DeskLimit?1:18,benchBeats:mode==SocketMode.BenchLimit?4:48,
            units:mode==SocketMode.MaterialLimit?2:48,protect:mode==SocketMode.Protected,
            balanced:mode==SocketMode.Balanced,inspect:mode==SocketMode.Inspection,
            goal:mode==SocketMode.GoalSettled?candidates:mode==SocketMode.PairQuestion?1<<first:null,
            reference:Reference);
    }

    public static void Trials(TextWriter output,bool rows)
    {
        if (rows) output.WriteLine("domain,left,right,unknown,sensors,early,mode,requested,parent,child,yes_mask,risk_mask,desk_rows,desk_samples,reference_rounds,bench_beats,bench_units,final_live");
        foreach (var (name,stands) in new[]{("factory",TimingStands.Factory),("border",TimingStands.Border),("rewind",TimingStands.ReverseBorder)})
        {
            int count=0, requested=0, fitted=0, identified=0;
            for (int b=1;b<stands.Types;b++)
            for (int a=0;a<b;a++)
            foreach (int k in new[]{a,b})
            for (int sensors=0;sensors<8;sensors++)
            foreach (bool early in new[]{false,true})
            foreach (var mode in Enum.GetValues<SocketMode>())
            {
                var s = Start(stands,(1<<a)|(1<<b),k,sensors,mode,early);
                while (s.Step()) { }
                if ((s.Desk.Live & (1<<k))==0) throw new InvalidOperationException("Real batch eliminated.");
                var retained = s.Bench?.Retained;
                if (retained != null && (s.Rack.Scope(WasherProbeBank.Feedback)!=s.RequestedPair ||
                    !s.Trials.Any(t=>t.Row==WasherProbeBank.Feedback)))
                    throw new InvalidOperationException("Retained socket lacks a scoped operational use.");
                count++;
                if (s.RequestedPair!=0) requested++;
                if (retained!=null) fitted++;
                if (s.Desk.Answered) identified++;
                if (!rows) continue;
                int rounds=s.Checks.Count(c=>c.Left.HasValue);
                int ticks=s.Checks.Where(c=>c.Left.HasValue).Sum(c=>2*c.Edit.Child.Beats);
                int material=s.Checks.Sum(c=>(c.Left?.Units??0)+(c.Right?.Units??0));
                output.WriteLine($"{name},{a},{b},{k},{sensors},{(early?1:0)},{mode},{s.RequestedPair},{(int?)retained?.Parent.Word},{(int?)retained?.Child.Word},{s.Rack.Yes(6)},{s.Rack.DoubleRelease(6)},{string.Join(".",s.Trials.Select(t=>t.Row))},{string.Join(".",s.Trials.Select(t=>t.Sample))},{rounds},{ticks},{material},{s.Desk.Live}");
            }
            if (!rows) output.WriteLine($"{name}: {count} episodes, {requested} bench requests, {fitted} sockets retained and used, {identified} desk questions answered.");
        }
    }
}
