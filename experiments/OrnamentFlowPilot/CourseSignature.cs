using Gojo.Experiments.OrnamentPathCampaign;

namespace Gojo.Experiments.OrnamentFlowPilot;

internal sealed record TurnRun(int FirstStroke, char Hand, int Turns);
internal sealed record ReachRun(int FirstStroke, char Hand, string Trend, long[] Reaches);
internal sealed record LengthMatch(int Source, int Target, bool Reversed, int Count);
internal sealed record CourseSignature(string TurnWord, string ReachOrder, TurnRun[] TurnRuns, ReachRun[] ReachRuns,
    int LengthPeriod, LengthMatch[] LengthMatches)
{
    public int LongestContraction => ReachRuns.Where(run => run.Trend == "shrinking")
        .Select(run => run.Reaches.Length - 1).DefaultIfEmpty().Max();

    internal static CourseSignature Read(IReadOnlyList<PathSite> bay)
    {
        int n = bay.Count;
        if (n < 3) throw new ArgumentException("Supply a repeating path with at least three corners.", nameof(bay));
        var lengths = bay.Select(v => Math.Abs((long)v.X) + Math.Abs((long)v.Y)).ToArray();
        var turns = new char[n];
        var order = new char[n];
        for (int i = 0; i < n; i++)
        {
            int next = (i + 1) % n;
            var from = bay[i];
            var to = bay[next];
            long cross = (long)from.X * to.Y - (long)from.Y * to.X;
            if (cross == 0) throw new ArgumentException("The corner read requires actual turns.", nameof(bay));
            turns[i] = cross > 0 ? 'L' : 'R';
            order[i] = lengths[i] == lengths[next] ? '=' : lengths[i] > lengths[next] ? '>' : '<';
        }
        var hands = new List<TurnRun>();
        var reaches = new List<ReachRun>();
        bool uniformHand = turns.All(turn => turn == turns[0]);
        for (int i = 0; i < n; i++)
        {
            int previous = (i + n - 1) % n;
            if (turns[i] != turns[previous] || (i == 0 && uniformHand))
            {
                int count = 1;
                while (count < n && turns[(i + count) % n] == turns[i]) count++;
                hands.Add(new(i, turns[i], count));
            }
            if (order[i] == '=' || (turns[i] == turns[previous] && order[i] == order[previous])) continue;
            int changes = 1;
            while (changes < n && turns[(i + changes) % n] == turns[i] && order[(i + changes) % n] == order[i])
                changes++;
            var chain = Enumerable.Range(0, changes + 1).Select(j => lengths[(i + j) % n]).ToArray();
            reaches.Add(new(i, turns[i], order[i] == '>' ? "shrinking" : "growing", chain));
        }
        // A length read drops both axis and sense. Preserve each matching
        // prefix, including matches that cross the chosen start of the bay.
        var matches = new List<LengthMatch>();
        int period = n;
        for (int source = 0; source < n; source++)
        for (int target = 0; target < n; target++)
        foreach (int sense in new[] { 1, -1 })
        {
            int count = 0;
            while (count < n && lengths[(source + count) % n] == lengths[(target + sense * count + n) % n])
                count++;
            if (count >= 2) matches.Add(new(source, target, sense < 0, count));
            if (source == 0 && target > 0 && sense == 1 && count == n) period = Math.Min(period, target);
        }
        return new(new string(turns), new string(order), hands.ToArray(), reaches.ToArray(), period, matches.ToArray());
    }
}
