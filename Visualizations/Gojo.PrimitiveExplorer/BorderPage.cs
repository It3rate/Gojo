using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.PrimitiveExplorer;

public sealed partial class ExplorerSession
{
    private ExplorerPage BorderPage()
    {
        var p = new ExplorerPage("Read a repeating border",
            "Which part repeats, which part moves, and what can a neighbor know if a stroke is hidden?", "BorderScore.md");
        Choose(p,"Experiment",["Two streams","Missing ink","Same linear carrier","Coverage outlet","Boolean spans"],Mode,
            n => { Mode = n; Variant = Control = B = 0; },true);
        switch (Mode)
        {
            case 0: ShowBorderStreams(p); break;
            case 1: ShowBorderEvidence(p); break;
            case 2: ShowLinearLinks(p); break;
            case 3: ShowBorderCoverage(p); break;
            default: ShowBooleanSpans(p); break;
        }
        return p;
    }

    private static string SignText(byte support) => support switch { 1 => "negative", 2 => "zero", 4 => "positive", _ => "negative, zero or positive" };
    private static byte[] LowBytes(ulong bits,int count) =>
        Enumerable.Range(0,count).Select(i=>(byte)(bits >> (8*i))).ToArray();

    private void ShowBorderStreams(ExplorerPage p)
    {
        Choose(p,"Score",["Direct wave","Alternating siblings","Together","Local loop alone","Loop with drift","Separate fall","Opposed · alternate","Opposed · together"],Variant,n=>Variant=n);
        Choose(p,"Read",["As wired","Walk backward","Assume equal reaches"],Control,n=>Control=n);
        var score = Variant switch { 0 => BorderStudy.Direct(), 1 => BorderStudy.Alternating(), 2 => BorderStudy.Together(),
            3 => BorderStudy.LoopWithDrift(false), 4 => BorderStudy.LoopWithDrift(true), 5 => BorderStudy.SeparateFall(), _ => BorderStudy.OpposedSiblings(Variant==7) };
        var proof = score.Predict();
        bool reverse = Control == 1;
        var end = reverse ? score.ReadLaps(-proof.FirstLaps,-proof.SecondLaps) : proof.End;
        var read = Control == 2 ? end.AssumingEqualReaches() : end;
        for (int v = 0; v < 2; v++)
        {
            var bytes = score.VoiceBytes(v);
            var alone = score.ReadLaps(v == 0 ? 1 : 0,v == 1 ? 1 : 0);
            p.Bits($"Stream {v} · one lap", $"Local H {SignText(alone.SignSupport(0))}, V {SignText(alone.SignSupport(1))}. Each byte binds ODV, axis and reach.",
                bytes,8*bytes.Length,"voice"+v,bytes.Select((_,i)=>new BitMark(8*i,8,["voice"+v,$"v{v}p{i}"],$"Stream {v}, phase {i}")).ToArray());
        }
        p.Bits("Clock selections","Two bits per beat select stream 0, stream 1, both, or neither.",score.ClockBytes(),2*score.ClockLength,"clock");
        int total = 2*proof.Beats;
        Stepper(p,total,"Step one beat");
        var beats = BorderStudy.Trace(score,total,reverse);
        // Illustrative lengths only. Neither the proof nor the period uses these samples.
        var points = new List<(double X,double Y)> { (0,0) };
        foreach (var beat in beats)
        {
            var (x,y) = points[^1];
            foreach (var e in beat)
            {
                double length = Control != 2 && e.Move.Reach != 0 ? 0.6 : 1;
                if (e.Move.Axis == 0) x += e.Move.Sense*length;
                else y -= e.Move.Sense*length;
            }
            points.Add((x,y));
        }
        double xmin = points.Min(pt=>pt.X), ymin = points.Min(pt=>pt.Y);
        double scale = Math.Min(110,Math.Min(505/Math.Max(1,points.Max(pt=>pt.X)-xmin),152/Math.Max(1,points.Max(pt=>pt.Y)-ymin)));
        int step = Math.Min(Steps,total);
        for (int i = 0; i < total; i++)
        {
            var (x,y) = points[i]; var (nx,ny) = points[i+1];
            var key = beats[i] is [var e] ? $"v{e.Voice}p{e.Phase}" : "clock";
            p.Stroke(key,"A plotted beat combines the selected stream emissions.",100+(x-xmin)*scale,78+(y-ymin)*scale,
                100+(nx-xmin)*scale,78+(ny-ymin)*scale,i>=step);
        }
        var tip = points[step];
        p.Node("clock","Beat "+step,"Cursor in the illustrative drawing.",100+(tip.X-xmin)*scale,78+(tip.Y-ymin)*scale,true);
        p.Label("Two periods shown. Dashed strokes are the unplayed part.",350,260);
        p.Node("voice0",$"{proof.FirstLaps} first-stream lap(s)","Number of first-stream wraps in one joint period.",210,320);
        p.Node("voice1",$"{proof.SecondLaps} second-stream lap(s)","A disabled stream holds its head.",485,320);
        var last = step == 0 ? "Start" : string.Join(" + ",beats[step-1].Select(e=>$"stream {e.Voice}, cell {e.Phase}"));
        p.Events.Add($"Last beat: {last}. A shared beat has one sampled destination.");
        if (BorderStudy.TwiceClosedSweep(beats[..proof.Beats], Control==2) is long sweep)
            p.Label($"Projected return. Twice the signed sweep = {sweep} × H × V.",350,392);
        p.Result = $"First joint return: {proof.Beats} beats. {(Control==2 ? "Equal-reach prediction" : "Wired displacement")} over that period: H {SignText(read.SignSupport(0))}, V {SignText(read.SignSupport(1))}.";
        p.Boundary = "The drawing chooses illustrative lengths. Returning to a projected position alone supplies no shared ink-site identity.";
    }

    private static string MarkName(int mark) => new[]{"blank","right","up","left","down"}[mark];
    private static string WordName(ushort word) =>
        string.Join(" · ",Enumerable.Range(0,4).Select(i=>MarkName((word >> (4*i)) & 15)));

    private void ShowBorderEvidence(ExplorerPage p)
    {
        Choose(p,"Third stroke",["Inked","Pen lifted"],Variant,n=>Variant=n);
        Choose(p,"Visible evidence",["Hide third","Reveal all","Hide last two","Hide all"],Control,n=>Control=n);
        int visible = Control switch { 0 => 11, 1 => 15, 2 => 3, _ => 0 };
        var actual = BorderStudy.InkWord(BorderStudy.Trace(BorderStudy.Direct(),4),Variant == 0 ? 15 : 11);
        var evidence = BorderEvidence.Observe(actual,visible);
        var matches = BorderStudy.Predict(evidence);
        var words = matches.Select(h=>h.Word).Distinct().Order().ToArray();
        p.Bits("What the observer receives","Five possible marks per slot. A hidden slot admits all five, including blank.",
            LowBytes(evidence.Bits,3),20,"observed",Enumerable.Range(0,4).Select(i=>new BitMark(5*i,5,["observed","slot"+i],$"Slot {i+1}")).ToArray());
        p.Bits("Visible slots","Bit i admits the observer to slot i.",[(byte)visible],4,"observed");
        p.Label("Observed ink in four successive slots",352,52);
        for (int i = 0; i < 4; i++)
        {
            int field = evidence.Field(i);
            var labels = Enumerable.Range(0,5).Where(m=>(field & (1<<m))!=0).Select(MarkName).ToArray();
            p.Node("slot"+i,labels.Length==1 ? labels[0] : "?", "Compatible: "+string.Join(", ",labels),105+166*i,111,labels.Length==1,labels.Length!=1);
        }
        p.Label("Possible next repeat under the square-wave family",350,192);
        for (int i = 0; i < Math.Min(4,words.Length); i++) p.Label(WordName(words[i]),350,231+28*i);
        if (words.Length == 0) p.Label("No compatible family member",350,245);
        if (words.Length > 4) p.Label($"+ {words.Length-4} other whole words",350,351);
        p.Result = $"{evidence.CompletionCount()} raw completions. Family prediction: {words.Length} distinct repeat(s), {matches.Length} transform witness(es).";
        p.Boundary = "Predictions share one phase, rotation, reflection and traversal throughout. Revealing a real blank can reject the whole family.";
    }

    private void ShowLinearLinks(ExplorerPage p)
    {
        Choose(p,"Length binding",["Separate reaches","Shared reach"],Variant,n=>Variant=n);
        byte b = Variant == 0 ? (byte)17 : (byte)1;
        var score = new BorderScore([1,1,1],[b,b],3);
        var difference = score.ReadLaps(1,-1);
        p.Bits("Three forward links","All three use reach A on the positive horizontal carrier.",score.VoiceBytes(0),24,"aa");
        p.Bits("Two forward links","Their high nibbles select a reach in the same carrier basis.",score.VoiceBytes(1),16,"bb");
        for (int i = 0; i < 3; i++) p.Node("aa","A","One positive link from the first chain.",155+180*i,115,true);
        for (int i = 0; i < 2; i++) p.Node("bb",Variant==0 ? "B" : "A","One positive link from the second chain.",155+180*i,233,true);
        p.Label("Each box is one use. Its screen width carries no length evidence.",350,322);
        p.Result = $"Wired difference: {SignText(difference.SignSupport(0))}. Assuming equal links: {SignText(difference.AssumingEqualReaches().SignSupport(0))}.";
        p.Boundary = "Both chains go forward on one straight carrier. The equal-link view can be evaluated without changing either source.";
    }

    private void ShowBorderCoverage(ExplorerPage p)
    {
        Choose(p,"Pen",["All inked","Lift on right","Lift on down"],Variant,n=>Variant=n);
        Choose(p,"Reader",["Observe all","Hide second beat"],Control,n=>Control=n);
        Choose(p,"Lane spacing",["At limit","Beyond limit","Unobserved"],B,n=>B=n);
        var coverage = new BorderCoverage(BorderStudy.Trace(BorderStudy.Direct(),4),Variant switch { 1=>13u, 2=>11u, _=>15u },Control==0 ? 15u : 13u);
        var spacing = BorderStudy.LaneSpacing(B == 2 ? null : new Observation(B==0 ? (byte)1 : (byte)2));
        var wide = spacing.CompareTips().Test(2);
        Stepper(p,2,"Try side outlet");
        var side = new GuardedOffer(PresencePorts.Any(coverage,wide));
        for (int i = 0; i < Math.Min(2,Steps); i++) side.Request();
        ShowRailBits(p,coverage.Required,"Requested span","required",100);
        ShowRailBits(p,coverage.Certain,"Certain ink","ink",170);
        ShowRailBits(p,coverage.DefiniteWhite,"Definite white","white",240);
        p.Bits("Possible white","The required span minus certain ink.",LowBytes(coverage.PossibleWhite.Bits,1),5,"white");
        p.Bits("Spacing vs limit","On a separate normal rail the two spans read lane separation and allowed spacing.",
            [spacing.CompareTips().ReadPairs()],4,"spacing");
        p.Label("Spacing beyond the limit: "+Answer(wide.ReadSupport()),350,295);
        p.Node("branch",side.Receiver!=0 ? "Request received" : "Inbox empty","The response circuit supplies its own pulse.",350,346,side.Receiver!=0);
        p.Bits("Branch inbox","Receiver gate, inbox and spare site.",[side.Receiver],3,"branch");
        static string Answer(byte support) => support switch { 1=>"no", 2=>"yes", _=>"open" };
        p.Result = $"Uncovered H interval: {Answer(coverage.ReadSupport())}. Lanes too far apart: {Answer(wide.ReadSupport())}.";
        p.Boundary = "Either definite gap can release one locally supplied pulse down the prewired side outlet. The motor score stays the same.";
    }

    private static void ShowRailBits(ExplorerPage p, OrdinalInk span, string title, string key, int y)
    {
        int count = span.Rail.Count, width = 2*count-1;
        p.Bits(title,"Low bits alternate named stops and open gaps.",LowBytes(span.Bits,(width+7)/8),width,key);
        p.Label(title,98,y);
        for (int i = 0; i < count-1; i++)
        {
            double x = 215+i*112;
            bool occupied = (span.Bits & (2UL << (2*i))) != 0;
            p.Stroke(key,occupied ? "Occupied open gap" : "Unoccupied open gap",x,y+8,x+112,y+8,!occupied,occupied);
        }
        for (int i = 0; i < count; i++)
            p.Node(key,((char)('P'+i)).ToString(),"Point participation is a separate bit.",215+i*112,y+8,(span.Bits & (1UL<<(2*i)))!=0);
    }

    private void ShowBooleanSpans(ExplorerPage p)
    {
        Choose(p,"Operands",["Overlap","Touch","Apart"],Variant,n=>Variant=n);
        Choose(p,"Read law",["Intersection","Union","A minus B","Exclusive"],Control,n=>Control=n);
        var rail = new LandmarkRun(2,[2,0,3,1]);
        var a = OrdinalInk.Closed(rail,2,Variant==0 ? 3 : 0);
        var b = OrdinalInk.Closed(rail,Variant==2 ? 3 : 0,1);
        byte[] laws = [8,14,2,6];
        var answer = a.Combine(b,laws[Control])!;
        ShowRailBits(p,a,"Span A","first",85);
        ShowRailBits(p,b,"Span B","second",175);
        ShowRailBits(p,answer,"Result","result",265);
        p.Bits("Boolean law","One truth bit per pair of local occupancies.",[laws[Control]],4,"result");
        p.Result = $"Result {answer.Bits:X2}. Contains an open interval: {answer.ContainsInterval}.";
        p.Boundary = "The result can feed another operation on the same rail. Solid spans and occupied points are separately inspectable.";
    }
}
