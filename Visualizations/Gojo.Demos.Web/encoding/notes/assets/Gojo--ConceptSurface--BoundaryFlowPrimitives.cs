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
    /// Reads a lane-basis surface as a common basis read. The paired
    /// boundary/flow surface is kept as axis/face evidence, while the lane
    /// basis itself remains the basis result.
    /// </summary>
    public static bool TryReadBasisRead(
        Relation basis,
        Carrier root,
        out Relation basisRead)
    {
        if (ConceptSurfaceReader.TryReadRootElements(basis, out var elements) &&
            elements.Length >= 2 &&
            elements[0] is Relation boundaryFlowSurface &&
            TryRead(basis, out _, out var selected, out _))
        {
            basisRead = BasisReadSurface.Create(
                ConceptSurface.RootOrNew(root),
                selected,
                boundaryFlowSurface,
                basis,
                ConceptSurface.UnknownPoint(),
                ConceptSurface.UnknownPoint(),
                basis);
            return true;
        }

        basisRead = null!;
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

