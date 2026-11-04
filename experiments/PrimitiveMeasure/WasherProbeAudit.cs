using System.Numerics;

namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>After-the-fact comparisons. Neither the test stand nor the desk calls
/// this search when choosing a probe.</summary>
public static class WasherProbeAudit
{
    private static readonly WasherProbeBank Bank = WasherProbeBank.Calibrated;
    public static int? Goal(int index) => index switch
    {
        0 => null, 1 => Bank.DoubleRelease(5), 2 => Bank.Yes(WasherProbeBank.LateReceipt),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };

    public static bool Settled(int live, int surfaces, int? goal)
    {
        if (live == 0) return false;
        bool Uniform(int yes) => (live & yes) == 0 || (live & ~yes) == 0;
        if (goal.HasValue) return Uniform(goal.Value);
        return Enumerable.Range(0,WasherProbeBank.Count)
            .Where(i => (surfaces & (1 << Bank.Probe(i).Surface)) != 0).All(i=>Uniform(Bank.Yes(i)));
    }

    // Minimum guaranteed total beats with reliable samples. Each branch gets
    // its own next choice. A split strictly reduces both candidate masks.
    public static int? MinimumGuaranteedBeats(int live, int surfaces, int? goal, bool protectStock)
    {
        Dictionary<int,int?> costs = [];
        int? Visit(int remaining)
        {
            if (Settled(remaining,surfaces,goal)) return 0;
            if (costs.TryGetValue(remaining,out var cost)) return cost;
            int? best = null;
            for (int i = 0; i < WasherProbeBank.Count; i++)
            {
                var probe = Bank.Probe(i);
                if ((surfaces & (1 << probe.Surface)) == 0 ||
                    protectStock && (remaining & Bank.DoubleRelease(i)) != 0) continue;
                int yes = remaining & Bank.Yes(i), no = remaining & ~yes;
                if (yes == 0 || no == 0) continue;
                if (Visit(yes) is not int a || Visit(no) is not int b) continue;
                int total = probe.Beats + Math.Max(a,b);
                if (!best.HasValue || total < best) best = total;
            }
            return costs[remaining] = best;
        }
        return Visit(live);
    }

    public static void Trials(TextWriter output, bool rows)
    {
        if (rows) output.WriteLine("fixture,sensors,budget,protect,balanced,goal,first_read,probe_rows,samples,final_live,beats_spent,washers_released,resolved,reliable_guarantee_cost");
        int episodes = 0, answered = 0, probes = 0, beats = 0, spentParts = 0, missedGuarantees = 0;
        for (int sensors = 0; sensors < 8; sensors++)
        for (int g = 0; g < 3; g++)
        foreach (bool protect in new[]{false,true})
        {
            var goal = Goal(g);
            int? guarantee = MinimumGuaranteedBeats(WasherProbeBank.Family,sensors,goal,protect);
            for (int budget = 0; budget <= 9; budget++)
            foreach (bool balanced in new[]{false,true})
            foreach (int first in new[]{-1,0,3})
            for (int k = 0; k < WasherTestStand.Types; k++)
            {
                var desk = new WasherProbeDesk(sensors,budget,goal,protect,preferBalanced:balanced);
                var stand = new WasherTestStand(k);
                var path = new List<int>();
                var samples = new List<byte>();
                int parts = 0;
                byte Sample(WasherProbe p)
                {
                    byte observed = stand.Sample(p);
                    int drops = WasherSurvey.Observe(stand.LastTrace).Parts;
                    if (protect && drops > 1 || stand.LastTrace.Any(b =>
                        BitOperations.PopCount((uint)b.StockBits) + BitOperations.PopCount((uint)b.TrayBits) != k % 3))
                        throw new InvalidOperationException("Stock accounting failed.");
                    parts += drops;
                    return path.Count == 0 && first >= 0 ? (byte)first : observed;
                }
                while (desk.Step(Sample) is {} trial)
                {
                    if ((trial.After & (1 << k)) == 0) throw new InvalidOperationException("Lost the real fixture.");
                    path.Add(trial.Row);
                    samples.Add(trial.Sample);
                }
                bool resolved = Settled(desk.Live,sensors,goal);
                if (resolved) answered++;
                if (!resolved && first == -1 && guarantee <= budget) missedGuarantees++;
                episodes++;
                probes += path.Count;
                beats += budget - desk.BeatsLeft;
                spentParts += parts;
                if (rows) output.WriteLine($"{k},{sensors},{budget},{(protect?1:0)},{(balanced?1:0)},{g},{first},{string.Join(".",path)},{string.Join(".",samples)},{desk.Live},{budget-desk.BeatsLeft},{parts},{(resolved?1:0)},{guarantee ?? -1}");
            }
        }
        if (!rows) output.WriteLine($"{episodes} episodes; {answered} resolved to the requested read; {probes} tests, {beats} beats, {spentParts} washers released. {missedGuarantees} reliable episodes missed an affordable guaranteed result.");
    }

    /// <summary>Compare complete traces of all fixture pairs. Record a shortest
    /// distinguishing prefix per surface independently of the desk's test menu.</summary>
    public static void Witnesses(TextWriter output)
    {
        var stands = Enumerable.Range(0,WasherTestStand.Types).Select(k=>new WasherTestStand(k)).ToArray();
        var words = new int[stands.Length,3];
        var first = new (int Beats,int Forward,int Back)[stands.Length,stands.Length,3];
        int mask = WasherSurvey.Mask(WasherSurvey.Window);
        for (int f = 0; f <= mask; f++)
        for (int b = 0; b <= mask; b++)
        {
            for (int k = 0; k < stands.Length; k++)
            {
                stands[k].Drive(f,b,WasherSurvey.Window);
                var trace = stands[k].LastTrace;
                var h = WasherSurvey.Observe(trace);
                words[k,WasherProbe.Receipt] = h.Heard;
                words[k,WasherProbe.Fall] = h.Drops;
                int lamp = 0;
                for (int t = 0; t < trace.Length; t++)
                    if (trace[t].TrayBits != 0) lamp |= 1 << t;
                words[k,WasherProbe.Lamp] = lamp;
                for (int j = 0; j < k; j++)
                for (int port = 0; port < 3; port++)
                {
                    int difference = words[j,port] ^ words[k,port];
                    if (difference == 0) continue;
                    int length = BitOperations.TrailingZeroCount((uint)difference) + 1;
                    ref var saved = ref first[j,k,port];
                    if (saved.Beats == 0 || length < saved.Beats)
                        saved = (length,f & WasherSurvey.Mask(length),b & WasherSurvey.Mask(length));
                }
            }
        }
        output.WriteLine("first_fixture,second_fixture,surface,earliest_beat,forward_word,return_word,distinguishing_test_sockets");
        for (int k = 0; k < stands.Length; k++)
        for (int j = 0; j < k; j++)
        for (int port = 0; port < 3; port++)
        {
            var witness = first[j,k,port];
            int sockets = 0;
            for (int i = 0; i < WasherProbeBank.Count; i++)
                if (Bank.Probe(i).Surface == port && (((Bank.Yes(i) >> j) ^ (Bank.Yes(i) >> k)) & 1) != 0)
                    sockets |= 1<<i;
            if ((sockets == 0) != (witness.Beats == 0))
                throw new InvalidOperationException($"The test bank misses a distinction for {j}, {k}, surface {port}.");
            output.WriteLine($"{j},{k},{port},{witness.Beats},{witness.Forward},{witness.Back},{sockets}");
        }
    }
}
