using GojoDraw;
using SkiaSharp;

namespace Gojo.Blocks;

internal sealed record BlocksTheme(
    string Id,
    string Label,
    SKColor Background,
    SKColor RailFill,
    SKColor PanelFill,
    SKColor PageFill,
    SKColor PageStroke,
    SKColor Ink,
    SKColor Muted,
    SKColor Accent,
    SKColor SoftLine,
    GojoCadSkiaTheme CadTheme)
{
    public static IReadOnlyList<BlocksTheme> All { get; } =
    [
        new(
            "warm",
            "Warm",
            Background: new SKColor(248, 247, 243),
            RailFill: new SKColor(236, 235, 229),
            PanelFill: new SKColor(241, 240, 234),
            PageFill: new SKColor(255, 255, 252),
            PageStroke: new SKColor(215, 217, 210),
            Ink: new SKColor(47, 55, 65),
            Muted: new SKColor(105, 112, 120),
            Accent: new SKColor(66, 122, 153),
            SoftLine: new SKColor(197, 200, 196),
            CadTheme: GojoCadSkiaTheme.Tutorial),
        new(
            "graphite",
            "Graphite",
            Background: new SKColor(34, 38, 42),
            RailFill: new SKColor(45, 50, 55),
            PanelFill: new SKColor(39, 44, 49),
            PageFill: new SKColor(29, 33, 37),
            PageStroke: new SKColor(84, 91, 98),
            Ink: new SKColor(230, 233, 229),
            Muted: new SKColor(167, 174, 181),
            Accent: new SKColor(118, 174, 194),
            SoftLine: new SKColor(75, 82, 89),
            CadTheme: GojoCadSkiaTheme.Graphite)
    ];

    public static BlocksTheme At(int index) =>
        All[Math.Clamp(index, 0, All.Count - 1)];

    public static int NextIndex(int index) =>
        All.Count == 0 ? 0 : (index + 1) % All.Count;
}
