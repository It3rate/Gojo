using Gojo.Core;
using Gojo.Structures;
using GojoCAD;
using GojoCAD.Structures;
using SkiaSharp;

namespace GojoDraw;

/// <summary>
/// Minimal Skia adapter that traverses GojoCAD relation structure and renders
/// low-level physical forms it recognizes.
/// </summary>
public sealed class GojoCadSkiaAdapter
{
    public int DrawElement(
        SKCanvas canvas,
        IElement element,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas = null)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(paints);

        var renderable = SelectCurrentRenderableRungs(element);
        if (renderable is Relation union &&
            GraphicBooleanSurface.TryReadAreaUnion(union, out _, out _, out _, out _))
        {
            return DrawAreaUnion(canvas, union, projection, paints, hitAreas);
        }

        if (TryReadAreaUnionFrame(renderable, out var unions))
        {
            var drawn = 0;
            foreach (var framedUnion in unions)
            {
                drawn += DrawAreaUnion(canvas, framedUnion, projection, paints, hitAreas);
            }

            return drawn;
        }

        var graph = GraphicReadGraph.From(renderable);
        return DrawReadGraph(canvas, graph, projection, paints, hitAreas);
    }

    private static bool TryReadAreaUnionFrame(IElement element, out Relation[] unions)
    {
        if (element is Relation relation &&
            relation.Right is Carrier holder &&
            relation.TryReadOrderedElementsOn(holder, out var elements) &&
            elements.Length > 0)
        {
            var readUnions = new List<Relation>();
            foreach (var elementInFrame in elements)
            {
                if (elementInFrame is not Relation union ||
                    !GraphicBooleanSurface.TryReadAreaUnion(union, out _, out _, out _, out _))
                {
                    unions = [];
                    return false;
                }

                readUnions.Add(union);
            }

            unions = [.. readUnions];
            return true;
        }

        unions = [];
        return false;
    }

    private static IElement SelectCurrentRenderableRungs(IElement element) =>
        SelectCurrentRenderableRungs(element, []);

    private static IElement SelectCurrentRenderableRungs(IElement element, Dictionary<ulong, IElement> visited)
    {
        if (element is not Relation relation)
        {
            return element;
        }

        if (visited.TryGetValue(relation.Id, out var known))
        {
            return known;
        }

        visited[relation.Id] = relation;
        if (StructuralLadderSurface.TryReadCurrentRung(relation, out var currentRung) &&
            StructuralLadderSurface.TryReadRung(
                currentRung,
                out _,
                out _,
                out var result,
                out _,
                out _,
                out _,
                out _,
                out _))
        {
            var current = SelectCurrentRenderableRungs(result, visited);
            visited[relation.Id] = current;
            return current;
        }

        var left = SelectCurrentRenderableRungs(relation.Left, visited);
        var right = SelectCurrentRenderableRungs(relation.Right, visited);
        if (ReferenceEquals(left, relation.Left) &&
            ReferenceEquals(right, relation.Right))
        {
            return relation;
        }

        var replacement = new Relation(left, right);
        visited[relation.Id] = replacement;
        return replacement;
    }

    private static int DrawAreaUnion(
        SKCanvas canvas,
        Relation union,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas)
    {
        if (!GraphicBooleanSurface.TryReadAreaUnion(
                union,
                out var areaFrame,
                out var outline,
                out _,
                out _) ||
            BuildAreaUnionPath(areaFrame, outline, projection) is not { } path)
        {
            return 0;
        }

        var drawn = 1;
        using (path)
        {
            if (paints.DrawAreaUnionAsOutline)
            {
                if (paints.DrawAreaUnionAntiAreas)
                {
                    drawn += DrawAreaUnionOpeningWitness(canvas, areaFrame, path, projection, paints, hitAreas);
                }

                canvas.DrawPath(path, paints.AreaBoundary);
            }
            else
            {
                canvas.DrawPath(path, paints.AreaFill);
                if (paints.EraseAreaUnionOutline)
                {
                    DrawAreaUnionOutlineWitness(canvas, outline, projection, paints, hitAreas, fill: true);
                }

                if (paints.DrawAreaUnionAntiAreas)
                {
                    drawn += DrawAreaUnionOpeningWitness(canvas, areaFrame, path, projection, paints, hitAreas);
                }
            }
            AddHit(hitAreas, path.Bounds, "area union", priority: 2, union);
        }

        if (paints.DrawAreaUnionOutline)
        {
            return drawn + (paints.DrawAreaUnionAsOutline
                ? DrawAreaUnionOutlineWitness(canvas, outline, projection, paints, hitAreas, fill: false)
                : DrawReadGraph(canvas, GraphicReadGraph.From(outline), projection, paints, hitAreas));
        }

        return drawn;
    }

    private static SKPath? BuildAreaUnionPath(
        IElement areaFrame,
        IElement outline,
        IGojoCadSkiaProjection projection)
    {
        if (!AreaFrameHasAntiAreas(areaFrame) &&
            BuildAreaUnionOutlinePath(outline, projection) is { } outlinePath)
        {
            return outlinePath;
        }

        return BuildAreaFrameUnionPath(areaFrame, projection);
    }

    private static SKPath? BuildAreaFrameUnionPath(IElement areaFrame, IGojoCadSkiaProjection projection)
    {
        if (areaFrame is not Relation frame ||
            !GraphicBooleanSurface.TryReadAreaFrame(frame, out var participants) ||
            participants.Length == 0)
        {
            return null;
        }

        var composite = new SKPath();
        var hasComposite = false;
        var cuts = new List<SKPath>();
        foreach (var participant in participants)
        {
            var shape = BuildAreaParticipantPath(participant, projection, out var isCut);
            if (shape is null)
            {
                continue;
            }

            if (isCut)
            {
                cuts.Add(shape);
                continue;
            }

            if (!hasComposite)
            {
                composite.AddPath(shape);
                hasComposite = true;
            }
            else
            {
                ApplyPathOp(composite, shape, SKPathOp.Union);
            }

            shape.Dispose();
        }

        if (!hasComposite)
        {
            foreach (var cut in cuts)
            {
                cut.Dispose();
            }

            composite.Dispose();
            return null;
        }

        foreach (var cut in cuts)
        {
            ApplyCutPath(composite, cut);
            cut.Dispose();
        }

        return composite;
    }

    private static SKPath? BuildAreaUnionOutlinePath(IElement outline, IGojoCadSkiaProjection projection)
    {
        var graph = GraphicReadGraph.From(outline);
        var composite = new SKPath
        {
            FillType = SKPathFillType.EvenOdd
        };
        var hasPath = false;
        foreach (var boundary in graph.ReadsOf<ClosedSharedFrameBoundaryRead>()
            .DistinctBy(boundary => ReadSourceId(boundary.Source)))
        {
            using var path = BuildClosedSharedFrameBoundaryPath(boundary, projection);
            if (path is null)
            {
                continue;
            }

            composite.AddPath(path);
            hasPath = true;
        }

        foreach (var boundary in graph.ReadsOf<ClosedSegmentBoundaryRead>()
            .DistinctBy(boundary => ReadSourceId(boundary.Source)))
        {
            using var path = BuildClosedSegmentBoundaryPath(boundary, projection);
            if (path is null)
            {
                continue;
            }

            composite.AddPath(path);
            hasPath = true;
        }

        if (hasPath)
        {
            return composite;
        }

        composite.Dispose();
        return null;
    }

    private static bool AreaFrameHasAntiAreas(IElement areaFrame) =>
        GraphicReadGraph.From(areaFrame).ReadsOf<SharedFrameAreaRead>().Any(area => area.IsAntiArea) ||
        GraphicReadGraph.From(areaFrame).ReadsOf<AreaRead>().Any(area => area.IsAntiArea);

    private static SKPath? BuildAreaParticipantPath(
        IElement participant,
        IGojoCadSkiaProjection projection,
        out bool isCut)
    {
        isCut = false;
        var graph = GraphicReadGraph.From(participant);
        var areas = graph.ReadsOf<SharedFrameAreaRead>().ToArray();
        if (areas.Length == 0)
        {
            return null;
        }

        var positive = areas.Where(area => !area.IsAntiArea).ToArray();
        var negative = areas.Where(area => area.IsAntiArea).ToArray();
        if (positive.Length == 0)
        {
            isCut = negative.Length > 0;
            return isCut
                ? BuildAreaBoundaryUnionPath(negative, projection)
                : null;
        }

        var shape = BuildAreaBoundaryUnionPath(positive, projection);
        if (shape is null)
        {
            return null;
        }

        foreach (var area in negative)
        {
            using var path = BuildClosedSharedFrameBoundaryPath(area.Boundary, projection);
            if (path is not null)
            {
                ApplyCutPath(shape, path);
            }
        }

        return shape;
    }

    private static SKPath? BuildAreaBoundaryUnionPath(
        IEnumerable<SharedFrameAreaRead> areas,
        IGojoCadSkiaProjection projection)
    {
        var shape = new SKPath();
        var hasShape = false;
        foreach (var area in areas)
        {
            using var path = BuildClosedSharedFrameBoundaryPath(area.Boundary, projection);
            if (path is null)
            {
                continue;
            }

            if (!hasShape)
            {
                shape.AddPath(path);
                hasShape = true;
            }
            else
            {
                ApplyPathOp(shape, path, SKPathOp.Union);
            }
        }

        if (!hasShape)
        {
            shape.Dispose();
            return null;
        }

        return shape;
    }

    private static SKPath? BuildAreaBoundaryUnionPath(
        IEnumerable<SharedFrameAreaRead> sharedAreas,
        IEnumerable<AreaRead> segmentAreas,
        IGojoCadSkiaProjection projection)
    {
        var shape = new SKPath();
        var hasShape = false;

        void Include(SKPath? path)
        {
            if (path is null)
            {
                return;
            }

            using (path)
            {
                if (!hasShape)
                {
                    shape.AddPath(path);
                    hasShape = true;
                }
                else
                {
                    ApplyPathOp(shape, path, SKPathOp.Union);
                }
            }
        }

        foreach (var area in sharedAreas)
        {
            Include(BuildClosedSharedFrameBoundaryPath(area.Boundary, projection));
        }

        foreach (var area in segmentAreas)
        {
            Include(BuildClosedSegmentBoundaryPath(area.Boundary, projection));
        }

        if (hasShape)
        {
            return shape;
        }

        shape.Dispose();
        return null;
    }

    private static void ApplyCutPath(SKPath target, SKPath cut)
    {
        if (ApplyPathOp(target, cut, SKPathOp.Difference))
        {
            return;
        }

        target.AddPath(cut);
        target.FillType = SKPathFillType.EvenOdd;
    }

    private static bool ApplyPathOp(SKPath target, SKPath operand, SKPathOp operation)
    {
        using var result = new SKPath();
        if (ApplyPathOpInto(target, operand, operation, result))
        {
            target.Reset();
            target.AddPath(result);
            return true;
        }

        return false;
    }

    private static bool ApplyPathOpInto(SKPath target, SKPath operand, SKPathOp operation, SKPath result)
    {
        if (target.Op(operand, operation, result))
        {
            return true;
        }

        using var simplifiedTarget = target.Simplify();
        using var simplifiedOperand = operand.Simplify();
        result.Reset();
        if (simplifiedTarget is not null &&
            simplifiedOperand is not null &&
            simplifiedTarget.Op(simplifiedOperand, operation, result))
        {
            return true;
        }

        return false;
    }

    private static int DrawAreaUnionOutlineWitness(
        SKCanvas canvas,
        IElement outline,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas,
        bool fill)
    {
        var graph = GraphicReadGraph.From(outline);
        var drawn = 0;
        foreach (var boundary in graph.ReadsOf<ClosedSharedFrameBoundaryRead>())
        {
            using var path = BuildClosedSharedFrameBoundaryPath(boundary, projection);
            if (path is null)
            {
                continue;
            }

            canvas.DrawPath(path, fill ? paints.AntiAreaFill : paints.AreaBoundary);
            AddHit(hitAreas, path.Bounds, fill ? "area union opening" : "area union outline", priority: 2, boundary.Source);
            drawn++;
        }

        foreach (var boundary in graph.ReadsOf<ClosedSegmentBoundaryRead>())
        {
            using var path = BuildClosedSegmentBoundaryPath(boundary, projection);
            if (path is null)
            {
                continue;
            }

            canvas.DrawPath(path, fill ? paints.AntiAreaFill : paints.AreaBoundary);
            AddHit(hitAreas, path.Bounds, fill ? "area union opening" : "area union outline", priority: 2, boundary.Source);
            drawn++;
        }

        return drawn;
    }

    private static int DrawAreaUnionOpeningWitness(
        SKCanvas canvas,
        IElement areaFrame,
        SKPath composite,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas)
    {
        if (areaFrame is Relation frame &&
            GraphicBooleanSurface.TryReadAreaFrame(frame, out var participants))
        {
            var participantReads = participants
                .Select(participant =>
                {
                    var graph = GraphicReadGraph.From(participant);
                    return (
                        PositiveShared: graph.ReadsOf<SharedFrameAreaRead>()
                            .Where(area => !area.IsAntiArea)
                            .DistinctBy(area => ReadSourceId(area.Source))
                            .ToArray(),
                        NegativeShared: graph.ReadsOf<SharedFrameAreaRead>()
                            .Where(area => area.IsAntiArea)
                            .DistinctBy(area => ReadSourceId(area.Source))
                            .ToArray(),
                        PositiveSegment: graph.ReadsOf<AreaRead>()
                            .Where(area => !area.IsAntiArea)
                            .DistinctBy(area => ReadSourceId(area.Source))
                            .ToArray(),
                        NegativeSegment: graph.ReadsOf<AreaRead>()
                            .Where(area => area.IsAntiArea)
                            .DistinctBy(area => ReadSourceId(area.Source))
                            .ToArray());
                })
                .ToArray();
            var drawn = 0;
            for (var index = 0; index < participantReads.Length; index++)
            {
                var participant = participantReads[index];
                var hasLocalPositive = participant.PositiveShared.Length > 0 ||
                    participant.PositiveSegment.Length > 0;
                using var localOpeningClip = hasLocalPositive
                    ? BuildPositiveParticipantsPath(participantReads, index, projection)
                    : null;
                drawn += DrawAreaUnionOpeningWitness(
                    canvas,
                    participant.NegativeShared,
                    participant.NegativeSegment,
                    localOpeningClip,
                    projection,
                    paints,
                    hitAreas);
            }

            return drawn;
        }

        return DrawAreaUnionOpeningWitness(
            canvas,
            GraphicReadGraph.From(areaFrame).ReadsOf<SharedFrameAreaRead>().Where(area => area.IsAntiArea).ToArray(),
            GraphicReadGraph.From(areaFrame).ReadsOf<AreaRead>().Where(area => area.IsAntiArea).ToArray(),
            composite,
            projection,
            paints,
            hitAreas);
    }

    private static int DrawAreaUnionOpeningWitness(
        SKCanvas canvas,
        IReadOnlyList<SharedFrameAreaRead> sharedAreas,
        IReadOnlyList<AreaRead> segmentAreas,
        SKPath? openingClip,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas)
    {
        var drawn = 0;
        foreach (var area in sharedAreas)
        {
            using var path = BuildClosedSharedFrameBoundaryPath(area.Boundary, projection);
            if (path is null)
            {
                continue;
            }

            drawn += DrawAreaUnionOpeningPath(canvas, path, openingClip, paints, hitAreas, area.Source);
        }

        foreach (var area in segmentAreas)
        {
            using var path = BuildClosedSegmentBoundaryPath(area.Boundary, projection);
            if (path is null)
            {
                continue;
            }

            drawn += DrawAreaUnionOpeningPath(canvas, path, openingClip, paints, hitAreas, area.Source);
        }

        return drawn;
    }

    private static int DrawAreaUnionOpeningPath(
        SKCanvas canvas,
        SKPath path,
        SKPath? openingClip,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas,
        IElement source)
    {
        if (openingClip is null)
        {
            canvas.DrawPath(path, paints.AntiAreaFill);
            AddHit(hitAreas, path.Bounds, "area union anti opening", priority: 2, source);
            return 1;
        }

        using var visibleOpening = BuildVisibleOpeningPath(path, openingClip);
        if (visibleOpening is null)
        {
            return 0;
        }

        canvas.DrawPath(visibleOpening, paints.AntiAreaFill);
        AddHit(hitAreas, visibleOpening.Bounds, "area union anti opening", priority: 2, source);
        return 1;
    }

    private static SKPath? BuildPositiveParticipantsPath(
        IReadOnlyList<(
            SharedFrameAreaRead[] PositiveShared,
            SharedFrameAreaRead[] NegativeShared,
            AreaRead[] PositiveSegment,
            AreaRead[] NegativeSegment)> participants,
        int skipParticipant,
        IGojoCadSkiaProjection projection)
    {
        var composite = new SKPath();
        var hasComposite = false;
        for (var index = 0; index < participants.Count; index++)
        {
            if (index == skipParticipant)
            {
                continue;
            }

            using var path = BuildAreaBoundaryUnionPath(
                participants[index].PositiveShared,
                participants[index].PositiveSegment,
                projection);
            if (path is null)
            {
                continue;
            }

            if (!hasComposite)
            {
                composite.AddPath(path);
                hasComposite = true;
            }
            else
            {
                ApplyPathOp(composite, path, SKPathOp.Union);
            }
        }

        if (hasComposite)
        {
            return composite;
        }

        composite.Dispose();
        return null;
    }

    private static SKPath? BuildVisibleOpeningPath(SKPath opening, SKPath composite)
    {
        var visible = new SKPath();
        if (!ApplyPathOpInto(opening, composite, SKPathOp.Difference, visible))
        {
            visible.Dispose();
            visible = new SKPath();
            visible.AddPath(opening);
        }

        if (visible.Bounds.Width <= 0f ||
            visible.Bounds.Height <= 0f)
        {
            visible.Dispose();
            return null;
        }

        return visible;
    }

    private static int DrawReadGraph(
        SKCanvas canvas,
        GraphicReadGraph graph,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas)
    {
        var drawn = 0;
        var repeatControlSources = ReadRepeatControlSources(graph);
        var pointRoles = new HashSet<LocationKey>();
        foreach (var framePoint in graph.ReadsOf<SharedFramePointRead>())
        {
            foreach (var coordinate in framePoint.Coordinates)
            {
                pointRoles.Add(LocationKey.Of(coordinate));
            }
        }

        var angleControlledPathSegments = ReadAngleControlledPathSegmentSources(graph);
        var frameCurveSupportSources = ReadFrameCurveSupportSources(graph);
        var carrierClothoidTransitionSources = ReadCarrierClothoidTransitionSources(graph);
        var carrierInvoluteTransitionSources = ReadCarrierInvoluteTransitionSources(graph);
        var carrierClothoidLengthSources = ReadCarrierClothoidLengthSources(graph);
        var carrierInvoluteSupportSources = ReadCarrierInvoluteSupportSources(graph);

        foreach (var area in graph.ReadsOf<AreaRead>()
            .Where(read => !IsRepeatControlSource(read.Source, repeatControlSources)))
        {
            DrawArea(canvas, area, projection, paints, hitAreas, pointRoles);
            drawn++;
        }

        foreach (var boundary in graph.ReadsOf<ClosedSegmentBoundaryRead>()
            .Where(read => !IsRepeatControlSource(read.Source, repeatControlSources)))
        {
            DrawClosedSegmentBoundary(canvas, boundary, projection, paints, hitAreas);
            drawn++;
        }

        foreach (var radial in graph.ReadsOf<RadialSeedRead>()
            .Where(read => !IsRepeatControlSource(read.Source, repeatControlSources)))
        {
            DrawRadialFlow(canvas, radial, projection, paints, hitAreas);
            drawn++;
        }

        foreach (var sheet in graph.ReadsOf<SheetSeedRead>()
            .Where(read => !IsRepeatControlSource(read.Source, repeatControlSources)))
        {
            DrawSheetSeed(canvas, sheet, projection, paints, hitAreas);
            drawn++;
        }

        foreach (var line in graph.ReadsOf<LineSeedRead>()
            .Where(read => !IsRepeatControlSource(read.Source, repeatControlSources)))
        {
            DrawLineSeed(canvas, line, projection, paints, hitAreas);
            drawn++;
        }

        foreach (var attachment in graph.ReadsOf<RadiusAttachmentRead>()
            .Where(read => !IsRepeatControlSource(read.Source, repeatControlSources)))
        {
            DrawRadiusAttachment(canvas, attachment, projection, paints, hitAreas, pointRoles);
            drawn++;
        }

        drawn += DrawCircumferences(canvas, graph, projection, paints, hitAreas, pointRoles);

        foreach (var span in graph.ReadsOf<CircularSpanRead>()
            .Where(read => !IsRepeatControlSource(read.Source, repeatControlSources)))
        {
            DrawCircumferenceSpan(canvas, span, projection, paints, hitAreas, pointRoles);
            drawn++;
        }

        if (!paints.HideSharedFrameGeometry)
        {
            foreach (var segment in graph.ReadsOf<SharedFrameSegmentRead>()
                .Where(read => !IsRepeatControlSource(read.Source, repeatControlSources))
                .Where(read => !IsFrameCurveSupportSource(read.Source, frameCurveSupportSources)))
            {
                DrawSharedFrameSegment(canvas, segment, projection, paints, hitAreas);
                drawn++;
            }

            foreach (var arc in graph.ReadsOf<SharedFrameArcRead>()
                .Where(read => !IsRepeatControlSource(read.Source, repeatControlSources)))
            {
                DrawSharedFrameArc(canvas, arc, projection, paints, hitAreas);
                drawn++;
            }

            foreach (var quadratic in graph.ReadsOf<SharedFrameQuadraticCurveRead>()
                .Where(read => !IsRepeatControlSource(read.Source, repeatControlSources)))
            {
                DrawSharedFrameQuadraticCurve(canvas, quadratic, projection, paints, hitAreas);
                drawn++;
            }

            foreach (var cubic in graph.ReadsOf<SharedFrameCubicCurveRead>()
                .Where(read => !IsRepeatControlSource(read.Source, repeatControlSources)))
            {
                DrawSharedFrameCubicCurve(canvas, cubic, projection, paints, hitAreas);
                drawn++;
            }

            foreach (var clothoid in graph.ReadsOf<SharedFrameClothoidTransitionRead>()
                .Where(read => !IsRepeatControlSource(read.Source, repeatControlSources)))
            {
                DrawSharedFrameClothoidTransition(canvas, clothoid, projection, paints, hitAreas);
                drawn++;
            }

            foreach (var involute in graph.ReadsOf<SharedFrameInvoluteCircleRead>()
                .Where(read => !IsRepeatControlSource(read.Source, repeatControlSources)))
            {
                DrawSharedFrameInvolute(canvas, involute, projection, paints, hitAreas);
                drawn++;
            }
        }

        foreach (var transition in graph.ReadsOf<CarrierCurveTransitionRead>()
            .Where(read => !IsRepeatControlSource(read.Source, repeatControlSources))
            .Where(read => !IsCarrierClothoidTransitionSource(read.Source, carrierClothoidTransitionSources))
            .Where(read => !IsCarrierInvoluteTransitionSource(read.Source, carrierInvoluteTransitionSources)))
        {
            DrawCarrierTransition(canvas, transition, projection, paints, hitAreas, paints.SharedFrameCurve);
            drawn++;
        }

        foreach (var clothoid in graph.ReadsOf<CarrierClothoidTransitionRead>()
            .Where(read => !IsRepeatControlSource(read.Source, repeatControlSources)))
        {
            DrawCarrierClothoidTransition(canvas, clothoid, projection, paints, hitAreas);
            drawn++;
        }

        foreach (var involute in graph.ReadsOf<CarrierInvoluteCircleRead>()
            .Where(read => !IsRepeatControlSource(read.Source, repeatControlSources)))
        {
            DrawCarrierInvolute(canvas, involute, projection, paints, hitAreas);
            pointRoles.Add(LocationKey.Of(involute.UnwoundLength.Start.Location));
            pointRoles.Add(LocationKey.Of(involute.UnwoundLength.End.Location));
            drawn++;
        }

        foreach (var attachment in graph.ReadsOf<SharedFramePointAttachmentRead>()
            .Where(read => !IsRepeatControlSource(read.Source, repeatControlSources)))
        {
            DrawSharedFrameAttachment(canvas, attachment, projection, paints, hitAreas, pointRoles);
            drawn++;
        }

        foreach (var segment in graph.ReadsOf<PositiveSegmentRead>()
            .Where(segment => !IsRepeatControlSource(segment.Source, repeatControlSources))
            .Where(segment => !IsCarrierClothoidLengthSource(segment.Source, carrierClothoidLengthSources))
            .Where(segment => !IsCarrierInvoluteSupportSource(segment.Source, carrierInvoluteSupportSources))
            .Where(segment => !IsCircularCarrier(segment.Start.Location.Carrier)))
        {
            var partitions = graph.ReadsOf<SegmentPartitionRead>()
                .Where(partition => ReferenceEquals(partition.Segment, segment))
                .Select(partition => partition.Point);
            DrawPositiveSegment(
                canvas,
                segment,
                partitions,
                IsAngleControlledPathSegment(segment, angleControlledPathSegments),
                projection,
                paints,
                hitAreas,
                pointRoles);
            drawn++;
        }

        foreach (var segment in graph.ReadsOf<InverseSegmentRead>()
            .Where(read => !IsRepeatControlSource(read.Source, repeatControlSources)))
        {
            DrawInverseSegment(canvas, segment, projection, paints, hitAreas);
            drawn++;
        }

        foreach (var segment in graph.ReadsOf<DirectedSegmentRead>()
            .Where(read => !IsRepeatControlSource(read.Source, repeatControlSources)))
        {
            DrawDirectedSegment(canvas, segment, projection, paints, hitAreas);
            drawn++;
        }

        var sharedFrameAreaPaletteIndex = 0;
        foreach (var area in graph.ReadsOf<SharedFrameAreaRead>()
            .Where(read => !IsRepeatControlSource(read.Source, repeatControlSources)))
        {
            DrawSharedFrameArea(
                canvas,
                area,
                projection,
                paints,
                hitAreas,
                pointRoles,
                area.IsAntiArea ? -1 : sharedFrameAreaPaletteIndex++);
            drawn++;
        }

        foreach (var branch in graph.ReadsOf<BranchRead>())
        {
            DrawBranchMergeNode(canvas, branch.Junction, projection, paints.BranchNode, "branch", paints, hitAreas, pointRoles, branch.Source);
            drawn++;
        }

        foreach (var merge in graph.ReadsOf<MergeRead>())
        {
            DrawBranchMergeNode(canvas, merge.Junction, projection, paints.MergeNode, "merge", paints, hitAreas, pointRoles, merge.Source);
            drawn++;
        }

        foreach (var pathGraph in graph.ReadsOf<PathGraphRead>())
        {
            drawn += DrawPathGraphNodes(canvas, pathGraph, projection, paints, hitAreas, pointRoles, repeatControlSources);
        }

        foreach (var segment in graph.ReadsOf<PositiveSegmentRead>()
            .Where(segment => !IsRepeatControlSource(segment.Source, repeatControlSources))
            .Where(segment => IsAngleControlledPathSegment(segment, angleControlledPathSegments)))
        {
            DrawAngleControlledSegmentOverlay(canvas, segment, projection, paints);
            drawn++;
        }

        foreach (var point in graph.ReadsOf<PointRead>()
            .Where(point => !IsRepeatControlSource(point.Source, repeatControlSources))
            .Where(point => !point.Location.Carrier.IsUnknown))
        {
            var key = LocationKey.Of(point);
            if (pointRoles.Contains(key))
            {
                continue;
            }

            if (IsCircularCarrier(point.Location.Carrier))
            {
                var circle = ReadCircleProjection(point.Location, projection);
                DrawCircumferenceTick(canvas, circle.Center, point.Location, projection, paints, hitAreas, "circumference tick", priority: 5, point.Source);
                pointRoles.Add(key);
                drawn++;
            }
            else
            {
                DrawDot(canvas, point, projection, paints, hitAreas);
                drawn++;
            }
        }

        return drawn;
    }

    private static HashSet<ulong> ReadRepeatControlSources(GraphicReadGraph graph)
    {
        var sources = new HashSet<ulong>();
        foreach (var repeat in graph.ReadsOf<GraphicRepeatRead>())
        {
            AddRelationTree(repeat.Source, sources);
        }

        return sources;
    }

    private static void AddRelationTree(IElement source, HashSet<ulong> sources)
    {
        if (source is not Relation relation ||
            !sources.Add(relation.Id))
        {
            return;
        }

        AddRelationTree(relation.Left, sources);
        AddRelationTree(relation.Right, sources);
    }

    private static bool IsRepeatControlSource(IElement source, IReadOnlySet<ulong> repeatControlSources) =>
        source is Relation relation &&
        repeatControlSources.Contains(relation.Id);

    private static HashSet<ulong> ReadFrameCurveSupportSources(GraphicReadGraph graph)
    {
        var sources = new HashSet<ulong>();
        void Add(IElement source)
        {
            if (source is Relation relation)
            {
                sources.Add(relation.Id);
            }
        }

        foreach (var arc in graph.ReadsOf<SharedFrameArcRead>())
        {
            Add(arc.Span.Source);
        }

        foreach (var curve in graph.ReadsOf<SharedFrameQuadraticCurveRead>())
        {
            Add(curve.Chord.Source);
        }

        foreach (var curve in graph.ReadsOf<SharedFrameCubicCurveRead>())
        {
            Add(curve.Chord.Source);
            Add(curve.ControlSpan.Source);
        }

        foreach (var clothoid in graph.ReadsOf<SharedFrameClothoidTransitionRead>())
        {
            Add(clothoid.Transition.Incoming.Source);
            Add(clothoid.Transition.Outgoing.Source);
            Add(clothoid.Length.Source);
        }

        foreach (var involute in graph.ReadsOf<SharedFrameInvoluteCircleRead>())
        {
            Add(involute.UnwoundLength.Source);
        }

        foreach (var transition in graph.ReadsOf<CarrierCurveTransitionRead>())
        {
            Add(transition.Incoming.Source);
            Add(transition.Outgoing.Source);
        }

        foreach (var clothoid in graph.ReadsOf<CarrierClothoidTransitionRead>())
        {
            Add(clothoid.Length.Source);
        }

        foreach (var involute in graph.ReadsOf<CarrierInvoluteCircleRead>())
        {
            Add(involute.UnwoundLength.Source);
        }

        return sources;
    }

    private static bool IsFrameCurveSupportSource(IElement source, IReadOnlySet<ulong> frameCurveSupportSources) =>
        source is Relation relation &&
        frameCurveSupportSources.Contains(relation.Id);

    private static HashSet<ulong> ReadCarrierClothoidTransitionSources(GraphicReadGraph graph)
    {
        var sources = new HashSet<ulong>();
        foreach (var clothoid in graph.ReadsOf<CarrierClothoidTransitionRead>())
        {
            if (clothoid.Transition.Source is Relation relation)
            {
                sources.Add(relation.Id);
            }
        }

        return sources;
    }

    private static bool IsCarrierClothoidTransitionSource(
        IElement source,
        IReadOnlySet<ulong> carrierClothoidTransitionSources) =>
        source is Relation relation &&
        carrierClothoidTransitionSources.Contains(relation.Id);

    private static HashSet<ulong> ReadCarrierInvoluteTransitionSources(GraphicReadGraph graph)
    {
        var sources = new HashSet<ulong>();
        foreach (var involute in graph.ReadsOf<CarrierInvoluteCircleRead>())
        {
            if (involute.Source is Relation relation)
            {
                sources.Add(relation.Id);
            }
        }

        return sources;
    }

    private static bool IsCarrierInvoluteTransitionSource(
        IElement source,
        IReadOnlySet<ulong> carrierInvoluteTransitionSources) =>
        source is Relation relation &&
        carrierInvoluteTransitionSources.Contains(relation.Id);

    private static HashSet<ulong> ReadCarrierClothoidLengthSources(GraphicReadGraph graph)
    {
        var sources = new HashSet<ulong>();
        foreach (var clothoid in graph.ReadsOf<CarrierClothoidTransitionRead>())
        {
            if (clothoid.Length.Source is Relation relation)
            {
                sources.Add(relation.Id);
            }
        }

        return sources;
    }

    private static bool IsCarrierClothoidLengthSource(
        IElement source,
        IReadOnlySet<ulong> carrierClothoidLengthSources) =>
        source is Relation relation &&
        carrierClothoidLengthSources.Contains(relation.Id);

    private static HashSet<ulong> ReadCarrierInvoluteSupportSources(GraphicReadGraph graph)
    {
        var sources = new HashSet<ulong>();
        foreach (var involute in graph.ReadsOf<CarrierInvoluteCircleRead>())
        {
            if (involute.UnwoundLength.Source is Relation relation)
            {
                sources.Add(relation.Id);
            }
        }

        return sources;
    }

    private static bool IsCarrierInvoluteSupportSource(
        IElement source,
        IReadOnlySet<ulong> carrierInvoluteSupportSources) =>
        source is Relation relation &&
        carrierInvoluteSupportSources.Contains(relation.Id);

    private static int DrawCircumferences(
        SKCanvas canvas,
        GraphicReadGraph graph,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas,
        HashSet<LocationKey> pointRoles)
    {
        var drawn = 0;
        var circularLocations = graph.ReadsOf<CircularMarkerRead>()
            .Select(marker => new LocatedSource(marker.Point.Location, marker.Source))
            .Concat(graph.ReadsOf<CircularSpanRead>().SelectMany(span => new[]
            {
                new LocatedSource(span.Segment.Start.Location, span.Source),
                new LocatedSource(span.Segment.End.Location, span.Source)
            }))
            .Where(entry => IsCircularCarrier(entry.Location.Carrier))
            .GroupBy(entry => entry.Location.Carrier.Id);

        foreach (var group in circularLocations)
        {
            var marker = group.First();
            var circle = ReadCircleProjection(marker.Location, projection);
            var paint = projection.UsesSampledCarrierShape(marker.Location.Carrier)
                ? paints.CircumferenceTension
                : paints.Circumference;

            canvas.DrawCircle(circle.Center, circle.Radius, paint);
            AddHit(hitAreas, CircleBounds(circle.Center, circle.Radius), "circumference", priority: 1, marker.Source);
            drawn++;

            var origin = ReadCircularOrigin(marker.Location.Carrier);
            DrawBasisRect(canvas, origin, projection, paints, hitAreas, "circumference basis origin", priority: 6, marker.Source);
            pointRoles.Add(LocationKey.Of(origin));

            var observedTicks = group.GroupBy(entry => LocationKey.Of(entry.Location))
                .Select(group => group.First())
                .ToArray();
            DrawCircumferenceTickLadder(
                canvas,
                circle.Center,
                marker.Location.Carrier,
                observedTicks.Select(entry => entry.Location),
                projection,
                paints);

            foreach (var entry in observedTicks)
            {
                DrawCircumferenceTick(canvas, circle.Center, entry.Location, projection, paints, hitAreas, "circumference tick", priority: 5, entry.Source);
                pointRoles.Add(LocationKey.Of(entry.Location));
                drawn++;
            }
        }

        return drawn;
    }

    private static void DrawCircumferenceSpan(
        SKCanvas canvas,
        CircularSpanRead span,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas,
        HashSet<LocationKey> pointRoles)
    {
        var start = span.Segment.Start.Location;
        var end = span.Segment.End.Location;
        var circle = ReadCircleProjection(start, projection);
        var startPoint = projection.Project(start);
        var endPoint = projection.Project(end);

        var bounds = CircleBounds(circle.Center, circle.Radius);
        var startAngle = AngleDegrees(circle.Center, startPoint);
        var endAngle = AngleDegrees(circle.Center, endPoint);
        var sweep = ShortestSweep(startAngle, endAngle);
        var paint = projection.UsesSampledCarrierShape(start.Carrier) ||
            projection.UsesSampledProjection(start) ||
            projection.UsesSampledProjection(end)
                ? paints.CircumferenceSpanTension
                : paints.CircumferenceSpan;
        canvas.DrawArc(bounds, startAngle, sweep, false, paint);
        AddHit(hitAreas, bounds, "circumference span", priority: 3, span.Source);

        DrawCircumferenceTick(canvas, circle.Center, start, projection, paints, hitAreas, "circumference tick", priority: 5, span.Source);
        DrawCircumferenceTick(canvas, circle.Center, end, projection, paints, hitAreas, "circumference tick", priority: 5, span.Source);
        pointRoles.Add(LocationKey.Of(start));
        pointRoles.Add(LocationKey.Of(end));
    }

    private static void DrawDot(
        SKCanvas canvas,
        PointRead point,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas)
    {
        var location = point.Location;
        var center = projection.Project(location);
        canvas.DrawCircle(center, paints.DotRadius, paints.Dot);
        AddHit(hitAreas, CenteredRect(center, paints.DotRadius * 2.4f), $"point {location.Carrier.Id}:{location.Ticks}", priority: 6, point.Source);
    }

    private static void DrawRadialFlow(
        SKCanvas canvas,
        RadialSeedRead radial,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas)
    {
        var center = projection.Project(radial.Point.Location);
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateRadialGradient(
                center,
                paints.RadialRadius,
                [paints.RadialInner, paints.RadialOuter],
                [0f, 1f],
                SKShaderTileMode.Clamp),
            Style = SKPaintStyle.Fill
        };
        canvas.DrawCircle(center, paints.RadialRadius, paint);
        AddHit(hitAreas, CircleBounds(center, paints.RadialRadius), "radial flow", priority: 0, radial.Source);
    }

    private static void DrawPositiveSegment(
        SKCanvas canvas,
        PositiveSegmentRead segment,
        IEnumerable<PointRead> points,
        bool isAngleControlledPathSegment,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas,
        HashSet<LocationKey> pointRoles)
    {
        var start = segment.Start.Location;
        var end = segment.End.Location;
        var startPoint = projection.Project(start);
        var endPoint = projection.Project(end);
        var isSampled = projection.UsesSampledProjection(start) || projection.UsesSampledProjection(end);
        var paint = isAngleControlledPathSegment
            ? (isSampled ? paints.AngleControlledSegmentTension : paints.AngleControlledSegment)
            : (isSampled ? paints.SegmentTension : paints.Segment);

        canvas.DrawLine(startPoint, endPoint, paint);
        DrawEndpointBar(canvas, startPoint, endPoint, paints);
        DrawEndpointBar(canvas, endPoint, startPoint, paints);
        AddHit(hitAreas, LineBounds(startPoint, endPoint, 10f), "segment", priority: 2, segment.Source);

        foreach (var point in points)
        {
            DrawSegmentTick(canvas, point.Location, startPoint, endPoint, projection, paints, hitAreas, "segment tick", priority: 5, point.Source);
            pointRoles.Add(LocationKey.Of(point));
        }
    }

    private static void DrawLineSeed(
        SKCanvas canvas,
        LineSeedRead seed,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas)
    {
        var location = seed.Span.Left.Location;
        var center = projection.Project(location);
        var before = projection.Project(new CarrierLocation(location.Carrier, location.Ticks - 1));
        var after = projection.Project(new CarrierLocation(location.Carrier, location.Ticks + 1));
        var normal = PerpendicularUnit(before, after);
        var half = paints.LineSeedLength * 0.5f;
        var start = new SKPoint(center.X - normal.X * half, center.Y - normal.Y * half);
        var end = new SKPoint(center.X + normal.X * half, center.Y + normal.Y * half);
        var paint = projection.UsesSampledProjection(location)
            ? paints.LineSeedTension
            : paints.LineSeed;

        canvas.DrawLine(start, end, paint);
        AddHit(hitAreas, LineBounds(start, end, 8f), "line seed", priority: 2, seed.Source);
    }

    private static void DrawAngleControlledSegmentOverlay(
        SKCanvas canvas,
        PositiveSegmentRead segment,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints)
    {
        var start = segment.Start.Location;
        var end = segment.End.Location;
        var startPoint = projection.Project(start);
        var endPoint = projection.Project(end);
        var paint = projection.UsesSampledProjection(start) || projection.UsesSampledProjection(end)
            ? paints.AngleControlledSegmentTension
            : paints.AngleControlledSegment;
        canvas.DrawLine(startPoint, endPoint, paint);
    }

    private static HashSet<ulong> ReadAngleControlledPathSegmentSources(GraphicReadGraph graph)
    {
        var circularWitnesses = graph.ReadsOf<LinearBasisWitnessRead>()
            .Where(witness => witness.WitnessPoint.CircularMarker is not null)
            .ToArray();
        return [.. graph.ReadsOf<OpenSegmentPathRead>()
            .SelectMany(path => path.Segments)
            .Where(segment => circularWitnesses.Any(witness =>
                witness.LinearCarrier.SameIdentityAs(segment.Start.Location.Carrier)))
            .Select(segment => segment.Source)
            .OfType<Relation>()
            .Select(relation => relation.Id)];
    }

    private static bool IsAngleControlledPathSegment(
        PositiveSegmentRead segment,
        IReadOnlySet<ulong> angleControlledPathSegments) =>
        segment.Source is Relation relation &&
        angleControlledPathSegments.Contains(relation.Id);

    private static void DrawSheetSeed(
        SKCanvas canvas,
        SheetSeedRead seed,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas)
    {
        var start = projection.Project(seed.Span.Start.Location);
        var end = projection.Project(seed.Span.End.Location);
        var normal = PerpendicularUnit(start, end);
        var half = paints.SheetSeedWidth * 0.5f;
        using var path = new SKPath();
        path.MoveTo(start.X + normal.X * half, start.Y + normal.Y * half);
        path.LineTo(end.X + normal.X * half, end.Y + normal.Y * half);
        path.LineTo(end.X - normal.X * half, end.Y - normal.Y * half);
        path.LineTo(start.X - normal.X * half, start.Y - normal.Y * half);
        path.Close();
        var edgePaint = projection.UsesSampledProjection(seed.Span.Start.Location) ||
            projection.UsesSampledProjection(seed.Span.End.Location)
                ? paints.SheetSeedEdgeTension
                : paints.SheetSeedEdge;

        canvas.DrawPath(path, paints.SheetSeed);
        canvas.DrawPath(path, edgePaint);
        AddHit(hitAreas, path.Bounds, "sheet seed", priority: 1, seed.Source);
    }

    private static void DrawRadiusAttachment(
        SKCanvas canvas,
        RadiusAttachmentRead attachment,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas,
        HashSet<LocationKey> pointRoles)
    {
        var start = projection.Project(attachment.BasePoint.Location);
        var end = projection.Project(attachment.Marker.Point.Location);
        var paint = projection.UsesSampledProjection(attachment.BasePoint.Location) ||
            projection.UsesSampledProjection(attachment.Marker.Point.Location)
                ? paints.RadiusAttachmentTension
                : paints.RadiusAttachment;
        canvas.DrawLine(start, end, paint);
        AddHit(hitAreas, LineBounds(start, end, 7f), "radius attachment", priority: 2, attachment.Source);
        pointRoles.Add(LocationKey.Of(attachment.BasePoint.Location));
        pointRoles.Add(LocationKey.Of(attachment.Marker.Point.Location));
    }

    private static void DrawInverseSegment(
        SKCanvas canvas,
        InverseSegmentRead segment,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas)
    {
        var start = segment.Start.Location;
        var end = segment.End.Location;
        var startPoint = projection.Project(start);
        var endPoint = projection.Project(end);
        var paint = projection.UsesSampledProjection(start) || projection.UsesSampledProjection(end)
            ? paints.SegmentTension
            : paints.Segment;

        canvas.DrawLine(startPoint, endPoint, paint);
        DrawEndpointBar(canvas, startPoint, endPoint, paints);
        DrawEndpointBar(canvas, endPoint, startPoint, paints);
        AddHit(hitAreas, LineBounds(startPoint, endPoint, 10f), "inverse segment", priority: 2, segment.Source);
    }

    private static void DrawDirectedSegment(
        SKCanvas canvas,
        DirectedSegmentRead segment,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas)
    {
        if (IsCircularCarrier(segment.PositiveEnd.Location.Carrier))
        {
            DrawDirectedCircularSpan(canvas, segment, projection, paints, hitAreas);
            return;
        }

        var startPoint = projection.Project(segment.InverseStart.Location);
        var endPoint = projection.Project(segment.PositiveEnd.Location);
        var paint = projection.UsesSampledProjection(segment.InverseStart.Location) ||
            projection.UsesSampledProjection(segment.PositiveEnd.Location)
                ? paints.DirectedSegmentTension
                : paints.DirectedSegment;

        canvas.DrawLine(startPoint, endPoint, paint);
        DrawArrowTip(canvas, startPoint, endPoint, paints, paint);
        AddHit(hitAreas, LineBounds(startPoint, endPoint, 12f), "directed segment", priority: 4, segment.Source);
    }

    private static void DrawDirectedCircularSpan(
        SKCanvas canvas,
        DirectedSegmentRead segment,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas)
    {
        var circle = ReadCircleProjection(segment.PositiveEnd.Location, projection);
        var startPoint = projection.Project(segment.InverseStart.Location);
        var endPoint = projection.Project(segment.PositiveEnd.Location);
        var bounds = CircleBounds(circle.Center, circle.Radius);
        var startAngle = AngleDegrees(circle.Center, startPoint);
        var endAngle = AngleDegrees(circle.Center, endPoint);
        var sweep = ShortestSweep(startAngle, endAngle);
        var paint = projection.UsesSampledProjection(segment.InverseStart.Location) ||
            projection.UsesSampledProjection(segment.PositiveEnd.Location)
                ? paints.DirectedSegmentTension
                : paints.DirectedSegment;

        canvas.DrawArc(bounds, startAngle, sweep, false, paint);
        var arrowSweep = MathF.Abs(sweep) <= 0.001f ? 1f : MathF.Sign(sweep);
        var tangentStart = PointOnCircle(circle.Center, circle.Radius, endAngle - arrowSweep * 7f);
        DrawArrowTip(canvas, tangentStart, endPoint, paints, paint);
        AddHit(hitAreas, bounds, "directed arc", priority: 4, segment.Source);
    }

    private static void DrawSharedFrameSegment(
        SKCanvas canvas,
        SharedFrameSegmentRead segment,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas)
    {
        var start = projection.ProjectFrame(segment.Start);
        var end = projection.ProjectFrame(segment.End);

        canvas.DrawLine(start, end, paints.SharedFrameSegment);
        canvas.DrawCircle(start, paints.DotRadius * 0.62f, paints.SharedFramePoint);
        canvas.DrawCircle(end, paints.DotRadius * 0.62f, paints.SharedFramePoint);
        AddHit(hitAreas, LineBounds(start, end, 9f), "shared-frame segment", priority: 3, segment.Source);
    }

    private static void DrawSharedFrameArc(
        SKCanvas canvas,
        SharedFrameArcRead arc,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas)
    {
        var center = projection.ProjectFrame(arc.Center);
        var start = projection.ProjectFrame(arc.Start);
        var end = projection.ProjectFrame(arc.End);
        var radius = MathF.Max(0.1f, (Distance(center, start) + Distance(center, end)) * 0.5f);
        var bounds = CircleBounds(center, radius);
        var startAngle = AngleDegrees(center, start);
        var endAngle = AngleDegrees(center, end);
        var sweep = ShortestSweep(startAngle, endAngle);

        canvas.DrawArc(bounds, startAngle, sweep, false, paints.SharedFrameCurve);
        canvas.DrawCircle(start, paints.DotRadius * 0.52f, paints.SharedFramePoint);
        canvas.DrawCircle(end, paints.DotRadius * 0.52f, paints.SharedFramePoint);
        DrawFramePointBox(canvas, center, paints, 0.72f);
        AddHit(hitAreas, bounds, "shared-frame arc", priority: 3, arc.Source);
    }

    private static void DrawSharedFrameQuadraticCurve(
        SKCanvas canvas,
        SharedFrameQuadraticCurveRead curve,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas)
    {
        var start = projection.ProjectFrame(curve.Start);
        var control = projection.ProjectFrame(curve.Control);
        var end = projection.ProjectFrame(curve.End);
        using var path = new SKPath();
        path.MoveTo(start);
        path.QuadTo(control, end);
        canvas.DrawPath(path, paints.SharedFrameCurve);
        DrawFramePointBox(canvas, control, paints, 0.64f);
        AddHit(hitAreas, path.Bounds, "quadratic curve", priority: 3, curve.Source);
    }

    private static void DrawSharedFrameCubicCurve(
        SKCanvas canvas,
        SharedFrameCubicCurveRead curve,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas)
    {
        var start = projection.ProjectFrame(curve.Start);
        var first = projection.ProjectFrame(curve.FirstControl);
        var second = projection.ProjectFrame(curve.SecondControl);
        var end = projection.ProjectFrame(curve.End);
        using var path = new SKPath();
        path.MoveTo(start);
        path.CubicTo(first, second, end);
        canvas.DrawPath(path, paints.SharedFrameCurve);
        DrawFramePointBox(canvas, first, paints, 0.58f);
        DrawFramePointBox(canvas, second, paints, 0.58f);
        AddHit(hitAreas, path.Bounds, "cubic curve", priority: 3, curve.Source);
    }

    private static void DrawSharedFrameTransition(
        SKCanvas canvas,
        SharedFrameCurveTransitionRead transition,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas,
        SKPaint paint,
        string text = "curve transition")
    {
        var start = projection.ProjectFrame(transition.Incoming.Start);
        var junction = projection.ProjectFrame(transition.Junction);
        var end = projection.ProjectFrame(transition.Outgoing.End);
        using var path = new SKPath();
        path.MoveTo(start);
        path.QuadTo(junction, end);
        canvas.DrawPath(path, paint);
        DrawFramePointBox(canvas, junction, paints, 0.64f);
        AddHit(hitAreas, path.Bounds, text, priority: 3, transition.Source);
    }

    private static void DrawSharedFrameClothoidTransition(
        SKCanvas canvas,
        SharedFrameClothoidTransitionRead clothoid,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas)
    {
        var transition = clothoid.Transition;
        var start = projection.ProjectFrame(transition.Incoming.Start);
        var end = projection.ProjectFrame(transition.Outgoing.End);
        var incomingStart = projection.ProjectFrame(transition.Incoming.Start);
        var incomingEnd = projection.ProjectFrame(transition.Incoming.End);
        var outgoingStart = projection.ProjectFrame(transition.Outgoing.Start);
        var outgoingEnd = projection.ProjectFrame(transition.Outgoing.End);
        var startTangent = Unit(incomingStart, incomingEnd);
        var endTangent = Unit(outgoingStart, outgoingEnd);
        var lengthHint = ProjectedLength(clothoid.Length, projection);
        using var path = BuildClothoidPath(
            start,
            end,
            startTangent,
            endTangent,
            startCurvature: 0f,
            endCurvature: 0f,
            lengthHint);

        canvas.DrawPath(path, paints.SharedFrameCurveTension);
        DrawFramePointBox(canvas, projection.ProjectFrame(transition.Junction), paints, 0.64f);
        AddHit(hitAreas, path.Bounds, "clothoid transition", priority: 3, clothoid.Source);
    }

    private static void DrawSharedFrameInvolute(
        SKCanvas canvas,
        SharedFrameInvoluteCircleRead involute,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas)
    {
        var center = projection.ProjectFrame(involute.BaseArc.Center);
        var start = projection.ProjectFrame(involute.BaseArc.Start);
        var end = projection.ProjectFrame(involute.BaseArc.End);
        var length = ProjectedLength(involute.UnwoundLength, projection);
        using var path = BuildInvolutePath(center, start, end, length);
        canvas.DrawPath(path, paints.SharedFrameCurveTension);
        AddHit(hitAreas, path.Bounds, "involute circle", priority: 3, involute.Source);
    }

    private static void DrawCarrierTransition(
        SKCanvas canvas,
        CarrierCurveTransitionRead transition,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas,
        SKPaint paint,
        string text = "carrier transition")
    {
        var start = projection.Project(transition.Incoming.Start.Location);
        var entry = projection.Project(transition.Entry.Location);
        var exit = projection.Project(transition.Exit.Location);
        var end = projection.Project(transition.Outgoing.End.Location);
        using var path = new SKPath();
        if (transition.Junction is null)
        {
            path.MoveTo(entry);
            path.CubicTo(start, end, exit);
        }
        else
        {
            var junction = projection.Project(transition.Junction.Location);
            path.MoveTo(start);
            path.QuadTo(junction, end);
            canvas.DrawCircle(junction, paints.PathGraphNodeRadius * 0.72f, paints.PathGraphBranchNode);
        }

        canvas.DrawPath(path, paint);
        AddHit(hitAreas, path.Bounds, text, priority: 3, transition.Source);
    }

    private static void DrawCarrierClothoidTransition(
        SKCanvas canvas,
        CarrierClothoidTransitionRead clothoid,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas)
    {
        var transition = clothoid.Transition;
        var startPoint = transition.Junction is null
            ? transition.Entry
            : transition.Incoming.Start;
        var endPoint = transition.Junction is null
            ? transition.Exit
            : transition.Outgoing.End;
        var start = projection.Project(startPoint.Location);
        var end = projection.Project(endPoint.Location);
        var startTangent = ReadPathTangent(transition.Incoming, startPoint, projection);
        var endTangent = ReadPathTangent(transition.Outgoing, endPoint, projection);
        var startCurvature = ReadPathCurvature(transition.Incoming, startPoint, projection);
        var endCurvature = ReadPathCurvature(transition.Outgoing, endPoint, projection);
        var lengthHint = ProjectedLength(clothoid.Length, projection);
        using var path = BuildClothoidPath(
            start,
            end,
            startTangent,
            endTangent,
            startCurvature,
            endCurvature,
            lengthHint);

        canvas.DrawPath(path, paints.SharedFrameCurveTension);
        if (transition.Junction is not null)
        {
            var junction = projection.Project(transition.Junction.Location);
            canvas.DrawCircle(junction, paints.PathGraphNodeRadius * 0.72f, paints.PathGraphBranchNode);
        }

        AddHit(hitAreas, path.Bounds, ReadCarrierClothoidText(transition), priority: 3, clothoid.Source);
    }

    private static void DrawCarrierInvolute(
        SKCanvas canvas,
        CarrierInvoluteCircleRead involute,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas)
    {
        var center = projection.Project(involute.BaseArc.Origin);
        var start = projection.Project(involute.BaseArc.Segment.Start.Location);
        var end = projection.Project(involute.BaseArc.Segment.End.Location);
        var length = ReadCarrierInvoluteLength(involute, center, start, end);
        using var path = BuildInvolutePath(center, start, end, length);
        canvas.DrawPath(path, paints.SharedFrameCurveTension);
        AddHit(hitAreas, path.Bounds, "carrier involute", priority: 3, involute.Source);
    }

    private static float ReadCarrierInvoluteLength(
        CarrierInvoluteCircleRead involute,
        SKPoint center,
        SKPoint start,
        SKPoint end)
    {
        var radius = MathF.Max(1f, (Distance(center, start) + Distance(center, end)) * 0.5f);
        var sweep = MathF.Abs(ShortestSweep(AngleDegrees(center, start), AngleDegrees(center, end)));
        if (sweep < 0.01f)
        {
            sweep = 42f;
        }

        var arcLength = sweep * MathF.PI / 180f * radius;
        var baseTicks = Math.Abs(
            involute.BaseArc.Segment.End.Location.Ticks -
            involute.BaseArc.Segment.Start.Location.Ticks);
        var lengthTicks =
            involute.UnwoundLength.End.Location.Ticks -
            involute.UnwoundLength.Start.Location.Ticks;
        if (baseTicks <= 0 || lengthTicks == 0)
        {
            return arcLength;
        }

        return arcLength * lengthTicks / baseTicks;
    }

    private static SKPath BuildInvolutePath(
        SKPoint center,
        SKPoint start,
        SKPoint end,
        float unwoundLength)
    {
        var radius = MathF.Max(1f, (Distance(center, start) + Distance(center, end)) * 0.5f);
        var startAngle = AngleDegrees(center, start);
        var endAngle = AngleDegrees(center, end);
        var sweep = ShortestSweep(startAngle, endAngle);
        if (MathF.Abs(sweep) < 0.01f)
        {
            sweep = 42f;
        }

        var arcLength = MathF.Abs(sweep) * MathF.PI / 180f * radius;
        var length = MathF.Abs(unwoundLength) > 0.5f
            ? unwoundLength
            : arcLength;
        var path = new SKPath();
        const int Steps = 36;
        for (var index = 0; index <= Steps; index++)
        {
            var u = index / (float)Steps;
            var angle = startAngle + sweep * u;
            var basePoint = PointOnCircle(center, radius, angle);
            var radial = Unit(center, basePoint);
            var tangent = sweep >= 0f
                ? new SKPoint(radial.Y, -radial.X)
                : new SKPoint(-radial.Y, radial.X);
            var point = Add(basePoint, Scale(tangent, length * u));
            if (index == 0)
            {
                path.MoveTo(point);
            }
            else
            {
                path.LineTo(point);
            }
        }

        return path;
    }

    private static SKPath BuildClothoidPath(
        SKPoint start,
        SKPoint end,
        SKPoint startTangent,
        SKPoint endTangent,
        float startCurvature,
        float endCurvature,
        float lengthHint)
    {
        var distance = Math.Max(1f, Distance(start, end));
        var hinted = lengthHint > 0.5f
            ? Math.Min(lengthHint * 0.55f, distance * 0.82f)
            : distance * 0.38f;
        var startHandle = hinted * CurvatureHandleScale(startCurvature, distance);
        var endHandle = hinted * CurvatureHandleScale(endCurvature, distance);
        var first = Add(start, Scale(startTangent, startHandle));
        var second = Add(end, Scale(endTangent, -endHandle));
        var path = new SKPath();
        path.MoveTo(start);
        path.CubicTo(first, second, end);
        return path;
    }

    private static float CurvatureHandleScale(float curvature, float distance)
    {
        var normalized = MathF.Min(1.4f, MathF.Abs(curvature) * distance);
        return 1f / (1f + normalized * 0.24f);
    }

    private static float ProjectedLength(SharedFrameSegmentRead segment, IGojoCadSkiaProjection projection) =>
        Distance(projection.ProjectFrame(segment.Start), projection.ProjectFrame(segment.End));

    private static float ProjectedLength(PathSegmentRead segment, IGojoCadSkiaProjection projection) =>
        Distance(projection.Project(segment.Start.Location), projection.Project(segment.End.Location));

    private static SKPoint ReadPathTangent(
        PathSegmentRead segment,
        PointRead endpoint,
        IGojoCadSkiaProjection projection)
    {
        var start = projection.Project(segment.Start.Location);
        var end = projection.Project(segment.End.Location);
        if (!IsCircularCarrier(endpoint.Location.Carrier))
        {
            return Unit(start, end);
        }

        var delta = segment.End.Location.Ticks - segment.Start.Location.Ticks;
        if (delta == 0)
        {
            return Unit(start, end);
        }

        var direction = Math.Sign(delta);
        var step = Math.Max(1, Math.Abs(delta) / 16);
        var endpointPoint = projection.Project(endpoint.Location);
        if (SamePathLocation(endpoint.Location, segment.Start.Location))
        {
            var sample = new CarrierLocation(
                endpoint.Location.Carrier,
                GraphicStructure.Site(segment.Start.Location.Ticks + direction * step));
            return Unit(endpointPoint, projection.Project(sample));
        }

        if (SamePathLocation(endpoint.Location, segment.End.Location))
        {
            var sample = new CarrierLocation(
                endpoint.Location.Carrier,
                GraphicStructure.Site(segment.End.Location.Ticks - direction * step));
            return Unit(projection.Project(sample), endpointPoint);
        }

        return Unit(start, end);
    }

    private static float ReadPathCurvature(
        PathSegmentRead segment,
        PointRead endpoint,
        IGojoCadSkiaProjection projection)
    {
        if (!IsCircularCarrier(endpoint.Location.Carrier))
        {
            return 0f;
        }

        var center = projection.Project(ReadCircularOrigin(endpoint.Location.Carrier));
        var point = projection.Project(endpoint.Location);
        var radius = Math.Max(1f, Distance(center, point));
        var start = projection.Project(segment.Start.Location);
        var end = projection.Project(segment.End.Location);
        var sweep = ShortestSweep(AngleDegrees(center, start), AngleDegrees(center, end));
        return MathF.Sign(sweep == 0f ? 1f : sweep) / radius;
    }

    private static string ReadCarrierClothoidText(CarrierCurveTransitionRead transition)
    {
        if (transition.IsLineToArc)
        {
            return "line to arc clothoid";
        }

        if (transition.IsArcToLine)
        {
            return "arc to line clothoid";
        }

        if (transition.IsArcToArc)
        {
            return "arc to arc clothoid";
        }

        return transition.IsBridged
            ? "bridged line clothoid"
            : "line to line clothoid";
    }

    private static void DrawSharedFrameAttachment(
        SKCanvas canvas,
        SharedFramePointAttachmentRead attachment,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas,
        HashSet<LocationKey> pointRoles)
    {
        var point = projection.ProjectFrame(attachment.FramePoint);
        DrawFramePointBox(canvas, point, paints, 1.08f);
        var bounds = CenteredRect(point, paints.BasisSize * 1.6f);
        AddHit(hitAreas, bounds, "frame basis", priority: 6, attachment.Source);
        pointRoles.Add(LocationKey.Of(attachment.Point.Location));
    }

    private static void DrawClosedSegmentBoundary(
        SKCanvas canvas,
        ClosedSegmentBoundaryRead boundary,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas)
    {
        var path = BuildClosedSegmentBoundaryPath(boundary, projection);
        if (path is null)
        {
            return;
        }

        using (path)
        {
            if (!paints.HideAreaBoundary)
            {
                canvas.DrawPath(path, paints.AreaBoundary);
            }
            AddHit(hitAreas, path.Bounds, "closed boundary", priority: 1, boundary.Source);
        }
    }

    private static void DrawArea(
        SKCanvas canvas,
        AreaRead area,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas,
        HashSet<LocationKey> pointRoles)
    {
        var path = BuildClosedSegmentBoundaryPath(area.Boundary, projection);
        if (path is null)
        {
            return;
        }

        using (path)
        {
            canvas.DrawPath(path, area.IsAntiArea ? paints.AntiAreaFill : paints.AreaFill);
            if (!paints.HideAreaBoundary)
            {
                canvas.DrawPath(path, paints.AreaBoundary);
            }
            AddHit(hitAreas, path.Bounds, area.IsAntiArea ? "anti-area" : "area", priority: 2, area.Source);
        }

        foreach (var point in area.Participation.Points)
        {
            pointRoles.Add(LocationKey.Of(point.Location));
        }
    }

    private static void DrawClosedSharedFrameBoundary(
        SKCanvas canvas,
        ClosedSharedFrameBoundaryRead boundary,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas)
    {
        var path = BuildClosedSharedFrameBoundaryPath(boundary, projection);
        if (path is null)
        {
            return;
        }

        using (path)
        {
            if (!paints.HideAreaBoundary)
            {
                canvas.DrawPath(path, paints.AreaBoundary);
            }
            AddHit(hitAreas, path.Bounds, "closed frame boundary", priority: 1, boundary.Source);
        }
    }

    private static void DrawSharedFrameArea(
        SKCanvas canvas,
        SharedFrameAreaRead area,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas,
        HashSet<LocationKey> pointRoles,
        int paletteIndex)
    {
        var path = BuildClosedSharedFrameBoundaryPath(area.Boundary, projection);
        if (path is null)
        {
            return;
        }

        using (path)
        {
            using var paletteFill = CreateAreaPalettePaint(paints, paletteIndex);
            canvas.DrawPath(path, area.IsAntiArea ? paints.AntiAreaFill : paletteFill ?? paints.AreaFill);
            if (!paints.HideAreaBoundary)
            {
                canvas.DrawPath(path, paints.AreaBoundary);
            }
            AddHit(hitAreas, path.Bounds, area.IsAntiArea ? "frame anti-area" : "frame area", priority: 2, area.Source);
        }

        foreach (var point in area.Participation.Points)
        {
            pointRoles.Add(LocationKey.Of(point.Location));
        }
    }

    private static SKPaint? CreateAreaPalettePaint(GojoCadSkiaPaints paints, int paletteIndex)
    {
        if (paletteIndex < 0 ||
            paints.AreaFillPalette is not { Count: > 0 } palette)
        {
            return null;
        }

        return new SKPaint
        {
            IsAntialias = paints.AreaFill.IsAntialias,
            Color = palette[paletteIndex % palette.Count],
            Style = SKPaintStyle.Fill
        };
    }

    private static void DrawBranchMergeNode(
        SKCanvas canvas,
        PathJunctionRead junction,
        IGojoCadSkiaProjection projection,
        SKPaint fill,
        string text,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas,
        HashSet<LocationKey> pointRoles,
        IElement source)
    {
        var point = AverageProjectedPathSite(junction.Endpoints, projection);
        canvas.DrawCircle(point, paints.TopologyNodeRadius, fill);
        canvas.DrawCircle(point, paints.TopologyNodeRadius, paints.TopologyNodeBorder);
        AddHit(hitAreas, CenteredRect(point, paints.TopologyNodeRadius * 2.2f), text, priority: 7, source);

        foreach (var endpoint in junction.Endpoints)
        {
            pointRoles.Add(LocationKey.Of(endpoint.Point.Location));
        }
    }

    private static int DrawPathGraphNodes(
        SKCanvas canvas,
        PathGraphRead pathGraph,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas,
        HashSet<LocationKey> pointRoles,
        IReadOnlySet<ulong> repeatControlSources)
    {
        var drawn = 0;
        foreach (var node in pathGraph.Nodes)
        {
            var endpoints = node.Endpoints
                .Where(endpoint => !IsRepeatControlSource(endpoint.Segment.Source, repeatControlSources))
                .ToArray();
            if (endpoints.Length == 0 ||
                endpoints.Length == 2 ||
                !TryAverageProjectedPathGraphNode(endpoints, projection, out var point))
            {
                continue;
            }

            var paint = endpoints.Length >= 3
                ? paints.PathGraphBranchNode
                : paints.PathGraphTerminalNode;
            var text = endpoints.Length >= 3
                ? $"path branch valence {endpoints.Length}"
                : "path terminal valence 1";
            var source = endpoints[0].Segment.Source;
            canvas.DrawCircle(point, paints.PathGraphNodeRadius, paint);
            canvas.DrawCircle(point, paints.PathGraphNodeRadius, paints.TopologyNodeBorder);
            AddHit(hitAreas, CenteredRect(point, paints.PathGraphNodeRadius * 2.4f), text, priority: 6, source);
            foreach (var endpoint in endpoints)
            {
                if (endpoint.Point is not null)
                {
                    pointRoles.Add(LocationKey.Of(endpoint.Point));
                }
            }

            drawn++;
        }

        return drawn;
    }

    private static bool TryAverageProjectedPathGraphNode(
        IReadOnlyList<PathGraphEndpointRead> endpoints,
        IGojoCadSkiaProjection projection,
        out SKPoint point)
    {
        var points = endpoints
            .Select(endpoint =>
            {
                if (endpoint.Point is not null)
                {
                    return (SKPoint?)projection.Project(endpoint.Point.Location);
                }

                return endpoint.SharedFramePoint is not null
                    ? projection.ProjectFrame(endpoint.SharedFramePoint)
                    : (SKPoint?)null;
            })
            .Where(candidate => candidate.HasValue)
            .Select(candidate => candidate!.Value)
            .ToArray();
        if (points.Length == 0)
        {
            point = default;
            return false;
        }

        point = new SKPoint(points.Average(candidate => candidate.X), points.Average(candidate => candidate.Y));
        return true;
    }

    private static SKPath? BuildClosedSegmentBoundaryPath(
        ClosedSegmentBoundaryRead boundary,
        IGojoCadSkiaProjection projection)
    {
        var points = boundary.Sites
            .Select(site => AverageProjectedSite(site, projection))
            .Distinct()
            .ToArray();
        if (points.Length < 3)
        {
            return null;
        }

        var center = new SKPoint(points.Average(point => point.X), points.Average(point => point.Y));
        var ordered = points
            .OrderBy(point => MathF.Atan2(point.Y - center.Y, point.X - center.X))
            .ToArray();
        var path = new SKPath();
        path.MoveTo(ordered[0]);
        foreach (var point in ordered.Skip(1))
        {
            path.LineTo(point);
        }

        path.Close();
        return path;
    }

    private static SKPath? BuildClosedSharedFrameBoundaryPath(
        ClosedSharedFrameBoundaryRead boundary,
        IGojoCadSkiaProjection projection)
    {
        if (boundary.Segments.Count < 3 ||
            boundary.Sites.Count < 3)
        {
            return null;
        }

        var pointByEndpoint = new Dictionary<SharedFrameSegmentEndpointRead, SKPoint>();
        var siteByEndpoint = new Dictionary<SharedFrameSegmentEndpointRead, IReadOnlyList<SharedFrameSegmentEndpointRead>>();
        var arcBySpanSource = (boundary.Arcs ?? [])
            .GroupBy(arc => ReadSourceId(arc.Span.Source))
            .ToDictionary(group => group.Key, group => group.First());
        foreach (var site in boundary.Sites)
        {
            var point = AverageProjectedSharedFrameSite(site, projection);
            foreach (var endpoint in site)
            {
                pointByEndpoint[endpoint] = point;
                siteByEndpoint[endpoint] = site;
            }
        }

        var start = new SharedFrameSegmentEndpointRead(boundary.Segments[0], boundary.Segments[0].Start, IsStart: true);
        if (!pointByEndpoint.TryGetValue(start, out var startPoint))
        {
            return null;
        }

        var path = new SKPath();
        path.MoveTo(startPoint);
        var visited = new HashSet<ulong>();
        var incoming = start;
        while (visited.Add(ReadSourceId(incoming.Segment.Source)))
        {
            var outgoing = OtherSharedFrameEndpoint(incoming);
            if (!pointByEndpoint.TryGetValue(outgoing, out var nextPoint))
            {
                path.Dispose();
                return null;
            }

            if (arcBySpanSource.TryGetValue(ReadSourceId(incoming.Segment.Source), out var arc))
            {
                AddSharedFrameArcEdge(path, arc, incoming.Point, outgoing.Point, projection);
            }
            else
            {
                path.LineTo(nextPoint);
            }
            if (!siteByEndpoint.TryGetValue(outgoing, out var site))
            {
                break;
            }

            var next = site.FirstOrDefault(endpoint =>
                ReadSourceId(endpoint.Segment.Source) != ReadSourceId(incoming.Segment.Source) &&
                !visited.Contains(ReadSourceId(endpoint.Segment.Source)));
            if (next is null)
            {
                break;
            }

            incoming = next;
        }

        path.Close();
        return path;
    }

    private static void AddSharedFrameArcEdge(
        SKPath path,
        SharedFrameArcRead arc,
        SharedFramePointRead startPointRead,
        SharedFramePointRead endPointRead,
        IGojoCadSkiaProjection projection)
    {
        var center = projection.ProjectFrame(arc.Center);
        var start = projection.ProjectFrame(startPointRead);
        var end = projection.ProjectFrame(endPointRead);
        var radius = MathF.Max(0.1f, (Distance(center, start) + Distance(center, end)) * 0.5f);
        var bounds = CircleBounds(center, radius);
        var startAngle = AngleDegrees(center, start);
        var endAngle = AngleDegrees(center, end);
        var sweep = ShortestSweep(startAngle, endAngle);
        path.ArcTo(bounds, startAngle, sweep, forceMoveTo: false);
    }

    private static SharedFrameSegmentEndpointRead OtherSharedFrameEndpoint(SharedFrameSegmentEndpointRead endpoint) =>
        endpoint.IsStart
            ? new SharedFrameSegmentEndpointRead(endpoint.Segment, endpoint.Segment.End, IsStart: false)
            : new SharedFrameSegmentEndpointRead(endpoint.Segment, endpoint.Segment.Start, IsStart: true);

    private static ulong ReadSourceId(IElement source) =>
        source is Relation relation ? relation.Id : 0UL;

    private static SKPoint AverageProjectedSite(
        IReadOnlyList<SegmentEndpointRead> site,
        IGojoCadSkiaProjection projection)
    {
        var points = site
            .Select(endpoint => projection.Project(endpoint.Point.Location))
            .ToArray();
        return new SKPoint(points.Average(point => point.X), points.Average(point => point.Y));
    }

    private static SKPoint AverageProjectedPathSite(
        IReadOnlyList<PathSegmentEndpointRead> site,
        IGojoCadSkiaProjection projection)
    {
        var points = site
            .Select(endpoint => projection.Project(endpoint.Point.Location))
            .ToArray();
        return new SKPoint(points.Average(point => point.X), points.Average(point => point.Y));
    }

    private static SKPoint AverageProjectedSharedFrameSite(
        IReadOnlyList<SharedFrameSegmentEndpointRead> site,
        IGojoCadSkiaProjection projection)
    {
        var points = site
            .Select(endpoint => projection.ProjectFrame(endpoint.Point))
            .ToArray();
        return new SKPoint(points.Average(point => point.X), points.Average(point => point.Y));
    }

    private static void DrawSegmentTick(
        SKCanvas canvas,
        CarrierLocation location,
        SKPoint startPoint,
        SKPoint endPoint,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas,
        string text,
        int priority,
        IElement? source)
    {
        var point = projection.Project(location);
        var perpendicular = PerpendicularUnit(startPoint, endPoint);
        DrawTick(canvas, point, perpendicular, paints);
        var bounds = CenteredRect(point, paints.PartitionTickLength);
        AddHit(hitAreas, bounds, text, priority, source);
    }

    private static void DrawCircumferenceTick(
        SKCanvas canvas,
        SKPoint center,
        CarrierLocation location,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas,
        string text,
        int priority,
        IElement? source)
    {
        var point = projection.Project(location);
        DrawTick(canvas, point, Unit(center, point), paints);
        var bounds = CenteredRect(point, paints.PartitionTickLength);
        AddHit(hitAreas, bounds, text, priority, source);
    }

    private static void DrawCircumferenceTickLadder(
        SKCanvas canvas,
        SKPoint center,
        Carrier carrier,
        IEnumerable<CarrierLocation> observedLocations,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints)
    {
        var period = ReadObservedCircumferencePeriod(observedLocations);
        if (period <= 0)
        {
            return;
        }

        var tickStep = Math.Max(1, (int)Math.Ceiling(period / 16d));
        for (var tick = 0; tick < period; tick += tickStep)
        {
            DrawCircumferenceTickMark(
                canvas,
                center,
                new CarrierLocation(carrier, tick),
                projection,
                paints);
        }
    }

    private static long ReadObservedCircumferencePeriod(IEnumerable<CarrierLocation> observedLocations)
    {
        var deltas = observedLocations
            .Select(location => Math.Abs(location.Ticks))
            .Where(delta => delta > 0)
            .Distinct()
            .Order()
            .ToArray();
        if (deltas.Length < 2)
        {
            return 0;
        }

        var candidate = deltas[^1];
        var previous = deltas[^2];
        return candidate >= previous * 2
            ? candidate
            : 0;
    }

    private static void DrawCircumferenceTickMark(
        SKCanvas canvas,
        SKPoint center,
        CarrierLocation location,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints)
    {
        var point = projection.Project(location);
        DrawTick(canvas, point, Unit(center, point), paints);
    }

    private static void DrawBasisRect(
        SKCanvas canvas,
        CarrierLocation location,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas,
        string text,
        int priority,
        IElement? source)
    {
        var point = projection.Project(location);
        var bounds = CenteredRect(point, paints.BasisSize);
        canvas.DrawRect(bounds, paints.Basis);
        AddHit(hitAreas, bounds, text, priority, source);
    }

    private static void DrawFramePointBox(
        SKCanvas canvas,
        SKPoint point,
        GojoCadSkiaPaints paints,
        float scale)
    {
        var size = paints.BasisSize * scale;
        var bounds = CenteredRect(point, size);
        canvas.DrawRect(bounds, paints.SharedFramePoint);
        canvas.DrawRect(bounds, paints.Basis);
    }

    private static CircleProjection ReadCircleProjection(
        CarrierLocation marker,
        IGojoCadSkiaProjection projection)
    {
        var origin = ReadCircularOrigin(marker.Carrier);
        var center = projection.Project(origin);
        var markerPoint = projection.Project(marker);
        var radius = MathF.Max(0.1f, Distance(center, markerPoint));
        return new CircleProjection(center, radius);
    }

    private static void DrawEndpointBar(
        SKCanvas canvas,
        SKPoint point,
        SKPoint toward,
        GojoCadSkiaPaints paints)
    {
        var perpendicular = PerpendicularUnit(point, toward);
        var half = paints.EndpointBarLength * 0.5f;
        canvas.DrawLine(
            new SKPoint(point.X - perpendicular.X * half, point.Y - perpendicular.Y * half),
            new SKPoint(point.X + perpendicular.X * half, point.Y + perpendicular.Y * half),
            paints.EndpointBar);
    }

    private static void DrawTick(
        SKCanvas canvas,
        SKPoint center,
        SKPoint direction,
        GojoCadSkiaPaints paints)
    {
        var half = paints.PartitionTickLength * 0.5f;
        canvas.DrawLine(
            new SKPoint(center.X - direction.X * half, center.Y - direction.Y * half),
            new SKPoint(center.X + direction.X * half, center.Y + direction.Y * half),
            paints.Partition);
    }

    private static void DrawArrowTip(
        SKCanvas canvas,
        SKPoint start,
        SKPoint end,
        GojoCadSkiaPaints paints,
        SKPaint paint)
    {
        var direction = Unit(start, end);
        var perpendicular = new SKPoint(-direction.Y, direction.X);
        var back = new SKPoint(
            end.X - direction.X * paints.ArrowLength,
            end.Y - direction.Y * paints.ArrowLength);
        var left = new SKPoint(
            back.X + perpendicular.X * paints.ArrowWidth,
            back.Y + perpendicular.Y * paints.ArrowWidth);
        var right = new SKPoint(
            back.X - perpendicular.X * paints.ArrowWidth,
            back.Y - perpendicular.Y * paints.ArrowWidth);

        canvas.DrawLine(end, left, paint);
        canvas.DrawLine(end, right, paint);
    }

    private static void AddHit(
        ICollection<GojoCadSkiaHitArea>? hitAreas,
        SKRect bounds,
        string text,
        int priority,
        IElement? source = null)
    {
        hitAreas?.Add(new GojoCadSkiaHitArea(bounds, text, priority, source));
    }

    private static SKRect CenteredRect(SKPoint center, float size)
    {
        var half = size * 0.5f;
        return new SKRect(center.X - half, center.Y - half, center.X + half, center.Y + half);
    }

    private static SKRect CircleBounds(SKPoint center, float radius) =>
        new(center.X - radius, center.Y - radius, center.X + radius, center.Y + radius);

    private static SKRect LineBounds(SKPoint start, SKPoint end, float padding) => new(
        MathF.Min(start.X, end.X) - padding,
        MathF.Min(start.Y, end.Y) - padding,
        MathF.Max(start.X, end.X) + padding,
        MathF.Max(start.Y, end.Y) + padding);

    private static float AngleDegrees(SKPoint center, SKPoint point) =>
        MathF.Atan2(point.Y - center.Y, point.X - center.X) * 180f / MathF.PI;

    private static float ShortestSweep(float start, float end)
    {
        var sweep = end - start;
        while (sweep >= 180f)
        {
            sweep -= 360f;
        }

        while (sweep < -180f)
        {
            sweep += 360f;
        }

        return sweep;
    }

    private static SKPoint PointOnCircle(SKPoint center, float radius, float angleDegrees)
    {
        var radians = angleDegrees * MathF.PI / 180f;
        return new SKPoint(
            center.X + MathF.Cos(radians) * radius,
            center.Y + MathF.Sin(radians) * radius);
    }

    private static SKPoint PerpendicularUnit(SKPoint start, SKPoint end)
    {
        var unit = Unit(start, end);
        return new SKPoint(-unit.Y, unit.X);
    }

    private static SKPoint Unit(SKPoint start, SKPoint end)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var length = MathF.Sqrt(dx * dx + dy * dy);
        return length <= float.Epsilon
            ? new SKPoint(1, 0)
            : new SKPoint(dx / length, dy / length);
    }

    private static SKPoint Add(SKPoint point, SKPoint vector) =>
        new(point.X + vector.X, point.Y + vector.Y);

    private static SKPoint Scale(SKPoint vector, float scale) =>
        new(vector.X * scale, vector.Y * scale);

    private static float Distance(SKPoint left, SKPoint right)
    {
        var dx = right.X - left.X;
        var dy = right.Y - left.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private static bool IsCircularCarrier(Carrier carrier) => carrier.TryReadCircularOrigin(out _);

    private static bool SamePathLocation(CarrierLocation left, CarrierLocation right) =>
        left.SameTickRefAs(right) ||
        left.Carrier.SameIdentityAs(right.Carrier) &&
        left.Ticks == right.Ticks;

    private static CarrierLocation ReadCircularOrigin(Carrier carrier) =>
        carrier.TryReadCircularOrigin(out var origin)
            ? origin
            : new CarrierLocation(Carrier.Unknown, 0);

    private readonly record struct LocatedSource(CarrierLocation Location, IElement Source);

    private readonly record struct CircleProjection(SKPoint Center, float Radius);

    private readonly record struct LocationKey(ulong CarrierId, long Ticks)
    {
        public static LocationKey Of(CarrierLocation location) =>
            new(location.Carrier.Id, location.Ticks);

        public static LocationKey Of(PointRead point) => Of(point.Location);
    }
}
