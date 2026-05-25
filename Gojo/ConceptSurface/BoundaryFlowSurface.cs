using Gojo.Core;
using Gojo.Structures;

namespace Gojo.ConceptSurface;

/// <summary>
/// Local read value for a boundary/domain surface. The booleans are a compact
/// helper read over relation structure; they are not the source of meaning.
/// </summary>
public readonly record struct BoundaryExposure(bool LeftBoundary, bool RightBoundary)
{
    public static BoundaryExposure FullSigned => new(false, false);

    public static BoundaryExposure Positive => new(true, false);

    public static BoundaryExposure Negative => new(false, true);

    public static BoundaryExposure BoundedWhole => new(true, true);

    public bool IsFullSigned => !LeftBoundary && !RightBoundary;

    public bool IsPositive => LeftBoundary && !RightBoundary;

    public bool IsNegative => !LeftBoundary && RightBoundary;

    public bool IsBoundedWhole => LeftBoundary && RightBoundary;

    public string Label => this switch
    {
        { LeftBoundary: false, RightBoundary: false } => "PN",
        { LeftBoundary: true, RightBoundary: false } => "P",
        { LeftBoundary: false, RightBoundary: true } => "N",
        _ => "S"
    };

    /// <summary>Swaps positive and negative boundary sides while preserving full and bounded-whole domains.</summary>
    public BoundaryExposure Dual() => new(RightBoundary, LeftBoundary);

    public override string ToString() => Label;
}

/// <summary>
/// Local read value for exposed flow faces. "None" means no forward or inverse
/// flow surface is exposed by this local structure.
/// </summary>
public readonly record struct FlowExposure(bool ForwardFlow, bool InverseFlow)
{
    public static FlowExposure None => new(false, false);

    public static FlowExposure Forward => new(true, false);

    public static FlowExposure Inverse => new(false, true);

    public static FlowExposure Both => new(true, true);

    public bool IsNone => !ForwardFlow && !InverseFlow;

    public bool IsForwardOnly => ForwardFlow && !InverseFlow;

    public bool IsInverseOnly => !ForwardFlow && InverseFlow;

    public bool IsBoth => ForwardFlow && InverseFlow;

    public string Label => this switch
    {
        { ForwardFlow: false, InverseFlow: false } => "None",
        { ForwardFlow: true, InverseFlow: false } => "F",
        { ForwardFlow: false, InverseFlow: true } => "I",
        _ => "FI"
    };

    /// <summary>Swaps forward and inverse flow exposure while preserving none and both.</summary>
    public FlowExposure Dual() => new(InverseFlow, ForwardFlow);

    public override string ToString() => Label;
}

/// <summary>
/// Helper read for the sixteen boundary/flow combinations.
/// </summary>
public readonly record struct BoundaryFlowExposure(BoundaryExposure Boundary, FlowExposure Flow)
{
    public string Label => $"{Boundary}:{Flow}";

    public BoundaryFlowExposure DualBoundary() => new(Boundary.Dual(), Flow);

    public BoundaryFlowExposure DualFlow() => new(Boundary, Flow.Dual());

    public BoundaryFlowExposure Dual() => new(Boundary.Dual(), Flow.Dual());

    public override string ToString() => Label;
}

/// <summary>
/// Primitive grade-0 flow face. This is a helper read over a face surface:
/// the physical meaning lives in <see cref="FlowFaceSurface"/>.
/// </summary>
public readonly record struct FlowFace(bool IsInverse)
{
    public static FlowFace Forward => new(false);

    public static FlowFace Inverse => new(true);

    public string Label => IsInverse ? "I" : "F";

    public FlowFace Dual() => new(!IsInverse);

    public FlowLane ToLane() => IsInverse ? FlowLane.Inverse : FlowLane.Forward;

    public override string ToString() => Label;
}

/// <summary>
/// Physical grade-0 flow face evidence. None and FI are not primitive here:
/// they are summary reads over absent or combined face surfaces.
/// </summary>
public static class FlowFaceSurface
{
    public static Relation Create(
        Carrier root,
        FlowFace face,
        IElement selected,
        IElement evidence) =>
        Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            FacePoint(face),
            selected ?? ConceptSurface.UnknownPoint(),
            evidence ?? ConceptSurface.UnknownPoint());

    public static Relation Forward(Carrier root, IElement selected, IElement evidence) =>
        Create(root, FlowFace.Forward, selected, evidence);

    public static Relation Forward(Carrier root, IElement selected) =>
        Forward(root, selected, ConceptSurface.UnknownPoint());

    public static Relation Inverse(Carrier root, IElement selected, IElement evidence) =>
        Create(root, FlowFace.Inverse, selected, evidence);

    public static Relation Inverse(Carrier root, IElement selected) =>
        Inverse(root, selected, ConceptSurface.UnknownPoint());

    public static bool TryRead(
        Relation surface,
        out FlowFace face,
        out IElement selected,
        out IElement evidence)
    {
        if (ConceptSurfaceReader.TryReadRootElements(surface, out var elements) &&
            elements.Length == 3 &&
            TryReadFace(elements[0], out var read))
        {
            face = read;
            selected = elements[1];
            evidence = elements[2];
            return true;
        }

        face = default;
        selected = default!;
        evidence = default!;
        return false;
    }

    public static FlowExposure ReadExposure(IElement structure)
    {
        var faces = new List<(FlowFace Face, Relation Surface, IElement Selected)>();
        CollectFaces(structure, faces);
        return new FlowExposure(
            faces.Any(face => face.Face == FlowFace.Forward),
            faces.Any(face => face.Face == FlowFace.Inverse));
    }

    public static bool TryReadExposure(
        IElement structure,
        out FlowExposure exposure,
        out IElement selected,
        out Relation forwardFlow,
        out Relation inverseFlow)
    {
        var faces = new List<(FlowFace Face, Relation Surface, IElement Selected)>();
        CollectFaces(structure, faces);
        if (faces.Count == 0)
        {
            exposure = default;
            selected = default!;
            forwardFlow = null!;
            inverseFlow = null!;
            return false;
        }

        var forward = faces.FirstOrDefault(face => face.Face == FlowFace.Forward);
        var inverse = faces.FirstOrDefault(face => face.Face == FlowFace.Inverse);
        exposure = new FlowExposure(forward.Surface is not null, inverse.Surface is not null);
        selected = forward.Surface is not null
            ? forward.Selected
            : inverse.Selected;
        forwardFlow = forward.Surface ?? ConceptSurface.UnknownPoint();
        inverseFlow = inverse.Surface ?? ConceptSurface.UnknownPoint();
        return true;
    }

    private static Relation FacePoint(FlowFace face) =>
        Relation.Segment(new Carrier(), 0, face.IsInverse ? -1 : 1);

    private static bool TryReadFace(IElement element, out FlowFace face)
    {
        if (element is Relation segment &&
            segment.Left is Relation start &&
            segment.Right is Relation end &&
            ConceptSurfaceReader.TryReadPoint(start, out var startLocation) &&
            ConceptSurfaceReader.TryReadPoint(end, out var endLocation) &&
            startLocation.Carrier.SameIdentityAs(endLocation.Carrier) &&
            startLocation.Ticks == 0 &&
            (endLocation.Ticks == 1 || endLocation.Ticks == -1))
        {
            face = new FlowFace(endLocation.Ticks == -1);
            return true;
        }

        face = default;
        return false;
    }

    private static void CollectFaces(
        IElement structure,
        List<(FlowFace Face, Relation Surface, IElement Selected)> faces)
    {
        if (structure is not Relation relation)
        {
            return;
        }

        if (TryRead(relation, out var face, out var selected, out _))
        {
            faces.Add((face, relation, selected));
            return;
        }

        if (ConceptSurfaceReader.TryReadRootElements(relation, out var elements))
        {
            foreach (var element in elements)
            {
                CollectFaces(element, faces);
            }
        }
    }
}

/// <summary>
/// Explicit no-flow evidence: selected structure plus a zero-difference flow
/// witness. This is not primitive grade-0 flow; it is an inspectable statement
/// that no F/I face is exposed at this read.
/// </summary>
public static class NoFlowSurface
{
    public static Relation Create(Carrier root, IElement selected, IElement witness) =>
        Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            Relation.Segment(new Carrier(), 0, 0),
            selected ?? ConceptSurface.UnknownPoint(),
            witness ?? ConceptSurface.UnknownPoint());

    public static Relation Create(Carrier root, IElement selected) =>
        Create(root, selected, ConceptSurface.UnknownPoint());

    public static bool TryRead(
        Relation surface,
        out IElement selected,
        out IElement witness)
    {
        if (ConceptSurfaceReader.TryReadRootElements(surface, out var elements) &&
            elements.Length == 3 &&
            elements[0] is Relation marker &&
            marker.Left is Relation start &&
            marker.Right is Relation end &&
            ConceptSurfaceReader.TryReadPoint(start, out var startLocation) &&
            ConceptSurfaceReader.TryReadPoint(end, out var endLocation) &&
            startLocation.Carrier.SameIdentityAs(endLocation.Carrier) &&
            startLocation.Ticks == 0 &&
            endLocation.Ticks == 0)
        {
            selected = elements[1];
            witness = elements[2];
            return true;
        }

        selected = default!;
        witness = default!;
        return false;
    }
}

/// <summary>
/// Primitive grade-1 boundary side. A side is born over a primitive flow face;
/// detached/no-flow boundary reads are later abstractions.
/// </summary>
public readonly record struct BoundarySide(bool IsNegative)
{
    public static BoundarySide Positive => new(false);

    public static BoundarySide Negative => new(true);

    public string Label => IsNegative ? "N" : "P";

    public BoundarySide Dual() => new(!IsNegative);

    public BoundaryLane ToLane() => IsNegative ? BoundaryLane.Negative : BoundaryLane.Positive;

    public override string ToString() => Label;
}

/// <summary>
/// Physical grade-1 boundary side evidence. A valid side carries the flow face
/// it was cut over, so P/N are not born as flowless primitives.
/// </summary>
public static class BoundarySideSurface
{
    public static Relation Create(
        Carrier root,
        BoundarySide side,
        Relation flowFace,
        IElement cutEvidence) =>
        Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            SidePoint(side),
            flowFace ?? ConceptSurface.UnknownPoint(),
            cutEvidence ?? ConceptSurface.UnknownPoint());

    public static Relation Positive(Carrier root, Relation flowFace, IElement cutEvidence) =>
        Create(root, BoundarySide.Positive, flowFace, cutEvidence);

    public static Relation Positive(Carrier root, Relation flowFace) =>
        Positive(root, flowFace, ConceptSurface.UnknownPoint());

    public static Relation Negative(Carrier root, Relation flowFace, IElement cutEvidence) =>
        Create(root, BoundarySide.Negative, flowFace, cutEvidence);

    public static Relation Negative(Carrier root, Relation flowFace) =>
        Negative(root, flowFace, ConceptSurface.UnknownPoint());

    public static bool TryRead(
        Relation surface,
        out BoundarySide side,
        out Relation flowFace,
        out IElement cutEvidence)
    {
        if (ConceptSurfaceReader.TryReadRootElements(surface, out var elements) &&
            elements.Length == 3 &&
            TryReadSide(elements[0], out var read) &&
            elements[1] is Relation flowCandidate &&
            FlowFaceSurface.TryRead(flowCandidate, out _, out _, out _))
        {
            side = read;
            flowFace = flowCandidate;
            cutEvidence = elements[2];
            return true;
        }

        side = default;
        flowFace = null!;
        cutEvidence = default!;
        return false;
    }

    public static bool TryReadExposure(IElement structure, out BoundaryExposure exposure)
    {
        return TryReadExposure(
            structure,
            out exposure,
            out _,
            out _,
            out _);
    }

    public static bool TryReadExposure(
        IElement structure,
        out BoundaryExposure exposure,
        out IElement selected,
        out Relation positiveSide,
        out Relation negativeSide)
    {
        var sides = new List<(BoundarySide Side, Relation Surface, IElement Selected)>();
        CollectSides(structure, sides);
        if (sides.Count == 0)
        {
            exposure = default;
            selected = default!;
            positiveSide = null!;
            negativeSide = null!;
            return false;
        }

        var positive = sides.FirstOrDefault(side => side.Side == BoundarySide.Positive);
        var negative = sides.FirstOrDefault(side => side.Side == BoundarySide.Negative);
        exposure = (positive.Surface is not null, negative.Surface is not null) switch
        {
            (true, true) => BoundaryExposure.FullSigned,
            (true, false) => BoundaryExposure.Positive,
            (false, true) => BoundaryExposure.Negative,
            _ => default
        };
        selected = positive.Surface is not null
            ? positive.Selected
            : negative.Selected;
        positiveSide = positive.Surface ?? ConceptSurface.UnknownPoint();
        negativeSide = negative.Surface ?? ConceptSurface.UnknownPoint();
        return true;
    }

    private static Relation SidePoint(BoundarySide side) =>
        Relation.Point(new Carrier(), side.IsNegative ? 1 : 0);

    private static bool TryReadSide(IElement element, out BoundarySide side)
    {
        if (ConceptSurfaceReader.TryReadPoint(element, out var location) &&
            (location.Ticks == 0 || location.Ticks == 1))
        {
            side = new BoundarySide(location.Ticks == 1);
            return true;
        }

        side = default;
        return false;
    }

    private static void CollectSides(
        IElement structure,
        List<(BoundarySide Side, Relation Surface, IElement Selected)> sides)
    {
        if (structure is not Relation relation)
        {
            return;
        }

        if (TryRead(relation, out var side, out var flowFace, out _))
        {
            FlowFaceSurface.TryRead(flowFace, out _, out var selected, out _);
            sides.Add((side, relation, selected));
            return;
        }

        if (ConceptSurfaceReader.TryReadRootElements(relation, out var elements))
        {
            foreach (var element in elements)
            {
                CollectSides(element, sides);
            }
        }
    }
}

/// <summary>
/// Derived boundary-side evidence after flow has been stripped or deliberately
/// ignored. Primitive P/N sides should use <see cref="BoundarySideSurface"/>
/// over a concrete flow face; this shape keeps P:None and N:None inspectable
/// without pretending they were born as grade-1 primitives.
/// </summary>
public static class StrippedBoundarySideSurface
{
    public static Relation Create(
        Carrier root,
        BoundarySide side,
        IElement selected,
        IElement residue)
    {
        var noFlow = FlowSurface.None(new Carrier(), selected ?? ConceptSurface.UnknownPoint());
        return Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            SidePoint(side),
            noFlow,
            selected ?? ConceptSurface.UnknownPoint(),
            residue ?? ConceptSurface.UnknownPoint());
    }

    public static Relation Positive(Carrier root, IElement selected, IElement residue) =>
        Create(root, BoundarySide.Positive, selected, residue);

    public static Relation Negative(Carrier root, IElement selected, IElement residue) =>
        Create(root, BoundarySide.Negative, selected, residue);

    public static bool TryRead(
        Relation surface,
        out BoundarySide side,
        out Relation noFlowSurface,
        out IElement selected,
        out IElement residue)
    {
        if (ConceptSurfaceReader.TryReadRootElements(surface, out var elements) &&
            elements.Length == 4 &&
            TryReadSide(elements[0], out var read) &&
            elements[1] is Relation flowCandidate &&
            FlowSurface.TryRead(flowCandidate, out var flow, out _, out _, out _) &&
            flow.IsNone)
        {
            side = read;
            noFlowSurface = flowCandidate;
            selected = elements[2];
            residue = elements[3];
            return true;
        }

        side = default;
        noFlowSurface = null!;
        selected = default!;
        residue = default!;
        return false;
    }

    public static bool TryReadExposure(
        IElement structure,
        out BoundaryExposure exposure,
        out IElement selected,
        out Relation positiveSide,
        out Relation negativeSide)
    {
        var sides = new List<(BoundarySide Side, Relation Surface, IElement Selected)>();
        CollectSides(structure, sides);
        if (sides.Count == 0)
        {
            exposure = default;
            selected = default!;
            positiveSide = null!;
            negativeSide = null!;
            return false;
        }

        var positive = sides.FirstOrDefault(side => side.Side == BoundarySide.Positive);
        var negative = sides.FirstOrDefault(side => side.Side == BoundarySide.Negative);
        exposure = (positive.Surface is not null, negative.Surface is not null) switch
        {
            (true, true) => BoundaryExposure.FullSigned,
            (true, false) => BoundaryExposure.Positive,
            (false, true) => BoundaryExposure.Negative,
            _ => default
        };
        selected = positive.Surface is not null
            ? positive.Selected
            : negative.Selected;
        positiveSide = positive.Surface ?? ConceptSurface.UnknownPoint();
        negativeSide = negative.Surface ?? ConceptSurface.UnknownPoint();
        return true;
    }

    private static void CollectSides(
        IElement structure,
        List<(BoundarySide Side, Relation Surface, IElement Selected)> sides)
    {
        if (structure is not Relation relation)
        {
            return;
        }

        if (TryRead(relation, out var side, out _, out var selected, out _))
        {
            sides.Add((side, relation, selected));
            return;
        }

        if (ConceptSurfaceReader.TryReadRootElements(relation, out var elements))
        {
            foreach (var element in elements)
            {
                CollectSides(element, sides);
            }
        }
    }

    private static Relation SidePoint(BoundarySide side) =>
        Relation.Point(new Carrier(), side.IsNegative ? 1 : 0);

    private static bool TryReadSide(IElement element, out BoundarySide side)
    {
        if (ConceptSurfaceReader.TryReadPoint(element, out var location) &&
            (location.Ticks == 0 || location.Ticks == 1))
        {
            side = new BoundarySide(location.Ticks == 1);
            return true;
        }

        side = default;
        return false;
    }
}

/// <summary>
/// Physical bounded-whole evidence: a selected whole, its boundary pair, and an
/// optional partition/resolution witness. This is the primitive foothold for
/// the `S` boundary read.
/// </summary>
public static class BoundedWholeSurface
{
    public static Relation Create(
        Carrier root,
        IElement whole,
        Relation leftBoundary,
        Relation rightBoundary,
        IElement partition)
    {
        var boundaryPair = new Relation(
            leftBoundary ?? ConceptSurface.UnknownPoint(),
            rightBoundary ?? ConceptSurface.UnknownPoint());

        return Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            whole ?? ConceptSurface.UnknownPoint(),
            boundaryPair,
            partition ?? ConceptSurface.UnknownPoint());
    }

    public static Relation Create(
        Carrier root,
        IElement whole,
        Relation leftBoundary,
        Relation rightBoundary) =>
        Create(root, whole, leftBoundary, rightBoundary, ConceptSurface.UnknownPoint());

    public static Relation Create(Carrier root, IElement whole) =>
        Create(
            root,
            whole,
            ConceptSurface.UnknownPoint(),
            ConceptSurface.UnknownPoint(),
            ConceptSurface.UnknownPoint());

    public static bool TryRead(
        Relation surface,
        out IElement whole,
        out Relation leftBoundary,
        out Relation rightBoundary,
        out IElement partition)
    {
        if (ConceptSurfaceReader.TryReadRootElements(surface, out var elements) &&
            elements.Length == 3 &&
            elements[1] is Relation boundaryPair &&
            boundaryPair.Left is Relation left &&
            boundaryPair.Right is Relation right &&
            ConceptSurfaceReader.TryReadPoint(left, out _) &&
            ConceptSurfaceReader.TryReadPoint(right, out _))
        {
            whole = elements[0];
            leftBoundary = left;
            rightBoundary = right;
            partition = elements[2];
            return true;
        }

        whole = default!;
        leftBoundary = null!;
        rightBoundary = null!;
        partition = default!;
        return false;
    }

    public static bool TryFind(
        IElement structure,
        out Relation surface,
        out IElement whole,
        out Relation leftBoundary,
        out Relation rightBoundary,
        out IElement partition)
    {
        if (structure is not Relation relation)
        {
            surface = null!;
            whole = default!;
            leftBoundary = null!;
            rightBoundary = null!;
            partition = default!;
            return false;
        }

        if (TryRead(relation, out whole, out leftBoundary, out rightBoundary, out partition))
        {
            surface = relation;
            return true;
        }

        if (ConceptSurfaceReader.TryReadRootElements(relation, out var elements))
        {
            foreach (var element in elements)
            {
                if (TryFind(
                        element,
                        out surface,
                        out whole,
                        out leftBoundary,
                        out rightBoundary,
                        out partition))
                {
                    return true;
                }
            }
        }

        surface = null!;
        whole = default!;
        leftBoundary = null!;
        rightBoundary = null!;
        partition = default!;
        return false;
    }
}

/// <summary>
/// Builds the lowest currently available structural evidence for a boundary
/// exposure over a flow exposure. The helper only assembles carrier/relation
/// structure; the boundary/flow meaning remains inspectable after construction.
/// </summary>
public static class BoundaryFlowPrimitiveEvidence
{
    public static bool TryCreate(
        Carrier root,
        BoundaryExposure boundary,
        FlowExposure flow,
        IElement selected,
        out Relation boundaryEvidence,
        out Relation flowEvidence) =>
        TryCreate(
            root,
            boundary,
            flow,
            selected,
            ConceptSurface.UnknownPoint(),
            ConceptSurface.UnknownPoint(),
            out boundaryEvidence,
            out flowEvidence);

    public static bool TryCreate(
        Carrier root,
        BoundaryExposure boundary,
        FlowExposure flow,
        IElement selected,
        IElement boundaryWitness,
        IElement flowWitness,
        out Relation boundaryEvidence,
        out Relation flowEvidence)
    {
        var selectedSurface = selected ?? ConceptSurface.UnknownPoint();
        flowEvidence = CreateFlowEvidence(flow, selectedSurface, flowWitness);

        if (boundary.IsBoundedWhole)
        {
            boundaryEvidence = BoundedWholeSurface.Create(
                ConceptSurface.RootOrNew(root),
                selectedSurface);
            return true;
        }

        if (flow.IsNone)
        {
            boundaryEvidence = null!;
            return false;
        }

        var flowFaces = CreateFlowFaces(flow, selectedSurface, flowWitness);
        var boundarySides = CreateBoundarySides(boundary, flowFaces, boundaryWitness);
        if (boundarySides.Length == 0)
        {
            boundaryEvidence = null!;
            return false;
        }

        boundaryEvidence = Relation.OrderedGroup(ConceptSurface.RootOrNew(root), boundarySides);
        return true;
    }

    public static Relation CreateFlowEvidence(
        FlowExposure flow,
        IElement selected,
        IElement witness)
    {
        var flowFaces = CreateFlowFaces(flow, selected, witness);
        if (flowFaces.Length == 0)
        {
            return NoFlowSurface.Create(new Carrier(), selected, witness ?? ConceptSurface.UnknownPoint());
        }

        return flowFaces.Length == 1
            ? flowFaces[0]
            : Relation.OrderedGroup(new Carrier(), flowFaces);
    }

    public static Relation[] CreateFlowFaces(
        FlowExposure flow,
        IElement selected,
        IElement witness)
    {
        var faces = new List<Relation>(2);
        if (flow.ForwardFlow)
        {
            faces.Add(FlowFaceSurface.Forward(
                new Carrier(),
                selected ?? ConceptSurface.UnknownPoint(),
                witness ?? ConceptSurface.UnknownPoint()));
        }

        if (flow.InverseFlow)
        {
            faces.Add(FlowFaceSurface.Inverse(
                new Carrier(),
                selected ?? ConceptSurface.UnknownPoint(),
                witness ?? ConceptSurface.UnknownPoint()));
        }

        return [.. faces];
    }

    public static Relation[] CreateBoundarySides(
        BoundaryExposure boundary,
        Relation[] flowFaces,
        IElement witness)
    {
        var sides = new List<Relation>();
        var includePositive = boundary.IsPositive || boundary.IsFullSigned;
        var includeNegative = boundary.IsNegative || boundary.IsFullSigned;

        foreach (var flowFace in flowFaces)
        {
            if (includePositive)
            {
                sides.Add(BoundarySideSurface.Positive(
                    new Carrier(),
                    flowFace,
                    witness ?? ConceptSurface.UnknownPoint()));
            }

            if (includeNegative)
            {
                sides.Add(BoundarySideSurface.Negative(
                    new Carrier(),
                    flowFace,
                    witness ?? ConceptSurface.UnknownPoint()));
            }
        }

        return [.. sides];
    }
}

/// <summary>
/// Fuses one primitive flow face with one boundary side cut over it and reads
/// the result as the existing occupied-lane vocabulary.
/// </summary>
public static class FlowBoundaryGradeLaneSurface
{
    public static Relation Create(
        Carrier root,
        FlowFace flow,
        BoundarySide side,
        IElement selected,
        IElement cutEvidence)
    {
        var flowFace = FlowFaceSurface.Create(new Carrier(), flow, selected, ConceptSurface.UnknownPoint());
        var sideSurface = BoundarySideSurface.Create(new Carrier(), side, flowFace, cutEvidence);
        return Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            flowFace,
            sideSurface,
            selected ?? ConceptSurface.UnknownPoint());
    }

    public static Relation Create(
        Carrier root,
        FlowFace flow,
        BoundarySide side,
        IElement selected) =>
        Create(root, flow, side, selected, ConceptSurface.UnknownPoint());

    public static bool TryRead(
        Relation surface,
        out BoundaryFlowLane lane,
        out Relation flowFace,
        out Relation boundarySide,
        out IElement selected)
    {
        if (ConceptSurfaceReader.TryReadRootElements(surface, out var elements) &&
            elements.Length >= 3 &&
            elements[0] is Relation flowCandidate &&
            elements[1] is Relation sideCandidate &&
            FlowFaceSurface.TryRead(flowCandidate, out var flow, out _, out _) &&
            BoundarySideSurface.TryRead(sideCandidate, out var side, out var sideFlow, out _) &&
            sideFlow.Equals(flowCandidate))
        {
            lane = new BoundaryFlowLane(side.ToLane(), flow.ToLane());
            flowFace = flowCandidate;
            boundarySide = sideCandidate;
            selected = elements[2];
            return true;
        }

        lane = default;
        flowFace = null!;
        boundarySide = null!;
        selected = default!;
        return false;
    }
}

/// <summary>
/// Local read value for the boundary side occupied by one value. This is a
/// compact read over lane-marker structure, not a concept tag.
/// </summary>
public readonly record struct BoundaryLane(bool PositiveSide, bool NegativeSide, bool WholeSegment)
{
    public static BoundaryLane Positive => new(true, false, false);

    public static BoundaryLane Negative => new(false, true, false);

    public static BoundaryLane Whole => new(false, false, true);

    public bool IsPositive => PositiveSide && !NegativeSide && !WholeSegment;

    public bool IsNegative => !PositiveSide && NegativeSide && !WholeSegment;

    public bool IsWhole => !PositiveSide && !NegativeSide && WholeSegment;

    public bool IsValid => IsPositive || IsNegative || IsWhole;

    public string Label => this switch
    {
        { PositiveSide: true, NegativeSide: false, WholeSegment: false } => "P",
        { PositiveSide: false, NegativeSide: true, WholeSegment: false } => "N",
        { PositiveSide: false, NegativeSide: false, WholeSegment: true } => "S",
        _ => "Invalid"
    };

    public BoundaryLane Dual() => IsPositive
        ? Negative
        : IsNegative
            ? Positive
            : Whole;

    public override string ToString() => Label;
}

/// <summary>
/// Local read value for the flow face occupied by one value. No-flow is a real
/// occupied lane for position/partition values; it is not a missing marker.
/// </summary>
public readonly record struct FlowLane(bool ForwardFace, bool InverseFace, bool NoFlowFace)
{
    public static FlowLane None => new(false, false, true);

    public static FlowLane Forward => new(true, false, false);

    public static FlowLane Inverse => new(false, true, false);

    public bool IsNone => !ForwardFace && !InverseFace && NoFlowFace;

    public bool IsForward => ForwardFace && !InverseFace && !NoFlowFace;

    public bool IsInverse => !ForwardFace && InverseFace && !NoFlowFace;

    public bool IsValid => IsNone || IsForward || IsInverse;

    public string Label => this switch
    {
        { ForwardFace: false, InverseFace: false, NoFlowFace: true } => "None",
        { ForwardFace: true, InverseFace: false, NoFlowFace: false } => "F",
        { ForwardFace: false, InverseFace: true, NoFlowFace: false } => "I",
        _ => "Invalid"
    };

    public FlowLane Dual() => IsForward
        ? Inverse
        : IsInverse
            ? Forward
            : None;

    public override string ToString() => Label;
}

/// <summary>
/// Occupied lane for a magnitude value on a boundary/flow basis. For example,
/// magnitude 5 on this lane can read as +5f, -5i, or a no-flow whole value.
/// </summary>
public readonly record struct BoundaryFlowLane(BoundaryLane Boundary, FlowLane Flow)
{
    public static BoundaryFlowLane PositiveForward => new(BoundaryLane.Positive, FlowLane.Forward);

    public static BoundaryFlowLane PositiveInverse => new(BoundaryLane.Positive, FlowLane.Inverse);

    public static BoundaryFlowLane PositiveNone => new(BoundaryLane.Positive, FlowLane.None);

    public static BoundaryFlowLane NegativeForward => new(BoundaryLane.Negative, FlowLane.Forward);

    public static BoundaryFlowLane NegativeInverse => new(BoundaryLane.Negative, FlowLane.Inverse);

    public static BoundaryFlowLane NegativeNone => new(BoundaryLane.Negative, FlowLane.None);

    public static BoundaryFlowLane WholeForward => new(BoundaryLane.Whole, FlowLane.Forward);

    public static BoundaryFlowLane WholeInverse => new(BoundaryLane.Whole, FlowLane.Inverse);

    public static BoundaryFlowLane WholeNone => new(BoundaryLane.Whole, FlowLane.None);

    public bool IsValid => Boundary.IsValid && Flow.IsValid;

    public bool IsSignedFlowPhase =>
        (Boundary.IsPositive || Boundary.IsNegative) &&
        (Flow.IsForward || Flow.IsInverse);

    public string Label => !IsValid
        ? "Invalid"
        : Flow.IsNone
            ? $"{Boundary}:None"
            : $"{Boundary}{Flow}";

    public BoundaryFlowLane DualBoundary() => new(Boundary.Dual(), Flow);

    public BoundaryFlowLane DualFlow() => new(Boundary, Flow.Dual());

    public BoundaryFlowLane Dual() => new(Boundary.Dual(), Flow.Dual());

    public bool TryStepFlowPhase(out BoundaryFlowLane next)
    {
        if (Boundary.IsPositive && Flow.IsForward)
        {
            next = PositiveInverse;
            return true;
        }

        if (Boundary.IsPositive && Flow.IsInverse)
        {
            next = NegativeForward;
            return true;
        }

        if (Boundary.IsNegative && Flow.IsForward)
        {
            next = NegativeInverse;
            return true;
        }

        if (Boundary.IsNegative && Flow.IsInverse)
        {
            next = PositiveForward;
            return true;
        }

        next = default;
        return false;
    }

    public override string ToString() => Label;
}

/// <summary>
/// Reads the legal occupied lanes implied by a boundary/flow exposure. The
/// exposure defines the legal lane basis; individual values still carry their
/// chosen occupied lane locally.
/// </summary>
public static class BoundaryFlowLaneBasis
{
    public static BoundaryLane[] BoundaryLanes(BoundaryExposure boundary) =>
        boundary switch
        {
            { LeftBoundary: false, RightBoundary: false } => [BoundaryLane.Positive, BoundaryLane.Negative],
            { LeftBoundary: true, RightBoundary: false } => [BoundaryLane.Positive],
            { LeftBoundary: false, RightBoundary: true } => [BoundaryLane.Negative],
            _ => [BoundaryLane.Whole]
        };

    public static FlowLane[] FlowLanes(FlowExposure flow) =>
        flow switch
        {
            { ForwardFlow: false, InverseFlow: false } => [FlowLane.None],
            { ForwardFlow: true, InverseFlow: false } => [FlowLane.Forward],
            { ForwardFlow: false, InverseFlow: true } => [FlowLane.Inverse],
            _ => [FlowLane.Forward, FlowLane.Inverse]
        };

    public static BoundaryFlowLane[] LegalLanes(BoundaryExposure boundary, FlowExposure flow)
    {
        var boundaryLanes = BoundaryLanes(boundary);
        var flowLanes = FlowLanes(flow);
        var lanes = new List<BoundaryFlowLane>(boundaryLanes.Length * flowLanes.Length);

        foreach (var boundaryLane in boundaryLanes)
        {
            foreach (var flowLane in flowLanes)
            {
                lanes.Add(new BoundaryFlowLane(boundaryLane, flowLane));
            }
        }

        return [.. lanes];
    }

    public static BoundaryFlowLane[] LegalLanes(BoundaryFlowExposure exposure) =>
        LegalLanes(exposure.Boundary, exposure.Flow);

    public static bool Allows(BoundaryExposure boundary, FlowExposure flow, BoundaryFlowLane lane) =>
        lane.IsValid &&
        BoundaryLanes(boundary).Contains(lane.Boundary) &&
        FlowLanes(flow).Contains(lane.Flow);

    public static bool Allows(BoundaryFlowExposure exposure, BoundaryFlowLane lane) =>
        Allows(exposure.Boundary, exposure.Flow, lane);

    public static bool TryInferSingleLane(
        BoundaryExposure boundary,
        FlowExposure flow,
        out BoundaryFlowLane lane)
    {
        var lanes = LegalLanes(boundary, flow);
        if (lanes.Length == 1)
        {
            lane = lanes[0];
            return true;
        }

        lane = default;
        return false;
    }

    public static bool TryReadLegalLanes(
        Relation boundaryFlowSurface,
        out BoundaryFlowExposure exposure,
        out BoundaryFlowLane[] lanes)
    {
        if (TryRead(boundaryFlowSurface, out exposure, out _, out lanes))
        {
            return true;
        }

        if (BoundaryFlowSurface.TryRead(boundaryFlowSurface, out exposure))
        {
            lanes = LegalLanes(exposure);
            return true;
        }

        if (CarrierAxisSurface.TryRead(boundaryFlowSurface, out _, out var boundary, out var flow, out _, out _))
        {
            exposure = new BoundaryFlowExposure(boundary, flow);
            lanes = LegalLanes(exposure);
            return true;
        }

        exposure = default;
        lanes = [];
        return false;
    }

    public static bool TryReadLegalLanes(
        Relation boundaryEvidence,
        Relation flowEvidence,
        out BoundaryFlowExposure exposure,
        out BoundaryFlowLane[] lanes)
    {
        if (BoundarySurface.TryRead(boundaryEvidence, out var boundary, out _, out _, out _) &&
            FlowSurface.TryRead(flowEvidence, out var flow, out _, out _, out _))
        {
            exposure = new BoundaryFlowExposure(boundary, flow);
            lanes = LegalLanes(exposure);
            return true;
        }

        exposure = default;
        lanes = [];
        return false;
    }

    public static bool Allows(Relation laneBasis, BoundaryFlowLane lane) =>
        TryReadLegalLanes(laneBasis, out _, out var lanes) &&
        lanes.Contains(lane);

    public static bool TryInferSingleLane(
        Relation laneBasis,
        out BoundaryFlowLane lane)
    {
        if (TryReadLegalLanes(laneBasis, out _, out var lanes) &&
            lanes.Length == 1)
        {
            lane = lanes[0];
            return true;
        }

        lane = default;
        return false;
    }

    public static Relation Create(
        Carrier root,
        Relation boundaryFlowSurface,
        IElement selected)
    {
        if (!BoundaryFlowSurface.TryRead(
                boundaryFlowSurface,
                out var boundary,
                out var flow,
                out var boundarySurface,
                out var flowSurface,
                out _))
        {
            return Relation.OrderedGroup(
                ConceptSurface.RootOrNew(root),
                boundaryFlowSurface ?? ConceptSurface.UnknownPoint(),
                selected ?? ConceptSurface.UnknownPoint(),
                ConceptSurface.UnknownPoint());
        }

        var lanes = LegalLanes(boundary, flow);
        var elements = new IElement[lanes.Length + 2];
        elements[0] = boundaryFlowSurface;
        elements[1] = selected ?? ConceptSurface.UnknownPoint();

        for (var index = 0; index < lanes.Length; index++)
        {
            elements[index + 2] = BoundaryFlowLaneSurface.Create(
                new Carrier(),
                lanes[index],
                boundarySurface,
                flowSurface);
        }

        return Relation.OrderedGroup(ConceptSurface.RootOrNew(root), elements);
    }

    public static Relation Create(
        Carrier root,
        BoundaryExposure boundary,
        FlowExposure flow,
        IElement selected)
    {
        var boundaryFlowSurface = BoundaryFlowSurface.Create(new Carrier(), boundary, flow, selected);
        return Create(root, boundaryFlowSurface, selected);
    }

    public static bool TryRead(
        Relation basis,
        out BoundaryFlowExposure exposure,
        out IElement selected,
        out BoundaryFlowLane[] lanes)
    {
        if (ConceptSurfaceReader.TryReadRootElements(basis, out var elements) &&
            elements.Length >= 2 &&
            elements[0] is Relation boundaryFlowSurface &&
            BoundaryFlowSurface.TryRead(boundaryFlowSurface, out exposure))
        {
            var explicitLanes = new List<BoundaryFlowLane>();
            for (var index = 2; index < elements.Length; index++)
            {
                if (elements[index] is Relation laneSurface &&
                    BoundaryFlowLaneSurface.TryRead(laneSurface, out var lane, out _, out _))
                {
                    explicitLanes.Add(lane);
                }
            }

            selected = elements[1];
            lanes = explicitLanes.Count == 0 ? LegalLanes(exposure) : [.. explicitLanes];
            return true;
        }

        exposure = default;
        selected = default!;
        lanes = [];
        return false;
    }

    /// <summary>
    /// Reads possible lanes from a unit-shape descriptor, or returns the
    /// low-resolution whole/no-flow default for descriptor-free unit shapes.
    /// </summary>
    public static bool TryReadUnitShapeBasis(
        UnitShape unitShape,
        out Relation basis,
        out BoundaryFlowExposure exposure,
        out IElement selected,
        out BoundaryFlowLane[] lanes)
    {
        if (TryReadUnitShapeDescriptorBasis(unitShape, out basis, out exposure, out selected, out lanes))
        {
            return true;
        }

        if (unitShape is not null)
        {
            basis = Create(
                new Carrier(),
                BoundaryExposure.BoundedWhole,
                FlowExposure.None,
                unitShape.Surface);
            return TryRead(basis, out exposure, out selected, out lanes);
        }

        basis = null!;
        exposure = default;
        selected = default!;
        lanes = [];
        return false;
    }

    /// <summary>Reads only explicit lane-basis descriptors, without applying the default.</summary>
    internal static bool TryReadUnitShapeDescriptorBasis(
        UnitShape unitShape,
        out Relation basis,
        out BoundaryFlowExposure exposure,
        out IElement selected,
        out BoundaryFlowLane[] lanes)
    {
        if (unitShape is not null)
        {
            foreach (var descriptor in unitShape.DescriptorSurfaces)
            {
                if (TryReadDescriptorBasis(descriptor, out basis, out exposure, out selected, out lanes))
                {
                    return true;
                }
            }
        }

        basis = null!;
        exposure = default;
        selected = default!;
        lanes = [];
        return false;
    }

    private static bool TryReadDescriptorBasis(
        IElement descriptor,
        out Relation basis,
        out BoundaryFlowExposure exposure,
        out IElement selected,
        out BoundaryFlowLane[] lanes)
    {
        if (descriptor is Relation candidate)
        {
            if (TryRead(candidate, out exposure, out selected, out lanes))
            {
                basis = candidate;
                return true;
            }

            if (ConceptSurfaceReader.TryReadRootElements(candidate, out var elements))
            {
                foreach (var element in elements)
                {
                    if (TryReadDescriptorBasis(element, out basis, out exposure, out selected, out lanes))
                    {
                        return true;
                    }
                }
            }
        }

        basis = null!;
        exposure = default;
        selected = default!;
        lanes = [];
        return false;
    }

    internal static bool TryReadExposure(Relation surface, out BoundaryFlowExposure exposure)
    {
        if (BoundaryFlowSurface.TryRead(surface, out exposure) ||
            TryRead(surface, out exposure, out _, out _))
        {
            return true;
        }

        if (CarrierAxisSurface.TryRead(surface, out _, out var boundary, out var flow, out _, out _))
        {
            exposure = new BoundaryFlowExposure(boundary, flow);
            return true;
        }

        exposure = default;
        return false;
    }
}

/// <summary>
/// Visible lane-marker surface for the boundary side occupied by a value.
/// </summary>
public static class BoundaryLaneSurface
{
    public static Relation Create(Carrier root, BoundaryLane lane, IElement evidence)
    {
        if (lane.IsPositive)
        {
            return StrippedBoundarySideSurface.Positive(
                ConceptSurface.RootOrNew(root),
                evidence ?? ConceptSurface.UnknownPoint(),
                ConceptSurface.UnknownPoint());
        }

        if (lane.IsNegative)
        {
            return StrippedBoundarySideSurface.Negative(
                ConceptSurface.RootOrNew(root),
                evidence ?? ConceptSurface.UnknownPoint(),
                ConceptSurface.UnknownPoint());
        }

        if (lane.IsWhole)
        {
            return BoundedWholeSurface.Create(
                ConceptSurface.RootOrNew(root),
                evidence ?? ConceptSurface.UnknownPoint());
        }

        return Relation.OrderedGroup(ConceptSurface.RootOrNew(root));
    }

    public static Relation Create(
        Carrier root,
        BoundaryLane lane,
        Relation flowFace,
        IElement evidence)
    {
        if (lane.IsPositive)
        {
            return BoundarySideSurface.Positive(
                ConceptSurface.RootOrNew(root),
                flowFace,
                evidence ?? ConceptSurface.UnknownPoint());
        }

        if (lane.IsNegative)
        {
            return BoundarySideSurface.Negative(
                ConceptSurface.RootOrNew(root),
                flowFace,
                evidence ?? ConceptSurface.UnknownPoint());
        }

        return Create(root, lane, evidence);
    }

    public static bool TryRead(
        Relation surface,
        out BoundaryLane lane,
        out IElement evidence)
    {
        if (BoundarySideSurface.TryRead(surface, out var side, out _, out var sideEvidence))
        {
            lane = side.ToLane();
            evidence = sideEvidence;
            return true;
        }

        if (BoundedWholeSurface.TryRead(surface, out _, out _, out _, out var partition))
        {
            lane = BoundaryLane.Whole;
            evidence = partition;
            return true;
        }

        if (StrippedBoundarySideSurface.TryRead(
                surface,
                out var strippedSide,
                out _,
                out var strippedSelected,
                out var strippedResidue))
        {
            lane = strippedSide.ToLane();
            evidence = strippedSelected ?? strippedResidue;
            return true;
        }

        lane = default;
        evidence = default!;
        return false;
    }
}

/// <summary>
/// Visible lane-marker surface for the flow face occupied by a value.
/// </summary>
public static class FlowLaneSurface
{
    public static Relation Create(Carrier root, FlowLane lane, IElement evidence)
    {
        if (lane.IsForward)
        {
            return FlowFaceSurface.Forward(
                ConceptSurface.RootOrNew(root),
                evidence ?? ConceptSurface.UnknownPoint());
        }

        if (lane.IsInverse)
        {
            return FlowFaceSurface.Inverse(
                ConceptSurface.RootOrNew(root),
                evidence ?? ConceptSurface.UnknownPoint());
        }

        if (lane.IsNone)
        {
            return FlowSurface.None(
                ConceptSurface.RootOrNew(root),
                evidence ?? ConceptSurface.UnknownPoint());
        }

        return Relation.OrderedGroup(ConceptSurface.RootOrNew(root));
    }

    public static bool TryRead(
        Relation surface,
        out FlowLane lane,
        out IElement evidence)
    {
        if (FlowFaceSurface.TryRead(surface, out var face, out _, out var faceEvidence))
        {
            lane = face.ToLane();
            evidence = faceEvidence;
            return true;
        }

        if (FlowSurface.TryRead(
                surface,
                out var flow,
                out _,
                out _,
                out _) &&
            flow.IsNone)
        {
            lane = FlowLane.None;
            evidence = surface;
            return true;
        }

        lane = default;
        evidence = default!;
        return false;
    }
}

/// <summary>
/// Visible marker that attaches one boundary lane and one flow lane.
/// </summary>
public static class BoundaryFlowLaneSurface
{
    public static Relation Create(
        Carrier root,
        BoundaryFlowLane lane,
        IElement boundaryEvidence,
        IElement flowEvidence)
    {
        var flowLaneSurface = FlowLaneSurface.Create(
            new Carrier(),
            lane.Flow,
            flowEvidence ?? ConceptSurface.UnknownPoint());
        var boundaryLaneSurface = lane.Flow.IsForward || lane.Flow.IsInverse
            ? BoundaryLaneSurface.Create(
                new Carrier(),
                lane.Boundary,
                flowLaneSurface,
                boundaryEvidence ?? ConceptSurface.UnknownPoint())
            : BoundaryLaneSurface.Create(
                new Carrier(),
                lane.Boundary,
                boundaryEvidence ?? ConceptSurface.UnknownPoint());

        return Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            boundaryLaneSurface,
            flowLaneSurface);
    }

    public static Relation Create(Carrier root, BoundaryFlowLane lane) =>
        Create(root, lane, ConceptSurface.UnknownPoint(), ConceptSurface.UnknownPoint());

    public static bool TryRead(
        Relation surface,
        out BoundaryFlowLane lane,
        out Relation boundaryLaneSurface,
        out Relation flowLaneSurface)
    {
        if (ConceptSurfaceReader.TryReadRootElements(surface, out var elements) &&
            elements.Length >= 2 &&
            elements[0] is Relation boundaryCandidate &&
            elements[1] is Relation flowCandidate &&
            BoundaryLaneSurface.TryRead(boundaryCandidate, out var boundaryLane, out _) &&
            FlowLaneSurface.TryRead(flowCandidate, out var flowLane, out _))
        {
            lane = new BoundaryFlowLane(boundaryLane, flowLane);
            boundaryLaneSurface = boundaryCandidate;
            flowLaneSurface = flowCandidate;
            return lane.IsValid;
        }

        lane = default;
        boundaryLaneSurface = null!;
        flowLaneSurface = null!;
        return false;
    }
}

public readonly record struct BoundaryFlowLaneTransitionRead(
    BoundaryFlowLane Source,
    BoundaryFlowLane Target,
    IElement Transition,
    IElement Witness);

/// <summary>
/// Directed evidence that tension may move from one occupied lane to another.
/// Possible lanes make a lane legal; this surface makes a lane-to-lane move
/// traversable.
/// </summary>
public static class BoundaryFlowLaneTransitionSurface
{
    public static Relation Create(
        Carrier root,
        BoundaryFlowLane source,
        BoundaryFlowLane target) =>
        Create(root, source, target, LaneRoute(source, target), ConceptSurface.UnknownPoint());

    public static Relation Create(
        Carrier root,
        BoundaryFlowLane source,
        BoundaryFlowLane target,
        IElement transition,
        IElement witness)
    {
        var sourceSurface = BoundaryFlowLaneSurface.Create(new Carrier(), source);
        var targetSurface = BoundaryFlowLaneSurface.Create(new Carrier(), target);
        return Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            sourceSurface,
            targetSurface,
            transition ?? LaneRoute(source, target),
            witness ?? ConceptSurface.UnknownPoint());
    }

    public static Relation[] BoundaryDualTransitions(
        Carrier root,
        BoundaryExposure boundary,
        FlowExposure flow)
    {
        if (!boundary.IsFullSigned)
        {
            return [];
        }

        var transitions = new List<Relation>();
        foreach (var flowLane in BoundaryFlowLaneBasis.FlowLanes(flow).Where(lane => !lane.IsNone))
        {
            var positive = new BoundaryFlowLane(BoundaryLane.Positive, flowLane);
            var negative = new BoundaryFlowLane(BoundaryLane.Negative, flowLane);
            transitions.Add(Create(ConceptSurface.RootOrNew(root), positive, negative));
            transitions.Add(Create(new Carrier(), negative, positive));
        }

        return [.. transitions];
    }

    public static Relation[] FlowPhaseStepTransitions(Carrier root)
    {
        var transitions = new List<Relation>();
        foreach (var source in new[]
                 {
                     BoundaryFlowLane.PositiveForward,
                     BoundaryFlowLane.PositiveInverse,
                     BoundaryFlowLane.NegativeForward,
                     BoundaryFlowLane.NegativeInverse
                 })
        {
            if (source.TryStepFlowPhase(out var target))
            {
                transitions.Add(Create(
                    transitions.Count == 0 ? ConceptSurface.RootOrNew(root) : new Carrier(),
                    source,
                    target));
            }
        }

        return [.. transitions];
    }

    public static bool TryRead(
        Relation surface,
        out BoundaryFlowLaneTransitionRead read)
    {
        if (ConceptSurfaceReader.TryReadRootElements(surface, out var elements) &&
            elements.Length >= 4 &&
            elements[0] is Relation sourceSurface &&
            elements[1] is Relation targetSurface &&
            BoundaryFlowLaneSurface.TryRead(sourceSurface, out var source, out _, out _) &&
            BoundaryFlowLaneSurface.TryRead(targetSurface, out var target, out _, out _))
        {
            read = new BoundaryFlowLaneTransitionRead(source, target, elements[2], elements[3]);
            return true;
        }

        read = default;
        return false;
    }

    public static bool TryFindTransition(
        IElement structure,
        BoundaryFlowLane source,
        BoundaryFlowLane target,
        out BoundaryFlowLaneTransitionRead read)
    {
        foreach (var candidate in ReadAll(structure))
        {
            if (candidate.Source.Equals(source) &&
                candidate.Target.Equals(target))
            {
                read = candidate;
                return true;
            }
        }

        read = default;
        return false;
    }

    public static bool TryFindUnitShapeTransition(
        UnitShape unitShape,
        BoundaryFlowLane source,
        BoundaryFlowLane target,
        out BoundaryFlowLaneTransitionRead read)
    {
        if (unitShape is not null)
        {
            foreach (var descriptor in unitShape.DescriptorSurfaces)
            {
                if (TryFindTransition(descriptor, source, target, out read))
                {
                    return true;
                }
            }
        }

        read = default;
        return false;
    }

    public static BoundaryFlowLaneTransitionRead[] ReadAll(IElement structure)
    {
        var reads = new List<BoundaryFlowLaneTransitionRead>();
        Collect(structure, reads);
        return [.. reads];
    }

    private static void Collect(
        IElement structure,
        List<BoundaryFlowLaneTransitionRead> reads)
    {
        if (structure is not Relation relation)
        {
            return;
        }

        if (TryRead(relation, out var read))
        {
            reads.Add(read);
            return;
        }

        if (ConceptSurfaceReader.TryReadRootElements(relation, out var elements))
        {
            foreach (var element in elements)
            {
                Collect(element, reads);
            }
        }
    }

    private static Relation LaneRoute(BoundaryFlowLane source, BoundaryFlowLane target) =>
        new(
            BoundaryFlowLaneSurface.Create(new Carrier(), source),
            BoundaryFlowLaneSurface.Create(new Carrier(), target));
}

/// <summary>
/// Small construction helpers for unit shapes whose tick character includes
/// boundary/flow lane descriptors. The returned unit shape carries ordinary
/// descriptor structure; the helper is not the source of meaning.
/// </summary>
public static class BoundaryFlowUnitShape
{
    public static UnitShape DescribedAtom(
        BoundaryExposure boundary,
        FlowExposure flow,
        IElement? selected,
        bool includeBoundaryDualTransitions)
    {
        var selectedSurface = selected ?? Relation.Point(new Carrier(), 1);
        var basis = TryCreatePrimitiveBasis(
            new Carrier(),
            boundary,
            flow,
            selectedSurface,
            out var primitiveBasis)
            ? primitiveBasis
            : BoundaryFlowLaneBasis.Create(
                new Carrier(),
                boundary,
                flow,
                selectedSurface);
        var descriptors = new List<IElement> { basis };

        if (includeBoundaryDualTransitions)
        {
            descriptors.AddRange(BoundaryFlowLaneTransitionSurface.BoundaryDualTransitions(
                new Carrier(),
                boundary,
                flow));
        }

        return UnitShape
            .DescribedAtom(basis)
            .WithDescriptors([.. descriptors]);
    }

    public static UnitShape DescribedAtom(
        BoundaryExposure boundary,
        FlowExposure flow,
        IElement? selected = null) =>
        DescribedAtom(boundary, flow, selected, includeBoundaryDualTransitions: false);

    public static UnitShape PositiveForward(IElement? selected = null) =>
        DescribedAtom(BoundaryExposure.Positive, FlowExposure.Forward, selected);

    public static UnitShape SignedForward(IElement? selected = null) =>
        DescribedAtom(
            BoundaryExposure.FullSigned,
            FlowExposure.Forward,
            selected,
            includeBoundaryDualTransitions: true);

    public static UnitShape SignedBothFlows(IElement? selected = null) =>
        DescribedAtom(
            BoundaryExposure.FullSigned,
            FlowExposure.Both,
            selected,
            includeBoundaryDualTransitions: true);

    public static UnitShape BoundedWholeNoFlow(IElement? selected = null) =>
        DescribedAtom(BoundaryExposure.BoundedWhole, FlowExposure.None, selected);

    public static bool TryCreatePrimitiveBasis(
        Carrier root,
        BoundaryExposure boundary,
        FlowExposure flow,
        IElement selected,
        out Relation basis)
    {
        var selectedSurface = selected ?? Relation.Point(new Carrier(), 1);
        if (!BoundaryFlowPrimitiveEvidence.TryCreate(
                new Carrier(),
                boundary,
                flow,
                selectedSurface,
                out var boundaryEvidence,
                out var flowEvidence))
        {
            basis = null!;
            return false;
        }

        var boundaryFlowSurface = BoundaryFlowSurface.Create(
            new Carrier(),
            boundaryEvidence,
            flowEvidence);
        basis = BoundaryFlowLaneBasis.Create(
            ConceptSurface.RootOrNew(root),
            boundaryFlowSurface,
            selectedSurface);
        return true;
    }
}

/// <summary>
/// Attaches an unsigned magnitude point to one occupied boundary/flow lane.
/// The magnitude carrier's unit shape normally supplies possible lanes; this
/// structure describes where this value actually sits.
/// </summary>
public static class BoundaryFlowValueSurface
{
    /// <summary>
    /// Creates a value with an explicit basis witness. Prefer
    /// <see cref="CreateOccupied(Carrier,Carrier,ulong,BoundaryFlowLane)"/> for
    /// ordinary carrier-backed values whose possible lanes live on UnitShape.
    /// </summary>
    public static Relation Create(
        Carrier root,
        Carrier valueCarrier,
        ulong magnitude,
        BoundaryFlowLane lane,
        IElement laneBasis,
        IElement residueOrTension)
    {
        var carrier = valueCarrier ?? Carrier.Unknown;
        var point = Relation.Point(carrier, checked((long)magnitude));
        return Create(root, point, lane, laneBasis, residueOrTension);
    }

    public static Relation Create(
        Carrier root,
        Carrier valueCarrier,
        ulong magnitude,
        BoundaryFlowLane lane,
        IElement laneBasis) =>
        Create(root, valueCarrier, magnitude, lane, laneBasis, ConceptSurface.UnknownPoint());

    /// <summary>
    /// Creates the normal occupied value shape: magnitude plus occupied lane,
    /// with possible lanes resolved from the magnitude carrier's unit shape.
    /// </summary>
    public static Relation CreateOccupied(
        Carrier root,
        Carrier valueCarrier,
        ulong magnitude,
        BoundaryFlowLane lane,
        IElement residueOrTension)
    {
        var carrier = valueCarrier ?? Carrier.Unknown;
        var point = Relation.Point(carrier, checked((long)magnitude));
        return CreateOccupied(root, point, lane, residueOrTension);
    }

    public static Relation CreateOccupied(
        Carrier root,
        Carrier valueCarrier,
        ulong magnitude,
        BoundaryFlowLane lane) =>
        CreateOccupied(root, valueCarrier, magnitude, lane, ConceptSurface.UnknownPoint());

    public static Relation CreateOccupied(
        Carrier root,
        Relation magnitudePoint,
        BoundaryFlowLane lane,
        IElement residueOrTension)
    {
        var marker = BoundaryFlowLaneSurface.Create(new Carrier(), lane);
        return Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            magnitudePoint ?? ConceptSurface.UnknownPoint(),
            marker,
            ConceptSurface.UnknownPoint(),
            residueOrTension ?? ConceptSurface.UnknownPoint());
    }

    /// <summary>
    /// Creates a value with an explicit basis witness or route/target context.
    /// This is not the normal home for possible lanes.
    /// </summary>
    public static Relation Create(
        Carrier root,
        Relation magnitudePoint,
        BoundaryFlowLane lane,
        IElement laneBasis,
        IElement residueOrTension)
    {
        var marker = BoundaryFlowLaneSurface.Create(
            new Carrier(),
            lane,
            laneBasis ?? ConceptSurface.UnknownPoint(),
            laneBasis ?? ConceptSurface.UnknownPoint());

        return Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            magnitudePoint ?? ConceptSurface.UnknownPoint(),
            marker,
            laneBasis ?? ConceptSurface.UnknownPoint(),
            residueOrTension ?? ConceptSurface.UnknownPoint());
    }

    public static bool TryRead(
        Relation valueSurface,
        out CarrierLocation magnitude,
        out BoundaryFlowLane lane,
        out Relation laneMarker,
        out IElement laneBasis,
        out IElement residueOrTension)
    {
        if (ConceptSurfaceReader.TryReadRootElements(valueSurface, out var elements) &&
            elements.Length >= 4 &&
            ConceptSurfaceReader.TryReadPoint(elements[0], out var magnitudeRead) &&
            elements[1] is Relation laneSurface &&
            BoundaryFlowLaneSurface.TryRead(laneSurface, out var laneRead, out _, out _))
        {
            magnitude = magnitudeRead;
            lane = laneRead;
            laneMarker = laneSurface;
            laneBasis = elements[2];
            residueOrTension = elements[3];
            return true;
        }

        magnitude = default;
        lane = default;
        laneMarker = null!;
        laneBasis = default!;
        residueOrTension = default!;
        return false;
    }

    public static bool TryReadOccupation(
        Relation valueSurface,
        Relation laneBasis,
        out CarrierLocation magnitude,
        out BoundaryFlowLane lane,
        out bool isLegal)
    {
        if (!TryRead(valueSurface, out magnitude, out lane, out _, out _, out _) ||
            !BoundaryFlowLaneBasis.TryReadLegalLanes(laneBasis, out _, out var lanes))
        {
            isLegal = false;
            return false;
        }

        isLegal = lanes.Contains(lane);
        return true;
    }

    public static bool TryReadValue(
        Relation valueSurface,
        out BoundaryFlowValueRead read)
    {
        if (TryReadValueWithBasis(valueSurface, out var basisRead))
        {
            read = basisRead.ToCompactRead();
            return true;
        }

        read = default;
        return false;
    }

    /// <summary>
    /// Reads a value through possible lanes resolved from the carrier unit-shape
    /// descriptor, then explicit basis witness, then the unit-shape default.
    /// </summary>
    public static bool TryReadValue(
        Relation valueSurface,
        Relation laneBasis,
        out BoundaryFlowValueRead read)
    {
        if (TryReadValueWithBasis(valueSurface, laneBasis, out var basisRead))
        {
            read = basisRead.ToCompactRead();
            return true;
        }

        read = default;
        return false;
    }

    public static bool TryReadValueWithBasis(
        Relation valueSurface,
        out BoundaryFlowValueBasisRead read)
    {
        if (!TryRead(valueSurface, out var magnitude, out var lane, out _, out var basisElement, out var residueOrTension) ||
            !TryResolveLaneBasis(magnitude, basisElement, out var laneBasis, out var exposure, out var lanes))
        {
            read = default;
            return false;
        }

        read = new BoundaryFlowValueBasisRead(
            magnitude,
            lane,
            laneBasis,
            exposure,
            lanes,
            lanes.Contains(lane),
            residueOrTension);
        return true;
    }

    public static bool TryReadValueWithBasis(
        Relation valueSurface,
        Relation laneBasis,
        out BoundaryFlowValueBasisRead read)
    {
        if (!TryRead(valueSurface, out var magnitude, out var lane, out _, out _, out var residueOrTension) ||
            !BoundaryFlowLaneBasis.TryReadLegalLanes(laneBasis, out var exposure, out var lanes))
        {
            read = default;
            return false;
        }

        read = new BoundaryFlowValueBasisRead(
            magnitude,
            lane,
            laneBasis,
            exposure,
            lanes,
            lanes.Contains(lane),
            residueOrTension);
        return true;
    }

    public static bool TryCreateInferred(
        Carrier root,
        Relation magnitudePoint,
        Relation laneBasis,
        out Relation valueSurface)
    {
        if (!ConceptSurfaceReader.TryReadPoint(magnitudePoint, out _) ||
            !BoundaryFlowLaneBasis.TryInferSingleLane(laneBasis, out var lane))
        {
            valueSurface = null!;
            return false;
        }

        valueSurface = Create(
            ConceptSurface.RootOrNew(root),
            magnitudePoint,
            lane,
            laneBasis,
            ConceptSurface.UnknownPoint());
        return true;
    }

    public static bool TryCreateFromCarrierUnitShape(
        Carrier root,
        Carrier valueCarrier,
        ulong magnitude,
        BoundaryFlowLane lane,
        out Relation valueSurface)
    {
        if (valueCarrier is null ||
            !BoundaryFlowLaneBasis.TryReadUnitShapeBasis(
                valueCarrier.UnitShape,
                out _,
                out _,
                out _,
                out var lanes) ||
            !lanes.Contains(lane))
        {
            valueSurface = null!;
            return false;
        }

        valueSurface = CreateOccupied(
            ConceptSurface.RootOrNew(root),
            valueCarrier,
            magnitude,
            lane);
        return true;
    }

    private static bool TryResolveLaneBasis(
        CarrierLocation magnitude,
        IElement explicitBasis,
        out Relation laneBasis,
        out BoundaryFlowExposure exposure,
        out BoundaryFlowLane[] lanes)
    {
        if (BoundaryFlowLaneBasis.TryReadUnitShapeDescriptorBasis(
                magnitude.Carrier.UnitShape,
                out laneBasis,
                out exposure,
                out _,
                out lanes))
        {
            return true;
        }

        if (explicitBasis is Relation basisRelation &&
            BoundaryFlowLaneBasis.TryReadLegalLanes(basisRelation, out exposure, out lanes))
        {
            laneBasis = basisRelation;
            return true;
        }

        if (BoundaryFlowLaneBasis.TryReadUnitShapeBasis(
                magnitude.Carrier.UnitShape,
                out laneBasis,
                out exposure,
                out _,
                out lanes))
        {
            return true;
        }

        laneBasis = null!;
        exposure = default;
        lanes = [];
        return false;
    }
}

/// <summary>
/// Compact read of a value surface through its resolved possible-lane basis.
/// The raw magnitude and occupied lane remain visible in the source structure.
/// </summary>
public readonly record struct BoundaryFlowValueRead(
    CarrierLocation Magnitude,
    BoundaryFlowLane Lane,
    BoundaryFlowExposure Basis,
    bool IsLegal,
    IElement ResidueOrTension)
{
    public bool IsUnsignedMagnitude => Magnitude.Ticks >= 0;

    public bool IsSignedBoundary => Lane.Boundary.IsPositive || Lane.Boundary.IsNegative;

    public bool IsWholeBoundary => Lane.Boundary.IsWhole;

    public bool IsNoFlow => Lane.Flow.IsNone;

    public bool IsForwardFlow => Lane.Flow.IsForward;

    public bool IsInverseFlow => Lane.Flow.IsInverse;

    public long SignedTicks => Lane.Boundary.IsNegative
        ? checked(-Magnitude.Ticks)
        : Magnitude.Ticks;

    public string Notation
    {
        get
        {
            var sign = Lane.Boundary.IsNegative ? "-" : string.Empty;
            var suffix = Lane.Flow.IsInverse
                ? "i"
                : Lane.Flow.IsForward
                    ? "f"
                    : Lane.Boundary.IsWhole
                        ? "s"
                        : string.Empty;

            return $"{sign}{Magnitude.Ticks}{suffix}";
        }
    }
}

/// <summary>
/// Structural read of a value surface through its resolved possible-lane basis.
/// This keeps the physical basis surface and visible legal lanes available
/// alongside the compact exposure read.
/// </summary>
public readonly record struct BoundaryFlowValueBasisRead(
    CarrierLocation Magnitude,
    BoundaryFlowLane Lane,
    Relation LaneBasis,
    BoundaryFlowExposure Basis,
    BoundaryFlowLane[] LegalLanes,
    bool IsLegal,
    IElement ResidueOrTension)
{
    public BoundaryFlowValueRead ToCompactRead() =>
        new(Magnitude, Lane, Basis, IsLegal, ResidueOrTension);

    public bool IsUnsignedMagnitude => ToCompactRead().IsUnsignedMagnitude;

    public bool IsSignedBoundary => ToCompactRead().IsSignedBoundary;

    public bool IsWholeBoundary => ToCompactRead().IsWholeBoundary;

    public bool IsNoFlow => ToCompactRead().IsNoFlow;

    public bool IsForwardFlow => ToCompactRead().IsForwardFlow;

    public bool IsInverseFlow => ToCompactRead().IsInverseFlow;

    public long SignedTicks => ToCompactRead().SignedTicks;

    public string Notation => ToCompactRead().Notation;
}

/// <summary>
/// Small lane-settlement experiment. It applies a signed delta to the visible
/// value read without changing core carrier settlement.
/// </summary>
public static class BoundaryFlowValueSettlement
{
    public static bool TryApplySignedDelta(
        Relation valueSurface,
        long deltaTicks,
        out Relation settledValue)
    {
        if (!BoundaryFlowValueSurface.TryReadValueWithBasis(valueSurface, out var basisRead))
        {
            settledValue = null!;
            return false;
        }

        return TryApplySignedDelta(valueSurface, basisRead.LaneBasis, deltaTicks, out settledValue);
    }

    public static bool TryApplySignedDelta(
        Relation valueSurface,
        Relation laneBasis,
        long deltaTicks,
        out Relation settledValue)
    {
        if (!BoundaryFlowValueSurface.TryReadValue(valueSurface, laneBasis, out var read) ||
            !read.IsUnsignedMagnitude ||
            !read.IsSignedBoundary)
        {
            settledValue = null!;
            return false;
        }

        var target = checked(read.SignedTicks + deltaTicks);
        var targetLane = new BoundaryFlowLane(
            target < 0
                ? BoundaryLane.Negative
                : target > 0
                    ? BoundaryLane.Positive
                    : read.Lane.Boundary,
            read.Lane.Flow);
        var targetMagnitude = AbsAsLong(target);

        if (BoundaryFlowLaneBasis.Allows(laneBasis, targetLane) &&
            CanMoveToLane(read.Magnitude.Carrier.UnitShape, laneBasis, read.Lane, targetLane))
        {
            settledValue = BoundaryFlowValueSurface.Create(
                new Carrier(),
                Relation.Point(read.Magnitude.Carrier, targetMagnitude),
                targetLane,
                laneBasis,
                Relation.Point(read.Magnitude.Carrier, 0));
            return true;
        }

        var heldLane = new BoundaryFlowLane(read.Lane.Boundary, read.Lane.Flow);
        settledValue = BoundaryFlowValueSurface.Create(
            new Carrier(),
            Relation.Point(read.Magnitude.Carrier, 0),
            heldLane,
            laneBasis,
            Relation.Point(read.Magnitude.Carrier, target));
        return true;
    }

    private static bool CanMoveToLane(
        UnitShape unitShape,
        Relation laneBasis,
        BoundaryFlowLane source,
        BoundaryFlowLane target) =>
        source.Equals(target) ||
        BoundaryFlowLaneTransitionSurface.TryFindUnitShapeTransition(unitShape, source, target, out _) ||
        BoundaryFlowLaneTransitionSurface.TryFindTransition(laneBasis, source, target, out _);

    private static long AbsAsLong(long value)
    {
        if (value == long.MinValue)
        {
            return long.MaxValue;
        }

        return Math.Abs(value);
    }
}

/// <summary>
/// Lane-aware route projection between value surfaces. This is a route-adjacent
/// experiment: the projection relation moves the signed/whole location, while
/// the target lane basis decides whether the resulting occupied lane is legal.
/// </summary>
public static class BoundaryFlowValueRoute
{
    public static Relation Create(
        Carrier root,
        Relation sourceValue,
        Relation projectionRoute,
        Relation targetBasis,
        Relation targetValue,
        IElement residueOrTension)
    {
        return Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            sourceValue ?? ConceptSurface.UnknownPoint(),
            projectionRoute ?? ConceptSurface.UnknownPoint(),
            targetBasis ?? ConceptSurface.UnknownPoint(),
            targetValue ?? ConceptSurface.UnknownPoint(),
            residueOrTension ?? ConceptSurface.UnknownPoint());
    }

    public static bool TryProject(
        Relation sourceValue,
        Relation projectionRoute,
        Carrier targetCarrier,
        out Relation routeSurface)
    {
        if (targetCarrier is null ||
            !BoundaryFlowLaneBasis.TryReadUnitShapeBasis(
                targetCarrier.UnitShape,
                out var targetBasis,
                out _,
                out _,
                out _))
        {
            routeSurface = null!;
            return false;
        }

        return TryProject(sourceValue, projectionRoute, targetBasis, targetCarrier, out routeSurface);
    }

    public static bool TryProject(
        Relation sourceValue,
        Relation projectionRoute,
        Relation targetBasis,
        Carrier targetCarrier,
        out Relation routeSurface)
    {
        if (!BoundaryFlowValueSurface.TryReadValue(sourceValue, out var sourceRead) ||
            !sourceRead.IsLegal ||
            projectionRoute is null ||
            targetBasis is null ||
            targetCarrier is null ||
            targetCarrier.IsUnknown ||
            !projectionRoute.TryProject(ProjectedSourceLocation(sourceRead), targetCarrier, out var projected) ||
            !projected.TryGetCarrierLocations(out var targetLocation, out var projectionResidue))
        {
            routeSurface = null!;
            return false;
        }

        var targetLane = ProjectedTargetLane(sourceRead, targetLocation, targetBasis);
        var isAllowed = BoundaryFlowLaneBasis.Allows(targetBasis, targetLane);
        var residueOrTension = isAllowed
            ? Relation.Point(projectionResidue.Carrier, projectionResidue.Ticks)
            : Relation.Point(targetLocation.Carrier, targetLocation.Ticks);
        var targetValue = BoundaryFlowValueSurface.Create(
            new Carrier(),
            Relation.Point(targetLocation.Carrier, AbsAsLong(targetLocation.Ticks)),
            targetLane,
            targetBasis,
            residueOrTension);

        routeSurface = Create(
            new Carrier(),
            sourceValue,
            projectionRoute,
            targetBasis,
            targetValue,
            residueOrTension);
        return true;
    }

    public static bool TryRead(
        Relation routeSurface,
        out Relation sourceValue,
        out Relation projectionRoute,
        out Relation targetBasis,
        out Relation targetValue,
        out IElement residueOrTension)
    {
        if (ConceptSurfaceReader.TryReadRootElements(routeSurface, out var elements) &&
            elements.Length >= 5 &&
            elements[0] is Relation source &&
            elements[1] is Relation projection &&
            elements[2] is Relation basis &&
            elements[3] is Relation target)
        {
            sourceValue = source;
            projectionRoute = projection;
            targetBasis = basis;
            targetValue = target;
            residueOrTension = elements[4];
            return true;
        }

        sourceValue = null!;
        projectionRoute = null!;
        targetBasis = null!;
        targetValue = null!;
        residueOrTension = default!;
        return false;
    }

    public static bool TryReadTargetValue(
        Relation routeSurface,
        out BoundaryFlowValueRead targetRead)
    {
        if (TryRead(
                routeSurface,
                out _,
                out _,
                out var targetBasis,
                out var targetValue,
                out _))
        {
            return BoundaryFlowValueSurface.TryReadValue(targetValue, targetBasis, out targetRead);
        }

        targetRead = default;
        return false;
    }

    private static CarrierLocation ProjectedSourceLocation(BoundaryFlowValueRead sourceRead) =>
        sourceRead.IsWholeBoundary
            ? sourceRead.Magnitude
            : new CarrierLocation(sourceRead.Magnitude.Carrier, sourceRead.SignedTicks);

    private static BoundaryFlowLane ProjectedTargetLane(
        BoundaryFlowValueRead sourceRead,
        CarrierLocation targetLocation,
        Relation targetBasis)
    {
        var boundary = sourceRead.IsWholeBoundary
            ? BoundaryLane.Whole
            : targetLocation.Ticks < 0
                ? BoundaryLane.Negative
                : targetLocation.Ticks > 0
                    ? BoundaryLane.Positive
                    : ZeroBoundaryLane(sourceRead.Lane.Boundary, targetBasis);
        return new BoundaryFlowLane(boundary, sourceRead.Lane.Flow);
    }

    private static BoundaryLane ZeroBoundaryLane(BoundaryLane sourceBoundary, Relation targetBasis)
    {
        if (BoundaryFlowLaneBasis.TryReadLegalLanes(targetBasis, out _, out var lanes) &&
            lanes.Select(lane => lane.Boundary).Distinct().Contains(sourceBoundary))
        {
            return sourceBoundary;
        }

        if (BoundaryFlowLaneBasis.TryReadLegalLanes(targetBasis, out _, out lanes) &&
            lanes.Select(lane => lane.Boundary).Distinct().ToArray() is [var only])
        {
            return only;
        }

        return BoundaryLane.Positive;
    }

    private static long AbsAsLong(long value)
    {
        if (value == long.MinValue)
        {
            return long.MaxValue;
        }

        return Math.Abs(value);
    }
}

/// <summary>
/// Accepts or blocks a lane-aware projected route value. The source route
/// remains visible; this surface adds accepted value, blocked value, and the
/// capacity or gate witness used by the local read.
/// </summary>
public static class BoundaryFlowValueTransfer
{
    public static Relation Create(
        Carrier root,
        Relation routeSurface,
        IElement acceptedValue,
        IElement blockedValue,
        IElement capacityOrGate)
    {
        return Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            routeSurface ?? ConceptSurface.UnknownPoint(),
            acceptedValue ?? ConceptSurface.UnknownPoint(),
            blockedValue ?? ConceptSurface.UnknownPoint(),
            capacityOrGate ?? ConceptSurface.UnknownPoint());
    }

    public static bool TryAccept(
        Relation routeSurface,
        out Relation transferSurface) =>
        TryAccept(routeSurface, null, out transferSurface);

    public static bool TryAccept(
        Relation routeSurface,
        Relation? capacity,
        out Relation transferSurface)
    {
        if (!BoundaryFlowValueRoute.TryRead(
                routeSurface,
                out _,
                out _,
                out var targetBasis,
                out var projectedValue,
                out _) ||
            !BoundaryFlowValueSurface.TryReadValue(projectedValue, targetBasis, out var projectedRead) ||
            !projectedRead.IsUnsignedMagnitude)
        {
            transferSurface = null!;
            return false;
        }

        var capacityTicks = ReadCapacity(capacity, projectedRead.Magnitude.Ticks);
        if (!projectedRead.IsLegal)
        {
            var blocked = CreateTransferValue(
                projectedRead,
                projectedRead.Magnitude.Ticks,
                SignedResidueTicks(projectedRead.Lane, projectedRead.Magnitude.Ticks),
                targetBasis);
            transferSurface = Create(
                new Carrier(),
                routeSurface,
                ConceptSurface.UnknownPoint(),
                blocked,
                capacity ?? ConceptSurface.UnknownPoint());
            return true;
        }

        var acceptedTicks = Math.Min(projectedRead.Magnitude.Ticks, capacityTicks);
        var blockedTicks = checked(projectedRead.Magnitude.Ticks - acceptedTicks);
        var accepted = CreateTransferValue(projectedRead, acceptedTicks, 0, targetBasis);
        var blockedValue = CreateTransferValue(
            projectedRead,
            blockedTicks,
            SignedResidueTicks(projectedRead.Lane, blockedTicks),
            targetBasis);

        transferSurface = Create(
            new Carrier(),
            routeSurface,
            accepted,
            blockedValue,
            capacity ?? ConceptSurface.UnknownPoint());
        return true;
    }

    public static bool TryRead(
        Relation transferSurface,
        out Relation routeSurface,
        out IElement acceptedValue,
        out IElement blockedValue,
        out IElement capacityOrGate)
    {
        if (ConceptSurfaceReader.TryReadRootElements(transferSurface, out var elements) &&
            elements.Length >= 4 &&
            elements[0] is Relation route)
        {
            routeSurface = route;
            acceptedValue = elements[1];
            blockedValue = elements[2];
            capacityOrGate = elements[3];
            return true;
        }

        routeSurface = null!;
        acceptedValue = default!;
        blockedValue = default!;
        capacityOrGate = default!;
        return false;
    }

    public static bool TryReadAcceptedValue(
        Relation transferSurface,
        out BoundaryFlowValueRead acceptedRead)
    {
        if (TryRead(transferSurface, out _, out var accepted, out _, out _) &&
            accepted is Relation acceptedRelation)
        {
            return BoundaryFlowValueSurface.TryReadValue(acceptedRelation, out acceptedRead);
        }

        acceptedRead = default;
        return false;
    }

    public static bool TryReadBlockedValue(
        Relation transferSurface,
        out BoundaryFlowValueRead blockedRead)
    {
        if (TryRead(transferSurface, out _, out _, out var blocked, out _) &&
            blocked is Relation blockedRelation)
        {
            return BoundaryFlowValueSurface.TryReadValue(blockedRelation, out blockedRead);
        }

        blockedRead = default;
        return false;
    }

    public static bool TryReadSummary(
        Relation transferSurface,
        out BoundaryFlowTransferRead read)
    {
        var hasAccepted = TryReadAcceptedValue(transferSurface, out var accepted);
        if (!TryReadBlockedValue(transferSurface, out var blocked))
        {
            read = default;
            return false;
        }

        read = new BoundaryFlowTransferRead(
            hasAccepted ? accepted : default,
            blocked,
            hasAccepted);
        return true;
    }

    private static Relation CreateTransferValue(
        BoundaryFlowValueRead projectedRead,
        long magnitudeTicks,
        long residueTicks,
        Relation targetBasis) =>
        BoundaryFlowValueSurface.Create(
            new Carrier(),
            Relation.Point(projectedRead.Magnitude.Carrier, magnitudeTicks),
            projectedRead.Lane,
            targetBasis,
            Relation.Point(projectedRead.Magnitude.Carrier, residueTicks));

    private static long ReadCapacity(Relation? capacity, long fallback)
    {
        if (capacity is null ||
            !ConceptSurfaceReader.TryReadPoint(capacity, out var capacityLocation))
        {
            return fallback;
        }

        return Math.Max(0, capacityLocation.Ticks);
    }

    private static long SignedResidueTicks(BoundaryFlowLane lane, long magnitudeTicks) =>
        lane.Boundary.IsNegative ? checked(-magnitudeTicks) : magnitudeTicks;
}

public readonly record struct BoundaryFlowTransferRead(
    BoundaryFlowValueRead Accepted,
    BoundaryFlowValueRead Blocked,
    bool HasAccepted)
{
    public bool IsFullyAccepted =>
        HasAccepted &&
        Accepted.IsLegal &&
        Blocked.Magnitude.Ticks == 0;

    public bool IsPartiallyAccepted =>
        HasAccepted &&
        Accepted.Magnitude.Ticks > 0 &&
        Blocked.Magnitude.Ticks > 0;

    public bool IsFullyBlocked =>
        !HasAccepted ||
        Accepted.Magnitude.Ticks == 0 && Blocked.Magnitude.Ticks > 0;
}

/// <summary>
/// Attaches a capacity witness to a specific target site and lane basis. The
/// capacity value is still ordinary point structure; this surface makes the
/// target-local attachment inspectable.
/// </summary>
public static class BoundaryFlowTargetCapacitySurface
{
    public static Relation Create(
        Carrier root,
        IElement targetSite,
        Relation targetBasis,
        Relation capacityPoint,
        IElement partition,
        IElement overflowWitness)
    {
        var site = targetSite ?? targetBasis ?? ConceptSurface.UnknownPoint();
        var capacity = ConceptCapacity.Create(
            new Carrier(),
            site,
            capacityPoint ?? ConceptSurface.UnknownPoint(),
            partition ?? ConceptSurface.UnknownPoint(),
            overflowWitness ?? ConceptSurface.UnknownPoint());

        return Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            site,
            targetBasis ?? ConceptSurface.UnknownPoint(),
            capacity);
    }

    public static Relation Create(
        Carrier root,
        IElement targetSite,
        Relation targetBasis,
        Relation capacityPoint) =>
        Create(
            root,
            targetSite,
            targetBasis,
            capacityPoint,
            ConceptSurface.UnknownPoint(),
            ConceptSurface.UnknownPoint());

    public static bool TryCreate(
        Carrier root,
        Relation targetValue,
        long capacityTicks,
        out Relation targetCapacitySurface)
    {
        if (capacityTicks < 0 ||
            !BoundaryFlowValueSurface.TryReadValueWithBasis(
                targetValue,
                out var targetRead))
        {
            targetCapacitySurface = null!;
            return false;
        }

        targetCapacitySurface = Create(
            root,
            targetValue,
            targetRead.LaneBasis,
            Relation.Point(targetRead.Magnitude.Carrier, capacityTicks));
        return true;
    }

    public static bool TryCreatePartitioned(
        Carrier root,
        Relation targetValue,
        long capacityTicks,
        long partitionCount,
        IElement selectedPartition,
        out Relation targetCapacitySurface)
    {
        if (capacityTicks < 0 ||
            !BoundaryFlowValueSurface.TryReadValueWithBasis(
                targetValue,
                out var targetRead))
        {
            targetCapacitySurface = null!;
            return false;
        }

        var capacityPoint = Relation.Point(targetRead.Magnitude.Carrier, capacityTicks);
        var partition = ConceptPartition.Create(
            new Carrier(),
            capacityPoint,
            new Carrier(),
            partitionCount,
            selectedPartition ?? ConceptSurface.UnknownPoint());
        targetCapacitySurface = Create(
            root,
            targetValue,
            targetRead.LaneBasis,
            capacityPoint,
            partition,
            ConceptSurface.UnknownPoint());
        return true;
    }

    public static bool TryCreatePartitioned(
        Carrier root,
        Relation targetValue,
        long capacityTicks,
        long partitionCount,
        out Relation targetCapacitySurface) =>
        TryCreatePartitioned(
            root,
            targetValue,
            capacityTicks,
            partitionCount,
            ConceptSurface.UnknownPoint(),
            out targetCapacitySurface);

    public static bool TryRead(
        Relation surface,
        out IElement targetSite,
        out Relation targetBasis,
        out Relation capacitySurface,
        out Relation capacityPoint,
        out IElement partition,
        out IElement overflowWitness)
    {
        if (ConceptSurfaceReader.TryReadRootElements(surface, out var elements) &&
            elements.Length >= 3 &&
            elements[1] is Relation basis &&
            elements[2] is Relation capacity &&
            ConceptCapacity.TryRead(
                capacity,
                out _,
                out var wholeCapacity,
                out var partitionRead,
                out var overflowRead) &&
            wholeCapacity is Relation capacityRelation &&
            ConceptSurfaceReader.TryReadPoint(capacityRelation, out _))
        {
            targetSite = elements[0];
            targetBasis = basis;
            capacitySurface = capacity;
            capacityPoint = capacityRelation;
            partition = partitionRead;
            overflowWitness = overflowRead;
            return true;
        }

        targetSite = default!;
        targetBasis = null!;
        capacitySurface = null!;
        capacityPoint = null!;
        partition = default!;
        overflowWitness = default!;
        return false;
    }

    public static bool TryReadCapacity(
        Relation surface,
        out BoundaryFlowTargetCapacityRead read)
    {
        if (TryRead(
                surface,
                out _,
                out var targetBasis,
                out _,
                out var capacityPoint,
                out _,
                out _) &&
            ConceptSurfaceReader.TryReadPoint(capacityPoint, out var capacityLocation) &&
            BoundaryFlowLaneBasis.TryReadLegalLanes(targetBasis, out var exposure, out _) &&
            TryReadTargetCarrier(surface, out var targetCarrier))
        {
            read = new BoundaryFlowTargetCapacityRead(
                capacityLocation,
                targetCarrier,
                exposure);
            return true;
        }

        read = default;
        return false;
    }

    public static bool TryReadPartition(
        Relation surface,
        out Relation partition,
        out long partitionCount,
        out IElement selectedPartition)
    {
        if (TryRead(
                surface,
                out _,
                out _,
                out _,
                out _,
                out var partitionCandidate,
                out _) &&
            partitionCandidate is Relation partitionRelation &&
            ConceptPartition.TryRead(
                partitionRelation,
                out _,
                out _,
                out _,
                out var count,
                out var selected))
        {
            partition = partitionRelation;
            partitionCount = count.Ticks;
            selectedPartition = selected;
            return true;
        }

        partition = null!;
        partitionCount = 0;
        selectedPartition = default!;
        return false;
    }

    internal static bool TryReadTargetCarrier(Relation surface, out Carrier carrier)
    {
        if (!TryRead(
                surface,
                out var targetSite,
                out var targetBasis,
                out _,
                out var capacityPoint,
                out _,
                out _))
        {
            carrier = null!;
            return false;
        }

        if (targetSite is Relation targetValue &&
            BoundaryFlowValueSurface.TryReadValue(targetValue, targetBasis, out var targetRead))
        {
            carrier = targetRead.Magnitude.Carrier;
            return true;
        }

        if (CarrierAxisSurface.TryRead(targetBasis, out var axisCarrier, out _, out _, out _, out _))
        {
            carrier = axisCarrier;
            return true;
        }

        if (targetSite is Carrier selectedCarrier)
        {
            carrier = selectedCarrier;
            return true;
        }

        if (ConceptSurfaceReader.TryReadPoint(capacityPoint, out var capacityLocation))
        {
            carrier = capacityLocation.Carrier;
            return true;
        }

        carrier = null!;
        return false;
    }
}

public readonly record struct BoundaryFlowTargetCapacityRead(
    CarrierLocation Capacity,
    Carrier TargetCarrier,
    BoundaryFlowExposure TargetBasis);

/// <summary>
/// Reads usable capacity from a target capacity surface and the target's
/// current value. Whole capacity, occupied value, remaining capacity, and
/// overflow stay visible as ordinary connected structure.
/// </summary>
public static class BoundaryFlowRemainingCapacitySurface
{
    public static Relation Create(
        Carrier root,
        Relation targetCapacity,
        Relation targetValue,
        Relation remainingCapacity,
        Relation overflow,
        Relation effectiveCapacity,
        Relation partitionResidue)
    {
        return Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            targetCapacity ?? ConceptSurface.UnknownPoint(),
            targetValue ?? ConceptSurface.UnknownPoint(),
            remainingCapacity ?? ConceptSurface.UnknownPoint(),
            overflow ?? ConceptSurface.UnknownPoint(),
            effectiveCapacity ?? ConceptSurface.UnknownPoint(),
            partitionResidue ?? ConceptSurface.UnknownPoint());
    }

    public static Relation Create(
        Carrier root,
        Relation targetCapacity,
        Relation targetValue,
        Relation remainingCapacity,
        Relation overflow) =>
        Create(
            root,
            targetCapacity,
            targetValue,
            remainingCapacity,
            overflow,
            remainingCapacity,
            ConceptSurface.UnknownPoint());

    public static bool TryCreate(
        Carrier root,
        Relation targetCapacity,
        Relation targetValue,
        out Relation remainingCapacitySurface)
    {
        if (!BoundaryFlowTargetCapacitySurface.TryReadCapacity(targetCapacity, out var capacityRead) ||
            !BoundaryFlowValueSurface.TryReadValue(targetValue, out var targetRead) ||
            !targetRead.IsLegal ||
            !targetRead.IsUnsignedMagnitude ||
            !targetRead.Basis.Equals(capacityRead.TargetBasis) ||
            !targetRead.Magnitude.Carrier.SameIdentityAs(capacityRead.TargetCarrier))
        {
            remainingCapacitySurface = null!;
            return false;
        }

        var occupiedTicks = targetRead.Magnitude.Ticks;
        var effectiveCapacity = EffectiveCapacityOf(targetCapacity, capacityRead.Capacity);
        var remainingTicks = Math.Max(0, checked(effectiveCapacity.Capacity.Ticks - occupiedTicks));
        var overflowTicks = Math.Max(0, checked(occupiedTicks - effectiveCapacity.Capacity.Ticks));
        remainingCapacitySurface = Create(
            root,
            targetCapacity,
            targetValue,
            Relation.Point(effectiveCapacity.Capacity.Carrier, remainingTicks),
            Relation.Point(effectiveCapacity.Capacity.Carrier, overflowTicks),
            Relation.Point(effectiveCapacity.Capacity.Carrier, effectiveCapacity.Capacity.Ticks),
            Relation.Point(effectiveCapacity.Residue.Carrier, effectiveCapacity.Residue.Ticks));
        return true;
    }

    public static bool TryRead(
        Relation surface,
        out Relation targetCapacity,
        out Relation targetValue,
        out Relation remainingCapacity,
        out Relation overflow)
    {
        if (ConceptSurfaceReader.TryReadRootElements(surface, out var elements) &&
            elements.Length >= 4 &&
            elements[0] is Relation capacity &&
            elements[1] is Relation value &&
            elements[2] is Relation remaining &&
            elements[3] is Relation overflowRelation &&
            BoundaryFlowTargetCapacitySurface.TryReadCapacity(capacity, out _) &&
            BoundaryFlowValueSurface.TryReadValue(value, out _) &&
            ConceptSurfaceReader.TryReadPoint(remaining, out _) &&
            ConceptSurfaceReader.TryReadPoint(overflowRelation, out _))
        {
            targetCapacity = capacity;
            targetValue = value;
            remainingCapacity = remaining;
            overflow = overflowRelation;
            return true;
        }

        targetCapacity = null!;
        targetValue = null!;
        remainingCapacity = null!;
        overflow = null!;
        return false;
    }

    public static bool TryReadEffectiveCapacity(
        Relation surface,
        out Relation effectiveCapacity,
        out Relation partitionResidue)
    {
        if (ConceptSurfaceReader.TryReadRootElements(surface, out var elements) &&
            elements.Length >= 6 &&
            elements[4] is Relation effective &&
            elements[5] is Relation residue &&
            ConceptSurfaceReader.TryReadPoint(effective, out _) &&
            ConceptSurfaceReader.TryReadPoint(residue, out _))
        {
            effectiveCapacity = effective;
            partitionResidue = residue;
            return true;
        }

        if (TryRead(
                surface,
                out var targetCapacity,
                out _,
                out _,
                out _) &&
            BoundaryFlowTargetCapacitySurface.TryReadCapacity(targetCapacity, out var capacityRead))
        {
            effectiveCapacity = Relation.Point(capacityRead.Capacity.Carrier, capacityRead.Capacity.Ticks);
            partitionResidue = Relation.Point(capacityRead.Capacity.Carrier, 0);
            return true;
        }

        effectiveCapacity = null!;
        partitionResidue = null!;
        return false;
    }

    public static bool TryReadCapacity(
        Relation surface,
        out BoundaryFlowRemainingCapacityRead read)
    {
        if (TryRead(
                surface,
                out var targetCapacity,
                out var targetValue,
                out var remainingCapacity,
                out var overflow) &&
            BoundaryFlowTargetCapacitySurface.TryReadCapacity(targetCapacity, out var capacityRead) &&
            BoundaryFlowValueSurface.TryReadValue(targetValue, out var targetRead) &&
            ConceptSurfaceReader.TryReadPoint(remainingCapacity, out var remainingLocation) &&
            ConceptSurfaceReader.TryReadPoint(overflow, out var overflowLocation))
        {
            var effective = TryReadEffectiveCapacity(
                    surface,
                    out var effectiveCapacity,
                    out var partitionResidue) &&
                ConceptSurfaceReader.TryReadPoint(effectiveCapacity, out var effectiveLocation) &&
                ConceptSurfaceReader.TryReadPoint(partitionResidue, out var residueLocation)
                    ? new EffectiveCapacityRead(effectiveLocation, residueLocation)
                    : EffectiveCapacityOf(targetCapacity, capacityRead.Capacity);

            read = new BoundaryFlowRemainingCapacityRead(
                capacityRead.Capacity,
                effective.Capacity,
                targetRead.Magnitude,
                remainingLocation,
                overflowLocation,
                effective.Residue,
                capacityRead.TargetCarrier,
                capacityRead.TargetBasis);
            return true;
        }

        read = default;
        return false;
    }

    private static EffectiveCapacityRead EffectiveCapacityOf(
        Relation targetCapacity,
        CarrierLocation wholeCapacity)
    {
        if (!BoundaryFlowTargetCapacitySurface.TryReadPartition(
                targetCapacity,
                out _,
                out var partitionCount,
                out var selectedPartition) ||
            partitionCount <= 0 ||
            !ConceptSurfaceReader.TryReadPoint(selectedPartition, out var selected))
        {
            return new EffectiveCapacityRead(
                wholeCapacity,
                new CarrierLocation(wholeCapacity.Carrier, 0));
        }

        var selectedTicks = Math.Clamp(selected.Ticks, 0, partitionCount);
        var scaled = checked(wholeCapacity.Ticks * selectedTicks);
        var effectiveTicks = scaled / partitionCount;
        var residueTicks = scaled % partitionCount;
        return new EffectiveCapacityRead(
            new CarrierLocation(wholeCapacity.Carrier, effectiveTicks),
            new CarrierLocation(selected.Carrier, residueTicks));
    }
}

public readonly record struct BoundaryFlowRemainingCapacityRead(
    CarrierLocation WholeCapacity,
    CarrierLocation EffectiveCapacity,
    CarrierLocation Occupied,
    CarrierLocation Remaining,
    CarrierLocation Overflow,
    CarrierLocation PartitionResidue,
    Carrier TargetCarrier,
    BoundaryFlowExposure TargetBasis);

internal readonly record struct EffectiveCapacityRead(
    CarrierLocation Capacity,
    CarrierLocation Residue);

/// <summary>
/// Accepts several projected routes against one shared capacity witness. Legal
/// projected lanes compete for capacity; illegal lanes bypass capacity and
/// remain blocked as lane residue.
/// </summary>
public static class BoundaryFlowSharedCapacityTransfer
{
    public static Relation Create(
        Carrier root,
        Relation capacity,
        Relation routes,
        Relation transfers,
        Relation residues,
        Relation unusedCapacity)
    {
        return Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            capacity ?? ConceptSurface.UnknownPoint(),
            routes ?? ConceptSurface.UnknownPoint(),
            transfers ?? ConceptSurface.UnknownPoint(),
            residues ?? ConceptSurface.UnknownPoint(),
            unusedCapacity ?? ConceptSurface.UnknownPoint());
    }

    public static bool TryAccept(
        Relation capacity,
        IReadOnlyList<Relation> routeSurfaces,
        out Relation sharedTransferSurface)
    {
        if (!TryReadCapacityWitness(
                capacity,
                out var capacityLocation,
                out var targetBasis,
                out var targetCarrier) ||
            capacityLocation.Ticks < 0)
        {
            sharedTransferSurface = null!;
            return false;
        }

        var routes = CleanRelations(routeSurfaces);
        var projectedReads = new BoundaryFlowValueRead[routes.Length];
        var demands = new long[routes.Length];
        for (var index = 0; index < routes.Length; index++)
        {
            if (targetBasis is not null &&
                !IsRouteLocalToTargetCapacity(routes[index], targetBasis, targetCarrier))
            {
                sharedTransferSurface = null!;
                return false;
            }

            if (!BoundaryFlowValueRoute.TryReadTargetValue(routes[index], out var projectedRead) ||
                !projectedRead.IsUnsignedMagnitude)
            {
                sharedTransferSurface = null!;
                return false;
            }

            projectedReads[index] = projectedRead;
            demands[index] = projectedRead.IsLegal ? projectedRead.Magnitude.Ticks : 0;
        }

        var accepted = AllocateSharedCapacity(demands, capacityLocation.Ticks);
        var transfers = new Relation[routes.Length];
        var residues = new IElement[routes.Length];
        var acceptedTotal = 0L;

        for (var index = 0; index < routes.Length; index++)
        {
            var routeCapacity = Relation.Point(projectedReads[index].Magnitude.Carrier, accepted[index]);
            if (!BoundaryFlowValueTransfer.TryAccept(routes[index], routeCapacity, out var transfer) ||
                !BoundaryFlowValueTransfer.TryRead(
                    transfer,
                    out _,
                    out _,
                    out var blocked,
                    out _))
            {
                sharedTransferSurface = null!;
                return false;
            }

            transfers[index] = transfer;
            residues[index] = blocked;
            acceptedTotal = checked(acceptedTotal + accepted[index]);
        }

        var unusedTicks = Math.Max(0, checked(capacityLocation.Ticks - acceptedTotal));
        sharedTransferSurface = Create(
            new Carrier(),
            capacity,
            Relation.OrderedGroup(new Carrier(), routes),
            Relation.OrderedGroup(new Carrier(), transfers),
            Relation.OrderedGroup(new Carrier(), residues),
            Relation.Point(capacityLocation.Carrier, unusedTicks));
        return true;
    }

    public static bool TryRead(
        Relation sharedTransferSurface,
        out Relation capacity,
        out Relation routes,
        out Relation transfers,
        out Relation residues,
        out Relation unusedCapacity)
    {
        if (ConceptSurfaceReader.TryReadRootElements(sharedTransferSurface, out var elements) &&
            elements.Length >= 5 &&
            elements[0] is Relation capacityRelation &&
            elements[1] is Relation routeGroup &&
            elements[2] is Relation transferGroup &&
            elements[3] is Relation residueGroup &&
            elements[4] is Relation unused)
        {
            capacity = capacityRelation;
            routes = routeGroup;
            transfers = transferGroup;
            residues = residueGroup;
            unusedCapacity = unused;
            return true;
        }

        capacity = null!;
        routes = null!;
        transfers = null!;
        residues = null!;
        unusedCapacity = null!;
        return false;
    }

    public static bool TryReadTransfers(
        Relation sharedTransferSurface,
        out Relation[] transfers)
    {
        if (TryRead(sharedTransferSurface, out _, out _, out var transferGroup, out _, out _) &&
            ConceptSurfaceReader.TryReadRootElements(transferGroup, out var elements))
        {
            transfers = [.. elements.OfType<Relation>()];
            return true;
        }

        transfers = [];
        return false;
    }

    public static bool TryReadSummaries(
        Relation sharedTransferSurface,
        out BoundaryFlowTransferRead[] summaries)
    {
        if (!TryReadTransfers(sharedTransferSurface, out var transfers))
        {
            summaries = [];
            return false;
        }

        var reads = new BoundaryFlowTransferRead[transfers.Length];
        for (var index = 0; index < transfers.Length; index++)
        {
            if (!BoundaryFlowValueTransfer.TryReadSummary(transfers[index], out reads[index]))
            {
                summaries = [];
                return false;
            }
        }

        summaries = reads;
        return true;
    }

    public static bool TryReadTotals(
        Relation sharedTransferSurface,
        out BoundaryFlowSharedCapacityRead read)
    {
        if (!TryRead(
                sharedTransferSurface,
                out var capacity,
                out _,
                out _,
                out _,
                out var unusedCapacity) ||
            !TryReadCapacityLocation(capacity, out var capacityLocation) ||
            !ConceptSurfaceReader.TryReadPoint(unusedCapacity, out var unusedLocation) ||
            !TryReadSummaries(sharedTransferSurface, out var summaries))
        {
            read = default;
            return false;
        }

        var accepted = 0L;
        var blocked = 0L;
        foreach (var summary in summaries)
        {
            if (summary.HasAccepted)
            {
                accepted = checked(accepted + summary.Accepted.Magnitude.Ticks);
            }

            blocked = checked(blocked + summary.Blocked.Magnitude.Ticks);
        }

        read = new BoundaryFlowSharedCapacityRead(
            capacityLocation.Ticks,
            accepted,
            blocked,
            unusedLocation.Ticks,
            summaries.Length);
        return true;
    }

    private static bool TryReadCapacityWitness(
        Relation capacity,
        out CarrierLocation capacityLocation,
        out Relation? targetBasis,
        out Carrier? targetCarrier)
    {
        if (BoundaryFlowRemainingCapacitySurface.TryReadCapacity(
                capacity,
                out var remainingRead))
        {
            capacityLocation = remainingRead.Remaining;
            targetBasis = BoundaryFlowRemainingCapacitySurface.TryRead(
                    capacity,
                    out var targetCapacity,
                    out _,
                    out _,
                    out _) &&
                BoundaryFlowTargetCapacitySurface.TryRead(
                    targetCapacity,
                    out _,
                    out var remainingBasis,
                    out _,
                    out _,
                    out _,
                    out _)
                    ? remainingBasis
                    : null;
            targetCarrier = remainingRead.TargetCarrier;
            return true;
        }

        if (ConceptSurfaceReader.TryReadPoint(capacity, out capacityLocation))
        {
            targetBasis = null;
            targetCarrier = null;
            return true;
        }

        if (BoundaryFlowTargetCapacitySurface.TryRead(
                capacity,
                out _,
                out var basis,
                out _,
                out var capacityPoint,
                out _,
                out _) &&
            ConceptSurfaceReader.TryReadPoint(capacityPoint, out capacityLocation))
        {
            targetBasis = basis;
            targetCarrier = BoundaryFlowTargetCapacitySurface.TryReadTargetCarrier(
                capacity,
                out var carrier)
                ? carrier
                : null;
            return true;
        }

        capacityLocation = default;
        targetBasis = null;
        targetCarrier = null;
        return false;
    }

    private static bool TryReadCapacityLocation(
        Relation capacity,
        out CarrierLocation capacityLocation)
    {
        if (BoundaryFlowRemainingCapacitySurface.TryReadCapacity(
                capacity,
                out var remainingRead))
        {
            capacityLocation = remainingRead.Remaining;
            return true;
        }

        if (ConceptSurfaceReader.TryReadPoint(capacity, out capacityLocation))
        {
            return true;
        }

        if (BoundaryFlowTargetCapacitySurface.TryRead(
                capacity,
                out _,
                out _,
                out _,
                out var capacityPoint,
                out _,
                out _) &&
            ConceptSurfaceReader.TryReadPoint(capacityPoint, out capacityLocation))
        {
            return true;
        }

        capacityLocation = default;
        return false;
    }

    private static bool IsRouteLocalToTargetCapacity(
        Relation routeSurface,
        Relation targetBasis,
        Carrier? targetCarrier)
    {
        if (!BoundaryFlowValueRoute.TryRead(
                routeSurface,
                out _,
                out _,
                out var routeTargetBasis,
                out var routeTargetValue,
                out _) ||
            !BoundaryFlowLaneBasis.TryReadLegalLanes(targetBasis, out var targetExposure, out _) ||
            !BoundaryFlowLaneBasis.TryReadLegalLanes(routeTargetBasis, out var routeExposure, out _) ||
            !targetExposure.Equals(routeExposure))
        {
            return false;
        }

        if (targetCarrier is null || targetCarrier.IsUnknown)
        {
            return true;
        }

        return BoundaryFlowValueSurface.TryReadValue(
                routeTargetValue,
                routeTargetBasis,
                out var routeTargetRead) &&
            routeTargetRead.Magnitude.Carrier.SameIdentityAs(targetCarrier);
    }

    private static long[] AllocateSharedCapacity(IReadOnlyList<long> demands, long capacity)
    {
        var accepted = new long[demands.Count];
        var remaining = Math.Max(0, capacity);
        var active = Enumerable.Range(0, demands.Count)
            .Where(index => demands[index] > 0)
            .ToList();

        while (remaining > 0 && active.Count != 0)
        {
            var count = active.Count;
            var baseShare = remaining / count;
            var extra = remaining % count;
            var moved = 0L;

            for (var order = 0; order < active.Count; order++)
            {
                var index = active[order];
                var demandLeft = demands[index] - accepted[index];
                var offer = baseShare + (order < extra ? 1 : 0);
                var acceptedTicks = Math.Min(demandLeft, offer);
                accepted[index] = checked(accepted[index] + acceptedTicks);
                moved = checked(moved + acceptedTicks);
            }

            if (moved == 0)
            {
                break;
            }

            remaining = checked(remaining - moved);
            active.RemoveAll(index => accepted[index] >= demands[index]);
        }

        return accepted;
    }

    private static Relation[] CleanRelations(IReadOnlyList<Relation> relations)
    {
        if (relations is null || relations.Count == 0)
        {
            return [];
        }

        return [.. relations.Where(relation => relation is not null)];
    }
}

public readonly record struct BoundaryFlowSharedCapacityRead(
    long CapacityMagnitude,
    long AcceptedMagnitude,
    long BlockedMagnitude,
    long UnusedCapacity,
    int TransferCount);

/// <summary>
/// Applies an accepted transfer to an existing target value and records the
/// transition as a time-step witness. This is still a concept-surface
/// settlement prototype; it does not mutate carrier state.
/// </summary>
public static class BoundaryFlowTargetSettlement
{
    public static Relation Create(
        Carrier root,
        Relation targetBefore,
        Relation transferSurface,
        Relation targetAfter,
        IElement residue,
        Relation timeline)
    {
        return Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            targetBefore ?? ConceptSurface.UnknownPoint(),
            transferSurface ?? ConceptSurface.UnknownPoint(),
            targetAfter ?? ConceptSurface.UnknownPoint(),
            residue ?? ConceptSurface.UnknownPoint(),
            timeline ?? ConceptSurface.UnknownPoint());
    }

    public static bool TrySettle(
        Relation targetBefore,
        Relation transferSurface,
        out Relation settlementSurface)
    {
        if (!BoundaryFlowValueSurface.TryReadValueWithBasis(
                targetBefore,
                out var beforeRead) ||
            !BoundaryFlowValueTransfer.TryReadSummary(transferSurface, out var transferRead))
        {
            settlementSurface = null!;
            return false;
        }

        var residue = transferRead.Blocked.Magnitude.Ticks == 0
            ? transferRead.Accepted.ResidueOrTension
            : BlockedResidue(transferSurface);
        var targetAfter = targetBefore;

        if (transferRead.HasAccepted &&
            transferRead.Accepted.Magnitude.Ticks != 0)
        {
            if (!CanApplyAcceptedToTarget(targetBefore, transferRead.Accepted, out var deltaTicks) ||
                !BoundaryFlowValueSettlement.TryApplySignedDelta(
                    targetBefore,
                    beforeRead.LaneBasis,
                    deltaTicks,
                    out targetAfter))
            {
                targetAfter = targetBefore;
                residue = transferRead.Accepted.ResidueOrTension;
            }
        }

        var time = new Carrier();
        var step = TimeStepSequence.Step(
            new Carrier(),
            time,
            0,
            1,
            targetBefore,
            transferSurface,
            targetAfter,
            residue);
        var timeline = TimeStepSequence.Create(new Carrier(), time, targetBefore, step);

        settlementSurface = Create(
            new Carrier(),
            targetBefore,
            transferSurface,
            targetAfter,
            residue,
            timeline);
        return true;
    }

    public static bool TryRead(
        Relation settlementSurface,
        out Relation targetBefore,
        out Relation transferSurface,
        out Relation targetAfter,
        out IElement residue,
        out Relation timeline)
    {
        if (ConceptSurfaceReader.TryReadRootElements(settlementSurface, out var elements) &&
            elements.Length >= 5 &&
            elements[0] is Relation before &&
            elements[1] is Relation transfer &&
            elements[2] is Relation after &&
            elements[4] is Relation time)
        {
            targetBefore = before;
            transferSurface = transfer;
            targetAfter = after;
            residue = elements[3];
            timeline = time;
            return true;
        }

        targetBefore = null!;
        transferSurface = null!;
        targetAfter = null!;
        residue = default!;
        timeline = null!;
        return false;
    }

    public static bool TryReadAfter(
        Relation settlementSurface,
        out BoundaryFlowValueRead afterRead)
    {
        if (TryRead(settlementSurface, out _, out _, out var targetAfter, out _, out _))
        {
            return BoundaryFlowValueSurface.TryReadValue(targetAfter, out afterRead);
        }

        afterRead = default;
        return false;
    }

    public static bool TryReadTimeline(Relation settlementSurface, out Relation timeline)
    {
        if (TryRead(settlementSurface, out _, out _, out _, out _, out var candidate) &&
            TimeStepSequence.TryRead(candidate, out _, out _, out _))
        {
            timeline = candidate;
            return true;
        }

        timeline = null!;
        return false;
    }

    private static bool CanApplyAcceptedToTarget(
        Relation targetBefore,
        BoundaryFlowValueRead accepted,
        out long deltaTicks)
    {
        if (!BoundaryFlowValueSurface.TryReadValue(targetBefore, out var before) ||
            !before.IsLegal ||
            !accepted.IsLegal ||
            !before.IsSignedBoundary ||
            !accepted.IsSignedBoundary ||
            before.Lane.Flow != accepted.Lane.Flow)
        {
            deltaTicks = 0;
            return false;
        }

        deltaTicks = accepted.SignedTicks;
        return true;
    }

    private static IElement BlockedResidue(Relation transferSurface)
    {
        if (BoundaryFlowValueTransfer.TryRead(
                transferSurface,
                out _,
                out _,
                out var blocked,
                out _))
        {
            return blocked;
        }

        return ConceptSurface.UnknownPoint();
    }
}

/// <summary>
/// Chains several target settlements into one visible run. The helper builds a
/// fresh combined timeline from the same before/transfer/after/residue evidence
/// already present in each local settlement step.
/// </summary>
public static class BoundaryFlowTargetSettlementRun
{
    public static Relation Create(
        Carrier root,
        Relation initialTarget,
        Relation finalTarget,
        Relation transfers,
        Relation residues,
        Relation timeline)
    {
        return Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            initialTarget ?? ConceptSurface.UnknownPoint(),
            finalTarget ?? ConceptSurface.UnknownPoint(),
            transfers ?? ConceptSurface.UnknownPoint(),
            residues ?? ConceptSurface.UnknownPoint(),
            timeline ?? ConceptSurface.UnknownPoint());
    }

    public static bool TrySettle(
        Relation initialTarget,
        IReadOnlyList<Relation> transferSurfaces,
        out Relation runSurface)
    {
        if (initialTarget is null)
        {
            runSurface = null!;
            return false;
        }

        var transfers = CleanRelations(transferSurfaces);
        var current = initialTarget;
        var time = new Carrier();
        var steps = new List<Relation>();
        var residues = new List<IElement>();

        for (var index = 0; index < transfers.Length; index++)
        {
            if (!BoundaryFlowTargetSettlement.TrySettle(current, transfers[index], out var settlement) ||
                !BoundaryFlowTargetSettlement.TryRead(
                    settlement,
                    out var before,
                    out var transition,
                    out var after,
                    out var residue,
                    out _))
            {
                runSurface = null!;
                return false;
            }

            steps.Add(TimeStepSequence.Step(
                new Carrier(),
                time,
                index,
                index + 1,
                before,
                transition,
                after,
                residue));
            residues.Add(residue);
            current = after;
        }

        var timeline = TimeStepSequence.Create(new Carrier(), time, initialTarget, [.. steps]);
        runSurface = Create(
            new Carrier(),
            initialTarget,
            current,
            Relation.OrderedGroup(new Carrier(), transfers),
            Relation.OrderedGroup(new Carrier(), [.. residues]),
            timeline);
        return true;
    }

    public static bool TrySettle(
        Relation initialTarget,
        out Relation runSurface,
        params Relation[] transferSurfaces) =>
        TrySettle(initialTarget, transferSurfaces, out runSurface);

    public static bool TryRead(
        Relation runSurface,
        out Relation initialTarget,
        out Relation finalTarget,
        out Relation transfers,
        out Relation residues,
        out Relation timeline)
    {
        if (ConceptSurfaceReader.TryReadRootElements(runSurface, out var elements) &&
            elements.Length >= 5 &&
            elements[0] is Relation initial &&
            elements[1] is Relation final &&
            elements[2] is Relation transferGroup &&
            elements[3] is Relation residueGroup &&
            elements[4] is Relation time)
        {
            initialTarget = initial;
            finalTarget = final;
            transfers = transferGroup;
            residues = residueGroup;
            timeline = time;
            return true;
        }

        initialTarget = null!;
        finalTarget = null!;
        transfers = null!;
        residues = null!;
        timeline = null!;
        return false;
    }

    public static bool TryReadFinal(
        Relation runSurface,
        out BoundaryFlowValueRead finalRead)
    {
        if (TryRead(runSurface, out _, out var final, out _, out _, out _))
        {
            return BoundaryFlowValueSurface.TryReadValue(final, out finalRead);
        }

        finalRead = default;
        return false;
    }

    public static bool TryReadTimeline(Relation runSurface, out Relation timeline)
    {
        if (TryRead(runSurface, out _, out _, out _, out _, out var candidate) &&
            TimeStepSequence.TryRead(candidate, out _, out _, out _))
        {
            timeline = candidate;
            return true;
        }

        timeline = null!;
        return false;
    }

    public static bool TryReadTotals(
        Relation runSurface,
        out BoundaryFlowSettlementRunTotals totals)
    {
        if (!TryReadTimeline(runSurface, out var timeline) ||
            !TimeStepSequence.TryRead(timeline, out _, out _, out var steps))
        {
            totals = default;
            return false;
        }

        var accepted = 0L;
        var blocked = 0L;
        foreach (var step in steps)
        {
            if (!TimeStepSequence.TryReadStep(
                    step,
                    out _,
                    out _,
                    out _,
                    out _,
                    out var transition,
                    out _,
                    out _) ||
                transition is not Relation transfer ||
                !BoundaryFlowValueTransfer.TryReadSummary(transfer, out var transferRead))
            {
                totals = default;
                return false;
            }

            if (transferRead.HasAccepted)
            {
                accepted = checked(accepted + transferRead.Accepted.Magnitude.Ticks);
            }

            blocked = checked(blocked + transferRead.Blocked.Magnitude.Ticks);
        }

        totals = new BoundaryFlowSettlementRunTotals(accepted, blocked, steps.Length);
        return true;
    }

    private static Relation[] CleanRelations(IReadOnlyList<Relation> relations)
    {
        if (relations is null || relations.Count == 0)
        {
            return [];
        }

        return [.. relations.Where(relation => relation is not null)];
    }
}

public readonly record struct BoundaryFlowSettlementRunTotals(
    long AcceptedMagnitude,
    long BlockedMagnitude,
    int StepCount);

/// <summary>
/// Builds and reads boundary/domain exposure surfaces. A boundary surface
/// records whether left and right boundary sides are present, plus the
/// selected whole/axis being described.
/// </summary>
public static class BoundarySurface
{
    public static Relation Create(
        Carrier root,
        BoundaryExposure exposure,
        IElement wholeOrAxis,
        IElement leftBoundary,
        IElement rightBoundary)
    {
        var selected = wholeOrAxis ?? ConceptSurface.UnknownPoint();
        if (exposure.IsBoundedWhole)
        {
            return BoundedWholeSurface.Create(
                ConceptSurface.RootOrNew(root),
                selected,
                leftBoundary as Relation ?? ConceptSurface.UnknownPoint(),
                rightBoundary as Relation ?? ConceptSurface.UnknownPoint());
        }

        var sides = new List<Relation>(2);
        if (exposure.IsPositive || exposure.IsFullSigned)
        {
            sides.Add(StrippedBoundarySideSurface.Positive(
                ConceptSurface.RootOrNew(root),
                selected,
                leftBoundary ?? ConceptSurface.UnknownPoint()));
        }

        if (exposure.IsNegative || exposure.IsFullSigned)
        {
            sides.Add(StrippedBoundarySideSurface.Negative(
                sides.Count == 0 ? ConceptSurface.RootOrNew(root) : new Carrier(),
                selected,
                rightBoundary ?? ConceptSurface.UnknownPoint()));
        }

        return Relation.OrderedGroup(ConceptSurface.RootOrNew(root), [.. sides]);
    }

    public static Relation Create(
        Carrier root,
        BoundaryExposure exposure,
        IElement wholeOrAxis) =>
        Create(root, exposure, wholeOrAxis, ConceptSurface.UnknownPoint(), ConceptSurface.UnknownPoint());

    public static Relation FullSigned(Carrier root, IElement axis) =>
        Create(root, BoundaryExposure.FullSigned, axis);

    public static Relation Positive(Carrier root, IElement axis, IElement leftBoundary) =>
        Create(root, BoundaryExposure.Positive, axis, leftBoundary, ConceptSurface.UnknownPoint());

    public static Relation Negative(Carrier root, IElement axis, IElement rightBoundary) =>
        Create(root, BoundaryExposure.Negative, axis, ConceptSurface.UnknownPoint(), rightBoundary);

    public static Relation BoundedWhole(
        Carrier root,
        IElement whole,
        IElement leftBoundary,
        IElement rightBoundary) =>
        Create(root, BoundaryExposure.BoundedWhole, whole, leftBoundary, rightBoundary);

    public static bool TryRead(
        Relation surface,
        out BoundaryExposure exposure,
        out IElement wholeOrAxis,
        out IElement leftBoundary,
        out IElement rightBoundary)
    {
        if (BoundarySideSurface.TryReadExposure(
                surface,
                out var sideExposure,
                out var selected,
                out var positiveSide,
                out var negativeSide))
        {
            exposure = sideExposure;
            wholeOrAxis = selected;
            leftBoundary = positiveSide;
            rightBoundary = negativeSide;
            return true;
        }

        if (BoundedWholeSurface.TryRead(
                surface,
                out var whole,
                out var left,
                out var right,
                out _))
        {
            exposure = BoundaryExposure.BoundedWhole;
            wholeOrAxis = whole;
            leftBoundary = left;
            rightBoundary = right;
            return true;
        }

        if (StrippedBoundarySideSurface.TryReadExposure(
                surface,
                out var strippedExposure,
                out var strippedSelected,
                out var strippedPositive,
                out var strippedNegative))
        {
            exposure = strippedExposure;
            wholeOrAxis = strippedSelected;
            leftBoundary = strippedPositive;
            rightBoundary = strippedNegative;
            return true;
        }

        exposure = default;
        wholeOrAxis = default!;
        leftBoundary = default!;
        rightBoundary = default!;
        return false;
    }
}

/// <summary>
/// Builds and reads flow exposure surfaces. Flow exposure is a local
/// structural read over forward and inverse flow evidence surfaces.
/// </summary>
public static class FlowSurface
{
    public static Relation Create(
        Carrier root,
        FlowExposure exposure,
        IElement selected,
        IElement forwardFlow,
        IElement inverseFlow)
    {
        var selectedSurface = selected ?? ConceptSurface.UnknownPoint();
        if (exposure.IsNone)
        {
            return NoFlowSurface.Create(
                ConceptSurface.RootOrNew(root),
                selectedSurface);
        }

        var faces = new List<Relation>(2);
        if (exposure.ForwardFlow)
        {
            faces.Add(CreateFace(
                FlowFace.Forward,
                selectedSurface,
                forwardFlow ?? ConceptSurface.UnknownPoint()));
        }

        if (exposure.InverseFlow)
        {
            faces.Add(CreateFace(
                FlowFace.Inverse,
                selectedSurface,
                inverseFlow ?? ConceptSurface.UnknownPoint()));
        }

        return faces.Count == 1
            ? faces[0]
            : Relation.OrderedGroup(ConceptSurface.RootOrNew(root), [.. faces]);
    }

    public static Relation Create(
        Carrier root,
        FlowExposure exposure,
        IElement selected) =>
        Create(root, exposure, selected, ConceptSurface.UnknownPoint(), ConceptSurface.UnknownPoint());

    public static Relation None(Carrier root, IElement selected) =>
        Create(root, FlowExposure.None, selected);

    public static Relation Forward(Carrier root, IElement selected, IElement forwardFlow) =>
        Create(root, FlowExposure.Forward, selected, forwardFlow, ConceptSurface.UnknownPoint());

    public static Relation Inverse(Carrier root, IElement selected, IElement inverseFlow) =>
        Create(root, FlowExposure.Inverse, selected, ConceptSurface.UnknownPoint(), inverseFlow);

    public static Relation Both(
        Carrier root,
        IElement selected,
        IElement forwardFlow,
        IElement inverseFlow) =>
        Create(root, FlowExposure.Both, selected, forwardFlow, inverseFlow);

    public static bool TryRead(
        Relation surface,
        out FlowExposure exposure,
        out IElement selected,
        out IElement forwardFlow,
        out IElement inverseFlow)
    {
        if (FlowFaceSurface.TryReadExposure(
                surface,
                out var faceExposure,
                out var faceSelected,
                out var forwardFace,
                out var inverseFace))
        {
            exposure = faceExposure;
            selected = faceSelected;
            forwardFlow = forwardFace;
            inverseFlow = inverseFace;
            return true;
        }

        if (NoFlowSurface.TryRead(surface, out var noFlowSelected, out _))
        {
            exposure = FlowExposure.None;
            selected = noFlowSelected;
            forwardFlow = ConceptSurface.UnknownPoint();
            inverseFlow = ConceptSurface.UnknownPoint();
            return true;
        }

        exposure = default;
        selected = default!;
        forwardFlow = default!;
        inverseFlow = default!;
        return false;
    }

    private static Relation CreateFace(FlowFace face, IElement selected, IElement evidence)
    {
        if (evidence is Relation existingFace &&
            FlowFaceSurface.TryRead(existingFace, out var readFace, out _, out _) &&
            readFace.Equals(face))
        {
            return existingFace;
        }

        return FlowFaceSurface.Create(new Carrier(), face, selected, evidence);
    }
}

/// <summary>
/// Pairs a boundary surface and a flow surface without collapsing their axes.
/// </summary>
public static class BoundaryFlowSurface
{
    public static Relation Create(
        Carrier root,
        Relation boundarySurface,
        Relation flowSurface,
        IElement witness)
    {
        return Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            boundarySurface ?? ConceptSurface.UnknownPoint(),
            flowSurface ?? ConceptSurface.UnknownPoint(),
            witness ?? ConceptSurface.UnknownPoint());
    }

    public static Relation Create(
        Carrier root,
        Relation boundarySurface,
        Relation flowSurface) =>
        Create(root, boundarySurface, flowSurface, ConceptSurface.UnknownPoint());

    public static Relation Create(
        Carrier root,
        BoundaryExposure boundary,
        FlowExposure flow,
        IElement selected)
    {
        if (BoundaryFlowPrimitiveEvidence.TryCreate(
                new Carrier(),
                boundary,
                flow,
                selected,
                out var primitiveBoundary,
                out var primitiveFlow))
        {
            return Create(root, primitiveBoundary, primitiveFlow);
        }

        var boundarySurface = BoundarySurface.Create(new Carrier(), boundary, selected);
        var flowSurface = FlowSurface.Create(new Carrier(), flow, selected);
        return Create(root, boundarySurface, flowSurface);
    }

    public static bool TryRead(
        Relation surface,
        out BoundaryExposure boundary,
        out FlowExposure flow,
        out Relation boundarySurface,
        out Relation flowSurface,
        out IElement witness)
    {
        if (ConceptSurfaceReader.TryReadRootElements(surface, out var elements) &&
            elements.Length >= 3 &&
            elements[0] is Relation boundaryCandidate &&
            elements[1] is Relation flowCandidate &&
            BoundarySurface.TryRead(boundaryCandidate, out var boundaryExposure, out _, out _, out _) &&
            FlowSurface.TryRead(flowCandidate, out var flowExposure, out _, out _, out _))
        {
            boundary = boundaryExposure;
            flow = flowExposure;
            boundarySurface = boundaryCandidate;
            flowSurface = flowCandidate;
            witness = elements[2];
            return true;
        }

        boundary = default;
        flow = default;
        boundarySurface = null!;
        flowSurface = null!;
        witness = default!;
        return false;
    }

    public static bool TryRead(
        Relation surface,
        out BoundaryFlowExposure exposure)
    {
        if (TryRead(surface, out var boundary, out var flow, out _, out _, out _))
        {
            exposure = new BoundaryFlowExposure(boundary, flow);
            return true;
        }

        exposure = default;
        return false;
    }
}

/// <summary>
/// Packages the boundary/domain exposure that belongs to a carrier-like axis.
/// This surface intentionally does not own flow exposure.
/// </summary>
public static class CarrierBoundarySurface
{
    public static Relation Create(
        Carrier root,
        Carrier carrier,
        Relation boundarySurface,
        IElement partition)
    {
        var axisCarrier = carrier ?? Carrier.Unknown;
        var selected = axisCarrier.IsUnknown
            ? (IElement)ConceptSurface.UnknownPoint()
            : axisCarrier;

        return Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            selected,
            boundarySurface ?? ConceptSurface.UnknownPoint(),
            partition ?? ConceptSurface.UnknownPoint());
    }

    public static Relation Create(
        Carrier root,
        Carrier carrier,
        Relation boundarySurface) =>
        Create(root, carrier, boundarySurface, ConceptSurface.UnknownPoint());

    public static Relation Create(
        Carrier root,
        Carrier carrier,
        BoundaryExposure boundary,
        IElement leftBoundary,
        IElement rightBoundary,
        IElement partition)
    {
        var axisCarrier = carrier ?? Carrier.Unknown;
        var selected = axisCarrier.IsUnknown
            ? (IElement)ConceptSurface.UnknownPoint()
            : axisCarrier;
        var boundarySurface = BoundarySurface.Create(
            new Carrier(),
            boundary,
            selected,
            leftBoundary ?? ConceptSurface.UnknownPoint(),
            rightBoundary ?? ConceptSurface.UnknownPoint());

        return Create(root, axisCarrier, boundarySurface, partition);
    }

    public static Relation Create(
        Carrier root,
        Carrier carrier,
        BoundaryExposure boundary) =>
        Create(
            root,
            carrier,
            boundary,
            ConceptSurface.UnknownPoint(),
            ConceptSurface.UnknownPoint(),
            ConceptSurface.UnknownPoint());

    public static Relation FullSigned(Carrier root, Carrier carrier) =>
        Create(root, carrier, BoundaryExposure.FullSigned);

    public static Relation Positive(Carrier root, Carrier carrier, IElement leftBoundary) =>
        Create(root, carrier, BoundaryExposure.Positive, leftBoundary, ConceptSurface.UnknownPoint(), ConceptSurface.UnknownPoint());

    public static Relation Negative(Carrier root, Carrier carrier, IElement rightBoundary) =>
        Create(root, carrier, BoundaryExposure.Negative, ConceptSurface.UnknownPoint(), rightBoundary, ConceptSurface.UnknownPoint());

    public static Relation BoundedWhole(
        Carrier root,
        Carrier carrier,
        IElement leftBoundary,
        IElement rightBoundary,
        IElement partition) =>
        Create(root, carrier, BoundaryExposure.BoundedWhole, leftBoundary, rightBoundary, partition);

    public static bool TryRead(
        Relation surface,
        out Carrier carrier,
        out BoundaryExposure boundary,
        out Relation boundarySurface,
        out IElement partition)
    {
        if (ConceptSurfaceReader.TryReadRootElements(surface, out var elements) &&
            elements.Length >= 3 &&
            elements[0] is Carrier selectedCarrier &&
            elements[1] is Relation boundaryCandidate &&
            BoundarySurface.TryRead(boundaryCandidate, out var boundaryRead, out _, out _, out _))
        {
            carrier = selectedCarrier;
            boundary = boundaryRead;
            boundarySurface = boundaryCandidate;
            partition = elements[2];
            return true;
        }

        carrier = null!;
        boundary = default;
        boundarySurface = null!;
        partition = default!;
        return false;
    }
}

/// <summary>
/// Packages flow exposure that belongs to a relation/traversal surface. This
/// surface intentionally does not own carrier boundary/domain meaning.
/// </summary>
public static class RelationFlowSurface
{
    public static Relation Create(
        Carrier root,
        Relation relation,
        Relation flowSurface,
        IElement gateOrCapacity)
    {
        var selected = relation ?? ConceptSurface.UnknownPoint();

        return Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            selected,
            flowSurface ?? ConceptSurface.UnknownPoint(),
            gateOrCapacity ?? ConceptSurface.UnknownPoint());
    }

    public static Relation Create(
        Carrier root,
        Relation relation,
        Relation flowSurface) =>
        Create(root, relation, flowSurface, ConceptSurface.UnknownPoint());

    public static Relation Create(
        Carrier root,
        Relation relation,
        FlowExposure flow,
        IElement forwardFlow,
        IElement inverseFlow,
        IElement gateOrCapacity)
    {
        var selected = relation ?? ConceptSurface.UnknownPoint();
        var flowSurface = FlowSurface.Create(
            new Carrier(),
            flow,
            selected,
            forwardFlow ?? ConceptSurface.UnknownPoint(),
            inverseFlow ?? ConceptSurface.UnknownPoint());

        return Create(root, selected, flowSurface, gateOrCapacity);
    }

    public static Relation Create(Carrier root, Relation relation, FlowExposure flow) =>
        Create(root, relation, flow, ConceptSurface.UnknownPoint(), ConceptSurface.UnknownPoint(), ConceptSurface.UnknownPoint());

    public static Relation None(Carrier root, Relation relation) =>
        Create(root, relation, FlowExposure.None);

    public static Relation Forward(Carrier root, Relation relation, IElement forwardFlow) =>
        Create(root, relation, FlowExposure.Forward, forwardFlow, ConceptSurface.UnknownPoint(), ConceptSurface.UnknownPoint());

    public static Relation Inverse(Carrier root, Relation relation, IElement inverseFlow) =>
        Create(root, relation, FlowExposure.Inverse, ConceptSurface.UnknownPoint(), inverseFlow, ConceptSurface.UnknownPoint());

    public static Relation Both(
        Carrier root,
        Relation relation,
        IElement forwardFlow,
        IElement inverseFlow,
        IElement gateOrCapacity) =>
        Create(root, relation, FlowExposure.Both, forwardFlow, inverseFlow, gateOrCapacity);

    public static bool TryRead(
        Relation surface,
        out Relation relation,
        out FlowExposure flow,
        out Relation flowSurface,
        out IElement forwardFlow,
        out IElement inverseFlow,
        out IElement gateOrCapacity)
    {
        if (ConceptSurfaceReader.TryReadRootElements(surface, out var elements) &&
            elements.Length >= 3 &&
            elements[0] is Relation selectedRelation &&
            elements[1] is Relation flowCandidate &&
            FlowSurface.TryRead(
                flowCandidate,
                out var flowRead,
                out _,
                out var forward,
                out var inverse))
        {
            relation = selectedRelation;
            flow = flowRead;
            flowSurface = flowCandidate;
            forwardFlow = forward;
            inverseFlow = inverse;
            gateOrCapacity = elements[2];
            return true;
        }

        relation = null!;
        flow = default;
        flowSurface = null!;
        forwardFlow = default!;
        inverseFlow = default!;
        gateOrCapacity = default!;
        return false;
    }

    public static bool TryDual(Relation surface, out Relation dual)
    {
        if (!TryRead(
                surface,
                out var relation,
                out var flow,
                out _,
                out var forwardFlow,
                out var inverseFlow,
                out var gateOrCapacity))
        {
            dual = null!;
            return false;
        }

        dual = Create(
            new Carrier(),
            relation,
            flow.Dual(),
            inverseFlow,
            forwardFlow,
            gateOrCapacity);
        return true;
    }
}

/// <summary>
/// Packages the boundary/domain and flow exposure of a carrier-like axis as
/// inspectable relation structure. Current <see cref="Carrier"/> instances are
/// still materialized faces; this surface records the lower-resolution axis
/// view around one selected carrier without changing carrier identity.
/// </summary>
public static class CarrierAxisSurface
{
    public static Relation Create(
        Carrier root,
        Carrier carrier,
        Relation boundarySurface,
        Relation flowSurface,
        IReadOnlyList<Relation> transitions)
    {
        var axisCarrier = carrier ?? Carrier.Unknown;
        var selected = axisCarrier.IsUnknown
            ? (IElement)ConceptSurface.UnknownPoint()
            : axisCarrier;
        var cleanTransitions = transitions is null
            ? []
            : transitions.Where(transition => transition is not null).Cast<IElement>().ToArray();

        return Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            [selected, boundarySurface ?? ConceptSurface.UnknownPoint(), flowSurface ?? ConceptSurface.UnknownPoint(), .. cleanTransitions]);
    }

    public static Relation Create(
        Carrier root,
        Carrier carrier,
        Relation boundarySurface,
        Relation flowSurface) =>
        Create(root, carrier, boundarySurface, flowSurface, []);

    public static Relation Create(
        Carrier root,
        Carrier carrier,
        BoundaryExposure boundary,
        FlowExposure flow,
        IElement boundaryWitness,
        IElement flowWitness)
    {
        var axisCarrier = carrier ?? Carrier.Unknown;
        var selected = axisCarrier.IsUnknown
            ? (IElement)ConceptSurface.UnknownPoint()
            : axisCarrier;
        if (!BoundaryFlowPrimitiveEvidence.TryCreate(
                new Carrier(),
                boundary,
                flow,
                selected,
                boundaryWitness ?? ConceptSurface.UnknownPoint(),
                flowWitness ?? ConceptSurface.UnknownPoint(),
                out var boundarySurface,
                out var flowSurface))
        {
            boundarySurface = BoundarySurface.Create(
                new Carrier(),
                boundary,
                selected,
                boundaryWitness ?? ConceptSurface.UnknownPoint(),
                boundaryWitness ?? ConceptSurface.UnknownPoint());
            flowSurface = FlowSurface.Create(
                new Carrier(),
                flow,
                selected,
                flowWitness ?? ConceptSurface.UnknownPoint(),
                flowWitness ?? ConceptSurface.UnknownPoint());
        }

        var transitions = BoundaryFlowLaneTransitionSurface.BoundaryDualTransitions(
            new Carrier(),
            boundary,
            flow);
        return Create(root, axisCarrier, boundarySurface, flowSurface, transitions);
    }

    public static Relation Create(
        Carrier root,
        Carrier carrier,
        BoundaryExposure boundary,
        FlowExposure flow) =>
        Create(root, carrier, boundary, flow, ConceptSurface.UnknownPoint(), ConceptSurface.UnknownPoint());

    public static Relation PositiveUnit(Carrier root, Carrier carrier) =>
        Create(root, carrier, BoundaryExposure.Positive, FlowExposure.Forward);

    public static Relation NegativeUnit(Carrier root, Carrier carrier) =>
        Create(root, carrier, BoundaryExposure.Negative, FlowExposure.Forward);

    public static Relation SignedForward(Carrier root, Carrier carrier) =>
        Create(root, carrier, BoundaryExposure.FullSigned, FlowExposure.Forward);

    public static Relation SignedBothFlows(Carrier root, Carrier carrier) =>
        Create(root, carrier, BoundaryExposure.FullSigned, FlowExposure.Both);

    public static Relation BoundedWholeNoFlow(Carrier root, Carrier carrier) =>
        Create(root, carrier, BoundaryExposure.BoundedWhole, FlowExposure.None);

    public static bool TryRead(
        Relation surface,
        out Carrier carrier,
        out BoundaryExposure boundary,
        out FlowExposure flow,
        out Relation boundarySurface,
        out Relation flowSurface)
    {
        if (ConceptSurfaceReader.TryReadRootElements(surface, out var elements) &&
            elements.Length >= 3 &&
            elements[0] is Carrier selectedCarrier &&
            elements[1] is Relation boundaryCandidate &&
            elements[2] is Relation flowCandidate &&
            BoundarySurface.TryRead(boundaryCandidate, out var boundaryRead, out _, out _, out _) &&
            FlowSurface.TryRead(flowCandidate, out var flowRead, out _, out _, out _))
        {
            carrier = selectedCarrier;
            boundary = boundaryRead;
            flow = flowRead;
            boundarySurface = boundaryCandidate;
            flowSurface = flowCandidate;
            return true;
        }

        carrier = null!;
        boundary = default;
        flow = default;
        boundarySurface = null!;
        flowSurface = null!;
        return false;
    }

    public static bool TryReadLegalLanes(
        Relation surface,
        out BoundaryFlowLane[] lanes)
    {
        if (TryRead(surface, out _, out var boundary, out var flow, out _, out _))
        {
            lanes = BoundaryFlowLaneBasis.LegalLanes(boundary, flow);
            return true;
        }

        lanes = [];
        return false;
    }

    /// <summary>
    /// Reads a structurally dualed axis surface without mutating the source.
    /// Boundary sides and flow exposure are dualed; no-flow and both-flow
    /// remain stable under flow dualing.
    /// </summary>
    public static bool TryDual(Relation surface, out Relation dual)
    {
        if (!TryRead(surface, out var carrier, out var boundary, out var flow, out _, out _))
        {
            dual = null!;
            return false;
        }

        dual = Create(new Carrier(), carrier, boundary.Dual(), flow.Dual());
        return true;
    }
}
