using Gojo.Core;
using Gojo.Structures;

namespace Gojo.ConceptSurface;

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
    /// Reads a carrier axis as the common witness skeleton: selected carrier
    /// through axis surface yields its lane basis, with boundary/flow evidence
    /// preserved as provenance.
    /// </summary>
    public static bool TryReadLaneBasisWitness(
        Relation surface,
        Carrier root,
        out Relation witness)
    {
        if (TryReadLaneBasisRead(surface, root, out var basisRead))
        {
            return BasisReadSurface.TryReadWitness(basisRead, root, out witness);
        }

        witness = null!;
        return false;
    }

    /// <summary>
    /// Reads a carrier axis into the common basis-read surface: selected
    /// carrier, axis evidence, legal lane basis, unresolved metric evidence,
    /// unresolved residue, and boundary/flow provenance.
    /// </summary>
    public static bool TryReadLaneBasisRead(
        Relation surface,
        Carrier root,
        out Relation basisRead)
    {
        if (TryRead(
                surface,
                out var carrier,
                out var boundary,
                out var flow,
                out var boundarySurface,
                out var flowSurface))
        {
            var selected = carrier.IsUnknown
                ? (IElement)ConceptSurface.UnknownPoint()
                : carrier;
            var laneBasis = BoundaryFlowLaneBasis.Create(
                new Carrier(),
                boundary,
                flow,
                selected);
            var provenance = Relation.OrderedGroup(
                new Carrier(),
                boundarySurface,
                flowSurface);

            basisRead = BasisReadSurface.Create(
                ConceptSurface.RootOrNew(root),
                selected,
                surface,
                laneBasis,
                ConceptSurface.UnknownPoint(),
                ConceptSurface.UnknownPoint(),
                provenance);
            return true;
        }

        basisRead = null!;
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

