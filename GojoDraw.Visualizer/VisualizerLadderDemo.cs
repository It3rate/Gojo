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
    public IElement Structure => Chain(Panels
        .SelectMany(panel => panel.Elements)
        .Select(element => element.Element)
        .ToArray());

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
    IReadOnlyList<VisualizerDemoElement> Elements,
    IElement Evidence,
    int ProjectionSeed,
    SKRect ModelBounds,
    IReadOnlyList<VisualizerDemoLabel> Labels,
    string? ResidueText = null);

internal sealed record VisualizerDemoElement(
    IElement Element,
    SKColor? Tint = null);

internal sealed record VisualizerDemoLabel(
    string Text,
    float X,
    float Y,
    SKColor Color);

internal static class VisualizerLadderDemo
{
    private static readonly SKColor CurrentTint = new(104, 77, 155, 235);
    private static readonly SKColor SettledTint = new(52, 94, 145, 225);
    private static readonly SKColor LowerRungTint = new(45, 130, 118, 220);

    private static readonly DemoDefinition[] Definitions =
    [
        new("feature replay", "Feature Replay Ladder", DemoKind.RoundedCorner),
        new("route replay", "Route Replay Ladder", DemoKind.Route),
        new("stroke replay", "Stroke Outline Ladder", DemoKind.StrokeOutline),
        new("branch replay", "Branch Replay Ladder", DemoKind.Branch),
        new("letter replay", "Letter Replay Ladder", DemoKind.Letter),
        new("crescent replay", "Crescent Replay Ladder", DemoKind.Crescent)
    ];

    public static IReadOnlyList<string> DemoLabels =>
        Definitions.Select(definition => definition.Label).ToArray();

    public static bool IsDemoLabel(string label) =>
        Definitions.Any(definition => string.Equals(definition.Label, label, StringComparison.OrdinalIgnoreCase));

    public static VisualizerDemoPage Create(string label)
    {
        var definition = Definitions.FirstOrDefault(definition =>
            string.Equals(definition.Label, label, StringComparison.OrdinalIgnoreCase)) ??
            Definitions[0];
        return Create(definition);
    }

    public static IReadOnlyList<VisualizerDemoPage> CreateAll() =>
        Definitions.Select(Create).ToArray();

    public static VisualizerDemoPage CreateFeatureReplay() =>
        Create(Definitions[0]);

    public static IElement CreateLegendGlyph(string label)
    {
        var definition = Definitions.FirstOrDefault(definition =>
            string.Equals(definition.Label, label, StringComparison.OrdinalIgnoreCase)) ??
            Definitions[0];
        return Chain(CreateDrawing(definition.Kind, DemoStage.FocusedTransform)
            .Select(element => element.Element)
            .ToArray());
    }

    public static IElement CreateLegendGlyph() =>
        CreateLegendGlyph(Definitions[0].Label);

    private static VisualizerDemoPage Create(DemoDefinition definition)
    {
        var replay = CreateReplayEvidence();
        var panels = new[]
        {
            new VisualizerDemoPanel(
                "1. Base rung",
                ReadCaption(definition.Kind, DemoStage.Base),
                CreateDrawing(definition.Kind, DemoStage.Base),
                replay.BaseRung,
                Random.Shared.Next(),
                DemoModelBounds(definition.Kind),
                CreateLabels(definition.Kind, DemoStage.Base)),
            new VisualizerDemoPanel(
                "2. Focus + transform",
                ReadCaption(definition.Kind, DemoStage.FocusedTransform),
                CreateDrawing(definition.Kind, DemoStage.FocusedTransform),
                replay.AffectedSlice,
                Random.Shared.Next(),
                DemoModelBounds(definition.Kind),
                CreateLabels(definition.Kind, DemoStage.FocusedTransform)),
            new VisualizerDemoPanel(
                "3. Partial replay",
                ReadCaption(definition.Kind, DemoStage.PartialReplay),
                CreateDrawing(definition.Kind, DemoStage.PartialReplay),
                replay.PartialReplayRung,
                Random.Shared.Next(),
                DemoModelBounds(definition.Kind),
                CreateLabels(definition.Kind, DemoStage.PartialReplay),
                replay.StaleCount == 1 ? "1 stale dependency" : $"{replay.StaleCount} stale dependencies"),
            new VisualizerDemoPanel(
                "4. Resumed rung",
                ReadCaption(definition.Kind, DemoStage.SettledReplay),
                CreateDrawing(definition.Kind, DemoStage.SettledReplay),
                replay.SettledRung,
                Random.Shared.Next(),
                DemoModelBounds(definition.Kind),
                CreateLabels(definition.Kind, DemoStage.SettledReplay))
        };

        return new VisualizerDemoPage(
            definition.PageTitle,
            StructuralLadderSurface.CreateWithRungs(
                new Carrier(),
                replay.BaseRung,
                replay.PartialReplayRung,
                replay.SettledRung),
            panels);
    }

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

    private static IReadOnlyList<VisualizerDemoElement> CreateDrawing(DemoKind kind, DemoStage stage)
    {
        var sketch = new FrameSketch();
        var elements = new List<IElement>();
        void Add(params IElement[] items) => elements.AddRange(items);

        switch (kind)
        {
            case DemoKind.RoundedCorner:
                AddRoundedCorner(sketch, stage, Add);
                break;
            case DemoKind.Route:
                AddRoute(sketch, stage, Add);
                break;
            case DemoKind.StrokeOutline:
                AddStrokeOutline(sketch, stage, Add);
                break;
            case DemoKind.Branch:
                AddBranch(sketch, stage, Add);
                break;
            case DemoKind.Letter:
                AddLetter(sketch, stage, Add);
                break;
            case DemoKind.Crescent:
                AddCrescent(sketch, stage, Add);
                break;
        }

        var tint = stage is DemoStage.SettledReplay ? SettledTint : CurrentTint;
        var rendered = elements
            .Select(element => new VisualizerDemoElement(element, tint))
            .ToList();
        rendered.AddRange(CreateLowerRungTriangle(sketch)
            .Select(element => new VisualizerDemoElement(element, LowerRungTint)));

        return rendered;
    }

    private static IReadOnlyList<IElement> CreateLowerRungTriangle(FrameSketch sketch)
    {
        Relation Segment(long ax, long ay, long bx, long by) => sketch.Segment(ax, ay, bx, by);
        return
        [
            Segment(-25, -18, -13, -18),
            Segment(-13, -18, -19, -9),
            Segment(-19, -9, -25, -18)
        ];
    }

    private static void AddRoundedCorner(FrameSketch sketch, DemoStage stage, Action<IElement[]> add)
    {
        Relation Segment(long ax, long ay, long bx, long by) => sketch.Segment(ax, ay, bx, by);
        Relation Arc(long cx, long cy, long ax, long ay, long bx, long by) => sketch.Arc(cx, cy, ax, ay, bx, by);
        switch (stage)
        {
            case DemoStage.Base:
                add([
                    Segment(-22, 0, 14, 0),
                    Segment(14, 0, 14, 18),
                    Segment(14, 12, 25, 12),
                    Segment(2, 0, 2, -9)
                ]);
                break;

            case DemoStage.FocusedTransform:
                add([
                    Segment(-22, 0, 5, 0),
                    Segment(5, 0, 14, 0),
                    Segment(14, 0, 14, 8),
                    Segment(14, 8, 14, 18),
                    Arc(8, 6, 8, 0, 14, 6)
                ]);
                break;

            case DemoStage.PartialReplay:
                add([
                    Segment(-22, 0, 8, 0),
                    Arc(8, 6, 8, 0, 14, 6),
                    Segment(14, 6, 14, 18)
                ]);
                break;

            case DemoStage.SettledReplay:
                add([
                    Segment(-22, 0, 8, 0),
                    Arc(8, 6, 8, 0, 14, 6),
                    Segment(14, 6, 14, 18),
                    Segment(14, 12, 25, 12),
                    Segment(8, 0, 0, -9)
                ]);
                break;
        }
    }

    private static void AddRoute(FrameSketch sketch, DemoStage stage, Action<IElement[]> add)
    {
        Relation Segment(long ax, long ay, long bx, long by) => sketch.Segment(ax, ay, bx, by);
        Relation Arc(long cx, long cy, long ax, long ay, long bx, long by) => sketch.Arc(cx, cy, ax, ay, bx, by);
        switch (stage)
        {
            case DemoStage.Base:
                add([
                    Segment(-23, 0, 12, 0),
                    Segment(12, 0, 12, 15),
                    Segment(12, 15, 26, 15),
                    Segment(3, 0, 3, -11)
                ]);
                break;
            case DemoStage.FocusedTransform:
                add([
                    Segment(-23, 0, 4, 0),
                    Arc(4, 7, 4, 0, 11, 7),
                    Segment(11, 7, 11, 18),
                    Segment(12, 15, 26, 15),
                    Segment(3, 0, 3, -11)
                ]);
                break;
            case DemoStage.PartialReplay:
                add([
                    Segment(-23, 0, 4, 0),
                    Arc(4, 7, 4, 0, 11, 7),
                    Segment(11, 7, 11, 18)
                ]);
                break;
            case DemoStage.SettledReplay:
                add([
                    Segment(-23, 0, 4, 0),
                    Arc(4, 7, 4, 0, 11, 7),
                    Segment(11, 7, 11, 18),
                    Segment(11, 15, 26, 15),
                    Segment(4, 0, -2, -11)
                ]);
                break;
        }
    }

    private static void AddStrokeOutline(FrameSketch sketch, DemoStage stage, Action<IElement[]> add)
    {
        Relation Segment(long ax, long ay, long bx, long by) => sketch.Segment(ax, ay, bx, by);
        Relation Arc(long cx, long cy, long ax, long ay, long bx, long by) => sketch.Arc(cx, cy, ax, ay, bx, by);
        switch (stage)
        {
            case DemoStage.Base:
                add([
                    Segment(-20, 0, 18, 0),
                    Segment(18, -5, 18, 5),
                    Segment(-20, -5, -20, 5),
                    Segment(0, 0, 0, -12)
                ]);
                break;
            case DemoStage.FocusedTransform:
                add([
                    Segment(-18, 4, 18, 4),
                    Segment(-18, -4, 18, -4),
                    Arc(-18, 0, -18, 4, -18, -4),
                    Arc(18, 0, 18, -4, 18, 4),
                    Segment(0, 0, 0, -12)
                ]);
                break;
            case DemoStage.PartialReplay:
                add([
                    Segment(-18, 4, 18, 4),
                    Segment(-18, -4, 18, -4),
                    Arc(-18, 0, -18, 4, -18, -4),
                    Arc(18, 0, 18, -4, 18, 4)
                ]);
                break;
            case DemoStage.SettledReplay:
                add([
                    Segment(-18, 4, 18, 4),
                    Segment(-18, -4, 18, -4),
                    Arc(-18, 0, -18, 4, -18, -4),
                    Arc(18, 0, 18, -4, 18, 4),
                    Segment(18, 4, 25, 8),
                    Segment(18, -4, 25, -8),
                    Segment(-18, 0, -26, -9)
                ]);
                break;
        }
    }

    private static void AddBranch(FrameSketch sketch, DemoStage stage, Action<IElement[]> add)
    {
        Relation Segment(long ax, long ay, long bx, long by) => sketch.Segment(ax, ay, bx, by);
        Relation Curve(long ax, long ay, long cx, long cy, long bx, long by) => sketch.Quadratic(ax, ay, cx, cy, bx, by);
        switch (stage)
        {
            case DemoStage.Base:
                add([
                    Segment(-24, 0, 0, 0),
                    Segment(0, 0, 17, 12),
                    Segment(0, 0, 17, -12),
                    Segment(0, 0, 0, 15)
                ]);
                break;
            case DemoStage.FocusedTransform:
                add([
                    Segment(-24, 0, -2, 0),
                    Curve(-2, 0, 4, 4, 10, 10),
                    Segment(10, 10, 17, 12),
                    Curve(-2, 0, 4, -4, 10, -10),
                    Segment(10, -10, 17, -12),
                    Segment(0, 0, 0, 15)
                ]);
                break;
            case DemoStage.PartialReplay:
                add([
                    Segment(-24, 0, -2, 0),
                    Curve(-2, 0, 4, 4, 10, 10),
                    Curve(-2, 0, 4, -4, 10, -10)
                ]);
                break;
            case DemoStage.SettledReplay:
                add([
                    Segment(-24, 0, -2, 0),
                    Curve(-2, 0, 4, 4, 10, 10),
                    Curve(-2, 0, 4, -4, 10, -10),
                    Segment(10, 10, 20, 14),
                    Segment(10, -10, 20, -14),
                    Segment(2, 0, 0, 15)
                ]);
                break;
        }
    }

    private static void AddLetter(FrameSketch sketch, DemoStage stage, Action<IElement[]> add)
    {
        Relation Segment(long ax, long ay, long bx, long by) => sketch.Segment(ax, ay, bx, by);
        Relation Curve(long ax, long ay, long cx, long cy, long bx, long by) => sketch.Quadratic(ax, ay, cx, cy, bx, by);
        switch (stage)
        {
            case DemoStage.Base:
                add([
                    Segment(-14, -12, -14, 14),
                    Segment(-14, 14, 8, 14),
                    Segment(8, 14, 8, 2),
                    Segment(8, 2, -14, 2),
                    Segment(-14, 2, 13, -12)
                ]);
                break;
            case DemoStage.FocusedTransform:
                add([
                    Segment(-14, -12, -14, 14),
                    Segment(-14, 14, 0, 14),
                    Curve(0, 14, 9, 14, 9, 8),
                    Curve(9, 8, 9, 2, 0, 2),
                    Segment(0, 2, -14, 2),
                    Segment(-14, 2, 13, -12)
                ]);
                break;
            case DemoStage.PartialReplay:
                add([
                    Segment(-14, -12, -14, 14),
                    Segment(-14, 14, 0, 14),
                    Curve(0, 14, 9, 14, 9, 8),
                    Curve(9, 8, 9, 2, 0, 2),
                    Segment(0, 2, -14, 2)
                ]);
                break;
            case DemoStage.SettledReplay:
                add([
                    Segment(-14, -12, -14, 14),
                    Segment(-14, 14, 0, 14),
                    Curve(0, 14, 9, 14, 9, 8),
                    Curve(9, 8, 9, 2, 0, 2),
                    Segment(0, 2, -14, 2),
                    Segment(-10, 2, 14, -12)
                ]);
                break;
        }
    }

    private static void AddCrescent(FrameSketch sketch, DemoStage stage, Action<IElement[]> add)
    {
        Relation Segment(long ax, long ay, long bx, long by) => sketch.Segment(ax, ay, bx, by);
        Relation Arc(long cx, long cy, long ax, long ay, long bx, long by) => sketch.Arc(cx, cy, ax, ay, bx, by);
        Relation Curve(long ax, long ay, long cx, long cy, long bx, long by) => sketch.Quadratic(ax, ay, cx, cy, bx, by);
        switch (stage)
        {
            case DemoStage.Base:
                add([
                    Arc(0, 0, 0, 14, 14, 0),
                    Arc(0, 0, 14, 0, 0, -14),
                    Arc(0, 0, 0, -14, -14, 0),
                    Arc(0, 0, -14, 0, 0, 14),
                    Segment(5, 10, 5, -10),
                    Segment(-2, 0, 10, 0)
                ]);
                break;
            case DemoStage.FocusedTransform:
                add([
                    Arc(0, 0, 0, 14, -14, 0),
                    Arc(0, 0, -14, 0, 0, -14),
                    Curve(0, 14, -8, 0, 0, -14),
                    Segment(-2, 0, 10, 0)
                ]);
                break;
            case DemoStage.PartialReplay:
                add([
                    Arc(0, 0, 0, 14, -14, 0),
                    Arc(0, 0, -14, 0, 0, -14),
                    Curve(0, 14, -8, 0, 0, -14)
                ]);
                break;
            case DemoStage.SettledReplay:
                add([
                    Arc(0, 0, 0, 14, -14, 0),
                    Arc(0, 0, -14, 0, 0, -14),
                    Curve(0, 14, -8, 0, 0, -14)
                ]);
                break;
        }
    }

    private static IReadOnlyList<VisualizerDemoLabel> CreateLabels(DemoKind kind, DemoStage stage)
    {
        var stale = new SKColor(169, 54, 57, 235);
        var settled = new SKColor(48, 87, 135, 230);
        var changed = new SKColor(86, 68, 145, 235);
        var positions = kind switch
        {
            DemoKind.RoundedCorner => new LabelPositions(
                new SKPoint(12f, 4f),
                new SKPoint(22f, 15f),
                new SKPoint(-7f, -18f),
                new SKPoint(1f, -12f),
                new SKPoint(8f, -4f),
                new SKPoint(9f, -4f),
                new SKPoint(22f, 15f),
                new SKPoint(-1f, -12f)),
            DemoKind.Route => new LabelPositions(
                new SKPoint(11f, 5f),
                new SKPoint(23f, 18f),
                new SKPoint(-7f, -18f),
                new SKPoint(1f, -13f),
                new SKPoint(7f, 4f),
                new SKPoint(8f, 4f),
                new SKPoint(23f, 18f),
                new SKPoint(-4f, -13f)),
            DemoKind.StrokeOutline => new LabelPositions(
                new SKPoint(0f, 4f),
                new SKPoint(24f, 9f),
                new SKPoint(-7f, -18f),
                new SKPoint(0f, -14f),
                new SKPoint(0f, 7f),
                new SKPoint(0f, 7f),
                new SKPoint(24f, 10f),
                new SKPoint(-25f, -11f)),
            DemoKind.Branch => new LabelPositions(
                new SKPoint(0f, 4f),
                new SKPoint(18f, 15f),
                new SKPoint(-7f, -18f),
                new SKPoint(18f, -15f),
                new SKPoint(3f, 4f),
                new SKPoint(3f, 4f),
                new SKPoint(18f, 15f),
                new SKPoint(18f, -15f)),
            DemoKind.Letter => new LabelPositions(
                new SKPoint(-17f, 11f),
                new SKPoint(9f, 11f),
                new SKPoint(-7f, -18f),
                new SKPoint(9f, -11f),
                new SKPoint(2f, 12f),
                new SKPoint(2f, 12f),
                new SKPoint(9f, 11f),
                new SKPoint(10f, -11f)),
            DemoKind.Crescent => new LabelPositions(
                new SKPoint(-10f, 12f),
                new SKPoint(12f, 8f),
                new SKPoint(-7f, -18f),
                new SKPoint(9f, 2f),
                new SKPoint(-9f, 12f),
                new SKPoint(-9f, 12f),
                new SKPoint(12f, 8f),
                new SKPoint(8f, 2f)),
            _ => new LabelPositions(
                new SKPoint(12f, 4f),
                new SKPoint(22f, 15f),
                new SKPoint(-7f, -18f),
                new SKPoint(1f, -12f),
                new SKPoint(8f, -4f),
                new SKPoint(9f, -4f),
                new SKPoint(22f, 15f),
                new SKPoint(-1f, -12f))
        };

        return stage switch
        {
            DemoStage.PartialReplay =>
            [
                new VisualizerDemoLabel("A'", positions.PartialA.X, positions.PartialA.Y, changed),
                new VisualizerDemoLabel("B", positions.SettledB.X, positions.SettledB.Y, stale),
                new VisualizerDemoLabel("C (from lower rung)", positions.C.X, positions.C.Y, LowerRungTint),
                new VisualizerDemoLabel("D", positions.SettledD.X, positions.SettledD.Y, stale)
            ],
            DemoStage.SettledReplay =>
            [
                new VisualizerDemoLabel("A'", positions.SettledA.X, positions.SettledA.Y, changed),
                new VisualizerDemoLabel("B'", positions.SettledB.X, positions.SettledB.Y, settled),
                new VisualizerDemoLabel("C (from lower rung)", positions.C.X, positions.C.Y, LowerRungTint),
                new VisualizerDemoLabel("D'", positions.SettledD.X, positions.SettledD.Y, settled)
            ],
            _ =>
            [
                new VisualizerDemoLabel("A", positions.BaseA.X, positions.BaseA.Y, changed),
                new VisualizerDemoLabel("B", positions.B.X, positions.B.Y, settled),
                new VisualizerDemoLabel("C (from lower rung)", positions.C.X, positions.C.Y, LowerRungTint),
                new VisualizerDemoLabel("D", positions.D.X, positions.D.Y, settled)
            ]
        };
    }

    private static string ReadCaption(DemoKind kind, DemoStage stage)
    {
        var noun = kind switch
        {
            DemoKind.RoundedCorner => "corner",
            DemoKind.Route => "route",
            DemoKind.StrokeOutline => "stroke",
            DemoKind.Branch => "junction",
            DemoKind.Letter => "letter bowl",
            DemoKind.Crescent => "outer curve",
            _ => "feature"
        };

        return stage switch
        {
            DemoStage.Base => $"A is the current {noun}; B and D depend on it. C comes from a lower rung.",
            DemoStage.FocusedTransform => $"The focus proposes A' while preserving the rest as relation evidence.",
            DemoStage.PartialReplay => "A' changes while C remains lower-rung evidence. B and D remain stale residue.",
            DemoStage.SettledReplay => "B' and D' replay against A'. The ladder's current rung is settled.",
            _ => string.Empty
        };
    }

    private static SKRect DemoModelBounds(DemoKind _) =>
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

    private sealed record DemoDefinition(
        string Label,
        string PageTitle,
        DemoKind Kind);

    private sealed record LabelPositions(
        SKPoint BaseA,
        SKPoint B,
        SKPoint C,
        SKPoint D,
        SKPoint PartialA,
        SKPoint SettledA,
        SKPoint SettledB,
        SKPoint SettledD);

    private sealed class FrameSketch
    {
        private readonly Carrier frame = new();
        private readonly Carrier x = new();
        private readonly Carrier y = new();
        private readonly Dictionary<(long X, long Y), Relation> points = [];

        public Relation Point(long px, long py)
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

        public Relation Segment(long ax, long ay, long bx, long by) =>
            GraphicStructure.FrameSegment(Point(ax, ay), Point(bx, by));

        public Relation Arc(long cx, long cy, long ax, long ay, long bx, long by) =>
            GraphicStructure.FrameArc(Point(cx, cy), Point(ax, ay), Point(bx, by));

        public Relation Quadratic(long ax, long ay, long cx, long cy, long bx, long by) =>
            GraphicStructure.FrameQuadraticCurve(Point(ax, ay), Point(cx, cy), Point(bx, by));
    }

    private enum DemoKind
    {
        RoundedCorner,
        Route,
        StrokeOutline,
        Branch,
        Letter,
        Crescent
    }

    private enum DemoStage
    {
        Base,
        FocusedTransform,
        PartialReplay,
        SettledReplay
    }
}
