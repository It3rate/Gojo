using SkiaSharp;

namespace GojoDraw;

/// <summary>
/// Temporary render-boundary paints until GojoCAD has style participation
/// surfaces for color, radius, and width.
/// </summary>
public sealed class GojoCadSkiaPaints : IDisposable
{
    public GojoCadSkiaPaints()
    {
        Dot = Fill(new SKColor(31, 45, 61));
        Segment = Stroke(new SKColor(42, 91, 143), 2.5f);
        SegmentTension = DashedStroke(new SKColor(42, 91, 143, 185), 2.2f);
        AngleControlledSegment = Stroke(new SKColor(199, 45, 47), 3f);
        AngleControlledSegmentTension = DashedStroke(new SKColor(199, 45, 47, 205), 2.7f);
        SharedFrameSegment = Stroke(new SKColor(115, 76, 155), 2.1f);
        SharedFrameCurve = Stroke(new SKColor(115, 76, 155), 2.45f);
        SharedFrameCurveTension = DashedStroke(new SKColor(115, 76, 155, 190), 2.2f);
        SharedFramePoint = Fill(new SKColor(115, 76, 155, 190));
        DirectedSegment = Stroke(new SKColor(63, 113, 83), 2.75f);
        DirectedSegmentTension = DashedStroke(new SKColor(63, 113, 83, 190), 2.45f);
        Circumference = Stroke(new SKColor(95, 103, 116, 135), 1.1f);
        CircumferenceTension = DashedStroke(new SKColor(95, 103, 116, 130), 1.1f);
        CircumferenceSpan = Stroke(new SKColor(42, 91, 143, 145), 1.6f);
        CircumferenceSpanTension = DashedStroke(new SKColor(42, 91, 143, 145), 1.45f);
        EndpointBar = Stroke(new SKColor(128, 132, 137, 170), 1.2f);
        Partition = Stroke(new SKColor(82, 88, 96, 170), 1.1f);
        Basis = Stroke(new SKColor(70, 76, 84, 185), 1.1f);
        LineSeed = Stroke(new SKColor(116, 75, 134, 145), 1.2f);
        LineSeedTension = DashedStroke(new SKColor(116, 75, 134, 135), 1.1f);
        SheetSeed = Fill(new SKColor(42, 91, 143, 28));
        SheetSeedEdge = Stroke(new SKColor(42, 91, 143, 80), 1f);
        SheetSeedEdgeTension = DashedStroke(new SKColor(42, 91, 143, 90), 1f);
        RadiusAttachment = Stroke(new SKColor(151, 92, 52, 120), 1.1f);
        RadiusAttachmentTension = DashedStroke(new SKColor(151, 92, 52, 130), 1.1f);
        AreaFill = Fill(new SKColor(71, 126, 94, 34));
        AntiAreaFill = Fill(new SKColor(226, 50, 55, 58));
        AreaBoundary = Stroke(new SKColor(71, 126, 94, 115), 1.2f);
        AreaSettlementPartition = DashedStroke(new SKColor(41, 105, 174, 150), 1.35f);
        BranchNode = Fill(new SKColor(63, 113, 83, 190));
        MergeNode = Fill(new SKColor(42, 91, 143, 190));
        PathGraphTerminalNode = Fill(new SKColor(82, 88, 96, 145));
        PathGraphBranchNode = Fill(new SKColor(151, 92, 52, 190));
        TopologyNodeBorder = Stroke(new SKColor(255, 255, 255, 225), 1.4f);
        HoverBackground = Fill(new SKColor(255, 255, 255, 235));
        HoverBorder = Stroke(new SKColor(80, 86, 96, 210), 1f);
        HoverText = Fill(new SKColor(31, 37, 45));
        RadialInner = new SKColor(162, 64, 46, 78);
        RadialOuter = new SKColor(162, 64, 46, 0);
    }

    public SKPaint Dot { get; }

    public SKPaint Segment { get; }

    public SKPaint SegmentTension { get; }

    public SKPaint AngleControlledSegment { get; }

    public SKPaint AngleControlledSegmentTension { get; }

    public SKPaint SharedFrameSegment { get; }

    public SKPaint SharedFrameCurve { get; }

    public SKPaint SharedFrameCurveTension { get; }

    public SKPaint SharedFramePoint { get; }

    public SKPaint DirectedSegment { get; }

    public SKPaint DirectedSegmentTension { get; }

    public SKPaint Circumference { get; }

    public SKPaint CircumferenceTension { get; }

    public SKPaint CircumferenceSpan { get; }

    public SKPaint CircumferenceSpanTension { get; }

    public SKPaint EndpointBar { get; }

    public SKPaint Partition { get; }

    public SKPaint Basis { get; }

    public SKPaint LineSeed { get; }

    public SKPaint LineSeedTension { get; }

    public SKPaint SheetSeed { get; }

    public SKPaint SheetSeedEdge { get; }

    public SKPaint SheetSeedEdgeTension { get; }

    public SKPaint RadiusAttachment { get; }

    public SKPaint RadiusAttachmentTension { get; }

    public SKPaint AreaFill { get; }

    public SKPaint AntiAreaFill { get; }

    public SKPaint AreaBoundary { get; }

    public SKPaint AreaSettlementPartition { get; }

    public SKPaint BranchNode { get; }

    public SKPaint MergeNode { get; }

    public SKPaint PathGraphTerminalNode { get; }

    public SKPaint PathGraphBranchNode { get; }

    public SKPaint TopologyNodeBorder { get; }

    public SKPaint HoverBackground { get; }

    public SKPaint HoverBorder { get; }

    public SKPaint HoverText { get; }

    public float DotRadius { get; init; } = 5.5f;

    public float RadialRadius { get; init; } = 34f;

    public SKColor RadialInner { get; init; }

    public SKColor RadialOuter { get; init; }

    public IReadOnlyList<SKColor>? AreaFillPalette { get; init; }

    public bool HideAreaBoundary { get; init; }

    public bool HideSharedFrameGeometry { get; init; }

    public bool DrawSharedFrameAreaPartitions { get; init; }

    public bool DrawAreaUnionOutline { get; init; }

    public bool DrawAreaUnionAsOutline { get; init; }

    public bool DrawAreaUnionFromSharedFrameAreaPartitions { get; init; }

    public bool DrawAreaUnionAntiAreas { get; init; }

    public bool DrawAreaUnionSettlementPartitions { get; init; }

    public bool EraseAreaUnionOutline { get; init; }

    public float EndpointBarLength { get; init; } = 10f;

    public float PartitionTickLength { get; init; } = 9f;

    public float BasisSize { get; init; } = 7f;

    public float ArrowLength { get; init; } = 11f;

    public float ArrowWidth { get; init; } = 5f;

    public float LineSeedLength { get; init; } = 38f;

    public float SheetSeedWidth { get; init; } = 18f;

    public float TopologyNodeRadius { get; init; } = 6.5f;

    public float PathGraphNodeRadius { get; init; } = 4.6f;

    public void Dispose()
    {
        Dot.Dispose();
        Segment.Dispose();
        SegmentTension.Dispose();
        AngleControlledSegment.Dispose();
        AngleControlledSegmentTension.Dispose();
        SharedFrameSegment.Dispose();
        SharedFrameCurve.Dispose();
        SharedFrameCurveTension.Dispose();
        SharedFramePoint.Dispose();
        DirectedSegment.Dispose();
        DirectedSegmentTension.Dispose();
        Circumference.Dispose();
        CircumferenceTension.Dispose();
        CircumferenceSpan.Dispose();
        CircumferenceSpanTension.Dispose();
        EndpointBar.Dispose();
        Partition.Dispose();
        Basis.Dispose();
        LineSeed.Dispose();
        LineSeedTension.Dispose();
        SheetSeed.Dispose();
        SheetSeedEdge.Dispose();
        SheetSeedEdgeTension.Dispose();
        RadiusAttachment.Dispose();
        RadiusAttachmentTension.Dispose();
        AreaFill.Dispose();
        AntiAreaFill.Dispose();
        AreaBoundary.Dispose();
        AreaSettlementPartition.Dispose();
        BranchNode.Dispose();
        MergeNode.Dispose();
        PathGraphTerminalNode.Dispose();
        PathGraphBranchNode.Dispose();
        TopologyNodeBorder.Dispose();
        HoverBackground.Dispose();
        HoverBorder.Dispose();
        HoverText.Dispose();
    }

    private static SKPaint Fill(SKColor color) => new()
    {
        IsAntialias = true,
        Color = color,
        Style = SKPaintStyle.Fill
    };

    private static SKPaint Stroke(SKColor color, float width) => new()
    {
        IsAntialias = true,
        Color = color,
        StrokeWidth = width,
        StrokeCap = SKStrokeCap.Round,
        StrokeJoin = SKStrokeJoin.Round,
        Style = SKPaintStyle.Stroke
    };

    private static SKPaint DashedStroke(SKColor color, float width)
    {
        var paint = Stroke(color, width);
        paint.PathEffect = SKPathEffect.CreateDash([6f, 5f], 0f);
        return paint;
    }
}
