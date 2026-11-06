namespace Gojo.Experiments.OrnamentPathCampaign;

public sealed record Criterion(string Key, string Group, string Read);

/// <summary>External observers of a finished construction. Missing opportunities are NaN.</summary>
public static partial class PathJudge
{
    public static readonly Criterion[] Criteria =
    [
        new("no-overdraw","craft","Unique drawn length as a fraction of deposited length."),
        new("no-crossings","craft","Share of edges free from proper interior crossings."),
        new("no-false-joins","craft","Share of edges free from unbound tips touching other ink."),
        new("forward-turns","craft","Continuation at connected consecutive strokes without doubling back."),
        new("period-match","rhythm","Agreement of finite ink support one measured period apart."),
        new("turn-recurrence","rhythm","Connected turn phrases that recur across executed cells."),
        new("turn-variety","rhythm","Diversity of signed turns among actual connected strokes."),
        new("extent-contrast","rhythm","Moderate variation of stroke extent."),
        new("accent-alternation","rhythm","Fraction of connected triples alternating shorter and longer strokes."),
        new("band-reach","field","Fraction of the strip length reached by the horizontal projection of ink."),
        new("register-balance","field","Length centroid near the midpoint of the drawn vertical span."),
        new("breathing-room","field","Clearance of neighboring strands on transverse sections."),
        new("gap-evenness","field","Consistency of adjacent gaps observed on transverse sections."),
        new("density-window","field","Unique stroke length per area at the selected period, in a moderate range."),
        new("weight-contrast","field","Moderate alternation in line mass over successive quarter-periods."),
        new("frame-respect","field","Fraction of unique line length lying inside the declared horizontal frame."),
        new("parallel-mirror","symmetry","Reflection across the horizontal midline of the occupied band."),
        new("end-mirror","symmetry","Reflection across the vertical midline of the complete frame."),
        new("half-turn","symmetry","A half-turn around the horizontal and vertical midlines."),
        new("half-glide","symmetry","A half-period shift combined with a horizontal reflection."),
        new("connected-run","topology","Consecutive marks of one owner sharing the actual endpoint."),
        new("branch-use","topology","Cells containing a witnessed branch at a material fork."),
        new("merge-use","topology","Cells containing a witnessed merge into a participating destination."),
        new("loop-use","topology","Cells completing an identity-connected cycle of distinct drawing edges."),
        new("owner-dialogue","topology","Cells with distinct owners incident on the same material site."),
        new("boundary-match","seam","Opposite cell faces presenting ink at matching heights."),
        new("tangent-match","seam","Opposite cell faces matching both height and supporting slope."),
        new("shared-continuation","seam","Opposite cell faces sharing their actual endpoint identity.")
    ];

    public static double[] Measure(PathTrace trace)
    {
        var score = new double[Criteria.Length];
        Array.Fill(score, double.NaN);
        if (trace.Edges.Length == 0) return score;
        if (trace.Pitch <= 0 || trace.Cells <= 0) throw new ArgumentOutOfRangeException(nameof(trace));
        MeasureInk(trace, score);
        MeasureHistory(trace, score);
        MeasureSeams(trace, score);
        return score;
    }

    private static double Window(double x, double a, double b, double c, double d)
        => x < b ? Math.Clamp((x-a)/(b-a),0,1) : x <= c ? 1 : Math.Clamp((d-x)/(d-c),0,1);

    private static double Variation(double sum, double squared, int count)
    {
        if (sum == 0 || count == 0) return double.NaN;
        double mean = sum / count;
        return Math.Sqrt(Math.Max(0, squared/count - mean*mean)) / mean;
    }

    private static void MeasureHistory(PathTrace trace, double[] score)
    {
        var degree = new int[trace.Sites.Length];
        var incident = new int[trace.Sites.Length];
        var parents = Enumerable.Range(0, trace.Sites.Length).ToArray();
        var neighbors = new HashSet<(int,int)>();
        var cycles = new bool[trace.Cells];
        var shared = new bool[trace.Cells];
        var branch = new bool[trace.Cells];
        var merge = new bool[trace.Cells];
        var previous = new PathEdge?[trace.Owners];
        var previousTurn = new int?[trace.Owners];
        var previousLengthChange = new int?[trace.Owners];
        var phrases = new HashSet<(int Cell,int Phrase)>();
        var phrasesByType = new Dictionary<int,int>();
        var turnTypes = new HashSet<int>();
        double extentSum=0, extentSquared=0, forward=0;
        int joined=0, transitions=0, triples=0, alternating=0;

        foreach (var edge in trace.Edges)
        {
            int u=edge.From, v=edge.To;
            var pair=u<v ? (u,v) : (v,u);
            if (neighbors.Add(pair))
            {
                degree[u]++; degree[v]++;
                int a=Root(u), b=Root(v);
                if (a==b) cycles[edge.Cell]=true;
                else parents[a]=b;
            }
            int ownerBit=1<<edge.Owner;
            if ((incident[u]&~ownerBit)!=0 || (incident[v]&~ownerBit)!=0) shared[edge.Cell]=true;
            incident[u]|=ownerBit; incident[v]|=ownerBit;
            int length=StepLength(trace,edge);
            extentSum+=length; extentSquared+=(double)length*length;
            if (previous[edge.Owner] is { } last)
            {
                transitions++;
                if (last.To==edge.From)
                {
                    joined++;
                    int turn=(edge.Direction-last.Direction+8)%8;
                    int angle=Math.Min(turn,8-turn);
                    forward+=angle<=2 ? 1 : angle==3 ? 0.5 : 0;
                    turnTypes.Add(turn);
                    if (previousTurn[edge.Owner] is int priorTurn && priorTurn+turn!=0)
                    {
                        int phrase=8*priorTurn+turn;
                        if (phrases.Add((edge.Cell,phrase)))
                            phrasesByType[phrase]=phrasesByType.GetValueOrDefault(phrase)+1;
                    }
                    int change=Math.Sign(length-StepLength(trace,last));
                    if (previousLengthChange[edge.Owner] is int oldChange)
                    {
                        triples++;
                        if (change*oldChange < 0) alternating++;
                    }
                    previousTurn[edge.Owner]=turn;
                    previousLengthChange[edge.Owner]=change;
                }
                else
                {
                    previousTurn[edge.Owner]=null;
                    previousLengthChange[edge.Owner]=null;
                }
            }
            previous[edge.Owner]=edge;
        }
        foreach (var e in trace.Events)
        {
            if (e.Kind=="branch" && e.From >= 0 && degree[e.From]>=3) branch[e.Cell]=true;
            if (e.Kind=="merge" && e.To >= 0 && degree[e.To]>=2) merge[e.Cell]=true;
        }
        if (joined>0)
        {
            score[3]=forward/joined;
            score[6]=Math.Min(1,turnTypes.Count/3.0);
        }
        if (phrases.Count>0)
            score[5]=(double)phrases.Count(p=>phrasesByType[p.Phrase]>=2)/phrases.Count;
        if (trace.Edges.Length>=2)
            score[7]=Window(Variation(extentSum,extentSquared,trace.Edges.Length),0,.22,.75,1.4);
        if (triples>0) score[8]=(double)alternating/triples;
        if (transitions>0) score[20]=(double)joined/transitions;
        score[21]=branch.Count(x=>x)/(double)trace.Cells;
        score[22]=merge.Count(x=>x)/(double)trace.Cells;
        score[23]=cycles.Count(x=>x)/(double)trace.Cells;
        if (trace.Owners>1) score[24]=shared.Count(x=>x)/(double)trace.Cells;
        return;

        int Root(int i)
        {
            while (i != parents[i]) { parents[i]=parents[parents[i]]; i=parents[i]; }
            return i;
        }
    }

    private static int StepLength(PathTrace trace, PathEdge e)
    {
        var a=trace.Sites[e.From]; var b=trace.Sites[e.To];
        return Math.Max(Math.Abs(b.X-a.X),Math.Abs(b.Y-a.Y));
    }
}
