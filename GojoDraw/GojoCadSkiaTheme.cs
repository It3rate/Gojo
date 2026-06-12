using SkiaSharp;

namespace GojoDraw;

/// <summary>
/// Render-boundary paint factory for repeated GojoCAD visual treatments.
/// This is display style only; Gojo structure still owns meaning.
/// </summary>
public sealed record GojoCadSkiaTheme(
    SKColor Ink,
    SKColor Muted,
    SKColor Structure,
    SKColor Flow,
    SKColor Tension,
    SKColor Fill,
    SKColor AntiFill,
    SKColor Selection,
    SKColor Residue,
    SKColor HoverFill)
{
    public static GojoCadSkiaTheme Default { get; } = new(
        Ink: new SKColor(31, 45, 61),
        Muted: new SKColor(95, 103, 116, 150),
        Structure: new SKColor(42, 91, 143),
        Flow: new SKColor(63, 113, 83),
        Tension: new SKColor(199, 45, 47),
        Fill: new SKColor(71, 126, 94, 34),
        AntiFill: new SKColor(226, 50, 55, 58),
        Selection: new SKColor(115, 76, 155),
        Residue: new SKColor(151, 92, 52),
        HoverFill: new SKColor(255, 255, 255, 235));

    public static GojoCadSkiaTheme Tutorial { get; } = new(
        Ink: new SKColor(47, 55, 65),
        Muted: new SKColor(104, 112, 120, 160),
        Structure: new SKColor(64, 104, 142),
        Flow: new SKColor(49, 132, 104),
        Tension: new SKColor(195, 63, 61),
        Fill: new SKColor(54, 132, 102, 42),
        AntiFill: new SKColor(209, 64, 76, 62),
        Selection: new SKColor(115, 82, 150),
        Residue: new SKColor(183, 111, 57),
        HoverFill: new SKColor(255, 255, 252, 238));

    public static GojoCadSkiaTheme Graphite { get; } = new(
        Ink: new SKColor(230, 233, 229),
        Muted: new SKColor(154, 164, 172, 170),
        Structure: new SKColor(118, 174, 194),
        Flow: new SKColor(106, 184, 145),
        Tension: new SKColor(222, 104, 101),
        Fill: new SKColor(106, 184, 145, 54),
        AntiFill: new SKColor(222, 104, 101, 68),
        Selection: new SKColor(183, 145, 220),
        Residue: new SKColor(226, 165, 94),
        HoverFill: new SKColor(39, 44, 49, 240));

    public GojoCadSkiaPaints CreatePaints()
    {
        var paints = new GojoCadSkiaPaints
        {
            RadialInner = WithAlpha(Tension, 78),
            RadialOuter = WithAlpha(Tension, 0),
            AreaFillPalette =
            [
                WithAlpha(Fill, 46),
                WithAlpha(Structure, 38),
                WithAlpha(Selection, 36),
                WithAlpha(Residue, 42)
            ]
        };

        paints.Dot.Color = Ink;
        paints.Segment.Color = Structure;
        paints.SegmentTension.Color = WithAlpha(Tension, 190);
        paints.AngleControlledSegment.Color = Tension;
        paints.AngleControlledSegmentTension.Color = WithAlpha(Tension, 205);
        paints.SharedFrameSegment.Color = Selection;
        paints.SharedFrameCurve.Color = Selection;
        paints.SharedFrameCurveTension.Color = WithAlpha(Selection, 190);
        paints.SharedFramePoint.Color = WithAlpha(Selection, 190);
        paints.DirectedSegment.Color = Flow;
        paints.DirectedSegmentTension.Color = WithAlpha(Flow, 190);
        paints.Circumference.Color = Muted;
        paints.CircumferenceTension.Color = WithAlpha(Muted, 140);
        paints.CircumferenceSpan.Color = WithAlpha(Structure, 155);
        paints.CircumferenceSpanTension.Color = WithAlpha(Tension, 150);
        paints.EndpointBar.Color = WithAlpha(Muted, 175);
        paints.Partition.Color = WithAlpha(Ink, 165);
        paints.Basis.Color = WithAlpha(Ink, 185);
        paints.LineSeed.Color = WithAlpha(Selection, 145);
        paints.LineSeedTension.Color = WithAlpha(Tension, 135);
        paints.SheetSeed.Color = WithAlpha(Structure, 28);
        paints.SheetSeedEdge.Color = WithAlpha(Structure, 85);
        paints.SheetSeedEdgeTension.Color = WithAlpha(Tension, 95);
        paints.RadiusAttachment.Color = WithAlpha(Residue, 135);
        paints.RadiusAttachmentTension.Color = WithAlpha(Tension, 145);
        paints.AreaFill.Color = Fill;
        paints.AntiAreaFill.Color = AntiFill;
        paints.AreaBoundary.Color = WithAlpha(Flow, 130);
        paints.AreaSettlementPartition.Color = WithAlpha(Tension, 210);
        paints.BranchNode.Color = WithAlpha(Flow, 195);
        paints.MergeNode.Color = WithAlpha(Structure, 195);
        paints.PathGraphTerminalNode.Color = WithAlpha(Muted, 155);
        paints.PathGraphBranchNode.Color = WithAlpha(Residue, 195);
        paints.HoverBackground.Color = HoverFill;
        paints.HoverBorder.Color = WithAlpha(Ink, 210);
        paints.HoverText.Color = Ink;

        return paints;
    }

    private static SKColor WithAlpha(SKColor color, byte alpha) =>
        new(color.Red, color.Green, color.Blue, alpha);
}
