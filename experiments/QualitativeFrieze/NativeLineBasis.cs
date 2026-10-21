using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using D = Gojo.Experiments.QualitativeFrieze.OrnamentDefaultsPolicy;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A selected, unmeasured geometric model for an additive motion
/// frame. A closed radial contour with four equal turns is related to the
/// actual four directed frame traversals through a commuting cyclic action.
/// The resulting right-angle read is conditional on that construction; it is
/// not discovered from axis names, independent slots, or rendered coordinates.</summary>
public sealed class NativeLineBasis
{
    // surface = actual motion frame ->
    //           (marked contour -> (marker-to-direction map -> direction successors))
    // receipt = (surface -> question) -> (contour read -> cyclic-action read)
    public QualitativeMotionFrame Frame { get; }
    public QualitativeMarkedContour Contour { get; }
    public Relation DirectionMap { get; }
    public Relation DirectionSuccessors { get; }
    public Relation DirectionDomain => S(Directions(Frame));
    public Relation Surface { get; }

    /// <summary>The option authors an explicit equal-turn promise. Subsequent
    /// reads inspect that promise rather than retaining this option as state.</summary>
    public NativeLineBasis(QualitativeMotionFrame frame, bool equalTurns = false)
    {
        Frame = frame;
        var markers = Enumerable.Range(0, 4).Select(_ => (IElement)new Placeholder()).ToArray();
        var traversal = Cycle(markers);
        var contour = new QualitativeMarkedContour(new Placeholder(), S(new Placeholder()), S(markers), traversal, markers[0]);
        Contour = equalTurns ? contour.WithEqualTurns(new Placeholder()) : contour;
        var directions = Directions(frame);
        DirectionMap = S(markers.Zip(directions).Select(pair => (IElement)new Relation(pair.First, pair.Second)).ToArray());
        DirectionSuccessors = Cycle(directions);
        Surface = Compose(frame, Contour, DirectionMap, DirectionSuccessors);
    }

    /// <summary>Connect existing native participation, including incomplete or
    /// contradictory participation that a later read must expose.</summary>
    public NativeLineBasis(QualitativeMotionFrame frame, QualitativeMarkedContour contour,
        Relation directionMap, Relation directionSuccessors)
    {
        Frame = frame; Contour = contour; DirectionMap = directionMap; DirectionSuccessors = directionSuccessors;
        Surface = Compose(frame, contour, directionMap, directionSuccessors);
    }

    public D.Evidence ReadRightAngle(Relation predicate)
    {
        var contourRead = Contour.Evaluate();
        var action = Contour.CompareAction(DirectionDomain, DirectionSuccessors, DirectionMap);
        var witness = new Relation(new Relation(Surface, predicate), new Relation(contourRead, action));
        if (!Contour.TryReadResult(contourRead, out _, out var violations, out var pending) ||
            !Contour.TryReadActionResult(action, out var matched, out var actionViolations, out var actionPending))
            return D.Evidence.FromAnswer(predicate, "Blocked", witness);
        if (E(violations).Length != 0 || E(actionViolations).Length != 0)
            return D.Evidence.FromAnswer(predicate, "Blocked", witness);
        if (!Closed(Contour.Markers, out var markers) || !Closed(DirectionMap, out _) ||
            !Closed(DirectionSuccessors, out var successors) || E(actionPending).Length != 0)
            return D.Evidence.FromAnswer(predicate, "Open", witness);
        if (markers.Length != 4 || !CanonicalDirectionCycle(successors))
            return D.Evidence.FromAnswer(predicate, "Blocked", witness);
        if (E(pending).Length != 0 || !Contour.TryReadClosedContour(contourRead, out _) ||
            !Contour.TryReadEqualTurns(contourRead, out _) || E(matched).Length != 4)
            return D.Evidence.FromAnswer(predicate, "Open", witness);
        return D.Evidence.FromAnswer(predicate, "Yes", witness);
    }

    /// <summary>Import the exact native model without its authoring wrapper.
    /// A fresh standard-frame interpretation is accompanied by its explicit
    /// alias correspondence; no source factory, screen basis or option survives.
    /// Semantic violations in an otherwise readable model remain available to
    /// ReadRightAngle as Blocked or Open evidence.</summary>
    public static bool TryRead(Relation surface, out NativeLineBasis basis, out Relation witness)
    {
        basis = null!; witness = new(surface, StructuralConstraint.Open());
        if (surface.Left is not Relation frameSurface || surface.Right is not Relation ||
            !QualitativeMotionFrameRead.TryReadStandard(frameSurface, out var frame) ||
            !frame.TryRebind(surface.Right, out var renamed, out var frameWitness) ||
            renamed is not Relation { Left: Relation contourSurface,
                Right: Relation { Left: Relation map, Right: Relation successors } } ||
            !TryReadContour(contourSurface, out var contour)) return false;
        basis = new(frame.Target, contour, map, successors);
        witness = new(surface, new Relation(frameWitness, basis.Surface));
        return true;
    }

    /// <summary>Read a model already imported with a larger native world. Its
    /// exact frame alias must be shared with that world's passages; importing
    /// a second independent frame here would break their correspondence.</summary>
    public static bool TryReadInFrame(QualitativeMotionFrame frame, Relation surface,
        out NativeLineBasis basis, out Relation witness)
    {
        basis = null!; witness = new(surface, StructuralConstraint.Open());
        if (!ReferenceEquals(surface.Left, frame.Surface) ||
            surface.Right is not Relation { Left: Relation contourSurface,
                Right: Relation { Left: Relation map, Right: Relation successors } } ||
            !TryReadContour(contourSurface, out var contour)) return false;
        basis = new(frame, contour, map, successors);
        witness = new(surface, basis.Surface);
        return true;
    }

    private bool CanonicalDirectionCycle(IElement[] rows)
    {
        var directions = Directions(Frame);
        return rows.Length == directions.Length && rows.Select((row, index) => row is Relation edge &&
            ReferenceEquals(edge.Left, directions[index]) && ReferenceEquals(edge.Right, directions[(index + 1) % directions.Length])).All(valid => valid);
    }

    private static bool TryReadContour(Relation surface, out QualitativeMarkedContour contour)
    {
        contour = null!;
        if (surface.Left is not Relation { Left: Relation { Left: var center, Right: Relation reach }, Right: Relation radii } ||
            surface.Right is not Relation { Left: Relation { Left: Relation markers, Right: Relation whole },
                Right: Relation { Left: Relation traversal, Right: Relation equalTurn } } ||
            !ReferenceEquals(whole.Right, traversal) || !ReferenceEquals(equalTurn.Left, whole) ||
            equalTurn.Right is not Relation promise || !Scope(markers, out var markerRows, out var markerClosed) ||
            !Scope(radii, out var radiusRows, out var radiiClosed) || markerClosed != radiiClosed ||
            markerRows.Length != radiusRows.Length) return false;
        // These are the actual radial attachments and actual whole, not a
        // search through nested relations for fields that happen to look right.
        for (var i = 0; i < markerRows.Length; i++)
            if (radiusRows[i] is not Relation { Left: Relation ends, Right: var radialReach } ||
                !ReferenceEquals(ends.Left, center) || !ReferenceEquals(ends.Right, markerRows[i]) ||
                !ReferenceEquals(radialReach, reach)) return false;
        contour = new(center, reach, markers, traversal, whole.Left, promise);
        return true;
    }

    private static Relation Compose(QualitativeMotionFrame frame, QualitativeMarkedContour contour, Relation map, Relation successors) =>
        new(frame.Surface, new Relation(contour.Surface, new Relation(map, successors)));
    private static IElement[] Directions(QualitativeMotionFrame frame) => [frame.Right, frame.Up, frame.Left, frame.Down];
    private static Relation Cycle(IElement[] entries) => S(entries.Select((entry, index) =>
        (IElement)new Relation(entry, entries[(index + 1) % entries.Length])).ToArray());
    private static bool Scope(Relation scope, out IElement[] entries, out bool closed)
    {
        closed = false;
        if (!StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail)) return false;
        closed = seam.Equals(tail); return true;
    }
    private static bool Closed(Relation scope, out IElement[] entries) => Scope(scope, out entries, out var closed) && closed;
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
