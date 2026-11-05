using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.PrimitiveExplorer;

public sealed partial class ExplorerSession
{
    // The names explain the selected experiment; they never select its answer.
    // A page read observes a retained result and cannot run the experiment.
    private InvarianceResult? invarianceResult;
    private int invarianceCase = -1, invarianceVariation = -1;

    private ExplorerPage InvariancePage()
    {
        if (invarianceCase != Mode || invarianceVariation != Variant)
        {
            invarianceResult = null;
            invarianceCase = Mode;
            invarianceVariation = Variant;
        }
        var selected = InvarianceExperiments.Cases[Mode];
        var p = new ExplorerPage("Small concepts under change",
            "Try a change, challenge the correspondence, then conceal a read. Compare the two observations: agreement can preserve either a true or a false read. Each concept sentence names a candidate meaning; its limit states what remains unproved.",
            "InvarianceExperiments.md")
        {
            DrawingGuide = (selected.Name, $"Concept: {selected.Concept}\nLimit: {selected.Limit}")
        };
        Choose(p, "Concept experiment", InvarianceExperiments.Cases.Select(item => item.Name).ToArray(), Mode,
            value => { Mode = value; Variant = 0; invarianceResult = null; }, true);
        Choose(p, "Change to try", [selected.PreservingChange, selected.ChallengingChange, "Conceal a required read"], Variant,
            value => { Variant = value; invarianceResult = null; });
        p.Actions.Add(new("Apply change", () =>
        {
            invarianceResult = InvarianceExperiments.Run(Mode, Variant);
            invarianceCase = Mode;
            invarianceVariation = Variant;
            Steps++;
            Refresh();
        }));
        p.Actions.Add(new("Reset", () => { invarianceResult = null; Steps = 0; Refresh(); }));

        InvarianceDrawing(p, invarianceResult);
        p.Boundary = "The names are supplied descriptions, not learned categories. Supported correspondence does not itself prove the concept true or cover every change. These sketches show endpoint connections with arbitrary spacing and angles. Losing a guarantee is different from proving its opposite.";
        if (invarianceResult is not { } result)
        {
            p.Result = "Not run yet. Choose a change and press Apply change to obtain an actual comparison.";
            p.Status = "No pair of observations has been captured.";
            p.Events.Add(Variant switch { 0 => selected.PreservingChange, 1 => selected.ChallengingChange, _ => "The experiment will withhold a required observation from its comparison." });
            p.Events.Add("Reading this page or selecting a change does not execute it.");
            return p;
        }

        var readBasis = Mode is 5 or 7
            ? "Bit w admits the complete three-material-role pattern w. Its three bits are site presence, not O/D/V. The control is a separate role."
            : Mode == 8 ? "Bit w admits ODV word w in the selected frame."
            : "Bit 0 admits false and bit 1 admits true for the stated local property. This is a read, not material.";
        p.Bits("Observation · first", readBasis + " " + Words(result.BeforeSupport), [result.BeforeSupport], 8, "before");
        p.Bits("Observation · second", readBasis + " " + Words(result.AfterSupport), [result.AfterSupport], 8, "after");
        p.Bits("Prediction · corresponding second", "The law's admitted true-correspondence tuples map the first observation to these possible second codes. This predicts the required correspondence; it does not choose the actual second outcome. " + Words(result.ExpectedAfterSupport),
            [result.ExpectedAfterSupport], 8, "expected");
        p.Rows("Connection · comparison", "[First observation, second observation, calculated answer]. These references can compare one source across a change or two separately constructed apparatus through an explicit correspondence.",
            result.ReferenceBits, [result.Wiring], "wiring", roleKeys: [["before", "after", "verdict"]], actual: result.RowBytes);
        p.Bits("Law · admitted correspondence", "Each set bit admits one [first word, second word, answer] tuple. The law can require the same word or a specific transformed word. The concept label is not an instruction.",
            result.LawBytes, result.LawBytes.Length * 8, "law");
        p.Bits("Read · correspondence answer", "01 = mismatch; 10 = supported; 11 = unresolved; 00 = no compatible observation. Matching sets of possible values do not by themselves establish matching actual outcomes.",
            [result.VerdictSupport], 2, "verdict");
        p.Bits("Witness · first source excerpt", "Selected bytes from the first source: observations, bindings or topology as described in the notes. This is a byte excerpt, not a complete restorable snapshot; padding is not extra participation.",
            result.BeforeBytes, result.BeforeBytes.Length * 8, "beforeartifact");
        p.Bits("Witness · comparison source excerpt", "Selected bytes from the changed source or separately constructed comparison apparatus. These bytes expose evidence; the comparator receives its connected public observations.",
            result.AfterBytes, result.AfterBytes.Length * 8, "afterartifact");
        if (result.RetainedPattern is { } pattern)
        {
            var encoded = pattern.Encode();
            p.Bits("Reusable · passage pattern", "A restored structure: version, role count, then ordered records of eight law bytes and packed role references. It retains no values, glyph name or original builder.",
                encoded, encoded.Length * 8, "pattern");
            p.Rows("Application · bound roles", "Each row maps a retained pattern role to a published local port of the receiving construction. Binding does not copy material.",
                OrderedBoundary.Bits(result.PatternRolePorts.Max() + 1),
                result.PatternRolePorts.Select((port, role) => new[] { role, port }).ToArray(), "pattern");
        }
        p.Result = $"{InvarianceVerdict(result.VerdictSupport)}. {result.Observation}";
        p.Status = "One fresh trial · public observations → correspondence law → reusable read";
        p.Events.Add(result.Change);
        p.Events.AddRange(result.Details);
        return p;
    }

    private static string InvarianceVerdict(byte support) => support switch
    {
        1 => "Correspondence does not hold",
        2 => "Correspondence supported",
        3 => "Correspondence unresolved",
        _ => "No compatible observation"
    };

    private static void InvarianceDrawing(ExplorerPage p, InvarianceResult? result)
    {
        var waiting = result is null;
        var uncertain = waiting || result!.VerdictSupport is 0 or 3;
        var supported = result?.VerdictSupport == 2;
        p.Label("SOURCE CONNECTIONS · SCHEMATIC", 350, 15);
        p.Edge("beforeartifact", "capture", "The first public observation comes from the original source or reference apparatus.", 160, 160, 160, 197);
        p.Edge("afterartifact", "capture", "The second observation comes from the changed source or separately constructed comparison apparatus.", 540, 160, 540, 197);
        p.Edge("change", "change / compare", "The trial either changes one connected source or compares separately constructed apparatus through explicit role correspondence. This arrow does not claim one construction physically became the other.",
            245, 95, 455, 95, active: !waiting, uncertain: waiting);
        var sites = result is null ? [] : result.BeforeLinks.Concat(result.AfterLinks).SelectMany(row => row).Distinct().Order().ToArray();
        InvarianceSketch(p, "beforeartifact", "First source", result?.BeforeLinks ?? [], sites, 160, 95);
        InvarianceSketch(p, "afterartifact", "Comparison source", result?.AfterLinks ?? [], sites, 540, 95);
        if (result?.RetainedPattern is { } retained)
            p.Node("pattern", $"{retained.StepCount} retained passages", "The complete passage pattern is restored and bound through local roles. Hover to inspect the actual retained bytes and application mapping.", 350, 170);
        p.Node("before", waiting ? "First read · ?" : "First · " + InvarianceReadLabel(result!.BeforeSupport),
            waiting ? "No observation has been captured yet." : "The comparator receives this published support: " + Words(result!.BeforeSupport),
            160, 220, uncertain: waiting || !ReadSupport.TrySingle(result!.BeforeSupport, out _));
        p.Node("after", waiting ? "Second read · ?" : "Second · " + InvarianceReadLabel(result!.AfterSupport),
            waiting ? "No comparison observation has been captured yet." : "The comparator receives this published support: " + Words(result!.AfterSupport),
            540, 220, uncertain: waiting || !ReadSupport.TrySingle(result!.AfterSupport, out _));
        p.Edge("wiring", "first", "First input role of the actual comparison row.", 185, 243, 320, 310, uncertain: waiting);
        p.Edge("wiring", "second", "Second input role of the actual comparison row.", 515, 243, 380, 310, uncertain: waiting);
        if (result is not null)
            p.Node("expected", "Expected · " + InvarianceReadLabel(result.ExpectedAfterSupport), "What the law predicts for the second read from the first. Compare this with the actual second read; equal candidate sets alone can still leave correspondence unresolved.",
                350, 267, uncertain: !ReadSupport.TrySingle(result.ExpectedAfterSupport, out _));
        p.Node("law", "Correspondence law", "The stored allowed-tuple bits specify which pairs of words correspond. No noun or shape name selects the result.",
            350, 330, active: !waiting, uncertain: waiting);
        p.Edge("verdict", "read", "The answer is another public, non-draining read. A later structure can connect to it without reading the source internals.",
            350, 353, 350, 407, active: supported, uncertain: uncertain);
        p.Node("verdict", waiting ? "No comparison yet" : result!.VerdictSupport switch
            { 1 => "01 · mismatch", 2 => "10 · supported", 3 => "11 · unresolved", _ => "00 · no observation" },
            waiting ? "Apply the change to produce a witness." : result!.Observation,
            350, 430, active: supported, uncertain: uncertain);
        p.Label("SAME READ = INVARIANCE  ·  CORRESPONDING CHANGE = EQUIVARIANCE", 350, 465);
    }

    private static string InvarianceReadLabel(byte support) => support == 0 ? "no admitted word" :
        ReadSupport.TrySingle(support, out var word) ? Word(word) : $"{ReadSupport.Words(support).Length} possibilities";

    private static void InvarianceSketch(ExplorerPage p, string key, string title, int[][] rows, int[] sites, double x, double y)
    {
        if (rows.Length == 0)
        {
            p.Node(key, title, "No endpoint sketch is supplied for this source read. Selected source bytes remain inspectable after the trial.", x, y);
            return;
        }
        // Site numbers are local references, not coordinates. Both sketches
        // use the same presentation placement so changes can be compared.
        (double X, double Y) Position(int site)
        {
            var index = Array.IndexOf(sites, site);
            var angle = -Math.PI / 2 + index * 2 * Math.PI / sites.Length;
            return (x + 46 * Math.Cos(angle), y + 46 * Math.Sin(angle));
        }
        p.Label(title, x, 32);
        for (var index = 0; index < rows.Length; index++)
        {
            var row = rows[index]; var from = Position(row[0]); var to = Position(row[1]);
            var detail = $"Source row {index}: site {row[0]} → site {row[1]}. Arrow order comes from the actual endpoint row; screen distance and angle have no encoded meaning.";
            if (row[0] == row[1]) p.Circle(key, detail, from.X + 10, from.Y - 10, 13);
            else p.Edge(key, "", detail, from.X, from.Y, to.X, to.Y, active: true);
        }
        foreach (var site in rows.SelectMany(row => row).Distinct())
        {
            var at = Position(site);
            p.Circle(key, $"Local site {site}. Repeated references name this same endpoint in the sketch.", at.X, at.Y, 4);
            var outwardX = (at.X - x) / 46; var outwardY = (at.Y - y) / 46;
            p.Label("s" + site, at.X + outwardX * 15, at.Y + outwardY * 13 - 5);
        }
    }
}
