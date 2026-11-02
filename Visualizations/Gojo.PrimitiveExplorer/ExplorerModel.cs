using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.PrimitiveExplorer;

// Presentation snapshots only. All bytes, predictions and settlements below
// come from the experiment. Screen positions and labels are never kernel data.
public sealed record BitMark(int Offset, int Width, string[] Keys, string Label);
public sealed record EncodingBlock(string Title, string Note, byte[] Bytes, int UsedBits,
    BitMark[] Marks, string Key);
public sealed record DrawingItem(string Kind, string Key, string Label, string Detail,
    double X, double Y, double X2 = 0, double Y2 = 0, bool Active = false, bool Uncertain = false);
public sealed record Choice(string Label, string[] Values, int Selected, Action<int> Change);
public sealed record ExplorerAction(string Label, Action Run);

public sealed class ExplorerPage(string title, string intro, string source)
{
    public string Title { get; } = title;
    public string Intro { get; } = intro;
    public string Source { get; } = source;
    public string Result { get; set; } = "";
    public string Boundary { get; set; } = "";
    public string Status { get; set; } = "Executable experiment";
    public List<EncodingBlock> Blocks { get; } = [];
    public List<DrawingItem> Drawing { get; } = [];
    public List<Choice> Choices { get; } = [];
    public List<ExplorerAction> Actions { get; } = [];
    public Dictionary<string, string> Details { get; } = [];
    public List<string> Events { get; } = [];

    public void Node(string key, string label, string detail, double x, double y, bool active = false, bool uncertain = false)
    {
        Drawing.Add(new("node", key, label, detail, x, y, Active: active, Uncertain: uncertain));
        Details[key] = detail;
    }
    public void Edge(string key, string label, string detail, double x, double y, double x2, double y2, bool active = false, bool uncertain = false)
    {
        Drawing.Add(new("edge", key, label, detail, x, y, x2, y2, active, uncertain));
        Details[key] = detail;
    }
    public void Label(string label, double x, double y) => Drawing.Add(new("label", "", label, "", x, y));
    public void Circle(string key, string detail, double x, double y, double radius, bool uncertain = false)
    {
        Drawing.Add(new("circle", key, "", detail, x, y, radius, Uncertain: uncertain)); Details[key] = detail;
    }
    public void Bits(string title, string note, byte[] bytes, int used, string key, params BitMark[] marks)
    {
        Blocks.Add(new(title, note, bytes, used, marks.Length == 0 ? [new(0, used, [key], title)] : marks, key));
        Details.TryAdd(key, note);
    }
    public void Rows(string title, string note, int bits, int[][] rows, string key, string[][]? roleKeys = null, byte[]? actual = null, string[]? rowKeys = null)
    {
        var packed = new EndpointRows();
        foreach (var row in rows) packed.Append(bits, row);
        var bytes = rows.Length == 0 ? [] : packed.PackedBytes(rows[0].Length, bits);
        if (actual is not null && !bytes.SequenceEqual(actual)) throw new InvalidOperationException($"Chart differs from actual rows: {title}");
        var marks = new List<BitMark>(); var offset = 0;
        for (var i = 0; i < rows.Length; i++)
            for (var j = 0; j < rows[i].Length; j++)
            {
                var role = roleKeys?[i][j] ?? key;
                marks.Add(new(offset, bits, [key, rowKeys?[i] ?? key, role], $"row {i}, role {j} → local reference {rows[i][j]}")); offset += bits;
            }
        Bits(title, note + "  " + string.Join(" · ", rows.Select(r => "[" + string.Join(",", r) + "]")), bytes, offset, key, marks.ToArray());
    }
}

public sealed class Observation(byte support) : IPrimitiveReadPort
{
    public byte Support { get; set; } = support;
    public byte ReadSupport() => Support;
}

public sealed partial class ExplorerSession
{
    public static readonly string[] Chapters = ["Three bits", "Prediction & shared flow", "Storage & ports", "Read laws & Boolean logic",
        "Refinement & correspondence", "Retained opposition", "Memory at zero", "Structural potential", "Passages & chains",
        "Conditionals", "Saturation & outlets", "Rectangle constraints", "Circles & polygons", "Repetition & stopping", "Composed patterns",
        "Closure & boundaries", "Cycles: claims & flow", "Work & read budgets", "Two communicating paths"];
    public int Chapter { get; private set; }
    public int A = 1, B = 3, Mode, Control = 1, Variant, Steps;
    public event Action? Changed;
    public void Navigate(int chapter) { Chapter = Math.Clamp(chapter, 0, Chapters.Length - 1); A = 1; B = Chapter == 3 ? 1 : 3; Mode = Variant = Steps = 0; Control = 1; Changed?.Invoke(); }
    public void Refresh() => Changed?.Invoke();
    public ExplorerPage Read() => Chapter switch
    {
        0 => WordPage(), 1 => PredictionPage(), 2 => StoragePage(), 3 => LawPage(), 4 => RefinementPage(),
        5 => OppositionPage(), 6 => MemoryPage(), 7 => PotentialPage(), 8 => PassagePage(false), 9 => PassagePage(true),
        10 => SaturationPage(), 11 => RectanglePage(), 12 => ContourPage(), 13 => RepetitionPage(), 14 => CompositionPage(),
        15 => BoundaryPage(), 16 => CyclePage(), 17 => WorkPage(), _ => PathExchangePage()
    };
    private void Choose(ExplorerPage p, string label, string[] choices, int selected, Action<int> change) =>
        p.Choices.Add(new(label, choices, selected, n => { change(n); Steps = 0; Refresh(); }));
    private void Stepper(ExplorerPage p, int maximum = 40, string label = "Advance one beat")
    {
        p.Actions.Add(new(label, () => { Steps = Math.Min(maximum, Steps + 1); Refresh(); }));
        p.Actions.Add(new("Reset", () => { Steps = 0; Refresh(); }));
    }
    public static string Word(int w) => Convert.ToString(w, 2).PadLeft(3, '0');
    public static string Hex(IEnumerable<byte> bytes) => string.Join(" ", bytes.Select(b => b.ToString("X2")));
    private static string Words(byte support) => support == 0 ? "no admitted result" : string.Join(" or ", ReadSupport.Words(support).Select(w => Word(w)));
    private static string[] WordChoices => Enumerable.Range(0, 8).Select(w => $"{Word(w)} · {(w < 4 ? "right" : "left")} / {((w & 2) == 0 ? "+" : "−")} / {((w & 1) == 0 ? "zero" : "nonzero")}").ToArray();
    private static byte Support(int selection) => selection switch { 0 => 1, 1 => 2, 2 => 3, _ => 0 };
    private static readonly string[] Supports = ["Absent · 000", "Present · 001", "Uncertain · {000,001}", "Missing · no observation"];
    private static void SupportBlock(ExplorerPage p, string key, string title, byte support) => p.Bits(title,
        $"READ RESULT, not a value. Bit w admits ODV word w. {Words(support)}.", [support], 8, key);
    private static void StateBlock(ExplorerPage p, string title, byte[] bytes, string[] keys) => p.Bits(title,
        "STORED V bits; O=0 and D=0 come from array position. Bit order follows local site order.", bytes, keys.Length, "state",
        keys.Select((k, i) => new BitMark(i, 1, [k, "state"], $"site {i}: {k}")).ToArray());
}
