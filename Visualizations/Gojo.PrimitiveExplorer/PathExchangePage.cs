using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.PrimitiveExplorer;

public sealed partial class ExplorerSession
{
    private static string PathVerdict(byte support) => support switch
    {
        1 => "broken", 2 => "continuous", 3 => "unresolved", _ => "no observation"
    };

    private ExplorerPage PathExchangePage()
    {
        var p = new ExplorerPage("Two private paths, one public loop",
            "Start with two checked paths. Advance once to edit B. Each later beat gives B one inspection and asks the host for its current answer.", "PathExchange.md");
        Choose(p, "Owner storage", ["Both ordinary pairs", "Both vertex chains", "A pairs / B chain"], Mode, n => Mode = n);
        Choose(p, "Edit on first beat", ["Reverse B’s third step", "Reverse, then repair on beat 6",
            "Change B’s middle D bit", "Attach a side exit at B2", "Try packing B’s route"], Variant, n => Variant = n);
        Choose(p, "Return seam", ["Missing", "Joined"], Control, n => Control = n);
        Stepper(p, 48);

        var a = PathExchangeExperiment.MakePath(4, Mode == 1);
        var b = PathExchangeExperiment.MakePath(4, Mode != 0);
        var loop = PathExchangeExperiment.Join(a, b, Control == 1);
        var cold = a.Advance(4) + b.Advance(4);
        var firstChecks = b.Inspections;
        var packedChecks = 0;
        for (var beat = 1; beat <= Steps; beat++)
        {
            if (beat == 1)
            {
                switch (Variant)
                {
                    case 0: case 1: b.ReverseStep(2); break;
                    case 2: b.ObserveSite(2, 3); break;
                    case 3: b.Attach(2, 5); break;
                    case 4: b.TryPackChain(out packedChecks); break;
                }
            }
            else if (Variant == 1 && beat == 6) b.ReverseStep(2);
            else b.Advance(1);
            var state = loop.ReadSupport();
            p.Events.Add($"Beat {beat}: B {PathVerdict(b.Passage.ReadSupport())} · loop {LoopVerdict(state)}");
        }
        var result = loop.ReadSupport();
        p.Rows("Host seam rows", "Public roles are A.in=0, A.out=1, B.in=2 and B.out=3.",
            2, Control == 1 ? [[1, 2], [3, 0]] : [[1, 2]], "seams", actual: loop.SeamBytes());
        SupportBlock(p, "A", "A’s public continuity read", a.Passage.ReadSupport());
        SupportBlock(p, "B", "B’s public continuity read", b.Passage.ReadSupport());
        SupportBlock(p, "host", "Host’s loop read", result);
        var rows = b.RowArity == 1 ? b.StepCount + 1 : b.StepCount;
        var marks = new List<BitMark>();
        for (var i = 0; i < rows; i++)
        for (var j = 0; j < b.RowArity; j++)
        {
            var keys = new List<string> { "topology" };
            if (i < b.StepCount) keys.Add("edge" + i);
            if (b.RowArity == 1 && i > 0) keys.Add("edge" + (i - 1));
            marks.Add(new((i * b.RowArity + j) * b.SiteBits, b.SiteBits, keys.ToArray(), $"Row {i}, field {j} of B's authored topology"));
        }
        p.Bits("Inside B · authored topology", b.RowArity == 1 ? "Each step uses two consecutive site references. Its neighbour reads the same field at the join." :
            "Each pair owns its departure and arrival references. Consecutive pairs need a join check.", b.TopologyBytes(), rows * b.RowArity * b.SiteBits, "topology", marks.ToArray());
        p.Rows("Inside B · selected cuts", "Each row selects the edges between two cut positions. Decreasing cuts reverse that traversal.",
            b.CutBits, Enumerable.Range(0, b.RunCount).Select(b.Run).ToArray(), "cuts", actual: b.RunBytes());
        p.Bits("Inside B · site observations", "Six two-bit entries. Fixed O comes from the bucket. Each entry owns V then D.", b.ValueBytes(), b.SiteCount * 2, "values",
            Enumerable.Range(0, b.SiteCount).Select(i => new BitMark(i * 2, 2, ["site" + i], $"B site {i}: V then D")).ToArray());
        if (b.BranchCount > 0)
            p.Rows("Inside B · side attachment", "Separate pair attached to an existing interior site.", b.SiteBits, [b.Branch(0)], "branch", actual: b.BranchBytes());

        p.Node("A", "A · continuous", "A publishes a current inlet-to-outlet claim from its own tables.", 130, 95, true);
        p.Node("B", "B · " + PathVerdict(b.Passage.ReadSupport()), "This port is sampled again after every edit. Its owner retains the inspection cursor.", 570, 95, b.Passage.ReadSupport() == 2, b.Passage.ReadSupport() == 3);
        p.Edge("seams", "A.out → B.in", "Host row [1,2]", 210, 80, 470, 80, true);
        if (Control == 1) p.Edge("seams", "B.out → A.in", "Host row [3,0]", 470, 120, 210, 120, true);
        p.Node("host", "Loop · " + LoopVerdict(result), "The host reads the two public claims and its two ordered seams.", 350, 180, result == 2, result == 3);
        p.Label("Inside B · selected traversal", 350, 255);
        static double X(int site) => 75 + 135 * site;
        foreach (var edge in b.SelectedSteps())
        {
            var key = "edge" + edge.Edge;
            var dx = Math.Sign(edge.To - edge.From) * 20;
            p.Edge(key, $"{edge.From} → {edge.To}", $"Occurrence {edge.Edge}, traversed from B{edge.From} to B{edge.To}.",
                X(edge.From) + dx, 320, X(edge.To) - dx, 320, true);
        }
        for (var i = 0; i <= 4; i++) p.Node("site" + i, "B" + i, $"Private site {i}. Its observation and route reference occupy separate arrays.", X(i), 320, i == 2 && Steps > 0 && Variant == 2);
        if (b.BranchCount > 0)
        {
            p.Edge("branch", "side exit", "B2 is referenced by both the main route and a separate connection.", X(2), 345, X(2), 390);
            p.Node("site5", "B5", "Separate destination.", X(2), 410);
        }
        p.Result = $"Loop {LoopVerdict(result)} · initial checks {cold} · B inspections since edit {b.Inspections - firstChecks} · pairs checked for packing {packedChecks}.";
        p.Boundary = "The host checks one selected walk. A branch adds an available connection. Choosing which route receives flow needs a passage law.";
        return p;
    }

    private static string LoopVerdict(byte support) => support == 2 ? "closed" : PathVerdict(support);
}
