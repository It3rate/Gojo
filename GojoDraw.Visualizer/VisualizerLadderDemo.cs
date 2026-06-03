using Gojo.Core;
using Gojo.Structures;
using GojoCAD;
using SkiaSharp;

namespace GojoDraw.Visualizer;

internal sealed record VisualizerDemoPage(
    string Label,
    IElement Evidence,
    IReadOnlyList<VisualizerDemoPanel> Panels)
{
    public IElement Structure => Chain(Panels.Select(panel => panel.Structure).ToArray());

    public VisualizerDemoPage Resample() =>
        this with
        {
            Panels = Panels
                .Select(panel => panel with { ProjectionSeed = Random.Shared.Next() })
                .ToArray()
        };

    private static IElement Chain(IReadOnlyList<IElement> elements)
    {
        if (elements.Count == 0)
        {
            return Carrier.Unknown;
        }

        var structure = elements[^1];
        for (var index = elements.Count - 2; index >= 0; index--)
        {
            structure = new Relation(elements[index], structure);
        }

        return structure;
    }
}

internal sealed record VisualizerDemoPanel(
    string Title,
    string Caption,
    IElement Structure,
    IElement Evidence,
    int ProjectionSeed,
    SKRect ModelBounds,
    string? ResidueText = null);

internal static class VisualizerLadderDemo
{
    public static VisualizerDemoPage CreateFeatureReplay()
    {
        var replay = CreateReplayEvidence();
        var panels = new[]
        {
            new VisualizerDemoPanel(
                "1. Base rung",
                "Sharp feature A feeds B and D. C is independent.",
                CreateFeatureDrawing(FeatureDrawingStage.Base),
                replay.BaseRung,
                Random.Shared.Next(),
                FeatureModelBounds()),
            new VisualizerDemoPanel(
                "2. Focus + transform",
                "The corner focus carries its exterior and proposes a rounded transition.",
                CreateFeatureDrawing(FeatureDrawingStage.FocusedTransform),
                replay.AffectedSlice,
                Random.Shared.Next(),
                FeatureModelBounds()),
            new VisualizerDemoPanel(
                "3. Partial replay",
                "A' and carried C are current. Dependent consumers remain explicit residue.",
                CreateFeatureDrawing(FeatureDrawingStage.PartialReplay),
                replay.PartialReplayRung,
                Random.Shared.Next(),
                FeatureModelBounds(),
                replay.StaleCount == 1 ? "1 stale dependency" : $"{replay.StaleCount} stale dependencies"),
            new VisualizerDemoPanel(
                "4. Resumed rung",
                "B' and D' replay against A'. The current rung is settled.",
                CreateFeatureDrawing(FeatureDrawingStage.SettledReplay),
                replay.SettledRung,
                Random.Shared.Next(),
                FeatureModelBounds())
        };

        return new VisualizerDemoPage(
            "Feature Replay Ladder",
            StructuralLadderSurface.CreateWithRungs(
                new Carrier(),
                replay.BaseRung,
                replay.PartialReplayRung,
                replay.SettledRung),
            panels);
    }

    public static IElement CreateLegendGlyph() =>
        CreateFeatureDrawing(FeatureDrawingStage.FocusedTransform);

    private static ReplayEvidence CreateReplayEvidence()
    {
        var sourceCarrier = new Carrier();
        var source = Relation.Segment(sourceCarrier, 0, 40);
        var producerSelected = Relation.Segment(sourceCarrier, 2, 4);
        var producerReplacement = Relation.Segment(new Carrier(), 0, 2);
        var firstConsumerSelected = Relation.Segment(sourceCarrier, 10, 12);
        var firstConsumerReplacement = Relation.Segment(new Carrier(), 0, 3);
        var independentSelected = Relation.Segment(sourceCarrier, 20, 22);
        var laterConsumerSelected = Relation.Segment(sourceCarrier, 30, 32);
        var laterConsumerReplacement = Relation.Segment(new Carrier(), 0, 4);

        var producerApplication = CreateSegmentApplication(
            source,
            Relation.Point(sourceCarrier, 3),
            producerSelected,
            producerReplacement);
        Require(TransformSurface.TryReadApplication(
            producerApplication,
            out _,
            out var producerFocus,
            out _,
            out _,
            out _,
            out _,
            out _,
            out _));

        var firstConsumerApplication = CreateSegmentApplication(
            source,
            Relation.Point(sourceCarrier, 11),
            firstConsumerSelected,
            firstConsumerReplacement);
        var independentApplication = CreateSegmentApplication(
            source,
            Relation.Point(sourceCarrier, 21),
            independentSelected,
            Relation.Segment(new Carrier(), 0, 2));
        var laterConsumerApplication = CreateSegmentApplication(
            source,
            Relation.Point(sourceCarrier, 31),
            laterConsumerSelected,
            laterConsumerReplacement);

        var producerToFirstConsumer = TransformSurface.ApplicationDependency(
            new Carrier(),
            producerApplication,
            firstConsumerApplication,
            FocusSurface.Bind(producerReplacement, firstConsumerSelected),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var producerToLaterConsumer = TransformSurface.ApplicationDependency(
            new Carrier(),
            producerApplication,
            laterConsumerApplication,
            FocusSurface.Bind(producerReplacement, laterConsumerSelected),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));

        var changedProducerSelected = Relation.Segment(sourceCarrier, 2, 6);
        var changedProducerReplacement = Relation.Segment(new Carrier(), 0, 4);
        var changedProducerFocus = CreateFocus(
            source,
            Relation.Point(sourceCarrier, 4),
            changedProducerSelected,
            Relation.Point(Carrier.Unknown, 0),
            changedProducerReplacement,
            FocusSurface.Bind(producerSelected, changedProducerSelected));
        Require(TransformSurface.TryCreateFocusChange(
            new Carrier(),
            producerFocus,
            changedProducerFocus,
            FocusSurface.Bind(producerSelected, changedProducerSelected),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var focusChange));

        var previousFrame = TransformSurface.ApplicationFrame(
            new Carrier(),
            producerApplication,
            firstConsumerApplication,
            independentApplication,
            laterConsumerApplication);
        var dependencyFrame = TransformSurface.ApplicationDependencyFrame(
            new Carrier(),
            producerToFirstConsumer,
            producerToLaterConsumer);
        Require(TransformSurface.TryCreateDependencyAffectedSlice(
            new Carrier(),
            previousFrame,
            dependencyFrame,
            focusChange,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var affectedSlice));

        Require(TransformSurface.TryReplayApplication(
            new Carrier(),
            producerApplication,
            changedProducerFocus,
            FocusSurface.Bind(producerSelected, changedProducerSelected),
            FocusSurface.Frame(new Carrier(), source, changedProducerReplacement),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var producerReplay));
        Require(TransformSurface.TryCreatePartialReplayFromDependencyAffectedSlice(
            new Carrier(),
            affectedSlice,
            [producerReplay],
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var partialReplay));
        Require(TransformSurface.TryReadPartialReplay(
            partialReplay,
            out _,
            out _,
            out _,
            out _,
            out var partialResidue,
            out _));
        Require(TransformSurface.TryReadStaleDependencyResidue(partialResidue, out var staleDependencies));
        Require(StructuralLadderSurface.TryCreateRungFromPartialReplay(
            new Carrier(),
            partialReplay,
            out var partialReplayRung));

        var changedLaterConsumerSelected = Relation.Segment(sourceCarrier, 30, 36);
        var changedLaterConsumerReplacement = Relation.Segment(new Carrier(), 0, 6);
        var changedLaterConsumerFocus = CreateFocus(
            source,
            Relation.Point(sourceCarrier, 33),
            changedLaterConsumerSelected,
            Relation.Point(Carrier.Unknown, 0),
            changedLaterConsumerReplacement,
            FocusSurface.Bind(laterConsumerSelected, changedLaterConsumerSelected));
        Require(TransformSurface.TryReplayApplication(
            new Carrier(),
            laterConsumerApplication,
            changedLaterConsumerFocus,
            FocusSurface.Bind(laterConsumerSelected, changedLaterConsumerSelected),
            FocusSurface.Frame(new Carrier(), source, changedLaterConsumerReplacement),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var laterConsumerReplay));
        Require(TransformSurface.TryCreatePartialReplayFromStaleDependencyResidue(
            new Carrier(),
            partialReplay,
            [laterConsumerReplay],
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var laterResumedReplay));

        var changedFirstConsumerSelected = Relation.Segment(sourceCarrier, 10, 15);
        var changedFirstConsumerReplacement = Relation.Segment(new Carrier(), 0, 5);
        var changedFirstConsumerFocus = CreateFocus(
            source,
            Relation.Point(sourceCarrier, 13),
            changedFirstConsumerSelected,
            Relation.Point(Carrier.Unknown, 0),
            changedFirstConsumerReplacement,
            FocusSurface.Bind(firstConsumerSelected, changedFirstConsumerSelected));
        Require(TransformSurface.TryReplayApplication(
            new Carrier(),
            firstConsumerApplication,
            changedFirstConsumerFocus,
            FocusSurface.Bind(firstConsumerSelected, changedFirstConsumerSelected),
            FocusSurface.Frame(new Carrier(), source, changedFirstConsumerReplacement),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var firstConsumerReplay));
        Require(TransformSurface.TryCreatePartialReplayFromStaleDependencyResidue(
            new Carrier(),
            laterResumedReplay,
            [firstConsumerReplay],
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var settledReplay));
        Require(StructuralLadderSurface.TryCreateRungFromPartialReplay(
            new Carrier(),
            settledReplay,
            out var settledRung));

        return new ReplayEvidence(
            StructuralLadderSurface.BaseRung(new Carrier(), previousFrame),
            affectedSlice,
            partialReplayRung,
            settledRung,
            staleDependencies.Length);
    }

    private static Relation CreateSegmentApplication(
        IElement source,
        IElement focusBasis,
        IElement selected,
        IElement replacement)
    {
        var transform = CreateMapTransform(new Placeholder(), new Placeholder());
        return CreateApplication(
            transform,
            CreateFocus(
                source,
                focusBasis,
                selected,
                Relation.Point(Carrier.Unknown, 0),
                replacement,
                FocusSurface.Bind(selected, replacement)),
            FocusSurface.BindingFrame(
                new Carrier(),
                FocusSurface.Bind(ReadInputPort(transform), selected),
                FocusSurface.Bind(ReadOutputPort(transform), replacement)),
            FocusSurface.Frame(new Carrier(), source, replacement));
    }

    private static Relation CreateMapTransform(IElement inputPort, IElement outputPort) =>
        TransformSurface.Create(
            new Carrier(),
            TransformSurface.Ports(new Carrier(), inputPort),
            TransformSurface.Map(inputPort, outputPort),
            TransformSurface.Ports(new Carrier(), outputPort),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));

    private static Relation CreateFocus(
        IElement source,
        IElement focusBasis,
        IElement participation,
        IElement complement,
        IElement replacement,
        IElement correspondence) =>
        FocusSurface.Create(
            new Carrier(),
            source,
            focusBasis,
            participation,
            complement,
            replacement,
            correspondence,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));

    private static Relation CreateApplication(
        Relation transform,
        Relation focus,
        IElement bindings,
        IElement result)
    {
        Require(TransformSurface.TryCreateApplication(
            new Carrier(),
            transform,
            focus,
            bindings,
            result,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var application));
        return application;
    }

    private static IElement ReadInputPort(Relation transform)
    {
        Require(TransformSurface.TryRead(transform, out var inputPorts, out _, out _, out _, out _));
        Require(TransformSurface.TryReadPorts(inputPorts, out var ports));
        return ports[0];
    }

    private static IElement ReadOutputPort(Relation transform)
    {
        Require(TransformSurface.TryRead(transform, out _, out _, out var outputPorts, out _, out _));
        Require(TransformSurface.TryReadPorts(outputPorts, out var ports));
        return ports[0];
    }

    private static IElement CreateFeatureDrawing(FeatureDrawingStage stage)
    {
        var frame = new Carrier();
        var x = new Carrier();
        var y = new Carrier();
        var points = new Dictionary<(long X, long Y), Relation>();

        Relation Point(long px, long py)
        {
            var key = (X: px, Y: py);
            if (!points.TryGetValue(key, out var point))
            {
                point = GraphicStructure.FramePoint(
                    frame,
                    new CarrierLocation(x, px),
                    new CarrierLocation(y, py));
                points[key] = point;
            }

            return point;
        }

        Relation Segment(long ax, long ay, long bx, long by) =>
            GraphicStructure.FrameSegment(Point(ax, ay), Point(bx, by));

        Relation Arc(long cx, long cy, long ax, long ay, long bx, long by) =>
            GraphicStructure.FrameArc(Point(cx, cy), Point(ax, ay), Point(bx, by));

        var elements = new List<IElement>();
        switch (stage)
        {
            case FeatureDrawingStage.Base:
                elements.AddRange([
                    Segment(-22, 0, 14, 0),
                    Segment(14, 0, 14, 18),
                    Segment(14, 12, 25, 12),
                    Segment(2, 0, 2, -9),
                    Segment(-22, -13, -10, -13)
                ]);
                break;

            case FeatureDrawingStage.FocusedTransform:
                elements.AddRange([
                    Segment(-22, 0, 5, 0),
                    Segment(5, 0, 14, 0),
                    Segment(14, 0, 14, 8),
                    Segment(14, 8, 14, 18),
                    Arc(8, 6, 8, 0, 14, 6),
                    Segment(-22, -13, -10, -13)
                ]);
                break;

            case FeatureDrawingStage.PartialReplay:
                elements.AddRange([
                    Segment(-22, 0, 8, 0),
                    Arc(8, 6, 8, 0, 14, 6),
                    Segment(14, 6, 14, 18),
                    Segment(-22, -13, -10, -13)
                ]);
                break;

            case FeatureDrawingStage.SettledReplay:
                elements.AddRange([
                    Segment(-22, 0, 8, 0),
                    Arc(8, 6, 8, 0, 14, 6),
                    Segment(14, 6, 14, 18),
                    Segment(14, 12, 25, 12),
                    Segment(8, 0, 0, -9),
                    Segment(-22, -13, -10, -13)
                ]);
                break;
        }

        return Chain(elements);
    }

    private static SKRect FeatureModelBounds() =>
        new(-27f, -21f, 31f, 23f);

    private static IElement Chain(IReadOnlyList<IElement> elements)
    {
        if (elements.Count == 0)
        {
            return Carrier.Unknown;
        }

        var structure = elements[^1];
        for (var index = elements.Count - 2; index >= 0; index--)
        {
            structure = new Relation(elements[index], structure);
        }

        return structure;
    }

    private static void Require(bool condition)
    {
        if (!condition)
        {
            throw new InvalidOperationException("The ladder replay demo could not build its Gojo evidence.");
        }
    }

    private sealed record ReplayEvidence(
        Relation BaseRung,
        Relation AffectedSlice,
        Relation PartialReplayRung,
        Relation SettledRung,
        int StaleCount);

    private enum FeatureDrawingStage
    {
        Base,
        FocusedTransform,
        PartialReplay,
        SettledReplay
    }
}
