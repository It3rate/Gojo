using Gojo.Core;

namespace GojoCAD.GCode;

/// <summary>
/// G-code construction and local-read helpers. The helpers build ordinary
/// carrier/relation structure; G-code meaning remains in that structure rather
/// than in these code-side readers.
/// </summary>
public static class GCodeStructure
{
    public const long RapidMove = 0;
    public const long LinearMove = 1;
    public const long ClockwiseArc = 2;
    public const long CounterClockwiseArc = 3;
    public const long InchUnits = 20;
    public const long MillimeterUnits = 21;
    public const long AbsoluteDistance = 90;
    public const long IncrementalDistance = 91;
    public const long ToolChange = 6;

    public static Relation Program(Carrier root, Relation basis, params IElement[] elements) =>
        Relation.OrderedGroup(RootOrNew(root), [basis, .. elements]);

    public static Relation Basis(
        Carrier root,
        Carrier frame,
        Carrier xAxis,
        Carrier yAxis,
        Carrier zAxis,
        Carrier feedAxis,
        Carrier toolAxis,
        Carrier gWordAxis,
        Carrier mWordAxis,
        Carrier arcPhaseAxis,
        long coordinateTicksPerUnit,
        long arcTicksPerTurn)
    {
        return Relation.OrderedGroup(
            RootOrNew(root),
            frame,
            xAxis,
            yAxis,
            zAxis,
            feedAxis,
            toolAxis,
            gWordAxis,
            mWordAxis,
            arcPhaseAxis,
            new CarrierLocation(xAxis, Math.Max(1, coordinateTicksPerUnit)),
            new CarrierLocation(yAxis, Math.Max(1, coordinateTicksPerUnit)),
            new CarrierLocation(arcPhaseAxis, Math.Max(4, arcTicksPerTurn)));
    }

    public static Relation FramePoint(Carrier frame, Carrier xAxis, Carrier yAxis, long xTicks, long yTicks) =>
        GraphicStructure.FramePoint(
            RootOrNew(frame),
            new CarrierLocation(RootOrNew(xAxis), xTicks),
            new CarrierLocation(RootOrNew(yAxis), yTicks));

    public static Relation TravelMotion(
        Carrier root,
        CarrierLocation command,
        CarrierLocation pen,
        CarrierLocation tool,
        Relation start,
        Relation end,
        Relation xFlow,
        Relation yFlow,
        CarrierLocation feed) =>
        Relation.OrderedGroup(RootOrNew(root), command, pen, tool, feed, start, end, xFlow, yFlow);

    public static Relation LineMotion(
        Carrier root,
        CarrierLocation command,
        CarrierLocation pen,
        CarrierLocation tool,
        CarrierLocation feed,
        Relation start,
        Relation end,
        Relation xFlow,
        Relation yFlow) =>
        Relation.OrderedGroup(
            RootOrNew(root),
            command,
            pen,
            tool,
            feed,
            GraphicStructure.FrameSegment(start, end),
            xFlow,
            yFlow);

    public static Relation ArcMotion(
        Carrier root,
        CarrierLocation command,
        CarrierLocation pen,
        CarrierLocation tool,
        CarrierLocation feed,
        IElement renderedCircumference,
        Relation center,
        Relation start,
        Relation end,
        Relation startRadius,
        Relation endRadius,
        Relation circumferenceFlow,
        Relation xFlow,
        Relation yFlow) =>
        Relation.OrderedGroup(
            RootOrNew(root),
            command,
            pen,
            tool,
            feed,
            renderedCircumference,
            center,
            start,
            end,
            startRadius,
            endRadius,
            circumferenceFlow,
            xFlow,
            yFlow);

    public static Relation PenChange(
        Carrier root,
        CarrierLocation command,
        CarrierLocation before,
        CarrierLocation after) =>
        Relation.OrderedGroup(RootOrNew(root), command, new Relation(before, after));

    public static Relation StrokeChange(
        Carrier root,
        CarrierLocation command,
        CarrierLocation before,
        CarrierLocation after) =>
        Relation.OrderedGroup(RootOrNew(root), command, new Relation(before, after));

    public static Relation ModeChange(Carrier root, CarrierLocation command) =>
        Relation.OrderedGroup(RootOrNew(root), command);

    public static Relation AxisFlow(Carrier axis, long deltaTicks)
    {
        var forward = RootOrNew(axis);
        var face = deltaTicks < 0 ? forward.Inverse() : forward;
        return new Relation(
            new CarrierLocation(face, 0),
            new CarrierLocation(face, Math.Abs(deltaTicks)));
    }

    public static CarrierLocation PenState(Carrier zAxis, bool isDown) =>
        new(isDown ? RootOrNew(zAxis) : RootOrNew(zAxis).Inverse(), 1);

    public static CarrierLocation ToolState(Carrier toolAxis, long toolNumber) =>
        new(RootOrNew(toolAxis), Math.Max(0, toolNumber));

    public static CarrierLocation FeedState(Carrier feedAxis, long feedTicks) =>
        new(RootOrNew(feedAxis), Math.Max(0, feedTicks));

    public static CarrierLocation GWord(Carrier gWordAxis, long code) =>
        new(RootOrNew(gWordAxis), Math.Max(0, code));

    public static CarrierLocation MWord(Carrier mWordAxis, long code) =>
        new(RootOrNew(mWordAxis), Math.Max(0, code));

    public static Relation RadialElement(
        Carrier root,
        Relation center,
        Relation endpoint,
        Carrier radiusAxis,
        long radiusTicks) =>
        Relation.OrderedGroup(
            RootOrNew(root),
            center,
            endpoint,
            new Relation(
                new CarrierLocation(RootOrNew(radiusAxis), 0),
                new CarrierLocation(RootOrNew(radiusAxis), Math.Max(0, radiusTicks))));

    public static Relation CircumferenceFlow(
        Carrier circumference,
        long spanTicks,
        bool isClockwise)
    {
        var forward = RootOrNew(circumference);
        var face = isClockwise ? forward.Inverse() : forward;
        return new Relation(
            new CarrierLocation(face, 0),
            new CarrierLocation(face, Math.Abs(spanTicks)));
    }

    public static bool TryReadProgram(
        Relation program,
        out Relation basis,
        out IElement[] elements)
    {
        if (TryReadOrderedElements(program, out var ordered) &&
            ordered.Length >= 1 &&
            ordered[0] is Relation basisRelation)
        {
            basis = basisRelation;
            elements = [.. ordered.Skip(1)];
            return true;
        }

        basis = null!;
        elements = [];
        return false;
    }

    public static bool TryReadBasis(Relation basis, out GCodeBasisRead read)
    {
        if (TryReadOrderedElements(basis, out var elements) &&
            elements.Length == 12 &&
            elements[0] is Carrier frame &&
            elements[1] is Carrier xAxis &&
            elements[2] is Carrier yAxis &&
            elements[3] is Carrier zAxis &&
            elements[4] is Carrier feedAxis &&
            elements[5] is Carrier toolAxis &&
            elements[6] is Carrier gWordAxis &&
            elements[7] is Carrier mWordAxis &&
            elements[8] is Carrier arcPhaseAxis &&
            elements[9] is CarrierLocation xResolution &&
            elements[10] is CarrierLocation yResolution &&
            elements[11] is CarrierLocation arcResolution &&
            xResolution.Carrier.SameIdentityAs(xAxis) &&
            yResolution.Carrier.SameIdentityAs(yAxis) &&
            arcResolution.Carrier.SameIdentityAs(arcPhaseAxis))
        {
            read = new GCodeBasisRead(
                frame,
                xAxis,
                yAxis,
                zAxis,
                feedAxis,
                toolAxis,
                gWordAxis,
                mWordAxis,
                arcPhaseAxis,
                xResolution.Ticks,
                yResolution.Ticks,
                arcResolution.Ticks);
            return true;
        }

        read = default!;
        return false;
    }

    public static bool TryReadLineMotion(
        Relation motion,
        out CarrierLocation command,
        out CarrierLocation pen,
        out CarrierLocation tool,
        out CarrierLocation feed,
        out Relation segment,
        out Relation xFlow,
        out Relation yFlow)
    {
        if (TryReadOrderedElements(motion, out var elements) &&
            elements.Length == 7 &&
            elements[0] is CarrierLocation commandLocation &&
            elements[1] is CarrierLocation penLocation &&
            elements[2] is CarrierLocation toolLocation &&
            elements[3] is CarrierLocation feedLocation &&
            elements[4] is Relation segmentRelation &&
            elements[5] is Relation xFlowRelation &&
            elements[6] is Relation yFlowRelation &&
            commandLocation.Ticks == LinearMove)
        {
            command = commandLocation;
            pen = penLocation;
            tool = toolLocation;
            feed = feedLocation;
            segment = segmentRelation;
            xFlow = xFlowRelation;
            yFlow = yFlowRelation;
            return true;
        }

        command = default;
        pen = default;
        tool = default;
        feed = default;
        segment = null!;
        xFlow = null!;
        yFlow = null!;
        return false;
    }

    public static bool TryReadTravelMotion(
        Relation motion,
        out CarrierLocation command,
        out CarrierLocation pen,
        out CarrierLocation tool,
        out CarrierLocation feed,
        out Relation start,
        out Relation end,
        out Relation xFlow,
        out Relation yFlow)
    {
        if (TryReadOrderedElements(motion, out var elements) &&
            elements.Length == 8 &&
            elements[0] is CarrierLocation commandLocation &&
            elements[1] is CarrierLocation penLocation &&
            elements[2] is CarrierLocation toolLocation &&
            elements[3] is CarrierLocation feedLocation &&
            elements[4] is Relation startRelation &&
            elements[5] is Relation endRelation &&
            elements[6] is Relation xFlowRelation &&
            elements[7] is Relation yFlowRelation)
        {
            command = commandLocation;
            pen = penLocation;
            tool = toolLocation;
            feed = feedLocation;
            start = startRelation;
            end = endRelation;
            xFlow = xFlowRelation;
            yFlow = yFlowRelation;
            return true;
        }

        command = default;
        pen = default;
        tool = default;
        feed = default;
        start = null!;
        end = null!;
        xFlow = null!;
        yFlow = null!;
        return false;
    }

    public static bool TryReadArcMotion(
        Relation motion,
        out CarrierLocation command,
        out CarrierLocation pen,
        out CarrierLocation tool,
        out CarrierLocation feed,
        out IElement renderedCircumference,
        out Relation center,
        out Relation start,
        out Relation end,
        out Relation startRadius,
        out Relation endRadius,
        out Relation circumferenceFlow,
        out Relation xFlow,
        out Relation yFlow)
    {
        if (TryReadOrderedElements(motion, out var elements) &&
            elements.Length == 13 &&
            elements[0] is CarrierLocation commandLocation &&
            elements[1] is CarrierLocation penLocation &&
            elements[2] is CarrierLocation toolLocation &&
            elements[3] is CarrierLocation feedLocation &&
            elements[5] is Relation centerRelation &&
            elements[6] is Relation startRelation &&
            elements[7] is Relation endRelation &&
            elements[8] is Relation startRadiusRelation &&
            elements[9] is Relation endRadiusRelation &&
            elements[10] is Relation circumferenceFlowRelation &&
            elements[11] is Relation xFlowRelation &&
            elements[12] is Relation yFlowRelation &&
            (commandLocation.Ticks == ClockwiseArc || commandLocation.Ticks == CounterClockwiseArc))
        {
            command = commandLocation;
            pen = penLocation;
            tool = toolLocation;
            feed = feedLocation;
            renderedCircumference = elements[4];
            center = centerRelation;
            start = startRelation;
            end = endRelation;
            startRadius = startRadiusRelation;
            endRadius = endRadiusRelation;
            circumferenceFlow = circumferenceFlowRelation;
            xFlow = xFlowRelation;
            yFlow = yFlowRelation;
            return true;
        }

        command = default;
        pen = default;
        tool = default;
        feed = default;
        renderedCircumference = default!;
        center = null!;
        start = null!;
        end = null!;
        startRadius = null!;
        endRadius = null!;
        circumferenceFlow = null!;
        xFlow = null!;
        yFlow = null!;
        return false;
    }

    public static bool TryReadAxisFlow(Relation flow, out Carrier carrier, out long ticks, out bool isInverse)
    {
        if (flow.Left is CarrierLocation start &&
            flow.Right is CarrierLocation end &&
            start.Carrier.SameIdentityAs(end.Carrier) &&
            start.Ticks == 0)
        {
            carrier = start.Carrier.IsInverse ? start.Carrier.Inverse() : start.Carrier;
            ticks = end.Ticks;
            isInverse = start.Carrier.IsInverse;
            return true;
        }

        carrier = null!;
        ticks = 0;
        isInverse = false;
        return false;
    }

    public static bool TryReadStrokeChange(
        Relation change,
        out CarrierLocation command,
        out CarrierLocation before,
        out CarrierLocation after)
    {
        if (TryReadOrderedElements(change, out var elements) &&
            elements.Length == 2 &&
            elements[0] is CarrierLocation commandLocation &&
            commandLocation.Ticks == ToolChange &&
            elements[1] is Relation transition &&
            transition.Left is CarrierLocation beforeLocation &&
            transition.Right is CarrierLocation afterLocation)
        {
            command = commandLocation;
            before = beforeLocation;
            after = afterLocation;
            return true;
        }

        command = default;
        before = default;
        after = default;
        return false;
    }

    public static bool TryReadPenChange(
        Relation change,
        out CarrierLocation command,
        out CarrierLocation before,
        out CarrierLocation after)
    {
        if (TryReadOrderedElements(change, out var elements) &&
            elements.Length == 2 &&
            elements[0] is CarrierLocation commandLocation &&
            elements[1] is Relation transition &&
            transition.Left is CarrierLocation beforeLocation &&
            transition.Right is CarrierLocation afterLocation &&
            !beforeLocation.Carrier.SameIdentityAs(afterLocation.Carrier))
        {
            command = commandLocation;
            before = beforeLocation;
            after = afterLocation;
            return true;
        }

        command = default;
        before = default;
        after = default;
        return false;
    }

    private static bool TryReadOrderedElements(Relation relation, out IElement[] elements)
    {
        if (relation?.Right is Carrier holder &&
            relation.TryReadOrderedElementsOn(holder, out elements))
        {
            return true;
        }

        elements = [];
        return false;
    }

    private static Carrier RootOrNew(Carrier carrier) =>
        carrier is null || carrier.IsUnknown ? new Carrier() : carrier;
}

public sealed record GCodeBasisRead(
    Carrier Frame,
    Carrier XAxis,
    Carrier YAxis,
    Carrier ZAxis,
    Carrier FeedAxis,
    Carrier ToolAxis,
    Carrier GWordAxis,
    Carrier MWordAxis,
    Carrier ArcPhaseAxis,
    long XTicksPerUnit,
    long YTicksPerUnit,
    long ArcTicksPerTurn);
